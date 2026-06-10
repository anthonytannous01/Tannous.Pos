package com.tannous.pos.feature.reports

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.DemandForecastDto
import com.tannous.pos.core.ui.LocalIsArabic

/**
 * Full demand forecast: complete time-block breakdown, all top items, all ingredients.
 * Reads its data from the shared [DashboardViewModel] (hoisted from the dashboard route).
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ForecastDetailScreen(
    onNavigateBack: () -> Unit,
    viewModel: DashboardViewModel
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val forecast = uiState.forecast
    val isArabic = LocalIsArabic.current

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "التوقعات" else "Forecast") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        }
    ) { padding ->
        when {
            forecast == null -> Box(
                Modifier.fillMaxSize().padding(padding),
                contentAlignment = Alignment.Center
            ) { CircularProgressIndicator() }

            forecast.insufficientDataMessage != null -> Box(
                Modifier.fillMaxSize().padding(padding),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    forecast.insufficientDataMessage!!,
                    style = MaterialTheme.typography.bodyLarge,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(32.dp)
                )
            }

            else -> ForecastDetailContent(
                forecast = forecast,
                isArabic = isArabic,
                modifier = Modifier.fillMaxSize().padding(padding)
            )
        }
    }
}

@Composable
private fun ForecastDetailContent(
    forecast: DemandForecastDto,
    isArabic: Boolean,
    modifier: Modifier = Modifier
) {
    LazyColumn(
        modifier = modifier.padding(horizontal = 12.dp),
        verticalArrangement = Arrangement.spacedBy(10.dp),
        contentPadding = PaddingValues(vertical = 12.dp)
    ) {
        // ── Header ───────────────────────────────────────────────────────────
        item {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    (if (isArabic) "توقعات يوم " else "Forecast for ") +
                        "${forecast.dayOfWeekName}, ${forecast.targetDate.take(10)}",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.weight(1f)
                )
                ConfidenceChip(forecast.confidence)
            }
        }
        item {
            Text(
                "~${forecast.estimatedOrders} " +
                    (if (isArabic) "طلبات" else "orders") +
                    "  |  ~$${forecast.estimatedRevenue.toPlainString()}",
                style = MaterialTheme.typography.titleSmall,
                color = MaterialTheme.colorScheme.primary,
                fontWeight = FontWeight.SemiBold
            )
        }

        // ── Section 1: time breakdown ────────────────────────────────────────
        if (forecast.timeBlocks.isNotEmpty()) {
            item {
                Text(
                    if (isArabic) "التوزيع الزمني" else "Time breakdown",
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
            items(forecast.timeBlocks) { block ->
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(
                        containerColor =
                            if (block.isPeakBlock) MaterialTheme.colorScheme.primaryContainer
                            else MaterialTheme.colorScheme.surfaceVariant
                    )
                ) {
                    Row(
                        Modifier.fillMaxWidth().padding(12.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column {
                            Text(
                                block.label,
                                style = MaterialTheme.typography.bodyMedium,
                                fontWeight = if (block.isPeakBlock) FontWeight.Bold
                                             else FontWeight.Medium
                            )
                            if (block.isPeakBlock) {
                                Text(
                                    if (isArabic) "وقت الذروة" else "Peak time",
                                    style = MaterialTheme.typography.labelSmall,
                                    color = MaterialTheme.colorScheme.primary
                                )
                            }
                        }
                        Text(
                            "~${block.estimatedOrders} " +
                                (if (isArabic) "طلبات" else "orders") +
                                " · $${block.estimatedSales.toPlainString()}",
                            style = MaterialTheme.typography.bodyMedium,
                            fontWeight = FontWeight.SemiBold
                        )
                    }
                }
            }
        }

        // ── Section 2: all top items ─────────────────────────────────────────
        if (forecast.topItems.isNotEmpty()) {
            item {
                Text(
                    if (isArabic) "أصناف للتحضير" else "Items to prep",
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
            items(forecast.topItems) { item ->
                val name = if (isArabic)
                    item.nameAr?.takeIf { it.isNotBlank() } ?: item.name
                else item.name
                Card(Modifier.fillMaxWidth()) {
                    Row(
                        Modifier.fillMaxWidth().padding(12.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text(
                            name,
                            style = MaterialTheme.typography.bodyMedium,
                            fontWeight = FontWeight.Medium,
                            modifier = Modifier.weight(1f)
                        )
                        Text(
                            "×${item.estimatedQty.stripTrailingZeros().toPlainString()}",
                            style = MaterialTheme.typography.bodyLarge,
                            fontWeight = FontWeight.Bold
                        )
                    }
                }
            }
        }

        // ── Section 3: all ingredients ───────────────────────────────────────
        if (forecast.ingredientDemands.isNotEmpty()) {
            item {
                Text(
                    if (isArabic) "مكونات للتخزين" else "Ingredients to stock",
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
            items(forecast.ingredientDemands) { ing ->
                val name = if (isArabic)
                    ing.nameAr?.takeIf { it.isNotBlank() } ?: ing.name
                else ing.name
                Card(Modifier.fillMaxWidth()) {
                    Row(
                        Modifier.fillMaxWidth().padding(12.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text(
                            name,
                            style = MaterialTheme.typography.bodyMedium,
                            fontWeight = FontWeight.Medium,
                            modifier = Modifier.weight(1f)
                        )
                        Text(
                            "${ing.estimatedQty.stripTrailingZeros().toPlainString()} ${ing.unit}",
                            style = MaterialTheme.typography.bodyLarge,
                            fontWeight = FontWeight.Bold
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun ConfidenceChip(confidence: String) {
    val color = when (confidence) {
        "High"   -> MaterialTheme.colorScheme.primaryContainer
        "Medium" -> MaterialTheme.colorScheme.secondaryContainer
        else     -> MaterialTheme.colorScheme.surfaceVariant
    }
    Surface(color = color, shape = MaterialTheme.shapes.small) {
        Text(
            confidence,
            style = MaterialTheme.typography.labelMedium,
            fontWeight = FontWeight.SemiBold,
            modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp)
        )
    }
}
