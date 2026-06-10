package com.tannous.pos.feature.sell

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
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.DeliveryDto
import com.tannous.pos.core.ui.LocalIsArabic

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DeliveryQueueScreen(
    onNavigateBack: () -> Unit,
    viewModel: DeliveryQueueViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }

    LaunchedEffect(uiState.error) {
        uiState.error?.let {
            snackbarHostState.showSnackbar(it, duration = SnackbarDuration.Short)
            viewModel.clearError()
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text("Delivery Queue") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = "Back")
                    }
                },
                actions = {
                    IconButton(onClick = { viewModel.refresh() }) {
                        Icon(Icons.Default.Refresh, contentDescription = "Refresh")
                    }
                }
            )
        }
    ) { padding ->
        val displayed = uiState.deliveries.filter {
            uiState.selectedChannel == null || it.channel == uiState.selectedChannel
        }

        Column(Modifier.fillMaxSize().padding(padding)) {
            if (!uiState.isLoading &&
                (uiState.deliveries.isNotEmpty() || uiState.selectedChannel != null)
            ) {
                ChannelFilterRow(
                    selectedChannel = uiState.selectedChannel,
                    onSelect = { viewModel.filterByChannel(it) }
                )
            }

            when {
                uiState.isLoading -> Box(
                    Modifier.fillMaxSize(),
                    contentAlignment = Alignment.Center
                ) { CircularProgressIndicator() }

                displayed.isEmpty() -> Box(
                    Modifier.fillMaxSize(),
                    contentAlignment = Alignment.Center
                ) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        Text("No active deliveries", style = MaterialTheme.typography.titleMedium)
                        Text("Active deliveries appear here automatically",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                }

                else -> LazyColumn(
                    modifier = Modifier.fillMaxSize(),
                    contentPadding = PaddingValues(12.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    items(displayed, key = { it.id }) { delivery ->
                        DeliveryCard(
                            delivery = delivery,
                            onAssign    = { name, phone -> viewModel.updateStatus(delivery.id, DELIVERY_ASSIGNED,   name, phone) },
                            onPickedUp  = { viewModel.updateStatus(delivery.id, DELIVERY_PICKED_UP) },
                            onOnWay     = { viewModel.updateStatus(delivery.id, DELIVERY_ON_WAY) },
                            onDelivered = { viewModel.updateStatus(delivery.id, DELIVERY_DELIVERED) },
                            onFailed    = { viewModel.updateStatus(delivery.id, DELIVERY_FAILED) }
                        )
                    }
                }
            }
        }
    }
}

