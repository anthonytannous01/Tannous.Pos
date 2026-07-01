package com.tannous.pos.feature.sell

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.local.entity.OrderEntity
import com.tannous.pos.core.data.repository.isAlreadyVoidedStatus
import com.tannous.pos.core.data.repository.isVoidableStatus
import com.tannous.pos.core.ui.LocalIsArabic
import com.tannous.pos.core.util.currencyFormatterFor
import java.text.NumberFormat
import java.time.ZoneId
import java.time.format.DateTimeFormatter

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun OrderHistoryScreen(
    onNavigateBack: () -> Unit,
    onNavigateToReceipt: (String) -> Unit,
    viewModel: OrderHistoryViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic = LocalIsArabic.current
    val snackbarHostState = remember { SnackbarHostState() }
    val currencyFormatter = remember(uiState.currencyCode) {
        currencyFormatterFor(uiState.currencyCode)
    }
    var voidDialogOrderId by remember { mutableStateOf<String?>(null) }
    var voidReason by remember { mutableStateOf("") }

    LaunchedEffect(uiState.voidError) {
        uiState.voidError?.let { message ->
            snackbarHostState.showSnackbar(message, duration = SnackbarDuration.Long)
            viewModel.clearVoidError()
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "سجل الطلبات" else "Order History") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = if (isArabic) "رجوع" else "Back")
                    }
                },
                actions = {
                    IconButton(
                        onClick = { viewModel.refresh() },
                        enabled = !uiState.isRefreshing
                    ) {
                        Icon(Icons.Default.Refresh, contentDescription = if (isArabic) "تحديث" else "Refresh")
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
            LazyRow(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 8.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                items(OrderHistoryFilter.entries.toList()) { filter ->
                    FilterChip(
                        selected = uiState.filter == filter,
                        onClick = { viewModel.setFilter(filter) },
                        label = { Text(orderHistoryFilterLabel(filter, isArabic)) }
                    )
                }
            }

            if (uiState.isRefreshing) {
                LinearProgressIndicator(modifier = Modifier.fillMaxWidth())
            }

            uiState.refreshError?.let { error ->
                Text(
                    text = error,
                    color = MaterialTheme.colorScheme.error,
                    modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)
                )
            }

            if (uiState.orders.isEmpty()) {
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(32.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = if (isArabic) "لا توجد طلبات" else "No orders found",
                        style = MaterialTheme.typography.bodyLarge
                    )
                }
            } else {
                LazyColumn(
                    modifier = Modifier.fillMaxSize(),
                    contentPadding = PaddingValues(horizontal = 16.dp, vertical = 8.dp),
                    verticalArrangement = Arrangement.spacedBy(4.dp)
                ) {
                    items(uiState.orders, key = { it.id }) { order ->
                        OrderHistoryRow(
                            order = order,
                            currencyFormatter = currencyFormatter,
                            isVoiding = uiState.voidingOrderId == order.id,
                            isArabic = isArabic,
                            onTap = { onNavigateToReceipt(order.id) },
                            onVoidClick = {
                                voidDialogOrderId = order.id
                                voidReason = ""
                            }
                        )
                    }
                }
            }
        }
    }

    voidDialogOrderId?.let { orderId ->
        AlertDialog(
            onDismissRequest = {
                voidDialogOrderId = null
                voidReason = ""
            },
            title = { Text(if (isArabic) "إلغاء الطلب" else "Void Order") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text(if (isArabic) "أدخل سبب إلغاء هذا الطلب." else "Enter a reason to void this order.")
                    OutlinedTextField(
                        value = voidReason,
                        onValueChange = { if (it.length <= 500) voidReason = it },
                        label = { Text(if (isArabic) "السبب" else "Reason") },
                        modifier = Modifier.fillMaxWidth(),
                        minLines = 2,
                        supportingText = { Text("${voidReason.length}/500") }
                    )
                }
            },
            confirmButton = {
                TextButton(
                    onClick = {
                        viewModel.voidOrder(orderId, voidReason)
                        voidDialogOrderId = null
                        voidReason = ""
                    },
                    enabled = voidReason.isNotBlank()
                ) {
                    Text(if (isArabic) "إلغاء الطلب" else "Void", color = MaterialTheme.colorScheme.error)
                }
            },
            dismissButton = {
                TextButton(onClick = {
                    voidDialogOrderId = null
                    voidReason = ""
                }) {
                    Text(if (isArabic) "تراجع" else "Cancel")
                }
            }
        )
    }
}

@Composable
private fun OrderHistoryRow(
    order: OrderEntity,
    currencyFormatter: NumberFormat,
    isVoiding: Boolean,
    isArabic: Boolean,
    onTap: () -> Unit,
    onVoidClick: () -> Unit
) {
    Column(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(vertical = 8.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(
                modifier = Modifier
                    .weight(1f)
                    .clickable(onClick = onTap)
            ) {
                Text(
                    text = order.orderNumber ?: order.id.take(8),
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = FontWeight.Medium
                )
                Text(
                    text = "${currencyFormatter.format(order.total)} · ${orderStatusLabel(order, isArabic)}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Text(
                    text = order.createdAt
                        .atZone(ZoneId.systemDefault())
                        .format(DateTimeFormatter.ofPattern("MMM d, HH:mm")),
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            when {
                isVoiding -> CircularProgressIndicator(modifier = Modifier.size(20.dp))
                order.status.isAlreadyVoidedStatus() -> {
                    Text(
                        text = if (isArabic) "ملغى" else "Voided",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
                order.receiptNumber?.startsWith("PENDING") == true -> {
                    Text(
                        text = if (isArabic) "معلق" else "Pending",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
                order.status.isVoidableStatus() -> {
                    TextButton(onClick = onVoidClick) {
                        Text(if (isArabic) "إلغاء" else "Void", color = MaterialTheme.colorScheme.error)
                    }
                }
            }
        }
        Divider()
    }
}

private fun orderHistoryFilterLabel(filter: OrderHistoryFilter, isArabic: Boolean): String = when (filter) {
    OrderHistoryFilter.All -> if (isArabic) "الكل" else "All"
    OrderHistoryFilter.Paid -> if (isArabic) "مدفوع" else "Paid"
    OrderHistoryFilter.Open -> if (isArabic) "مفتوح" else "Open"
    OrderHistoryFilter.Voided -> if (isArabic) "ملغى" else "Voided"
    OrderHistoryFilter.PendingSync -> if (isArabic) "معلق" else "Pending"
}

private fun orderStatusLabel(order: OrderEntity, isArabic: Boolean): String = when {
    order.receiptNumber?.startsWith("PENDING") == true -> if (isArabic) "في الانتظار" else "Queued"
    order.status.isAlreadyVoidedStatus() -> if (isArabic) "ملغى" else "Voided"
    order.status in setOf("6", "Paid", "PAID") -> if (isArabic) "مدفوع" else "Paid"
    order.status in setOf("1", "Open", "OPEN") -> if (isArabic) "مفتوح" else "Open"
    else -> order.status
}
