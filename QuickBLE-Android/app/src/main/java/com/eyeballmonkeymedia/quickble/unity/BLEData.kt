package com.mb3hel.quickble.unity

import java.nio.ByteBuffer
import java.nio.ByteOrder

/**
 * This is a wrapper class for use with unity only
 *
 * This is wrapped like this for Unity compatibility. Java byte[] objects cannot be passed to unity via interfaces, but work as properties or as return types
 */
class BLEData (var data: ByteArray?){}