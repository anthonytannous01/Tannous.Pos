package com.tannous.pos.feature.sell

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
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
import com.tannous.pos.core.data.model.KdsTicketDto

// KDS status constants (mirrors backend KdsStatus enum)
private const val KDS_PENDING     = 0
private const val KDS_IN_PROGRESS = 1
private const val KDS_DONE        = 2

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun KdsScreen(
    onNavigateBack: () -> Unit,
    viewModel: KdsViewModel = hiltViewModel()
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
                title = {
                    Column {
                        Text("Kitchen Display")
                        Text(
                            text = "${uiState.tickets.size} active item(s)",
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                },
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
        if (uiState.tickets.isEmpty()) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding),
                contentAlignment = Alignment.Center
            ) {
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Text("No active tickets", style = MaterialTheme.typography.titleMedium)
                    Text(
                        "All orders are done or no orders placed yet.",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
        } else {
            // Split into two columns: Pending (left) | In Progress (right)
            val pending    = uiState.tickets.filter { it.kdsStatus == KDS_PENDING }
            val inProgress = uiState.tickets.filter { it.kdsStatus == KDS_IN_PROGRESS }

            Row(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
                    .padding(8.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                // Pending column
                Column(modifier = Modifier.weight(1f)) {
                    KdsColumnHeader("PENDING", pending.size, MaterialTheme.colorScheme.errorContainer)
                    LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        items(pending, key = { it.orderLineId }) { ticket ->
                            KdsTicketCard(
                                ticket = ticket,
                                actionLabel = "Start",
                                actionColor = MaterialTheme.colorScheme.primary,
                                onAction = { viewModel.advanceStatus(ticket) }
                            )
                        }
                    }
                }

                // In Progress column
                Column(modifier = Modifier.weight(1f)) {
                    KdsColumnHeader("IN PROGRESS", inProgress.size, MaterialTheme.colorScheme.tertiaryContainer)
                    LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        items(inProgress, key = { it.orderLineId }) { ticket ->
                            KdsTicketCard(
                                ticket = ticket,
                                actionLabel = "Done ✓",
                                actionColor = Color(0xFF2E7D32),
                                onAction = { viewModel.advanceStatus(ticket) }
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun KdsColumnHeader(title: String, count: Int, color: Color) {
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .background(color, shape = MaterialTheme.shapes.small)
            .padding(horizontal = 12.dp, vertical = 6.dp)
    ) {
        Text(
            text = "$title ($count)",
            style = MaterialTheme.typography.labelLarge,
            fontWeight = FontWeight.Bold
        )
    }
    Spacer(modifier = Modifier.height(8.dp))
}

@Composable
private fun KdsTicketCard(
    ticket: KdsTicketDto,
    actionLabel: String,
    actionColor: Color,
    onAction: () -> Unit
) {
    // Urgency colour: green < 5 min, amber 5–10, red > 10
    val urgencyColor = when {
        ticket.elapsedMinutes >= 10 -> MaterialTheme.colorScheme.errorContainer
        ticket.elapsedMinutes >= 5  -> Color(0xFFFFF9C4) // light yellow
        else                        -> MaterialTheme.colorScheme.surface
    }

    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = urgencyColor),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(
            modifier = Modifier.padding(12.dp),
            verticalArrangement = Arrangement.spacedBy(4.dp)
        ) {
            // Order number + type
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = "#${ticket.orderNumber}",
                    style = MaterialTheme.typography.labelLarge,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = "${ticket.elapsedMinutes}m ago",
                    style = MaterialTheme.typography.labelSmall,
                    color = if (ticket.elapsedMinutes >= 10)
                        MaterialTheme.colorScheme.error
                    else
                        MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            // Order type badge
            Text(
                text = ticket.orderType.uppercase(),
                style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )

            Divider()

            // Item name + quantity
            Text(
                text = "${ticket.quantity.toInt()} × ${ticket.menuItemName}",
                style = MaterialTheme.typography.bodyLarge,
                fontWeight = FontWeight.Medium
            )

            // Add-ons
            if (ticket.addOns.isNotEmpty()) {
                ticket.addOns.forEach { addon ->
                    Text(
                        text = "+ $addon",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }

            // Special notes
            if (!ticket.notes.isNullOrBlank()) {
                Text(
                    text = "⚠ ${ticket.notes}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.error,
                    fontWeight = FontWeight.Medium
                )
            }

            Spacer(modifier = Modifier.height(4.dp))

            // Action button
            Button(
                onClick = onAction,
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(containerColor = actionColor)
            ) {
                Text(actionLabel)
            }
        }
    }
}
