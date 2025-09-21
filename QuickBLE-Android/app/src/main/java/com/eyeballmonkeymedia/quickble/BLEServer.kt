package com.mb3hel.quickble

import android.app.AlertDialog
import android.bluetooth.*
import android.bluetooth.le.AdvertiseCallback
import android.bluetooth.le.AdvertiseData
import android.bluetooth.le.AdvertiseSettings
import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import android.os.Handler
import android.os.Looper
import android.os.ParcelUuid
import android.support.annotation.RequiresApi
import android.util.Log
import java.util.*
import kotlin.collections.ArrayList
import kotlin.concurrent.thread

@RequiresApi(api = Build.VERSION_CODES.LOLLIPOP)
class BLEServer (val context: Context, val delegate: BLEDelegate){

    //region Variables and Properties

    private val notificationCoordinator = NotificationCoordinator(this)

    private val UNKNOWN_WRITING_DEVICE_ADDRESS = "unknown";

    // Platform Specific Objects
    private var serviceObjects = ArrayList<BluetoothGattService>()
    private var characteristicObjects = ArrayList<BluetoothGattCharacteristic>()
    private var descriptorObjects = ArrayList<BluetoothGattDescriptor>()
    // UUIDs for Gatt Objects
    /**
     * The services available on the server
     */
    var services = ArrayList<String>()
        private set
    /**
     * The characteristics available on the server
     */
    var characteristics = ArrayList<String>()
        private set
    /**
     * The descriptors available on the server
     */
    var descriptors = ArrayList<String>()
        private set
    /**
     * The services the server is advertising that it has (only these services can be scanned for pre-connect)
     */
    var advertiseServices = ArrayList<String>()
        private set

    // Platform Specific Options
    var advertiseMode = AdvertiseMode.Balanced
    var advertiseTxPower = AdvertiseTxPower.Medium
    var advertiseDeviceName = true
    var notifyChangingDevice = false
    var readInternalWrites = false

    // Status
    /**
     * Is the server running
     */
    var isRunning = false
        private set
    /**
     * Is the server being advertised
     */
    var isAdvertising = false
        private set

    // Android Specific Bluetooth Objects
    private val btManager = context.getSystemService(Context.BLUETOOTH_SERVICE) as BluetoothManager
    private val btAdapter = btManager.adapter
    private var btAdvertiser = btAdapter.bluetoothLeAdvertiser
    internal var gattServer: BluetoothGattServer? = null
    private val mainThread = Handler(Looper.getMainLooper())
    private var connectedDevices = ArrayList<BluetoothDevice>()

    // Enable BT Dialog Text
    var REQUEST_BT_TITLE = "Bluetooth Required"
    var REQUEST_BT_MESSAGE = "Enable bluetooth?"
    var REQUEST_BT_CONFIRM = "Yes"
    var REQUEST_BT_DENY = "No"

    //endregion

