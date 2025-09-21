using AOT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace QuickBLE.Unity.Apple {
#if UNITY_IOS || UNITY_STANDALONE_OSX
    class AppleValues {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void AdvertiseCallback(IntPtr src, int error);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void BtPowerChangeCallback(IntPtr src, bool enabled);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate void CharReadCallback(IntPtr src, string characteristic, string writingDeviceAddress, bool success, char* value, int len);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate void CharWriteCallback(IntPtr src, string characteristic, bool success, char* value, int len);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void ConnectToDeviceCallback(IntPtr src, string address, string name, bool success);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate void DescReadCallback(IntPtr src, string descriptor, string writingDeviceAddress, bool success, char* value, int len);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate void DescWriteCallback(IntPtr src, string descriptor, bool success, char* value, int len);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void DeviceConnectCallback(IntPtr src, string address, string name);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void DisconnectFromDeviceCallback(IntPtr src, string address, string name);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void DeviceDiscoveredCallback(IntPtr src, string address, string name, int rssi);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void DeviceDisconnectCallback(IntPtr src, string address, string name);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void BluetoothRequestCallback(IntPtr src, bool choseToEnable);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void ServiceDiscoveredCallback(IntPtr src);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void SentNotificationCallback(IntPtr src, string characteristic, bool success);

        public static string UnknownDevice = "Unknown";
    }
    class AppleHelper {
        /// <summary>
        /// Convert an ObjC int to a BtError
        /// </summary>
        /// <param name="error">The int from ObjC</param>
        /// <returns>A BtError</returns>
        public static BtError ToBtError(int error) {
            switch (error) {
                case 2:
                    return BtError.NoBluetooth;
                case 3:
                    return BtError.NoServer;
                case 4:
                    return BtError.Disabled;
                case 5:
                    return BtError.None;
                case 6:
                    return BtError.AlreadyRunning;
                default:
                    return BtError.Unknown;
            }
        }
        /// <summary>
        /// Convert a BtError to an ObjC int
        /// </summary>
        /// <param name="error">The BtError</param>
        /// <returns>An ObjC int</returns>
        public static int FromBtError(BtError error) {
            switch (error) {
                case BtError.None:
                    return 5;
                case BtError.NoBluetooth:
                    return 2;
                case BtError.Disabled:
                    return 4;
                case BtError.NoServer:
                    return 3;
                case BtError.AlreadyRunning:
                    return 6;
                default:
                    return 0;
            }
        }
        /// <summary>
        /// Convert an ObjC int to an AdvertiseError
        /// </summary>
        /// <param name="error">The ObjC int</param>
        /// <returns>An AdvertiseError</returns>
        public static AdvertiseError ToAdvertiseError(int error) {
            switch (error) {
                case 0:
                    return AdvertiseError.None;
                case 4:
                    return AdvertiseError.DataTooLarge;
                case 11:
                    return AdvertiseError.TooManyAdvertisers;
                case 9:
                    return AdvertiseError.AlreadyStarted;
                default:
                    return AdvertiseError.Unknown;
            }
        }
        /// <summary>
        /// Convert an AdvertiseError to an ObjC int
        /// </summary>
        /// <param name="error">The AdvertiseError</param>
        /// <returns>An ObjC int</returns>
        public static int FromAdvertiseError(AdvertiseError error) {
            switch (error) {
                case AdvertiseError.None:
                    return 0;
                case AdvertiseError.DataTooLarge:
                    return 4;
                case AdvertiseError.TooManyAdvertisers:
                    return 11;
                case AdvertiseError.AlreadyStarted:
                    return 9;
                default:
                    return -1;
            }
        }
        /// <summary>
        /// Convert an ObjC int to CharPermissions
        /// </summary>
        /// <param name="permissions">The ObjC int</param>
        /// <returns>CharPermissions</returns>
        public static CharPermissions ToCharPermissions(int permissions) {
            List<CharPermissions> perms = new List<CharPermissions>();
            if ((permissions & 1) == 1) {
                perms.Add(CharPermissions.Read);
            }
            if ((permissions & 2) == 2) {
                perms.Add(CharPermissions.ReadEncrypted);
            }
            if ((permissions & 4) == 4) {
                perms.Add(CharPermissions.Write);
            }
            if ((permissions & 8) == 8) {
                perms.Add(CharPermissions.WriteEncrypted);
            }
            if (perms.Count == 0) {
                throw new Exception("The int is invalid!!!");
            } else if (perms.Count == 1) {
                return perms[0];
            } else {
                CharPermissions p = perms[0];
                perms.RemoveAt(0);
                foreach (CharPermissions perm in perms) {
                    p = p | perm;
                }
                return p;
            }
        }
        /// <summary>
        /// Convert CharPermissions to an ObjC int
        /// </summary>
        /// <param name="permissions">The CharPermissions</param>
        /// <returns>An ObjC int</returns>
        public static int FromCharPermissions(CharPermissions permissions) {
            int perms = 0;
            if ((permissions & CharPermissions.Read) == CharPermissions.Read) {
                perms = perms | 1;
            }
            if ((permissions & CharPermissions.Write) == CharPermissions.Write) {
                perms = perms | 4;
            }
            if ((permissions & CharPermissions.ReadEncrypted) == CharPermissions.ReadEncrypted) {
                perms = perms | 2;
            }
            if ((permissions & CharPermissions.WriteEncrypted) == CharPermissions.WriteEncrypted) {
                perms = perms | 8;
            }
            return perms;
        }
        /// <summary>
        /// Convert an ObjC int to CharProperties
        /// </summary>
        /// <param name="properties">The ObjC int</param>
        /// <returns>The CharProperties</returns>
        public static CharProperties ToCharProperties(int properties) {
            List<CharProperties> props = new List<CharProperties>();
            if ((properties & 1) == 1) {
                props.Add(CharProperties.Broadcast);
            }
            if ((properties & 2) == 2) {
                props.Add(CharProperties.Read);
            }
            if ((properties & 4) == 4) {
                props.Add(CharProperties.WriteNoResponse);
            }
            if ((properties & 8) == 8) {
                props.Add(CharProperties.Write);
            }
            if ((properties & 16) == 16) {
                props.Add(CharProperties.Notify);
            }
            if ((properties & 32) == 32) {
                props.Add(CharProperties.Indicate);
            }
            if ((properties & 64) == 64) {
                props.Add(CharProperties.SignedWrite);
            }
            if ((properties & 128) == 128) {
                props.Add(CharProperties.Extended);
            }

            if (props.Count == 0) {
                throw new Exception("The int is invalid!!!");
            } else if (props.Count == 1) {
                return props[0];
            } else {
                CharProperties p = props[0];
                props.RemoveAt(0);
                foreach (CharProperties prop in props) {
                    p = p | prop;
                }
                return p;
            }
        }
        /// <summary>
        /// Convert CharProperties to an ObjC int
        /// </summary>
        /// <param name="properties">The CharProperties</param>
        /// <returns>An ObjC int</returns>
        public static int FromCharProperties(CharProperties properties) {
            int props = 0;
            if ((properties & CharProperties.Broadcast) == CharProperties.Broadcast) {
                props = props | 1;
            }
            if ((properties & CharProperties.Read) == CharProperties.Read) {
                props = props | 2;
            }
            if ((properties & CharProperties.WriteNoResponse) == CharProperties.WriteNoResponse) {
                props = props | 4;
            }
            if ((properties & CharProperties.Write) == CharProperties.Write) {
                props = props | 8;
            }
            if ((properties & CharProperties.Notify) == CharProperties.Notify) {
                props = props | 16;
            }
            if ((properties & CharProperties.Indicate) == CharProperties.Indicate) {
                props = props | 32;
            }
            if ((properties & CharProperties.SignedWrite) == CharProperties.SignedWrite) {
                props = props | 64;
            }
            if ((properties & CharProperties.Extended) == CharProperties.Extended) {
                props = props | 128;
            }
            return props;
        }
        /// <summary>
		/// Converts a c style string array (char**) to a c# string[]
        /// </summary>
        /// <param name="array">The c style array</param>
		/// <param name="len">The number of c-strings (char*) in the array</param>
		/// <returns>The c# string[]</returns>
		public unsafe static string[] ToStringArray(char** array, int len) {
            string[] output = new string[len];
            for (int i = 0; i < len; i++) {
                IntPtr p = new IntPtr(array[i]);
                output[i] = Marshal.PtrToStringAnsi(p);
            }
            return output;
        }
        public unsafe static byte[] ToByteArray(char* array, int len) {
            byte[] output = new byte[len];
            Marshal.Copy(new IntPtr(array), output, 0, len);
            return output;
        }
    }
    class AppleDelegate {
        // Callbacks must be static for iOS b/c it is compiled AOT
        // Hold a list of objects and delegates and respond to the correct delegate based on the object
        private static List<IntPtr> Objects = new List<IntPtr>();
        private static List<BLEDelegate> UnityDelegates = new List<BLEDelegate>();
        public static void AddDelegate(IntPtr obj, BLEDelegate unityDelegate) {
            Objects.Add(obj);
            UnityDelegates.Add(unityDelegate);
        }
        // Server Specific
        [MonoPInvokeCallback(typeof(AppleValues.AdvertiseCallback))]
        public static void advertiseCallback(IntPtr obj, int error) {
            MainThread.Instance().Run(() => {
                UnityDelegates[Objects.IndexOf(obj)].OnAdvertise(AppleHelper.ToAdvertiseError(error));
            });
        }
        [MonoPInvokeCallback(typeof(AppleValues.DeviceConnectCallback))]
        public static void deviceConnectedCallback(IntPtr obj, string address, string name) {
            MainThread.Instance().Run(() => {
                if (name == null)
                    name = AppleValues.UnknownDevice;
                UnityDelegates[Objects.IndexOf(obj)].OnDeviceConnected(address, name.ToString());
            });
        }
        [MonoPInvokeCallback(typeof(AppleValues.DeviceDisconnectCallback))]
        public static void deviceDisconnectedCallback(IntPtr obj, string address, string name) {
            MainThread.Instance().Run(() => {
                if (name == null)
                    name = AppleValues.UnknownDevice;
                UnityDelegates[Objects.IndexOf(obj)].OnDeviceDisconnected(address, name.ToString());
            });
        }
        [MonoPInvokeCallback(typeof(AppleValues.DeviceDiscoveredCallback))]
        public static void deviceDiscoveredCallback(IntPtr obj, string address, string name, int rssi) {
            MainThread.Instance().Run(() => {
                if (name == null)
                    name = AppleValues.UnknownDevice;
                UnityDelegates[Objects.IndexOf(obj)].OnDeviceDiscovered(address, name.ToString(), rssi);
            });
        }
        [MonoPInvokeCallback(typeof(AppleValues.SentNotificationCallback))]
        public static void sentNotificationCallback(IntPtr obj, string characteristic, bool success){
            MainThread.Instance().Run(() => {
                UnityDelegates[Objects.IndexOf(obj)].OnNotificationSent(characteristic, success);
            });
        }
        // Client Specific
        [MonoPInvokeCallback(typeof(AppleValues.ConnectToDeviceCallback))]
        public static void connectToDeviceCallback(IntPtr obj, string address, string name, bool success) {
            MainThread.Instance().Run(() => {
                if (name == null)
                    name = AppleValues.UnknownDevice;
                UnityDelegates[Objects.IndexOf(obj)].OnConnectToDevice(address, name.ToString(), success);
            });
        }
        [MonoPInvokeCallback(typeof(AppleValues.DisconnectFromDeviceCallback))]
        public static void disconnectFromDeviceCallback(IntPtr obj, string address, string name) {
            MainThread.Instance().Run(() => {
                if (name == null)
                    name = AppleValues.UnknownDevice;
                UnityDelegates[Objects.IndexOf(obj)].OnDisconnectFromDevice(address, name.ToString());
            });
        }
        [MonoPInvokeCallback(typeof(AppleValues.ServiceDiscoveredCallback))]
        public static void servicesDiscoveredCallback(IntPtr obj) {
            MainThread.Instance().Run(() => {
                UnityDelegates[Objects.IndexOf(obj)].OnServicesDiscovered();
            });
        }
        // Shared
        [MonoPInvokeCallback(typeof(AppleValues.CharReadCallback))]
        public unsafe static void characteristicReadCallback(IntPtr obj, string characteristic, string writingDeviceAddress, bool success, char* value, int len) {
            MainThread.Instance().Run(() => {
                UnityDelegates[Objects.IndexOf(obj)].OnCharacteristicRead(characteristic, writingDeviceAddress, success, AppleHelper.ToByteArray(value, len));
            });
        }
        [MonoPInvokeCallback(typeof(AppleValues.CharWriteCallback))]
        public unsafe static void characteristicWriteCallback(IntPtr obj, string characteristic, bool success, char* value, int len) {
            MainThread.Instance().Run(() => {
                UnityDelegates[Objects.IndexOf(obj)].OnCharacteristicWrite(characteristic, success, AppleHelper.ToByteArray(value, len));
            });
        }
        [MonoPInvokeCallback(typeof(AppleValues.DescReadCallback))]
        public unsafe static void descriptorReadCallback(IntPtr obj, string descriptor, string writingDeviceAddress, bool success, char* value, int len) {
            MainThread.Instance().Run(() => {
                UnityDelegates[Objects.IndexOf(obj)].OnDescriptorRead(descriptor, writingDeviceAddress, success, AppleHelper.ToByteArray(value, len));
            });
        }
        [MonoPInvokeCallback(typeof(AppleValues.DescWriteCallback))]
        public unsafe static void descriptorWriteCallback(IntPtr obj, string descriptor, bool success, char* value, int len) {
            MainThread.Instance().Run(() => {
                UnityDelegates[Objects.IndexOf(obj)].OnDescriptorWrite(descriptor, success, AppleHelper.ToByteArray(value, len));
            });
        }
        [MonoPInvokeCallback(typeof(AppleValues.BtPowerChangeCallback))]
        public static void bluetoothPowerChangeCallback(IntPtr obj, bool enabled) {
            MainThread.Instance().Run(() => {
                UnityDelegates[Objects.IndexOf(obj)].OnBluetoothPowerChanged(enabled);
            });
        }
        [MonoPInvokeCallback(typeof(AppleValues.BluetoothRequestCallback))]
        public static void bluetoothRequestCallback(IntPtr obj, bool choseToEnable){
            MainThread.Instance().Run(() => {
                UnityDelegates[Objects.IndexOf(obj)].OnBluetoothRequestResult(choseToEnable);
            });
        }
    }
    class AppleServer : IBLEServer {
        #region Native
        IntPtr NativeServer;
        // Native Functions
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern IntPtr Server_Init([MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.AdvertiseCallback advc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.BtPowerChangeCallback btpc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.CharReadCallback crc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.CharWriteCallback cwc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.ConnectToDeviceCallback ctdc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.DescReadCallback drc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.DescWriteCallback dwc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.DeviceConnectCallback dcc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.DisconnectFromDeviceCallback disconfromc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.DeviceDiscoveredCallback discovc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.DeviceDisconnectCallback disconnc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.ServiceDiscoveredCallback sdc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.BluetoothRequestCallback rbc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.SentNotificationCallback snc);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern bool Server_IsRunning(IntPtr server);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern bool Server_IsAdvertising(IntPtr server);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Server_NotifyDevice(IntPtr server, string characteristic, string deviceAddress);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private unsafe static extern int Server_GetServices(IntPtr server, char*** rtn);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private unsafe static extern int Server_GetCharacteristics(IntPtr server, char*** rtn);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private unsafe static extern int Server_GetDescriptors(IntPtr server, char*** rtn);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private unsafe static extern int Server_GetAdvertiseServices(IntPtr server, char*** rtn);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Server_AddService(IntPtr server, string service);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern bool Server_AddIncludedService(IntPtr server, string service, string parentService);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern bool Server_AddCharacteristic(IntPtr server, string characteristic, string parentService, int properties, int permissions);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern bool Server_AddDescriptor(IntPtr server, string descriptor, string parentCharacteristic);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Server_AdvertiseService(IntPtr server, string service, bool advertise);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Server_ClearGatt(IntPtr server);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern int Server_CheckBluetooth(IntPtr server);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Server_RequestEnableBt(IntPtr server);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern int Server_StartServer(IntPtr server);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Server_StopServer(IntPtr server);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Server_StartAdvertising(IntPtr server);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Server_StopAdvertising(IntPtr server);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Server_WriteCharacteristic(IntPtr server, string characteristic, byte[] data, int len, bool notify);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Server_ReadCharacteristic(IntPtr server, string characteristic);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Server_WriteDescriptor(IntPtr server, string descriptor, byte[] data, int len);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Server_ReadDescriptor(IntPtr server, string desriptor);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern bool Server_HasService(IntPtr server, string service);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern bool Server_HasCharacteristic(IntPtr server, string characteristic);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern bool Server_HasDescriptor(IntPtr server, string descriptor);
        #endregion

        #region Properties
        private unsafe List<string> GetServices() {
            char** array;
            int len = Server_GetServices(NativeServer, &array);
            return new List<string>(AppleHelper.ToStringArray(array, len));
        }
        private unsafe List<string> GetCharacteristics() {
            char** array;
            int len = Server_GetCharacteristics(NativeServer, &array);
            return new List<string>(AppleHelper.ToStringArray(array, len));
        }
        private unsafe List<string> GetDescriptors() {
            char** array;
            int len = Server_GetDescriptors(NativeServer, &array);
            return new List<string>(AppleHelper.ToStringArray(array, len));
        }
        private unsafe List<string> GetAdvertiseServices() {
            char** array;
            int len = Server_GetAdvertiseServices(NativeServer, &array);
            return new List<string>(AppleHelper.ToStringArray(array, len));
        }

        public List<string> Services {
            get {
                return GetServices();
            }
        }
        public List<string> Characteristics {
            get {
                return GetCharacteristics();
            }
        }
        public List<string> Descriptors {
            get {
                return GetDescriptors();
            }
        }
        public List<string> AdvertiseServices {
            get {
                return GetAdvertiseServices();
            }
        }
        public bool IsRunning {
            get {
                return Server_IsRunning(NativeServer);
            }
        }
        public bool IsAdvertising {
            get {
                return Server_IsAdvertising(NativeServer);
            }
        }
        #endregion

        public unsafe AppleServer(BLEDelegate unityDelegate) {
            NativeServer = Server_Init(AppleDelegate.advertiseCallback, AppleDelegate.bluetoothPowerChangeCallback, AppleDelegate.characteristicReadCallback, AppleDelegate.characteristicWriteCallback, AppleDelegate.connectToDeviceCallback, AppleDelegate.descriptorReadCallback, AppleDelegate.descriptorWriteCallback, AppleDelegate.deviceConnectedCallback, AppleDelegate.disconnectFromDeviceCallback, AppleDelegate.deviceDiscoveredCallback, AppleDelegate.deviceDisconnectedCallback, AppleDelegate.servicesDiscoveredCallback, AppleDelegate.bluetoothRequestCallback, AppleDelegate.sentNotificationCallback);
            AppleDelegate.AddDelegate(NativeServer, unityDelegate);
        }

        #region Server Control
        public void AddService(string service) {
            Server_AddService(NativeServer, service);
        }
        public bool AddIncludedService(string service, string parentService) {
            return Server_AddIncludedService(NativeServer, service, parentService);
        }
        public bool AddCharacteristic(string characteristic, string parentService, CharProperties properties = CharProperties.Read | CharProperties.Write | CharProperties.Notify, CharPermissions permissions = CharPermissions.Read | CharPermissions.Write) {
            return Server_AddCharacteristic(NativeServer, characteristic, parentService, AppleHelper.FromCharProperties(properties), AppleHelper.FromCharPermissions(permissions));
        }
        public bool AddDescriptor(string descriptor, string parentCharacteristic) {
            return Server_AddDescriptor(NativeServer, descriptor, parentCharacteristic);
        }
        public void AdvertiseService(string service, bool advertise = true) {
            Server_AdvertiseService(NativeServer, service, advertise);
        }
        public void ClearGatt() {
            Server_ClearGatt(NativeServer);
        }
        public BtError CheckBluetooth() {
            return AppleHelper.ToBtError(Server_CheckBluetooth(NativeServer));
        }
        public void RequestEnableBt() {
            Server_RequestEnableBt(NativeServer);
        }
        public BtError StartServer() {
            return AppleHelper.ToBtError(Server_StartServer(NativeServer));
        }
        public void StopServer() {
            Server_StopServer(NativeServer);
        }
        public void StartAdvertising() {
            Server_StartAdvertising(NativeServer);
        }
        public void StopAdvertising() {
            Server_StopAdvertising(NativeServer);
        }
        public void NotifyDevice(string characteristic, string deviceAddress) {
            Server_NotifyDevice(NativeServer, characteristic, deviceAddress);
        }
        #endregion

        #region Characteristics and Descriptors
        public void WriteCharacteristic(string characteristic, byte[] data, bool notify) {
            Server_WriteCharacteristic(NativeServer, characteristic, data, data.Length, notify);
        }
        public void ReadCharacteristic(string characteristic) {
            Server_ReadCharacteristic(NativeServer, characteristic);
        }
        public void WriteDescriptor(string descriptor, byte[] data) {
            Server_WriteDescriptor(NativeServer, descriptor, data, data.Length);
        }
        public void ReadDescriptor(string descriptor) {
            Server_ReadDescriptor(NativeServer, descriptor);
        }
        public bool HasService(string service) {
            return Server_HasService(NativeServer, service);
        }
        public bool HasCharacteristic(string characteristic) {
            return Server_HasCharacteristic(NativeServer, characteristic);
        }
        public bool HasDescriptor(string descriptor) {
            return Server_HasDescriptor(NativeServer, descriptor);
        }
        #endregion

    }
    class AppleClient : IBLEClient {
        #region Native
        IntPtr NativeClient;
        // Native Functions
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern IntPtr Client_Init([MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.AdvertiseCallback advc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.BtPowerChangeCallback btpc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.CharReadCallback crc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.CharWriteCallback cwc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.ConnectToDeviceCallback ctdc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.DescReadCallback drc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.DescWriteCallback dwc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.DeviceConnectCallback dcc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.DisconnectFromDeviceCallback disconfromc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.DeviceDiscoveredCallback discovc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.DeviceDisconnectCallback disconnc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.ServiceDiscoveredCallback sdc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.BluetoothRequestCallback rbc, [MarshalAs(UnmanagedType.FunctionPtr)] AppleValues.SentNotificationCallback scb);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern bool Client_IsScanning(IntPtr client);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern bool Client_IsConnected(IntPtr client);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private unsafe static extern int Client_GetServices(IntPtr client, char*** rtn);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private unsafe static extern int Client_GetCharacteristics(IntPtr client, char*** rtn);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private unsafe static extern int Client_GetDescriptors(IntPtr client, char*** rtn);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private unsafe static extern int Client_GetScanServices(IntPtr client, char*** rtn);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Client_ScanForService(IntPtr client, string service, bool scanFor);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern int Client_CheckBluetooth(IntPtr client);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Client_RequestEnableBt(IntPtr client);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern int Client_ScanForDevices(IntPtr client);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Client_StopScanning(IntPtr client);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Client_ConnectToDevice(IntPtr client, string deviceAddress);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Client_Disconnect(IntPtr client);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Client_SubscribeToCharacteristic(IntPtr client, string characteristic, bool subscribe);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Client_WriteCharacteristic(IntPtr client, string characteristic, byte[] data, int len);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Client_ReadCharacteristic(IntPtr client, string characteristic);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Client_WriteDescriptor(IntPtr client, string descriptor, byte[] data, int len);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern void Client_ReadDescriptor(IntPtr client, string desriptor);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern bool Client_HasService(IntPtr client, string service);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern bool Client_HasCharacteristic(IntPtr client, string characteristic);
#if UNITY_IOS
        [DllImport("__Internal")]
#endif
#if UNITY_STANDALONE_OSX
        [DllImport("QuickBLE")]
#endif
        private static extern bool Client_HasDescriptor(IntPtr client, string descriptor);
        #endregion

        #region Properties
        private unsafe List<string> GetServices() {
            char** array;
            int len = Client_GetServices(NativeClient, &array);
            return new List<string>(AppleHelper.ToStringArray(array, len));
        }
        private unsafe List<string> GetCharacteristics() {
            char** array;
            int len = Client_GetCharacteristics(NativeClient, &array);
            return new List<string>(AppleHelper.ToStringArray(array, len));
        }
        private unsafe List<string> GetDescriptors() {
            char** array;
            int len = Client_GetDescriptors(NativeClient, &array);
            return new List<string>(AppleHelper.ToStringArray(array, len));
        }
        private unsafe List<string> GetScanServices() {
            char** array;
            int len = Client_GetScanServices(NativeClient, &array);
            return new List<string>(AppleHelper.ToStringArray(array, len));
        }

        public List<string> Services {
            get {
                return GetServices();
            }
        }
        public List<string> Characteristics {
            get {
                return GetCharacteristics();
            }
        }
        public List<string> Descriptors {
            get {
                return GetDescriptors();
            }
        }
        public List<string> ScanServices {
            get {
                return GetScanServices();
            }
        }
        public bool IsScanning {
            get {
                return Client_IsScanning(NativeClient);
            }
        }
        public bool IsConnected {
            get {
                return Client_IsConnected(NativeClient);
            }
        }
        #endregion

        public unsafe AppleClient(BLEDelegate unityDelegate) {
            NativeClient = Client_Init(AppleDelegate.advertiseCallback, AppleDelegate.bluetoothPowerChangeCallback, AppleDelegate.characteristicReadCallback, AppleDelegate.characteristicWriteCallback, AppleDelegate.connectToDeviceCallback, AppleDelegate.descriptorReadCallback, AppleDelegate.descriptorWriteCallback, AppleDelegate.deviceConnectedCallback, AppleDelegate.disconnectFromDeviceCallback, AppleDelegate.deviceDiscoveredCallback, AppleDelegate.deviceDisconnectedCallback, AppleDelegate.servicesDiscoveredCallback, AppleDelegate.bluetoothRequestCallback, AppleDelegate.sentNotificationCallback);
            AppleDelegate.AddDelegate(NativeClient, unityDelegate);
        }

        #region Server Control
        public void ScanForService(string service, bool scanFor = true) {
            Client_ScanForService(NativeClient, service, scanFor);
        }
        public BtError CheckBluetooth() {
            return AppleHelper.ToBtError(Client_CheckBluetooth(NativeClient));
        }
        public void RequestEnableBt() {
            Client_RequestEnableBt(NativeClient);
        }
        public BtError ScanForDevices() {
            return AppleHelper.ToBtError(Client_ScanForDevices(NativeClient));
        }
        public void StopScanning() {
            Client_StopScanning(NativeClient);
        }
        public void ConnectToDevice(string deviceAddress) {
            Client_ConnectToDevice(NativeClient, deviceAddress);
        }
        public void Disconnect() {
            Client_Disconnect(NativeClient);
        }
        public void SubscribeToCharacteristic(string characteristic, bool subscribe) {
            Client_SubscribeToCharacteristic(NativeClient, characteristic, subscribe);
        }
        #endregion

        #region Characteristics and Descriptors
        public void WriteCharacteristic(string characteristic, byte[] data) {
            Client_WriteCharacteristic(NativeClient, characteristic, data, data.Length);
        }
        public void ReadCharacteristic(string characteristic) {
            Client_ReadCharacteristic(NativeClient, characteristic);
        }
        public void WriteDescriptor(string descriptor, byte[] data) {
            Client_WriteDescriptor(NativeClient, descriptor, data, data.Length);
        }
        public void ReadDescriptor(string descriptor) {
            Client_ReadDescriptor(NativeClient, descriptor);
        }
        public bool HasService(string service) {
            return Client_HasService(NativeClient, service);
        }
        public bool HasCharacteristic(string characteristic) {
            return Client_HasCharacteristic(NativeClient, characteristic);
        }
        public bool HasDescriptor(string descriptor) {
            return Client_HasDescriptor(NativeClient, descriptor);
        }
        #endregion

    }
#endif
}