extends Node

class_name QuickBLE

# Note: iOS may also work as a singleton (not cretain, but it looks that way)
#       See demo https://github.com/Shin-NiL/Godot-Share
#                https://github.com/kloder-games/godot-admob

var quickbleSingleton = null

var godotObjects = {}

func getAndroidVersion():
    var out = []
    OS.execute("getprop", ["ro.build.version.sdk"], true, out)
    return int(out[0])

func isAndroid():
	return OS.get_name() == "Android"

func isIOS():
	return OS.get_name() == "iOS"

####################################################
# QuickBLE values and Helper functions
####################################################

enum CharProperties { Broadcast = 1, Read = 2, WriteNoResponse = 4, Write = 8, Notify = 16, Indicate = 32, SignedWrite = 64, Extended = 128 }
enum CharPermissions { Read = 1, Write = 2, ReadEncrypted = 4, WriteEncrypted = 8 }

const BtError = {
	None = "None",
	NoBluetooth = "NoBluetooth",
	NoBLE = "NoBLE",
	Disabled = "Disabled",
	NoServer = "NoServer",
	AlreadyRunning = "AlreadyRunning",
	Unknown = "Unknown",
	UnsupportedPlatform = "UnsupportedPlatform",
}

const AdvertiseError = {
	None = "None",
	DataTooLarge = "DataTooLarge",
	TooManyAdvertisers = "TooManyAdvertisers",
	AlreadyStarted = "AlreadyStarted",
	Unknown = "Unknown"
}

#enum BtError { None, NoBluetooth, NoBLE, Disabled, NoServer, AlreadyRunning, Unknown, UnsupporetedPlatform }
#enum AdvertiseError { None, DataTooLarge, TooManyAdvertisers, AlreadyStarted, Unknown }

func fromCharProps(props):
	# Same on iOS and Android
	var rtn = 0
	if(props & CharProperties.Broadcast == CharProperties.Broadcast):
		rtn = rtn | 1
	if(props & CharProperties.Read == CharProperties.Read):
		rtn = rtn | 2
	if(props & CharProperties.WriteNoResponse == CharProperties.WriteNoResponse):
		rtn = rtn | 4
	if(props & CharProperties.Write == CharProperties.Write):
		rtn = rtn | 8
	if(props & CharProperties.Notify == CharProperties.Notify):
		rtn = rtn | 16
	if(props & CharProperties.Indicate == CharProperties.Indicate):
		rtn = rtn | 32
	if(props & CharProperties.SignedWrite == CharProperties.SignedWrite):
		rtn = rtn | 64
	if(props & CharProperties.Extended == CharProperties.Extended):
		rtn = rtn | 128
	return rtn

func fromCharPerms(perms):
	if(isAndroid()):
		var rtn = 0
		if(perms & CharPermissions.Read == CharPermissions.Read):
			rtn = rtn | 1
		if(perms & CharPermissions.ReadEncrypted == CharPermissions.ReadEncrypted):
			rtn = rtn | 2
		if(perms & CharPermissions.Write == CharPermissions.Write):
			rtn = rtn | 16
		if(perms & CharPermissions.WriteEncrypted == CharPermissions.WriteEncrypted):
			rtn = rtn | 32
		return rtn
	elif(isIOS()):
		var rtn = 0
		if(perms & CharPermissions.Read == CharPermissions.Read):
			rtn = rtn | 1
		if(perms & CharPermissions.ReadEncrypted == CharPermissions.ReadEncrypted):
			rtn = rtn | 2
		if(perms & CharPermissions.Write == CharPermissions.Write):
			rtn = rtn | 4
		if(perms & CharPermissions.WriteEncrypted == CharPermissions.WriteEncrypted):
			rtn = rtn | 8
		return rtn
	return 0

