package com.mb3hel.quickble

import android.app.AlertDialog
import android.bluetooth.*
import android.bluetooth.le.ScanCallback
import android.bluetooth.le.ScanFilter
import android.bluetooth.le.ScanResult
import android.bluetooth.le.ScanSettings
import android.content.Context
import android.content.pm.PackageManager
import android.media.VolumeShaper
import android.os.Build
import android.os.Handler
import android.os.Looper
import android.os.ParcelUuid
import android.support.annotation.RequiresApi
import android.util.Log
import java.util.*
import kotlin.collections.ArrayList
import kotlin.concurrent.thread

@RequiresApi(api = Build.VERSION_CODES.JELLY_BEAN_MR2)
class BLEClient (val context: Context, val delegate: BLEDelegate, val useNewMethod: Boolean = true){

    //region Variables and Properties

    val gattOperationQueue = GattOperationQueue(this)

    private val UNKNOWN_WRITING_DEVICE_ADDRESS = "unknown";

    // Platform Specific Objects
    private var serviceObjects = ArrayList<BluetoothGattService>()
    private var characteristicObjects = ArrayList<BluetoothGattCharacteristic>()
    private var descriptorObjects = ArrayList<BluetoothGattDescriptor>()
    // UUIDs for Gatt Objects
    /**
     * The services available on the connected server
     */
    var services = ArrayList<String>()
        private set
    /**
     * The characteristics available on the connected server
     */
    var characteristics = ArrayList<String>()
        private set
    /**
     * The descriptors available on the connected server
     */
    var descriptors = ArrayList<String>()
        private set
    /**
     * The services that are being scanned for when scanning for devices
     */
    var scanServices = ArrayList<String>()
        private set

    // Platform Specific Options
    var scanMode = if(Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) ScanMode.Balanced else ScanMode.NotApplicable
        set(value) {
            if(useNewMethod){
                field = value
            }
        }

    // Status
    /**
     * Is the client scanning for servers
     */
    var isScanning = false
        private set
    /**
     * Is the client connected to a server
     */
    var isConnected = false
        get() = (gattConnection != null)
        private set

    // Keep track of detected devices
    private var deviceAddresses = ArrayList<String>()
    var devices = ArrayList<BluetoothDevice>()
        private set

    // Android Specific Bluetooth Objects
    private val mainThread = Handler(Looper.getMainLooper())
    private val btManager = context.getSystemService(Context.BLUETOOTH_SERVICE) as BluetoothManager
    private val btAdapter = btManager.adapter
    var gattConnection: BluetoothGatt? = null
        private set

    // Enable BT Dialog Text
    var REQUEST_BT_TITLE = "Bluetooth Required"
    var REQUEST_BT_MESSAGE = "Enable bluetooth?"
    var REQUEST_BT_CONFIRM = "Yes"
    var REQUEST_BT_DENY = "No"
    //endregion

    /**
     * Create a new QuickBLE Client for Gatt central role
     */
    init{
        // Watch for Bluetooth Power Changes
        thread(start = true){
            var lastState = false
            while(true){
                val state = btAdapter.isEnabled
                if(state != lastState){
                    lastState = state
                    if(!state){
                        stopScanning()
                        disconnect()
                    }
                    mainThread.post {
                        delegate.onBluetoothPowerChanged(state)
                    }
                }
                Thread.sleep(100)
            }
        }
    }

    //region Client Control
    /**
     * Scan for devices advertising a certain service (only available when using new scan method on Lolipop+ devices)
     * @param service The service to scan for
     * @param scanFor Whether or not to scan for the service
     */
    fun scanForService(service: String, scanFor: Boolean = true){
        if(scanFor && !scanServices.contains(service.toUpperCase())){
            scanServices.add(service.toUpperCase())
        }else if(!scanFor && scanServices.contains(service.toUpperCase())){
            scanServices.remove(service.toUpperCase())
        }
    }
    /**
     * Check bluetooth low energy client compatibility for the device
     * @return An error code (BtError)
     */
    fun checkBluetooth(): Int{
        if (!context.packageManager.hasSystemFeature(PackageManager.FEATURE_BLUETOOTH))
            return BtError.NoBluetooth
        if (!context.packageManager.hasSystemFeature(PackageManager.FEATURE_BLUETOOTH_LE))
            return BtError.NoBLE
        if (btAdapter == null || !btAdapter.isEnabled)
            return BtError.Disabled
        return BtError.None
    }
    /**
     * Show a dialog requesting that the user enable bluetooth
     */
    fun requestEnableBt(){
        val builder = AlertDialog.Builder(context, AlertDialog.THEME_DEVICE_DEFAULT_LIGHT)
        builder.setTitle(REQUEST_BT_TITLE).setMessage(REQUEST_BT_MESSAGE)
        builder.setPositiveButton(REQUEST_BT_CONFIRM, { _, _ ->
            btAdapter.enable()
            delegate.onBluetoothRequestResult(true)
        })
        builder.setNegativeButton(REQUEST_BT_DENY, {_, _  ->
            delegate.onBluetoothRequestResult(false)
        })
        builder.create().show()
    }