/** Distinct brand colour per delivery channel for badges. */
@Composable
private fun channelColor(channel: Int): Color = when (channel) {
    1    -> Color(0xFFFF6B35) // Toters
    2    -> Color(0xFFFF6900) // Talabat
    3    -> Color(0xFF009DE0) // Wolt
    else -> MaterialTheme.colorScheme.primary // Own / Other
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun ChannelFilterRow(
    selectedChannel: Int?,
    onSelect: (Int?) -> Unit
) {
    val isArabic = LocalIsArabic.current
    // (label, channel value) — null channel means "All".
    val filters = listOf(
        (if (isArabic) "الكل" else "All") to null,
        (if (isArabic) deliveryChannelLabelsAr[0] else deliveryChannelLabels[0]) to 0,
        (if (isArabic) deliveryChannelLabelsAr[1] else deliveryChannelLabels[1]) to 1,
        (if (isArabic) deliveryChannelLabelsAr[2] else deliveryChannelLabels[2]) to 2
    )

    LazyRow(
        modifier = Modifier.fillMaxWidth(),
        contentPadding = PaddingValues(horizontal = 12.dp, vertical = 8.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        items(filters) { (label, channel) ->
            FilterChip(
                selected = selectedChannel == channel,
                onClick = { onSelect(channel) },
                label = { Text(label ?: "") }
            )
        }
    }
}

/**
 * Prominent channel badge. For external platform orders shows the platform reference
 * (e.g. "Toters #4821") in the channel brand colour; otherwise a neutral channel label.
 */
@Composable
private fun ChannelBadge(delivery: DeliveryDto) {
    val isArabic = LocalIsArabic.current
    val color = channelColor(delivery.channel)

    if (!delivery.externalOrderReference.isNullOrBlank()) {
        Surface(
            color = color,
            shape = MaterialTheme.shapes.small
        ) {
            Text(
                text = delivery.externalOrderReference!!,
                style = MaterialTheme.typography.labelMedium,
                fontWeight = FontWeight.Bold,
                color = Color.White,
                modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp)
            )
        }
    } else {
        val label = (if (isArabic) deliveryChannelLabelsAr[delivery.channel]
                     else deliveryChannelLabels[delivery.channel]) ?: delivery.channelName
        Surface(
            color = color.copy(alpha = 0.15f),
            shape = MaterialTheme.shapes.small
        ) {
            Text(
                text = label,
                style = MaterialTheme.typography.labelSmall,
                color = color,
                modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp)
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun DeliveryCard(
    delivery:    DeliveryDto,
    onAssign:    (driverName: String, driverPhone: String) -> Unit,
    onPickedUp:  () -> Unit,
    onOnWay:     () -> Unit,
    onDelivered: () -> Unit,
    onFailed:    () -> Unit
) {
    var showAssignDialog by remember { mutableStateOf(false) }
    var driverName  by remember { mutableStateOf(delivery.driverName ?: "") }
    var driverPhone by remember { mutableStateOf(delivery.driverPhone ?: "") }

    if (showAssignDialog) {
        AlertDialog(
            onDismissRequest = { showAssignDialog = false },
            title = { Text("Assign Driver") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedTextField(
                        value = driverName, onValueChange = { driverName = it },
                        label = { Text("Driver Name *") },
                        modifier = Modifier.fillMaxWidth(), singleLine = true
                    )
                    OutlinedTextField(
                        value = driverPhone, onValueChange = { driverPhone = it },
                        label = { Text("Driver Phone") },
                        modifier = Modifier.fillMaxWidth(), singleLine = true
                    )
                }
            },
            confirmButton = {
                TextButton(
                    onClick = {
                        onAssign(driverName, driverPhone)
                        showAssignDialog = false
                    },
                    enabled = driverName.isNotBlank()
                ) { Text("Assign") }
            },
            dismissButton = { TextButton(onClick = { showAssignDialog = false }) { Text("Cancel") } }
        )
    }

    val statusColor = when (delivery.status) {
        DELIVERY_ASSIGNED  -> MaterialTheme.colorScheme.primary
        DELIVERY_PICKED_UP, DELIVERY_ON_WAY -> MaterialTheme.colorScheme.tertiary
        DELIVERY_DELIVERED -> MaterialTheme.colorScheme.secondary
        DELIVERY_FAILED, DELIVERY_CANCELLED -> MaterialTheme.colorScheme.error
        else -> MaterialTheme.colorScheme.onSurfaceVariant
    }

    Card(modifier = Modifier.fillMaxWidth()) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(6.dp)
        ) {
            // Header row
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = delivery.orderNumber ?: delivery.orderId.take(8),
                    fontWeight = FontWeight.Bold,
                    style = MaterialTheme.typography.bodyLarge
                )
                Row(
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    ChannelBadge(delivery)
                    Text(
                        delivery.statusName,
                        style = MaterialTheme.typography.labelMedium,
                        color = statusColor,
                        fontWeight = FontWeight.SemiBold
                    )
                }
            }

            // Customer + address
            delivery.customerName?.let {
                Text(it, style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.Medium)
            }
            Text(delivery.deliveryAddress, style = MaterialTheme.typography.bodySmall)
            delivery.apartmentDetails?.let {
                Text(it, style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            delivery.customerPhone?.let {
                Text("📞 $it", style = MaterialTheme.typography.bodySmall)
            }

            // Driver info
            if (!delivery.driverName.isNullOrBlank()) {
                Text(
                    "🏍 ${delivery.driverName}${if (!delivery.driverPhone.isNullOrBlank()) " · ${delivery.driverPhone}" else ""}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            delivery.estimatedMinutes?.let {
                Text("~${it} min", style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }

            delivery.notes?.let {
                Text(it, style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }

            // Action buttons
            Divider(modifier = Modifier.padding(vertical = 4.dp))
            Row(
                horizontalArrangement = Arrangement.spacedBy(6.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                when (delivery.status) {
                    DELIVERY_PENDING -> {
                        Button(onClick = { showAssignDialog = true },
                            modifier = Modifier.weight(1f)) { Text("Assign") }
                        OutlinedButton(onClick = onFailed,
                            modifier = Modifier.weight(1f),
                            colors = ButtonDefaults.outlinedButtonColors(
                                contentColor = MaterialTheme.colorScheme.error)) { Text("Failed") }
                    }
                    DELIVERY_ASSIGNED -> {
                        Button(onClick = onPickedUp,
                            modifier = Modifier.weight(1f)) { Text("Picked Up") }
                        OutlinedButton(onClick = onFailed,
                            modifier = Modifier.weight(1f),
                            colors = ButtonDefaults.outlinedButtonColors(
                                contentColor = MaterialTheme.colorScheme.error)) { Text("Failed") }
                    }
                    DELIVERY_PICKED_UP -> {
                        Button(onClick = onOnWay,
                            modifier = Modifier.weight(1f)) { Text("On the Way") }
                        OutlinedButton(onClick = onFailed,
                            modifier = Modifier.weight(1f),
                            colors = ButtonDefaults.outlinedButtonColors(
                                contentColor = MaterialTheme.colorScheme.error)) { Text("Failed") }
                    }
                    DELIVERY_ON_WAY -> {
                        Button(onClick = onDelivered,
                            modifier = Modifier.weight(1f)) { Text("Delivered ✓") }
                        OutlinedButton(onClick = onFailed,
                            modifier = Modifier.weight(1f),
                            colors = ButtonDefaults.outlinedButtonColors(
                                contentColor = MaterialTheme.colorScheme.error)) { Text("Failed") }
                    }
                }
            }
        }
    }
}
