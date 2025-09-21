extends Node

class_name BLEServer

# BLEDelegate signals (server specific)
#warning-ignore:unused_signal
signal onAdvertise(error)
#warning-ignore:unused_signal
signal onDeviceConnected(address, name)
#warning-ignore:unused_signal
signal onDeviceDisconnected(address, name)
#warning-ignore:unused_signal
signal onNotificationSent(characteristic, success)
# BLEDelegate signals (shared)
#warning-ignore:unused_signal
signal onCharacteristicRead(characteristic, writingDeviceAddress, success, value)
#warning-ignore:unused_signal
signal onCharacteristicWrite(characteristic, success, value)
#warning-ignore:unused_signal
signal onDescriptorRead(descriptor, writingDeviceAddress, success, value)
#warning-ignore:unused_signal
signal onDescriptorWrite(descriptor, success, value)
#warning-ignore:unused_signal
signal onBluetoothPowerChanged(enabled)
#warning-ignore:unused_signal
signal onBluetoothRequestResult(choseToEnable)

var id: int = -1
var helper = null
var quickbleSingleton = null    # The quickble singleton instance

func addService(service: String):
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return
	quickbleSingleton.serverAddService(id, service)
	
func addIncludedService(service: String, parentService: String)->bool:
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return false
	return quickbleSingleton.serverAddIncludedService(id, service, parentService)
	
func addCharacteristic(characteristic: String, parentService: String, properties: int = 26, permissions: int = 3)->bool:
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return false
	return quickbleSingleton.serverAddCharacteristic(id, characteristic, parentService, helper.fromCharProps(properties), helper.fromCharPerms(permissions))

func addDescriptor(descriptor: String, parentCharacteristic: String)->bool:
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return false
	if(helper.isAndroid()):
		return quickbleSingleton.serverAddDescriptor(id, descriptor, parentCharacteristic, 17)
	# iOS does not support setting descriptor permissions
	return quickbleSingleton.serverAddDescriptor(id, descriptor, parentCharacteristic)

func advertiseService(service: String, advertise: bool = true):
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return
	quickbleSingleton.serverAdvertiseService(id, service, advertise)

func clearGatt():
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return
	quickbleSingleton.serverClearGatt(id)
	
func checkBluetooth()->String:
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return "UnsupportedPlatform"
	return helper.toBtError(quickbleSingleton.serverCheckBluetooth(id))

func requestEnableBt():
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return
	quickbleSingleton.serverRequestEnableBt(id)

func startServer()->String:
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return "UnsupportedPlatform"
	return helper.toBtError(quickbleSingleton.serverStartServer(id))

func stopServer():
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return
	quickbleSingleton.serverStopServer(id)

func startAdvertising():
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return
	quickbleSingleton.serverStartAdvertising(id)

func stopAdvertising():
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return
	quickbleSingleton.serverStopAdvertising(id)

func notifyDevice(characteristic: String, deviceAddress: String):
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return
	quickbleSingleton.serverNotifyDevice(id, characteristic, deviceAddress)

func writeCharacteristic(characteristic: String, value: PoolByteArray, notify: bool = true):
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return
	quickbleSingleton.serverWriteCharacteristic(id, characteristic, value, notify)

func readCharacteristic(characteristic: String):
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return
	quickbleSingleton.serverReadCharacteristic(id, characteristic)

func writeDescritor(descriptor: String, value: PoolByteArray):
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return
	quickbleSingleton.serverWriteDescriptor(id, descriptor, value)

func readDescriptor(descriptor: String):
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return
	quickbleSingleton.serverReadDescriptor(id, descriptor)

func hasService(service: String)->bool:
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return false
	return quickbleSingleton.serverHasService(id, service)

func hasCharacteristic(characteristic: String)->bool:
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return false
	return quickbleSingleton.serverHasCharacteristic(id, characteristic)

func hasDescriptor(descriptor: String)->bool:
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return false
	return quickbleSingleton.serverHasDescriptor(id, descriptor)

func isRunning()->bool:
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return false
	return quickbleSingleton.serverIsRunning(id)

func isAdvertising()->bool:
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return false
	return quickbleSingleton.serverIsAdvertising(id)

func getServices()->Array:
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return Array()
	return quickbleSingleton.serverGetServices(id)

func getCharacteristics()->Array:
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return Array()
	return quickbleSingleton.serverGetCharacteristics(id)
	
func getDescriptors()->Array:
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return Array()
	return quickbleSingleton.serverGetDescriptors(id)
	
func getAdvertiseServices()->Array:
	if(quickbleSingleton == null or (helper.isAndroid() and helper.getAndroidVersion() < 21)):
		return Array()
	return quickbleSingleton.serverGetAdvertiseServices(id)

func _init(serverId, quickbleSingleton, helper):
	id = serverId
	self.quickbleSingleton = quickbleSingleton
	self.helper = helper
