package com.tannous.pos.feature.sell

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
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
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.TableDto

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TableMapScreen(
    onNavigateBack: () -> Unit,
    onTableSelected: ((tableId: String) -> Unit)? = null, // null = view mode, non-null = picker mode
    viewModel: TablesViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }
    var selectedTable by remember { mutableStateOf<TableDto?>(null) }

    LaunchedEffect(uiState.error) {
        uiState.error?.let {
            snackbarHostState.showSnackbar(it, duration = SnackbarDuration.Short)
            viewModel.clearError()
        }
    }

    // Status change dialog
    if (selectedTable != null && onTableSelected == null) {
        TableStatusDialog(
            table = selectedTable!!,
            onDismiss = { selectedTable = null },
            onStatusChange = { status ->
                viewModel.updateStatus(selectedTable!!.id, status)
                selectedTable = null
            }
        )
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text(if (onTableSelected != null) "Select Table" else "Table Map") },
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
        if (uiState.floorPlans.isEmpty()) {
            Box(Modifier.fillMaxSize().padding(padding), contentAlignment = Alignment.Center) {
                Text("No floor plans configured.\nAdd tables in settings.",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    textAlign = TextAlign.Center)
            }
        } else {
            Column(Modifier.fillMaxSize().padding(padding)) {

                // Floor plan tab strip
                if (uiState.floorPlans.size > 1) {
                    LazyRow(
                        modifier = Modifier.padding(horizontal = 12.dp, vertical = 8.dp),
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        items(uiState.floorPlans) { fp ->
                            val selected = fp.id == uiState.selectedFloorPlanId
                            FilterChip(
                                selected = selected,
                                onClick = { viewModel.selectFloorPlan(fp.id) },
                                label = { Text(fp.name) }
                            )
                        }
                    }
                }

                // Legend
                StatusLegend()

                // Table grid
                val tables = uiState.selectedFloorPlan?.tables ?: emptyList()
                LazyVerticalGrid(
                    columns = GridCells.Adaptive(minSize = 100.dp),
                    modifier = Modifier.fillMaxSize().padding(12.dp),
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    items(tables) { table ->
                        TableCard(
                            table = table,
                            pickerMode = onTableSelected != null,
                            onClick = {
                                if (onTableSelected != null && table.status == TABLE_AVAILABLE) {
                                    onTableSelected(table.id)
                                } else if (onTableSelected == null) {
                                    selectedTable = table
                                }
                            }
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun StatusLegend() {
    Row(
        Modifier.padding(horizontal = 12.dp).fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        listOf(
            TABLE_AVAILABLE to "Available",
            TABLE_OCCUPIED  to "Occupied",
            TABLE_RESERVED  to "Reserved",
            TABLE_CLEANING  to "Cleaning"
        ).forEach { (status, label) ->
            Row(verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                Box(Modifier.size(12.dp).background(tableColor(status), MaterialTheme.shapes.small))
                Text(label, style = MaterialTheme.typography.labelSmall)
            }
        }
    }
}

@Composable
private fun TableCard(table: TableDto, pickerMode: Boolean, onClick: () -> Unit) {
    val color = tableColor(table.status)
    val dimmed = pickerMode && table.status != TABLE_AVAILABLE

    Box(
        modifier = Modifier
            .aspectRatio(1f)
            .background(
                color.copy(alpha = if (dimmed) 0.3f else 1f),
                MaterialTheme.shapes.medium
            )
            .border(
                width = if (table.activeOrderId != null) 2.dp else 0.dp,
                color = MaterialTheme.colorScheme.outline,
                shape = MaterialTheme.shapes.medium
            )
            .clickable(enabled = !dimmed, onClick = onClick),
        contentAlignment = Alignment.Center
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center) {
            Text(
                table.tableNumber,
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
                color = Color.White
            )
            table.label?.let {
                Text(it, style = MaterialTheme.typography.labelSmall, color = Color.White)
            }
            Text(
                "Cap: ${table.capacity}",
                style = MaterialTheme.typography.labelSmall,
                color = Color.White.copy(alpha = 0.8f)
            )
        }
    }
}

@Composable
private fun TableStatusDialog(
    table: TableDto,
    onDismiss: () -> Unit,
    onStatusChange: (Int) -> Unit
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Table ${table.tableNumber}${table.label?.let { " — $it" } ?: ""}") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                Text("Capacity: ${table.capacity} | Current: ${statusLabel(table.status)}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
                listOf(
                    TABLE_AVAILABLE to "Mark Available",
                    TABLE_OCCUPIED  to "Mark Occupied",
                    TABLE_RESERVED  to "Mark Reserved",
                    TABLE_CLEANING  to "Mark Cleaning"
                ).forEach { (status, label) ->
                    if (status != table.status) {
                        OutlinedButton(
                            onClick = { onStatusChange(status) },
                            modifier = Modifier.fillMaxWidth()
                        ) { Text(label) }
                    }
                }
            }
        },
        confirmButton = {},
        dismissButton = {
            TextButton(onClick = onDismiss) { Text("Cancel") }
        }
    )
}

private fun tableColor(status: Int): Color = when (status) {
    TABLE_AVAILABLE -> Color(0xFF2E7D32) // green
    TABLE_OCCUPIED  -> Color(0xFFC62828) // red
    TABLE_RESERVED  -> Color(0xFFE65100) // amber
    TABLE_CLEANING  -> Color(0xFF1565C0) // blue
    else            -> Color(0xFF757575)
}

private fun statusLabel(status: Int): String = when (status) {
    TABLE_AVAILABLE -> "Available"
    TABLE_OCCUPIED  -> "Occupied"
    TABLE_RESERVED  -> "Reserved"
    TABLE_CLEANING  -> "Cleaning"
    else            -> "Unknown"
}
