import Foundation
import CoreBluetooth

#if os(iOS)
    import UIKit
#elseif os(macOS)
    import Cocoa
#endif

@objc public class BLEClient: NSObject, CBCentralManagerDelegate, CBPeripheralDelegate{
    //MARK: Properties and Variables
    
    private let UNKNOWN_WRITING_DEV_ADDRESS = "unknown"
    
    private(set) var delegate: BLEDelegate!
    // Platform Specific Objects
    private var serviceObjects: [CBService] = []
    private var characteristicObjects: [CBCharacteristic] = []
    private var descriptorObjects: [CBDescriptor] = []
    //UUIDs for Gatt Objects
    private(set) var services: [String] = []
    private(set) var characteristics: [String] = []
    private(set) var descriptors: [String] = []
    private(set) var scanServices: [String] = []
    
    // Platform Specific Options
    var continuousScan = false
    
    // Status
    private(set) var isScanning = false;
    var isConnected: Bool{
        get{
            return connectedPeripheral != nil
        }
    }
    
    // Objective-C Mess:
    @objc public func getIsScanning() -> Bool{
        return isScanning;
    }
    @objc public func getIsConnected() -> Bool{
        return isConnected;
    }
    @objc public func getServices() -> [String]{
        return services
    }
    @objc public func getCharacteristics() -> [String]{
        return characteristics
    }
    @objc public func getDescriptors() -> [String]{
        return descriptors;
    }
    @objc public func getScanServices() -> [String]{
        return scanServices;
    }
    @objc public func getContinuousScan() -> Bool{
        return continuousScan;
    }
    @objc public func setContinuousScan(_ continuous: Bool){
        continuousScan = continuous;
    }
    @objc public func setRequestBtTitle(_ title: String){
        REQUEST_BT_TITLE = title;
    }
    @objc public func getRequestBtTitle() -> String{
        return REQUEST_BT_TITLE;
    }
    @objc public func setRequestBtMessage(_ message: String){
        REQUEST_BT_MESSAGE = message;
    }
    @objc public func getRequestBtMessage() -> String{
        return REQUEST_BT_MESSAGE;
    }
    @objc public func setRequestBtConfirm(_ confirm: String){
        REQUEST_BT_CONFIRM = confirm;
    }
    @objc public func getRequestBtConfrim() -> String{
        return REQUEST_BT_CONFIRM;
    }
    @objc public func setRequestBtDeny(_ deny: String){
        REQUEST_BT_DENY = deny;
    }
    @objc public func getRequestBtDeny() -> String{
        return REQUEST_BT_DENY;
    }
    
    
    // Keep track of detected devices
    private var deviceAddresses: [String] = []
    private(set) var devices: [CBPeripheral] = []

    // Enable BT Dialog Text
    var REQUEST_BT_TITLE = "Bluetooth Required"
    var REQUEST_BT_MESSAGE = "Please enable bluetooth in Settings."
    var REQUEST_BT_CONFIRM = "Settings"
    var REQUEST_BT_DENY = "Cancel"
    
    //Apple Specific Bluetooth Objects
    private var centralManager: CBCentralManager!
    private var connectedPeripheral: CBPeripheral?
    
    //MARK: Constructor
    @objc public init(_ delegate: BLEDelegate){
        super.init()
        self.delegate = delegate
        self.centralManager = CBCentralManager(delegate: self, queue: nil, options: [CBCentralManagerOptionShowPowerAlertKey: false])
    }
    
    /**
     Expand UUID if it is only 4 chars (stick it into BLE Base UUID)
    */
    private func expand(uuidString: String) -> String{
        if(uuidString.count == 4){
            // Add the BLE Base UUID
            return "0000" + uuidString + "-0000-1000-8000-00805F9B34FB";
        }
        return uuidString
    }
    
    //MARK: Client Control
    
