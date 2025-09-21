using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Devices.Radios;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Diagnostics;

/* READTHIS:
 * 
 * The Server on UWP is a little strange
 * When advertising there are 2 parameters: IsConnectable and IsDiscoverable
 * When both are true a UUID is advertised.
 * IsConnectable means the services can be accessed, but not found by not connected devices (started, but "not" advertising)
 * IsDiscoverable means the services can be found by a scan (Advertising)
 * The dummy service is used to control the server (StartServer and StopServer)
 * The other services have IsDiscoverable changed based on whether or not they should be advertising (StartAdvertising and StopAdvertising)
 * I have no idea if start/stop advertising actualy works.
 * 
 */

namespace QuickBLE {
    public class BLEServer {
        #region Variables and Properties

        private const string UNKNOWN_WRITING_DEV_ADDRESS = "unknown";

        // Platform Specific Objects
        private List<GattServiceProviderAdvertisingParameters> serviceParams = new List<GattServiceProviderAdvertisingParameters>();
        private List<GattServiceProvider> serviceObjects = new List<GattServiceProvider>();
        private List<BLECharacteristic<GattLocalCharacteristic>> characteristicObjects = new List<BLECharacteristic<GattLocalCharacteristic>>();
        private List<BLEDescriptor<GattLocalDescriptor>> descriptorObjects = new List<BLEDescriptor<GattLocalDescriptor>>();
        // UUIDs for Gatt Objects
        /// <summary>
        /// The services available on the server
        /// </summary>
        public List<string> Services { get; private set; } = new List<string>();
        /// <summary>
        /// The characteristics available on the erver
        /// </summary>
        public List<string> Characteristics { get; private set; } = new List<string>();
        /// <summary>
        /// The descriptors available on the server
        /// </summary>
        public List<string> Descriptors { get; private set; } = new List<string>();
        /// <summary>
        /// The services the server is advertising that it has (only these services can be scanned for pre-connect)
        /// </summary>
        public List<string> AdvertiseServices { get; private set; } = new List<string>();

        // Devices
        private List<BluetoothLEDevice> ConnectedDevices = new List<BluetoothLEDevice>();
        private List<ulong> connectedDeviceAddresses = new List<ulong>();

        // Platform Specific Options
        public bool ReadInternalWrites = false;
        public bool NotifyChangingDevice = false;
        // Yeah UWP!!!
        private bool advertiseDeviceName = true;
        public bool AdvertiseDeviceName {
            get {
                if (AdvertiseServices.Count == 0) {
                    return advertiseDeviceName;
                } else {
                    return true;
                }
            }
            set {
                if (AdvertiseServices.Count == 0) {
                    advertiseDeviceName = value;
                }
            }
        }

        // UWP
        BluetoothAdapter adapter;

        // Status
        /// <summary>
        /// Is the server running
        /// </summary>
        public bool IsRunning { get; private set; } = false;
        /// <summary>
        /// Is the server advertising
        /// </summary>
        public bool IsAdvertising { get; private set; } = false;

        // Enable BT Dialog Text
        public string REQUEST_BT_TITLE = "Bluetooth Required";
        public string REQUEST_BT_MESSAGE = "Please enable bluetooth in Settings";
        public string REQUEST_BT_CONFIRM = "Settings";
        public string REQUEST_BT_DENY = "Cancel";

        GattServiceProvider dummy;

        private CoreDispatcher mainThread = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;

        // The delegate
        private BLEDelegate serverDelegate;
        #endregion

