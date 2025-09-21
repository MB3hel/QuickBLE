# Godot QuickBLE API Documentation

The Godot API is similar to the general API for QuickBLE, however there are a few extra steps required because of the way Godot and/or GDScript work.

## QuickBLE Manager
The `quickble.gd` script is the core QuickBLE library manager for Godot. It handles loading the Android or iOS singleton and converting values from iOS or Andoid into Godot values. *There should only ever be one instance of this class/script at a time therefore it is recommended to add it to the AutoLoad scripts for a project.*

##### Functions
| Function | What it does|
|---|---|
| createServer() | Returns a `BLEServer` object with an id that corresponds to a native BLEServer object. This is the only way a `BLEServer` should be created in Godot. |
| destoryServer(server) | Destroys an existing `BLEServer`. Destorys / frees the native object that corresponds to the Godot `BLEServer`. |
| | |
| createClient() | Returns a `BLEClient` object with an id that corresponds to a native BLEClient object. This is the only way a `BLEClient` should be created in Godot. |
| destroyClient(client) | Destroys an existing `BLEClient`. Destroys / frees the native object that corresponds to the Godot `BLEClient` |
| | |
| isAndroid() | Returns true if running on an Android device. |
| isIOS() | Returns true if running on an iOS device. |
| getAndroidVersion() | On Android: returns the SDK version int for the current device. |
| | |
| hasLocationPermission() | Checks if the Android device has the required location permission. On other platforms returns true. |
| requestLocationPermission() | If Android: requests that the user allow location access for `BLEClient` scanning on Android version 6.0.0 and newer. |

Other functions in `quickble.gd` serve to convert ints from Android or iOS to Godot types or to handle callbacks from the native server/client objects. They should not be used.

## BLEServer and BLEClient
Platform specific properties are not exposed.
Methods match the generic api documentation
