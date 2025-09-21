import Foundation
#if os(iOS)
    import UIKit
#elseif os(macOS)
    import Cocoa
#endif
import CoreBluetooth

@objc public class DelayedNotification: NSObject{
    private(set) var valueToNotify: Data
    private(set) var characteristic: BLECharacteristic
    private(set) var devicesToNotify: [CBCentral]?
    
    @objc public init(_ data: Data, _ char: BLECharacteristic, _ devsNotify: [CBCentral]?){
        valueToNotify = data
        characteristic = char
        devicesToNotify = devsNotify
    }
}

@objc public class BLEServer : NSObject, CBPeripheralManagerDelegate{
    
    //MARK: Properties and Variables
    
    private let notificationLock = DispatchSemaphore(value: 1)
    
    private let UNKNOWN_WRITING_DEV_ADDRESS = "unknown"
    
    // Platform Specific Objects
    private var delegate : BLEDelegate!;
    private var serviceObjects : [CBMutableService] = [];
    private var characteristicObjects : [BLECharacteristic] = [];
    private var descriptorObjects : [BLEDescriptor] = [];
    // UUIDs for Gatt Objects
    private(set) var services: [String] = []
    private(set) var characteristics: [String] = []
    private(set) var descriptors: [String] = []
    private(set) var advertiseServices : [String] = [];
    
    // Platform specific options
    var advertiseDeviceName = true;
    var advertiseMode = AdvertiseMode.Balanced
    var notifyChangingDevice = false
    var readInternalWrites = false
    
    // Status
    @objc private(set) var isRunning = false
    @objc private(set) var isAdvertising = false
    
    // Enable BT Dialog Text
    var REQUEST_BT_TITLE = "Bluetooth Required"
    var REQUEST_BT_MESSAGE = "Please enable bluetooth in Settings."
    var REQUEST_BT_CONFIRM = "Settings"
    var REQUEST_BT_DENY = "Cancel"
    
