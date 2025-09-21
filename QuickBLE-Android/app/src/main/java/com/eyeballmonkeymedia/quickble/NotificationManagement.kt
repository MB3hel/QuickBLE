package com.mb3hel.quickble

import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothGatt
import android.bluetooth.BluetoothGattCharacteristic
import android.bluetooth.BluetoothGattService
import android.os.Build
import android.support.annotation.RequiresApi
import android.util.Log
import java.lang.Exception
import java.util.concurrent.locks.ReentrantLock
import kotlin.concurrent.thread

/**
 * Android gatt server callback has onNotificationSent callback. Need to wait until it is called before sending another notification
 * otherwise some notifications may not be sent. A DelayedNotification is used to notify a single device of a single characteristic's
 * value. The QuickBLE delegate treats notifying a set of devices (all connected devices)
 * that a characteristic has changed as a single event, but android treats it as several different events. NotificationSets group
 * DelayedNotification events. When all notifications from a notificationSet are sent. Notifications from each set are
 * added to the NotificationCoordinator's buffer in the order of the devices array used to create the NotificationSet.
 * Notifications are sent in the order they were added to the queue. Each BLEServer has a NotificationCoordinator. When a notify
 * event is requested it queues a NotificationSet. The NotificationCoordinator ensures that each one is sent *after* the previous one
 * ensuring that all notifications are sent. The BluetoothGattServerCallback's onNotificationSent method calls the coordinator's
 * onNotificationSent method. This will sent the next notification (if there is one) and it will check if a notification set is complete.
 * If a notification set is complete (all notifications have been sent) the coordinator calls the BLEServer's handleNotificationSetSent
 * method which handles notifying the BLEDelegate.
 */


class ExitException: Exception() {}

class DelayedNotification(val characteristic: BluetoothGattCharacteristic, val device: BluetoothDevice?, val data: ByteArray?){}

class NotificationSet(val characteristic: BluetoothGattCharacteristic, val devices: Array<BluetoothDevice?>, val data: ByteArray?){
    var errorCount = 0
    val notifications: Array<DelayedNotification?> = arrayOfNulls(devices.size)
    init{
        for(i in devices.indices){
            notifications[i] = DelayedNotification(characteristic, devices[i], data)
        }
    }
}

@RequiresApi(api = Build.VERSION_CODES.LOLLIPOP)
class NotificationCoordinator(val server: BLEServer) {

    private val lock = ReentrantLock(true)
    private val buffer = ArrayList<DelayedNotification?>()
    private var currentNotification: DelayedNotification? = null
    private val notificationSets = ArrayList<NotificationSet>()

    fun queueNotifications(notificationSet: NotificationSet){
        // Do not queue if the server is not running
        if(!server.isRunning)
            return
        // Do not queue if there are no notifications to sent (no connected devices or only connected device was the one that change the char)
        if(notificationSet.notifications.isEmpty()) {
            // Still need to make sure the delegate method gets called
            notificationSet.errorCount = 1 // If errorCount is not 0 success=false
            server.handleNotificationSetSent(notificationSet)
            return
        }
        var shouldProcess = false
        lock.lock()
        try{
            buffer.addAll(notificationSet.notifications)
            notificationSets.add(notificationSet)
            shouldProcess = buffer.size == notificationSet.notifications.size
        }catch (e: Exception){
            Log.w("QuickBLE", "Exception queuing notification: ", e)
        }finally {
            lock.unlock()
        }
        if(shouldProcess)
            processNotifications()
    }

    fun serverStopped(){
        lock.lock()
        try {
            reset()
        }catch (e: Exception) {
        }finally {
            lock.unlock()
        }
    }

    /**
     * DO NOT CALL THIS WITHOUT ACQUIRING LOCK FIRST!!!!
     */
    private fun reset(){
        notificationSets.forEach {
            it.errorCount = it.notifications.size
            server.handleNotificationSetSent(it)
        }
        buffer.clear()
        currentNotification = null
        notificationSets.clear()
    }

    private fun processNotifications(){
        thread(start=true){
            var success: Boolean? = false
            lock.lock()
            // When using lock always use try/catch/finally to ensure that the lock is unlocked even in the case of an exception
            try{
                // If server is not running clear the notification buffer
                if(!server.isRunning){
                    reset()
                    throw ExitException() // This will ensure that the finally block still runs to unlock
                }

                if(buffer.size > 0 && buffer[0] != null){
                    currentNotification = buffer[0]
                    if (buffer[0] != null && server.gattServer != null && server.isRunning) {
                        val indicate = buffer[0]!!.characteristic.properties and BluetoothGattCharacteristic.PROPERTY_INDICATE == BluetoothGattCharacteristic.PROPERTY_INDICATE

                        // On android notifications are always of the current value. Get around this by temporarily replacing the value
                        // Synchronize so value can not change while doing this
                        synchronized(buffer[0]!!.characteristic){
                            val oldValue = buffer[0]!!.characteristic.value
                            buffer[0]!!.characteristic.value = buffer[0]!!.data
                            success = server.gattServer?.notifyCharacteristicChanged(buffer[0]!!.device, buffer[0]!!.characteristic, indicate)
                            buffer[0]!!.characteristic.value = oldValue
                        }
                    }
                    if (success != true) {
                        // Something was null and or notification could not be triggered
                        onNotificationSent(currentNotification!!.device, BluetoothGatt.GATT_FAILURE)
                    }
                }
            }catch (e: Exception){
                // ExitException is a custom exception used to "return" from the try block to ensure that unlock is still run in finally
                if(e !is ExitException)
                    Log.w("QuickBLE", "Exception processing notification: ", e)
            }finally {
                lock.unlock()
            }
        }
    }

    fun onNotificationSent(device: BluetoothDevice?, status: Int){
        lock.lock()
        try{
            if(notificationSets.size != 0 && buffer.size != 0 && currentNotification != null){
                // Increment error counter if needed
                if(status != BluetoothGatt.GATT_SUCCESS){
                    notificationSets[0].errorCount++
                }
                // If this is the last notification of the current set notify the server
                if(currentNotification == notificationSets[0].notifications.last()){
                    server.handleNotificationSetSent(notificationSets[0])
                    notificationSets.removeAt(0)
                }
                buffer.remove(currentNotification)
                currentNotification = null
            }
        }catch (e: Exception){
            Log.w("QuickBLE", "Exception when processing sent notification: ", e)
        }finally {
            lock.unlock()
            if(buffer.size > 0)
                processNotifications()
        }
    }
}