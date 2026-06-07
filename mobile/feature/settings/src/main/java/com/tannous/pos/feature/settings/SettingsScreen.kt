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
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.TextButton
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.ui.LanguageViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun LebanonPresetDialog(
    onConfirm: () -> Unit,
    onDismiss: () -> Unit
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Lebanon Quick Setup") },
        text = {
            Text(
                "This will set:\n" +
                "• VAT = 11%\n" +
                "• Show LBP on receipts = ON\n" +
                "• Stamp duty ($2 USD) = ON\n\n" +
                "You will still need to enter the current exchange rate manually."
            )
        },
        confirmButton = {
            TextButton(onClick = {
                onConfirm()
                onDismiss()
            }) { Text("Apply") }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text("Cancel") }
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
    viewModel: SettingsViewModel = hiltViewModel(),
    languageViewModel: LanguageViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
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
            snackbarHostState.showSnackbar("Settings saved")
            viewModel.clearError()
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text("Settings") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = "Back")
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
                                        text = "${uiState.failedSyncCount} sync operation(s) failed",
                                        style = MaterialTheme.typography.bodyMedium,
                                        fontWeight = FontWeight.Medium,
                                        color = MaterialTheme.colorScheme.onErrorContainer
                                    )
                                    Text(
                                        text = "Some adjustments or operations could not be sent to the server. Contact support if this persists.",
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
                                    viewModel.toggleLanguage()
                                    languageViewModel.refresh()
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
                            Text("Reports")
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(
                                Icons.Default.ArrowForward,
                                contentDescription = null
                            )
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
                            Text("Order History")
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
                            Text("Inventory")
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
                            Text("Printing Preview")
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
                            Text("Sales Dashboard")
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
                            Text("Menu Engineering")
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
                            Text("Table Map")
                            Spacer(modifier = Modifier.weight(1f))
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
                            Text("Kitchen Display (KDS)")
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
                            Text("Digital Menu (QR)")
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
                            Text("Reservations")
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
                            Text("Delivery Queue")
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
                                Text("Self-Ordering Kiosk")
                                Text("PIN: 1234 to exit",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant)
                            }
                            Spacer(modifier = Modifier.weight(1f))
                            Icon(Icons.Default.ArrowForward, contentDescription = null)
                        }
                    }

                    SettingsSectionHeader("Business Information")
                    OutlinedTextField(
                        value = uiState.storeName,
                        onValueChange = { viewModel.onFieldChange(SettingsField.StoreName, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text("Store Name *") },
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = uiState.address,
                        onValueChange = { viewModel.onFieldChange(SettingsField.Address, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text("Address") },
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = uiState.phone,
                        onValueChange = { viewModel.onFieldChange(SettingsField.Phone, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text("Phone") },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Phone),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = uiState.email,
                        onValueChange = { viewModel.onFieldChange(SettingsField.Email, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text("Email") },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = uiState.website,
                        onValueChange = { viewModel.onFieldChange(SettingsField.Website, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text("Website") },
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = uiState.taxNumber,
                        onValueChange = { viewModel.onFieldChange(SettingsField.TaxNumber, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text("Tax Number") },
                        singleLine = true
                    )

                    SettingsSectionHeader("Tax")
                    SettingsToggleRow(
                        label = "Enable Tax",
                        checked = uiState.taxEnabled,
                        onCheckedChange = {
                            viewModel.onToggleChange(SettingsToggle.TaxEnabled, it)
                        }
                    )
                    OutlinedTextField(
                        value = uiState.taxRate,
                        onValueChange = { viewModel.onFieldChange(SettingsField.TaxRate, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text("Tax Rate (%)") },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                        enabled = uiState.taxEnabled,
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = uiState.currency,
                        onValueChange = { viewModel.onFieldChange(SettingsField.Currency, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text("Currency Code (e.g. USD)") },
                        singleLine = true
                    )

                    SettingsSectionHeader("Receipt")
                    OutlinedTextField(
                        value = uiState.receiptHeader,
                        onValueChange = { viewModel.onFieldChange(SettingsField.ReceiptHeader, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text("Receipt Header") },
                        minLines = 2
                    )
                    OutlinedTextField(
                        value = uiState.receiptFooter,
                        onValueChange = { viewModel.onFieldChange(SettingsField.ReceiptFooter, it) },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text("Receipt Footer") },
                        minLines = 2
                    )

                    SettingsSectionHeader("Features")
                    SettingsToggleRow(
                        label = "Require Customer Info",
                        checked = uiState.requireCustomerInfo,
                        onCheckedChange = {
                            viewModel.onToggleChange(SettingsToggle.RequireCustomerInfo, it)
                        }
                    )
                    SettingsToggleRow(
                        label = "Inventory Tracking",
                        checked = uiState.enableInventoryTracking,
                        onCheckedChange = {
                            viewModel.onToggleChange(SettingsToggle.EnableInventoryTracking, it)
                        }
                    )
                    SettingsToggleRow(
                        label = "Recipe Management",
                        checked = uiState.enableRecipeManagement,
                        onCheckedChange = {
                            viewModel.onToggleChange(SettingsToggle.EnableRecipeManagement, it)
                        }
                    )

                    // ── Loyalty Programme ────────────────────────────────────
                    SettingsSectionHeader("Loyalty Programme")
                    SettingsToggleRow(
                        label = "Enable Loyalty Points",
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
                            label = { Text("Points per USD spent") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            supportingText = { Text("e.g. 10 = 10 points per \$1") },
                            singleLine = true
                        )
                        OutlinedTextField(
                            value = uiState.loyaltyPointValueUsd,
                            onValueChange = {
                                viewModel.onFieldChange(SettingsField.LoyaltyPointValueUsd, it)
                            },
                            modifier = Modifier.fillMaxWidth(),
                            label = { Text("Point value in USD") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                            supportingText = { Text("e.g. 0.01 = 1 cent per point") },
                            singleLine = true
                        )
                        OutlinedTextField(
                            value = uiState.loyaltyMinRedeemPoints,
                            onValueChange = {
                                viewModel.onFieldChange(SettingsField.LoyaltyMinRedeemPoints, it)
                            },
                            modifier = Modifier.fillMaxWidth(),
                            label = { Text("Minimum points to redeem") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            singleLine = true
                        )
                    }

                    // ── Lebanese Market ──────────────────────────────────────
                    SettingsSectionHeader("Lebanese Market")

                    // Quick-setup banner
                    OutlinedCard(modifier = Modifier.fillMaxWidth()) {
                        Column(
                            modifier = Modifier.padding(12.dp),
                            verticalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            Text(
                                text = "Lebanon Quick Setup",
                                style = MaterialTheme.typography.bodyMedium,
                                fontWeight = FontWeight.Medium
                            )
                            Text(
                                text = "Set VAT 11%, LBP display, and stamp duty in one tap.",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                            OutlinedButton(
                                onClick = { showLebanonPresetDialog = true },
                                modifier = Modifier.fillMaxWidth()
                            ) {
                                Text("Apply Lebanon Preset")
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
                        label = { Text("Exchange Rate (LBP per 1 USD)") },
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
                        label = "Show LBP on Receipts",
                        checked = uiState.showLbpOnReceipt,
                        onCheckedChange = {
                            viewModel.onToggleChange(SettingsToggle.ShowLbpOnReceipt, it)
                        }
                    )

                    SettingsToggleRow(
                        label = "Stamp Duty (2025 Budget Law)",
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
                            label = { Text("Stamp Duty Amount (USD)") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                            supportingText = { Text("Default: \$2.00 per receipt (2025 law)") },
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
                        Text(if (uiState.isSaving) "Saving…" else "Save Settings")
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
