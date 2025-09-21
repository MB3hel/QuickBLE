
namespace QuickBLE {
    public interface BLEDelegate {
        // Server Specific
        void OnAdvertise(int error);
        void OnDeviceConnected(string address, string name);
        void OnDeviceDisconnected(string address, string name);

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

    }
}
