#include "QuickBLESingleton.h"

////////////////////////////////////////////////////
/// BLEDelegate implementation
////////////////////////////////////////////////////
@interface GodotDelegate : NSObject<BLEDelegate>
@property (retain) NSObject* object;
@property int objectId;
@property QuickBLESingleton *singleton;

- (instancetype)initWithSingleton:(QuickBLESingleton *) singleton;

@end
@implementation GodotDelegate

- (instancetype)initWithSingleton:(QuickBLESingleton *) singleton{
    self = [super init];
    if(self){
        _singleton = singleton;
    }
    return self;
}

// The actual delegate:

- (void)onAdvertiseWithError:(NSInteger)error {
    Object *obj = ObjectDB::get_instance(_singleton->getInstanceId());
    obj->call_deferred("_onAdvertise", _objectId, (int)error);
}
- (void)onBluetoothPowerChange:(BOOL)enabled {
    Object *obj = ObjectDB::get_instance(_singleton->getInstanceId());
    obj->call_deferred("_onBluetoothPowerChanged", _objectId, enabled);
}
- (void)onCharacteristicRead:(NSString * _Nonnull)characteristic writingDeviceAddress:(NSString * _Nonnull)writingDeviceAddress success:(BOOL)success value:(NSData * _Nullable)value {
    Object *obj = ObjectDB::get_instance(_singleton->getInstanceId());
    obj->call_deferred("_onCharacteristicRead", _objectId, QuickBLESingleton::fromNSString(characteristic), QuickBLESingleton::fromNSString(writingDeviceAddress), success, QuickBLESingleton::toPoolByteArray(value));
}
- (void)onCharacteristicWrite:(NSString * _Nonnull)characteristic success:(BOOL)success value:(NSData * _Nullable)value {
    Object *obj = ObjectDB::get_instance(_singleton->getInstanceId());
    obj->call_deferred("_onCharacteristicWrite", _objectId, QuickBLESingleton::fromNSString(characteristic), success, QuickBLESingleton::toPoolByteArray(value));
}
- (void)onConnectToDeviceWithAddress:(NSString *)address name:(NSString *)name success:(BOOL)success{
    Object *obj = ObjectDB::get_instance(_singleton->getInstanceId());
    obj->call_deferred("_onConnectToDevice", _objectId, QuickBLESingleton::fromNSString(address), QuickBLESingleton::fromNSString(name), success);
}
- (void)onDescriptorRead:(NSString * _Nonnull)descriptor writingDeviceAddress:(NSString * _Nonnull)writingDeviceAddress success:(BOOL)success value:(NSData * _Nullable)value {
    Object *obj = ObjectDB::get_instance(_singleton->getInstanceId());
    obj->call_deferred("_onDescriptorRead", _objectId, QuickBLESingleton::fromNSString(descriptor), QuickBLESingleton::fromNSString(writingDeviceAddress), success, QuickBLESingleton::toPoolByteArray(value));
}
- (void)onDescriptorWrite:(NSString * _Nonnull)descriptor success:(BOOL)success value:(NSData * _Nullable)value {
    Object *obj = ObjectDB::get_instance(_singleton->getInstanceId());
    obj->call_deferred("_onDescriptorWrite", _objectId, QuickBLESingleton::fromNSString(descriptor), success, QuickBLESingleton::toPoolByteArray(value));
}
- (void)onDeviceConnectedWithAddress:(NSString *)address name:(NSString *)name{
    Object *obj = ObjectDB::get_instance(_singleton->getInstanceId());
    obj->call_deferred("_onDeviceConnected", _objectId, QuickBLESingleton::fromNSString(address), QuickBLESingleton::fromNSString(name));
}
- (void)onDeviceDisconnectedWithAddress:(NSString *)address name:(NSString *)name {
    Object *obj = ObjectDB::get_instance(_singleton->getInstanceId());
    obj->call_deferred("_onDeviceDisconnected", _objectId, QuickBLESingleton::fromNSString(address), QuickBLESingleton::fromNSString(name));
}
- (void)onDeviceDiscoveredWithAddress:(NSString *)address name:(NSString *)name rssi:(NSInteger)rssi {
    Object *obj = ObjectDB::get_instance(_singleton->getInstanceId());
    obj->call_deferred("_onDeviceDiscovered", _objectId, QuickBLESingleton::fromNSString(address), QuickBLESingleton::fromNSString(name), (int)rssi);
}
- (void)onDisconnectFromDeviceWithAddress:(NSString *)address name:(NSString *)name{
    Object *obj = ObjectDB::get_instance(_singleton->getInstanceId());
    obj->call_deferred("_onDisconnectFromDevice", _objectId, QuickBLESingleton::fromNSString(address), QuickBLESingleton::fromNSString(name));
}
- (void)onServicesDiscovered {
    Object *obj = ObjectDB::get_instance(_singleton->getInstanceId());
    obj->call_deferred("_onServicesDiscovered", _objectId);
}
- (void)onBluetoothRequestResult: (BOOL)choseToEnable{
    Object *obj = ObjectDB::get_instance(_singleton->getInstanceId());
    obj->call_deferred("_onBluetoothRequestResult", _objectId, choseToEnable);
}
    
