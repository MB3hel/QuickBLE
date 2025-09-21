using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuickBLE.Unity.Android {
#if UNITY_ANDROID
    class AndroidValues {
        public static string Package = "com.mb3hel.quickble.";
        public static int SDKLevel {
            get {
                using (var buildVersion = new AndroidJavaClass("android.os.Build$VERSION")) {
                    return buildVersion.GetStatic<int>("SDK_INT");
                }
            }
        }
        public static string UnknownDevice = "Unknown";
    }
    class AndroidHelper {
        /// <summary>
        /// Convert an Android int to a BtError
        /// </summary>
        /// <param name="error">The int from Android</param>
        /// <returns>A BtError</returns>
        public static BtError ToBtError(int error) {
            switch (error) {
                case 0:
                    return BtError.None;
                case 1:
                    return BtError.NoBluetooth;
                case 2:
                    return BtError.NoBLE;
                case 3:
                    return BtError.Disabled;
                case 4:
                    return BtError.NoServer;
                case 5:
                    return BtError.AlreadyRunning;
                default:
                    return BtError.Unknown;
            }
        }
        /// <summary>
        /// Convert a BtError to an Android int
        /// </summary>
        /// <param name="error">The BtError</param>
        /// <returns>An Android int</returns>
        public static int FromBtError(BtError error) {
            switch (error) {
                case BtError.None:
                    return 0;
                case BtError.NoBluetooth:
                    return 1;
                case BtError.NoBLE:
                    return 2;
                case BtError.Disabled:
                    return 3;
                case BtError.NoServer:
                    return 4;
                case BtError.AlreadyRunning:
                    return 5;
                default:
                    return -1;
            }
        }
        /// <summary>
        /// Convert an Android int to an AdvertiseError
        /// </summary>
        /// <param name="error">The Android int</param>
        /// <returns>An AdvertiseError</returns>
        public static AdvertiseError ToAdvertiseError(int error) {
            switch (error) {
                case 0:
                    return AdvertiseError.None;
                case 1:
                    return AdvertiseError.DataTooLarge;
                case 2:
                    return AdvertiseError.TooManyAdvertisers;
                case 3:
                    return AdvertiseError.AlreadyStarted;
                default:
                    return AdvertiseError.Unknown;
            }
        }
        /// <summary>
        /// Convert an AdvertiseError to an Android int
        /// </summary>
        /// <param name="error">The AdvertiseError</param>
        /// <returns>An Android int</returns>
        public static int FromAdvertiseError(AdvertiseError error) {
            switch (error) {
                case AdvertiseError.None:
                    return 0;
                case AdvertiseError.DataTooLarge:
                    return 1;
                case AdvertiseError.TooManyAdvertisers:
                    return 2;
                case AdvertiseError.AlreadyStarted:
                    return 3;
                default:
                    return -1;
            }
        }
        /// <summary>
        /// Convert an Android int to CharPermissions
        /// </summary>
        /// <param name="permissions">The Android int</param>
        /// <returns>CharPermissions</returns>
        public static CharPermissions ToCharPermissions(int permissions) {
            List<CharPermissions> perms = new List<CharPermissions>();
            if((permissions & 1) == 1) {
                perms.Add(CharPermissions.Read);
            }
            if((permissions & 2) == 2) {
                perms.Add(CharPermissions.ReadEncrypted);
            }
            if((permissions & 16) == 16) {
                perms.Add(CharPermissions.Write);
            }
            if((permissions & 32) == 32) {
                perms.Add(CharPermissions.WriteEncrypted);
            }
            if(perms.Count == 0) {
                throw new Exception("The int is invalid!!!");
            }else if(perms.Count == 1) {
                return perms[0];
            } else {
                CharPermissions p = perms[0];
                perms.RemoveAt(0);
                foreach(CharPermissions perm in perms){
                    p = p | perm;
                }
                return p;
            }
        }
        /// <summary>
        /// Convert CharPermissions to an Android int
        /// </summary>
        /// <param name="permissions">The CharPermissions</param>
        /// <returns>An Android int</returns>
        public static int FromCharPermissions(CharPermissions permissions) {
            int perms = 0;
            if((permissions & CharPermissions.Read) == CharPermissions.Read) {
                perms = perms | 1;
            }
            if ((permissions & CharPermissions.Write) == CharPermissions.Write) {
                perms = perms | 16;
            }
            if ((permissions & CharPermissions.ReadEncrypted) == CharPermissions.ReadEncrypted) {
                perms = perms | 2;
            }
            if ((permissions & CharPermissions.WriteEncrypted) == CharPermissions.WriteEncrypted) {
                perms = perms | 32;
            }
            return perms;
        }
        /// <summary>
        /// Convert an Android int to CharProperties
        /// </summary>
        /// <param name="properties">The Android int</param>
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
        /// Convert CharProperties to an Android int
        /// </summary>
        /// <param name="properties">The CharProperties</param>
        /// <returns>An Android int</returns>
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
        /// Get a C# List from a Java ArrayList
        /// </summary>
        /// <typeparam name="T">The primitive type for the List</typeparam>
        /// <param name="arrayList">The AndroidJavaObjects representing the ArrayList</param>
        /// <returns>The C# List</returns>
        public static List<T> FromArrayList<T>(AndroidJavaObject arrayList) {
            List<T> rtn = new List<T>();
            int count = arrayList.Call<int>("size");
            for (int i = 0; i < count; i++) {
                rtn.Add(arrayList.Call<T>("get", i));
            }
            return rtn;
        }
    }
    class AndroidDelegate: AndroidJavaProxy {
        BLEDelegate UnityDelegate;
        public AndroidDelegate(BLEDelegate unityDelegate) : base(AndroidValues.Package + "unity.BLEDelegateUnity") {
            UnityDelegate = unityDelegate;
        }
        // Server Specific
        public void onAdvertise(int error) {
            MainThread.Instance().Run(() => {
                UnityDelegate.OnAdvertise(AndroidHelper.ToAdvertiseError(error));
            });
        }
        public void onDeviceConnected(string address, object name) {
            MainThread.Instance().Run(() => {
                if (name == null)
                    name = AndroidValues.UnknownDevice;
                // Use object not string because if null is passed proxy looks for AndroidJavaObject
                UnityDelegate.OnDeviceConnected(address, name.ToString());
            });
            
        }
        public void onDeviceDisconnected(string address, object name) {
            MainThread.Instance().Run(() => {
                if (name == null)
                    name = AndroidValues.UnknownDevice;
                UnityDelegate.OnDeviceDisconnected(address, name.ToString());
            });
        }
        public void onNotificationSent(string characteristic, bool success) {
            MainThread.Instance().Run(() => {
                UnityDelegate.OnNotificationSent(characteristic, success);
            });
        }
        // Client Specific
        public void onDeviceDiscovered(string address, object name, int rssi) {
            MainThread.Instance().Run(() => {
                if (name == null)
                    name = AndroidValues.UnknownDevice;
                UnityDelegate.OnDeviceDiscovered(address, name.ToString(), rssi);
            });
        }
        public void onConnectToDevice(string address, object name, bool success) {
            MainThread.Instance().Run(() => {
                if (name == null)
                    name = AndroidValues.UnknownDevice;
                UnityDelegate.OnConnectToDevice(address, name.ToString(), success);
            });
        }
        public void onDisconnectFromDevice(string address, object name) {
            MainThread.Instance().Run(() => {
                if (name == null)
                    name = AndroidValues.UnknownDevice;
                UnityDelegate.OnDisconnectFromDevice(address, name.ToString());
            });
        }
        public void onServicesDiscovered() {
            MainThread.Instance().Run(() => {
                UnityDelegate.OnServicesDiscovered();
            });
        }
        // Shared
        public void onCharacteristicRead(string characteristic, string writingDeviceAddress, bool success, AndroidJavaObject value) {
            MainThread.Instance().Run(() => {
                UnityDelegate.OnCharacteristicRead(characteristic, writingDeviceAddress, success, value.Call<byte[]>("getData"));
            });
        }
        public void onCharacteristicWrite(string characteristic, bool success, AndroidJavaObject value) {
            MainThread.Instance().Run(() => {
                UnityDelegate.OnCharacteristicWrite(characteristic, success, value.Call<byte[]>("getData"));
            });
        }
        public void onDescriptorRead(string descriptor, string writingDeviceAddress, bool success, AndroidJavaObject value) {
            MainThread.Instance().Run(() => {
                UnityDelegate.OnDescriptorRead(descriptor, writingDeviceAddress, success, value.Call<byte[]>("getData"));
            });
        }
        public void onDescriptorWrite(string descriptor, bool success, AndroidJavaObject value) {
            MainThread.Instance().Run(() => {
                UnityDelegate.OnDescriptorWrite(descriptor, success, value.Call<byte[]>("getData"));
            });
        }
        public void onBluetoothPowerChanged(bool enabled) {
            MainThread.Instance().Run(() => {
                UnityDelegate.OnBluetoothPowerChanged(enabled);
            });
        }
        public void onBluetoothRequestResult(bool choseToEnable){
            MainThread.Instance().Run(() => {
                UnityDelegate.OnBluetoothRequestResult(choseToEnable);
            });
        }
    }
    class AndroidServer : IBLEServer {
#region Native Objects
        private AndroidJavaObject NativeServer;
        private AndroidJavaObject NativeDelegateObject;
        private AndroidDelegate NativeDelegate;
        private AndroidJavaObject Context;
        private bool supported = AndroidValues.SDKLevel >= 21;
#endregion

#region Properties
        public List<string> Services {
            get {
                if (supported) {
                    AndroidJavaObject list = NativeServer.Call<AndroidJavaObject>("getServices");
                    return AndroidHelper.FromArrayList<string>(list);
                }
                return new List<string>();
            }
        }
        public List<string> Characteristics {
            get {
                if (supported) {
                    AndroidJavaObject list = NativeServer.Call<AndroidJavaObject>("getCharacteristics");
                    return AndroidHelper.FromArrayList<string>(list);
                }
                return new List<string>();
            }
        }
        public List<string> Descriptors {
            get {
                if (supported) {
                    AndroidJavaObject list = NativeServer.Call<AndroidJavaObject>("getDescriptors");
                    return AndroidHelper.FromArrayList<string>(list);
                }
                return new List<string>();
            }
        }
        public List<string> AdvertiseServices {
            get {
                if (supported) {
                    AndroidJavaObject list = NativeServer.Call<AndroidJavaObject>("getAdvertiseServices");
                    return AndroidHelper.FromArrayList<string>(list);
                }
                return new List<string>();
            }
        }
        public bool IsRunning {
            get {
                if (supported) {
                    return NativeServer.Call<bool>("isRunning");
                }
                return false;
            }
        }
        public bool IsAdvertising {
            get {
                if (supported) {
                    return NativeServer.Call<bool>("isAdvertising");
                }
                return false;
            }
        }
#endregion

        /// <summary>
        /// Create an Android Specific QuickBLE Server
        /// </summary>
        /// <param name="unityDelegate">The BLEDelegate to use for callbacks</param>
        public AndroidServer(BLEDelegate unityDelegate) {
            if (supported) {
                NativeDelegate = new AndroidDelegate(unityDelegate);
                NativeDelegateObject = new AndroidJavaObject(AndroidValues.Package + "unity.UnityDelegate", NativeDelegate);
                AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                Context = jc.GetStatic<AndroidJavaObject>("currentActivity");
                NativeServer = new AndroidJavaObject(AndroidValues.Package + "BLEServer", Context, NativeDelegateObject);
            }
        }

#region Server Control
        public void AddService(string service) {
            if (supported) {
                NativeServer.Call("addService", service);
            }
        }
        public bool AddIncludedService(string service, string parentService) {
            if (supported) {
                return NativeServer.Call<bool>("addIncludedService", service, parentService);
            }
            return false;
        }
        public bool AddCharacteristic(string characteristic, string parentService, CharProperties properties = CharProperties.Read | CharProperties.Write | CharProperties.Notify, CharPermissions permissions = CharPermissions.Read | CharPermissions.Write) {
            if (supported) {
                return NativeServer.Call<bool>("addCharacteristic", characteristic, parentService, AndroidHelper.FromCharProperties(properties), AndroidHelper.FromCharPermissions(permissions));
            }
            return false;
        }
        public bool AddDescriptor(string descriptor, string parentCharacteristic) {
            if (supported) {
                return NativeServer.Call<bool>("addDescriptor", descriptor, 17);
            }
            return false;
        }
        public void AdvertiseService(string service, bool advertise = true) {
            if (supported) {
                NativeServer.Call("advertiseService", service, advertise);
            }
        }
        public void ClearGatt() {
            if (supported) {
                NativeServer.Call("clearGatt");
            }
        }
        public BtError CheckBluetooth() {
            if (supported) {
                return AndroidHelper.ToBtError(NativeServer.Call<int>("checkBluetooth"));
            }
            return BtError.NoServer;
        }
        public void RequestEnableBt() {
            if (supported) {
                NativeServer.Call("requestEnableBt");
            }
        }
        public BtError StartServer() {
            if (supported) {
                return AndroidHelper.ToBtError(NativeServer.Call<int>("startServer"));
            }
            return BtError.NoServer;
        }
        public void StopServer() {
            if (supported) {
                NativeServer.Call("stopServer");
            }
        }
        public void StartAdvertising() {
            if (supported) {
                NativeServer.Call("startAdvertising");
            }
        }
        public void StopAdvertising() {
            if (supported) {
                NativeServer.Call("stopAdvertising");
            }
        }
        public void NotifyDevice(string characteristic, string deviceAddress) {
            if (supported) {
                NativeServer.Call("notifyDevice", characteristic, deviceAddress);
            }
        }
#endregion

#region Characteristics and Descriptors
        public void WriteCharacteristic(string characteristic, byte[] data, bool notify) {
            if (supported) {
                NativeServer.Call("writeCharacteristic", characteristic, data, notify);
            }
        }
        public void ReadCharacteristic(string characteristic) {
            if (supported) {
                NativeServer.Call("readCharacteristic", characteristic);
            }
        }
        public void WriteDescriptor(string descriptor, byte[] data) {
            if (supported) {
                IntPtr val = AndroidJNIHelper.ConvertToJNIArray(data);
                NativeServer.Call("writeDescriptor", descriptor, val);
            }
        }
        public void ReadDescriptor(string descriptor) {
            if (supported) {
                NativeServer.Call("readDescriptor", descriptor);
            }
        }
        public bool HasService(string service) {
            if (supported) {
                return NativeServer.Call<bool>("hasService", service);
            }
            return false;
        }
        public bool HasCharacteristic(string characteristic) {
            if (supported) {
                return NativeServer.Call<bool>("hasCharacteristic", characteristic);
            }
            return false;
        }
        public bool HasDescriptor(string descriptor) {
            if (supported) {
                return NativeServer.Call<bool>("hasDescriptor", descriptor);
            }
            return false;
        }
#endregion

    }
    class AndroidClient : IBLEClient{
#region Native Objects
        private AndroidJavaObject NativeClient;
        private AndroidJavaObject NativeDelegateObject;
        private AndroidDelegate NativeDelegate;
        private AndroidJavaObject Context;
        private bool supported = AndroidValues.SDKLevel >= 18;
#endregion

#region Properties
        public List<string> Services {
            get {
                if (supported) {
                    AndroidJavaObject list = NativeClient.Call<AndroidJavaObject>("getServices");
                    return AndroidHelper.FromArrayList<string>(list);
                }
                return new List<string>();
            }
        }
        public List<string> Characteristics {
            get {
                if (supported) {
                    AndroidJavaObject list = NativeClient.Call<AndroidJavaObject>("getCharacteristics");
                    return AndroidHelper.FromArrayList<string>(list);
                }
                return new List<string>();
            }
        }
        public List<string> Descriptors {
            get {
                if (supported) {
                    AndroidJavaObject list = NativeClient.Call<AndroidJavaObject>("getDescriptors");
                    return AndroidHelper.FromArrayList<string>(list);
                }
                return new List<string>();
            }
        }
        public List<string> ScanServices {
            get {
                if (supported) {
                    AndroidJavaObject list = NativeClient.Call<AndroidJavaObject>("getScanServices");
                    return AndroidHelper.FromArrayList<string>(list);
                }
                return new List<string>();
            }
        }
        public bool IsScanning {
            get {
                if (supported) {
                    return NativeClient.Call<bool>("isScanning");
                }
                return false;
            }
        }
        public bool IsConnected {
            get {
                if (supported) {
                    return NativeClient.Call<bool>("isConnected");
                }
                return false;
            }
        }
#endregion

        /// <summary>
        /// Create an Android Specific QuickBLE Client
        /// </summary>
        /// <param name="unityDelegate">The BLEDelegate to use for callbacks</param>
        public AndroidClient(BLEDelegate unityDelegate, bool useNewMethod = true) {
            if (supported) {
                NativeDelegate = new AndroidDelegate(unityDelegate);
                NativeDelegateObject = new AndroidJavaObject(AndroidValues.Package + "unity.UnityDelegate", NativeDelegate);
                AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                Context = jc.GetStatic<AndroidJavaObject>("currentActivity");
                NativeClient = new AndroidJavaObject(AndroidValues.Package + "BLEClient", Context, NativeDelegateObject, useNewMethod);
            }
        }

#region Client Control
        public void ScanForService(string service, bool scanFor = true) {
            if (supported) {
                NativeClient.Call("scanForService", service, scanFor);
            }
        }
        public BtError CheckBluetooth() {
            if (supported) {
                return AndroidHelper.ToBtError(NativeClient.Call<int>("checkBluetooth"));
            }
            return BtError.Unknown;
        }
        public void RequestEnableBt() {
            if (supported) {
                NativeClient.Call("requestEnableBt");
            }
        }
        public BtError ScanForDevices() {
            if (supported) {
                return AndroidHelper.ToBtError(NativeClient.Call<int>("scanForDevices"));
            }
            return BtError.Unknown;
        }
        public void StopScanning() {
            if (supported) {
                NativeClient.Call("stopScanning");
            }
        }
        public void ConnectToDevice(string deviceAddress) {
            if (supported) {
                NativeClient.Call("connectToDevice", deviceAddress);
            }
        }
        public void Disconnect() {
            if (supported) {
                NativeClient.Call("disconnect");
            }
        }
        public void SubscribeToCharacteristic(string characteristic, bool subscribe = true) {
            if (supported) {
                NativeClient.Call("subscribeToCharacteristic", characteristic, subscribe);
            }
        }
#endregion

#region Characteristics and Descriptors
        public void ReadCharacteristic(string characteristic) {
            if (supported) {
                NativeClient.Call("readCharacteristic", characteristic);
            }
        }
        public void WriteCharacteristic(string characteristic, byte[] data) {
            if (supported) {
                NativeClient.Call("writeCharacteristic", characteristic, data);
            }
        }
        public void ReadDescriptor(string descriptor) {
            if (supported) {
                NativeClient.Call("readDescriptor", descriptor);
            }
        }
        public void WriteDescriptor(string descriptor, byte[] data) {
            if (supported) {
                NativeClient.Call("writeDescriptor", descriptor, data);
            }
        }
        public bool HasService(string service) {
            if (supported) {
                return NativeClient.Call<bool>("hasService", service);
            }
            return false;
        }
        public bool HasCharacteristic(string characteristic) {
            if (supported) {
                return NativeClient.Call<bool>("hasCharacteristic", characteristic);
            }
            return false;
        }
        public bool HasDescriptor(string descriptor) {
            if (supported) {
                return NativeClient.Call<bool>("hasDescriptor", descriptor);
            }
            return false;
        }
#endregion

    }
#endif
}