    /**
     Scan for devices advertising a certain service
     - parameter service: The service to scan for
     - parameter scanFor: Whether or not to scan for the service
    */
    @objc public func scanForService(_ service: String, scanFor: Bool = true){
        if(scanFor && !scanServices.contains(service.uppercased())){
            scanServices.append(service.uppercased())
        }else if(!scanFor && scanServices.contains(service.uppercased())){
            scanServices.remove(at: scanServices.index(of: service.uppercased())!)
        }
    }
    /**
     Check bluetooth low energy client compatibility for the device
     - returns: An error code (BtError)
    */
    @objc public func checkBluetooth() -> Int{
        return centralManager.state.rawValue
    }
    
#if os(iOS)
    /**
     Show adialog requesting that the user enable bluetooth
     - parameter vc: The current UIViewController
     */
    @objc public func requestEnableBt(){
        let vc = UIApplication.shared.keyWindow?.rootViewController;
        let alert = UIAlertController(title: REQUEST_BT_TITLE, message: REQUEST_BT_MESSAGE, preferredStyle: .alert);
        let cancelAction = UIAlertAction(title: REQUEST_BT_DENY, style: .cancel, handler: {(action) -> Void in
            alert.dismiss(animated: true, completion: nil)
            self.delegate.onBluetoothRequestResult(false)
        })
        let settingsAction = UIAlertAction(title: REQUEST_BT_CONFIRM, style: .default, handler: {(action) -> Void in
            let btSettings = URL(string: "App-Prefs:root=Bluetooth")
            if(btSettings != nil){
                if UIApplication.shared.canOpenURL(btSettings!) {
                    if #available(iOS 10.0, *) {
                        UIApplication.shared.open(btSettings!, options: [:], completionHandler: nil)
                    } else {
                        UIApplication.shared.openURL(btSettings!)
                    }
                }
            }
            alert.dismiss(animated: true, completion: nil)
            self.delegate.onBluetoothRequestResult(true)
        })
        alert.addAction(settingsAction)
        alert.addAction(cancelAction)
        vc?.present(alert, animated: true, completion: nil);
    }
#elseif os(macOS)
    /**
     Show adialog requesting that the user enable bluetooth
     */
    @objc public func requestEnableBt(){
        let alert = NSAlert();
        alert.addButton(withTitle: REQUEST_BT_CONFIRM)
        alert.addButton(withTitle: REQUEST_BT_DENY)
        alert.messageText = REQUEST_BT_TITLE
        alert.informativeText = REQUEST_BT_MESSAGE
        alert.alertStyle = .warning
        if(alert.runModal() == NSApplication.ModalResponse.alertFirstButtonReturn){
            self.delegate.onBluetoothRequestResult(true)
            // Yes this is absolutely rediculous, even by apple's standards
            // To open bt preferences: run command in bash
            let task = Process()
            task.launchPath = "/usr/bin/open"
            task.arguments = ["/System/Library/PreferencePanes/Bluetooth.prefPane"]
            task.launch()
        }else{
            self.delegate.onBluetoothRequestResult(false)
        }
    }