func toBtError(ival: int):
	if(isAndroid()):
		match ival:
			0:
				return BtError.None
			1:
				return BtError.NoBluetooth
			2:
				return BtError.NoBLE
			3:
				return BtError.Disabled
			4: 
				return BtError.NoServer
			5:
				return BtError.AlreadyRunning
	elif(isIOS()):
		match ival:
			0:
				return BtError.Unknown
			1:
				return BtError.Unknown
			2:
				return BtError.NoBluetooth
			3:
				return BtError.NoServer
			4: 
				return BtError.Disabled
			5:
				return BtError.None
			6:
				return BtError.AlreadyRunning
	return BtError.Unknown

func toAdvertiseError(ival: int):
	if(isAndroid()):
		match ival:
			0:
				return AdvertiseError.None
			1:
				return AdvertiseError.DataTooLarge
			2:
				return AdvertiseError.TooManyAdvertisers
			3:
				return AdvertiseError.AlreadyStarted
	elif(isIOS()):
		match ival:
			0:
				return AdvertiseError.None
			4:
				return AdvertiseError.DataTooLarge
			11:
				return AdvertiseError.TooManyAdvertisers
			9:
				return AdvertiseError.AlreadyStarted
	return AdvertiseError.Unknown

####################################################
# Server functions
####################################################

func createServer()->BLEServer:
	if(quickbleSingleton == null or (isAndroid() and getAndroidVersion() < 21)):
		return BLEServer.new(-1, null, self)
	var serverId = quickbleSingleton.createServer()
	var server = BLEServer.new(serverId, quickbleSingleton, self)
	godotObjects[serverId] = server
	return server

func destoryServer(server):
	if(quickbleSingleton == null or (isAndroid() and getAndroidVersion() < 21)):
		return
	quickbleSingleton.destroyServer(server.id);
	godotObjects.erase(server.id)

func connectServerDelegate(server: BLEServer, delegate):
	"""Connect the server signals to methods with same names as BLEDelegate functions on a given delegate object."""
	server.connect("onAdvertise", delegate, "onAdvertise")
	server.connect("onDeviceConnected", delegate, "onDeviceConnected")
	server.connect("onDeviceDisconnected", delegate, "onDeviceDisconnected")
	server.connect("onNotificationSent", delegate, "onNotificationSent")
	server.connect("onCharacteristicRead", delegate, "onCharacteristicRead")
	server.connect("onCharacteristicWrite", delegate, "onCharacteristicWrite")
	server.connect("onDescriptorRead", delegate, "onDescriptorRead")
	server.connect("onDescriptorWrite", delegate, "onDescriptorWrite")
	server.connect("onBluetoothPowerChanged", delegate, "onBluetoothPowerChanged")
	server.connect("onBluetoothRequestResult", delegate, "onBluetoothRequestResult")
	
####################################################
# Client functions
####################################################

func createClient()->BLEClient:
	if(quickbleSingleton == null):
		return BLEClient.new(-1, null, self)
	var clientId = quickbleSingleton.createClient()
	var client = BLEClient.new(clientId, quickbleSingleton, self)
	godotObjects[clientId] = client
	return client

func destroyClient(client):
	if(quickbleSingleton == null):
		return
	quickbleSingleton.destoryClient(client.id);
	godotObjects.erase(client.id)

func connectClientDelegate(client: BLEClient, delegate):
	"""Connect the client signals to methods with same names as BLEDelegate functions on a given delegate object."""
	client.connect("onDeviceDiscovered", delegate, "onDeviceDiscovered")
	client.connect("onConnectToDevice", delegate, "onConnectToDevice")
	client.connect("onDisconnectFromDevice", delegate, "onDisconnectFromDevice")
	client.connect("onServicesDiscovered", delegate, "onServicesDiscovered")
	client.connect("onCharacteristicRead", delegate, "onCharacteristicRead")
	client.connect("onCharacteristicWrite", delegate, "onCharacteristicWrite")
	client.connect("onDescriptorRead", delegate, "onDescriptorRead")
	client.connect("onDescriptorWrite", delegate, "onDescriptorWrite")
	client.connect("onBluetoothPowerChanged", delegate, "onBluetoothPowerChanged")
	client.connect("onBluetoothRequestResult", delegate, "onBluetoothRequestResult")

