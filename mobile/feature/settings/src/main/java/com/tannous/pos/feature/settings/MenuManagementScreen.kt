package com.tannous.pos.feature.settings

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.local.entity.CategoryEntity
import com.tannous.pos.core.data.local.entity.MenuItemEntity
import com.tannous.pos.core.ui.LocalIsArabic

// ──────────────────────────────────────────────────────────────────────────────
// Category dialog  (create / edit)
// ──────────────────────────────────────────────────────────────────────────────
@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun CategoryDialog(
    initial: CategoryEntity?,          // null = create mode
    onConfirm: (name: String, description: String?) -> Unit,
    onDismiss: () -> Unit
) {
    val isArabic = LocalIsArabic.current
    var name        by remember(initial) { mutableStateOf(initial?.name ?: "") }
    var description by remember(initial) { mutableStateOf(initial?.description ?: "") }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(if (initial == null) (if (isArabic) "فئة جديدة" else "New Category")
                       else (if (isArabic) "تعديل الفئة" else "Edit Category")) },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it },
                    label = { Text(if (isArabic) "الاسم *" else "Name *") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = description,
                    onValueChange = { description = it },
                    label = { Text(if (isArabic) "الوصف" else "Description") },
                    modifier = Modifier.fillMaxWidth()
                )
            }
        },
        confirmButton = {
            TextButton(
                onClick = {
                    if (name.isNotBlank()) {
                        onConfirm(name.trim(), description.takeIf { it.isNotBlank() })
                    }
                },
                enabled = name.isNotBlank()
            ) { Text(if (initial == null) (if (isArabic) "إنشاء" else "Create") else (if (isArabic) "حفظ" else "Save")) }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text(if (isArabic) "إلغاء" else "Cancel") }
        }
    )
}

// ──────────────────────────────────────────────────────────────────────────────
// Menu item dialog  (create / edit)
// ──────────────────────────────────────────────────────────────────────────────
@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun MenuItemDialog(
    initial: MenuItemEntity?,
    categories: List<CategoryEntity>,
    preselectedCategoryId: String?,
    onConfirm: (name: String, nameAr: String?, price: String, categoryId: String, description: String?) -> Unit,
    onDismiss: () -> Unit
) {
    var name        by remember(initial) { mutableStateOf(initial?.name ?: "") }
    var nameAr      by remember(initial) { mutableStateOf(initial?.let {
        // MenuItemEntity may store nameAr via description field — it's not in entity directly.
        // nameAr is sent to server but not stored separately in Room, so we leave it blank on edit.
        ""
    } ?: "") }
    var price       by remember(initial) { mutableStateOf(initial?.price?.toPlainString() ?: "") }
    var description by remember(initial) { mutableStateOf(initial?.description ?: "") }
    var categoryId  by remember(initial) {
        mutableStateOf(initial?.categoryId ?: preselectedCategoryId ?: categories.firstOrNull()?.id ?: "")
    }
    val isArabic = LocalIsArabic.current
    var categoryMenuExpanded by remember { mutableStateOf(false) }
    var priceError  by remember { mutableStateOf(false) }

    val selectedCategory = categories.find { it.id == categoryId }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(if (initial == null) (if (isArabic) "عنصر قائمة جديد" else "New Menu Item")
                       else (if (isArabic) "تعديل العنصر" else "Edit Item")) },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it },
                    label = { Text("Name (EN) *") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = nameAr,
                    onValueChange = { nameAr = it },
                    label = { Text("Name (AR)") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = price,
                    onValueChange = {
                        price = it
                        priceError = it.toBigDecimalOrNull() == null && it.isNotBlank()
                    },
                    label = { Text(if (isArabic) "السعر *" else "Price *") },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                    isError = priceError,
                    supportingText = if (priceError) ({ Text(if (isArabic) "أدخل رقماً صحيحاً" else "Enter a valid number") }) else null,
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = description,
                    onValueChange = { description = it },
                    label = { Text(if (isArabic) "الوصف" else "Description") },
                    modifier = Modifier.fillMaxWidth()
                )
                // Category dropdown
                ExposedDropdownMenuBox(
                    expanded = categoryMenuExpanded,
                    onExpandedChange = { categoryMenuExpanded = it }
                ) {
                    OutlinedTextField(
                        value = selectedCategory?.name ?: if (isArabic) "اختر فئة" else "Select category",
                        onValueChange = {},
                        readOnly = true,
                        label = { Text(if (isArabic) "الفئة *" else "Category *") },
                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = categoryMenuExpanded) },
                        modifier = Modifier.fillMaxWidth().menuAnchor()
                    )
                    ExposedDropdownMenu(
                        expanded = categoryMenuExpanded,
                        onDismissRequest = { categoryMenuExpanded = false }
                    ) {
                        categories.forEach { cat ->
                            DropdownMenuItem(
                                text = { Text(cat.name) },
                                onClick = {
                                    categoryId = cat.id
                                    categoryMenuExpanded = false
                                }
                            )
                        }
                    }
                }
            }
        },
        confirmButton = {
            TextButton(
                onClick = {
                    val p = price.trim()
                    if (name.isNotBlank() && categoryId.isNotBlank() && p.toBigDecimalOrNull() != null) {
                        onConfirm(name.trim(), nameAr.takeIf { it.isNotBlank() }, p, categoryId, description.takeIf { it.isNotBlank() })
                    }
                },
                enabled = name.isNotBlank() && categoryId.isNotBlank() && price.toBigDecimalOrNull() != null
            ) { Text(if (initial == null) (if (isArabic) "إضافة" else "Add") else (if (isArabic) "حفظ" else "Save")) }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text(if (isArabic) "إلغاء" else "Cancel") }
        }
    )
}

