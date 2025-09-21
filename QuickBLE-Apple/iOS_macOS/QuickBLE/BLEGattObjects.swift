import Foundation
import CoreBluetooth

@objc public class BLEDescriptor: CBMutableDescriptor{
    public var dynamicValue: Any?
    public override init(type UUID: CBUUID, value: Any?){
        super.init(type: UUID, value: nil)
        self.dynamicValue = value
    }
    public convenience init(descriptor: CBDescriptor){
        self.init(type: descriptor.uuid, value: descriptor.value)
    }
}
@objc public class BLECharacteristic: CBMutableCharacteristic{
    public var dynamicValue: Data?
    public override init(type UUID: CBUUID, properties: CBCharacteristicProperties, value: Data?, permissions: CBAttributePermissions){
        super.init(type: UUID, properties: properties, value: nil, permissions: permissions)
        self.dynamicValue = value
    }
    public convenience init(characteristic: CBCharacteristic){
        self.init(type: characteristic.uuid, properties: characteristic.properties, value: characteristic.value, permissions: CBAttributePermissions.readable)
    }
}
