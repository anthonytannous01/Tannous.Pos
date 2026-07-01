package com.tannous.pos.feature.settings

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.TableRestaurant
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.FloorPlanDto
import com.tannous.pos.core.ui.LocalIsArabic
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TableManagementScreen(
    onNavigateBack: () -> Unit,
    viewModel: TableManagementViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic = LocalIsArabic.current
    val snackbarHostState = remember { SnackbarHostState() }
    val scope = rememberCoroutineScope()

    var showAddFloorPlanDialog by remember { mutableStateOf(false) }
    var addTableForPlanId by remember { mutableStateOf<String?>(null) }

    // Show errors as snackbar
    LaunchedEffect(uiState.error) {
        uiState.error?.let {
            scope.launch { snackbarHostState.showSnackbar(it) }
            viewModel.clearError()
        }
    }

    if (showAddFloorPlanDialog) {
        AddFloorPlanDialog(
            isArabic = isArabic,
            onConfirm = { name, description ->
                viewModel.createFloorPlan(name, description)
                showAddFloorPlanDialog = false
            },
            onDismiss = { showAddFloorPlanDialog = false }
        )
    }

    addTableForPlanId?.let { planId ->
        AddTableDialog(
            isArabic = isArabic,
            onConfirm = { tableNumber, capacity, label ->
                viewModel.createTable(planId, tableNumber, capacity, label)
                addTableForPlanId = null
            },
            onDismiss = { addTableForPlanId = null }
        )
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "إدارة الطاولات" else "Table Management") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(
                            if (isArabic) Icons.Default.ArrowBack else Icons.Default.ArrowBack,
                            contentDescription = null
                        )
                    }
                },
                actions = {
                    IconButton(onClick = { showAddFloorPlanDialog = true }) {
                        Icon(Icons.Default.Add, contentDescription = if (isArabic) "إضافة منطقة" else "Add Floor Plan")
                    }
                }
            )
        },
        snackbarHost = { SnackbarHost(snackbarHostState) }
    ) { padding ->
        Box(modifier = Modifier.fillMaxSize().padding(padding)) {
            when {
                uiState.isLoading -> {
                    CircularProgressIndicator(modifier = Modifier.align(Alignment.Center))
                }
                uiState.floorPlans.isEmpty() -> {
                    Column(
                        modifier = Modifier.align(Alignment.Center).padding(32.dp),
                        horizontalAlignment = Alignment.CenterHorizontally,
                        verticalArrangement = Arrangement.spacedBy(16.dp)
                    ) {
                        Icon(
                            Icons.Default.TableRestaurant,
                            contentDescription = null,
                            modifier = Modifier.size(64.dp),
                            tint = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                        Text(
                            if (isArabic) "لا توجد مناطق بعد" else "No floor plans yet",
                            style = MaterialTheme.typography.titleMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                        Text(
                            if (isArabic) "اضغط + لإضافة منطقة (مثل: الداخل، الشرفة)" else "Tap + to add a zone (e.g. Indoor, Terrace)",
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                        Button(onClick = { showAddFloorPlanDialog = true }) {
                            Text(if (isArabic) "إضافة منطقة" else "Add Floor Plan")
                        }
                    }
                }
                else -> {
                    LazyColumn(
                        modifier = Modifier.fillMaxSize(),
                        contentPadding = PaddingValues(16.dp),
                        verticalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        items(uiState.floorPlans) { plan ->
                            FloorPlanCard(
                                plan = plan,
                                isArabic = isArabic,
                                onAddTable = { addTableForPlanId = plan.id },
                                onDeleteTable = { tableId -> viewModel.deleteTable(tableId) }
                            )
                        }
                    }
                }
            }

            if (uiState.isSaving) {
                CircularProgressIndicator(modifier = Modifier.align(Alignment.Center))
            }
        }
    }
}

@Composable
private fun FloorPlanCard(
    plan: FloorPlanDto,
    isArabic: Boolean,
    onAddTable: () -> Unit,
    onDeleteTable: (String) -> Unit
) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(plan.name, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
                    plan.description?.let {
                        Text(it, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                }
                TextButton(onClick = onAddTable) {
                    Icon(Icons.Default.Add, contentDescription = null, modifier = Modifier.size(16.dp))
                    Spacer(Modifier.width(4.dp))
                    Text(if (isArabic) "إضافة طاولة" else "Add Table")
                }
            }

            if (plan.tables.isEmpty()) {
                Text(
                    if (isArabic) "لا توجد طاولات — اضغط \"إضافة طاولة\"" else "No tables yet — tap \"Add Table\"",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(top = 4.dp)
                )
            } else {
                Divider()
                plan.tables.forEach { table ->
                    Row(
                        modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column(modifier = Modifier.weight(1f)) {
                            val label = if (table.label != null) "${table.tableNumber} — ${table.label}" else table.tableNumber
                            Text(label, style = MaterialTheme.typography.bodyMedium)
                            Text(
                                if (isArabic) "سعة: ${table.capacity}" else "Capacity: ${table.capacity}",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                        IconButton(onClick = { onDeleteTable(table.id) }) {
                            Icon(
                                Icons.Default.Delete,
                                contentDescription = if (isArabic) "حذف" else "Delete",
                                tint = MaterialTheme.colorScheme.error
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun AddFloorPlanDialog(
    isArabic: Boolean,
    onConfirm: (String, String?) -> Unit,
    onDismiss: () -> Unit
) {
    var name by remember { mutableStateOf("") }
    var description by remember { mutableStateOf("") }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(if (isArabic) "إضافة منطقة جديدة" else "Add Floor Plan") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it },
                    label = { Text(if (isArabic) "الاسم *" else "Name *") },
                    placeholder = { Text(if (isArabic) "مثال: الداخل" else "e.g. Indoor") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = description,
                    onValueChange = { description = it },
                    label = { Text(if (isArabic) "الوصف (اختياري)" else "Description (optional)") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
            }
        },
        confirmButton = {
            TextButton(
                onClick = { onConfirm(name, description.ifBlank { null }) },
                enabled = name.isNotBlank()
            ) {
                Text(if (isArabic) "إضافة" else "Add")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text(if (isArabic) "إلغاء" else "Cancel")
            }
        }
    )
}

@Composable
private fun AddTableDialog(
    isArabic: Boolean,
    onConfirm: (String, Int, String?) -> Unit,
    onDismiss: () -> Unit
) {
    var tableNumber by remember { mutableStateOf("") }
    var label by remember { mutableStateOf("") }
    var capacityText by remember { mutableStateOf("2") }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(if (isArabic) "إضافة طاولة" else "Add Table") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedTextField(
                    value = tableNumber,
                    onValueChange = { tableNumber = it },
                    label = { Text(if (isArabic) "رقم الطاولة *" else "Table Number *") },
                    placeholder = { Text(if (isArabic) "مثال: T1" else "e.g. T1") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = label,
                    onValueChange = { label = it },
                    label = { Text(if (isArabic) "الاسم (اختياري)" else "Label (optional)") },
                    placeholder = { Text(if (isArabic) "مثال: بجانب النافذة" else "e.g. Window seat") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = capacityText,
                    onValueChange = { if (it.all(Char::isDigit)) capacityText = it },
                    label = { Text(if (isArabic) "السعة" else "Capacity") },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
            }
        },
        confirmButton = {
            TextButton(
                onClick = {
                    val capacity = capacityText.toIntOrNull()?.coerceAtLeast(1) ?: 2
                    onConfirm(tableNumber, capacity, label.ifBlank { null })
                },
                enabled = tableNumber.isNotBlank()
            ) {
                Text(if (isArabic) "إضافة" else "Add")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text(if (isArabic) "إلغاء" else "Cancel")
            }
        }
    )
}
