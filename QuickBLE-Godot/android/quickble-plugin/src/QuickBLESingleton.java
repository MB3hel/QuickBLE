package org.godotengine.godot;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.util.Log;
import com.godot.game.R;
import javax.microedition.khronos.opengles.GL10;
import android.content.pm.PackageManager;
import android.Manifest;
import android.support.v4.app.ActivityCompat;
import android.support.v4.content.ContextCompat;

import java.util.HashMap;
import java.util.ArrayList;
import com.mb3hel.quickble.*;

/**
 * Cusotom BLEDelegate. Individual instance for each server or client. Delegate tracks the object for the server/client it is the delegagte for.
 * Delegate functions call generic callback funcs in Singleton. Object is used to pair with id that goes with godot engine object
 */
class GodotBLEDelegate implements BLEDelegate{
    private QuickBLESingleton singleton;
    int srcId;
    private Object srcObj;
    
    public GodotBLEDelegate(QuickBLESingleton singleton){
        this.singleton = singleton;
    }
    
    public void setSourceObject(int id, Object obj){
        this.srcId = id;
        this.srcObj = obj;
    }
    
    // Server Specific callback functions
    @Override
    public void onAdvertise(int error){
        GodotLib.calldeferred(singleton.getInstanceId(), "_onAdvertise", new Object[]{srcId, error});
    }
    @Override
    public void onDeviceConnected(String address, String name){
        if(name == null)
            name = "";
        GodotLib.calldeferred(singleton.getInstanceId(), "_onDeviceConnected", new Object[]{srcId, address, name});
    }
    @Override
    public void onDeviceDisconnected(String address, String name){
        if(name == null)
            name = "";
        GodotLib.calldeferred(singleton.getInstanceId(), "_onDeviceDisconnected", new Object[]{srcId, address, name});
    }
    @Override
    public void onNotificationSent(String characteristic, boolean success){
        GodotLib.calldeferred(singleton.getInstanceId(), "_onNotificationSent", new Object[]{srcId, characteristic, success});
    }
    
    // Client specific callback functions
    @Override
    public void onDeviceDiscovered(String address, String name, int rssi){
        if(name == null)
            name = "";
        GodotLib.calldeferred(singleton.getInstanceId(), "_onDeviceDiscovered", new Object[]{srcId, address, name, rssi});
    }
    @Override
    public void onConnectToDevice(String address, String name, boolean success){
        if(name == null)
            name = "";
        GodotLib.calldeferred(singleton.getInstanceId(), "_onConnectToDevice", new Object[]{srcId, address, name, success});
    }
    @Override
    public void onDisconnectFromDevice(String address, String name){
        if(name == null)
            name = "";
        GodotLib.calldeferred(singleton.getInstanceId(), "_onDisconnectFromDevice", new Object[]{srcId, address, name});
    }
    @Override
    public void onServicesDiscovered(){
        GodotLib.calldeferred(singleton.getInstanceId(), "_onServicesDiscovered", new Object[]{srcId});
    }
    
    // Shared callback functions (server and client use these)
    @Override
    public void onCharacteristicRead(String characteristic, String writingDeviceAddress, boolean success, byte[] value){
        GodotLib.calldeferred(singleton.getInstanceId(), "_onCharacteristicRead", new Object[]{srcId, characteristic, writingDeviceAddress, success, value});
    }
    @Override
    public void onCharacteristicWrite(String characteristic, boolean success, byte[] value){
        GodotLib.calldeferred(singleton.getInstanceId(), "_onCharacteristicWrite", new Object[]{srcId, characteristic, success, value});
    }
    @Override
    public void onDescriptorRead(String descriptor, String writingDeviceAddress, boolean success, byte[] value){
        GodotLib.calldeferred(singleton.getInstanceId(), "_onDescriptorRead", new Object[]{srcId, descriptor, writingDeviceAddress, success, value});
    }
    @Override 
    public void onDescriptorWrite(String descriptor, boolean success, byte[] value){
        GodotLib.calldeferred(singleton.getInstanceId(), "_onDescriptorWrite", new Object[]{srcId, descriptor, success, value});
    }
    @Override 
    public void onBluetoothPowerChanged(boolean enabled){
        GodotLib.calldeferred(singleton.getInstanceId(), "_onBluetoothPowerChanged", new Object[]{srcId, enabled});
    }
    @Override
    public void onBluetoothRequestResult(boolean choseToEnable){
        GodotLib.calldeferred(singleton.getInstanceId(), "_onBluetoothRequestResult", new Object[]{srcId, choseToEnable});
    }
}