- (void)onNotificationSent:(NSString * _Nonnull)characterstic success:(BOOL)success {
    Object *obj = ObjectDB::get_instance(_singleton->getInstanceId());
    obj->call_deferred("_onNotificationSent", _objectId, QuickBLESingleton::fromNSString(characterstic), success);
}
@end

/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////

int QuickBLESingleton::currentId = -1;

////////////////////////////////////////////////////
/// Client functions
////////////////////////////////////////////////////
int QuickBLESingleton::createClient(){
    GodotDelegate *delegate = [[GodotDelegate alloc] initWithSingleton:this];
    BLEClient *client = [[BLEClient alloc] init:delegate];
    int clientId = ++currentId;
    delegate.object = client;
    delegate.objectId = clientId;
    objects[clientId] = (NSObject*) client;
    return clientId;
}

void QuickBLESingleton::destroyClient(int clientId){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    [client stopScanning];
    [client disconnect];
    objects.erase(clientId);
}

void QuickBLESingleton::clientScanForService(int clientId, String service, bool scanFor){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    [client scanForService:toNSString(service) scanFor: scanFor];
}

int QuickBLESingleton::clientCheckBluetooth(int clientId){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return ERROR_UNKNOWN_ID;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    return (int)[client checkBluetooth];
}

void QuickBLESingleton::clientRequestEnableBt(int clientId){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    [client requestEnableBt];
}

int QuickBLESingleton::clientScanForDevices(int clientId){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return ERROR_UNKNOWN_ID;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    return (int)[client scanForDevices];
}

void QuickBLESingleton::clientStopScanning(int clientId){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    [client stopScanning];
}

void QuickBLESingleton::clientConnectToDevice(int clientId, String deviceAddress){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    [client connectToDevice:toNSString(deviceAddress)];
}

void QuickBLESingleton::clientDisconnect(int clientId){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    [client disconnect];
}

void QuickBLESingleton::clientSubscribeToCharacteristic(int clientId, String characteristic, bool subscribe){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    [client subscribeToCharacteristic: toNSString(characteristic) subscribe: subscribe];
}

void QuickBLESingleton::clientReadCharacteristic(int clientId, String characteristic){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    [client readCharacteristic:toNSString(characteristic)];
}

void QuickBLESingleton::clientWriteCharacteristic(int clientId, String characteristic, PoolByteArray data){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    [client writeCharacteristic: toNSString(characteristic) data: fromPoolByteArray(data)];
}

void QuickBLESingleton::clientReadDescriptor(int clientId, String descriptor){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    [client readDescriptor: toNSString(descriptor)];
}

void QuickBLESingleton::clientWriteDescriptor(int clientId, String descriptor, PoolByteArray data){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    [client writeDescriptor: toNSString(descriptor) data: fromPoolByteArray(data)];
}

bool QuickBLESingleton::clientHasService(int clientId, String service){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return false;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    return [client hasService: toNSString(service)];
}

bool QuickBLESingleton::clientHasCharacteristic(int clientId, String characteristic){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return false;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    return [client hasCharacteristic: toNSString(characteristic)];
}

bool QuickBLESingleton::clientHasDescriptor(int clientId, String descriptor){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return false;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    return [client hasDescriptor: toNSString(descriptor)];
}

bool QuickBLESingleton::clientIsScanning(int clientId){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return false;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    return [client getIsScanning];
}

bool QuickBLESingleton::clientIsConnected(int clientId){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return false;
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    return [client getIsConnected];
}

Array QuickBLESingleton::clientGetServices(int clientId){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return Array();
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    return getStringArray([client getServices]);
}

Array QuickBLESingleton::clientGetCharacteristics(int clientId){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return Array();
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    return getStringArray([client getCharacteristics]);
}