#endif
    /**
     Start scanning for devices
     - returns: An error code (BtError)
    */
    @objc public func scanForDevices() -> Int{
        if(!isScanning){
            services.removeAll()
            characteristics.removeAll()
            descriptors.removeAll()
            serviceObjects.removeAll()
            characteristicObjects.removeAll()
            descriptorObjects.removeAll()
            let error = checkBluetooth()
            if(error != BtError.None){
                return error
            }
            var uuids: [CBUUID] = []
            for s in scanServices{
                uuids.append(CBUUID(string: s))
            }
            centralManager.scanForPeripherals(withServices: uuids, options: [CBCentralManagerScanOptionAllowDuplicatesKey: continuousScan])
            isScanning = true;
            return BtError.None
        }else{
            return BtError.AlreadyRunning
        }
    }
    /**
     Stop scanning
    */
    @objc public func stopScanning(){
        if(isScanning){
            centralManager.stopScan()
            isScanning = false;
        }
    }
    
    /**
     Connect to a specified device
     - parameter deviceAddress: The address of the device to connect to
    */
    @objc public func connectToDevice(_ deviceAddress: String){
        if(connectedPeripheral != nil){
            disconnect()
        }
        let dev = devices.first(where: {$0.identifier.uuidString.uppercased() == deviceAddress.uppercased()})
        if(dev == nil){
            return;
        }
        centralManager.connect(dev!, options: [CBConnectPeripheralOptionNotifyOnConnectionKey: true, CBConnectPeripheralOptionNotifyOnDisconnectionKey: true, CBConnectPeripheralOptionNotifyOnNotificationKey: true])
    }
    /**
     Disconnect from server if connected
    */
    @objc public func disconnect(){
        if(isConnected){
            centralManager.cancelPeripheralConnection(connectedPeripheral!)
            services.removeAll()
            characteristics.removeAll()
            descriptors.removeAll()
            serviceObjects.removeAll()
            characteristicObjects.removeAll()
            descriptorObjects.removeAll()
            connectedPeripheral = nil
        }
    }
    /**
     Subscribe to a characteristic to recieve notifications when it's value is changed
     - parameter characteristic: The characteristic to subscribe to
     - parameter subscribe: Whether or not to subscribe to the characteristic (false to unsubscribe)
    */
    @objc public func subscribeToCharacteristic(_ characteristic: String, subscribe: Bool = true){
        guard let char = getCharacteristic(uuid: CBUUID(string: characteristic)) else{
            return
        }
        if(connectedPeripheral != nil){
            connectedPeripheral?.setNotifyValue(subscribe, for: char)
        }
    }
    
    //MARK Characteristics and Descriptors
    
    /**
     Read a value from a characteristic
     - parameter characteristic: The characteristic to read
    */
    @objc public func readCharacteristic(_ characteristic: String){
        guard let char = getCharacteristic(uuid: CBUUID(string: characteristic)) else{
            return
        }
        if(connectedPeripheral != nil){
            connectedPeripheral!.readValue(for: char)
        }
    }
    /**
     Write a value to a characteristic
     - parameter characteristic: The characteristic to write
     - parameter data: The value to write (as Data)
    */
    @objc public func writeCharacteristic(_ characteristic: String, data: Data?){
        guard let char = self.getCharacteristic(uuid: CBUUID(string: characteristic)) else{
            return
        }
        if self.connectedPeripheral != nil, data != nil{
            self.connectedPeripheral!.writeValue(data!, for: char, type: .withResponse)
        }
    }
    /**
     Read a value from a descriptor
     - parameter descriptor: The descriptor to read
    */
    @objc public func readDescriptor(_ descriptor: String){
        guard let desc = getDescriptor(uuid: CBUUID(string: descriptor)) else{
            return
        }
        if(connectedPeripheral != nil){
            connectedPeripheral!.readValue(for: desc)
        }
    }
    /**
     Write a value to a descriptor
     - parameter descriptor: The descriptor to write
     - parameter data: The value to write (as Data)
    */
    @objc public func writeDescriptor(_ descriptor: String, data: Data?){
        guard let desc = getDescriptor(uuid: CBUUID(string: descriptor)) else{
            return
        }
        if connectedPeripheral != nil, data != nil{
            connectedPeripheral!.writeValue(data!, for: desc)
        }
    }
    
    /**
     Get a service from a UUID
     -parameter uuid: The uuid of the service
     - returns: The CBService or nil
    */
    private func getService(uuid: CBUUID) -> CBService?{
        return serviceObjects.first(where: { $0.uuid.isEqual(uuid) })
    }
    /**
     Get a characteristic from a UUID
     -parameter uuid: The uuid of the characteristic
     - returns: The CBCharacteristic or nil
     */
    private func getCharacteristic(uuid: CBUUID) -> CBCharacteristic?{
        return characteristicObjects.first(where: { $0.uuid.isEqual(uuid) })
    }
    /**
     Get a descriptor from a UUID
     -parameter uuid: The uuid of the descriptor
     - returns: The CBDescriptor or nil
     */
    private func getDescriptor(uuid: CBUUID) -> CBDescriptor?{
        return descriptorObjects.first(where: { $0.uuid.isEqual(uuid) })
    }
    
    /**
     Check if the client has a service
     - parameter service: The service to search for
     - returns: Whether or not the client has the service
     */
    @objc public func hasService(_ service: String) -> Bool{
        return services.contains(service.uppercased())
    }
    /**
     Check if the client has a characteristic
     - parameter characteristic: The service to search for
     - returns: Whether or not the client has the characteristic
     */
    @objc public func hasCharacteristic(_ characteristic: String) -> Bool{
        return characteristics.contains(characteristic.uppercased())
    }
    /**
     Check if the client has a descriptor
     - parameter descriptor: The descriptor to search for
     - returns: Whether or not the client has the descriptor
     */
    @objc public func hasDescriptor(_ descriptor: String) -> Bool{
        return descriptors.contains(descriptor.uppercased())
    }
    
    //MARK: CBCentralManagerDelegate
    @objc public func centralManagerDidUpdateState(_ central: CBCentralManager) {
        let result = central.state.rawValue == BtError.None;
        if (!result){
            stopScanning()
            disconnect()
        }
        delegate.onBluetoothPowerChange(result)
    }
    @objc public func centralManager(_ central: CBCentralManager, didConnect peripheral: CBPeripheral) {
        peripheral.delegate = self
        peripheral.discoverServices(nil)
        connectedPeripheral = peripheral
        services.removeAll()
        characteristics.removeAll()
        descriptors.removeAll()
        delegate.onConnectToDevice(address: peripheral.identifier.uuidString.uppercased(), name: peripheral.name, success: true)
    }
    @objc public func centralManager(_ central: CBCentralManager, didFailToConnect peripheral: CBPeripheral, error: Error?) {
        delegate.onConnectToDevice(address: peripheral.identifier.uuidString.uppercased(), name: peripheral.name, success: false)
    }
    @objc public func centralManager(_ central: CBCentralManager, didDisconnectPeripheral peripheral: CBPeripheral, error: Error?) {
        delegate.onDisconnectFromDevice(address: peripheral.identifier.uuidString.uppercased(), name: peripheral.name)
    }
    @objc public func centralManager(_ central: CBCentralManager, didDiscover peripheral: CBPeripheral, advertisementData: [String : Any], rssi RSSI: NSNumber) {
        if(!deviceAddresses.contains(peripheral.identifier.uuidString)){
            devices.append(peripheral)
            deviceAddresses.append(peripheral.identifier.uuidString)
        }
        delegate.onDeviceDiscovered(address: peripheral.identifier.uuidString.uppercased(), name: peripheral.name, rssi: RSSI.intValue)
    }
    
    // Keep track of how many responses there should be and have been for discovering chars/incServices/descs
    // This is Apple Specific
    private var charCallCount = 0
    private var incCallCount = 0
    private var descCallCount = 0
    
    private var completedChar = 0
    private var completedInc = 0
    private var completedDesc = 0
    
    //MARK: CBPeripheralDelegate
    @objc public func peripheral(_ peripheral: CBPeripheral, didDiscoverServices error: Error?) {
        peripheral.delegate = self
        if(peripheral.services != nil){
            serviceObjects.append(contentsOf: peripheral.services!)
            charCallCount = serviceObjects.count
            incCallCount = serviceObjects.count
            descCallCount = 0
            for service in peripheral.services!{
                services.append(expand(uuidString: service.uuid.uuidString))
                peripheral.discoverIncludedServices(nil, for: service)
                peripheral.discoverCharacteristics(nil, for: service);
            }
        }
    }
    @objc public func peripheral(_ peripheral: CBPeripheral, didDiscoverIncludedServicesFor service: CBService, error: Error?) {
        completedInc += 1
        peripheral.delegate = self
        if(service.includedServices != nil){
            serviceObjects.append(contentsOf: service.includedServices!)
            charCallCount += service.includedServices!.count
            for s in service.includedServices!{
                services.append(expand(uuidString: s.uuid.uuidString))
                peripheral.discoverCharacteristics(nil, for: s)
            }
        }
        allDiscovered()
    }
    @objc public func peripheral(_ peripheral: CBPeripheral, didDiscoverCharacteristicsFor service: CBService, error: Error?) {
        completedChar += 1
        peripheral.delegate = self
        if(service.characteristics != nil){
            characteristicObjects.append(contentsOf: service.characteristics!)
            descCallCount += service.characteristics!.count
            for characteristic in service.characteristics!{
                characteristics.append(expand(uuidString: characteristic.uuid.uuidString))
                peripheral.discoverDescriptors(for: characteristic)
            }
        }
        allDiscovered()
    }
    @objc public func peripheral(_ peripheral: CBPeripheral, didDiscoverDescriptorsFor characteristic: CBCharacteristic, error: Error?) {
        completedDesc += 1
        if(characteristic.descriptors != nil){
            descriptorObjects.append(contentsOf: characteristic.descriptors!)
            for descriptor in characteristic.descriptors!{
                descriptors.append(expand(uuidString: descriptor.uuid.uuidString))
            }
        }
        allDiscovered()
    }
    @objc public func peripheral(_ peripheral: CBPeripheral, didUpdateValueFor characteristic: CBCharacteristic, error: Error?) {
        delegate.onCharacteristicRead(expand(uuidString: characteristic.uuid.uuidString).uppercased(), writingDeviceAddress: UNKNOWN_WRITING_DEV_ADDRESS, success: error == nil, value: characteristic.value)
    }
    @objc public func peripheral(_ peripheral: CBPeripheral, didWriteValueFor characteristic: CBCharacteristic, error: Error?) {
        delegate.onCharacteristicWrite(expand(uuidString: characteristic.uuid.uuidString).uppercased(), success: error == nil, value: characteristic.value)
    }
    @objc public func peripheral(_ peripheral: CBPeripheral, didUpdateValueFor descriptor: CBDescriptor, error: Error?) {
        delegate.onDescriptorRead(expand(uuidString: descriptor.uuid.uuidString).uppercased(), writingDeviceAddress: UNKNOWN_WRITING_DEV_ADDRESS, success: error == nil, value: descriptor.value as? Data)
    }
    @objc public func peripheral(_ peripheral: CBPeripheral, didWriteValueFor descriptor: CBDescriptor, error: Error?) {
        delegate.onDescriptorWrite(expand(uuidString: descriptor.uuid.uuidString).uppercased(), success: error == nil, value: descriptor.value as? Data)
    }
    
    // Check if all services chars and descs have been discovered
    // All chars must be discovered before the client can subscribe to notifications (this is solution on iOS and macOS)
    private func allDiscovered(){
        if charCallCount <= completedChar, incCallCount <= completedInc, descCallCount <= completedDesc{
            delegate.onServicesDiscovered()
        }
    }
}
