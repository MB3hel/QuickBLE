package com.mb3hel.quickble

import android.bluetooth.BluetoothAdapter
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.os.Handler

interface BtError {
    companion object {
        val None = 0
        val NoBluetooth = 1
        val NoBLE = 2
        val Disabled = 3
        val NoServer = 4
        val AlreadyRunning = 5
    }
}
interface AdvertiseError {
    companion object {
        val None = 0
        val DataTooLarge = 1
        val TooManyAdvertisers = 2
        val AlreadyStarted = 3
        val InternalError = 4
        val FeatureUnsupported = 5
    }
}
interface DescPermissions {
    companion object {
        val Read = 1
        val ReadEncrypted = 2
        val Write = 16
        val WriteEncrypted = 32
        val WriteSigned = 128
        val WriteSignedMitm = 256
    }
}
interface CharPermissions {
    companion object {
        val Read = 1
        val ReadEncrypted = 2
        val Write = 16
        val WriteEncrypted = 32
    }
}
interface CharProperties {
    companion object {
        val Broadcast = 1
        val ExtendedProps = 128
        val Indicate = 32
        val Notify = 16
        val Read = 2
        val SignedWrite = 64
        val Write = 8
        val WriteNoResponse = 4
    }
}
interface AdvertiseMode{
    companion object {
        val LowLatency = 1
        val Balanced = 1
        val LowPower = 0
    }
}
interface AdvertiseTxPower{
    companion object {
        val UltraLow = 0
        val Low = 1
        val Medium = 2
        val High = 3
    }
}
interface ScanMode{
    companion object {
        val Opportunistic = -1
        val LowPower = 0
        val Balanced = 1
        val LowLatency = 2
        val NotApplicable = -255
    }
}