    /**
     * Create a new QuickBLE server for Gatt Peripheral role
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
                        btAdvertiser.stopAdvertising(advertiseCallback)
                    }
                    mainThread.post {
                        delegate.onBluetoothPowerChanged(state)
                    }
                }
                Thread.sleep(100)
            }
        }
    }


    //region Server Control
    /**
     * Add a primary service to the server
     * @param service The UUID of the service
     */
    fun addService(service: String){
        if(!services.contains(service.toUpperCase())){
            val s = BluetoothGattService(UUID.fromString(service), BluetoothGattService.SERVICE_TYPE_PRIMARY)
            services.add(service.toUpperCase())
            serviceObjects.add(s)
        }
    }
    /**
     * Add an included (secondary) service a service
     * @param service The UUID of the included service
     * @param parentService The UUID of the parent (primary) service
     * @return Was the service successfully added
     */
    fun addIncludedService(service: String, parentService: String): Boolean{
        if(!services.contains(service.toUpperCase()) && services.contains(parentService.toUpperCase())){
            val parent = getService(UUID.fromString(parentService))
            if(parent == null)
                return false
            val s = BluetoothGattService(UUID.fromString(service), BluetoothGattService.SERVICE_TYPE_SECONDARY)
            parent.addService(s)
            serviceObjects.add(s)
            services.add(service.toUpperCase())
            return true;
        }
        return false
    }
    /**
     * Add a characteristic to a service
     * @param characteristic The UUID of the characteristic
     * @param parentService The UUID of the parent service
     * @param properties The properties of the characteristic (CharProperties)
     * @param permissions The permissions of the characteristic (CharPermissions)
     * @return Was the characteristic successfully added
     */
    fun addCharacteristic(characteristic: String, parentService: String, properties: Int = CharProperties.Read or CharProperties.Write or CharProperties.Notify, permissions: Int = CharPermissions.Read or CharPermissions.Write): Boolean{
        if(!characteristics.contains(characteristic.toUpperCase()) && services.contains(parentService.toUpperCase())){
            val parent = getService(UUID.fromString(parentService))
            if(parent == null)
                return false
            // Add WriteSigned permission if SignedWrite property is set
            var perms = permissions;
            if((properties and CharProperties.SignedWrite) == CharProperties.SignedWrite){
                perms = perms or BluetoothGattCharacteristic.PROPERTY_SIGNED_WRITE
            }
            val c = BluetoothGattCharacteristic(UUID.fromString(characteristic), properties, perms)
            parent.addCharacteristic(c)
            characteristicObjects.add(c)
            characteristics.add(characteristic.toUpperCase())
            addDescriptor("00002902-0000-1000-8000-00805f9b34fb", characteristic, DescPermissions.Read or DescPermissions.Write) // Client Characteristic Config Descriptor needed for notifications on android
            return true
        }
        return false
    }
    /**
     * Add a descriptor to a characteristic
     * @param descriptor The UUID of the descriptor
     * @param parentCharacteristic The UUID of the parent characteristic
     * @param permissions The permissions of the descriptor (DescPermissions)
     * @return Was the descriptor successfully added
     */
    fun addDescriptor(descriptor: String, parentCharacteristic: String, permissions: Int = DescPermissions.Read or DescPermissions.Write): Boolean{
        if(!descriptors.contains(descriptor.toUpperCase()) && characteristics.contains(parentCharacteristic.toUpperCase())){
            val parent = getCharacteristic(UUID.fromString(parentCharacteristic))
            if(parent == null)
                return false
            val d = BluetoothGattDescriptor(UUID.fromString(descriptor), permissions)
            parent.addDescriptor(d)
            descriptorObjects.add(d)
            descriptors.add(descriptor.toUpperCase())
        }
        return false
    }

    /**
     * Advertise a service's UUID in the advertisement packet
     * @param service The Service to advertise
     * @param advertise Whether or not to advertise the UUID
     */
    fun advertiseService(service: String, advertise: Boolean){
        if(!advertiseServices.contains(service.toUpperCase()) && advertise){
            advertiseServices.add(service.toUpperCase())
        }
        if(advertiseServices.contains(service.toUpperCase()) && !advertise){
            advertiseServices.remove(service)
        }
    }

    /**
     * Remove all services, characteristics, and descriptors from the server
     */
    fun clearGatt(){
        stopServer()
        services.clear()
        characteristics.clear()
        descriptors.clear()
        advertiseServices.clear()
        serviceObjects.clear()
        characteristicObjects.clear()
        descriptorObjects.clear()
    }

