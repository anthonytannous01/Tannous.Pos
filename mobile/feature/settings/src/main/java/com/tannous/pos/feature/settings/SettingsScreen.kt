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
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreen(
    onNavigateBack: () -> Unit,
    onNavigateToPrintingPreview: () -> Unit,
    onNavigateToReports: () -> Unit,
    onNavigateToOrderHistory: () -> Unit,
    viewModel: SettingsViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }
    val scrollState = rememberScrollState()

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
