package com.tannous.pos.core.data.model

import kotlinx.serialization.Serializable

@Serializable
enum class PrinterConnectionType {
    BLUETOOTH, USB, NETWORK
}

@Serializable
data class PrinterConfig(
    val connectionType: PrinterConnectionType = PrinterConnectionType.BLUETOOTH,
    val bluetoothAddress: String? = null,
    val bluetoothDeviceName: String? = null,
    val networkHost: String? = null,
    val networkPort: Int = 9100,
    val paperWidthMm: Int = 80
)