    /**
     * Check bluetooth low energy server compatibility for the device
     * @return An error code (BtError)
     */
    fun checkBluetooth(): Int{
        if (!context.packageManager.hasSystemFeature(PackageManager.FEATURE_BLUETOOTH))
            return BtError.NoBluetooth
        if (!context.packageManager.hasSystemFeature(PackageManager.FEATURE_BLUETOOTH_LE))
            return BtError.NoBLE
        if (btAdapter == null || !btAdapter.isEnabled)
            return BtError.Disabled
        return if (!btAdapter.isMultipleAdvertisementSupported) BtError.NoServer else BtError.None
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
        builder.setNegativeButton(REQUEST_BT_DENY, { _, _ ->
            delegate.onBluetoothRequestResult(false)
        })
        builder.create().show()
    }
    /**
     * Start the server and start advertising
     * @return An error code (BtError)
     */
    fun startServer(): Int {
        btAdvertiser = btAdapter.bluetoothLeAdvertiser
        if(!isRunning) {
            if(isAdvertising){
                btAdapter.bluetoothLeAdvertiser?.stopAdvertising(advertiseCallback)
                isAdvertising = false
            }
            val error = checkBluetooth()
            if (error != BtError.None)
                return error
            gattServer = btManager.openGattServer(context, gattServerCallback)
            serviceObjects.asSequence().forEach {
                gattServer?.addService(it)
                Thread.sleep(100) // Prevent status 133 (GATT_ERROR) on some devices
            }
            btAdapter.bluetoothLeAdvertiser?.startAdvertising(buildAdvertiseSettings(), buildAdvertiseData(), advertiseCallback)
            isRunning = true
            isAdvertising = true
            return BtError.None
        }else{
            return BtError.AlreadyRunning
        }
    }
    /**
     * Stop the server and stop advertising
     */
    fun stopServer(){
        if(isRunning){
            // Don't close abruptly on the coordinator
            notificationCoordinator.serverStopped()
            connectedDevices.forEach {
                gattServer?.cancelConnection(it)
            }
            if(isAdvertising) {
                btAdapter.bluetoothLeAdvertiser?.stopAdvertising(advertiseCallback)
                isAdvertising = false
            }

            gattServer?.close()
            isRunning = false
        }
    }
    /**
     * Start advertising if the server is running
     */
    fun startAdvertising(){
        btAdvertiser = btAdapter.bluetoothLeAdvertiser
        if(isRunning && !isAdvertising){
            btAdapter.bluetoothLeAdvertiser?.startAdvertising(buildAdvertiseSettings(), buildAdvertiseData(), advertiseCallback)
            isAdvertising = true
        }
    }
    /**
     * Stop advertising if the server is running
     */
    fun stopAdvertising(){
        if(isRunning){
            btAdapter.bluetoothLeAdvertiser?.stopAdvertising(advertiseCallback)
            isAdvertising = false
        }
    }

    /**
     * Send a notification of a characteristic's value to a certain device
     * @param characteristic The characteristic to notify the value of
     * @param deviceAddress The address of the device to send the notification to
     */
    fun notifyDevice(characteristic: String, deviceAddress: String){
        val char = getCharacteristic(UUID.fromString(characteristic))
        if(char == null){
            return
        }
        val device = connectedDevices.firstOrNull { it.address.toUpperCase().equals(deviceAddress.toUpperCase())}
        if(device == null)
            return

        var d: ByteArray? = null
        synchronized(char) {
            d = Arrays.copyOf(char.value, char.value.size)
        }
        notificationCoordinator.queueNotifications(NotificationSet(char, arrayOf(device), d))

    }

    /**
     * Notify devices subscribed to a characteristic
     * @param characteristic The characteristic to notify the value of
     * @param device The device that just changed the characteristic
     */
    private fun notifyDevices(characteristic: BluetoothGattCharacteristic, device: BluetoothDevice?){
        val devices = ArrayList<BluetoothDevice?>()
        devices.addAll(connectedDevices)
        if(device != null)
            devices.remove(device)
        var d: ByteArray? = null
        synchronized(characteristic) {
            d = Arrays.copyOf(characteristic.value, characteristic.value.size)
        }
        notificationCoordinator.queueNotifications(NotificationSet(characteristic, devices.toTypedArray(), d))
    }
    /**
     * Build advertise settings based on options
     */
    private fun buildAdvertiseSettings(): AdvertiseSettings{
        return AdvertiseSettings.Builder().setAdvertiseMode(advertiseMode).setTxPowerLevel(advertiseTxPower).setConnectable(true).setTimeout(0).build()
    }
    /**
     * Build advertise data based on advertise services
     */
    private fun buildAdvertiseData(): AdvertiseData{
        val data = AdvertiseData.Builder()
        data.setIncludeDeviceName(advertiseDeviceName)

        advertiseServices.asSequence().forEach {
            data.addServiceUuid(ParcelUuid(UUID.fromString(it)))
        }
        return data.build()
    }
    //endregion

