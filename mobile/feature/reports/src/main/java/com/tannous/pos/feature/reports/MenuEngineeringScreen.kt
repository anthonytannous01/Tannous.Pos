package com.tannous.pos.feature.reports

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
import com.tannous.pos.core.data.model.MenuEngineeringItemDto

// Category constants matching backend MenuEngineeringCategory enum
private const val STAR      = 0
private const val PLOWHORSE = 1
private const val PUZZLE    = 2
private const val DOG       = 3

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MenuEngineeringScreen(
    onNavigateBack: () -> Unit,
    viewModel: MenuEngineeringViewModel = hiltViewModel()
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
                        Text("Menu Engineering")
                        uiState.report?.let {
                            Text(
                                "${it.items.size} items · ${it.totalOrders} orders",
                                style = MaterialTheme.typography.labelSmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                    }
                },
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
    ) { padding ->
        when {
            uiState.isLoading -> Box(
                Modifier.fillMaxSize().padding(padding),
                contentAlignment = Alignment.Center
            ) { CircularProgressIndicator() }

            uiState.report == null || uiState.report!!.items.isEmpty() -> Box(
                Modifier.fillMaxSize().padding(padding),
                contentAlignment = Alignment.Center
            ) {
                Text("No sales data for this period.",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }

            else -> {
                val report = uiState.report!!
                val byCategory = report.items.groupBy { it.category }

                LazyColumn(
                    modifier = Modifier.fillMaxSize().padding(padding).padding(horizontal = 12.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp),
                    contentPadding = PaddingValues(vertical = 12.dp)
                ) {
                    // Legend
                    item { MatrixLegend() }

                    // Stars
                    byCategory[STAR]?.let { stars ->
                        item { CategoryHeader("⭐ Stars", stars.size, Color(0xFF1B5E20)) }
                        items(stars) { ItemRow(it) }
                    }

                    // Plowhorses
                    byCategory[PLOWHORSE]?.let { ph ->
                        item { CategoryHeader("🐴 Plowhorses", ph.size, Color(0xFF1565C0)) }
                        items(ph) { ItemRow(it) }
                    }

                    // Puzzles
                    byCategory[PUZZLE]?.let { pz ->
                        item { CategoryHeader("🧩 Puzzles", pz.size, Color(0xFFE65100)) }
                        items(pz) { ItemRow(it) }
                    }

                    // Dogs
                    byCategory[DOG]?.let { dogs ->
                        item { CategoryHeader("🐶 Dogs", dogs.size, Color(0xFF757575)) }
                        items(dogs) { ItemRow(it) }
                    }
                }
            }
        }
    }
}

@Composable
private fun MatrixLegend() {
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
            Text("Classification Guide", style = MaterialTheme.typography.labelLarge,
                fontWeight = FontWeight.Bold)
            Text("⭐ Stars — high popularity + high margin → protect",
                style = MaterialTheme.typography.bodySmall)
            Text("🐴 Plowhorses — popular but low margin → reduce cost or reprice",
                style = MaterialTheme.typography.bodySmall)
            Text("🧩 Puzzles — high margin but unpopular → reposition or bundle",
                style = MaterialTheme.typography.bodySmall)
            Text("🐶 Dogs — low popularity + low margin → remove or overhaul",
                style = MaterialTheme.typography.bodySmall)
        }
    }
}

@Composable
private fun CategoryHeader(label: String, count: Int, color: Color) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .background(color.copy(alpha = 0.12f), MaterialTheme.shapes.small)
            .padding(horizontal = 12.dp, vertical = 6.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text("$label ($count)", style = MaterialTheme.typography.labelLarge,
            fontWeight = FontWeight.Bold, color = color)
    }
}

@Composable
private fun ItemRow(item: MenuEngineeringItemDto) {
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.Top
            ) {
                Column(Modifier.weight(1f)) {
                    Text(item.name, style = MaterialTheme.typography.bodyMedium,
                        fontWeight = FontWeight.Medium)
                    if (item.categoryName.isNotBlank())
                        Text(item.categoryName, style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
                Column(horizontalAlignment = Alignment.End) {
                    Text("${item.unitsSold} sold",
                        style = MaterialTheme.typography.labelMedium,
                        fontWeight = FontWeight.Bold)
                    Text("${item.popularityIndex}% of sales",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
            }

            Divider()

            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                LabelValue("Revenue", "$${item.revenue.toPlainString()}")
                LabelValue("CM/unit", "$${item.contributionMargin.toPlainString()}")
                LabelValue("CM%", "${item.contributionMarginPct}%",
                    color = if (item.isHighMargin) Color(0xFF2E7D32) else Color(0xFFC62828))
            }
        }
    }
}

@Composable
private fun LabelValue(label: String, value: String, color: Color = Color.Unspecified) {
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Text(label, style = MaterialTheme.typography.labelSmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant)
        Text(value, style = MaterialTheme.typography.bodySmall,
            fontWeight = FontWeight.Medium,
            color = if (color == Color.Unspecified) MaterialTheme.colorScheme.onSurface else color)
    }
}
