#ifndef QuickBLE_UnityBridge_h
#define QuickBLE_UnityBridge_h

#include <TargetConditionals.h>

#ifndef MACOS_BUNDLE_UNITY
#import "QuickBLE/QuickBLE.h"
#import "QuickBLE/QuickBLE-Swift.h"
#else
#import <CoreBluetooth/CoreBluetooth.h>
#import "QuickBLE-Swift.h"
#endif

#define true 1
#define false 0

extern "C" {
    //MARK: Delegate/Callback types
    typedef void ( * AdvertiseCallback)(NSObject*, int);
    typedef void ( * BtPowerCallback)(NSObject*, bool);
    typedef void ( * RequestBtCallback)(NSObject*, bool);
    typedef void ( * CharReadCallback)(NSObject*, const char*, const char*, bool, unsigned char*, int);
    typedef void ( * CharWriteCallback)(NSObject*, const char*, bool, unsigned char*, int);
    typedef void ( * ConnectToDeviceCallback)(NSObject*, const char*, const char*, bool);
    typedef void ( * DescReadCallback)(NSObject*, const char*, const char*, bool, unsigned char*, int);
    typedef void ( * DescWriteCallback)(NSObject*, const char*, bool, unsigned char*, int);
    typedef void ( * DeviceConnectedCallback)(NSObject*, const char*, const char*);
    typedef void ( * DisconnectFromDeviceCallback)(NSObject*, const char*, const char*);
    typedef void ( * DeviceDiscoveredCallback)(NSObject*, const char*, const char*, int);
    typedef void ( * DeviceDisconnectCallback)(NSObject*, const char*, const char*);
    typedef void ( * ServiceDiscoveredCallback)(NSObject*);
    typedef void ( * SentNotificationCallback)(NSObject*, const char*, bool);
    
    // Make sure objects do not get deallocated once scope of init function is left
    NSMutableArray<BLEServer*> *__activeServers = [[NSMutableArray alloc] init];
    NSMutableArray<BLEClient*> *__activeClients = [[NSMutableArray alloc] init];
    
    //MARK: Support
    char** getArray(NSArray<NSString*> *array); // Convert NSArray to const char**
    unsigned char* getBytes(NSData *data);
    
    //MARK: Server
    BLEServer* Server_Init(AdvertiseCallback advc, BtPowerCallback btpc, CharReadCallback crc, CharWriteCallback cwc, ConnectToDeviceCallback ctdc, DescReadCallback drc, DescWriteCallback dwc, DeviceConnectedCallback dcc, DisconnectFromDeviceCallback disconfromc, DeviceDiscoveredCallback discovc, DeviceDisconnectCallback disconnc, ServiceDiscoveredCallback sdc, RequestBtCallback rbc, SentNotificationCallback snc);
    //Properties
    bool Server_IsRunning(BLEServer* server);
    bool Server_IsAdvertising(BLEServer* server);
    int Server_GetServices(BLEServer* server, char*** rtn);
    int Server_GetCharacteristics(BLEServer* server, char*** rtn);
    int Server_GetDescriptors(BLEServer* server, char*** rtn);
    int Server_GetAdvertiseServices(BLEServer* server, char*** rtn);
    // Server Control
    void Server_AddService(BLEServer* server, const char* service);
    bool Server_AddIncludedService(BLEServer* server, const char* service, const char* parentService);
    bool Server_AddCharacteristic(BLEServer* server, const char* characteristic, const char* parentService, int properties, int permissions);
    bool Server_AddDescriptor(BLEServer* server, const char* descriptor, const char* parentCharacteristic);
    void Server_AdvertiseService(BLEServer* server, const char* service, bool advertise);
    void Server_ClearGatt(BLEServer* server);
    int Server_CheckBluetooth(BLEServer* server);
    void Server_RequestEnableBt(BLEServer* server);
    int Server_StartServer(BLEServer* server);
    void Server_StopServer(BLEServer* server);
    void Server_StartAdvertising(BLEServer* server);
    void Server_StopAdvertising(BLEServer* server);
    void Server_NotifyDevice(BLEServer* server, const char* characteristic, const char* deviceAddress);
    // Characteristics and Descriptors
    void Server_WriteCharacteristic(BLEServer* server, const char* characteristic, unsigned char* data, int len, bool notify);
    void Server_ReadCharacteristic(BLEServer* server, const char* characteristic);
    void Server_WriteDescriptor(BLEServer* server, const char* descriptor, unsigned char* data, int len);
    void Server_ReadDescriptor(BLEServer* server, const char* descriptor);
    bool Server_HasService(BLEServer* server, const char* service);
    bool Server_HasCharacteristic(BLEServer* server, const char* characteristic);
    bool Server_HasDescriptor(BLEServer* server, const char* descriptor);
    
    //MARK: Client
    BLEClient* Client_Init(AdvertiseCallback advc, BtPowerCallback btpc, CharReadCallback crc, CharWriteCallback cwc, ConnectToDeviceCallback ctdc, DescReadCallback drc, DescWriteCallback dwc, DeviceConnectedCallback dcc, DisconnectFromDeviceCallback disconfromc, DeviceDiscoveredCallback discovc, DeviceDisconnectCallback disconnc, ServiceDiscoveredCallback sdc, RequestBtCallback rbc, SentNotificationCallback snc);
    // Properties
    bool Client_IsScanning(BLEClient* client);
    bool Client_IsConnected(BLEClient* client);
    int Client_GetServices(BLEClient* client, char*** rtn);
    int Client_GetCharacteristics(BLEClient* client, char*** rtn);
    int Client_GetDescriptors(BLEClient* client, char*** rtn);
    int Client_GetScanServices(BLEClient* client, char*** rtn);
    // Client Control
    void Client_ScanForService(BLEClient* client, const char* service, bool scanFor);
    int Client_CheckBluetooth(BLEClient* client);
    void Client_RequestEnableBt(BLEClient* client);
    int Client_ScanForDevices(BLEClient* client);
    void Client_StopScanning(BLEClient* client);
    void Client_ConnectToDevice(BLEClient* client, const char* deviceAddress);
    void Client_Disconnect(BLEClient* client);
    void Client_SubscribeToCharacteristic(BLEClient* client, const char* characteristic, bool subscribe);
    // Characteristics and Descriptors
    void Client_ReadCharacteristic(BLEClient* client, const char* characteristic);
    void Client_WriteCharacteristic(BLEClient* client, const char* characteristic, unsigned char* data, int len);
    void Client_ReadDescriptor(BLEClient* client, const char* descriptor);
    void Client_WriteDescriptor(BLEClient* client, const char* descriptor, unsigned char* data, int len);
    bool Client_HasService(BLEClient* client, const char* service);
    bool Client_HasCharacteristic(BLEClient* client, const char* characteristic);
    bool Client_HasDescriptor(BLEClient* client, const char* descriptor);
}

#endif /* QuickBLE_UnityBridge_h */
