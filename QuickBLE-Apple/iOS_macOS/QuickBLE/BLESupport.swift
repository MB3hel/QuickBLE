import Foundation

@objc public class BtError : NSObject{
    public static let Unknown = 0;
    public static let Resetting = 1;
    public static let NoBluetooth = 2; //Unsuported
    public static let Unauthorized = 3;
    public static let Disabled = 4; //PoweredOff
    public static let None = 5; //PoweredOn
    public static let AlreadyRunning = 6;
}
@objc public class AdvertiseError : NSObject{
    public static let None = 0
    public static let DataTooLarge = 4
    public static let TooManyAdvertisers = 11
    public static let AlreadyStarted = 9
    public static let InternalError = -1
}
@objc public class CharProperties : NSObject{
    public static let Broadcast = 1;
    public static let Read = 2;
    public static let WriteNoResponse = 4;
    public static let Write = 8;
    public static let Notify = 16;
    public static let Indicate = 32;
    public static let SignedWrite = 64;
    public static let ExtendedProps = 128;
}
@objc public class CharPermissions : NSObject{
    public static let Read = 1;
    public static let ReadEncrypted = 2;
    public static let Write = 4;
    public static let WriteEncrypted = 8;
}
@objc public class AdvertiseMode: NSObject{
    public static let LowLatency = 2
    public static let Balanced = 1
    public static let LowPower = 0
}
