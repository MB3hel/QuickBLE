#import <Foundation/Foundation.h>
#import "QuickBLE_UnityBridge.h"

@interface Delegate : NSObject<BLEDelegate>
    @property NSObject* object;
    @property AdvertiseCallback ac;
    @property BtPowerCallback btpc;
    @property CharReadCallback crc;
    @property CharWriteCallback cwc;
    @property ConnectToDeviceCallback ctdc;
    @property DescReadCallback drc;
    @property DescWriteCallback dwc;
    @property DeviceConnectedCallback dcc;
    @property DeviceDisconnectCallback disconnc;
    @property DeviceDiscoveredCallback discovc;
    @property DisconnectFromDeviceCallback disconnfromc;
    @property ServiceDiscoveredCallback sc;
    @property RequestBtCallback rbc;
    @property SentNotificationCallback snc;
@end
@implementation Delegate

- (void)onAdvertiseWithError:(NSInteger)error {
    _ac(_object, (int)error);
}
- (void)onBluetoothPowerChange:(BOOL)enabled {
    _btpc(_object, enabled);
}
- (void)onCharacteristicRead:(NSString * _Nonnull)characteristic writingDeviceAddress:(NSString * _Nonnull)writingDeviceAddress success:(BOOL)success value:(NSData * _Nullable)value {
    _crc(_object, [characteristic UTF8String], [writingDeviceAddress UTF8String], success, getBytes(value), (int)[value length]);
}
- (void)onCharacteristicWrite:(NSString * _Nonnull)characteristic success:(BOOL)success value:(NSData * _Nullable)value {
    _cwc(_object, [characteristic UTF8String], success, getBytes(value), (int)[value length]);
}
- (void)onConnectToDeviceWithAddress:(NSString *)address name:(NSString *)name success:(BOOL)success{
    _ctdc(_object, [address UTF8String], [name UTF8String], success);
}
- (void)onDescriptorRead:(NSString * _Nonnull)descriptor writingDeviceAddress:(NSString * _Nonnull)writingDeviceAddress success:(BOOL)success value:(NSData * _Nullable)value {
    _drc(_object, [descriptor UTF8String], [writingDeviceAddress UTF8String], success, getBytes(value), (int)[value length]);
}
- (void)onDescriptorWrite:(NSString * _Nonnull)descriptor success:(BOOL)success value:(NSData * _Nullable)value {
    _dwc(_object, [descriptor UTF8String], success, getBytes(value), (int)[value length]);
}
- (void)onDeviceConnectedWithAddress:(NSString *)address name:(NSString *)name{
    _dcc(_object, [address UTF8String], [name UTF8String]);
}
- (void)onDeviceDisconnectedWithAddress:(NSString *)address name:(NSString *)name {
    _disconnc(_object, [address UTF8String], [name UTF8String]);
}
- (void)onDeviceDiscoveredWithAddress:(NSString *)address name:(NSString *)name rssi:(NSInteger)rssi {
    _discovc(_object, [address UTF8String], [name UTF8String], (int)rssi);
}
- (void)onDisconnectFromDeviceWithAddress:(NSString *)address name:(NSString *)name{
    _disconnfromc(_object, [address UTF8String], [name UTF8String]);
}
- (void)onServicesDiscovered {
    _sc(_object);
}
- (void)onBluetoothRequestResult: (BOOL)choseToEnable{
    _rbc(_object, choseToEnable);
}

- (void)onNotificationSent:(NSString * _Nonnull)characterstic success:(BOOL)success {
    _snc(_object, [characterstic UTF8String], success);
}


@end

