using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace QuickBLE {
    public class BLEDescriptor<T> {
        public T Descriptor;
        public byte[] DynamicValue;

        public BLEDescriptor(T descriptor, byte[] value) {
            Descriptor = descriptor;
            DynamicValue = value;
        }
    }
    public class BLECharacteristic<T> {
        public T Characteristic;
        public byte[] DynamicValue;
        public IReadOnlyList<GattSubscribedClient> previousClients = new List<GattSubscribedClient>();

        public BLECharacteristic(T characteristic, byte[] value) {
            Characteristic = characteristic;
            DynamicValue = value;
        }
    }
}
