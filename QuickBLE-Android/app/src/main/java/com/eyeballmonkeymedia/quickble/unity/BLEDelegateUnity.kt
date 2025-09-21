package com.mb3hel.quickble.unity

import com.mb3hel.quickble.BLEDelegate

/**
 * This class and interface exist only to be used with Unity Game engine!
 * This class handles conversions of the BLEData class to the byte[]'s needed in unity
 * Unity's JNI bridge for Android does not handle byte[] when passed as function argument but it works with byte[] as property.
 * BLEData class contains a ByteArray
 *
 *
 * In Unity:
 * Create AndroidNativeProxy that extends to BLEDelegateUnity interface.
 * Create a UnityDelegate class instance as AndroidJavaObject with the proxy as an argument
 * Pass the UnityDelegate (AndroidJavaObject) to the native BLEServer
 */
class UnityDelegate(val unityDelegate: BLEDelegateUnity): BLEDelegate {

    // Server Specific
    override fun onAdvertise(error: Int){
        unityDelegate.onAdvertise(error)
    }
    override fun onDeviceConnected(address: String, name: String?){
        unityDelegate.onDeviceConnected(address, name)
    }
    override fun onDeviceDisconnected(address: String, name: String?){
        unityDelegate.onDeviceDisconnected(address, name)
    }

    override fun onNotificationSent(characteristic: String, success: Boolean) {
        unityDelegate.onNotificationSent(characteristic, success)
    }

    // Client Specific
    override fun onDeviceDiscovered(address: String, name: String?, rssi: Int){
        unityDelegate.onDeviceDiscovered(address, name, rssi)
    }
    override fun onConnectToDevice(address: String, name: String?, success: Boolean){
        unityDelegate.onConnectToDevice(address, name, success)
    }
    override fun onDisconnectFromDevice(address: String, name: String?){
        unityDelegate.onDisconnectFromDevice(address, name)
    }
    override fun onServicesDiscovered(){
        unityDelegate.onServicesDiscovered()
    }

    // Shared
    override fun onCharacteristicRead(characteristic: String, writingDeviceAddress: String, success: Boolean, value: ByteArray?){
        unityDelegate.onCharacteristicRead(characteristic, writingDeviceAddress, success, BLEData(value))
    }
    override fun onCharacteristicWrite(characteristic: String, success: Boolean, value: ByteArray?){
        unityDelegate.onCharacteristicWrite(characteristic, success, BLEData(value))
    }
    override fun onDescriptorRead(descriptor: String, writingDeviceAddress: String, success: Boolean, value: ByteArray?){
        unityDelegate.onDescriptorRead(descriptor, writingDeviceAddress, success, BLEData(value))
    }
    override fun onDescriptorWrite(descriptor: String, success: Boolean, value: ByteArray?){
        unityDelegate.onDescriptorWrite(descriptor, success, BLEData(value))
    }
    override fun onBluetoothPowerChanged(enabled: Boolean){
        unityDelegate.onBluetoothPowerChanged(enabled)
    }

    override fun onBluetoothRequestResult(choseToEnable: Boolean) {
        unityDelegate.onBluetoothRequestResult(choseToEnable);
    }
}

interface BLEDelegateUnity{
    // Server Specific
    fun onAdvertise(error: Int)
    fun onDeviceConnected(address: String, name: String?)
    fun onDeviceDisconnected(address: String, name: String?)
    fun onNotificationSent(characteristic: String, success: Boolean)

    // Client Specific
    fun onDeviceDiscovered(address: String, name: String?, rssi: Int)
    fun onConnectToDevice(address: String, name: String?, success: Boolean)
    fun onDisconnectFromDevice(address: String, name: String?)
    fun onServicesDiscovered()

    // Shared (The byte[] objects here have been changed to BLEData objects for compatibility reasons with untiy)
    fun onCharacteristicRead(characteristic: String, writingDeviceAddress: String, success: Boolean, value: BLEData)
    fun onCharacteristicWrite(characteristic: String, success: Boolean, value: BLEData)
    fun onDescriptorRead(descriptor: String, writingDeviceAddress: String, success: Boolean, value: BLEData)
    fun onDescriptorWrite(descriptor: String, success: Boolean, value: BLEData)
    fun onBluetoothPowerChanged(enabled: Boolean)
    fun onBluetoothRequestResult(choseToEnable: Boolean)
}