####################################################
# Setup functions
####################################################

func _init():
	if (isAndroid() or isIOS()) and Engine.has_singleton("QuickBLESingleton"):
		quickbleSingleton = Engine.get_singleton("QuickBLESingleton")
		quickbleSingleton.assignInstanceId(get_instance_id())

func _ready():
	pass

func hasLocationPermission()->bool:
	if(isAndroid()):
		return quickbleSingleton.hasLocationPermission()
	return true

func requestLocationPermission():
	if(isAndroid()):
		quickbleSingleton.requestLocationPerission()

func _locationPermissionResult(granted):
	pass

####################################################
# BLEDelegate
####################################################

# BLEDelegate callback functions. Emit signals on object matching given id.
func _onAdvertise(id, error):
	var obj = godotObjects[id]
	if(obj == null):
		print("Unknown server/client id in callback")
		return
	obj.emit_signal("onAdvertise", toAdvertiseError(error))

func _onDeviceConnected(id, address, name):
	var obj = godotObjects[id]
	if(obj == null):
		print("Unknown server/client id in callback")
		return
	obj.emit_signal("onDeviceConnected", address, name)

func _onDeviceDisconnected(id, address, name):
	var obj = godotObjects[id]
	if(obj == null):
		print("Unknown server/client id in callback")
		return
	obj.emit_signal("onDeviceDisconnected", address, name)

func _onNotificationSent(id, characteristic, success):
	var obj = godotObjects[id]
	if(obj == null):
		print("Unknown server/client id in callback")
		return
	obj.emit_signal("onNotificationSent", characteristic, success)

func _onDeviceDiscovered(id, address, name, rssi):
	var obj = godotObjects[id]
	if(obj == null):
		print("Unknown server/client id in callback")
		return
	obj.emit_signal("onDeviceDiscovered", address, name, rssi)

func _onConnectToDevice(id, address, name, success):
	var obj = godotObjects[id]
	if(obj == null):
		print("Unknown server/client id in callback")
		return
	obj.emit_signal("onConnectToDevice", address, name, success)

func _onDisconnectFromDevice(id, address, name):
	var obj = godotObjects[id]
	if(obj == null):
		print("Unknown server/client id in callback")
		return
	obj.emit_signal("onDisconnectFromDevice", address, name)

func _onServicesDiscovered(id):
	var obj = godotObjects[id]
	if(obj == null):
		print("Unknown server/client id in callback")
		return
	obj.emit_signal("onServicesDiscovered")

func _onCharacteristicRead(id, characteristic, writingDeviceAddress, success, value):
	var obj = godotObjects[id]
	if(obj == null):
		print("Unknown server/client id in callback")
		return
	obj.emit_signal("onCharacteristicRead", characteristic, writingDeviceAddress, success, value)

func _onCharacteristicWrite(id, characteristic, success, value):
	var obj = godotObjects[id]
	if(obj == null):
		print("Unknown server/client id in callback")
		return
	obj.emit_signal("onCharacteristicWrite", characteristic, success, value)

func _onDescriptorRead(id, descriptor, writingDeviceAddress, success, value):
	var obj = godotObjects[id]
	if(obj == null):
		print("Unknown server/client id in callback")
		return
	obj.emit_signal("onDescriptorRead", descriptor, writingDeviceAddress, success, value)

func _onDescriptorWrite(id, descriptor, success, value):
	var obj = godotObjects[id]
	if(obj == null):
		print("Unknown server/client id in callback")
		return
	obj.emit_signal("onDescriptorWrite", descriptor, success, value)

func _onBluetoothPowerChanged(id, enabled):
	var obj = godotObjects[id]
	if(obj == null):
		print("Unknown server/client id in callback")
		return
	obj.emit_signal("onBluetoothPowerChanged", enabled)

func _onBluetoothRequestResult(id, choseToEnable):
	var obj = godotObjects[id]
	if(obj == null):
		print("Unknown server/client id in callback")
		return
	obj.emit_signal("onBluetoothRequestResult", choseToEnable)
