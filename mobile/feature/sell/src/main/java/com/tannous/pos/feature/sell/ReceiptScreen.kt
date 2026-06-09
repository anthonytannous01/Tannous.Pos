package com.tannous.pos.feature.sell

import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Share
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.tannous.pos.core.util.currencyFormatterFor
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.OrderDto
import com.tannous.pos.core.data.repository.isAlreadyVoidedStatus
import com.tannous.pos.core.data.repository.isVoidableStatus
import com.tannous.pos.core.ui.LocalIsArabic
import kotlinx.coroutines.delay

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReceiptScreen(
    order: OrderDto,
    onDone: () -> Unit,
    viewModel: ReceiptViewModel = hiltViewModel()
) {
    val printState by viewModel.printState.collectAsStateWithLifecycle()
    val voidState by viewModel.voidState.collectAsStateWithLifecycle()
    val orderLines by viewModel.orderLines.collectAsStateWithLifecycle()
    val currencyCode by viewModel.currencyCode.collectAsStateWithLifecycle()
    val currencyFormatter = remember(currencyCode) { currencyFormatterFor(currencyCode) }
    val isArabic = LocalIsArabic.current
    
    val snackbarHostState = remember { SnackbarHostState() }
    var showVoidDialog by remember { mutableStateOf(false) }
    var showFeedbackDialog by remember { mutableStateOf(false) }
    var voidReason by remember { mutableStateOf("") }

    val isPendingSync = order.receiptNumber?.startsWith("PENDING") == true
    val canVoid = order.status.isVoidableStatus() &&
        !order.status.isAlreadyVoidedStatus() &&
        !isPendingSync &&
        voidState !is VoidState.Voiding
    
    // Best-effort fetch of line items for this order (never blocks the receipt)
    LaunchedEffect(order.id) {
        viewModel.loadOrderLines(order.id)
    }
    
    // Show snackbar for print results
    LaunchedEffect(printState) {
        when (printState) {
            is PrintState.Success -> {
                snackbarHostState.showSnackbar("Receipt printed successfully")
                viewModel.clearPrintState()
            }
            is PrintState.Error -> {
                val errorState = printState as PrintState.Error
                snackbarHostState.showSnackbar(
                    message = "Print failed: ${errorState.message}",
                    duration = SnackbarDuration.Long
                )
                viewModel.clearPrintState()
            }
            else -> {}
        }
    }

    LaunchedEffect(voidState) {
        when (voidState) {
            is VoidState.Success -> {
                snackbarHostState.showSnackbar("Order voided successfully")
                viewModel.clearVoidState()
                delay(1500)
                onDone()
            }
            is VoidState.Error -> {
                snackbarHostState.showSnackbar(
                    (voidState as VoidState.Error).message,
                    duration = SnackbarDuration.Long
                )
                viewModel.clearVoidState()
            }
            else -> {}
        }
    }

    if (showVoidDialog) {
        AlertDialog(
            onDismissRequest = {
                showVoidDialog = false
                voidReason = ""
            },
            title = { Text("Void Order") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text("This will cancel the order. A reason is required.")
                    OutlinedTextField(
                        value = voidReason,
                        onValueChange = { if (it.length <= 500) voidReason = it },
                        label = { Text("Reason") },
                        modifier = Modifier.fillMaxWidth(),
                        minLines = 2,
                        supportingText = { Text("${voidReason.length}/500") }
                    )
                }
            },
            confirmButton = {
                TextButton(
                    onClick = {
                        showVoidDialog = false
                        viewModel.voidOrder(order.id, voidReason)
                        voidReason = ""
                    },
                    enabled = voidReason.isNotBlank()
                ) {
                    Text("Void", color = MaterialTheme.colorScheme.error)
                }
            },
            dismissButton = {
                TextButton(onClick = {
                    showVoidDialog = false
                    voidReason = ""
                }) {
                    Text("Cancel")
                }
            }
        )
    }
    
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Order Finalized") }
            )
        },
        snackbarHost = {
            SnackbarHost(hostState = snackbarHostState)
        }
    ) { paddingValues ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
                .padding(16.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            Spacer(modifier = Modifier.height(32.dp))
            
            // Success icon
            Icon(
                imageVector = Icons.Filled.CheckCircle,
                contentDescription = "Success",
                modifier = Modifier.size(80.dp),
                tint = MaterialTheme.colorScheme.primary
            )
            
            Text(
                text = "Order Finalized Successfully!",
                style = MaterialTheme.typography.headlineSmall,
                fontWeight = FontWeight.Bold,
                textAlign = TextAlign.Center
            )
            
            Spacer(modifier = Modifier.height(16.dp))
            
            // Receipt card
            Card(
                modifier = Modifier.fillMaxWidth(),
                elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    // Order number
                    if (order.orderNumber != null) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(
                                text = "Order #",
                                style = MaterialTheme.typography.bodyLarge
                            )
                            Text(
                                text = order.orderNumber ?: "",
                                style = MaterialTheme.typography.bodyLarge,
                                fontWeight = FontWeight.Bold
                            )
                        }
                    }
                    
                    // Receipt number
                    val receiptNumber = order.receiptNumber
                    if (receiptNumber != null) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(
                                text = "Receipt #",
                                style = MaterialTheme.typography.bodyLarge
                            )
                            Text(
                                text = receiptNumber,
                                style = MaterialTheme.typography.bodyLarge,
                                fontWeight = FontWeight.Bold
                            )
                        }
                    }
                    
                    // Line items (best-effort; populated from GET /orders/{id})
                    if (orderLines.isNotEmpty()) {
                        Divider(modifier = Modifier.padding(vertical = 8.dp))
                        Text(
                            text = "Items",
                            style = MaterialTheme.typography.titleSmall,
                            fontWeight = FontWeight.Bold
                        )
                        orderLines.forEach { line ->
                            val lineName = if (isArabic) line.nameAr?.takeIf { it.isNotBlank() } ?: line.name else line.name
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.SpaceBetween
                            ) {
                                Text(
                                    text = "$lineName × ${line.quantity}",
                                    style = MaterialTheme.typography.bodyMedium,
                                    modifier = Modifier.weight(1f)
                                )
                                Text(
                                    text = currencyFormatter.format(line.totalPrice),
                                    style = MaterialTheme.typography.bodyMedium
                                )
                            }
                        }
                    }

                    Divider(modifier = Modifier.padding(vertical = 8.dp))
                    
                    // Totals
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text(
                            text = "Subtotal",
                            style = MaterialTheme.typography.bodyMedium
                        )
                        Text(
                            text = currencyFormatter.format(order.subTotal),
                            style = MaterialTheme.typography.bodyMedium
                        )
                    }
                    
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text(
                            text = "Tax",
                            style = MaterialTheme.typography.bodyMedium
                        )
                        Text(
                            text = currencyFormatter.format(order.tax),
                            style = MaterialTheme.typography.bodyMedium
                        )
                    }
                    
                    Divider(modifier = Modifier.padding(vertical = 8.dp))
                    
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text(
                            text = "Total",
                            style = MaterialTheme.typography.titleLarge,
                            fontWeight = FontWeight.Bold
                        )
                        Text(
                            text = currencyFormatter.format(order.total),
                            style = MaterialTheme.typography.titleLarge,
                            fontWeight = FontWeight.Bold
                        )
                    }
                    
                    // Sync status
                    if (order.syncedAt == null && order.receiptNumber?.startsWith("PENDING") == true) {
                        Spacer(modifier = Modifier.height(8.dp))
                        Divider()
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            text = "⚠️ Queued for sync",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.tertiary,
                            textAlign = TextAlign.Center,
                            modifier = Modifier.fillMaxWidth()
                        )
                        Text(
                            text = "This order will be synced when connection is restored.",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            textAlign = TextAlign.Center,
                            modifier = Modifier.fillMaxWidth()
                        )
                    }
                }
            }
            
            Spacer(modifier = Modifier.weight(1f))
            
            // Action buttons
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                // Share button
                OutlinedButton(
                    onClick = { viewModel.shareReceipt(order) },
                    modifier = Modifier.weight(1f)
                ) {
                    Icon(
                        imageVector = Icons.Filled.Share,
                        contentDescription = "Share",
                        modifier = Modifier.size(18.dp)
                    )
                    Spacer(modifier = Modifier.width(4.dp))
                    Text("Share")
                }
                
                // Print button
                Button(
                    onClick = { viewModel.printReceipt(order) },
                    modifier = Modifier.weight(1f),
                    enabled = printState !is PrintState.Printing
                ) {
                    if (printState is PrintState.Printing) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(18.dp),
                            color = MaterialTheme.colorScheme.onPrimary
                        )
                    } else {
                        Icon(
                            imageVector = Icons.Filled.Share,
                            contentDescription = "Print",
                            modifier = Modifier.size(18.dp)
                        )
                        Spacer(modifier = Modifier.width(4.dp))
                        Text("Print")
                    }
                }
            }

            OutlinedButton(
                onClick = { showVoidDialog = true },
                modifier = Modifier.fillMaxWidth(),
                enabled = canVoid,
                colors = ButtonDefaults.outlinedButtonColors(
                    contentColor = MaterialTheme.colorScheme.error
                )
            ) {
                when {
                    voidState is VoidState.Voiding -> {
                        CircularProgressIndicator(modifier = Modifier.size(16.dp))
                        Spacer(modifier = Modifier.width(8.dp))
                        Text("Voiding…")
                    }
                    order.status.isAlreadyVoidedStatus() -> Text("Order Voided")
                    isPendingSync -> Text("Sync required to void")
                    else -> Text("Void Order")
                }
            }
            
            Spacer(modifier = Modifier.height(8.dp))
            
            // SMS confirmation indicator (shown when customer had a phone number)
            if (!order.customerPhone.isNullOrBlank() && !isPendingSync &&
                !order.status.isAlreadyVoidedStatus()) {
                Text(
                    text = "📱 Confirmation sent to ${order.customerPhone}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            // Feedback button (only for paid, synced orders)
            if (!order.status.isAlreadyVoidedStatus() && !isPendingSync) {
                OutlinedButton(
                    onClick = { showFeedbackDialog = true },
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text("Leave Feedback ⭐")
                }
            }

            // Done button
            Button(
                onClick = onDone,
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp)
            ) {
                Text("Done")
            }

            if (showFeedbackDialog) {
                FeedbackPromptDialog(
                    orderId     = order.id,
                    orderNumber = order.orderNumber,
                    branchId    = null,
                    onDismiss   = { showFeedbackDialog = false }
                )
            }
        }
    }
}
