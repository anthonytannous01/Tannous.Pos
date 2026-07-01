package com.tannous.pos.feature.sell

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.IntrinsicSize
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
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
import com.tannous.pos.core.data.model.KdsStationDto
import com.tannous.pos.core.data.model.KdsTicketDto
import com.tannous.pos.core.ui.LocalIsArabic

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
    val isArabic = LocalIsArabic.current
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
                        Text(if (isArabic) "شاشة المطبخ" else "Kitchen Display")
                        Text(
                            text = if (isArabic) "${uiState.tickets.size} عنصر نشط" else "${uiState.tickets.size} active item(s)",
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = if (isArabic) "رجوع" else "Back")
                    }
                },
                actions = {
                    IconButton(onClick = { viewModel.refresh() }) {
                        Icon(Icons.Default.Refresh, contentDescription = if (isArabic) "تحديث" else "Refresh")
                    }
                }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            if (uiState.stations.isNotEmpty()) {
                KdsStationFilterRow(
                    stations = uiState.stations,
                    selectedStation = uiState.selectedStation,
                    isArabic = isArabic,
                    onSelectStation = viewModel::selectStation
                )
            }

            if (uiState.tickets.isEmpty()) {
                Box(
                    modifier = Modifier.fillMaxSize(),
                    contentAlignment = Alignment.Center
                ) {
                    Column(
                        horizontalAlignment = Alignment.CenterHorizontally,
                        verticalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        Text(if (isArabic) "لا توجد تذاكر نشطة" else "No active tickets", style = MaterialTheme.typography.titleMedium)
                        Text(
                            if (isArabic) "تم إنجاز جميع الطلبات أو لم تُقدَّم أي طلبات بعد." else "All orders are done or no orders placed yet.",
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
                        .padding(8.dp),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    // Pending column
                    Column(modifier = Modifier.weight(1f)) {
                        KdsColumnHeader(if (isArabic) "معلق" else "PENDING", pending.size, MaterialTheme.colorScheme.errorContainer)
                        LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                            items(pending, key = { it.orderLineId }) { ticket ->
                                KdsTicketCard(
                                    ticket = ticket,
                                    actionLabel = if (isArabic) "ابدأ" else "Start",
                                    actionColor = MaterialTheme.colorScheme.primary,
                                    onAction = { viewModel.advanceStatus(ticket) }
                                )
                            }
                        }
                    }

                    // In Progress column
                    Column(modifier = Modifier.weight(1f)) {
                        KdsColumnHeader(if (isArabic) "قيد التنفيذ" else "IN PROGRESS", inProgress.size, MaterialTheme.colorScheme.tertiaryContainer)
                        LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                            items(inProgress, key = { it.orderLineId }) { ticket ->
                                KdsTicketCard(
                                    ticket = ticket,
                                    actionLabel = if (isArabic) "تم ✓" else "Done ✓",
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
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun KdsStationFilterRow(
    stations: List<KdsStationDto>,
    selectedStation: KdsStationDto?,
    isArabic: Boolean,
    onSelectStation: (KdsStationDto?) -> Unit
) {
    LazyRow(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 8.dp, vertical = 4.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        item(key = "all") {
            FilterChip(
                selected = selectedStation == null,
                onClick = { onSelectStation(null) },
                label = { Text(if (isArabic) "الكل" else "All") }
            )
        }
        items(stations, key = { it.id }) { station ->
            val label = if (isArabic) station.nameAr?.takeIf { it.isNotBlank() } ?: station.name
                        else station.name
            val chipColor = parseStationColor(station.color)
            FilterChip(
                selected = selectedStation?.id == station.id,
                onClick = { onSelectStation(station) },
                label = { Text(label) },
                colors = if (chipColor != null) {
                    FilterChipDefaults.filterChipColors(
                        selectedContainerColor = chipColor.copy(alpha = 0.35f),
                        selectedLabelColor = MaterialTheme.colorScheme.onSurface
                    )
                } else {
                    FilterChipDefaults.filterChipColors()
                }
            )
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
    val isArabic = LocalIsArabic.current
    val displayName = if (isArabic) ticket.menuItemNameAr?.takeIf { it.isNotBlank() } ?: ticket.menuItemName
                      else ticket.menuItemName
    // Urgency colour: green < 5 min, amber 5–10, red > 10
    val urgencyColor = when {
        ticket.elapsedMinutes >= 10 -> MaterialTheme.colorScheme.errorContainer
        ticket.elapsedMinutes >= 5  -> Color(0xFFFFF9C4) // light yellow
        else                        -> MaterialTheme.colorScheme.surface
    }
    val stationStripColor = parseStationColor(ticket.stationColor) ?: Color.Transparent

    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = urgencyColor),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Row(modifier = Modifier.fillMaxWidth().height(IntrinsicSize.Min)) {
            if (stationStripColor != Color.Transparent) {
                Box(
                    modifier = Modifier
                        .width(4.dp)
                        .fillMaxHeight()
                        .background(stationStripColor)
                )
            }

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
                        text = if (isArabic) "منذ ${ticket.elapsedMinutes} د" else "${ticket.elapsedMinutes}m ago",
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
                    text = "${ticket.quantity.toInt()} × $displayName",
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
}

private fun parseStationColor(hex: String?): Color? {
    if (hex.isNullOrBlank()) return null
    return try {
        Color(android.graphics.Color.parseColor(hex))
    } catch (_: IllegalArgumentException) {
        null
    }
}