    //region Characteristics and Descriptors
    /**
     * Write a value to a characteristic
     * @param characteristic The characteristic to write
     * @param data The value to write (as bytes)
     * @param notify Whether or not to notify subscribed devices
     */
    fun writeCharacteristic(characteristic: String, data: ByteArray?, notify: Boolean = true){
        var success = false
        val char = getCharacteristic(UUID.fromString(characteristic))
        if(char != null){
            // Synchronized b/c notification may temporarily lock to send a custom value
            synchronized(char) {
                if (char.setValue(data)) {
                    success = true
                }
            }

            if(success && notify){
                notifyDevices(char, null)
            }
        }
        mainThread.post {
            val d = if(data == null) null else Arrays.copyOf(data, data.size)
            delegate.onCharacteristicWrite(characteristic.toUpperCase(), success, d)
            if(readInternalWrites){
                delegate.onCharacteristicRead(characteristic.toUpperCase(), UNKNOWN_WRITING_DEVICE_ADDRESS, success, d)
            }
        }
    }
    /**
     * Read a value from a characteristic
     * @param characteristic The characteristic to read
     */
    fun readCharacteristic(characteristic: String){
        val char = getCharacteristic(UUID.fromString(characteristic))
        if(char != null){
            // Synchronized b/c notification may temporarily lock to send a custom value
            var d: ByteArray? = null
            synchronized(char) {
                d = Arrays.copyOf(char.value, char.value.size)
            }

            mainThread.post {
                delegate.onCharacteristicRead(characteristic.toUpperCase(), UNKNOWN_WRITING_DEVICE_ADDRESS, true, d)
            }
        }else{
            mainThread.post {
                delegate.onCharacteristicRead(characteristic.toUpperCase(), UNKNOWN_WRITING_DEVICE_ADDRESS, false, null)
            }
        }
    }
    /**
     * Write a value to a descriptor
     * @param descriptor The descriptor to write
     * @param data The value to write (as bytes)
     */
    fun writeDescriptor(descriptor: String, data: ByteArray?){
        var success = false
        val desc = getDescriptor(UUID.fromString(descriptor))
        if(desc != null){
            if(desc.setValue(data)){
                success = true
            }
        }
        mainThread.post {
            val d = if(data == null) null else Arrays.copyOf(data, data.size)
            delegate.onDescriptorWrite(descriptor.toUpperCase(), success, d)
            if(readInternalWrites){
                delegate.onDescriptorRead(descriptor.toUpperCase(), UNKNOWN_WRITING_DEVICE_ADDRESS, true, d)
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
            val d = Arrays.copyOf(desc.value, desc.value.size)
            mainThread.post {
                delegate.onDescriptorRead(descriptor.toUpperCase(), UNKNOWN_WRITING_DEVICE_ADDRESS, true, d)
            }
        }else{
            mainThread.post {
                delegate.onDescriptorRead(descriptor.toUpperCase(), UNKNOWN_WRITING_DEVICE_ADDRESS,false, null)
            }
        }
    }

    internal fun handleNotificationSetSent(notificationSet: NotificationSet){
        mainThread.post{
            delegate.onNotificationSent(notificationSet.characteristic.uuid.toString().toUpperCase(), notificationSet.errorCount == 0)
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
        return services.contains(service.toUpperCase())
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

    //region BluetoothGattServerCallback
    private val gattServerCallback = object: BluetoothGattServerCallback(){
        override fun onNotificationSent(device: BluetoothDevice?, status: Int) {
            super.onNotificationSent(device, status)
            notificationCoordinator.onNotificationSent(device, status)
        }
        override fun onDescriptorReadRequest(device: BluetoothDevice?, requestId: Int, offset: Int, descriptor: BluetoothGattDescriptor?) {
            super.onDescriptorReadRequest(device, requestId, offset, descriptor)
            var status = BluetoothGatt.GATT_FAILURE
            var value: ByteArray? = null
            if(descriptor != null){
                val desc = getDescriptor(descriptor.uuid)
                if(desc != null){
                    status = BluetoothGatt.GATT_SUCCESS
                    value = desc.value
                }
            }
            thread(start = true){
                gattServer?.sendResponse(device, requestId, status, offset, value)
            }
        }
        override fun onDescriptorWriteRequest(device: BluetoothDevice?, requestId: Int, descriptor: BluetoothGattDescriptor?, preparedWrite: Boolean, responseNeeded: Boolean, offset: Int, value: ByteArray?) {
            super.onDescriptorWriteRequest(device, requestId, descriptor, preparedWrite, responseNeeded, offset, value)
            var status = BluetoothGatt.GATT_FAILURE
            if(descriptor != null){
                val desc = getDescriptor(descriptor.uuid)
                if(desc != null){
                    status = BluetoothGatt.GATT_SUCCESS
                    desc.value = value
                    val d = Arrays.copyOf(desc.value, desc.value.size)
                    mainThread.post {
                        delegate.onDescriptorRead(desc.uuid.toString().toUpperCase(), device!!.address.toUpperCase(), true, d)
                    }
                }
            }
            if(responseNeeded){
                thread(start = true){
                    gattServer?.sendResponse(device, requestId, status, offset, if(status == BluetoothGatt.GATT_SUCCESS) value else null)
                }
            }
        }
        override fun onCharacteristicWriteRequest(device: BluetoothDevice?, requestId: Int, characteristic: BluetoothGattCharacteristic?, preparedWrite: Boolean, responseNeeded: Boolean, offset: Int, value: ByteArray?) {
            super.onCharacteristicWriteRequest(device, requestId, characteristic, preparedWrite, responseNeeded, offset, value)
            var status = BluetoothGatt.GATT_FAILURE
            if(characteristic != null){
                val char = getCharacteristic(characteristic.uuid)
                if(char != null){
                    status = BluetoothGatt.GATT_SUCCESS
                    var d: ByteArray? = null
                    synchronized(char) {
                        char.value = value
                        d = Arrays.copyOf(char.value, char.value.size)
                    }

                    mainThread.post {
                        delegate.onCharacteristicRead(char.uuid.toString().toUpperCase(), device!!.address.toUpperCase(),true, d)
                    }
                    notifyDevices(char, device)
                }
            }
            if(responseNeeded){
                thread(start = true){
                    gattServer?.sendResponse(device, requestId, status, offset, if(status == BluetoothGatt.GATT_SUCCESS) value else null)
                }
            }
        }
        override fun onCharacteristicReadRequest(device: BluetoothDevice?, requestId: Int, offset: Int, characteristic: BluetoothGattCharacteristic?) {
            super.onCharacteristicReadRequest(device, requestId, offset, characteristic)
            var status = BluetoothGatt.GATT_FAILURE
            var value: ByteArray? = null
            if(characteristic != null){
                val char = getCharacteristic(characteristic.uuid)
                if(char != null){
                    status = BluetoothGatt.GATT_SUCCESS
                    synchronized(char) {
                        value = Arrays.copyOf(char.value, char.value.size)
                    }

                }
            }
            thread(start = true){
                gattServer?.sendResponse(device, requestId, status, offset, value)
            }
        }
        override fun onConnectionStateChange(device: BluetoothDevice?, status: Int, newState: Int) {
            super.onConnectionStateChange(device, status, newState)
            if(device != null){
                if(status == BluetoothGatt.GATT_SUCCESS){
                    if(!connectedDevices.contains(device) && newState == BluetoothGatt.STATE_CONNECTED){
                        connectedDevices.add(device)
                        mainThread.post {
                            delegate.onDeviceConnected(device.address.toUpperCase(), device.name)
                        }
                    }
                    if(connectedDevices.contains(device) && newState == BluetoothGatt.STATE_DISCONNECTED){
                        mainThread.post {
                            delegate.onDeviceDisconnected(device.address.toUpperCase(), device.name)
                        }
                        connectedDevices.remove(device)
                    }
                }else if(connectedDevices.contains(device)){
                    mainThread.post{
                        delegate.onDeviceDisconnected(device.address.toUpperCase(), device.name)
                    }
                    connectedDevices.remove(device)
                }
            }
        }
    }
    //endregion

    //region AdvertiseCallback
    private val advertiseCallback = object: AdvertiseCallback(){
        override fun onStartSuccess(settingsInEffect: AdvertiseSettings?) {
            super.onStartSuccess(settingsInEffect)
            mainThread.post {
                delegate.onAdvertise(AdvertiseError.None)
            }
        }

        override fun onStartFailure(errorCode: Int) {
            super.onStartFailure(errorCode)
            mainThread.post {
                delegate.onAdvertise(errorCode)
            }
        }
    }
    //endregion
}