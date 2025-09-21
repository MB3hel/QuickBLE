using QuickBLE.Unity.Android;
using QuickBLE.Unity.Apple;
//using QuickBLE.Unity.UWP;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuickBLE.Unity {
    #region Types
    public enum BtError { None, NoBluetooth, NoBLE, Disabled, NoServer, AlreadyRunning, Unknown, UnsupportedPlatform }
    public enum AdvertiseError { None, DataTooLarge, TooManyAdvertisers, AlreadyStarted, Unknown }
    public enum CharProperties { Broadcast = 1, Read = 2, WriteNoResponse = 4, Write = 8, Notify = 16, Indicate = 32, SignedWrite = 64, Extended = 128 }
    public enum CharPermissions { Read = 1, ReadEncrypted = 2, Write = 4, WriteEncrypted = 8 }
    #endregion

    #region Classes
    public class BLEServer {
        #region Properties and Variables

        // Native Interface
        IBLEServer BridgeServer;

        // Constructor properties
        private BLEDelegate ServerDelegate;

        // UUIDs for Gatt Services
        public List<string> Services {
            get { return BridgeServer.Services; }
        }
        public List<string> Characteristics {
            get { return BridgeServer.Characteristics; }
        }
        public List<string> Descriptors {
            get { return BridgeServer.Descriptors; }
        }
        public List<string> AdvertiseServices {
            get { return BridgeServer.AdvertiseServices; }
        }

        // Status
        public bool IsRunning {
            get { return BridgeServer.IsRunning; }
        }
        public bool IsAdvertising {
            get { return BridgeServer.IsAdvertising; }
        }
        #endregion

        /// <summary>
        /// Create a new Bluetooth Low Energy Server
        /// </summary>
        /// <param name="serverDelegate">The BLEDelegate to send callbacks to</param>
        public BLEServer(BLEDelegate serverDelegate) {
            ServerDelegate = serverDelegate;
#if UNITY_ANDROID
            BridgeServer = new AndroidServer(ServerDelegate);
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            BridgeServer = new AppleServer(ServerDelegate);
#elif UNITY_WSA && !UNITY_EDITOR
            BridgeServer = new UWPServer(ServerDelegate);
#else
            BridgeServer = new FallbackServer();
#endif
        }

        #region Server control
        /// <summary>
        /// Add a service to the server
        /// </summary>
        /// <param name="service">The UUID of the service</param>
        public void AddService(string service) {
            BridgeServer.AddService(service);
        }
        /// <summary>
        /// Add an included (second) service to a service
        /// </summary>
        /// <param name="service">The UUID for the included service</param>
        /// <param name="parentService">The UUID of the parent service</param>
        /// <returns>Was the service successfully added</returns>
        public bool AddIncludedService(string service, string parentService) {
            return BridgeServer.AddIncludedService(service, parentService);
        }
        /// <summary>
        /// Add a characteristic to a service
        /// </summary>
        /// <param name="characteristic">The UUID of the characteristic</param>
        /// <param name="parentService">The UUID of the parent service</param>
        /// <param name="properties">CharProperties for the characteristic</param>
        /// <param name="permissions">CharPermissions for the characteristic</param>
        /// <returns>Was the characteristic successfully added</returns>
        public bool AddCharacteristic(string characteristic, string parentService, CharProperties properties = CharProperties.Read | CharProperties.Write | CharProperties.Notify, CharPermissions permissions = CharPermissions.Read | CharPermissions.Write) {
            return BridgeServer.AddCharacteristic(characteristic, parentService, properties, permissions);
        }
        /// <summary>
        /// Add a descriptor a characteristic
        /// </summary>
        /// <param name="descriptor">The UUID of the descriptor</param>
        /// <param name="parentCharacteristic">The UUID of the parent characteristic</param>
        /// <returns>Was the descriptor successfully added.</returns>
        public bool AddDescriptor(string descriptor, string parentCharacteristic) {
            return BridgeServer.AddDescriptor(descriptor, parentCharacteristic);
        }
        /// <summary>
        /// Advertise a service's UUID
        /// </summary>
        /// <param name="service">The UUID to advertise</param>
        /// <param name="advertise">Whether or not to advertise the UUID</param>
        public void AdvertiseService(string service, bool advertise = true) {
            BridgeServer.AdvertiseService(service, advertise);
        }
        /// <summary>
        /// Remove all services, characteristics, and descriptors from the server and stop the server.
        /// </summary>
        public void ClearGatt() {
            BridgeServer.ClearGatt();
        }
        /// <summary>
        /// Check if the Server is supported on this device
        /// </summary>
        /// <returns>A BtError</returns>
        public BtError CheckBluetooth() {
            return BridgeServer.CheckBluetooth();
        }
        /// <summary>
        /// Show a dialog requesting that the user enable bluetooth
        /// </summary>
        public void RequestEnableBt() {
            BridgeServer.RequestEnableBt();
        }
        /// <summary>
        /// Start the gatt server
        /// </summary>
        /// <returns>A BtError</returns>
        public BtError StartServer() {
            return BridgeServer.StartServer();
        }
        /// <summary>
        /// Stop the gatt server
        /// </summary>
        public void StopServer() {
            BridgeServer.StopServer();
        }
        /// <summary>
        /// Start advertising the gatt server
        /// </summary>
        public void StartAdvertising() {
            BridgeServer.StartAdvertising();
        }
        /// <summary>
        /// Stop advertising the gatt server
        /// </summary>
        public void StopAdvertising() {
            BridgeServer.StopAdvertising();
        }

        public void NotifyDevice(string characteristic, string deviceAddress) {
            BridgeServer.NotifyDevice(characteristic, deviceAddress);
        }
        #endregion

        #region Characteristics and Descriptors
        /// <summary>
        /// Write a value to a characteristic
        /// </summary>
        /// <param name="characteristic">The UUID of the characteristic</param>
        /// <param name="data">The value to write</param>
        /// <param name="notify">Whether or not to notify subscribed devices</param>
        public void WriteCharacteristic(string characteristic, byte[] data, bool notify) {
            BridgeServer.WriteCharacteristic(characteristic, data, notify);
        }
        /// <summary>
        /// Read the value of a characteristic
        /// </summary>
        /// <param name="characteristc">The UUID of the characteristic</param>
        public void ReadCharacteristic(string characteristc) {
            BridgeServer.ReadCharacteristic(characteristc);
        }
        /// <summary>
        /// Write a value to a descriptor
        /// </summary>
        /// <param name="descriptor">The UUID of the descriptor</param>
        /// <param name="data">The value to write</param>
        public void WriteDescriptor(string descriptor, byte[] data) {
            BridgeServer.WriteDescriptor(descriptor, data);
        }
        /// <summary>
        /// Read the value of a descriptor
        /// </summary>
        /// <param name="descriptor">The UUID of the descriptor</param>
        public void ReadDescriptor(string descriptor) {
            BridgeServer.ReadDescriptor(descriptor);
        }
        /// <summary>
        /// Check if the server has a service
        /// </summary>
        /// <param name="service">The UUID of the service</param>
        /// <returns>Whether or not the server has the service</returns>
        public bool HasService(string service) {
            return BridgeServer.HasService(service);
        }
        /// <summary>
        /// Check if the server has a characteristic
        /// </summary>
        /// <param name="characteristic">The UUID of the characteristic</param>
        /// <returns>Whether or not the server has the characteristic</returns>
        public bool HasCharacteristic(string characteristic) {
            return BridgeServer.HasCharacteristic(characteristic);
        }
        /// <summary>
        /// Check if the server has a descriptor
        /// </summary>
        /// <param name="descriptor">The UUID of the descriptor</param>
        /// <returns>Whether or not the server has the descriptor</returns>
        public bool HasDescriptor(string descriptor) {
            return BridgeServer.HasDescriptor(descriptor);
        }
        #endregion

    }
    public class BLEClient {
        // Native Interface
        IBLEClient BridgeClient;

        #region Properties and Variables
        // Constructor properties
        private BLEDelegate ClientDelegate;

        // UUIDs for Gatt Services
        public List<string> Services {
            get { return BridgeClient.Services; }
        }
        public List<string> Characteristics {
            get { return BridgeClient.Characteristics; }
        }
        public List<string> Descriptors {
            get { return BridgeClient.Descriptors; }
        }
        public List<string> ScanServices {
            get { return BridgeClient.ScanServices; }
        }

        // Status
        public bool IsScanning {
            get { return BridgeClient.IsScanning; }
        }
        public bool IsConnected {
            get { return BridgeClient.IsConnected; }
        }
        #endregion

        /// <summary>
        /// Create a new Bluetooth Low Energy Client
        /// </summary>
            /// <param name="clientDelegate">The BLEDelegate to send callbacks to</param>
        public BLEClient(BLEDelegate clientDelegate) {
            ClientDelegate = clientDelegate;
#if UNITY_ANDROID
            BridgeClient = new AndroidClient(ClientDelegate);
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            BridgeClient = new AppleClient(ClientDelegate);
#elif UNITY_WSA && !UNITY_EDITOR
            BridgeClient = new UWPClient(ClientDelegate);
#else
            BridgeClient = new FallbackClient();
#endif
        }

        #region Client Control
        /// <summary>
        /// Scan for devices advertising a certain service
        /// </summary>
        /// <param name="service">The service to scan for</param>
        /// <param name="scanFor">Whether or not to scan for the service</param>
        public void ScanForService(string service, bool scanFor = true) {
            BridgeClient.ScanForService(service, scanFor);
        }
        /// <summary>
        /// Check bluetooth low energy client compatibility for the device
        /// </summary>
        /// <returns>A BtError</returns>
        public BtError CheckBluetooth() {
            return BridgeClient.CheckBluetooth();
        }
        /// <summary>
        /// Show a dialog requesting that the user enable bluetooth
        /// </summary>
        public void RequestEnableBt() {
            BridgeClient.RequestEnableBt();
        }
        /// <summary>
        /// Start scanning for devices
        /// </summary>
        /// <returns>A BtError</returns>
        public BtError ScanForDevices() {
            return BridgeClient.ScanForDevices();
        }
        /// <summary>
        /// Stop Scanning
        /// </summary>
        public void StopScanning() {
            BridgeClient.StopScanning();
        }
        /// <summary>
        /// Connect to a specified device
        /// </summary>
        /// <param name="deviceAddress">The device to connect to</param>
        public void ConnectToDevice(string deviceAddress) {
            BridgeClient.ConnectToDevice(deviceAddress);
        }
        /// <summary>
        /// Disconnect from the server if connected
        /// </summary>
        public void Disconnect() {
            BridgeClient.Disconnect();
        }
        /// <summary>
        /// Subscribe to a characteristic to recieve notifications when its value is changed
        /// </summary>
        /// <param name="characteristic">The characteristic to subscribe to</param>
        /// <param name="subscribe">Whether or not to subscrive to the characteristic (false to unsubscribe)</param>
        public void SubscribeToCharacteristic(string characteristic, bool subscribe = true) {
            BridgeClient.SubscribeToCharacteristic(characteristic, subscribe);
        }
        #endregion

        #region Characteristics and Descriptors
        /// <summary>
        /// Read a value from a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to read</param>
        public void ReadCharacteristic(string characteristic) {
            BridgeClient.ReadCharacteristic(characteristic);
        }
        /// <summary>
        /// Write a value to a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to write</param>
        /// <param name="data">The value to write (as bytes)</param>
        public void WriteCharacteristic(string characteristic, byte[] data) {
            BridgeClient.WriteCharacteristic(characteristic, data);
        }
        /// <summary>
        /// Read a value from a descriptor
        /// </summary>
        /// <param name="descriptor">The descriptor to read</param>
        public void ReadDescriptor(string descriptor) {
            BridgeClient.ReadDescriptor(descriptor);
        }
        /// <summary>
        /// Write a value to a descriptor
        /// </summary>
        /// <param name="descriptor">The descriptor to write</param>
        /// <param name="data">The value to write (as bytes)</param>
        public void WriteDescriptor(string descriptor, byte[] data) {
            BridgeClient.WriteDescriptor(descriptor, data);
        }
        /// <summary>
        /// Check if the server has a service
        /// </summary>
        /// <param name="service">The service to search for</param>
        /// <returns>Whether or not the server has the service</returns>
        public bool HasService(string service) {
            return BridgeClient.HasService(service);
        }
        /// <summary>
        /// CHeck if the server has a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to search for</param>
        /// <returns>Whether or not the server has the characteristic</returns>
        public bool HasCharacteristic(string characteristic) {
            return BridgeClient.HasCharacteristic(characteristic);
        }
        /// <summary>
        /// Check if the server has a descriptor
        /// </summary>
        /// <param name="descriptor">The descriptor to search for</param>
        /// <returns>Whether or not the server has the descriptor</returns>
        public bool HasDescriptor(string descriptor) {
            return BridgeClient.HasDescriptor(descriptor);
        }
        #endregion
    }
    #endregion

    #region Interfaces
    public interface IBLEServer {
        #region Properties
        List<string> Services { get; }
        List<string> Characteristics { get; }
        List<string> Descriptors { get; }
        List<string> AdvertiseServices { get; }
        bool IsRunning { get; }
        bool IsAdvertising { get; }
        #endregion
        #region Server Control
        void AddService(string service);
        bool AddIncludedService(string service, string parentService);
        bool AddCharacteristic(string characteristic, string parentService, CharProperties properties = CharProperties.Read | CharProperties.Write | CharProperties.Notify, CharPermissions permissions = CharPermissions.Read | CharPermissions.Write);
        bool AddDescriptor(string descriptor, string parentCharacteristic);
        void AdvertiseService(string service, bool advertise = true);
        void ClearGatt();
        BtError CheckBluetooth();
        void RequestEnableBt();
        BtError StartServer();
        void StopServer();
        void StartAdvertising();
        void StopAdvertising();
        void NotifyDevice(string characteristic, string deviceAddress);
        #endregion
        #region Characteristics and Descriptors
        void WriteCharacteristic(string characteristic, byte[] data, bool notify);
        void ReadCharacteristic(string characteristic);
        void WriteDescriptor(string descriptor, byte[] data);
        void ReadDescriptor(string descriptor);
        bool HasService(string service);
        bool HasCharacteristic(string characteristic);
        bool HasDescriptor(string descriptor);
        #endregion
    }
    public interface IBLEClient {
        #region Properties
        List<string> Services { get; }
        List<string> Characteristics { get; }
        List<string> Descriptors { get; }
        List<string> ScanServices { get; }
        bool IsScanning { get; }
        bool IsConnected { get; }
        #endregion
        #region Client Control
        void ScanForService(string service, bool scanFor = true);
        BtError CheckBluetooth();
        void RequestEnableBt();
        BtError ScanForDevices();
        void StopScanning();
        void ConnectToDevice(string deviceAddress);
        void Disconnect();
        void SubscribeToCharacteristic(string characteristic, bool subscribe = true);
        #endregion
        #region Characteristics and Descriptors
        void WriteCharacteristic(string characteristic, byte[] data);
        void ReadCharacteristic(string characteristic);
        void WriteDescriptor(string descriptor, byte[] data);
        void ReadDescriptor(string descriptor);
        bool HasService(string service);
        bool HasCharacteristic(string characteristic);
        bool HasDescriptor(string descriptor);
        #endregion
    }
    public interface BLEDelegate {
        // Server Specific
        void OnAdvertise(AdvertiseError error);
        void OnDeviceConnected(string address, string name);
        void OnDeviceDisconnected(string address, string name);
        void OnNotificationSent(string characteristic, bool success);

        // Client Specific
        void OnDeviceDiscovered(string address, string name, int rssi);
        void OnConnectToDevice(string address, string name, bool success);
        void OnDisconnectFromDevice(string address, string name);
        void OnServicesDiscovered();

        // Shared
        void OnCharacteristicRead(string characteristic, string writingDeviceAddress, bool success, byte[] value);
        void OnCharacteristicWrite(string characteristic, bool success, byte[] value);
        void OnDescriptorRead(string descriptor, string writingDeviceAddress, bool success, byte[] value);
        void OnDescriptorWrite(string descriptor, bool success, byte[] value);
        void OnBluetoothPowerChanged(bool enabled);
        void OnBluetoothRequestResult(bool choseToEnable);
    }
    #endregion

    #region Fallback Classes
    public class FallbackServer : IBLEServer {
        public List<string> Services {
            get { return new List<string>(); }
        }

        public List<string> Characteristics {
            get { return new List<string>(); }
        }

        public List<string> Descriptors {
            get { return new List<string>(); }
        }

        public List<string> AdvertiseServices {
            get { return new List<string>(); }
        }

        public bool IsRunning {
            get { return false; }
        }

        public bool IsAdvertising {
            get { return false; }
        }

        public bool AddCharacteristic(string characteristic, string parentService, CharProperties properties = (CharProperties)26, CharPermissions permissions = (CharPermissions)5) {
            return false;
        }

        public bool AddDescriptor(string descriptor, string parentCharacteristic) {
            return false;
        }

        public bool AddIncludedService(string service, string parentService) {
            return false;
        }

        public void AddService(string service) { }

        public void AdvertiseService(string service, bool advertise = true) { }

        public BtError CheckBluetooth() {
            return BtError.UnsupportedPlatform;
        }

        public void ClearGatt() { }

        public bool HasCharacteristic(string characteristic) {
            return false;
        }

        public bool HasDescriptor(string descriptor) {
            return false;
        }

        public bool HasService(string service) {
            return false;
        }

        public void ReadCharacteristic(string characteristic) { }

        public void ReadDescriptor(string descriptor) { }

        public void RequestEnableBt() { }

        public void StartAdvertising() { }

        public BtError StartServer() {
            return BtError.UnsupportedPlatform;
        }

        public void StopAdvertising() { }

        public void StopServer() { }

        public void NotifyDevice(string characteristic, string deviceAddress) { }

        public void WriteCharacteristic(string characteristic, byte[] data, bool notify) { }

        public void WriteDescriptor(string descriptor, byte[] data) { }
    }
    public class FallbackClient : IBLEClient {
        public List<string> Services {
            get { return new List<string>(); }
        }

        public List<string> Characteristics {
            get { return new List<string>(); }
        }

        public List<string> Descriptors {
            get { return new List<string>(); }
        }

        public List<string> ScanServices {
            get { return new List<string>(); }
        }

        public bool IsScanning {
            get { return false; }
        }

        public bool IsConnected {
            get { return false; }
        }

        public BtError CheckBluetooth() {
            return BtError.UnsupportedPlatform;
        }

        public void ConnectToDevice(string deviceAddress) { }

        public void Disconnect() { }

        public bool HasCharacteristic(string characteristic) {
            return false;
        }

        public bool HasDescriptor(string descriptor) {
            return false;
        }

        public bool HasService(string service) {
            return false;
        }

        public void ReadCharacteristic(string characteristic) { }

        public void ReadDescriptor(string descriptor) { }

        public void RequestEnableBt() { }

        public BtError ScanForDevices() {
            return BtError.UnsupportedPlatform;
        }

        public void ScanForService(string service, bool scanFor = true) { }

        public void StopScanning() { }

        public void SubscribeToCharacteristic(string characteristic, bool subscribe = true) { }

        public void WriteCharacteristic(string characteristic, byte[] data) { }

        public void WriteDescriptor(string descriptor, byte[] data) { }
    }
    #endregion
}