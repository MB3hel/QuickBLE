#ifndef __QUICKBLE_SINGLETON_H__
#define __QUICKBLE_SINGLETON_H__

#include "core/reference.h"
#include "core/ustring.h"
#include "core/array.h"
#include "core/variant.h"

#include <unordered_map>

#ifdef __OBJC__
#import <Foundation/Foundation.h>
#import <CoreBluetooth/CoreBluetooth.h>
#import <QuickBLE/QuickBLE-Swift.h>
typedef NSObject* obj_type;
#else
typedef void* obj_type;
#endif

/**
 * Class to act as the communication layer between godot and the objective c (swift) BLEServer and BLEClient objects.
 */
class QuickBLESingleton : public Reference {
    GDCLASS(QuickBLESingleton, Reference);

protected:
    static void _bind_methods();
    
    QuickBLESingleton *instance; // Required for singleton to work?
    
public:
    
    // Client functions
    int createClient();
    void destroyClient(int clientId);
    void clientScanForService(int clientId, String service, bool scanFor);
    int clientCheckBluetooth(int clientId);
    void clientRequestEnableBt(int clientId);
    int clientScanForDevices(int clientId);
    void clientStopScanning(int clientId);
    void clientConnectToDevice(int clientId, String deviceAddress);
    void clientDisconnect(int clientId);
    void clientSubscribeToCharacteristic(int clientId, String characteristic, bool subscribe);
    void clientReadCharacteristic(int clientId, String characteristic);
    void clientWriteCharacteristic(int clientId, String characteristic, PoolByteArray data);
    void clientReadDescriptor(int clientId, String descriptor);
    void clientWriteDescriptor(int clientId, String descriptor, PoolByteArray data);
    bool clientHasService(int clientId, String service);
    bool clientHasCharacteristic(int clientId, String characteristic);
    bool clientHasDescriptor(int clientId, String descriptor);
    bool clientIsScanning(int clientId);
    bool clientIsConnected(int clientId);
    Array clientGetServices(int clientId);
    Array clientGetCharacteristics(int clientId);
    Array clientGetDescriptors(int clientId);
    Array clientGetScanServices(int clientId);
    
    // Server functions
    int createServer();
    void destroyServer(int serverId);
    void serverAddService(int serverId, String service);
    bool serverAddIncludedService(int serverId, String service, String parentService);
    bool serverAddCharacteristic(int serverId, String characteristic, String parentService, int properties, int permissions);
    bool serverAddDescriptor(int serverId, String descriptor, String parentCharacteristic);
    void serverAdvertiseService(int serverId, String service, bool advertise);
    void serverClearGatt(int serverId);
    int serverCheckBluetooth(int serverId);
    void serverRequestEnableBt(int serverId);
    int serverStartServer(int serverId);
    void serverStopServer(int serverId);
    void serverStartAdvertising(int serverId);
    void serverStopAdvertising(int serverId);
    void serverNotifyDevice(int serverId, String characteristic, String deviceAddress);
    void serverWriteCharacteristic(int serverId, String characteristic, PoolByteArray data, bool notify);
    void serverReadCharacteristic(int serverId, String characteristic);
    void serverWriteDescriptor(int serverId, String descriptor, PoolByteArray data);
    void serverReadDescriptor(int serverId, String descriptor);
    bool serverHasService(int serverId, String service);
    bool serverHasCharacteristic(int serverId, String characteristic);
    bool serverHasDescriptor(int serverId, String descriptor);
    bool serverIsRunning(int serverId);
    bool serverIsAdvertising(int serverId);
    Array serverGetServices(int serverId);
    Array serverGetCharacteristics(int serverId);
    Array serverGetDescriptors(int serverId);
    Array serverGetAdvertiseServices(int serverId);
    
    // Singleton Implementation
    void assignInstanceId(int instanceId);
    int getInstanceId();
    
    QuickBLESingleton();
    ~QuickBLESingleton();

#ifdef __OBJC__
    static NSData *fromPoolByteArray(PoolByteArray data);
    static PoolByteArray toPoolByteArray(NSData *data);
    
    static Array getStringArray(NSArray<NSString*> *array);
    
    static NSString *toNSString(String str);
    static String fromNSString(NSString *str);
#endif
    
private:
    const static int ERROR_UNKNOWN_ID = -999;
    int instanceId;
    
    static int currentId;
    std::unordered_map<int, obj_type> objects;
    
};

#endif // __QUICKBLE_SINGLETON_H__
