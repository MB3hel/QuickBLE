using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Devices.Radios;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth.Advertisement;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace QuickBLE {
    public class BLEClient {
        #region Variables and Properties

        private const string UNKNOWN_WRITING_DEV_ADDRESS = "unknown";

        // Platform Specific Objects
        private List<GattDeviceService> serviceObjects = new List<GattDeviceService>();
        private List<GattCharacteristic> characteristicObjects = new List<GattCharacteristic>();
        private List<GattDescriptor> descriptorObjects = new List<GattDescriptor>();
        // UUIDs for Gatt Objects
        /// <summary>
        /// The services available on the connected server.
        /// </summary>
        public List<string> Services { get; private set; } = new List<string>();
        /// <summary>
        /// The characteristics available on the connected server.
        /// </summary>
        public List<string> Characteristics { get; private set; } = new List<string>();
        /// <summary>
        /// The descriptors available on the connected server.
        /// </summary>
        public List<string> Descriptors { get; private set; } = new List<string>();
        /// <summary>
        /// The services that are being scanned for when scanning for devices
        /// </summary>
        public List<string> ScanServices { get; private set; } = new List<string>();

        // Which characteristics we are subscribed to notifications from
        private List<GattCharacteristic> subscribeCharacteristics = new List<GattCharacteristic>();

        // Devices
        List<ulong> deviceAddresses = new List<ulong>();
        List<BluetoothLEDevice> devices = new List<BluetoothLEDevice>();

        // Status
        /// <summary>
        /// Is the client scanning for servers
        /// </summary>
        public bool IsScanning { get; private set; } = false;
        /// <summary>
        /// Is the client connected to a server
        /// </summary>
        public bool IsConnected { get; private set; } = false;

        // Enable BT Dialog Text
        public string REQUEST_BT_TITLE = "Bluetooth Required";
        public string REQUEST_BT_MESSAGE = "Please enable bluetooth in Settings";
        public string REQUEST_BT_CONFIRM = "Settings";
        public string REQUEST_BT_DENY = "Cancel";

        // UWP Bluetooth stuff
        BluetoothLEAdvertisementWatcher watcher;
        BluetoothLEDevice connectedDevice;
        BluetoothAdapter adapter;

        // The delegate
        private BLEDelegate clientDelegate;

        private CoreDispatcher mainThread = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;

#endregion

        /// <summary>
        /// Create a new BLEClient for Bluetooth Gatt central role
        /// </summary>
        /// <param name="clientDelegate"></param>
        public BLEClient(BLEDelegate clientDelegate) {
            this.clientDelegate = clientDelegate;
            Task.Run(async () => {
                int error = await CheckBluetooth();
                if (error != BtError.NoBluetooth && error != BtError.NoBLE && error != BtError.NoServer) {
                    var lastState = error == BtError.None;
                    // Watch for bt power changes
                    while (true) {
                        var e = await CheckBluetooth();
                        var state = e == BtError.None;
                        if (state != lastState) {
                            lastState = state;
                            // Should not wait for this. That would slow down checking for power changes
                            mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                                clientDelegate.OnBluetoothPowerChanged(state);
                            });
                        }
                        await Task.Delay(100);
                    }
                }
            });
        }

        #region Client Control
        /// <summary>
        /// Scan for devices advertising a certain service
        /// </summary>
        /// <param name="service">The service to scan for</param>
        /// <param name="scanFor">Whether or not to scan for the service</param>
        public void ScanForService(string service, bool scanFor = true) {
            if (scanFor && !ScanServices.Contains(service.ToUpper())) {
                ScanServices.Add(service.ToUpper());
            }
            if (!scanFor && ScanServices.Contains(service.ToUpper())) {
                ScanServices.Remove(service);
            }
        }
        /// <summary>
        /// Check bluetooth low energy client compatibility for the device
        /// </summary>
        /// <returns>An error code (BtError)</returns>
        public async Task<int> CheckBluetooth() {
            var radios = await Radio.GetRadiosAsync();
            var r = radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
            if (r == null) {
                return BtError.NoBluetooth;
            }
            adapter = await BluetoothAdapter.GetDefaultAsync();
            if (adapter != null) {
                if (!adapter.IsLowEnergySupported || !adapter.IsCentralRoleSupported) {
                    return BtError.NoBLE;
                }
            }
            if (r.State != RadioState.On) {
                return BtError.Disabled;
            }
            return BtError.None;
            return BtError.Unsupported;
        }
        /// <summary>
        /// Show a dialog requesting that the user enable bluetooth
        /// </summary>
        public async Task RequestEnableBt() {
            ContentDialog locationPromptDialog = new ContentDialog {
                Title = REQUEST_BT_TITLE,
                Content = REQUEST_BT_MESSAGE,
                CloseButtonText = REQUEST_BT_DENY,
                PrimaryButtonText = REQUEST_BT_CONFIRM
            };
            ContentDialogResult result = await locationPromptDialog.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(@"ms-settings:bluetooth"));
            }
        }
        /// <summary>
        /// Start scanning for devices
        /// </summary>
        /// <returns></returns>
        public async Task<int> ScanForDevices() {
            if (!IsScanning && !IsConnected) {
                Services.Clear();
                Characteristics.Clear();
                Descriptors.Clear();
                serviceObjects.Clear();
                characteristicObjects.Clear();
                descriptorObjects.Clear();
                subscribeCharacteristics.Clear();
                int error = await CheckBluetooth();
                if (error != BtError.None) {
                    return error;
                }
                watcher = new BluetoothLEAdvertisementWatcher();
                watcher.Received += DeviceDiscovered;
                watcher.Start();
                IsScanning = true;
                return BtError.None;
            } else {
                return BtError.AlreadyRunning;
            }
            return BtError.Unsupported;
        }
        /// <summary>
        /// Stop scanning
        /// </summary>
        public void StopScanning() {
            if (IsScanning) {
                watcher.Stop();
                watcher = null;
                IsScanning = false;
            }
        }
        /// <summary>
        /// Connect to a discovered device
        /// </summary>
        /// <param name="deviceAddress">The address of the device to connect to</param>
        public async void ConnectToDevice(string deviceAddress) {
            BluetoothLEDevice device = devices.First(f => (f.BluetoothAddress + "").ToUpper().Equals(deviceAddress.ToUpper()));
            GattDeviceServicesResult result = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (result.Status == GattCommunicationStatus.Success) {
                IsConnected = true;
                connectedDevice = device;
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnConnectToDevice((device.BluetoothAddress + "").ToUpper(), device.Name, true);
                });
                foreach (GattDeviceService service in result.Services) {
                    await AddService(service);
                }
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnServicesDiscovered();
                });
            } else {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnConnectToDevice((device.BluetoothAddress + "").ToUpper(), device.Name, false);
                });
            }
        }
        /// <summary>
        /// Disconnect from a server if connected
        /// </summary>
        public void Disconnect() {
            if (IsConnected) {
                // Don't need to watch value changes anymore
                foreach (GattCharacteristic c in characteristicObjects) {
                    c.ValueChanged -= CharacteristicValueChanged;
                }
                // Unsubscribe from characteristics
                foreach (GattCharacteristic c in subscribeCharacteristics) {
                    SubscribeToCharacteristic(c.Uuid.ToString(), false);
                }
                int i = devices.IndexOf(connectedDevice);
                connectedDevice.Dispose();
                connectedDevice = null;
                // Make sure the disposed object will not be used again
                // Force it to get a new device id
                devices.RemoveAt(i);
                deviceAddresses.RemoveAt(i);
                Services.Clear();
                Characteristics.Clear();
                Descriptors.Clear();
                serviceObjects.Clear();
                characteristicObjects.Clear();
                descriptorObjects.Clear();
                subscribeCharacteristics.Clear();
                IsConnected = false;
            }
        }
        /// <summary>
        /// Subscribe to a characteristic to receive notifications when its value is changed
        /// </summary>
        /// <param name="characteristic">The characteristic to subscribe to</param>
        /// <param name="subscribe">Whether not to subscribe to the characteristic (false to unsubscribe)</param>
        public async void SubscribeToCharacteristic(string characteristic, bool subscribe = true) {
            GattCharacteristic c = GetCharacteristic(new Guid(characteristic));
            if (c == null)
                return;
            var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.None;
            if (subscribe) {
                if (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate)) {
                    cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate;
                } else if (c.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify)) {
                    cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
                }
            }
            GattCommunicationStatus result = GattCommunicationStatus.ProtocolError;
            try {
                result = await c.WriteClientCharacteristicConfigurationDescriptorAsync(cccdValue);
            } catch (Exception e) {

            }
            if (result != GattCommunicationStatus.Success) {
                //Check Bt Permissions
            } else {
                subscribeCharacteristics.Add(c);
                c.ValueChanged += CharacteristicValueChanged;
            }
        }

        /// <summary>
        /// Get a service from a UUID
        /// </summary>
        /// <param name="service">THe UUID of the service</param>
        /// <returns>The GattLocalService or null</returns>
        private GattDeviceService GetService(Guid service) {
            return serviceObjects.FirstOrDefault(s => s.Uuid.Equals(service));
            return null;
        }
        /// <summary>
        /// Get a characteristic from a UUID
        /// </summary>
        /// <param name="characteristic">The UUID of the characteristic</param>
        /// <returns>The BLECharacteristic or null</returns>
        private GattCharacteristic GetCharacteristic(Guid characteristic) {
            return characteristicObjects.FirstOrDefault(c => c.Uuid.Equals(characteristic));
            return null;
        }
        /// <summary>
        /// Get a descriptor from a UUID
        /// </summary>
        /// <param name="descriptor">The UUID of the descriptor</param>
        /// <returns>The BLEDescriptor or null</returns>
        private GattDescriptor GetDescriptor(Guid descriptor) {
            return descriptorObjects.FirstOrDefault(d => d.Uuid.Equals(descriptor));
            return null;
        }
        /// <summary>
        ///  Add a service (and it's included services, characteristics, and descriptors) to the lists
        /// </summary>
        /// <param name="service">The service to add</param>
        private async Task AddService(GattDeviceService service) {
            if (!serviceObjects.Contains(service)) {
                GattDeviceServicesResult res = await service.GetIncludedServicesAsync();
                if (res.Status == GattCommunicationStatus.Success) {
                    foreach (GattDeviceService s in res.Services) {
                        await AddService(s);
                    }
                }
                GattCharacteristicsResult charRes = await service.GetCharacteristicsAsync();
                if (charRes.Status == GattCommunicationStatus.Success) {
                    foreach (GattCharacteristic c in charRes.Characteristics) {
                        GattDescriptorsResult descRes = await c.GetDescriptorsAsync();
                        if (descRes.Status == GattCommunicationStatus.Success) {
                            foreach (GattDescriptor d in descRes.Descriptors) {
                                descriptorObjects.Add(d);
                                Descriptors.Add(d.Uuid.ToString().ToUpper());
                            }
                        }
                        characteristicObjects.Add(c);
                        Characteristics.Add(c.Uuid.ToString().ToUpper());
                    }
                }
                serviceObjects.Add(service);
                Services.Add(service.Uuid.ToString().ToUpper());
            }
        }

        #endregion

        #region Characteristics and Descriptors
        /// <summary>
        /// Read the value of a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to read</param>
        public async void ReadCharacteristic(string characteristic) {
            GattCharacteristic c = GetCharacteristic(new Guid(characteristic));
            if (c == null) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnCharacteristicRead(characteristic.ToUpper(), UNKNOWN_WRITING_DEV_ADDRESS, false, null);
                });
                return;
            }
            GattReadResult result = null;
            try {
                result = await c.ReadValueAsync();
            } catch (Exception e) {

            }
            if (result != null && result.Status == GattCommunicationStatus.Success) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnCharacteristicRead(characteristic.ToUpper(), UNKNOWN_WRITING_DEV_ADDRESS, true, result.Value.ToArray());
                });
            } else {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnCharacteristicRead(characteristic.ToUpper(), UNKNOWN_WRITING_DEV_ADDRESS, false, null);
                });
            }
        }
        /// <summary>
        /// Write a value to a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to write to</param>
        /// <param name="value">The value to write</param>
        public async void WriteCharacteristic(string characteristic, byte[] value) {
            GattCharacteristic c = GetCharacteristic(new Guid(characteristic));
            if (c == null) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnCharacteristicWrite(characteristic.ToUpper(), false, null);
                });
                return;
            }
            GattWriteResult result = null;
            try {
                result = await c.WriteValueWithResultAsync(WindowsRuntimeBufferExtensions.AsBuffer(value));
            } catch (Exception e) {

            }
            if (result?.Status == GattCommunicationStatus.Success) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnCharacteristicWrite(characteristic.ToUpper(), true, value);
                });
            } else {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnCharacteristicWrite(characteristic.ToUpper(), false, null);
                });
            }
        }
        /// <summary>
        /// Read the value of a descriptor
        /// </summary>
        /// <param name="descriptor">The descriptor to read</param>
        public async void ReadDescriptor(string descriptor) {
            GattDescriptor d = GetDescriptor(new Guid(descriptor));
            if (d == null) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnDescriptorRead(descriptor.ToUpper(), UNKNOWN_WRITING_DEV_ADDRESS, false, null);
                });
                return;
            }
            GattReadResult result = null;
            try {
                result = await d.ReadValueAsync();
            } catch (Exception e) {

            }
            if (result?.Status == GattCommunicationStatus.Success) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnDescriptorRead(descriptor.ToUpper(), UNKNOWN_WRITING_DEV_ADDRESS, true, result.Value.ToArray());
                });
            } else {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnDescriptorRead(descriptor.ToUpper(), UNKNOWN_WRITING_DEV_ADDRESS, false, null);
                });
            }
        }
        /// <summary>
        /// Write a value to a descriptor
        /// </summary>
        /// <param name="descriptor">The descriptor to write to</param>
        /// <param name="value">The value to write</param>
        public async void WriteDescriptor(string descriptor, byte[] value) {
            GattDescriptor d = GetDescriptor(new Guid(descriptor));
            if (d == null) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnDescriptorWrite(descriptor.ToUpper(), false, null);
                });
                return;
            }
            GattWriteResult result = null;
            try {
                result = await d.WriteValueWithResultAsync(WindowsRuntimeBufferExtensions.AsBuffer(value));
            } catch (Exception e) {

            }
            if (result?.Status == GattCommunicationStatus.Success) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnDescriptorWrite(descriptor.ToUpper(), true, value);
                });
            } else {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnDescriptorWrite(descriptor.ToUpper(), false, null);
                });
            }
        }

        /// <summary>
        /// Check if the connected server has a service
        /// </summary>
        /// <param name="service">The service to check for</param>
        /// <returns>Whether or not the connected server has the service</returns>
        public bool HasService(string service) {
            return Services.Contains(service.ToUpper());
        }
        /// <summary>
        /// Check if the connected server has a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to check for</param>
        /// <returns>Whether or not the connected server has the characteristic</returns>
        public bool HasCharacteristic(string characteristic) {
            return Characteristics.Contains(characteristic.ToUpper());
        }
        /// <summary>
        /// Check if the connected server has a descriptor
        /// </summary>
        /// <param name="descriptor">The descriptor to check for</param>
        /// <returns>Whether or not the connected server has the descriptor</returns>
        public bool HasDescriptor(string descriptor) {
            return Descriptors.Contains(descriptor.ToUpper());
        }