        /// <summary>
        /// Create a new BLEServer for Bluetooth Gatt Peripheral role
        /// </summary>
        /// <param name="serverDelegate">The BLEDelegate for the server</param>
        public BLEServer(BLEDelegate serverDelegate) {
            this.serverDelegate = serverDelegate;
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
                                serverDelegate.OnBluetoothPowerChanged(state);
                            });
                        }
                        await Task.Delay(100);
                    }
                }
            });
        }

        #region Server Control
        /// <summary>
        /// Add a primary service to the server
        /// </summary>
        /// <param name="service">The UUID of the service</param>
        public async Task AddService(string service) {
            if (!Services.Contains(service.ToUpper())) {
                var res = await GattServiceProvider.CreateAsync(new Guid(service));
                if (res.Error == BluetoothError.Success) {
                    var s = res.ServiceProvider;
                    Services.Add(service.ToUpper());
                    serviceObjects.Add(s);
                    serviceParams.Add(new GattServiceProviderAdvertisingParameters());
                }
            }
        }
        /// <summary>
        /// ***DOES NOT WORK ON UWP***
        /// ***NOT IMPLEMENTED***
        /// Add an included (seconday) service to a service
        /// </summary>
        /// <param name="service">The UUID of the included service</param>
        /// <param name="parentService">The UUID of the parent (primary) service</param>
        /// <returns>Whether or not the service was successfully added</returns>
        public async Task<bool> AddIncludedService(string service, string parentService) {
            // I do not know how to do included services
            // I can find no info about them with UWP online so I assume MS API does not allow them
            // Will implement when/if I figure out how to
            throw new NotImplementedException("Included services have not been implemented for QuickBLE UWP.");
        }
        /// <summary>
        /// Add a characteristic to a service
        /// </summary>
        /// <param name="characteristic">The UUID of the characteristic</param>
        /// <param name="parentService">The UUID of the parent service</param>
        /// <param name="properties">The properties of the characteristic (CharProperties)</param>
        /// <param name="permissions">The permissions of the characteristic (CharPermissions)</param>
        /// <returns>Was te characteristic successfully added</returns>
        public async Task<bool> AddCharacteristic(string characteristic, string parentService, int properties = CharProperties.Read | CharProperties.Write | CharProperties.Notify, int permissions = CharPermissions.Read | CharPermissions.Write) {
            if (!Characteristics.Contains(characteristic.ToUpper()) && Services.Contains(parentService.ToUpper())) {
                var parent = GetService(new Guid(parentService));
                if (parent == null)
                    return false;
                var parameters = new GattLocalCharacteristicParameters();
                parameters.CharacteristicProperties = (GattCharacteristicProperties)properties;
                parameters.ReadProtectionLevel = GetReadPermissions(permissions);
                parameters.WriteProtectionLevel = GetWritePermissions(permissions);
                var res = await parent.Service.CreateCharacteristicAsync(new Guid(characteristic), parameters);
                if (res.Error == BluetoothError.Success) {
                    var c = new BLECharacteristic<GattLocalCharacteristic>(res.Characteristic, null);
                    c.Characteristic.ReadRequested += OnCharacteristicRead;
                    c.Characteristic.WriteRequested += OnCharacteristicWrite;
                    c.Characteristic.SubscribedClientsChanged += OnSubscribedClientsChanged;
                    Characteristics.Add(characteristic.ToUpper());
                    characteristicObjects.Add(c);
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Add a descriptor to a characteristic
        /// </summary>
        /// <param name="descriptor">The UUID of the descriptor</param>
        /// <param name="parentCharacteristic">The UUID of the parent characteristic</param>
        /// <param name="permissions">The permissions of the descriptor (DescPermissions)</param>
        /// <returns>Was the descriptor successfully added</returns>
        public async Task<bool> AddDescriptor(string descriptor, string parentCharacteristic, int permissions = DescPermissions.Read | DescPermissions.Write) {
            if (!Descriptors.Contains(descriptor.ToUpper()) && Characteristics.Contains(parentCharacteristic.ToUpper())) {
                var parent = GetCharacteristic(new Guid(parentCharacteristic));
                if (parent == null)
                    return false;
                var parameters = new GattLocalDescriptorParameters();
                parameters.ReadProtectionLevel = GetReadPermissions(permissions);
                parameters.WriteProtectionLevel = GetWritePermissions(permissions);
                var res = await parent.Characteristic.CreateDescriptorAsync(new Guid(descriptor), parameters);
                if (res.Error == BluetoothError.Success) {
                    var d = new BLEDescriptor<GattLocalDescriptor>(res.Descriptor, null);
                    d.Descriptor.ReadRequested += OnDescriptorRead;
                    d.Descriptor.WriteRequested += OnDescriptorWrite;
                    Descriptors.Add(descriptor.ToUpper());
                    descriptorObjects.Add(d);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Advertise a service's UUID in the advertisement packer
        /// </summary>
        /// <param name="service">The service to advertise</param>
        /// <param name="advertise">Whether or not to advertise the UUID</param>
        public void AdvertiseService(string service, bool advertise = true) {
            if (!AdvertiseServices.Contains(service.ToUpper()) && advertise) {
                AdvertiseServices.Add(service.ToUpper());
            }
            if (AdvertiseServices.Contains(service.ToUpper()) && !advertise) {
                AdvertiseServices.Remove(service.ToUpper());
            }
        }

        /// <summary>
        /// Remove all services, characteristics, and descriptors from the server
        /// </summary>
        public void ClearGatt() {
            StopServer();
            Services.Clear();
            Characteristics.Clear();
            Descriptors.Clear();
            serviceObjects.Clear();
            characteristicObjects.Clear();
            descriptorObjects.Clear();
            AdvertiseServices.Clear();
        }

        /* On UWP The server cannot be truly started and stopped
         * It can only advertise certain services.
         * As a result Start/Stop server just start/stop advertising */

        /// <summary>
        /// Check bluetooth low energy server compatibility for the device
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
                if (!adapter.IsLowEnergySupported) {
                    return BtError.NoBLE;
                }
                if (!adapter.IsPeripheralRoleSupported) {
                    return BtError.NoServer;
                }
            }
            if (r.State != RadioState.On) {
                return BtError.Disabled;
            }
            return BtError.None;
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
        /// After Bt is enabled the dummy service will not be created with RadioNotAvailable Error. This will retry for a time limit;
        /// </summary>
        private async Task<bool> CreateDummyService() {
            if (dummy != null)
                return true;
            int maxTries = 10;  // Tried enough. Probably will not work.
            int maxTime = 2000; // Timeout
            int start = DateTime.Now.Millisecond;
            int now = start;
            int i = 0;
            bool done = false;
            while (i < maxTries && (start + maxTime) > now) {
                GattServiceProviderResult result = await GattServiceProvider.CreateAsync(new Guid("00000000-0000-1000-8000-00805F9B34FB"));
                switch (result.Error) {
                    case BluetoothError.Success:
                        dummy = result.ServiceProvider;
                        done = true;
                        break;
                    case BluetoothError.RadioNotAvailable:
                        // This could be that the radio was just enabled. We will let it retry a few times.
                        break;
                    default:
                        done = true;
                        break;
                }
                i++;
                now = DateTime.Now.Millisecond;
                if (done)
                    break;
            }
            return dummy != null;
            return false;
        }

        /// <summary>
        /// Start and advertise the server
        /// </summary>
        /// <returns>An error code (BtError)</returns>
        public async Task<int> StartServer() {
            if (!IsRunning) {
                int error = await CheckBluetooth();
                if (error != BtError.None)
                    return error;
                // If no services are advertised and device name should be advertised something needs discoverable but not connectable
                // UWP: When discoverable and connectable: The UUID is advertised
                if (IsAdvertising) {
                    foreach (GattServiceProvider service in serviceObjects) {
                        try {
                            service.StopAdvertising();
                        } catch (Exception e) {
                            Debug.WriteLine(e.StackTrace);
                        }
                    }
                    if (dummy != null) {
                        try {
                            dummy.StopAdvertising();
                        } catch (Exception e) {
                            Debug.WriteLine(e.StackTrace);
                        }
                    }
                }
                if (dummy == null)
                    await CreateDummyService();
                if (dummy != null) {
                    try {
                        dummy.StartAdvertising(new GattServiceProviderAdvertisingParameters() {
                            IsConnectable = false,
                            IsDiscoverable = true
                        });
                    } catch (Exception e) {
                        Debug.WriteLine(e.StackTrace);
                    }
                }
                foreach (GattServiceProvider service in serviceObjects) {
                    GattServiceProviderAdvertisingParameters parameters = serviceParams[serviceObjects.IndexOf(service)];
                    parameters.IsConnectable = true;
                    parameters.IsDiscoverable = AdvertiseServices.Contains(service.Service.Uuid.ToString().ToUpper());
                    try {
                        service.StartAdvertising(parameters);
                    } catch (Exception e) {
                        Debug.WriteLine(e.StackTrace);
                    }
                }
                // On UWP the server is always running
                // Only discoverable & connectable services call AdvertisementStatusChanged
                IsRunning = true;
                IsAdvertising = true;
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    serverDelegate.OnAdvertise(AdvertiseError.None);
                });
                return BtError.None;
            } else {
                return BtError.AlreadyRunning;
            }
        }
        /// <summary>
        /// Stop the server and stop advertising.
        /// </summary>
        public void StopServer() {
            if (IsRunning) {
                foreach (GattServiceProvider service in serviceObjects) {
                    try {
                        service.StopAdvertising();
                    } catch (Exception e) { }
                }
                if (dummy != null) {
                    try {
                        dummy.StopAdvertising();
                    } catch (Exception e) { }
                }
                IsRunning = false;
                IsAdvertising = false;
                connectedDeviceAddresses.Clear();
                ConnectedDevices.Clear();
            }
        }
        /// <summary>
        /// Stop advertising the server
        /// </summary>
        public void StopAdvertising() {
            if (IsAdvertising) {
                // Stop the previous advertisement and advertise them connectable only. Not discoverable.
                foreach (GattServiceProvider service in serviceObjects) {
                    GattServiceProviderAdvertisingParameters parameters = serviceParams[serviceObjects.IndexOf(service)];
                    parameters.IsConnectable = true;
                    parameters.IsDiscoverable = false;
                }
                IsAdvertising = false;
            }
        }
        /// <summary>
        /// Advertise the server
        /// </summary>
        public void StartAdvertising() {
            if (!IsAdvertising) {
                // Stop previous types of advertiement and advertise as connectable and discoverable if needed
                foreach (GattServiceProvider service in serviceObjects) {
                    GattServiceProviderAdvertisingParameters parameters = serviceParams[serviceObjects.IndexOf(service)];
                    parameters.IsConnectable = true;
                    parameters.IsDiscoverable = AdvertiseServices.Contains(service.Service.Uuid.ToString().ToUpper());
                }
                IsAdvertising = true;
            }
        }

        /// <summary>
        /// Send a notification of a characteristic's value to a certain device
        /// </summary>
        /// <param name="characteristic">The characteristic to notify the value of</param>
        /// <param name="deviceAddress">The  of the device to send the notification to</param>
        public void NotifyDevice(string characteristic, string deviceAddress) {
            Task.Run(async () => {
            var c = GetCharacteristic(new Guid(characteristic));
            if (c == null)
                return;
            var selectedDev = ConnectedDevices.First(f => (f.BluetoothAddress + "").ToUpper().Equals(deviceAddress.ToUpper()));
                var writer = new DataWriter();
                writer.ByteOrder = ByteOrder.LittleEndian;
                writer.WriteBytes(c.DynamicValue);
                IBuffer val = writer.DetachBuffer();
                foreach (GattSubscribedClient dev in c.previousClients) {
                    if (dev.Session.DeviceId.Id.Equals(selectedDev.DeviceId)) {
                        await c.Characteristic.NotifyValueAsync(val, dev);
                    }
                }
            });
        }

        /// <summary>
        /// Notify device subscribed to a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to notify the value of</param>
        /// <param name="client">The device that just changed the characteristic</param>
        private void NotifyDevices(BLECharacteristic<GattLocalCharacteristic> characteristic, BluetoothLEDevice client) {
            Task.Run(async () => {
                var writer = new DataWriter();
                writer.ByteOrder = ByteOrder.LittleEndian;
                writer.WriteBytes(characteristic.DynamicValue);
                IBuffer val = writer.DetachBuffer();
                foreach(GattSubscribedClient dev in characteristic.previousClients) {
                    if (client == null || NotifyChangingDevice || !dev.Session.DeviceId.Id.Equals(client.DeviceId)) {
                        await characteristic.Characteristic.NotifyValueAsync(val, dev);
                    }
                }
            });
        }
        /// <summary>
        /// Get read GattProtectionLevel from CharPermissions/DescPermissions
        /// </summary>
        /// <param name="permissions">The permissions int (CharPermissions or DescPermissions)</param>
        /// <returns>The GattProtectionLevel</returns>
        private GattProtectionLevel GetReadPermissions(int permissions) {
            if ((permissions & CharPermissions.ReadEncrypted) == CharPermissions.ReadEncrypted) {
                return GattProtectionLevel.EncryptionRequired;
            }
            return GattProtectionLevel.Plain;
        }
        /// <summary>
        /// Get write GattProtectionLevel from CharPermissions/DescPermissions
        /// </summary>
        /// <param name="permissions">The permissions int (CharPermissions or DescPermissions)</param>
        /// <returns>The GattProtectionLevel</returns>
        private GattProtectionLevel GetWritePermissions(int permissions) {
            if ((permissions & CharPermissions.WriteEncrypted) == CharPermissions.WriteEncrypted) {
                return GattProtectionLevel.EncryptionRequired;
            }
            return GattProtectionLevel.Plain;
        }
        #endregion

        #region Characteristics and Descriptors
        /// <summary>
        /// Write a value to a characteristic
        /// </summary>
        /// <param name="characteristic">The characterisic to write</param>
        /// <param name="data">The value to write (as bytes)</param>
        /// <param name="notify">Whether ot not to notify subscribed devices</param>
        public async void WriteCharacteristic(string characteristic, byte[] data, bool notify) {
            var c = GetCharacteristic(new Guid(characteristic));
            if (c == null)
                return;
            c.DynamicValue = data;
            if (data != null && notify) {
                NotifyDevices(c, null);
            }
            await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                serverDelegate.OnCharacteristicWrite(characteristic.ToUpper(), true, data);
            });
            if (ReadInternalWrites) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    serverDelegate.OnCharacteristicRead(characteristic.ToUpper(), UNKNOWN_WRITING_DEV_ADDRESS, true, data);
                });
            }
        }
        /// <summary>
        /// Read a value from a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to read</param>
        public async void ReadCharacteristic(string characteristic) {
            var c = GetCharacteristic(new Guid(characteristic));
            if (c == null) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    serverDelegate.OnCharacteristicRead(characteristic.ToUpper(), UNKNOWN_WRITING_DEV_ADDRESS, false, null);
                });
                return;
            }
            await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                serverDelegate.OnCharacteristicRead(characteristic.ToUpper(), UNKNOWN_WRITING_DEV_ADDRESS, true, c.DynamicValue);
            });
        }
        /// <summary>
        /// Write a value to a descriptor
        /// </summary>
        /// <param name="descriptor">The descriptor to write</param>
        /// <param name="data">The value to write (as bytes)</param>
        public async void WriteDescriptor(string descriptor, byte[] data) {
            var d = GetDescriptor(new Guid(descriptor));
            if (d == null)
                return;
            d.DynamicValue = data;
            await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                serverDelegate.OnDescriptorWrite(descriptor.ToUpper(), true, data);
            });
            if (ReadInternalWrites) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    serverDelegate.OnDescriptorRead(descriptor.ToUpper(), UNKNOWN_WRITING_DEV_ADDRESS, true, data);
                });
            }
        }
        /// <summary>
        /// Read a value from a descriptor
        /// </summary>
        /// <param name="descriptor">The descriptor to read</param>
        public async void ReadDescriptor(string descriptor) {
            var d = GetDescriptor(new Guid(descriptor));
            if (d == null) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    serverDelegate.OnDescriptorRead(descriptor.ToUpper(), UNKNOWN_WRITING_DEV_ADDRESS, false, null);
                });
                return;
            }
            await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                serverDelegate.OnDescriptorRead(descriptor.ToUpper(), UNKNOWN_WRITING_DEV_ADDRESS, true, d.DynamicValue);
            });
        }

        /// <summary>
        /// Get a service from a UUID
        /// </summary>
        /// <param name="service">THe UUID of the service</param>
        /// <returns>The GattLocalService or null</returns>
        private GattServiceProvider GetService(Guid service) {
            return serviceObjects.FirstOrDefault(s => s.Service.Uuid.Equals(service));
        }
        /// <summary>
        /// Get a characteristic from a UUID
        /// </summary>
        /// <param name="characteristic">The UUID of the characteristic</param>
        /// <returns>The BLECharacteristic or null</returns>
        private BLECharacteristic<GattLocalCharacteristic> GetCharacteristic(Guid characteristic) {
            return characteristicObjects.FirstOrDefault(c => c.Characteristic.Uuid.Equals(characteristic));
        }
        /// <summary>
        /// Get a descriptor from a UUID
        /// </summary>
        /// <param name="descriptor">The UUID of the descriptor</param>
        /// <returns>The BLEDescriptor or null</returns>
        private BLEDescriptor<GattLocalDescriptor> GetDescriptor(Guid descriptor) {
            return descriptorObjects.FirstOrDefault(d => d.Descriptor.Uuid.Equals(descriptor));
        }

        /// <summary>
        /// Check if the server has a service
        /// </summary>
        /// <param name="service">The service to check for</param>
        /// <returns>Whether or not the server has the service</returns>
        public bool HasService(string service) {
            return Services.Contains(service.ToUpper());
        }
        /// <summary>
        /// Check if the server has a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to check for</param>
        /// <returns>Whether or not the server has the characteristic</returns>
        public bool HasCharacteristic(string characteristic) {
            return Characteristics.Contains(characteristic.ToUpper());
        }
        /// <summary>
        /// Check if the server has a descriptor
        /// </summary>
        /// <param name="descriptor">The descriptor to check for</param>
        /// <returns>Whether or not the server has the descriptor</returns>
        public bool HasDescriptor(string descriptor) {
            return Descriptors.Contains(descriptor.ToUpper());
        }
        #endregion

        // Device management for connect/disconnect
        private async void AddDevice(BluetoothDeviceId deviceId) {
            AddDevice(await BluetoothLEDevice.FromIdAsync(deviceId.Id));
        }
        private async void AddDevice(BluetoothLEDevice device) {
            if (!connectedDeviceAddresses.Contains(device.BluetoothAddress)) {
                device.ConnectionStatusChanged += ConnectionStatusChanged;
                ConnectedDevices.Add(device);
                connectedDeviceAddresses.Add(device.BluetoothAddress);
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    serverDelegate.OnDeviceConnected((device.BluetoothAddress + "").ToUpper(), device.Name);
                });
                // Allow more than 1 client to connect
                for (int i = 0; i < serviceObjects.Count; i++) {
                    serviceParams[i].IsConnectable = true;
                    serviceParams[i].IsConnectable = AdvertiseServices.Contains(Services[i].ToUpper());
                }
            }
        }
        private async void RemoveDevice(BluetoothDeviceId deviceId) {
            RemoveDevice(await BluetoothLEDevice.FromIdAsync(deviceId.Id));
        }
        private async void RemoveDevice(BluetoothLEDevice device) {
            if (connectedDeviceAddresses.Contains(device.BluetoothAddress)) {
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    serverDelegate.OnDeviceDisconnected((device.BluetoothAddress + "").ToUpper(), device.Name);
                });
                device.ConnectionStatusChanged -= ConnectionStatusChanged;
                int i = connectedDeviceAddresses.IndexOf(device.BluetoothAddress);
                if (i != -1) {
                    connectedDeviceAddresses.RemoveAt(i);
                    ConnectedDevices.RemoveAt(i);
                }
            }
        }

        #region Event Handlers
        // Device reconnect or disconnect
        private void ConnectionStatusChanged(BluetoothLEDevice sender, object args) {
            switch (sender.ConnectionStatus) {
                case BluetoothConnectionStatus.Connected:
                    AddDevice(sender.BluetoothDeviceId);
                    break;
                case BluetoothConnectionStatus.Disconnected:
                    RemoveDevice(sender);
                    break;
            }
        }
        // Characteristic Read Request
        private async void OnCharacteristicRead(GattLocalCharacteristic sender, GattReadRequestedEventArgs args) {
            using (args.GetDeferral()) {
                AddDevice(args.Session.DeviceId);
                var c = GetCharacteristic(sender.Uuid);
                if (c == null) {
                    return;
                }
                var writer = new DataWriter();
                writer.WriteBytes(c.DynamicValue);
                var request = await args.GetRequestAsync();
                if (request == null) {
                    return;
                }
                request.RespondWithValue(writer.DetachBuffer());
            }
        }
        // Characteristic Write Request
        private async void OnCharacteristicWrite(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args) {
            using (args.GetDeferral()) {
                var device = await BluetoothLEDevice.FromIdAsync(args.Session.DeviceId.Id);
                AddDevice(device);
                var c = GetCharacteristic(sender.Uuid);
                if (c == null) {
                    return;
                }
                var request = await args.GetRequestAsync();
                if (request == null) {
                    return;
                }
                var reader = DataReader.FromBuffer(request.Value);
                c.DynamicValue = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(c.DynamicValue);
                if (request.Option == GattWriteOption.WriteWithResponse) {
                    request.Respond();
                }
                NotifyDevices(c, await BluetoothLEDevice.FromIdAsync(args.Session.DeviceId.Id));
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    serverDelegate.OnCharacteristicRead(c.Characteristic.Uuid.ToString().ToUpper(), (device.BluetoothAddress + "").ToUpper(), true, c.DynamicValue);
                });
            }
        }
        // Subscribe or Unsubscribe
        private void OnSubscribedClientsChanged(GattLocalCharacteristic sender, object args) {
            var c = GetCharacteristic(sender.Uuid);
            if (c == null)
                return;
            //List<GattSubscribedClient> removed = c.previousClients.Except(c.Characteristic.SubscribedClients).ToList();
            List<GattSubscribedClient> added = c.Characteristic.SubscribedClients.Except(c.previousClients).ToList();
            /*removed.ForEach(client => {
                RemoveDevice(client.Session.DeviceId);
            });*/
            added.ForEach(client => {
                AddDevice(client.Session.DeviceId);
            });
            c.previousClients = c.Characteristic.SubscribedClients;
        }
        // Descriptor Read Request
        private async void OnDescriptorRead(GattLocalDescriptor sender, GattReadRequestedEventArgs args) {
            using (args.GetDeferral()) {
                AddDevice(args.Session.DeviceId);
                var d = GetDescriptor(sender.Uuid);
                if (d == null) {
                    return;
                }
                var writer = new DataWriter();
                writer.WriteBytes(d.DynamicValue);
                var request = await args.GetRequestAsync();
                if (request == null) {
                    return;
                }
                request.RespondWithValue(writer.DetachBuffer());
            }
        }
        // Descriptor Write Request
        private async void OnDescriptorWrite(GattLocalDescriptor sender, GattWriteRequestedEventArgs args) {
            using (args.GetDeferral()) {
                var device = await BluetoothLEDevice.FromIdAsync(args.Session.DeviceId.Id);
                AddDevice(device);
                var d = GetDescriptor(sender.Uuid);
                if (d == null) {
                    return;
                }
                var request = await args.GetRequestAsync();
                if (request == null) {
                    return;
                }
                var reader = DataReader.FromBuffer(request.Value);
                d.DynamicValue = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(d.DynamicValue);
                if (request.Option == GattWriteOption.WriteWithResponse) {
                    request.Respond();
                }
                await mainThread.RunAsync(CoreDispatcherPriority.Normal, () => {
                    serverDelegate.OnDescriptorRead(d.Descriptor.Uuid.ToString().ToUpper(), (device.BluetoothAddress + "").ToUpper(), true, d.DynamicValue);
                });
            }
        }
        #endregion

    }
}
