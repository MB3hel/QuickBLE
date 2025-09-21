# QuickBLE Godot

## Using:

### Adding the scripts to a project
No custom version of the editor is required as the module works as a singleton (the editor does not need to know about the module). To use QuickBLE in your project:

- Add the scripts from the `godot_scripts` directory to your project. It is recommended to place these in a subdirectory such as `res://quickble`.
- The main quickble script must be setup as a singleton for the project. To add it as a singleton (auto load script):
    - Goto `Project` > `Project Settings` > `AutoLoad`
    - Choose the `quickble.gd` script in path
    - Enter `quickble` as the node name
    - Click add
- To use quickble get the `quickble` node the singleton was added to and create a server or client.
```
onready var quickble = get_node("/root/quickble")

func _ready():
    var server = quickble.createServer()
    var client = quickble.createClient()
```

### Exporting with QuickBLE

You will need to build custom export templates for each platform as described in the section below.

#### Android
- Copy the `quickble-module` folder from the release zip(`quickble[QBLE_VERSION]-android-plugin.zip`) directory in this project to the `android` folder in the target godot project.
- Enable [custom build](https://docs.godotengine.org/en/3.2/getting_started/workflow/export/android_custom_build.html)
- May also want to add android/build to the gitignore for the godot project
- Open the project in the godot editor and open project settings. Under the `Android` tab of the `General` add the following to the modules lsit

    ```org/godotengine/godot/QuickBLESingleton```

### iOS:
First, choose the iphone.zip as the custom template in the export settings. After exporting a few more things will need to be done with the exported project:

- Set the minimum iOS version to 10.0 (9.0 works but Godot seems to build with 10.0 SDK so there will be lots of warnings if the minimum is 9.0)
- Add the `QuickBLE.framework` (from libs folder) to the linked libraries in the General section of the project settings. You will likely have to drag and drop it from finder.
- Add `QuickBLE.framework` to the list of embeded binaries (General section of project settings).
- In Build Settings (be sure to enable show all instead of show common) enable "Always Embed Swift Standard Libraries"
- In build settings add the following to "Runpath Search Paths"
```
@executable_path/Frameworks
```

- You may also need to add a `dummy.swift` file to the project in order for swift to actually be embedded.

These changes must be made each time the project is re-exported. To avoid this do not export the full project after the first time. Instead export the pck and replace the one in the project folder. Make sure to clean before rebuilding or Xcode will not rebuild with the new pck.


## Building Godot Export Templates

### Update QuickBLE Libraries
- Put Android aar in `android/quickble-module/libs/QuickBLE.aar`
- Make sure dependencies match those of the QuickBLE project in `android/quickble-module/gradle.conf`

- Put iOS framework in `ios/lib`
    - Technically this can be done with the exported Xcode project as long as there are no changes to the singleton.
    - The only time it is necessary to rebuild the export template is if the singleton (QuickBLESingleton.mm/.h) changes.

### Android
- For android just zip the quickble-module directory.

### iOS (build from macOS)

- Make sure Xcode (SDK and command line tools) are installed

- clone this repo. Copy the ios folder to GODOT_SOURCE/modules. Rename the folder to `quickble`

- Build iOS libraries (from terminal in root of godot source tree)

```
# Build release library
scons p=iphone tools=no target=release arch=arm -j4
scons p=iphone tools=no target=release arch=arm64 -j4
lipo -create bin/libgodot.iphone.opt.arm.a bin/libgodot.iphone.opt.arm64.a -output bin/libgodot.iphone.release.fat.a

# Build debug library
scons p=iphone tools=no target=release_debug arch=arm -j4
scons p=iphone tools=no target=release_debug arch=arm64 -j4
lipo -create bin/libgodot.iphone.opt.debug.arm.a bin/libgodot.iphone.opt.debug.arm64.a -output bin/libgodot.iphone.debug.fat.a
```

If an error message similar to the one below occurs
`xcrun: error: SDK "iphoneos" cannot be located`
Open Xcode and goto Xcode > Preferences > Locations and select the correct version of the command line tools.

- Package as template zip
```
cp bin/libgodot.iphone.release.fat.a misc/dist/ios_xcode/
cp bin/libgodot.iphone.debug.fat.a misc/dist/ios_xcode/
cp -r modules/quickble/lib misc/dist/ios_xcode/
cd misc/dist/ios_xcode
zip -r -X ../../../bin/iphone.zip ./*
```

Note: Running in the simulator is not supported by QuickBLE (simulator does not support bluetooth) so the x86_64 libraries are not built. If simulator support is required run the below commands instead of the lipo commands above (before packaging the export template as a zip).

```
# Release simulator library
scons p=iphone tools=no target=release arch=x86_64 -j4
lipo -create bin/libgodot.iphone.opt.arm.a bin/libgodot.iphone.opt.arm64.a bin/libgodot.iphone.opt.x86_64.a -output bin/libgodot.iphone.release.fat.a

# Debug simulator library
scons p=iphone tools=no target=release_debug arch=x86_64 -j4
lipo -create bin/libgodot.iphone.opt.debug.arm.a bin/libgodot.iphone.opt.debug.arm64.a bin/libgodot.iphone.opt.debug.x86_64.a -output bin/libgodot.iphone.debug.fat.a
```