    // Buffers for notifications that failed to queue
    private var delayedNotoificationBuffer: [DelayedNotification] = []
    
    
    // Objective-C Mess:
    @objc public func getIsRunning() -> Bool{
        return isRunning;
    }
    @objc public func getIsAdvertising() -> Bool{
        return isAdvertising;
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
    @objc public func getAdvertiseServices() -> [String]{
        return advertiseServices;
    }
    @objc public func setAdvertiseDeviceName(_ advertise: Bool){
        advertiseDeviceName = advertise;
    }
    @objc public func getAdvertiseDeviceName() -> Bool{
        return advertiseDeviceName;
    }
    @objc public func setAdvertiseMode(_ mode: Int){
        advertiseMode = mode;
    }
    @objc public func getAdvertiseMode() -> Int{
        return advertiseMode;
    }
    @objc public func setNotifyChangingDevice(_ notify: Bool){
        notifyChangingDevice = notify;
    }
    @objc public func getNotifyChangingDevice() -> Bool{
        return notifyChangingDevice;
    }
    @objc public func setReadInternalWrites(_ read: Bool){
        readInternalWrites = read;
    }
    @objc public func getReadInternalWrites() -> Bool{
        return readInternalWrites;
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
    
    //Apple Specific Bluetooth Objects
    private var peripheralManager : CBPeripheralManager!;
    private var connectedDevices : [CBCentral] = [];
    
    //MARK: Constructor
    @objc public init(_ delegate : BLEDelegate){
        super.init();
        self.delegate = delegate;
        self.peripheralManager = CBPeripheralManager(delegate: self, queue: nil, options: [CBPeripheralManagerOptionShowPowerAlertKey: false])
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
    
    //MARK: Server Config
    
    /**
     Add a primary service to the server
     - parameter service: The UUID of the service
    */
    @objc public func addService(_ service: String){
        if(!services.contains(service.uppercased())){
            let s = CBMutableService(type: CBUUID(string: service), primary: true)
            services.append(service.uppercased())
            serviceObjects.append(s)
        }
    }
    /**
     Add an included (secondary) service to a service
     - parameter service: The UUID of the included service
     - parameter parentService: The UUID of the parent(primary) service
     - returns: Was the service successfully added
    */
    @objc public func addIncludedService(_ service: String, parentService: String) -> Bool{
        if(!services.contains(service.uppercased()) && services.contains(parentService.uppercased())){
            let parent = getService(uuid: CBUUID(string: parentService))
            if(parent == nil){
                return false
            }
            let s = CBMutableService(type: CBUUID(string: service), primary: false)
            parent?.includedServices?.append(s)
            services.append(service.uppercased())
            serviceObjects.append(s)
            return true
        }
        return false
    }
    /**
     Add a characteristic to a service
     - parameter characteristic: The UUID of the characteristic
     - parameter parentService: The UUID of the parent serviec
     - parameter properties: The properties of the characteristic (CharProperties)
     - parameter permissions: The permissions of the characteristic (CharPermissions)
     - returns: Was the characteristic successfully added
    */
    @objc public func addCharacteristic(_ characteristic: String, parentService: String, properties: Int = CharProperties.Read | CharProperties.Write | CharProperties.Notify, permissions: Int = CharPermissions.Read | CharPermissions.Write) -> Bool{
        if(!characteristics.contains(characteristic.uppercased()) && services.contains(parentService.uppercased())){
            let parent = getService(uuid: CBUUID(string: parentService))
            if(parent == nil){
                return false
            }
            let c = BLECharacteristic(type: CBUUID(string: characteristic), properties: getProperties(properties), value: nil, permissions: getPermissions(permissions))
            if(parent?.characteristics == nil){
                parent?.characteristics = [c]
            }else{
                parent?.characteristics!.append(c)
            }
            characteristics.append(characteristic.uppercased())
            characteristicObjects.append(c)
            return true
        }
        return false
    }
    /**
     Add a descriptor to a characteristic
     - parameter descriptor: The UUID of the descriptor
     - parameter parentCharacteristic: The UUID of the parent characteristic
     - return: Was the descriptor successfully added
    */
    @objc public func addDescriptor(_ descriptor: String, parentCharacteristic: String) -> Bool{
        if(!descriptors.contains(descriptor.uppercased()) && characteristics.contains(parentCharacteristic.uppercased())){
            let parent = getCharacteristic(uuid: CBUUID(string: parentCharacteristic))
            if(parent == nil){
                return false
            }
            let d = BLEDescriptor(type: CBUUID(string: descriptor), value: nil)
            if(parent?.descriptors == nil){
                parent?.descriptors = [d]
            }else{
                parent?.descriptors!.append(d)
            }
            descriptors.append(descriptor.uppercased())
            descriptorObjects.append(d)
            return true
        }
        return false
    }
    
    /**
     Advertise a service's UUID in the advertisement packet
     - parameter service: The service to advertise
     - parameter advertise: Whether or not to advertise the UUID
    */
    @objc public func advertiseService(_ service: String, advertise: Bool){
        if(advertise && !advertiseServices.contains(service.uppercased())){
            advertiseServices.append(service.uppercased())
        }
        if(!advertise && advertiseServices.contains(service.uppercased())){
            advertiseServices.remove(at: advertiseServices.index(of: service.uppercased())!)
        }
    }
    
    /**
     Remove all services, characteristics, and descriptors from the server
    */
    @objc public func clearGatt(){
        stopServer()
        services.removeAll()
        characteristics.removeAll()
        descriptors.removeAll()
        advertiseServices.removeAll()
        serviceObjects.removeAll()
        characteristicObjects.removeAll()
        descriptorObjects.removeAll()
    }
    
    /**
     Check bluetooth low energy server compatibility for the device
    */
    @objc public func checkBluetooth() -> Int{
        return peripheralManager.state.rawValue;
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
     - parameter vc: The current NSViewController
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
     Start the server and start advertising
     - returns: An error code (BtError)
    */
    @objc public func startServer() -> Int{
        if(!isRunning){
            let error = checkBluetooth();
            if(error != BtError.None){
                return error;
            }
            for service in serviceObjects{
                peripheralManager.add(service)
            }
            if(isAdvertising){
                peripheralManager.stopAdvertising()
            }
            peripheralManager.startAdvertising(buildAdvertiseData())
            isRunning = true;
            isAdvertising = true
            return BtError.None;
            
        }else{
            return BtError.AlreadyRunning;
        }
    }
    /**
     Stop the server and stop advertising
     */
    @objc public func stopServer(){
        if(isRunning){
            if(isAdvertising){
                peripheralManager.stopAdvertising();
            }
            
            peripheralManager.removeAllServices()
            isRunning = false;
            isAdvertising = false
        }
    }
    /**
     Stop advertising if the server is running
    */
    @objc public func stopAdvertising(){
        if(isAdvertising){
            peripheralManager.stopAdvertising()
            isAdvertising = false
        }
    }
    /**
     Start advertising if the server is running
    */
    @objc public func startAdvertising(){
        if(!isAdvertising){
            peripheralManager.startAdvertising(buildAdvertiseData());
            isAdvertising = true
        }
    }
    
    /**
     Send a notification of a characteristic's value to a certain device
     - parameter characteristic: The characteristic to notify the value of
     - parameter deviceAddress: The address of the device to send the notification to
     */
    @objc public func notifyDevice(_ characteristic: String, deviceAddress: String){
        DispatchQueue.global(qos: .background).async {
            guard let char = self.getCharacteristic(uuid: CBUUID(string: characteristic)) else{
                self.delegate.onNotificationSent(characteristic, success: false)
                return
            }
            let dev = self.connectedDevices.first(where: {$0.identifier.uuidString.uppercased() == deviceAddress.uppercased()})
            if(dev == nil){
                self.delegate.onNotificationSent(characteristic, success: false)
                return
            }
            
            var valueToSend: Data
            if(char.dynamicValue == nil){
                valueToSend = Data()
            }else{
                valueToSend = Data(char.dynamicValue!)
            }
            
            self.notificationLock.wait()
            self.delayedNotoificationBuffer.append(DelayedNotification(valueToSend, char, [dev!]))
            self.notificationLock.signal()
            
            self.processNotifications()
        }
    }
    /**
     Notify devices subscribed to a characteristic
     - parameter characteristic: The characteristic to notify the value of
     - parameter device: The device that just changed the characteristic
    */
    private func notifyDevices(_ characteristic: BLECharacteristic, device: CBCentral?){
        DispatchQueue.global(qos: .background).async {
            var devicesToNotify: [CBCentral]? = []
            if(device == nil || self.notifyChangingDevice){
                devicesToNotify = nil
            }else{
                for d in self.connectedDevices{
                    if(d != device){
                        devicesToNotify!.append(d)
                    }
                }
            }
            
            var valueToSend: Data
            if(characteristic.dynamicValue == nil){
                valueToSend = Data()
            }else{
                let uint8Array = characteristic.dynamicValue!.map { $0 }
                valueToSend = Data(bytes: uint8Array)
            }
            
            self.notificationLock.wait()
            self.delayedNotoificationBuffer.append(DelayedNotification(valueToSend, characteristic, devicesToNotify))
            self.notificationLock.signal()
            
            self.processNotifications()
        }
    }
    
    private func processNotifications(){
        DispatchQueue.global(qos: .background).async{
            self.notificationLock.wait()
            
            for notification in self.delayedNotoificationBuffer{
                let res = self.peripheralManager.updateValue(Data(notification.valueToNotify), for: notification.characteristic, onSubscribedCentrals: notification.devicesToNotify)
                if(res){
                    NSLog("Sent: " + String(data: notification.valueToNotify, encoding: .utf8)!)
                    self.delayedNotoificationBuffer.remove(at: self.delayedNotoificationBuffer.index(of: notification)!)
                }
            }
            
            self.notificationLock.signal()
        }
    }
    
    /**                    self.delegate.onNotificationSent(notification.characteristic.uuid.uuidString.uppercased(), success: true)

     Build advertise data based on options
    */
    private func buildAdvertiseData() -> [String: Any]{
        var serviceUuids : [CBUUID] = [];
        for service in advertiseServices{
            serviceUuids.append(CBUUID(string:service))
        }
        var name = ""
        #if os(iOS)
            name = (advertiseDeviceName) ? UIDevice.current.name : ""
        #elseif os(macOS)
            name = (advertiseDeviceName) ? Host.current().localizedName ?? "" : ""
        #endif
        return [CBAdvertisementDataLocalNameKey:  name,
                CBAdvertisementDataServiceUUIDsKey: serviceUuids];
    }
    /**
     Get Characteristic properties object from int
     - parameter properties: The int (CharProperties) properties
     - returns: The CBCharacteristicProperties object
    */
    private func getProperties(_ properties: Int) -> CBCharacteristicProperties{
        var props: CBCharacteristicProperties = []
        if(properties & CharProperties.Broadcast == CharProperties.Broadcast){
            props.insert(.broadcast)
        }
        if(properties & CharProperties.Read == CharProperties.Read){
            props.insert(.read)
        }
        if(properties & CharProperties.WriteNoResponse == CharProperties.WriteNoResponse){
            props.insert(.writeWithoutResponse)
        }
        if(properties & CharProperties.Write == CharProperties.Write){
            props.insert(.write)
        }
        if(properties & CharProperties.Notify == CharProperties.Notify){
            props.insert(.notify)
        }
        if(properties & CharProperties.Indicate == CharProperties.Indicate){
            props.insert(.indicate)
        }
        if(properties & CharProperties.SignedWrite == CharProperties.SignedWrite){
            props.insert(.authenticatedSignedWrites)
        }
        if(properties & CharProperties.ExtendedProps == CharProperties.ExtendedProps){
            props.insert(.extendedProperties)
        }
        return props
    }
    /**
     Get Characteristic permissions object from int
     - parameter permissions: The int (CharPermissions) permissions
     - returns: The CBAttributePermissions object
     */
    private func getPermissions(_ permissions: Int) -> CBAttributePermissions{
        var perms: CBAttributePermissions = []
        if(permissions & CharPermissions.Read == CharPermissions.Read){
            perms.insert(.readable)
        }
        if(permissions & CharPermissions.Write == CharPermissions.Write){
            perms.insert(.writeable)
        }
        if(permissions & CharPermissions.WriteEncrypted == CharPermissions.WriteEncrypted){
            perms.insert(.writeEncryptionRequired)
        }
        if(permissions & CharPermissions.ReadEncrypted == CharPermissions.ReadEncrypted){
            perms.insert(.readEncryptionRequired)
        }
        return perms;
    }
    /**
     Get the latency based on the advertiseMode setting
     - returns: CBPeripheralManagerConnectionLatency object
     */
    private func getLatency() -> CBPeripheralManagerConnectionLatency{
        switch(advertiseMode){
        case AdvertiseMode.LowLatency:
            return .low
        case AdvertiseMode.Balanced:
            return .medium
        case AdvertiseMode.LowPower:
            return .high
        default:
            return .medium
        }
    }
    
    //MARK: Characteristics and Descriptors
    
    /**
     Write a value to a characteristic
     - parameter characteristic: The characteristic to write
     - parameter data: The value to write (as NSData)
     - parameter notify: Whether or not to notify subscribed devices
    */
    @objc public func writeCharacteristic(_ characteristic: String, data: Data?, notify: Bool = true){
        guard let char = getCharacteristic(uuid: CBUUID(string: characteristic)) else{
            delegate.onCharacteristicWrite(characteristic.uppercased(), success: false, value: nil)
            return
        }
        char.dynamicValue = data;
        if notify{
            notifyDevices(char, device: nil);
        }
        delegate.onCharacteristicWrite(characteristic.uppercased(), success: true, value: data);
        if(readInternalWrites){
            delegate.onCharacteristicRead(characteristic.uppercased(), writingDeviceAddress: UNKNOWN_WRITING_DEV_ADDRESS, success: true, value: data)
        }
    }
    /**
     Read a value from a characteristic
     - parameter characteristic: The characteristic to read
    */
    @objc public func readCharacteristic(_ characteristic: String){
        guard let char = getCharacteristic(uuid: CBUUID(string: characteristic)) else{
            delegate.onCharacteristicRead(characteristic.uppercased(), writingDeviceAddress: UNKNOWN_WRITING_DEV_ADDRESS, success: false, value: nil)
            return
        }
        delegate.onCharacteristicRead(characteristic.uppercased(), writingDeviceAddress: UNKNOWN_WRITING_DEV_ADDRESS, success: true, value: char.dynamicValue)
    }
    /**
     Write a value to a descriptor
     - parameter descriptor: The descriptor to write
     - parameter data: The value to write (as Data)
    */
    @objc public func writeDescriptor(_ descriptor: String, data: Data?){
        guard let desc = getDescriptor(uuid: CBUUID(string: descriptor)) else{
            delegate.onDescriptorWrite(descriptor.uppercased(), success: false, value: nil)
            return
        }
        desc.dynamicValue = data
        delegate.onDescriptorWrite(descriptor.uppercased(), success: true, value: data)
        if(readInternalWrites){
            delegate.onDescriptorRead(descriptor.uppercased(), writingDeviceAddress: UNKNOWN_WRITING_DEV_ADDRESS, success: true, value: data)
        }
    }
    /**
     Read a value from a descriptor
     - parameter descriptor: The descriptor to read
    */
    @objc public func readDescriptor(_ descriptor: String){
        guard let desc = getDescriptor(uuid: CBUUID(string: descriptor)) else{
            delegate.onDescriptorRead(descriptor.uppercased(), writingDeviceAddress: UNKNOWN_WRITING_DEV_ADDRESS, success: false, value: nil)
            return
        }
        delegate.onDescriptorRead(descriptor.uppercased(), writingDeviceAddress: UNKNOWN_WRITING_DEV_ADDRESS, success: true, value: desc.dynamicValue as? Data)
    }
    /**
     Get a service from a UUID
     -parameter uuid: The uuid of the service
     - returns: The CBService or nil
     */
    private func getService(uuid: CBUUID) -> CBMutableService?{
        return serviceObjects.first(where: { $0.uuid.isEqual(uuid) })
    }
    /**
     Get a characteristic from a UUID
     -parameter uuid: The uuid of the characteristic
     - returns: The CBCharacteristic or nil
     */
    private func getCharacteristic(uuid: CBUUID) -> BLECharacteristic?{
        return characteristicObjects.first(where: { $0.uuid.isEqual(uuid) })
    }
    /**
     Get a descriptor from a UUID
     -parameter uuid: The uuid of the descriptor
     - returns: The CBDescriptor or nil
     */
    private func getDescriptor(uuid: CBUUID) -> BLEDescriptor?{
        return descriptorObjects.first(where: { $0.uuid.isEqual(uuid) })
    }
    
    /**
     Check if the server has a service
     - parameter service: The service to search for
     - returns: Whether or not the server has the service
    */
    @objc public func hasService(_ service: String) -> Bool{
        return services.contains(service.uppercased())
    }
    /**
     Check if the server has a characteristic
     - parameter characteristic: The service to search for
     - returns: Whether or not the server has the characteristic
     */
    @objc public func hasCharacteristic(_ characteristic: String) -> Bool{
        return characteristics.contains(characteristic.uppercased())
    }
    /**
     Check if the server has a descriptor
     - parameter descriptor: The descriptor to search for
     - returns: Whether or not the server has the descriptor
     */
    @objc public func hasDescriptor(_ descriptor: String) -> Bool{
        return descriptors.contains(descriptor.uppercased())
    }
    
    //MARK: CBPeripheralManagerDelegate
    @objc public func peripheralManagerDidStartAdvertising(_ peripheral: CBPeripheralManager, error: Error?) {
        var e = AdvertiseError.None
        if(error != nil){
            switch((error! as NSError).code){
            case AdvertiseError.AlreadyStarted:
                e = AdvertiseError.AlreadyStarted
            case AdvertiseError.DataTooLarge:
                e = AdvertiseError.DataTooLarge
            case AdvertiseError.TooManyAdvertisers:
                e = AdvertiseError.TooManyAdvertisers
            default:
                e = AdvertiseError.InternalError
            }
        }
        delegate.onAdvertise(error: e);
    }
    @objc public func peripheralManagerDidUpdateState(_ peripheral: CBPeripheralManager) {
        let result = peripheral.state.rawValue == BtError.None;
        if (!result){
            stopServer()
        }
        delegate.onBluetoothPowerChange(result)
    }
    @objc public func peripheralManager(_ peripheral: CBPeripheralManager, didReceiveRead request: CBATTRequest) {
        addDevice(request.central);
        guard let char = getCharacteristic(uuid: request.characteristic.uuid) else{
            self.peripheralManager.respond(to: request, withResult: .attributeNotFound)
            return
        }
        request.value = char.dynamicValue;
        self.peripheralManager.respond(to: request, withResult: .success);
    }
    @objc public func peripheralManager(_ peripheral: CBPeripheralManager, didReceiveWrite requests: [CBATTRequest]) {
        for request in requests{
            addDevice(request.central);
            guard let char = getCharacteristic(uuid: request.characteristic.uuid) else{
                self.peripheralManager.respond(to: request, withResult: .attributeNotFound);
                return;
            }
            char.dynamicValue = request.value
            self.peripheralManager.respond(to: request, withResult: .success)
            delegate.onCharacteristicRead(expand(uuidString: char.uuid.uuidString).uppercased(), writingDeviceAddress: request.central.identifier.uuidString.uppercased(), success: true, value: char.dynamicValue)
            notifyDevices(char, device: request.central)
        }
    }
    @objc public func peripheralManager(_ peripheral: CBPeripheralManager, central: CBCentral, didSubscribeTo characteristic: CBCharacteristic) {
        addDevice(central);
    }
    @objc public func peripheralManager(_ peripheral: CBPeripheralManager, central: CBCentral, didUnsubscribeFrom characteristic: CBCharacteristic) {
        removeDevice(central);
    }
    @objc public func peripheralManagerIsReady(toUpdateSubscribers peripheral: CBPeripheralManager) {
        processNotifications()
    }
    
    // Device List management for connect/disconnect
    private func removeDevice(_ device : CBCentral){
        if(connectedDevices.contains(device)){
            delegate.onDeviceDisconnected(address: device.identifier.uuidString.uppercased(), name: nil)
            connectedDevices.remove(at: connectedDevices.index(of: device)!)
        }
    }
    private func addDevice(_ device: CBCentral){
        if(!connectedDevices.contains(device)){
            connectedDevices.append(device);
            peripheralManager.setDesiredConnectionLatency(getLatency(), for: device)
            delegate.onDeviceConnected(address: device.identifier.uuidString.uppercased(), name: nil)
        }
    }
    
}