public class QuickBLESingleton extends Godot.SingletonBase {
    
    /**
     * List of functions this singleton will expose to Godot
     */
    private static final String[] functionList = new String[]{
        // Singleton functions
        "assignInstanceId",
        "requestLocationPerission",
        "hasLocationPermission",
        
        // Server functions
        "createServer",
        "destroyServer",
        "serverAddService",
        "serverAddIncludedService",
        "serverAddCharacteristic",
        "serverAddDescriptor",
        "serverAdvertiseService",
        "serverClearGatt",
        "serverCheckBluetooth",
        "serverRequestEnableBt",
        "serverStartServer",
        "serverStopServer",
        "serverStartAdvertising",
        "serverStopAdvertising",
        "serverNotifyDevice",
        "serverWriteCharacteristic",
        "serverReadCharacteristic",
        "serverWriteDescriptor",
        "serverReadDescriptor",
        "serverHasService",
        "serverHasCharacteristic",
        "serverHasDescriptor",
        "serverIsRunning",
        "serverIsAdvertising",
        "serverGetServices",
        "serverGetCharacteristics",
        "serverGetDescriptors",
        "serverGetAdvertiseServices",
        
        // Client functions
        "createClient",
        "destroyClient",
        "clientScanForService",
        "clientCheckBluetooth",
        "clientRequestEnableBt",
        "clientScanForDevices",
        "clientStopScanning",
        "clientConnectToDevice",
        "clientDisconnect",
        "clientSubscribeToCharacteristic",
        "clientReadCharacteristic",
        "clientWriteCharacteristic",
        "clientReadDescriptor",
        "clientWriteDescriptor",
        "clientHasService",
        "clientHasCharacteristic",
        "clientHasDescriptor",
        "clientIsScanning",
        "clientIsConnected",
        "clientGetServices",
        "clientGetCharacteristics",
        "clientGetDescriptors",
        "clientGetScanServices"
    };
    
    private static final int ERROR_UNKNOWN_ID = -999;
    
    // Keep track of specific servers and clients 
    private static int currentId = -1;
    HashMap<Integer, Object> objects = new HashMap<>();
    
    ///////////////////////////////////////////////////////////////////////
    /// BLEClient function set
    ///////////////////////////////////////////////////////////////////////
    
    public int createClient(){
        // Setup the server and the delegate. Make sure both know about each other
        GodotBLEDelegate delegate = new GodotBLEDelegate(this);
        BLEClient client = new BLEClient(appActivity, delegate, true);
        int id = ++currentId;
        delegate.setSourceObject(id, client);
        objects.put(id, client);
        return id;
    }
    
    public void destroyClient(int id){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        client.stopScanning();
        client.disconnect();
        objects.remove(id);
    }
    
    public void clientScanForService(int id, String service, boolean scanFor){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        client.scanForService(service, scanFor);
    }
    
