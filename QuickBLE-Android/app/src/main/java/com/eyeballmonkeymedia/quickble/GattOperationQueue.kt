package com.mb3hel.quickble

import android.bluetooth.BluetoothGatt
import android.bluetooth.BluetoothGattCharacteristic
import android.bluetooth.BluetoothGattDescriptor
import android.util.Log
import java.lang.Exception
import java.util.*
import java.util.concurrent.locks.ReentrantLock
import kotlin.concurrent.thread

/**
 * Android BluetoothGatt (Gatt clients) can only perform one GATT operation(read/write char/desc) at a time. No other
 * operation can be triggered until the current operation is complete (the callback's method is called).
 * In order to allow the BLEClient's read/write methods to be called *without* waiting for the callback a
 *                         queue is needed (similar to NotificationCoordinator for server)
 * The GattOperationQueue * queues DelayedGattOperations which have an OperandType (Char/Desc) and an OperationType (Read/Write).
 * When the client is told to perform an operation it will add it to its GattOperationQueue. The BLEClient's BluetoothGattCallback
 * will call the queue's read/write char/desc methods.
 * This will allow the queue to determine when an operation is complete. When an operation completes
 * the queue will perform the next operation (if any) and will call the client's handleGattOperationComplete method. This method
 * in the client will handle calling BLEDelegate methods as needed.
 */

enum class OperandType{
    Characteristic,
    Descriptor
}

enum class OperationType{
    Read,
    Write
}

class DelayedGattOperation(val operandType: OperandType, val operationType: OperationType, val operand: Any, var data: ByteArray?)

class GattOperationQueue(val client: BLEClient){
    private var lock = ReentrantLock(true)
    private var queue = ArrayList<DelayedGattOperation>()
    private var currentOperation: DelayedGattOperation? = null

    fun queueOperation(operation: DelayedGattOperation){
        if(!client.isConnected)
            return
        lock.lock()
        try{
            queue.add(operation)
            if(queue.size == 1)
                processOperations()
        }catch (e: Exception){
            Log.w("QuickBLE", "Queue Gatt operation exception: ", e)
        }finally {
            lock.unlock()
        }
    }

    /**
     * DO NOT CALL THIS WITHOUT LOCKING FIRST
     */
    private fun reset(){
        for(item in queue){
            client.handleGattOperationComplete(item, false)
        }
    }

    fun clientDisconnected(){
        lock.lock()
        try{
            reset()
        }catch (e: Exception){
            Log.w("QuickBLE", "Exception while handling client disconnect in queue: ", e)
        }finally {
            lock.unlock()
        }
    }

    private fun processOperations(){
        thread(start=true){
            lock.lock()
            try{
                //TODO: Check if still connected then perform the read/write operation
                if(client.isConnected){
                    currentOperation = queue[0]
                    if(currentOperation!!.operandType == OperandType.Characteristic){
                        // Characteristic
                        if(currentOperation!!.operationType == OperationType.Read){
                            // Read
                            client.gattConnection?.readCharacteristic(currentOperation!!.operand as BluetoothGattCharacteristic)
                        }else{
                            // Write
                            val char = currentOperation!!.operand as BluetoothGattCharacteristic
                            char.value = currentOperation!!.data
                            client.gattConnection?.writeCharacteristic(char)
                        }
                    }else{
                        // Descriptor
                        if(currentOperation!!.operationType == OperationType.Read){
                            // Read
                            client.gattConnection?.readDescriptor(currentOperation!!.operand as BluetoothGattDescriptor)
                        }else{
                            // Write
                            val desc = currentOperation!!.operand as BluetoothGattDescriptor
                            desc.value = currentOperation!!.data
                            client.gattConnection?.writeDescriptor(desc)
                        }
                    }
                }else{
                    reset()
                }
            }catch(e: Exception){

            }finally {
                lock.unlock()
            }
        }
    }

    fun onCharacteristicRead(characteristic: BluetoothGattCharacteristic?, status: Int){
        if(characteristic == null)
            return
        lock.lock()
        try {
            currentOperation!!.data = Arrays.copyOf(characteristic.value, characteristic.value.size)
            client.handleGattOperationComplete(currentOperation!!, status == BluetoothGatt.GATT_SUCCESS)
            queue.remove(currentOperation!!)
        }catch (e: Exception){
            Log.w("QuickBLE", "Exception while handling GATT operation complete.")
        }finally {
            lock.unlock()
            if(queue.size > 0)
                processOperations()
        }
    }

    fun onCharacteristicWrite(characteristic: BluetoothGattCharacteristic?, status: Int){
        lock.lock()
        try {
            client.handleGattOperationComplete(currentOperation!!, status == BluetoothGatt.GATT_SUCCESS)
            queue.remove(currentOperation!!)
        }catch (e: Exception){
            Log.w("QuickBLE", "Exception while handling GATT operation complete.")
        }finally {
            lock.unlock()
            if(queue.size > 0)
                processOperations()
        }
    }

    fun onDescriptorRead(descriptor: BluetoothGattDescriptor?, status: Int){
        if(descriptor == null)
            return
        lock.lock()
        try {
            currentOperation!!.data = Arrays.copyOf(descriptor.value, descriptor.value.size)
            client.handleGattOperationComplete(currentOperation!!, status == BluetoothGatt.GATT_SUCCESS)
            queue.remove(currentOperation!!)
        }catch (e: Exception){
            Log.w("QuickBLE", "Exception while handling GATT operation complete.")
        }finally {
            lock.unlock()
            if(queue.size > 0)
                processOperations()
        }
    }

    fun onDescriptorWrite(descriptor: BluetoothGattDescriptor?, status: Int){
        lock.lock()
        try {
            client.handleGattOperationComplete(currentOperation!!, status == BluetoothGatt.GATT_SUCCESS)
            queue.remove(currentOperation!!)
        }catch (e: Exception){
            Log.w("QuickBLE", "Exception while handling GATT operation complete.")
        }finally {
            lock.unlock()
            if(queue.size > 0)
                processOperations()
        }
    }
}