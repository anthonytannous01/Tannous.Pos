package com.tannous.pos.feature.settings

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.ArrowForward
import androidx.compose.material.icons.filled.List
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.Person
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.TextButton
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.repository.SettingsRepository
import com.tannous.pos.core.ui.LanguageViewModel
import com.tannous.pos.core.ui.LocalIsArabic
import com.tannous.pos.feature.settings.printer.PrinterSettingsSection

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun LebanonPresetDialog(
    onConfirm: () -> Unit,
    onDismiss: () -> Unit
) {
    val isArabic = LocalIsArabic.current
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(if (isArabic) "إعداد لبنان السريع" else "Lebanon Quick Setup") },
        text = {
            Text(
                if (isArabic)
                    "سيتم ضبط:\n• ضريبة القيمة المضافة = 11%\n• عرض الليرة اللبنانية على الفواتير = مفعّل\n• رسوم الطابع (2 دولار) = مفعّل\n\nستحتاج إلى إدخال سعر الصرف الحالي يدوياً."
                else
                    "This will set:\n• VAT = 11%\n• Show LBP on receipts = ON\n• Stamp duty (\$2 USD) = ON\n\nYou will still need to enter the current exchange rate manually."
            )
        },
        confirmButton = {
            TextButton(onClick = {
                onConfirm()
                onDismiss()
            }) { Text(if (isArabic) "تطبيق" else "Apply") }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text(if (isArabic) "إلغاء" else "Cancel") }
        }
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreen(
    onNavigateBack: () -> Unit,
    onNavigateToPrintingPreview: () -> Unit,
    onNavigateToReports: () -> Unit,
    onNavigateToOrderHistory: () -> Unit,
    onNavigateToInventory: () -> Unit,
    onNavigateToKds: () -> Unit = {},
    onNavigateToDashboard: () -> Unit = {},
    onNavigateToMenuEngineering: () -> Unit = {},
    onNavigateToTables: () -> Unit = {},
    onNavigateToQrMenu: () -> Unit = {},
    onNavigateToReservations: () -> Unit = {},
    onNavigateToDelivery: () -> Unit = {},
    onNavigateToKiosk: () -> Unit = {},
    onNavigateToLoyaltyCrm: () -> Unit = {},
    onNavigateToSchedule: () -> Unit = {},
    onNavigateToAccounting: () -> Unit = {},
    onNavigateToIntegrations: () -> Unit = {},
    onNavigateToMenuManagement: () -> Unit = {},
    onNavigateToTableManagement: () -> Unit = {},
    viewModel: SettingsViewModel = hiltViewModel(),
    languageViewModel: LanguageViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic = LocalIsArabic.current
    val snackbarHostState = remember { SnackbarHostState() }
    val scrollState = rememberScrollState()
    var showLebanonPresetDialog by remember { mutableStateOf(false) }

    if (showLebanonPresetDialog) {
        LebanonPresetDialog(
            onConfirm = { viewModel.applyLebanonPreset() },
            onDismiss = { showLebanonPresetDialog = false }
        )
    }

    LaunchedEffect(uiState.error) {
        uiState.error?.let { message ->
            snackbarHostState.showSnackbar(message, duration = SnackbarDuration.Long)
            viewModel.clearError()
        }
    }

    LaunchedEffect(uiState.saveSuccess) {
        if (uiState.saveSuccess) {
            snackbarHostState.showSnackbar(if (isArabic) "تم حفظ الإعدادات" else "Settings saved")
            viewModel.clearError()
        }
    }

    LaunchedEffect(uiState.printerPrintState) {
        when (val state = uiState.printerPrintState) {
            is PrinterPrintState.Success -> {
                snackbarHostState.showSnackbar(state.message)
                viewModel.clearPrinterPrintState()
            }
            is PrinterPrintState.Error -> {
                snackbarHostState.showSnackbar(state.message, duration = SnackbarDuration.Long)
                viewModel.clearPrinterPrintState()
            }
            else -> Unit
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "الإعدادات" else "Settings") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = if (isArabic) "رجوع" else "Back")
                    }
                }
            )
        }
    ) { paddingValues ->
        when {
            uiState.isLoading -> {
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(paddingValues),
                    contentAlignment = Alignment.Center
                ) {
                    CircularProgressIndicator()
                }
            }
            else -> {
                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(paddingValues)
                        .verticalScroll(scrollState)
                        .padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    if (uiState.failedSyncCount > 0) {
                        Card(
                            modifier = Modifier.fillMaxWidth(),
                            colors = CardDefaults.cardColors(
                                containerColor = MaterialTheme.colorScheme.errorContainer
                            )
                        ) {
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(16.dp),
                                verticalAlignment = Alignment.CenterVertically,
                                horizontalArrangement = Arrangement.spacedBy(12.dp)
                            ) {
                                Icon(
                                    imageVector = Icons.Default.Info,
                                    contentDescription = null,
                                    tint = MaterialTheme.colorScheme.onErrorContainer
                                )
                                Column(modifier = Modifier.weight(1f)) {
                                    Text(
                                        text = if (isArabic) "${uiState.failedSyncCount} عملية مزامنة فشلت" else "${uiState.failedSyncCount} sync operation(s) failed",
                                        style = MaterialTheme.typography.bodyMedium,
                                        fontWeight = FontWeight.Medium,
                                        color = MaterialTheme.colorScheme.onErrorContainer
                                    )
                                    Text(
                                        text = if (isArabic) "لم يتم إرسال بعض العمليات إلى الخادم. تواصل مع الدعم إذا استمرت المشكلة." else "Some adjustments or operations could not be sent to the server. Contact support if this persists.",
                                        style = MaterialTheme.typography.bodySmall,
                                        color = MaterialTheme.colorScheme.onErrorContainer
                                    )
                                }
                            }
                        }
                    }

                    // Language toggle
                    Card(modifier = Modifier.fillMaxWidth()) {
                        Row(
                            modifier = Modifier.fillMaxWidth().padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Column {
                                Text("Language / اللغة",
                                    style = MaterialTheme.typography.bodyLarge,
                                    fontWeight = androidx.compose.ui.text.font.FontWeight.SemiBold)
                                Text(
                                    if (uiState.language == "ar") "العربية" else "English",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                            }
                            Switch(
                                checked = uiState.language == "ar",
                                onCheckedChange = {
                                    // Capture new value before the async toggle writes to DB,
                                    // so we avoid a write/read race with an async refresh.
                                    val newIsArabic = uiState.language != SettingsRepository.LANG_AR
                                    viewModel.toggleLanguage()
                                    languageViewModel.setIsArabic(newIsArabic)
                                }
                            )
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToReports
                    ) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(
                                Icons.Default.List,
                                contentDescription = null,
                                modifier = Modifier.size(24.dp)
                            )
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "التقارير" else "Reports")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(
                                Icons.Default.ArrowForward,
                                contentDescription = null
                            )
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToMenuManagement
                    ) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(
                                Icons.Default.List,
                                contentDescription = null,
                                modifier = Modifier.size(24.dp)
                            )
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "إدارة القائمة" else "Manage Menu")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(Icons.Default.ArrowForward, contentDescription = null)
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToOrderHistory
                    ) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(
                                Icons.Default.List,
                                contentDescription = null,
                                modifier = Modifier.size(24.dp)
                            )
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "سجل الطلبات" else "Order History")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(
                                Icons.Default.ArrowForward,
                                contentDescription = null
                            )
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToInventory
                    ) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(
                                Icons.Default.List,
                                contentDescription = null,
                                modifier = Modifier.size(24.dp)
                            )
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "المخزون" else "Inventory")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(
                                Icons.Default.ArrowForward,
                                contentDescription = null
                            )
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToPrintingPreview
                    ) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(
                                Icons.Default.Info,
                                contentDescription = null,
                                modifier = Modifier.size(24.dp)
                            )
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "معاينة الطباعة" else "Printing Preview")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(
                                Icons.Default.ArrowForward,
                                contentDescription = null
                            )
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToDashboard
                    ) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(
                                Icons.Default.List,
                                contentDescription = null,
                                modifier = Modifier.size(24.dp)
                            )
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "لوحة المبيعات" else "Sales Dashboard")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(Icons.Default.ArrowForward, contentDescription = null)
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToMenuEngineering
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth().padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(Icons.Default.List, contentDescription = null,
                                modifier = Modifier.size(24.dp))
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "هندسة القائمة" else "Menu Engineering")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(Icons.Default.ArrowForward, contentDescription = null)
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToTables
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth().padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(Icons.Default.List, contentDescription = null,
                                modifier = Modifier.size(24.dp))
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "خريطة الطاولات" else "Table Map")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(Icons.Default.ArrowForward, contentDescription = null)
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToTableManagement
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth().padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(Icons.Default.List, contentDescription = null,
                                modifier = Modifier.size(24.dp))
                            Spacer(modifier = Modifier.width(16.dp))
                            Column(modifier = Modifier.weight(1f)) {
                                Text(if (isArabic) "إدارة الطاولات" else "Configure Tables")
                                Text(
                                    if (isArabic) "إنشاء مناطق وطاولات" else "Create floor plans and tables",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                            }
                            Icon(Icons.Default.ArrowForward, contentDescription = null)
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToKds
                    ) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(
                                Icons.Default.List,
                                contentDescription = null,
                                modifier = Modifier.size(24.dp)
                            )
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "شاشة المطبخ (KDS)" else "Kitchen Display (KDS)")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(
                                Icons.Default.ArrowForward,
                                contentDescription = null
                            )
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToQrMenu
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth().padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(Icons.Default.Info, contentDescription = null,
                                modifier = Modifier.size(24.dp))
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "القائمة الرقمية (QR)" else "Digital Menu (QR)")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(Icons.Default.ArrowForward, contentDescription = null)
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToReservations
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth().padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(Icons.Default.List, contentDescription = null,
                                modifier = Modifier.size(24.dp))
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "الحجوزات" else "Reservations")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(Icons.Default.ArrowForward, contentDescription = null)
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToDelivery
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth().padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(Icons.Default.List, contentDescription = null,
                                modifier = Modifier.size(24.dp))
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "قائمة التوصيل" else "Delivery Queue")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(Icons.Default.ArrowForward, contentDescription = null)
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToKiosk
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth().padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(Icons.Default.Info, contentDescription = null,
                                modifier = Modifier.size(24.dp))
                            Spacer(modifier = Modifier.width(16.dp))
                            Column {
                                Text(if (isArabic) "كشك الطلب الذاتي" else "Self-Ordering Kiosk")
                                Text(if (isArabic) "رمز الخروج: ${uiState.kioskPin}" else "Exit PIN: ${uiState.kioskPin}",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant)
                            }
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(Icons.Default.ArrowForward, contentDescription = null)
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToLoyaltyCrm
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth().padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(Icons.Default.Person, contentDescription = null,
                                modifier = Modifier.size(24.dp))
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "برنامج الولاء" else "Loyalty CRM")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(Icons.Default.ArrowForward, contentDescription = null)
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToSchedule
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth().padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(Icons.Default.Person, contentDescription = null,
                                modifier = Modifier.size(24.dp))
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "جدول الموظفين وساعات العمل" else "Staff Schedule & Time Clock")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(Icons.Default.ArrowForward, contentDescription = null)
                        }
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToAccounting
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth().padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(Icons.Default.List, contentDescription = null,
                                modifier = Modifier.size(24.dp))
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "المحاسبة والتكاملات" else "Accounting & Integrations")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(Icons.Default.ArrowForward, contentDescription = null)
                        }
                    }

                    run {
                        PrinterSettingsSection(
                            viewModel = viewModel,
                            isArabic = isArabic,
                            connectionType = uiState.printerConnectionType,
                            bluetoothDeviceName = uiState.printerBluetoothDeviceName,
                            bluetoothAddress = uiState.printerBluetoothAddress,
                            networkHost = uiState.printerNetworkHost,
                            networkPort = uiState.printerNetworkPort,
                            paperWidthMm = uiState.printerPaperWidthMm,
                            bondedDevices = uiState.bondedBluetoothDevices,
                            showDeviceSheet = uiState.showBluetoothDeviceSheet,
                            printerPrintState = uiState.printerPrintState,
                            onDismissDeviceSheet = viewModel::dismissBluetoothDeviceSheet,
                            onShowDeviceSheet = viewModel::showBluetoothDeviceSheet,
                            onClearBluetoothDevice = viewModel::clearBluetoothPrinter,
                            onSelectDevice = viewModel::setBluetoothPrinter,
                            onConnectionTypeChange = viewModel::setPrinterConnectionType,
                            onNetworkHostChange = viewModel::setPrinterNetworkHost,
                            onNetworkPortChange = viewModel::setPrinterNetworkPort,
                            onPaperWidthChange = viewModel::setPrinterPaperWidth,
                            onPrintTest = viewModel::printTestReceipt,
                            onClearPrintState = viewModel::clearPrinterPrintState
                        )
                    }

                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onNavigateToIntegrations
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth().padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(Icons.Default.Info, contentDescription = null,
                                modifier = Modifier.size(24.dp))
                            Spacer(modifier = Modifier.width(16.dp))
                            Text(if (isArabic) "الواجهة البرمجية والتكاملات" else "API & Integrations")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(Icons.Default.ArrowForward, contentDescription = null)
                        }
                    }

                    // Kiosk exit PIN configuration
                    run {
                        var pinInput by remember(uiState.kioskPin) { mutableStateOf(uiState.kioskPin) }
                        var showPin  by remember { mutableStateOf(false) }
                        var pinError by remember { mutableStateOf("") }
                        Card(modifier = Modifier.fillMaxWidth()) {
                            Column(modifier = Modifier.fillMaxWidth().padding(16.dp),
                                verticalArrangement = Arrangement.spacedBy(8.dp)) {
                                Text(if (isArabic) "رمز خروج الكشك" else "Kiosk Exit PIN",
                                    style = MaterialTheme.typography.bodyLarge,
                                    fontWeight = FontWeight.SemiBold)
                                Text(if (isArabic) "رمز مكون من 4 أرقام يدخله المشغل للخروج من وضع الكشك." else "4-digit PIN operators enter to exit kiosk mode.",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant)
                                Row(
                                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    OutlinedTextField(
                                        value = pinInput,
                                        onValueChange = { v ->
                                            if (v.length <= 8 && v.all { it.isDigit() }) {
                                                pinInput = v
                                                pinError = ""
                                            }
                                        },
                                        label = { Text(if (isArabic) "رمز الخروج" else "Exit PIN") },
                                        visualTransformation = if (showPin) VisualTransformation.None
                                                               else PasswordVisualTransformation(),
                                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.NumberPassword),
                                        isError = pinError.isNotBlank(),
                                        supportingText = if (pinError.isNotBlank()) {{ Text(pinError) }} else null,
                                        modifier = Modifier.weight(1f),
                                        singleLine = true,
                                        trailingIcon = {
                                            TextButton(onClick = { showPin = !showPin }) {
                                                Text(if (showPin) (if (isArabic) "إخفاء" else "Hide") else (if (isArabic) "إظهار" else "Show"),
                                                    style = MaterialTheme.typography.labelSmall)
                                            }
                                        }
                                    )
                                    Button(onClick = {
                                        if (pinInput.length < 4) {
                                            pinError = if (isArabic) "4 أرقام على الأقل" else "Minimum 4 digits"
                                        } else {
                                            viewModel.saveKioskPin(pinInput)
                                        }
                                    }) { Text(if (isArabic) "حفظ" else "Save") }
                                }
                            }
                        }
                    }

                    SettingsSectionHeader(if (isArabic) "معلومات المنشأة" else "Business Information")
                    OutlinedTextField(
                        value = uiState.storeName,
                        onValueChange = { viewModel.onFieldChange(SettingsField.StoreName, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text(if (isArabic) "اسم المنشأة *" else "Store Name *") },
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = uiState.address,
                        onValueChange = { viewModel.onFieldChange(SettingsField.Address, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text(if (isArabic) "العنوان" else "Address") },
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = uiState.phone,
                        onValueChange = { viewModel.onFieldChange(SettingsField.Phone, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text(if (isArabic) "الهاتف" else "Phone") },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Phone),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = uiState.email,
                        onValueChange = { viewModel.onFieldChange(SettingsField.Email, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text(if (isArabic) "البريد الإلكتروني" else "Email") },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = uiState.website,
                        onValueChange = { viewModel.onFieldChange(SettingsField.Website, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text(if (isArabic) "الموقع الإلكتروني" else "Website") },
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = uiState.taxNumber,
                        onValueChange = { viewModel.onFieldChange(SettingsField.TaxNumber, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text(if (isArabic) "الرقم الضريبي" else "Tax Number") },
                        singleLine = true
                    )

                    SettingsSectionHeader(if (isArabic) "الضريبة" else "Tax")
                    SettingsToggleRow(
                        label = if (isArabic) "تفعيل الضريبة" else "Enable Tax",
                        checked = uiState.taxEnabled,
                        onCheckedChange = {
                            viewModel.onToggleChange(SettingsToggle.TaxEnabled, it)
                        }
                    )
                    OutlinedTextField(
                        value = uiState.taxRate,
                        onValueChange = { viewModel.onFieldChange(SettingsField.TaxRate, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text(if (isArabic) "نسبة الضريبة (%)" else "Tax Rate (%)") },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                        enabled = uiState.taxEnabled,
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = uiState.currency,
                        onValueChange = { viewModel.onFieldChange(SettingsField.Currency, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text(if (isArabic) "رمز العملة (مثال: USD)" else "Currency Code (e.g. USD)") },
                        singleLine = true
                    )

                    SettingsSectionHeader(if (isArabic) "الفاتورة" else "Receipt")
                    OutlinedTextField(
                        value = uiState.receiptHeader,
                        onValueChange = { viewModel.onFieldChange(SettingsField.ReceiptHeader, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text(if (isArabic) "رأس الفاتورة" else "Receipt Header") },
                        minLines = 2
                    )
                    OutlinedTextField(
                        value = uiState.receiptFooter,
                        onValueChange = { viewModel.onFieldChange(SettingsField.ReceiptFooter, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text(if (isArabic) "ذيل الفاتورة" else "Receipt Footer") },
                        minLines = 2
                    )

                    SettingsSectionHeader(if (isArabic) "الميزات" else "Features")
                    SettingsToggleRow(
                        label = if (isArabic) "طلب بيانات العميل" else "Require Customer Info",
                        checked = uiState.requireCustomerInfo,
                        onCheckedChange = {
                            viewModel.onToggleChange(SettingsToggle.RequireCustomerInfo, it)
                        }
                    )
                    SettingsToggleRow(
                        label = if (isArabic) "تتبع المخزون" else "Inventory Tracking",
                        checked = uiState.enableInventoryTracking,
                        onCheckedChange = {
                            viewModel.onToggleChange(SettingsToggle.EnableInventoryTracking, it)
                        }
                    )
                    SettingsToggleRow(
                        label = if (isArabic) "إدارة الوصفات" else "Recipe Management",
                        checked = uiState.enableRecipeManagement,
                        onCheckedChange = {
                            viewModel.onToggleChange(SettingsToggle.EnableRecipeManagement, it)
                        }
                    )

                    // ── Loyalty Programme ────────────────────────────────────
                    SettingsSectionHeader(if (isArabic) "برنامج الولاء" else "Loyalty Programme")
                    SettingsToggleRow(
                        label = if (isArabic) "تفعيل نقاط الولاء" else "Enable Loyalty Points",
                        checked = uiState.loyaltyEnabled,
                        onCheckedChange = {
                            viewModel.onToggleChange(SettingsToggle.LoyaltyEnabled, it)
                        }
                    )
                    if (uiState.loyaltyEnabled) {
                        OutlinedTextField(
                            value = uiState.loyaltyPointsPerDollar,
                            onValueChange = {
                                viewModel.onFieldChange(SettingsField.LoyaltyPointsPerDollar, it)
                            },
                            modifier = Modifier.fillMaxWidth(),
                            label = { Text(if (isArabic) "نقاط لكل دولار" else "Points per USD spent") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            supportingText = { Text(if (isArabic) "مثال: 10 = 10 نقاط لكل \$1" else "e.g. 10 = 10 points per \$1") },
                            singleLine = true
                        )
                        OutlinedTextField(
                            value = uiState.loyaltyPointValueUsd,
                            onValueChange = {
                                viewModel.onFieldChange(SettingsField.LoyaltyPointValueUsd, it)
                            },
                            modifier = Modifier.fillMaxWidth(),
                            label = { Text(if (isArabic) "قيمة النقطة بالدولار" else "Point value in USD") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                            supportingText = { Text(if (isArabic) "مثال: 0.01 = سنت لكل نقطة" else "e.g. 0.01 = 1 cent per point") },
                            singleLine = true
                        )
                        OutlinedTextField(
                            value = uiState.loyaltyMinRedeemPoints,
                            onValueChange = {
                                viewModel.onFieldChange(SettingsField.LoyaltyMinRedeemPoints, it)
                            },
                            modifier = Modifier.fillMaxWidth(),
                            label = { Text(if (isArabic) "الحد الأدنى للاستبدال" else "Minimum points to redeem") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            singleLine = true
                        )
                    }

                    // ── Notifications ────────────────────────────────────────
                    SettingsSectionHeader(if (isArabic) "الإشعارات" else "Notifications")
                    SettingsToggleRowWithDescription(
                        label = if (isArabic) "إشعارات نقاط الولاء" else "Loyalty Points Notifications",
                        description = if (isArabic) "أرسل إشعاراً عند كسب نقاط"
                                        else "Notify customers via WhatsApp when they earn points",
                        checked = uiState.notifyOnLoyaltyEarn,
                        onCheckedChange = { viewModel.saveNotifyOnLoyaltyEarn(it) }
                    )
                    SettingsToggleRowWithDescription(
                        label = if (isArabic) "تأكيد الحجوزات" else "Reservation Confirmations",
                        description = if (isArabic) "أرسل تأكيداً عند إنشاء حجز"
                                        else "Send a WhatsApp confirmation when a reservation is created",
                        checked = uiState.notifyOnReservationConfirm,
                        onCheckedChange = { viewModel.saveNotifyOnReservationConfirm(it) }
                    )

                    // ── Lebanese Market ──────────────────────────────────────
                    SettingsSectionHeader(if (isArabic) "السوق اللبناني" else "Lebanese Market")

                    // Quick-setup banner
                    OutlinedCard(modifier = Modifier.fillMaxWidth()) {
                        Column(
                            modifier = Modifier.padding(12.dp),
                            verticalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            Text(
                                text = if (isArabic) "إعداد لبنان السريع" else "Lebanon Quick Setup",
                                style = MaterialTheme.typography.bodyMedium,
                                fontWeight = FontWeight.Medium
                            )
                            Text(
                                text = if (isArabic) "ضبط ضريبة القيمة المضافة 11% وعرض الليرة ورسوم الطابع بنقرة واحدة." else "Set VAT 11%, LBP display, and stamp duty in one tap.",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                            OutlinedButton(
                                onClick = { showLebanonPresetDialog = true },
                                modifier = Modifier.fillMaxWidth()
                            ) {
                                Text(if (isArabic) "تطبيق إعداد لبنان" else "Apply Lebanon Preset")
                            }
                        }
                    }

                    // Exchange rate field
                    OutlinedTextField(
                        value = uiState.exchangeRateLbpPerUsd,
                        onValueChange = {
                            viewModel.onFieldChange(SettingsField.ExchangeRateLbpPerUsd, it)
                        },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text(if (isArabic) "سعر الصرف (ل.ل. لكل دولار)" else "Exchange Rate (LBP per 1 USD)") },
                        placeholder = { Text("e.g. 89500") },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                        supportingText = {
                            val rate = uiState.exchangeRateLbpPerUsd.trim().toBigDecimalOrNull()
                            if (rate != null && rate > java.math.BigDecimal.ZERO)
                                Text("1 USD = ${rate.toLong()} LBP")
                        },
                        singleLine = true
                    )

                    SettingsToggleRow(
                        label = if (isArabic) "عرض الليرة اللبنانية على الفواتير" else "Show LBP on Receipts",
                        checked = uiState.showLbpOnReceipt,
                        onCheckedChange = {
                            viewModel.onToggleChange(SettingsToggle.ShowLbpOnReceipt, it)
                        }
                    )

                    SettingsToggleRow(
                        label = if (isArabic) "رسوم الطابع (قانون موازنة 2025)" else "Stamp Duty (2025 Budget Law)",
                        checked = uiState.stampDutyEnabled,
                        onCheckedChange = {
                            viewModel.onToggleChange(SettingsToggle.StampDutyEnabled, it)
                        }
                    )

                    if (uiState.stampDutyEnabled) {
                        OutlinedTextField(
                            value = uiState.stampDutyAmountUsd,
                            onValueChange = {
                                viewModel.onFieldChange(SettingsField.StampDutyAmountUsd, it)
                            },
                            modifier = Modifier.fillMaxWidth(),
                            label = { Text(if (isArabic) "مبلغ رسوم الطابع (دولار)" else "Stamp Duty Amount (USD)") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                            supportingText = { Text(if (isArabic) "الافتراضي: \$2.00 لكل فاتورة (قانون 2025)" else "Default: \$2.00 per receipt (2025 law)") },
                            singleLine = true
                        )
                    }

                    Button(
                        onClick = { viewModel.saveSettings() },
                        enabled = !uiState.isSaving && !uiState.isLoading,
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        if (uiState.isSaving) {
                            CircularProgressIndicator(
                                modifier = Modifier.size(16.dp),
                                strokeWidth = 2.dp
                            )
                            Spacer(modifier = Modifier.width(8.dp))
                        }
                        Text(if (uiState.isSaving) (if (isArabic) "جارٍ الحفظ…" else "Saving…") else (if (isArabic) "حفظ الإعدادات" else "Save Settings"))
                    }
                }
            }
        }
    }
}

@Composable
private fun SettingsSectionHeader(title: String) {
    Text(
        text = title,
        style = MaterialTheme.typography.titleSmall,
        color = MaterialTheme.colorScheme.secondary,
        modifier = Modifier.padding(top = 8.dp)
    )
}

@Composable
private fun SettingsToggleRowWithDescription(
    label: String,
    description: String,
    checked: Boolean,
    onCheckedChange: (Boolean) -> Unit
) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Column(modifier = Modifier.weight(1f).padding(end = 12.dp)) {
            Text(label, style = MaterialTheme.typography.bodyLarge)
            Text(
                description,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
        Switch(checked = checked, onCheckedChange = onCheckedChange)
    }
}

@Composable
private fun SettingsToggleRow(
    label: String,
    checked: Boolean,
    onCheckedChange: (Boolean) -> Unit
) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(label)
        Switch(checked = checked, onCheckedChange = onCheckedChange)
    }
}
