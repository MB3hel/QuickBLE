extends Node

class_name BLEClient

# BLEDelegate signals (client specific)
#warning-ignore:unused_signal
signal onDeviceDiscovered(address, name, rssi)
#warning-ignore:unused_signal
signal onConnectToDevice(address, name, success)
#warning-ignore:unused_signal
signal onDisconnectFromDevice(address, name)
#warning-ignore:unused_signal
signal onServicesDiscovered()
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

func scanForService(service: String, scanFor: bool = true):
	if(quickbleSingleton == null):
		return
	quickbleSingleton.clientScanForService(id, service, scanFor)

func checkBluetooth()->String:
	if(quickbleSingleton == null):
		return "UnsupportedPlatform"
	return helper.toBtError(quickbleSingleton.clientCheckBluetooth(id))

func requestEnableBt():
	if(quickbleSingleton == null):
		return
	quickbleSingleton.clientRequestEnableBt(id)

func scanForDevices()->String:
	if(quickbleSingleton == null):
		return "UnsupportedPlatform"
	return helper.toBtError(quickbleSingleton.clientScanForDevices(id))

func stopScanning():
	if(quickbleSingleton == null):
		return
	quickbleSingleton.clientStopScanning(id);

func connectToDevice(deviceAddress: String):
	if(quickbleSingleton == null):
		return
	quickbleSingleton.clientConnectToDevice(id, deviceAddress)

func disconnectFromDevice():
	if(quickbleSingleton == null):
		return
	quickbleSingleton.clientDisconnect(id)

func subscribeToCharacteristic(characteristic: String, subscribe: bool = true):
	if(quickbleSingleton == null):
		return
	quickbleSingleton.clientSubscribeToCharacteristic(id, characteristic, subscribe)

func readCharacteristic(characteristic: String):
	if(quickbleSingleton == null):
		return
	quickbleSingleton.clientReadCharacteristic(id, characteristic)

func writeCharacteristic(characteristic: String, data: PoolByteArray):
	if(quickbleSingleton == null):
		return
	quickbleSingleton.clientWriteCharacteristic(id, characteristic, data)

func readDescriptor(descriptor: String):
	if(quickbleSingleton == null):
		return
	quickbleSingleton.clientReadDescriptor(id, descriptor)

func writeDescriptor(descriptor: String, data: PoolByteArray):
	if(quickbleSingleton == null):
		return
	quickbleSingleton.clientWriteDescriptor(id, descriptor, data)

func hasService(service: String)->bool:
	if(quickbleSingleton == null):
		return false
	return quickbleSingleton.clientHasService(id, service)

func hasCharacteristic(characteristic: String)->bool:
	if(quickbleSingleton == null):
		return false
	return quickbleSingleton.clientHasCharacteristic(id, characteristic)

func hasDescriptor(descriptor: String)->bool:
	if(quickbleSingleton == null):
		return false
	return quickbleSingleton.clientHasDescriptor(id, descriptor)

func isScanning()->bool:
	if(quickbleSingleton == null):
		return false
	return quickbleSingleton.clientIsScanning(id)

func isConnected()->bool:
	if(quickbleSingleton == null):
		return false
	return quickbleSingleton.clientIsConnected(id)

func getServices()->Array:
	if(quickbleSingleton == null):
		return Array()
	return quickbleSingleton.clientGetServices(id)

func getCharacteristics()->Array:
	if(quickbleSingleton == null):
		return Array()
	return quickbleSingleton.clientGetCharacteristics(id)

func getDescriptors()->Array:
	if(quickbleSingleton == null):
		return Array()
	return quickbleSingleton.clientGetDescriptors(id)

func getScanServices()->Array:
	if(quickbleSingleton == null):
		return Array()
	return quickbleSingleton.clientGetScanServices(id)

func _init(clientId, quickbleSingleton, helper):
	id = clientId
	self.quickbleSingleton = quickbleSingleton
	self.helper = helper