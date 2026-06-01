package com.tannous.pos.feature.inventory

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.InventoryItemDto
import com.tannous.pos.core.util.currencyFormatterFor
import java.math.BigDecimal

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun InventoryScreen(
    onNavigateBack: () -> Unit,
    viewModel: InventoryViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }
    val currencyFormatter = remember(uiState.currencyCode) {
        currencyFormatterFor(uiState.currencyCode)
    }

    LaunchedEffect(uiState.submitSuccess) {
        uiState.submitSuccess?.let { msg ->
            snackbarHostState.showSnackbar(msg)
            viewModel.clearSubmitSuccess()
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text("Inventory") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = "Back")
                    }
                },
                actions = {
                    IconButton(onClick = { viewModel.load() }) {
                        Icon(Icons.Default.Refresh, contentDescription = "Refresh")
                    }
                }
            )
        }
    ) { paddingValues ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
        ) {
            Row(
                modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)
            ) {
                FilterChip(
                    selected = uiState.filter == InventoryFilter.All,
                    onClick = { viewModel.setFilter(InventoryFilter.All) },
                    label = { Text("All") }
                )
                Spacer(modifier = Modifier.width(8.dp))
                FilterChip(
                    selected = uiState.filter == InventoryFilter.LowStock,
                    onClick = { viewModel.setFilter(InventoryFilter.LowStock) },
                    label = { Text("Low Stock") }
                )
            }

            when {
                uiState.isLoading -> {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        CircularProgressIndicator()
                    }
                }
                uiState.error != null -> {
                    Column(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(16.dp),
                        horizontalAlignment = Alignment.CenterHorizontally,
                        verticalArrangement = Arrangement.Center
                    ) {
                        Text(
                            uiState.error!!,
                            color = MaterialTheme.colorScheme.error
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                        Button(onClick = { viewModel.load() }) {
                            Text("Retry")
                        }
                    }
                }
                uiState.items.isEmpty() -> {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            if (uiState.filter == InventoryFilter.LowStock) {
                                "No low stock items"
                            } else {
                                "No inventory items"
                            }
                        )
                    }
                }
                else -> {
                    LazyColumn {
                        items(uiState.items, key = { it.id }) { item ->
                            InventoryItemRow(
                                item = item,
                                currencyFormatter = currencyFormatter,
                                onAdjust = {
                                    viewModel.openAction(item, InventoryAction.Adjust)
                                },
                                onWastage = {
                                    viewModel.openAction(item, InventoryAction.Wastage)
                                }
                            )
                        }
                    }
                }
            }
        }
    }

    val actionItem = uiState.actionItem
    val actionType = uiState.actionType
    if (actionItem != null && actionType != null) {
        InventoryActionDialog(
            item = actionItem,
            actionType = actionType,
            isSubmitting = uiState.isSubmitting,
            submitError = uiState.submitError,
            onDismiss = { viewModel.dismissAction() },
            onSubmit = { quantity, reason ->
                viewModel.submitAction(quantity, reason)
            }
        )
    }
}

@Composable
private fun InventoryItemRow(
    item: InventoryItemDto,
    currencyFormatter: java.text.NumberFormat,
    onAdjust: () -> Unit,
    onWastage: () -> Unit
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 4.dp)
    ) {
        Column(modifier = Modifier.padding(12.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        item.ingredientName,
                        style = MaterialTheme.typography.titleSmall,
                        fontWeight = FontWeight.Medium
                    )
                    Text(
                        "${item.currentStock.stripTrailingZeros().toPlainString()} ${item.ingredientUnit}",
                        style = MaterialTheme.typography.bodySmall
                    )
                    if (item.currentStock <= item.minimumStock) {
                        Text(
                            "Low stock",
                            color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.labelSmall
                        )
                    }
                }
                Column(horizontalAlignment = Alignment.End) {
                    Text(
                        "Min: ${item.minimumStock.stripTrailingZeros().toPlainString()}",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Text(
                        "Cost: ${currencyFormatter.format(item.averageCost)}/${item.ingredientUnit}",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.End
            ) {
                TextButton(onClick = onWastage) {
                    Text("Wastage", color = MaterialTheme.colorScheme.error)
                }
                Spacer(modifier = Modifier.width(8.dp))
                TextButton(onClick = onAdjust) {
                    Text("Adjust")
                }
            }
        }
    }
}

@Composable
private fun InventoryActionDialog(
    item: InventoryItemDto,
    actionType: InventoryAction,
    isSubmitting: Boolean,
    submitError: String?,
    onDismiss: () -> Unit,
    onSubmit: (BigDecimal, String) -> Unit
) {
    var quantityInput by rememberSaveable { mutableStateOf("") }
    var reasonInput by rememberSaveable { mutableStateOf("") }
    var quantityError by remember { mutableStateOf<String?>(null) }

    LaunchedEffect(item.id, actionType) {
        quantityInput = ""
        reasonInput = ""
        quantityError = null
    }

    AlertDialog(
        onDismissRequest = { if (!isSubmitting) onDismiss() },
        title = {
            Text(
                if (actionType == InventoryAction.Wastage) "Record Wastage" else "Adjust Stock"
            )
        },
        text = {
            Column {
                Text(item.ingredientName, style = MaterialTheme.typography.bodyMedium)
                Text(
                    "Current: ${item.currentStock.stripTrailingZeros().toPlainString()} ${item.ingredientUnit}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Spacer(modifier = Modifier.height(8.dp))
                OutlinedTextField(
                    value = quantityInput,
                    onValueChange = {
                        quantityInput = it
                        quantityError = null
                    },
                    label = {
                        Text(
                            if (actionType == InventoryAction.Wastage) {
                                "Quantity wasted (${item.ingredientUnit})"
                            } else {
                                "Quantity change (${item.ingredientUnit})"
                            }
                        )
                    },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                    isError = quantityError != null,
                    supportingText = quantityError?.let { err -> { Text(err) } },
                    singleLine = true
                )
                Spacer(modifier = Modifier.height(8.dp))
                OutlinedTextField(
                    value = reasonInput,
                    onValueChange = { reasonInput = it },
                    label = { Text("Reason") },
                    maxLines = 2
                )
                if (submitError != null) {
                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        submitError,
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodySmall
                    )
                }
            }
        },
        confirmButton = {
            Button(
                onClick = {
                    val qty = quantityInput.trim().toBigDecimalOrNull()
                    when {
                        reasonInput.isBlank() -> quantityError = "Reason is required"
                        qty == null -> quantityError = "Enter a valid quantity"
                        actionType == InventoryAction.Wastage && qty <= BigDecimal.ZERO ->
                            quantityError = "Enter a positive quantity"
                        actionType == InventoryAction.Adjust && qty == BigDecimal.ZERO ->
                            quantityError = "Enter a non-zero quantity"
                        else -> onSubmit(qty, reasonInput.trim())
                    }
                },
                enabled = !isSubmitting
            ) {
                if (isSubmitting) {
                    CircularProgressIndicator(modifier = Modifier.size(16.dp))
                } else {
                    Text(if (actionType == InventoryAction.Wastage) "Record" else "Adjust")
                }
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss, enabled = !isSubmitting) {
                Text("Cancel")
            }
        }
    )
}