#endregion

        #region Event Handlers
        // AdvertisementWatcher Detected device
        private async void DeviceDiscovered(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args) {
            bool returnDevice = ScanServices.Count == 0;
            if (!returnDevice) {
                foreach (Guid uuid in args.Advertisement.ServiceUuids) {
                    returnDevice = ScanServices.Contains(uuid.ToString().ToUpper());
                    if (returnDevice)
                        break;
                }
            }
            if (returnDevice) {
                BluetoothLEDevice device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                if (!deviceAddresses.Contains(device.BluetoothAddress)) {
                    devices.Add(device);
                    deviceAddresses.Add(device.BluetoothAddress);
                }
                string advertisedName = device.Name;
                List<BluetoothLEAdvertisementDataSection> complete = new List<BluetoothLEAdvertisementDataSection>(args.Advertisement.GetSectionsByType(BluetoothLEAdvertisementDataTypes.CompleteLocalName));
                List<BluetoothLEAdvertisementDataSection> smallName = new List<BluetoothLEAdvertisementDataSection>(args.Advertisement.GetSectionsByType(BluetoothLEAdvertisementDataTypes.ShortenedLocalName));
                if (complete.Count > 0) {
                    advertisedName = Encoding.UTF8.GetString(complete[0].Data.ToArray());
                } else if (smallName.Count > 0) {
                    advertisedName = Encoding.UTF8.GetString(smallName[0].Data.ToArray());
                }
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    clientDelegate.OnDeviceDiscovered((device.BluetoothAddress + "").ToUpper(), advertisedName, args.RawSignalStrengthInDBm);
                });
            }
        }
        private async void CharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args) {
            await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                clientDelegate.OnCharacteristicRead(sender.Uuid.ToString().ToUpper(), UNKNOWN_WRITING_DEV_ADDRESS, true, args.CharacteristicValue.ToArray());
            });
        }
#endregion
    }
}