    public int clientCheckBluetooth(int id){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return ERROR_UNKNOWN_ID;
        }
        return client.checkBluetooth();
    }
    
    public void clientRequestEnableBt(int id){
        final BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        runOnUiThread(new Runnable(){
            @Override
            public void run(){
                client.requestEnableBt();
            }
        });
    }
    
    public int clientScanForDevices(int id){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return ERROR_UNKNOWN_ID;
        }
        return client.scanForDevices();
    }
    
    public void clientStopScanning(int id){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        client.stopScanning();
    }
    
    public void clientConnectToDevice(int id, String deviceAddress){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        client.connectToDevice(deviceAddress);
    }
    
    public void clientDisconnect(int id){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        client.disconnect();
    }
    
    public void clientSubscribeToCharacteristic(int id, String characteristic, boolean subscribe){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        client.subscribeToCharacteristic(characteristic, subscribe);
    }
    
    public void clientReadCharacteristic(int id, String characteristic){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        client.readCharacteristic(characteristic);
    }
    
    public void clientWriteCharacteristic(int id, String characteristic, byte[] data){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        client.writeCharacteristic(characteristic, data);
    }
    
    public void clientReadDescriptor(int id, String descriptor){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        client.readDescriptor(descriptor);
    }
    
    public void clientWriteDescriptor(int id, String descriptor, byte[] data){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        client.writeDescriptor(descriptor, data);
    }
    
    public boolean clientHasService(int id, String service){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return false;
        }
        return client.hasService(service);
    }
    
    public boolean clientHasCharacteristic(int id, String characteristic){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return false;
        }
        return client.hasCharacteristic(characteristic);
    }
    
    public boolean clientHasDescriptor(int id, String descriptor){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return false;
        }
        return client.hasDescriptor(descriptor);
    }
    
    public boolean clientIsScanning(int id){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return false;
        }
        return client.isScanning();
    }
    
    public boolean clientIsConnected(int id){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return false;
        }
        return client.isConnected();
    }
    
    public String[] clientGetServices(int id){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return new String[]{};
        }
        ArrayList<String> al = client.getServices();
        String[] a = new String[al.size()];
        return al.toArray(a);
    }
    
    public String[] clientGetCharacteristics(int id){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return new String[]{};
        }
        ArrayList<String> al = client.getCharacteristics();
        String[] a = new String[al.size()];
        return al.toArray(a);
    }
    
    public String[] clientGetDescriptors(int id){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return new String[]{};
        }
        ArrayList<String> al = client.getDescriptors();
        String[] a = new String[al.size()];
        return al.toArray(a);
    }
    
    public String[] clientGetScanServices(int id){
        BLEClient client = (BLEClient) objects.get(id);
        if(client == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return new String[]{};
        }
        ArrayList<String> al = client.getScanServices();
        String[] a = new String[al.size()];
        return al.toArray(a);
    }
    
    ///////////////////////////////////////////////////////////////////////
    /// BLEServer function set
    ///////////////////////////////////////////////////////////////////////
    
    public int createServer(){
        // Setup the server and the delegate. Make sure both know about each other
        GodotBLEDelegate delegate = new GodotBLEDelegate(this);
        BLEServer server = new BLEServer(appActivity, delegate);
        int id = ++currentId;
        delegate.setSourceObject(id, server);
        objects.put(id, server);
        return id;
    }
    
    public void destroyServer(int id){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        server.stopServer();
        objects.remove(id);
    }
    
    public void serverAddService(int id, String service){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        server.addService(service);
    }
    
    public boolean serverAddIncludedService(int id, String service, String parentService){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return false;
        }
        return server.addIncludedService(service, parentService);
    }
    
    public boolean serverAddCharacteristic(int id, String characteristic, String parentService, int properties, int permissions){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return false;
        }
        return server.addCharacteristic(characteristic, parentService, properties, permissions);
    }
    
    public boolean serverAddDescriptor(int id, String descriptor, String parentCharacteristic, int permissions){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return false;
        }
        return server.addDescriptor(descriptor, parentCharacteristic, permissions);
    }
    
    public void serverAdvertiseService(int id, String service, boolean advertise){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        server.advertiseService(service, advertise);
    }
    
    public void serverClearGatt(int id){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        server.clearGatt();
    }
    
    public int serverCheckBluetooth(int id){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return ERROR_UNKNOWN_ID;
        }
        return server.checkBluetooth();
    }
    
    public void serverRequestEnableBt(int id){
        final BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        runOnUiThread(new Runnable(){
            @Override
            public void run(){
                server.requestEnableBt();
            }
        });
    }
    
    public int serverStartServer(int id){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return ERROR_UNKNOWN_ID;
        }
        return server.startServer();
    }
    
    public void serverStopServer(int id){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        server.stopServer();
    }
    
    public void serverStartAdvertising(int id){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        server.startAdvertising();
    }
    
    public void serverStopAdvertising(int id){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        server.stopAdvertising();
    }
    
    public void serverNotifyDevice(int id, String characteristic, String deviceAddress){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        server.notifyDevice(characteristic, deviceAddress);
    }
    
    public void serverWriteCharacteristic(int id, String characteristic, byte[] data, boolean notify){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        server.writeCharacteristic(characteristic, data, notify);
    }
    
    public void serverReadCharacteristic(int id, String characteristic){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        server.readCharacteristic(characteristic);
    }
    
    public void serverWriteDescriptor(int id, String descriptor, byte[] data){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        server.writeDescriptor(descriptor, data);
    }
    
    public void serverReadDescriptor(int id, String descriptor){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return;
        }
        server.readDescriptor(descriptor);
    }
    
    public boolean serverHasService(int id, String service){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return false;
        }
        return server.hasService(service);
    }
    
    public boolean serverHasCharacteristic(int id, String characteristic){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return false;
        }
        return server.hasCharacteristic(characteristic);
    }
    
    public boolean serverHasDescriptor(int id, String descriptor){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return false;
        }
        return server.hasDescriptor(descriptor);
    }
    
    public boolean serverIsRunning(int id){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return false;
        }
        return server.isRunning();
    }
    
    public boolean serverIsAdvertising(int id){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return false;
        }
        return server.isAdvertising();
    }
    
    public String[] serverGetServices(int id){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return new String[]{};
        }
        ArrayList<String> al = server.getServices();
        String[] a = new String[al.size()];
        return al.toArray(a);
    }
    
    public String[] serverGetCharacteristics(int id){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return new String[]{};
        }
        ArrayList<String> al = server.getCharacteristics();
        String[] a = new String[al.size()];
        return al.toArray(a);
    }
    
    public String[] serverGetDescriptors(int id){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return new String[]{};
        }
        ArrayList<String> al = server.getDescriptors();
        String[] a = new String[al.size()];
        return al.toArray(a);
    }
    
    public String[] serverGetAdvertiseServices(int id){
        BLEServer server = (BLEServer) objects.get(id);
        if(server == null){
            Log.e("GodotQuickBLE", "Unknown server/client id: " + id);
            return new String[]{};
        }
        ArrayList<String> al = server.getAdvertiseServices();
        String[] a = new String[al.size()];
        return al.toArray(a);
    }
      
    ///////////////////////////////////////////////////////////////////////
    /// Singleton Implementation
    ///////////////////////////////////////////////////////////////////////
    
    protected Activity appActivity;
    protected Context appContext;
    private int instanceId = 0;
    
    // Helper fuctions from 
    private void requestPermission(String permission, int requestCode) {
        if (!checkPermission(permission)) {
            ActivityCompat.requestPermissions(appActivity, new String[]{permission}, requestCode);
        }
    }

    private boolean checkPermission(String permission) {
        return ContextCompat.checkSelfPermission(appActivity, permission) == PackageManager.PERMISSION_GRANTED;
    }
    
    // Required for Bluetooth LE scanning on Android 6+
    public void requestLocationPerission(){
        requestPermission(Manifest.permission.ACCESS_COARSE_LOCATION, 73);
        
    }
    
    public boolean hasLocationPermission(){
        return checkPermission(Manifest.permission.ACCESS_COARSE_LOCATION);
    }
    
    public void assignInstanceId(int instanceId){
        this.instanceId = instanceId;
    }
    
    public int getInstanceId(){
        return instanceId;
    }
    
    public void runOnUiThread(Runnable runnable){
        appActivity.runOnUiThread(runnable);
    }
    
    static public Godot.SingletonBase initialize(Activity activity) {
        return new QuickBLESingleton(activity);
    }
    
    public QuickBLESingleton(Activity activity){
        // register class name and functions
        registerClass("QuickBLESingleton", functionList);
        
        this.appActivity = activity;
        this.appContext = appActivity.getApplicationContext();
        
    }
    
    // forwarded callbacks you can reimplement, as SDKs often need them

    protected void onMainActivityResult(int requestCode, int resultCode, Intent data) {}
    protected void onMainRequestPermissionsResult(int requestCode, String[] permissions, int[] grantResults) {
        if(requestCode == 73 && grantResults.length > 0 && permissions[0] == Manifest.permission.ACCESS_COARSE_LOCATION){
            GodotLib.calldeferred(getInstanceId(), "_locationPermissionResult", new Object[]{grantResults[0] == PackageManager.PERMISSION_GRANTED});
        }
    }

    protected void onMainPause() {}
    protected void onMainResume() {}
    protected void onMainDestroy() {}

    protected void onGLDrawFrame(GL10 gl) {}
    protected void onGLSurfaceChanged(GL10 gl, int width, int height) {} // singletons will always miss first onGLSurfaceChanged call
}