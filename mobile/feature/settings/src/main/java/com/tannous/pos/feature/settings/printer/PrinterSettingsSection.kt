package com.tannous.pos.feature.settings.printer

import android.Manifest
import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothManager
import android.content.pm.PackageManager
import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Print
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.core.content.ContextCompat
import com.tannous.pos.core.data.model.PrinterConnectionType
import com.tannous.pos.feature.settings.PrinterPrintState
import com.tannous.pos.feature.settings.SettingsViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PrinterSettingsSection(
    viewModel: SettingsViewModel,
    isArabic: Boolean,
    connectionType: PrinterConnectionType,
    bluetoothDeviceName: String?,
    bluetoothAddress: String?,
    networkHost: String,
    networkPort: String,
    paperWidthMm: Int,
    bondedDevices: List<Pair<String, String>>,
    showDeviceSheet: Boolean,
    printerPrintState: PrinterPrintState,
    onDismissDeviceSheet: () -> Unit,
    onShowDeviceSheet: () -> Unit,
    onClearBluetoothDevice: () -> Unit,
    onSelectDevice: (name: String, address: String) -> Unit,
    onConnectionTypeChange: (PrinterConnectionType) -> Unit,
    onNetworkHostChange: (String) -> Unit,
    onNetworkPortChange: (String) -> Unit,
    onPaperWidthChange: (Int) -> Unit,
    onPrintTest: () -> Unit,
    onClearPrintState: () -> Unit
) {
    val context = LocalContext.current
    var paperExpanded by remember { mutableStateOf(false) }
    var connectionExpanded by remember { mutableStateOf(false) }
    val sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)

    val permissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions()
    ) { grants ->
        if (grants.values.all { it }) {
            onShowDeviceSheet()
            viewModel.refreshBondedDevices()
        }
    }

    fun requestBluetoothAndScan() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            val connectGranted = ContextCompat.checkSelfPermission(
                context, Manifest.permission.BLUETOOTH_CONNECT
            ) == PackageManager.PERMISSION_GRANTED
            val scanGranted = ContextCompat.checkSelfPermission(
                context, Manifest.permission.BLUETOOTH_SCAN
            ) == PackageManager.PERMISSION_GRANTED
            if (connectGranted && scanGranted) {
                onShowDeviceSheet()
                viewModel.refreshBondedDevices()
            } else {
                permissionLauncher.launch(
                    arrayOf(
                        Manifest.permission.BLUETOOTH_CONNECT,
                        Manifest.permission.BLUETOOTH_SCAN
                    )
                )
            }
        } else {
            onShowDeviceSheet()
            viewModel.refreshBondedDevices()
        }
    }

    Card(modifier = Modifier.fillMaxWidth()) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(Icons.Default.Print, contentDescription = null, modifier = Modifier.size(24.dp))
                Spacer(modifier = Modifier.width(8.dp))
                Text(
                    text = if (isArabic) "طابعة الفواتير" else "Receipt Printer",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.SemiBold
                )
            }

            Text(
                text = if (isArabic) "نوع الاتصال" else "Connection",
                style = MaterialTheme.typography.labelMedium
            )
            ExposedDropdownMenuBox(
                expanded = connectionExpanded,
                onExpandedChange = { connectionExpanded = it }
            ) {
                val connectionLabel = when (connectionType) {
                    PrinterConnectionType.BLUETOOTH -> "Bluetooth"
                    PrinterConnectionType.NETWORK -> "LAN"
                    PrinterConnectionType.USB -> "USB (Coming Soon)"
                }
                OutlinedTextField(
                    value = connectionLabel,
                    onValueChange = {},
                    readOnly = true,
                    trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = connectionExpanded) },
                    modifier = Modifier
                        .menuAnchor()
                        .fillMaxWidth()
                )
                ExposedDropdownMenu(
                    expanded = connectionExpanded,
                    onDismissRequest = { connectionExpanded = false }
                ) {
                    DropdownMenuItem(
                        text = { Text("Bluetooth") },
                        onClick = {
                            onConnectionTypeChange(PrinterConnectionType.BLUETOOTH)
                            connectionExpanded = false
                        }
                    )
                    DropdownMenuItem(
                        text = { Text("LAN") },
                        onClick = {
                            onConnectionTypeChange(PrinterConnectionType.NETWORK)
                            connectionExpanded = false
                        }
                    )
                    DropdownMenuItem(
                        text = { Text("USB (Coming Soon)") },
                        onClick = { connectionExpanded = false },
                        enabled = false
                    )
                }
            }

            when (connectionType) {
                PrinterConnectionType.BLUETOOTH -> {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column(modifier = Modifier.weight(1f)) {
                            Text(
                                text = if (isArabic) "الجهاز" else "Device",
                                style = MaterialTheme.typography.labelMedium
                            )
                            Text(
                                text = when {
                                    bluetoothDeviceName != null && bluetoothAddress != null ->
                                        "$bluetoothDeviceName ($bluetoothAddress)"
                                    bluetoothAddress != null -> bluetoothAddress
                                    else -> if (isArabic) "لم يتم التعيين" else "Not configured"
                                },
                                style = MaterialTheme.typography.bodyMedium
                            )
                        }
                        if (bluetoothAddress != null) {
                            IconButton(onClick = onClearBluetoothDevice) {
                                Icon(Icons.Default.Close, contentDescription = "Clear device")
                            }
                        }
                    }
                    OutlinedButton(
                        onClick = { requestBluetoothAndScan() },
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Text(if (isArabic) "مسح طابعات Bluetooth" else "Scan for Bluetooth Printers")
                    }
                }
                PrinterConnectionType.NETWORK -> {
                    OutlinedTextField(
                        value = networkHost,
                        onValueChange = onNetworkHostChange,
                        label = { Text(if (isArabic) "المضيف" else "Host") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = networkPort,
                        onValueChange = onNetworkPortChange,
                        label = { Text(if (isArabic) "المنفذ" else "Port") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                }
                PrinterConnectionType.USB -> {
                    Text(
                        text = "USB printing coming in a future update.",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }

            ExposedDropdownMenuBox(
                expanded = paperExpanded,
                onExpandedChange = { paperExpanded = it }
            ) {
                OutlinedTextField(
                    value = "$paperWidthMm mm",
                    onValueChange = {},
                    readOnly = true,
                    label = { Text(if (isArabic) "عرض الورق" else "Paper width") },
                    trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = paperExpanded) },
                    modifier = Modifier
                        .menuAnchor()
                        .fillMaxWidth()
                )
                ExposedDropdownMenu(
                    expanded = paperExpanded,
                    onDismissRequest = { paperExpanded = false }
                ) {
                    listOf(58, 80).forEach { width ->
                        DropdownMenuItem(
                            text = { Text("$width mm") },
                            onClick = {
                                onPaperWidthChange(width)
                                paperExpanded = false
                            }
                        )
                    }
                }
            }

            OutlinedButton(
                onClick = onPrintTest,
                modifier = Modifier.fillMaxWidth(),
                enabled = printerPrintState !is PrinterPrintState.Printing
            ) {
                Text(if (isArabic) "طباعة فاتورة تجريبية" else "Print Test Receipt")
            }

            when (val state = printerPrintState) {
                is PrinterPrintState.Success -> {
                    Text(
                        text = state.message,
                        color = MaterialTheme.colorScheme.primary,
                        style = MaterialTheme.typography.bodySmall
                    )
                }
                is PrinterPrintState.Error -> {
                    Text(
                        text = state.message,
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodySmall
                    )
                }
                else -> Unit
            }
        }
    }

    if (showDeviceSheet) {
        ModalBottomSheet(
            onDismissRequest = onDismissDeviceSheet,
            sheetState = sheetState
        ) {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 8.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                Text(
                    text = if (isArabic) "الأجهزة المقترنة" else "Paired Bluetooth Printers",
                    style = MaterialTheme.typography.titleMedium
                )
                val adapter = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                    val manager = context.getSystemService(BluetoothManager::class.java)
                    manager?.adapter
                } else {
                    @Suppress("DEPRECATION")
                    BluetoothAdapter.getDefaultAdapter()
                }
                val devices = if (bondedDevices.isNotEmpty()) {
                    bondedDevices
                } else {
                    adapter?.bondedDevices?.map { (it.name ?: "Unknown") to it.address }.orEmpty()
                }
                if (devices.isEmpty()) {
                    Text(
                        text = if (isArabic) "لا توجد أجهزة مقترنة" else "No paired devices found",
                        style = MaterialTheme.typography.bodyMedium
                    )
                } else {
                    devices.forEach { (name, address) ->
                        Button(
                            onClick = {
                                onSelectDevice(name, address)
                                onDismissDeviceSheet()
                            },
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Text("$name\n$address")
                        }
                    }
                }
                Spacer(modifier = Modifier.size(24.dp))
            }
        }
    }
}
