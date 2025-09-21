# WARNING: ARCHIVED!!!

**This code was a prototype (may not be suitable for production) and has mostly been abandoned!**


# QuickBLE API
QuickBLE is a multiplatform API for Bluetooth Low Energy GATT. QuickBLE is designed to be used with Unity3D Game Engine, but can be used in native apps as well.
This is a building and general usage guide. For API information see the [API Docs](API.MD).
## Platforms
- **Android**
The Android library is written in Kotlin. The library can be built as an aar to be used in any Android project.
- **iOS and macOS**
The iOS and macOS library is written in Swift. The library can be built as a framework and added to any iOS or macOS Project.
- ~~**Windows 10 UWP**~~
~~The UWP library is written in C#. The library can be built as a DLL and added to any C# UWP project targeting Windows 10 Creators Update or later.~~

## Licensing
GPLv3 - see `COPYING`

## Building
#### Android
- Go to the android project directory
- Run the following command
`gradlew exportAar`
- The resulting AAR will be located in app/release

#### iOS and macOS
- Open the project in Xcode
- Click the target selector and click edit schemes
- Set Build Configuration to Release
- Build for Generic iOS Device and/or for macOS
- The resulting framework(s) can be seen in Xcode's explorer under the Products folder

#### ~~UWP~~
- ~~Open the Visual Studio Solution~~
- ~~Select Release and Any CPU~~
- ~~Select Build > Build Solution~~
- ~~The resulting DLL will be under QuickBLE/bin/Release~~

## Using with Unity
- Copy the QuickBLE.cs and MainThread.cs scripts to the Unity project
- Add each platofrm's QuickBLE code to the project
- Attach the MainThread Script to any object in each scene where QuickBLE will be used (this us used to make sure delegate methods can modify UI components in Unity)
#### Platforms
##### Android
Build the AAR then copy it to the Unity project's Assets/Plugins/Android directory (create the directory if it does not exist). Also copy the jar/aar files in the dependencies folder (under app/release). Make sure to add the AndroidBridge Unity Script (QuickBLEAndroidBridge.cs).
##### iOS
Build the framework for iOS and copy it along with the UnityBridge files (QuickBLEUnityBridge.h/.mm) to the Untiy Project's Assets/Plugins/iOS directory (create it if it doesn't exist). Make sure to add the AppleBridge Unity Script (QuickBLEAppleBridge.cs). When you export the Xcode project from Unity, follow the steps below.


- Before Exporting: Make sure Unity (Player Settings) has minimum iOS version set to 9
- Under the project's build settings (Xcode) find "Runpath Search Paths" and add
- "@executable_path/Frameworks" without the quotes.
- Change "Always Embed Swift Standard Libraries" to Yes
- Add QuickBLE.framework to embeded binaries in the project's general settings
##### macOS
The macOS library must be built as a bundle to be used in Unity. Build the macOS_Unity project and copy the resulting QuickBLE.bundle file to the Unity project's Assets/Plugins/macOS directory. In the editor select which architecture is supported by the bundle (only need to support x64 as QuickBLE only supports macOS 10.13 and newer). The plugin can also be used in the macOS editor, however it is not reccommended as the editor can cause strange issues with native code. Be sure to also add the QuickBLEAppleBridge.cs file to the Unity project.
##### ~~UWP~~
~~Build the QuickBLE project and copy the resulting DLL to the Assets/Plugins/WSA directory of the Unity project.~~
~~Then, Select the QuickBLE DLL in the Unity editor and set it to work on WSA Player with the following settings:~~
- ~~***SDK: UWP***~~
- ~~***Scripting Backend: .Net***~~

~~Finally, copy the QuickBLEUWPBridge.cs script to the Unity project. Before building:~~
- ~~Make sure to use the .Net scripting~~
- ~~Make sure to use a UWP SDK at or above the Creator's Update SDK (10.0.15063.0). If using 10.0.15063.0 Unity will have errors after generating the Visual Studio solution, but they can be ignored.~~
- ~~Make sure to add Bluetooth capabilities to the app~~
- ~~After building (generating VS solution) make sure to set the min version to Creators Update (using Visual Studio)~~

## Examples

TODO: QuickBLETest project not published yet