// ──────────────────────────────────────────────────────────────────────────────
// Delete confirmation dialog
// ──────────────────────────────────────────────────────────────────────────────
@Composable
private fun DeleteConfirmDialog(label: String, onConfirm: () -> Unit, onDismiss: () -> Unit) {
    val isArabic = LocalIsArabic.current
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(if (isArabic) "حذف" else "Delete") },
        text = { Text(if (isArabic) "حذف \"$label\"؟ لا يمكن التراجع عن هذا." else "Delete \"$label\"? This cannot be undone.") },
        confirmButton = {
            TextButton(onClick = { onConfirm(); onDismiss() }) {
                Text(if (isArabic) "حذف" else "Delete", color = MaterialTheme.colorScheme.error)
            }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text(if (isArabic) "إلغاء" else "Cancel") } }
    )
}

// ──────────────────────────────────────────────────────────────────────────────
// Main screen
// ──────────────────────────────────────────────────────────────────────────────
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MenuManagementScreen(
    onNavigateBack: () -> Unit,
    viewModel: MenuManagementViewModel = hiltViewModel()
) {
    val uiState     by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic    = LocalIsArabic.current
    val snackbarHostState = remember { SnackbarHostState() }

    // dialogs state
    var showCategoryDialog   by remember { mutableStateOf(false) }
    var editingCategory      by remember { mutableStateOf<CategoryEntity?>(null) }
    var deletingCategory     by remember { mutableStateOf<CategoryEntity?>(null) }

    var showItemDialog       by remember { mutableStateOf(false) }
    var editingItem          by remember { mutableStateOf<MenuItemEntity?>(null) }
    var deletingItem         by remember { mutableStateOf<MenuItemEntity?>(null) }

    // Snackbar for errors / success
    LaunchedEffect(uiState.error, uiState.successMessage) {
        uiState.error?.let {
            snackbarHostState.showSnackbar(it, duration = SnackbarDuration.Long)
            viewModel.clearMessage()
        }
        uiState.successMessage?.let {
            snackbarHostState.showSnackbar(it)
            viewModel.clearMessage()
        }
    }

    // Category dialog
    if (showCategoryDialog) {
        CategoryDialog(
            initial = editingCategory,
            onConfirm = { name, desc ->
                val editing = editingCategory
                if (editing == null) viewModel.createCategory(name, desc)
                else viewModel.updateCategory(editing.id, name, desc)
                showCategoryDialog = false
                editingCategory = null
            },
            onDismiss = { showCategoryDialog = false; editingCategory = null }
        )
    }

    // Menu item dialog
    if (showItemDialog) {
        MenuItemDialog(
            initial = editingItem,
            categories = uiState.categories,
            preselectedCategoryId = uiState.selectedCategoryId,
            onConfirm = { name, nameAr, price, catId, desc ->
                val editing = editingItem
                if (editing == null) viewModel.createMenuItem(name, nameAr, price, catId, desc)
                else viewModel.updateMenuItem(editing.id, name, nameAr, price, catId, desc)
                showItemDialog = false
                editingItem = null
            },
            onDismiss = { showItemDialog = false; editingItem = null }
        )
    }

    // Delete category confirm
    deletingCategory?.let { cat ->
        DeleteConfirmDialog(
            label = cat.name,
            onConfirm = { viewModel.deleteCategory(cat.id) },
            onDismiss = { deletingCategory = null }
        )
    }

    // Delete item confirm
    deletingItem?.let { item ->
        val displayName = if (isArabic) item.name else item.name
        DeleteConfirmDialog(
            label = displayName,
            onConfirm = { viewModel.deleteMenuItem(item.id) },
            onDismiss = { deletingItem = null }
        )
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "إدارة القائمة" else "Manage Menu") },
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
    ) { paddingValues ->
        if (uiState.isLoading && uiState.categories.isEmpty()) {
            Box(
                modifier = Modifier.fillMaxSize().padding(paddingValues),
                contentAlignment = Alignment.Center
            ) { CircularProgressIndicator() }
        } else {
            // Two-panel layout: categories (left) + items (right)
            Row(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(paddingValues)
            ) {
                // ── LEFT: Categories panel ─────────────────────────────────
                Column(
                    modifier = Modifier
                        .width(260.dp)
                        .fillMaxHeight()
                        .background(MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.4f))
                        .padding(8.dp)
                ) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text(
                            text = if (isArabic) "الفئات" else "Categories",
                            style = MaterialTheme.typography.titleSmall,
                            fontWeight = FontWeight.SemiBold,
                            modifier = Modifier.padding(start = 4.dp, bottom = 4.dp)
                        )
                        IconButton(
                            onClick = { editingCategory = null; showCategoryDialog = true },
                            modifier = Modifier.size(32.dp)
                        ) {
                            Icon(Icons.Default.Add, contentDescription = "Add category",
                                modifier = Modifier.size(18.dp))
                        }
                    }

                    // "All items" filter chip
                    val allSelected = uiState.selectedCategoryId == null
                    Card(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(vertical = 2.dp)
                            .clickable { viewModel.selectCategory(null) },
                        colors = CardDefaults.cardColors(
                            containerColor = if (allSelected)
                                MaterialTheme.colorScheme.primaryContainer
                            else
                                MaterialTheme.colorScheme.surface
                        )
                    ) {
                        Text(
                            text = if (isArabic) "الكل" else "All Items",
                            modifier = Modifier.padding(horizontal = 12.dp, vertical = 10.dp),
                            style = MaterialTheme.typography.bodyMedium,
                            fontWeight = if (allSelected) FontWeight.SemiBold else FontWeight.Normal
                        )
                    }

                    LazyColumn(modifier = Modifier.fillMaxSize()) {
                        items(uiState.categories, key = { it.id }) { cat ->
                            val selected = uiState.selectedCategoryId == cat.id
                            Card(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(vertical = 2.dp)
                                    .clickable { viewModel.selectCategory(cat.id) },
                                colors = CardDefaults.cardColors(
                                    containerColor = if (selected)
                                        MaterialTheme.colorScheme.primaryContainer
                                    else
                                        MaterialTheme.colorScheme.surface
                                )
                            ) {
                                Row(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(horizontal = 12.dp, vertical = 6.dp),
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Text(
                                        text = cat.name,
                                        style = MaterialTheme.typography.bodyMedium,
                                        fontWeight = if (selected) FontWeight.SemiBold else FontWeight.Normal,
                                        modifier = Modifier.weight(1f)
                                    )
                                    IconButton(
                                        onClick = { editingCategory = cat; showCategoryDialog = true },
                                        modifier = Modifier.size(28.dp)
                                    ) {
                                        Icon(Icons.Default.Edit, contentDescription = "Edit",
                                            modifier = Modifier.size(14.dp),
                                            tint = MaterialTheme.colorScheme.onSurfaceVariant)
                                    }
                                    IconButton(
                                        onClick = { deletingCategory = cat },
                                        modifier = Modifier.size(28.dp)
                                    ) {
                                        Icon(Icons.Default.Delete, contentDescription = "Delete",
                                            modifier = Modifier.size(14.dp),
                                            tint = MaterialTheme.colorScheme.error)
                                    }
                                }
                            }
                        }
                    }
                }

                // ── RIGHT: Menu items panel ────────────────────────────────
                val visibleItems = if (uiState.selectedCategoryId == null) {
                    uiState.menuItems
                } else {
                    uiState.menuItems.filter { it.categoryId == uiState.selectedCategoryId }
                }

                val selectedCatName = uiState.categories
                    .find { it.id == uiState.selectedCategoryId }?.name

                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(horizontal = 12.dp, vertical = 8.dp)
                ) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Column {
                            Text(
                                text = selectedCatName
                                    ?: if (isArabic) "جميع العناصر" else "All Items",
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.SemiBold
                            )
                            Text(
                                text = "${visibleItems.size} ${if (isArabic) "عنصر" else "items"}",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                        Button(
                            onClick = {
                                editingItem = null
                                showItemDialog = true
                            },
                            enabled = uiState.categories.isNotEmpty()
                        ) {
                            Icon(Icons.Default.Add, contentDescription = null,
                                modifier = Modifier.size(16.dp))
                            Spacer(modifier = Modifier.width(6.dp))
                            Text(if (isArabic) "إضافة عنصر" else "Add Item")
                        }
                    }

                    Spacer(modifier = Modifier.height(8.dp))

                    if (uiState.categories.isEmpty()) {
                        Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                            Column(horizontalAlignment = Alignment.CenterHorizontally,
                                verticalArrangement = Arrangement.spacedBy(8.dp)) {
                                Text(
                                    if (isArabic) "لا توجد فئات بعد" else "No categories yet",
                                    style = MaterialTheme.typography.bodyLarge,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                                Text(
                                    if (isArabic) "أضف فئة من اليسار أولاً"
                                    else "Add a category on the left first",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                            }
                        }
                    } else if (visibleItems.isEmpty()) {
                        Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                            Text(
                                if (isArabic) "لا توجد عناصر في هذه الفئة"
                                else "No items here — tap Add Item",
                                style = MaterialTheme.typography.bodyMedium,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                    } else {
                        LazyColumn(
                            modifier = Modifier.fillMaxSize(),
                            verticalArrangement = Arrangement.spacedBy(6.dp)
                        ) {
                            items(visibleItems, key = { it.id }) { item ->
                                val catName = uiState.categories.find { it.id == item.categoryId }?.name ?: ""
                                MenuItemRow(
                                    item = item,
                                    categoryName = catName,
                                    isArabic = isArabic,
                                    onEdit = { editingItem = item; showItemDialog = true },
                                    onDelete = { deletingItem = item }
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun MenuItemRow(
    item: MenuItemEntity,
    categoryName: String,
    isArabic: Boolean,
    onEdit: () -> Unit,
    onDelete: () -> Unit
) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 10.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = item.name,
                    style = MaterialTheme.typography.bodyLarge,
                    fontWeight = FontWeight.Medium
                )
                if (categoryName.isNotBlank()) {
                    Text(
                        text = categoryName,
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
                item.description?.let { desc ->
                    if (desc.isNotBlank()) {
                        Text(
                            text = desc,
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
            }
            Text(
                text = "$${item.price.toPlainString()}",
                style = MaterialTheme.typography.bodyLarge,
                fontWeight = FontWeight.SemiBold,
                modifier = Modifier.padding(horizontal = 12.dp)
            )
            IconButton(onClick = onEdit) {
                Icon(Icons.Default.Edit, contentDescription = "Edit",
                    tint = MaterialTheme.colorScheme.primary)
            }
            IconButton(onClick = onDelete) {
                Icon(Icons.Default.Delete, contentDescription = "Delete",
                    tint = MaterialTheme.colorScheme.error)
            }
        }
    }
}
