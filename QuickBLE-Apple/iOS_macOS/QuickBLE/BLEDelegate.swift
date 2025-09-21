import Foundation
import CoreBluetooth

@objc public protocol BLEDelegate{
    // Server Specific
    @objc func onAdvertise(error: Int)
    @objc func onDeviceConnected(address: String, name: String?)
    @objc func onDeviceDisconnected(address: String, name: String?)
    @objc func onNotificationSent(_ characterstic: String, success: Bool)
    
    // Client Specific
    @objc func onDeviceDiscovered(address: String, name: String?, rssi: Int)
    @objc func onConnectToDevice(address: String, name: String?, success: Bool)
    @objc func onDisconnectFromDevice(address: String, name: String?)
    @objc func onServicesDiscovered()
    
    // Shared
    @objc func onCharacteristicRead(_ characteristic: String, writingDeviceAddress: String, success: Bool, value: Data?)
    @objc func onCharacteristicWrite(_ characteristic: String, success: Bool, value: Data?)
    @objc func onDescriptorRead(_ descriptor: String, writingDeviceAddress: String, success: Bool, value: Data?)
    @objc func onDescriptorWrite(_ descriptor: String, success: Bool, value: Data?)
    @objc func onBluetoothPowerChange(_ enabled: Bool)
    @objc func onBluetoothRequestResult(_ choseToEnable: Bool)
}
