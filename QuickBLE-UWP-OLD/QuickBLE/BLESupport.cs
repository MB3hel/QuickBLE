
namespace QuickBLE {
    public class BtError {
        public const int None = 0;
        public const int NoBluetooth = 1;
        public const int NoBLE = 2;
        public const int Disabled = 3;
        public const int NoServer = 4;
        public const int AlreadyRunning = 5;
        public const int Unsupported = 6;
    }
    public class AdvertiseError {
        public const int None = 0;
        public const int TooManyAdvertisers = 2; // Resource in use
        public const int AlreadyStarted = 3;
        public const int FeatureUnsupported = 6; // NotSupported
    }
    public class DescPermissions {
        public const int Read = 1;
        public const int ReadEncrypted = 2;
        public const int Write = 4;
        public const int WriteEncrypted = 8;
    }
    public class CharPermissions {
        public const int Read = 1;
        public const int ReadEncrypted = 2;
        public const int Write = 4;
        public const int WriteEncrypted = 8;
    }
    public class CharProperties {
        public const int Broadcast = 1;
        public const int ExtendedProps = 128;
        public const int Indicate = 32;
        public const int Notify = 16;
        public const int Read = 2;
        public const int SignedWrite = 64;
        public const int Write = 8;
        public const int WriteNoResponse = 4;
    }
}
