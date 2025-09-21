package com.mb3hel.quickble

import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothGattCharacteristic
import android.bluetooth.BluetoothGattDescriptor
import android.bluetooth.BluetoothGattService

interface BLEDelegate{
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

    // Shared
    fun onCharacteristicRead(characteristic: String, writingDeviceAddress: String, success: Boolean, value: ByteArray?)
    fun onCharacteristicWrite(characteristic: String, success: Boolean, value: ByteArray?)
    fun onDescriptorRead(descriptor: String, writingDeviceAddress: String, success: Boolean, value: ByteArray?)
    fun onDescriptorWrite(descriptor: String, success: Boolean, value: ByteArray?)
    fun onBluetoothPowerChanged(enabled: Boolean)
    fun onBluetoothRequestResult(choseToEnable: Boolean)

}
