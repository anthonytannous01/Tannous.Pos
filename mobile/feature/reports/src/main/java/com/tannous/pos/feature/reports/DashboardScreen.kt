package com.tannous.pos.feature.reports

import androidx.compose.foundation.background
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
import com.tannous.pos.core.data.model.HourlySalesDto
import com.tannous.pos.core.data.model.SalesSummaryDto
import java.math.BigDecimal

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DashboardScreen(
    onNavigateBack: () -> Unit,
    viewModel: DashboardViewModel = hiltViewModel()
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
                title = { Text("Sales Dashboard") },
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
        when {
            uiState.isLoading -> Box(
                Modifier.fillMaxSize().padding(padding),
                contentAlignment = Alignment.Center
            ) { CircularProgressIndicator() }

            uiState.summary == null -> Box(
                Modifier.fillMaxSize().padding(padding),
                contentAlignment = Alignment.Center
            ) {
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Text("No data yet", style = MaterialTheme.typography.titleMedium)
                    Text("No paid orders today.", style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
            }

            else -> DashboardContent(
                summary = uiState.summary!!,
                modifier = Modifier.padding(padding)
            )
        }
    }
}

@Composable
private fun DashboardContent(summary: SalesSummaryDto, modifier: Modifier = Modifier) {
    LazyColumn(
        modifier = modifier
            .fillMaxSize()
            .padding(horizontal = 12.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
        contentPadding = PaddingValues(vertical = 12.dp)
    ) {
        // ── Core KPI cards ───────────────────────────────────────────────────
        item {
            Text("Today's Sales", style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold)
        }
        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                KpiCard("Net Sales", "$${summary.netSales.toPlainString()}",
                    MaterialTheme.colorScheme.primaryContainer, Modifier.weight(1f))
                KpiCard("Orders", summary.ordersCount.toString(),
                    MaterialTheme.colorScheme.secondaryContainer, Modifier.weight(1f))
            }
        }
        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                KpiCard("Avg Ticket", "$${summary.avgTicket.toPlainString()}",
                    MaterialTheme.colorScheme.tertiaryContainer, Modifier.weight(1f))
                KpiCard("Void Rate", "${summary.voidRate}%",
                    if (summary.voidRate > BigDecimal("5"))
                        MaterialTheme.colorScheme.errorContainer
                    else
                        MaterialTheme.colorScheme.surfaceVariant,
                    Modifier.weight(1f))
            }
        }

        // ── VAT + stamp duty ─────────────────────────────────────────────────
        if (summary.taxCollected > BigDecimal.ZERO || summary.stampDutyCollected > BigDecimal.ZERO) {
            item {
                Text("Tax & Duties", style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            item {
                Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    if (summary.taxCollected > BigDecimal.ZERO)
                        KpiCard("VAT Collected", "$${summary.taxCollected.toPlainString()}",
                            MaterialTheme.colorScheme.surfaceVariant, Modifier.weight(1f))
                    if (summary.stampDutyCollected > BigDecimal.ZERO)
                        KpiCard("Stamp Duty", "$${summary.stampDutyCollected.toPlainString()}",
                            MaterialTheme.colorScheme.surfaceVariant, Modifier.weight(1f))
                }
            }
        }

        // ── Order type split ─────────────────────────────────────────────────
        item {
            Text("Order Types", style = MaterialTheme.typography.titleSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                KpiCard("Dine-In",   summary.dineInCount.toString(),
                    MaterialTheme.colorScheme.surfaceVariant, Modifier.weight(1f))
                KpiCard("Takeaway",  summary.takeawayCount.toString(),
                    MaterialTheme.colorScheme.surfaceVariant, Modifier.weight(1f))
                KpiCard("Delivery",  summary.deliveryCount.toString(),
                    MaterialTheme.colorScheme.surfaceVariant, Modifier.weight(1f))
            }
        }

        // ── Hourly sparkline ─────────────────────────────────────────────────
        if (summary.hourlySales.isNotEmpty()) {
            item {
                Text("Sales by Hour", style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            item { HourlyChart(summary.hourlySales) }
        }

        // ── Payment methods ──────────────────────────────────────────────────
        if (summary.paymentMethods.isNotEmpty()) {
            item {
                Text("Payment Methods", style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            items(summary.paymentMethods) { pm ->
                Card(Modifier.fillMaxWidth()) {
                    Row(
                        Modifier.fillMaxWidth().padding(12.dp),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Column {
                            Text("${pm.method} (${pm.currency})",
                                style = MaterialTheme.typography.bodyMedium,
                                fontWeight = FontWeight.Medium)
                            Text("${pm.count} transaction(s)",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant)
                        }
                        Text("$${pm.amount.toPlainString()}",
                            style = MaterialTheme.typography.bodyLarge,
                            fontWeight = FontWeight.Bold,
                            color = MaterialTheme.colorScheme.primary)
                    }
                }
            }
        }

        // ── Top items ────────────────────────────────────────────────────────
        if (summary.topItems.isNotEmpty()) {
            item {
                Text("Top Items", style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            items(summary.topItems.take(8)) { item ->
                Card(Modifier.fillMaxWidth()) {
                    Row(
                        Modifier.fillMaxWidth().padding(12.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column(Modifier.weight(1f)) {
                            Text(item.name, style = MaterialTheme.typography.bodyMedium,
                                fontWeight = FontWeight.Medium)
                            Text("Qty: ${item.qty}",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant)
                        }
                        Text("$${item.sales.toPlainString()}",
                            style = MaterialTheme.typography.bodyLarge,
                            fontWeight = FontWeight.Bold)
                    }
                }
            }
        }
    }
}

@Composable
private fun KpiCard(label: String, value: String, color: Color, modifier: Modifier = Modifier) {
    Card(
        modifier = modifier,
        colors = CardDefaults.cardColors(containerColor = color)
    ) {
        Column(
            modifier = Modifier.padding(12.dp),
            verticalArrangement = Arrangement.spacedBy(4.dp)
        ) {
            Text(label, style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant)
            Text(value, style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold)
        }
    }
}

@Composable
private fun HourlyChart(hours: List<HourlySalesDto>) {
    val maxSales = hours.maxOfOrNull { it.sales } ?: BigDecimal.ONE
    Card(Modifier.fillMaxWidth()) {
        LazyRow(
            modifier = Modifier.padding(12.dp),
            horizontalArrangement = Arrangement.spacedBy(4.dp)
        ) {
            items(hours) { h ->
                val heightFraction = if (maxSales > BigDecimal.ZERO)
                    (h.sales.toFloat() / maxSales.toFloat()).coerceIn(0.05f, 1f)
                else 0.05f
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.Bottom,
                    modifier = Modifier.height(80.dp)
                ) {
                    Box(
                        Modifier
                            .width(20.dp)
                            .fillMaxHeight(heightFraction)
                            .background(
                                MaterialTheme.colorScheme.primary,
                                MaterialTheme.shapes.small
                            )
                    )
                    Spacer(Modifier.height(2.dp))
                    Text(
                        "${h.hour}h",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
        }
    }
}