Array QuickBLESingleton::clientGetDescriptors(int clientId){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return Array();
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    return getStringArray([client getDescriptors]);
}

Array QuickBLESingleton::clientGetScanServices(int clientId){
    if(objects.find(clientId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", clientId);
        return Array();
    }
    BLEClient *client = (BLEClient*)objects[clientId];
    return getStringArray([client getScanServices]);
}

////////////////////////////////////////////////////
/// Server functions
////////////////////////////////////////////////////

int QuickBLESingleton::createServer(){
    GodotDelegate *delegate = [[GodotDelegate alloc] initWithSingleton:this];
    BLEServer *server = [[BLEServer alloc] init:delegate];
    int serverId = ++currentId;
    delegate.object = server;
    delegate.objectId = serverId;
    objects[serverId] = (NSObject*) server;
    return serverId;
}

void QuickBLESingleton::destroyServer(int serverId){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    [server stopServer];
    objects.erase(serverId);
}

void QuickBLESingleton::serverAddService(int serverId, String service){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    [server addService: toNSString(service)];
}

bool QuickBLESingleton::serverAddIncludedService(int serverId, String service, String parentService){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return false;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    return [server addIncludedService: toNSString(service) parentService: toNSString(parentService)];
}

bool QuickBLESingleton::serverAddCharacteristic(int serverId, String characteristic, String parentService, int properties, int permissions){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return false;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    return [server addCharacteristic: toNSString(characteristic) parentService: toNSString(parentService) properties: properties permissions: permissions];
}

bool QuickBLESingleton::serverAddDescriptor(int serverId, String descriptor, String parentCharacteristic){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return false;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    return [server addDescriptor: toNSString(descriptor) parentCharacteristic: toNSString(parentCharacteristic)];
}

void QuickBLESingleton::serverAdvertiseService(int serverId, String service, bool advertise){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    [server advertiseService: toNSString(service) advertise:advertise];
}

void QuickBLESingleton::serverClearGatt(int serverId){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    [server clearGatt];
}

int QuickBLESingleton::serverCheckBluetooth(int serverId){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return ERROR_UNKNOWN_ID;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    return (int)[server checkBluetooth];
}

void QuickBLESingleton::serverRequestEnableBt(int serverId){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    [server requestEnableBt];
}

int QuickBLESingleton::serverStartServer(int serverId){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return ERROR_UNKNOWN_ID;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    return (int)[server startServer];
}

void QuickBLESingleton::serverStopServer(int serverId){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    [server stopServer];
}

void QuickBLESingleton::serverStartAdvertising(int serverId){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    [server startAdvertising];
}

void QuickBLESingleton::serverStopAdvertising(int serverId){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    [server stopAdvertising];
}

void QuickBLESingleton::serverNotifyDevice(int serverId, String characteristic, String deviceAddress){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    [server notifyDevice: toNSString(characteristic) deviceAddress: toNSString(deviceAddress)];
}

void QuickBLESingleton::serverWriteCharacteristic(int serverId, String characteristic, PoolByteArray data, bool notify){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    [server writeCharacteristic: toNSString(characteristic) data: fromPoolByteArray(data) notify:notify];
}

void QuickBLESingleton::serverReadCharacteristic(int serverId, String characteristic){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    [server readCharacteristic: toNSString(characteristic)];
}

void QuickBLESingleton::serverWriteDescriptor(int serverId, String descriptor, PoolByteArray data){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    [server writeDescriptor: toNSString(descriptor) data: fromPoolByteArray(data)];
}

void QuickBLESingleton::serverReadDescriptor(int serverId, String descriptor){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    [server readDescriptor: toNSString(descriptor)];
}

bool QuickBLESingleton::serverHasService(int serverId, String service){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return false;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    return [server hasService: toNSString(service)];
}

bool QuickBLESingleton::serverHasCharacteristic(int serverId, String characteristic){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return false;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    return [server hasCharacteristic: toNSString(characteristic)];
}

bool QuickBLESingleton::serverHasDescriptor(int serverId, String descriptor){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return false;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    return [server hasDescriptor: toNSString(descriptor)];
}

bool QuickBLESingleton::serverIsRunning(int serverId){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return false;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    return [server getIsRunning];
}

bool QuickBLESingleton::serverIsAdvertising(int serverId){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return false;
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    return [server getIsAdvertising];
}

Array QuickBLESingleton::serverGetServices(int serverId){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return Array();
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    return getStringArray([server getServices]);
}

Array QuickBLESingleton::serverGetCharacteristics(int serverId){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return Array();
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    return getStringArray([server getCharacteristics]);
}

Array QuickBLESingleton::serverGetDescriptors(int serverId){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return Array();
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    return getStringArray([server getDescriptors]);
}

Array QuickBLESingleton::serverGetAdvertiseServices(int serverId){
    if(objects.find(serverId) == objects.end()){
        NSLog(@"Unknown server/client id: %i", serverId);
        return Array();
    }
    BLEServer *server = (BLEServer*)objects[serverId];
    return getStringArray([server getAdvertiseServices]);
}

////////////////////////////////////////////////////
/// Godot Reference/Singleton Functions
////////////////////////////////////////////////////

void QuickBLESingleton::_bind_methods(){
    ClassDB::bind_method(D_METHOD("assignInstanceId"), &QuickBLESingleton::assignInstanceId);
    
    // Server functions
    ClassDB::bind_method(D_METHOD("createServer"), &QuickBLESingleton::createServer);
    ClassDB::bind_method(D_METHOD("destroyServer"), &QuickBLESingleton::destroyServer);
    ClassDB::bind_method(D_METHOD("serverAddService"), &QuickBLESingleton::serverAddService);
    ClassDB::bind_method(D_METHOD("serverAddIncludedService"), &QuickBLESingleton::serverAddIncludedService);
    ClassDB::bind_method(D_METHOD("serverAddCharacteristic"), &QuickBLESingleton::serverAddCharacteristic);
    ClassDB::bind_method(D_METHOD("serverAddDescriptor"), &QuickBLESingleton::serverAddDescriptor);
    ClassDB::bind_method(D_METHOD("serverAdvertiseService"), &QuickBLESingleton::serverAdvertiseService);
    ClassDB::bind_method(D_METHOD("serverClearGatt"), &QuickBLESingleton::serverClearGatt);
    ClassDB::bind_method(D_METHOD("serverCheckBluetooth"), &QuickBLESingleton::serverCheckBluetooth);
    ClassDB::bind_method(D_METHOD("serverRequestEnableBt"), &QuickBLESingleton::serverRequestEnableBt);
    ClassDB::bind_method(D_METHOD("serverStartServer"), &QuickBLESingleton::serverStartServer);
    ClassDB::bind_method(D_METHOD("serverStopServer"), &QuickBLESingleton::serverStopServer);
    ClassDB::bind_method(D_METHOD("serverStartAdvertising"), &QuickBLESingleton::serverStartAdvertising);
    ClassDB::bind_method(D_METHOD("serverStopAdvertising"), &QuickBLESingleton::serverStopAdvertising);
    ClassDB::bind_method(D_METHOD("serverNotifyDevice"), &QuickBLESingleton::serverNotifyDevice);
    ClassDB::bind_method(D_METHOD("serverWriteCharacteristic"), &QuickBLESingleton::serverWriteCharacteristic);
    ClassDB::bind_method(D_METHOD("serverReadCharacteristic"), &QuickBLESingleton::serverReadCharacteristic);
    ClassDB::bind_method(D_METHOD("serverWriteDescriptor"), &QuickBLESingleton::serverWriteDescriptor);
    ClassDB::bind_method(D_METHOD("serverReadDescriptor"), &QuickBLESingleton::serverReadDescriptor);
    ClassDB::bind_method(D_METHOD("serverHasService"), &QuickBLESingleton::serverHasService);
    ClassDB::bind_method(D_METHOD("serverHasCharacteristic"), &QuickBLESingleton::serverHasCharacteristic);
    ClassDB::bind_method(D_METHOD("serverHasDescriptor"), &QuickBLESingleton::serverHasDescriptor);
    ClassDB::bind_method(D_METHOD("serverIsRunning"), &QuickBLESingleton::serverIsRunning);
    ClassDB::bind_method(D_METHOD("serverIsAdvertising"), &QuickBLESingleton::serverIsAdvertising);
    ClassDB::bind_method(D_METHOD("serverGetServices"), &QuickBLESingleton::serverGetServices);
    ClassDB::bind_method(D_METHOD("serverGetCharacteristics"), &QuickBLESingleton::serverGetCharacteristics);
    ClassDB::bind_method(D_METHOD("serverGetDescriptors"), &QuickBLESingleton::serverGetDescriptors);
    ClassDB::bind_method(D_METHOD("serverGetAdvertiseServices"), &QuickBLESingleton::serverGetAdvertiseServices);
    
    // Client functions
    ClassDB::bind_method(D_METHOD("createClient"), &QuickBLESingleton::createClient);
    ClassDB::bind_method(D_METHOD("destroyClient"), &QuickBLESingleton::destroyClient);
    ClassDB::bind_method(D_METHOD("clientScanForService"), &QuickBLESingleton::clientScanForService);
    ClassDB::bind_method(D_METHOD("clientCheckBluetooth"), &QuickBLESingleton::clientCheckBluetooth);
    ClassDB::bind_method(D_METHOD("clientRequestEnableBt"), &QuickBLESingleton::clientRequestEnableBt);
    ClassDB::bind_method(D_METHOD("clientScanForDevices"), &QuickBLESingleton::clientScanForDevices);
    ClassDB::bind_method(D_METHOD("clientStopScanning"), &QuickBLESingleton::clientStopScanning);
    ClassDB::bind_method(D_METHOD("clientConnectToDevice"), &QuickBLESingleton::clientConnectToDevice);
    ClassDB::bind_method(D_METHOD("clientDisconnect"), &QuickBLESingleton::clientDisconnect);
    ClassDB::bind_method(D_METHOD("clientSubscribeToCharacteristic"), &QuickBLESingleton::clientSubscribeToCharacteristic);
    ClassDB::bind_method(D_METHOD("clientReadCharacteristic"), &QuickBLESingleton::clientReadCharacteristic);
    ClassDB::bind_method(D_METHOD("clientWriteCharacteristic"), &QuickBLESingleton::clientWriteCharacteristic);
    ClassDB::bind_method(D_METHOD("clientReadDescriptor"), &QuickBLESingleton::clientReadDescriptor);
    ClassDB::bind_method(D_METHOD("clientWriteDescriptor"), &QuickBLESingleton::clientWriteDescriptor);
    ClassDB::bind_method(D_METHOD("clientHasService"), &QuickBLESingleton::clientHasService);
    ClassDB::bind_method(D_METHOD("clientHasCharacteristic"), &QuickBLESingleton::clientHasCharacteristic);
    ClassDB::bind_method(D_METHOD("clientHasDescriptor"), &QuickBLESingleton::clientHasDescriptor);
    ClassDB::bind_method(D_METHOD("clientIsScanning"), &QuickBLESingleton::clientIsScanning);
    ClassDB::bind_method(D_METHOD("clientIsConnected"), &QuickBLESingleton::clientIsConnected);
    ClassDB::bind_method(D_METHOD("clientGetServices"), &QuickBLESingleton::clientGetServices);
    ClassDB::bind_method(D_METHOD("clientGetCharacteristics"), &QuickBLESingleton::clientGetCharacteristics);
    ClassDB::bind_method(D_METHOD("clientGetDescriptors"), &QuickBLESingleton::clientGetDescriptors);
    ClassDB::bind_method(D_METHOD("clientGetScanServices"), &QuickBLESingleton::clientGetScanServices);
}

void QuickBLESingleton::assignInstanceId(int instanceId){
    this->instanceId = instanceId;
}

int QuickBLESingleton::getInstanceId(){
    return instanceId;
}

NSData *QuickBLESingleton::fromPoolByteArray(PoolByteArray data){
    size_t len = data.size();
    uint8_t *buffer = new uint8_t[len];
    for(size_t i = 0; i < len; ++i){
        buffer[i] = data.get(i);
    }
    NSData *dataObj = [NSData dataWithBytes: buffer length: len];
    delete[] buffer;
    return dataObj;
}

PoolByteArray QuickBLESingleton::toPoolByteArray(NSData *data){
    PoolByteArray array;
    size_t len = [data length];
    for(size_t i = 0; i < len; ++i){
        array.push_back(((uint8_t*)[data bytes])[i]);
    }
    return array;
}

Array QuickBLESingleton::getStringArray(NSArray<NSString*> *array){
    Array rtnArray;
    size_t len = [array count];
    for(size_t i = 0; i < len; ++i){
        rtnArray.push_back(String([(NSString*)[array objectAtIndex:i] UTF8String]));
    }
    return rtnArray;
}

NSString *QuickBLESingleton::toNSString(String str){
    return [NSString stringWithUTF8String: str.utf8().get_data()];
}

String QuickBLESingleton::fromNSString(NSString *str){
    return String([str UTF8String]);
}

QuickBLESingleton::QuickBLESingleton(){
    ERR_FAIL_COND(instance != NULL);
    instance = this;
}

QuickBLESingleton::~QuickBLESingleton(){
    instance = NULL;
}
