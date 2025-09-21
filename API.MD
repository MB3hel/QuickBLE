# API Documentation
**NOTE:** This is a generic API guide. The exact implementation may vary by platform.
Most platforms have some options and settings specific to the platform (see the platform specific nuances section below).
## QuickBLE Server (Virtual Peripheral):
The Bluetooth Low Energy Server (BLEServer) creates a GATT server. This feature requires both hardware and software support (see table below).

##### System Requirements

| Platform | Requirements |
|--------|--------|
| Android | Android 5 (API 21) or newer and compatible hardware       |
| iOS / macOS | A device running iOS 9 or newer or a device running macOS 10.13 or newer.
| UWP (Windows 10) | A phone, tablet, or PC running WIndows 10 Version 1607 (Creators Update) or newer and compatible hardware.

##### Properties (These do not include platform specific functionality)
| Property | What it is |
|--------|--------|
| Services | A list of uuids for each service on the server.|
| Characteristics |  A list of uuids for each characteristic on the server. |
| Descriptors |  A list of uuids for each descriptor on the server. |
| AdvertiseServices |  A list of uuids for each service the server is advertising. |
| IsRunning | Is the server running. |
| IsAdvertising | Is the server being advertised. |
| NotifyChangingDevice | Should the device that wrote to a characteristic recieve a notification when it's value changed. |
| ReadInternalWrites | Should the read method be fired when the local server writes to a characteristic or descriptor. |
| REQUEST_BT_TITLE | The title used for the RequestEnableBt dialog. |
| REQUEST_BT_MESSAGE | The message used for the RequestEnableBt dialog. |
| REQUEST_BT_CONFIRM | The text used for the confirm button of theRequestEnableBt dialog. |
| REQUEST_BT_DENY | The text used for the deny button of theRequestEnableBt dialog. |

##### Functions
| Function | What it does |
|--------|--------|
| AddService(uuid) | Adds a service with the specified uuid to the server. |
| AddIncludedService(uuid, parentService)| Adds an included service to a service on the server. |
| AddCharacteristic(uuid, parentService[, properties[, permissions]]) | Adds a characteristic to a service on the server. |
| AddDescriptor(uuid, parentCharacteristic) | Adds a descriptor to a characteristic on the server. |
| AdvertiseService(uuid[, advertise]) | Adds or removes a service uuid from the advertisement packet. |
| ClearGatt() | Removes all services, characteristics, and descriptors from the server. |
| NotifyDevice(characteristic, deviceId) | Sends a notification of a characteristic's value to a single client (the client must be subscribed to the characteristic to recieve the notification). |
| CheckBluetooth() | Checks the device for server compatibility. |
| RequestEnableBt() | Shows a popup requesting that the user enable bluetooth. |
| StartServer() | Starts the server and starts advertising if the device is server compatible. |
| StopServer() | Stops the server if it is running (also stops advertising). |
| StartAdvertising() | Start advertising the server if it is running. |
| StopAdvertising() | Stop advertising the server if it is running. |
| WriteCharacteristic(uuid, data[, notify]) | Write a value to a characteistic and optionally notify subscribed clients. |
| ReadCharacteristic(uuid) | Read the value of a characteristic. |
| WriteDescriptor(uuid, data) | Write a value to a descriptor. |
| ReadDescriptor(uuid) | Read the value of a descriptor. |
| HasService(uuid) | Check if the server has a service. |
| HasCharacteristic(uuid) | Check if the server has a characteristic. |
| HasDescriptor(uuid) | Check if the server has a descriptor. |

## QuickBLE Client (Central):
The Bluetooth Low Energy Client (BLEClient) scans for and connects to GATT servers. This feature requires both hardware and software support (see table below).

##### System Requirements

| Platform | Requirements |
|--------|--------|
| Android | Android 4.3 (API 18) or newer and compatible hardware       |
| iOS / macOS | A device running iOS 9 or newer or a device running macOS 10.13 or newer.
| UWP (Windows 10) | A phone, tablet, or PC running Windows 10 Version 1607 (Creators Update) or newer and compatible hardware.

##### Properties (These do not include platform specific functionality)
| Property | What it is |
|--------|--------|
| Services | A list of uuids for each service on the connected server.|
| Characteristics |  A list of uuids for each characteristic on the connected server. |
| Descriptors |  A list of uuids for each descriptor on the connected server. |
| ScanServices |  A list of uuids for each service the client is scanning for. |
| IsScanning | Is the client scanning for servers. |
| IsConnected | Is the client connected to a server. |
| REQUEST_BT_TITLE | The title used for the RequestEnableBt dialog. |
| REQUEST_BT_MESSAGE | The message used for the RequestEnableBt dialog. |
| REQUEST_BT_CONFIRM | The text used for the confirm button of theRequestEnableBt dialog. |
| REQUEST_BT_DENY | The text used for the deny button of theRequestEnableBt dialog. |