    /**
     * Start scanning for devices
     * @return An error code (BtError)
     */
    fun scanForDevices(): Int{
        if(!isScanning && !isConnected){
            services.clear()
            characteristics.clear()
            descriptors.clear()
            serviceObjects.clear()
            characteristicObjects.clear()
            descriptorObjects.clear()
            val error = checkBluetooth()
            if(error != BtError.None){
                return error
            }
            if(Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP && useNewMethod){
                val settings = ScanSettings.Builder().setScanMode(scanMode).build()
                val filters = ArrayList<ScanFilter>()
                scanServices.forEach {
                    filters.add(ScanFilter.Builder().setServiceUuid(ParcelUuid(UUID.fromString(it))).build())
                }
                btAdapter.bluetoothLeScanner.startScan(filters, settings, scanCallback)

            }else{
                if(scanServices.size > 0){
                    val uuids = ArrayList<UUID>()
                    scanServices.forEach {
                        uuids.add(UUID.fromString(it))
                    }
                    btAdapter.startLeScan(uuids.toTypedArray(), leScanCallback)
                }else {
                    btAdapter.startLeScan(leScanCallback)
                }
            }
            isScanning = true
            return BtError.None
        }else{
            return BtError.AlreadyRunning
        }
    }
    /**
     * Stop scanning
     */
    fun stopScanning(){
        if(isScanning){
            if(Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP && useNewMethod){
                btAdapter.bluetoothLeScanner.stopScan(scanCallback)
            }else{
                btAdapter.stopLeScan(leScanCallback)
            }
            isScanning = false
        }
    }

    /**
     * Connect to a specified device
     * @param deviceAddress The address of the device to connect to
     */
    fun connectToDevice(deviceAddress: String){
        if(gattConnection != null){
            disconnect()
        }
        gattConnection = devices.firstOrNull { it.address.toUpperCase().equals(deviceAddress.toUpperCase()) }?.connectGatt(context, false, bluetoothGattCallback)
        gattConnection?.connect()
    }
    /**
     * Disconnect from server if connected
     */
    fun disconnect(){
        if(isConnected){
            services.clear()
            characteristics.clear()
            descriptors.clear()
            serviceObjects.clear()
            characteristicObjects.clear()
            descriptorObjects.clear()
            gattConnection?.disconnect()
            gattConnection = null
            isConnected = false
        }
    }

    /**
     * Subscribe to a characteristic to receive notifications when is's value is changed
     * @param characteristic The characteristic to subscribe to
     * @param subscribe Whether not to subscribe to the characteristic (false to unsubscribe)
     */
    fun subscribeToCharacteristic(characteristic: String, subscribe: Boolean = true) {
        val char = getCharacteristic(UUID.fromString(characteristic))
        if(char != null){
            gattConnection?.setCharacteristicNotification(char, subscribe)
            val descriptor = char.getDescriptor(UUID.fromString("00002902-0000-1000-8000-00805f9b34fb")) // Client characteristic config UUID
            if(descriptor != null){
                descriptor.value = if(subscribe) BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE else BluetoothGattDescriptor.DISABLE_NOTIFICATION_VALUE
                gattOperationQueue.queueOperation(DelayedGattOperation(OperandType.Descriptor, OperationType.Write, descriptor, descriptor.value))
            }
        }
    }

    /**
     * Add a service (and it's included services, characteristics, and descriptors) to the lists
     * @param service The service to add
     */
    private fun addService(service: BluetoothGattService){
        if(!serviceObjects.contains(service)){
            if(service.type == BluetoothGattService.SERVICE_TYPE_PRIMARY){
                service.includedServices.asSequence().filterNot { serviceObjects.contains(it) }.forEach {
                    addService(it)
                }
            }
            service.characteristics.asSequence().filterNot { characteristicObjects.contains(it) }.forEach {
                it.descriptors.asSequence().filterNot { descriptorObjects.contains(it) }.forEach {
                    descriptorObjects.add(it)
                    descriptors.add(it.uuid.toString().toUpperCase())
                }
                characteristicObjects.add(it)
                characteristics.add(it.uuid.toString().toUpperCase())
            }
            serviceObjects.add(service)
            services.add(service.uuid.toString().toUpperCase())
        }
    }
    //endregion