extern "C"{
    //MARK: Support
    // NSArray<NSString*> to char**
    char** getArray(NSArray<NSString*> *array){
        int size = 0;
        // Count the bytes
        for(int i = 0; i < [array count]; i++){
            size += [(NSString*)[array objectAtIndex:i] length] + 1;
        }
        // Allocate the c array
        char** buf = (char**) malloc(size);
        for(int i = 0; i < [array count]; i++){
            buf[i] = (char*)[(NSString*)[array objectAtIndex:i] UTF8String];
        }
        return buf;
    }
    unsigned char* getBytes(NSData *data){
        // Get the size of the reference
        int size = (int)[data length];
        // Allocate the c array
        unsigned char* buf = (unsigned char*) malloc(size);
        for(int i = 0; i < [data length]; i++){
            buf[i] = ((unsigned char*)[data bytes])[i];
        }
        return buf;
    }

    //MARK: Init
    BLEServer* Server_Init(AdvertiseCallback advc, BtPowerCallback btpc, CharReadCallback crc, CharWriteCallback cwc, ConnectToDeviceCallback ctdc, DescReadCallback drc, DescWriteCallback dwc, DeviceConnectedCallback dcc, DisconnectFromDeviceCallback disconfromc, DeviceDiscoveredCallback discovc, DeviceDisconnectCallback disconnc, ServiceDiscoveredCallback sdc, RequestBtCallback rbc, SentNotificationCallback snc){
        // Create the Delegate
        Delegate *delegate = [[Delegate alloc] init];
        delegate.ac = advc;
        delegate.btpc = btpc;
        delegate.crc = crc;
        delegate.cwc = cwc;
        delegate.ctdc = ctdc;
        delegate.drc = drc;
        delegate.dwc = dwc;
        delegate.dcc = dcc;
        delegate.disconnfromc = disconfromc;
        delegate.discovc = discovc;
        delegate.disconnc = disconnc;
        delegate.sc = sdc;
        delegate.rbc = rbc;
        delegate.snc = snc;
        // Initialize Swift Server
        BLEServer *server = [[BLEServer alloc] init:delegate];
        [__activeServers addObject:server];
        delegate.object = server;
        return server;
    }
    BLEClient* Client_Init(AdvertiseCallback advc, BtPowerCallback btpc, CharReadCallback crc, CharWriteCallback cwc, ConnectToDeviceCallback ctdc, DescReadCallback drc, DescWriteCallback dwc, DeviceConnectedCallback dcc, DisconnectFromDeviceCallback disconfromc, DeviceDiscoveredCallback discovc, DeviceDisconnectCallback disconnc, ServiceDiscoveredCallback sdc, RequestBtCallback rbc, SentNotificationCallback snc){
        // Create the Delegate
        Delegate *delegate = [[Delegate alloc] init];
        delegate.ac = advc;
        delegate.btpc = btpc;
        delegate.crc = crc;
        delegate.cwc = cwc;
        delegate.ctdc = ctdc;
        delegate.drc = drc;
        delegate.dwc = dwc;
        delegate.dcc = dcc;
        delegate.disconnfromc = disconfromc;
        delegate.discovc = discovc;
        delegate.disconnc = disconnc;
        delegate.sc = sdc;
        delegate.rbc = rbc;
        delegate.snc = snc;
        // Initialize Swift Client
        BLEClient *client = [[BLEClient alloc] init:delegate];
        [__activeClients addObject:client];
        delegate.object = client;
        return client;
    }
    
    //MARK: Server
    // Properties
    bool Server_IsRunning(BLEServer* server){
        return [server getIsRunning];
    }
    bool Server_IsAdvertising(BLEServer* server){
        return [server getIsAdvertising];
    }
    int Server_GetServices(BLEServer* server, char*** rtn){
        char** array = getArray([server getServices]);
        *rtn = array;
        return (int)[[server getServices] count];
    }
    int Server_GetCharacteristics(BLEServer* server, char*** rtn){
        char** array = getArray([server getCharacteristics]);
        *rtn = array;
        return (int)[[server getCharacteristics] count];
    }
    int Server_GetDescriptors(BLEServer* server, char*** rtn){
        char** array = getArray([server getDescriptors]);
        *rtn = array;
        return (int)[[server getDescriptors] count];
    }
    int Server_GetAdvertiseServices(BLEServer* server, char*** rtn){
        char** array = getArray([server getAdvertiseServices]);
        *rtn = array;
        return (int)[[server getAdvertiseServices] count];
    }
    // Server Control
    void Server_AddService(BLEServer* server, const char* service){
        [server addService:[NSString stringWithUTF8String:service]];
    }
    bool Server_AddIncludedService(BLEServer* server, const char* service, const char* parentService){
        return [server addIncludedService:[NSString stringWithUTF8String:service] parentService:[NSString stringWithUTF8String:parentService]];
    }
    bool Server_AddCharacteristic(BLEServer* server, const char* characteristic, const char* parentService, int properties, int permissions){
        return [server addCharacteristic:[NSString stringWithUTF8String:characteristic] parentService:[NSString stringWithUTF8String:parentService] properties:properties permissions:permissions];
    }
    bool Server_AddDescriptor(BLEServer* server, const char* descriptor, const char* parentCharacteristic){
        return [server addDescriptor:[NSString stringWithUTF8String:descriptor] parentCharacteristic:[NSString stringWithUTF8String:parentCharacteristic]];
    }
    void Server_AdvertiseService(BLEServer* server, const char* service, bool advertise){
        [server advertiseService:[NSString stringWithUTF8String:service] advertise:advertise];
    }
    void Server_ClearGatt(BLEServer* server){
        [server clearGatt];
    }
    int Server_CheckBluetooth(BLEServer* server){
        return (int)[server checkBluetooth];
    }
    void Server_RequestEnableBt(BLEServer* server){
        [server requestEnableBt];
    }
    int Server_StartServer(BLEServer* server){
        return (int)[server startServer];
    }
    void Server_StopServer(BLEServer* server){
        [server stopServer];
    }
    void Server_StartAdvertising(BLEServer* server){
        [server startAdvertising];
    }
    void Server_StopAdvertising(BLEServer* server){
        [server stopAdvertising];
    }
    void Server_NotifyDevice(BLEServer* server, const char* characteristic, const char* deviceAddress){
        [server notifyDevice:[NSString stringWithUTF8String:characteristic] deviceAddress: [NSString stringWithUTF8String:deviceAddress]];
    }
    // Characteristics and Descriptors
    void Server_WriteCharacteristic(BLEServer* server, const char* characteristic, unsigned char* data, int len, bool notify){
        [server writeCharacteristic:[NSString stringWithUTF8String:characteristic] data:[NSData dataWithBytes:data length:len] notify:true];
    }
    void Server_ReadCharacteristic(BLEServer* server, const char* characteristic){
        [server readCharacteristic:[NSString stringWithUTF8String:characteristic]];
    }
    void Server_WriteDescriptor(BLEServer* server, const char* descriptor, unsigned char* data, int len){
        [server writeDescriptor:[NSString stringWithUTF8String:descriptor] data:[NSData dataWithBytes:data length:len]];
    }
    void Server_ReadDescriptor(BLEServer* server, const char* descriptor){
        [server readDescriptor:[NSString stringWithUTF8String:descriptor]];
    }
    bool Server_HasService(BLEServer* server, const char* service){
        return [server hasService:[NSString stringWithUTF8String:service]];
    }
    bool Server_HasCharacteristic(BLEServer* server, const char* characteristic){
        return [server hasCharacteristic:[NSString stringWithUTF8String:characteristic]];
    }
    bool Server_HasDescriptor(BLEServer* server, const char* descriptor){
        return [server hasDescriptor:[NSString stringWithUTF8String:descriptor]];
    }

    //MARK: Client
    // Properties
    bool Client_IsScanning(BLEClient* client){
        return [client getIsScanning];
    }
    bool Client_IsConnected(BLEClient* client){
        return [client getIsConnected];
    }
    int Client_GetServices(BLEClient* client, char*** rtn){
        char** array = getArray([client getServices]);
        *rtn = array;
        return (int)[[client getServices] count];
    }
    int Client_GetCharacteristics(BLEClient* client, char*** rtn){
        char** array = getArray([client getCharacteristics]);
        *rtn = array;
        return (int)[[client getCharacteristics] count];
    }
    int Client_GetDescriptors(BLEClient* client, char*** rtn){
        char** array = getArray([client getDescriptors]);
        *rtn = array;
        return (int)[[client getDescriptors] count];
    }
    int Client_GetScanServices(BLEClient* client, char*** rtn){
        char** array = getArray([client getScanServices]);
        *rtn = array;
        return (int)[[client getScanServices] count];
    }
    // Client Control
    void Client_ScanForService(BLEClient* client, const char* service, bool scanFor){
        [client scanForService:[NSString stringWithUTF8String:service] scanFor:scanFor];
    }
    int Client_CheckBluetooth(BLEClient* client){
        return (int)[client checkBluetooth];
    }
    void Client_RequestEnableBt(BLEClient* client){
        [client requestEnableBt];
    }
    int Client_ScanForDevices(BLEClient* client){
        return (int)[client scanForDevices];
    }
    void Client_StopScanning(BLEClient* client){
        [client stopScanning];
    }
    void Client_ConnectToDevice(BLEClient* client, const char* deviceAddress){
        [client connectToDevice:[NSString stringWithUTF8String:deviceAddress]];
    }
    void Client_Disconnect(BLEClient* client){
        [client disconnect];
    }
    void Client_SubscribeToCharacteristic(BLEClient* client, const char* characteristic, bool subscribe){
        [client subscribeToCharacteristic:[NSString stringWithUTF8String:characteristic] subscribe:subscribe];
    }
    // Characteristics and Descriptors
    void Client_ReadCharacteristic(BLEClient* client, const char* characteristic){
        [client readCharacteristic:[NSString stringWithUTF8String:characteristic]];
    }
    void Client_WriteCharacteristic(BLEClient* client, const char* characteristic, unsigned char* data, int len){
        [client writeCharacteristic:[NSString stringWithUTF8String:characteristic] data:[NSData dataWithBytes:data length:len]];
    }
    void Client_ReadDescriptor(BLEClient* client, const char* descriptor){
        [client readDescriptor:[NSString stringWithUTF8String:descriptor]];
    }
    void Client_WriteDescriptor(BLEClient* client, const char* descriptor, unsigned char* data, int len){
        [client writeDescriptor:[NSString stringWithUTF8String:descriptor] data:[NSData dataWithBytes:data length:len]];
    }
    bool Client_HasService(BLEClient* client, const char* service){
        return [client hasService:[NSString stringWithUTF8String:service]];
    }
    bool Client_HasCharacteristic(BLEClient* client, const char* characteristic){
        return [client hasCharacteristic:[NSString stringWithUTF8String:characteristic]];
    }
    bool Client_HasDescriptor(BLEClient* client, const char* descriptor){
        return [client hasDescriptor:[NSString stringWithUTF8String:descriptor]];
    }
}