##### Functions
| Function | What it does |
|--------|--------|
| ScanForService(uuid[, scanFor]) | Adds or removes a service from the list of services to scan for. |
| CheckBluetooth() | Checks the device for client compatibility. |
| RequestEnableBt() | Shows a popup requesting that the user enable bluetooth. |
| ScanForDevices() | Starts scanning if the device is client compatible. |
| StopScanning() | Stops scanning for server if the client is scanning. |
| ConnectToDevice(deviceId) | Connect to a discovered server. |
| Disconnect() | Disconnect from a server if connected. |
| SubscribeToCharacteristic(uuid[, subscribe]) | Subscribe or unsubscribe from value changed notifications for a characteristic. |
| WriteCharacteristic(uuid, data) | Write a value to a characteistic. |
| ReadCharacteristic(uuid) | Read the value of a characteristic. |
| WriteDescriptor(uuid, data) | Write a value to a descriptor. |
| ReadDescriptor(uuid) | Read the value of a descriptor. |
| HasService(uuid) | Check if the connected server has a service. |
| HasCharacteristic(uuid) | Check if the connected server has a characteristic. |
| HasDescriptor(uuid) | Check if the connected server has a descriptor. |

## QuickBLE Delegate (BLEDelegate)
##### Server Specific
| Method | When it is called |
|--------|--------|
| OnAdvertise(error) | Called when the server starts advertising. |
| OnDeviceConnected(id, name, address) | Called when a device connects to the server. |
| OnDeviceDisconnected(id, name, address) | Called when a device disconnects from the server. |

##### Client Specific
| Method | When it is called |
|--------|--------|
| OnDeviceDiscovered(id, name, address, rssi) | Called when the client discoveres a server. |
| OnConnectToDevice(id, name, address, success) | Called when the client connects to a device. |
| OnDisconnectFromDevice(id, name, address) | Called when the client disconnects (or is disconnected) from a device. |
| OnServicesDiscovered() | Called when the client has discovered all services, characteristics, and descriptors form the connected server. |

##### Shared (Server & Client)
| Method | When it is called |
|--------|--------|
| OnCharacteristicRead(uuid, writingDeviceId, success, value) | Called when a characteristic's value is read (ReadCharacteristic is called) or when a client writes a new value to a characteristic (Server) or when a new value for a characteristic is recieved by notification from the server (Client - only for subscribed characteristics). writingDeviceId is always -1 for clients. For servers writingDeviceId is -1 if the server wrote the value to the characteristic, but if a client wrote the value to the characteristic writingDeviceId is the id of the client. |
| OnCharacteristicWrite(uuid, success, value) | Called when a characteristic's value is written (by WriteCharacteristic). |
| OnDescriptorRead(uuid, writingDeviceId, success, value) | Called when a desriptor's value is read (ReadDescriptor is called) or when a client writes a new value to a descriptor (Server). writingDeviceId is always -1 for clients. For servers writingDeviceId is -1 if the server wrote the value to the characteristic, but if a client wrote the value to the characteristic writingDeviceId is the id of the client.|
| OnDescriptorWrite(uuid, success, value) | Called when a descriptor's value is written (by WriteDescriptor). |
| OnBluetoothPowerChange(enabled) | Called when the bluetooth radio is turned on or off. |

## Platform Specific Nuances and Options

##### Android
| Role | Option | Value | Effect |
|--------|--------|--------|--------|
| Server | advertiseMode | An int from the AdvertiseMode class. | Affects the advertisement latency. |
| Server | advertiseTxPower | An int from the AdvertiseTxPower class. | Affects the power consumption of the advertisement. |
| Server | advertiseDeviceName | A boolean. | Whether or not the server should advertise it's name. |
| Client | scanMode | An int from the ScanMode class. | If using the new scan method on Android 5.0+: Affects the power consumtion and latency of the scanner. |

##### iOS/macOS
| Role | Option | Value | Effect |
|--------|--------|--------|--------|
| Server | advertiseDeviceName | A boolean. | Whether or not the server should advertise it's name. |
| Server | advertiseMode | An int from the AdvertiseMode class. | Affects the power consumtion and latency of the server's advertisement. |
| Client | continuousScan | A boolean. | Should the client fire the discover device delegate method each time it sees an advertisement packet (even if it has already reported the device). |

##### UWP
| Role | Option | Value | Effect |
|--------|--------|--------|--------|
| Server | AdvertiseDeviceName | A boolean. | Like on other platforms, this controls whether or not the server should advertise the device's name. Unlike on other platforms, this will always happen if ***ANY*** service is being advertised. |

**NOTES:**
- UWP does not support included service.
- UWP does not support start/stop advertising. It doesn't work.
- On UWP advertiseError will always be None.
- StopServer will not disconnect any connected devices, but it will block any further reads/writes to characteristics and descriptors on the server.
- On UWP the client does not notify the delegate when it looses connection with the server.

***Most of these limitations are due to the Windows 10 API. Most cannot be fixed.***