    //region Characteristics and Descriptors
    /**
     * Read a value from a characteristic
     * @param characteristic The characteristic to read
     */
    fun readCharacteristic(characteristic: String){
        val char = getCharacteristic(UUID.fromString(characteristic))
        if(char != null){
            gattOperationQueue.queueOperation(DelayedGattOperation(OperandType.Characteristic, OperationType.Read, char, null))
        }else{
            mainThread.post {
                delegate.onCharacteristicRead(characteristic.toUpperCase(), "", false, null)
            }
        }
    }
    /**
     * Write a value to a characteristic
     * @param characteristic The characteristic to write
     * @param data The value to write (as bytes)
     */
    fun writeCharacteristic(characteristic: String, data: ByteArray?){
        val char = getCharacteristic(UUID.fromString(characteristic))
        if(char != null){
            gattOperationQueue.queueOperation(DelayedGattOperation(OperandType.Characteristic, OperationType.Write, char, data))
        }else{
            mainThread.post {
                delegate.onCharacteristicWrite(characteristic.toUpperCase(), false, null)
            }
        }
    }
    /**
     * Read a value from a descriptor
     * @param descriptor The descriptor to read
     */
    fun readDescriptor(descriptor: String){
        val desc = getDescriptor(UUID.fromString(descriptor))
        if(desc != null){
            gattOperationQueue.queueOperation(DelayedGattOperation(OperandType.Descriptor, OperationType.Read, desc, null))
        }else{
            mainThread.post {
                delegate.onDescriptorRead(descriptor.toUpperCase(), UNKNOWN_WRITING_DEVICE_ADDRESS, false, null)
            }
        }
    }
    /**
     * Write a value to a descriptor
     * @param descriptor The descriptor to write
     * @param data The value to write (as bytes)
     */
    fun writeDescriptor(descriptor: String, data: ByteArray?){
        val desc = getDescriptor(UUID.fromString(descriptor))
        if(desc != null){
            gattOperationQueue.queueOperation(DelayedGattOperation(OperandType.Descriptor, OperationType.Write, desc, data))
        }else{
            mainThread.post {
                delegate.onDescriptorWrite(descriptor.toUpperCase(), false, null)
            }
        }
    }

    fun handleGattOperationComplete(operation: DelayedGattOperation, success: Boolean){
        if(operation.operandType == OperandType.Characteristic){
            // Characteristic
            if(operation.operationType == OperationType.Read){
                // Read
                mainThread.post {
                    delegate.onCharacteristicRead((operation.operand as BluetoothGattCharacteristic).uuid.toString().toUpperCase(), UNKNOWN_WRITING_DEVICE_ADDRESS, success, operation.data)
                }
            }else{
                // Write
                mainThread.post {
                    delegate.onCharacteristicWrite((operation.operand as BluetoothGattCharacteristic).uuid.toString().toUpperCase(), success, operation.data)
                }
            }
        }else{
            // Descriptor
            if(operation.operationType == OperationType.Read){
                // Read
                mainThread.post {
                    delegate.onDescriptorRead((operation.operand as BluetoothGattDescriptor).uuid.toString().toUpperCase(), UNKNOWN_WRITING_DEVICE_ADDRESS, success, operation.data)
                }
            }else{
                // Write
                mainThread.post {
                    delegate.onDescriptorWrite((operation.operand as BluetoothGattDescriptor).uuid.toString().toUpperCase(), success, operation.data)
                }
            }
        }
    }

    /**
     * Get a descriptor object from a UUID
     * @return The BluetoothGattDescriptor or null
     */
    private fun getDescriptor(uuid: UUID): BluetoothGattDescriptor?{
        return descriptorObjects.asSequence().firstOrNull { it.uuid == uuid }
    }
    /**
     * Get a characteristic object from a UUID
     * @return The BluetoothGattCharacteristic or null
     */
    private fun getCharacteristic(uuid: UUID): BluetoothGattCharacteristic?{
        return characteristicObjects.asSequence().firstOrNull { it.uuid == uuid }
    }
    /**
     * Get a service object from a UUID
     * @return The BluetoothGattService or null
     */
    private fun getService(uuid: UUID): BluetoothGattService?{
        return serviceObjects.asSequence().firstOrNull { it.uuid == uuid }
    }

    /**
     * Check if the server has a service
     * @param service The service to search for
     * @return Whether or not the server has the service
     */
    fun hasService(service: String): Boolean{
        return services.contains(service.toUpperCase());
    }
    /**
     * Check if the server has a characteristic
     * @param characteristic The characteristic to search for
     * @return Whether or not the server has the characteristic
     */
    fun hasCharacteristic(characteristic: String): Boolean{
        return characteristics.contains(characteristic.toUpperCase());
    }
    /**
     * Check if the server has a descriptor
     * @param descriptor The descriptor to search for
     * @return Whether or not the server has the descriptor
     */
    fun hasDescriptor(descriptor: String): Boolean{
        return descriptors.contains(descriptor.toUpperCase());
    }

    //endregion

    //region LeScanCallback (Old Method)
    private val leScanCallback = BluetoothAdapter.LeScanCallback { device, rssi, scanRecord ->
        if(device != null){
            if(!deviceAddresses.contains(device.address)) {
                devices.add(device)
                deviceAddresses.add(device.address)
            }
            mainThread.post {
                delegate.onDeviceDiscovered(device.address.toUpperCase(), device.name, rssi)
            }
        }
    }
    //endregion

    //region ScanCallback (New Method)
    private val scanCallback = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP)
        object:ScanCallback(){
            @RequiresApi(api = Build.VERSION_CODES.LOLLIPOP)
            override fun onScanFailed(errorCode: Int) {
                super.onScanFailed(errorCode)
            }
            @RequiresApi(api = Build.VERSION_CODES.LOLLIPOP)
            override fun onScanResult(callbackType: Int, result: ScanResult?) {
                super.onScanResult(callbackType, result)
                if(result != null){
                    if(!deviceAddresses.contains(result.device.address)) {
                        devices.add(result.device)
                        deviceAddresses.add(result.device.address)
                    }
                    mainThread.post {
                        delegate.onDeviceDiscovered(result.device.address.toUpperCase(), result.device.name, result.rssi)
                    }
                }
            }
            @RequiresApi(api = Build.VERSION_CODES.LOLLIPOP)
            override fun onBatchScanResults(results: MutableList<ScanResult>?) {
                super.onBatchScanResults(results)
                if(results != null){
                    results.asSequence().forEach {
                        devices.add(it.device)
                        mainThread.post {
                            delegate.onDeviceDiscovered(it.device.address.toUpperCase(), it.device.name, it.rssi)
                        }
                    }
                }
            }
        }
    else null
    //endregion

    //region BluetoothGattCallback
    private val bluetoothGattCallback = object:BluetoothGattCallback(){
        override fun onCharacteristicRead(gatt: BluetoothGatt?, characteristic: BluetoothGattCharacteristic?, status: Int) {
            super.onCharacteristicRead(gatt, characteristic, status)
            gattOperationQueue.onCharacteristicRead(characteristic, status)
        }
        override fun onCharacteristicWrite(gatt: BluetoothGatt?, characteristic: BluetoothGattCharacteristic?, status: Int) {
            super.onCharacteristicWrite(gatt, characteristic, status)
            gattOperationQueue.onCharacteristicWrite(characteristic, status)
        }
        override fun onServicesDiscovered(gatt: BluetoothGatt?, status: Int) {
            super.onServicesDiscovered(gatt, status)
            if(gatt != null){
                gatt.services.asSequence().forEach {
                    addService(it)
                }
                mainThread.post{
                    delegate.onServicesDiscovered()
                }
            }
        }
        override fun onDescriptorWrite(gatt: BluetoothGatt?, descriptor: BluetoothGattDescriptor?, status: Int) {
            super.onDescriptorWrite(gatt, descriptor, status)
            gattOperationQueue.onDescriptorWrite(descriptor, status)
        }
        override fun onCharacteristicChanged(gatt: BluetoothGatt?, characteristic: BluetoothGattCharacteristic?) {
            super.onCharacteristicChanged(gatt, characteristic)
            if(characteristic != null){
                val d = Arrays.copyOf(characteristic.value, characteristic.value.size)
                mainThread.post {
                    delegate.onCharacteristicRead(characteristic.uuid.toString().toUpperCase(), UNKNOWN_WRITING_DEVICE_ADDRESS, true, d)
                }
            }
        }
        override fun onDescriptorRead(gatt: BluetoothGatt?, descriptor: BluetoothGattDescriptor?, status: Int) {
            super.onDescriptorRead(gatt, descriptor, status)
            gattOperationQueue.onDescriptorRead(descriptor, status)
        }
        override fun onConnectionStateChange(gatt: BluetoothGatt?, status: Int, newState: Int) {
            super.onConnectionStateChange(gatt, status, newState)
            if(gatt != null && gatt == gattConnection){
                if(newState == BluetoothProfile.STATE_CONNECTED){
                    gatt.discoverServices()
                    mainThread.post {
                        delegate.onConnectToDevice(gatt.device.address.toUpperCase(), gatt.device.name, true)
                    }
                    isConnected = true
                }
                if(newState == BluetoothProfile.STATE_DISCONNECTED){
                    services.clear()
                    characteristics.clear()
                    descriptors.clear()
                    gattOperationQueue.clientDisconnected()
                    mainThread.post {
                        delegate.onDisconnectFromDevice(gatt.device.address.toUpperCase(), gatt.device.name)
                    }
                    isConnected = false
                }
            }
        }
    }
    //endregion

}