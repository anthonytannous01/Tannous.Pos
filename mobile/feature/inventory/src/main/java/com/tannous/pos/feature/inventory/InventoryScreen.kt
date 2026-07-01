package com.tannous.pos.feature.inventory

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Clear
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.KeyboardArrowDown
import androidx.compose.material.icons.filled.KeyboardArrowUp
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.IngredientDto
import com.tannous.pos.core.data.model.InventoryItemDto
import com.tannous.pos.core.data.model.RecipeDto
import com.tannous.pos.core.ui.LocalIsArabic
import com.tannous.pos.core.util.currencyFormatterFor
import java.math.BigDecimal

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun InventoryScreen(
    onNavigateBack: () -> Unit,
    viewModel: InventoryViewModel = hiltViewModel(),
    recipeViewModel: RecipeViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val recipeUiState by recipeViewModel.uiState.collectAsStateWithLifecycle()
    val isArabic = LocalIsArabic.current
    val snackbarHostState = remember { SnackbarHostState() }
    val currencyFormatter = remember(uiState.currencyCode) {
        currencyFormatterFor(uiState.currencyCode)
    }

    LaunchedEffect(uiState.selectedTab) {
        if (uiState.selectedTab == 2 &&
            recipeUiState.recipes.isEmpty() &&
            !recipeUiState.isLoading
        ) {
            recipeViewModel.load()
        }
    }

    LaunchedEffect(uiState.submitSuccess) {
        uiState.submitSuccess?.let { msg ->
            snackbarHostState.showSnackbar(msg)
            viewModel.clearSubmitSuccess()
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "المخزون" else "Inventory") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = if (isArabic) "رجوع" else "Back")
                    }
                },
                actions = {
                    IconButton(
                        onClick = {
                            when (uiState.selectedTab) {
                                0 -> viewModel.load()
                                1 -> viewModel.loadIngredients()
                                2 -> recipeViewModel.load()
                            }
                        }
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
            TabRow(selectedTabIndex = uiState.selectedTab) {
                Tab(
                    selected = uiState.selectedTab == 0,
                    onClick = { viewModel.selectTab(0) },
                    text = { Text(if (isArabic) "المخزون" else "Stock") }
                )
                Tab(
                    selected = uiState.selectedTab == 1,
                    onClick = { viewModel.selectTab(1) },
                    text = { Text(if (isArabic) "المكونات" else "Ingredients") }
                )
                Tab(
                    selected = uiState.selectedTab == 2,
                    onClick = { viewModel.selectTab(2) },
                    text = { Text(if (isArabic) "الوصفات" else "Recipes") }
                )
            }

            when (uiState.selectedTab) {
                0 -> StockTabContent(
                    uiState = uiState,
                    currencyFormatter = currencyFormatter,
                    viewModel = viewModel
                )
                1 -> IngredientsTabContent(
                    uiState = uiState,
                    currencyFormatter = currencyFormatter,
                    viewModel = viewModel
                )
                2 -> RecipesTabContent(
                    recipeUiState = recipeUiState,
                    recipeViewModel = recipeViewModel
                )
            }
        }
    }

    val actionItem = uiState.actionItem
    val actionType = uiState.actionType
    if (actionItem != null && actionType != null) {
        InventoryActionDialog(
            item = actionItem,
            actionType = actionType,
            isSubmitting = uiState.isSubmitting,
            submitError = uiState.submitError,
            onDismiss = { viewModel.dismissAction() },
            onSubmit = { quantity, reason ->
                viewModel.submitAction(quantity, reason)
            }
        )
    }

    if (uiState.showIngredientDialog) {
        IngredientDialog(
            ingredient = uiState.editingIngredient,
            isSaving = uiState.isSavingIngredient,
            saveError = uiState.ingredientSaveError,
            onDismiss = { viewModel.dismissIngredientDialog() },
            onSave = { name, description, cost, unit, isActive ->
                viewModel.saveIngredient(name, description, cost, unit, isActive)
            }
        )
    }

    if (uiState.showDeleteConfirm) {
        val isArabicLocal = LocalIsArabic.current
        AlertDialog(
            onDismissRequest = {
                if (!uiState.isDeletingIngredient) {
                    viewModel.dismissDelete()
                }
            },
            title = { Text(if (isArabicLocal) "حذف المكون" else "Delete Ingredient") },
            text = {
                Column {
                    Text(if (isArabicLocal) "حذف \"${uiState.deletingIngredient?.name}\"؟" else "Delete \"${uiState.deletingIngredient?.name}\"?")
                    if (uiState.deleteError != null) {
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            uiState.deleteError!!,
                            color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.bodySmall
                        )
                    }
                }
            },
            confirmButton = {
                Button(
                    onClick = { viewModel.deleteIngredient(force = false) },
                    enabled = !uiState.isDeletingIngredient,
                    colors = ButtonDefaults.buttonColors(
                        containerColor = MaterialTheme.colorScheme.error
                    )
                ) {
                    if (uiState.isDeletingIngredient) {
                        CircularProgressIndicator(modifier = Modifier.size(16.dp))
                    } else {
                        Text(if (isArabicLocal) "حذف" else "Delete")
                    }
                }
            },
            dismissButton = {
                TextButton(
                    onClick = { viewModel.dismissDelete() },
                    enabled = !uiState.isDeletingIngredient
                ) {
                    Text(if (isArabicLocal) "إلغاء" else "Cancel")
                }
            }
        )
    }

    if (uiState.showForceDeleteConfirm) {
        val isArabicLocal = LocalIsArabic.current
        AlertDialog(
            onDismissRequest = {
                if (!uiState.isDeletingIngredient) {
                    viewModel.dismissDelete()
                }
            },
            title = { Text(if (isArabicLocal) "المكون قيد الاستخدام" else "Ingredient In Use") },
            text = {
                Text(
                    if (isArabicLocal)
                        "\"${uiState.deletingIngredient?.name}\" مستخدم في وصفات نشطة. سيؤدي الحذف القسري إلى تعطيل تلك الوصفات. لا يمكن التراجع عن هذا."
                    else
                        "\"${uiState.deletingIngredient?.name}\" is used in active recipes. Force deleting will deactivate those recipes. This cannot be undone."
                )
            },
            confirmButton = {
                Button(
                    onClick = { viewModel.deleteIngredient(force = true) },
                    enabled = !uiState.isDeletingIngredient,
                    colors = ButtonDefaults.buttonColors(
                        containerColor = MaterialTheme.colorScheme.error
                    )
                ) {
                    if (uiState.isDeletingIngredient) {
                        CircularProgressIndicator(modifier = Modifier.size(16.dp))
                    } else {
                        Text(if (isArabicLocal) "حذف قسري" else "Force Delete")
                    }
                }
            },
            dismissButton = {
                TextButton(
                    onClick = { viewModel.dismissDelete() },
                    enabled = !uiState.isDeletingIngredient
                ) {
                    Text(if (isArabicLocal) "إلغاء" else "Cancel")
                }
            }
        )
    }

    // ── Recipe dialogs ────────────────────────────────────────────────────────

    if (recipeUiState.showRecipeDialog) {
        RecipeFormDialog(
            recipeUiState = recipeUiState,
            recipeViewModel = recipeViewModel
        )
    }

    if (recipeUiState.showDeleteConfirm) {
        val isArabicLocal = LocalIsArabic.current
        AlertDialog(
            onDismissRequest = { if (!recipeUiState.isDeleting) recipeViewModel.dismissDelete() },
            title = { Text(if (isArabicLocal) "حذف الوصفة" else "Delete Recipe") },
            text = {
                Column {
                    Text(if (isArabicLocal) "حذف \"${recipeUiState.deletingRecipe?.name}\"؟" else "Delete \"${recipeUiState.deletingRecipe?.name}\"?")
                    if (recipeUiState.deleteError != null) {
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            recipeUiState.deleteError!!,
                            color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.bodySmall
                        )
                    }
                }
            },
            confirmButton = {
                Button(
                    onClick = { recipeViewModel.deleteRecipe(force = false) },
                    enabled = !recipeUiState.isDeleting,
                    colors = ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.error)
                ) {
                    if (recipeUiState.isDeleting) CircularProgressIndicator(Modifier.size(16.dp))
                    else Text(if (isArabicLocal) "حذف" else "Delete")
                }
            },
            dismissButton = {
                TextButton(onClick = { recipeViewModel.dismissDelete() }, enabled = !recipeUiState.isDeleting) {
                    Text(if (isArabicLocal) "إلغاء" else "Cancel")
                }
            }
        )
    }

    if (recipeUiState.showForceDeleteConfirm) {
        val isArabicLocal = LocalIsArabic.current
        AlertDialog(
            onDismissRequest = { if (!recipeUiState.isDeleting) recipeViewModel.dismissDelete() },
            title = { Text(if (isArabicLocal) "الوصفة قيد الاستخدام" else "Recipe In Use") },
            text = {
                Text(
                    if (isArabicLocal)
                        "\"${recipeUiState.deletingRecipe?.name}\" مرتبطة بعنصر قائمة نشط. سيؤدي الحذف القسري إلى تعطيل ذلك العنصر. لا يمكن التراجع عن هذا."
                    else
                        "\"${recipeUiState.deletingRecipe?.name}\" is linked to an active menu item. Force deleting will deactivate that menu item. This cannot be undone."
                )
            },
            confirmButton = {
                Button(
                    onClick = { recipeViewModel.deleteRecipe(force = true) },
                    enabled = !recipeUiState.isDeleting,
                    colors = ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.error)
                ) {
                    if (recipeUiState.isDeleting) CircularProgressIndicator(Modifier.size(16.dp))
                    else Text(if (isArabicLocal) "حذف قسري" else "Force Delete")
                }
            },
            dismissButton = {
                TextButton(onClick = { recipeViewModel.dismissDelete() }, enabled = !recipeUiState.isDeleting) {
                    Text(if (isArabicLocal) "إلغاء" else "Cancel")
                }
            }
        )
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun StockTabContent(
    uiState: InventoryUiState,
    currencyFormatter: java.text.NumberFormat,
    viewModel: InventoryViewModel
) {
    val isArabic = LocalIsArabic.current
    Row(
        modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)
    ) {
        FilterChip(
            selected = uiState.filter == InventoryFilter.All,
            onClick = { viewModel.setFilter(InventoryFilter.All) },
            label = { Text(if (isArabic) "الكل" else "All") }
        )
        Spacer(modifier = Modifier.width(8.dp))
        FilterChip(
            selected = uiState.filter == InventoryFilter.LowStock,
            onClick = { viewModel.setFilter(InventoryFilter.LowStock) },
            label = { Text(if (isArabic) "مخزون منخفض" else "Low Stock") }
        )
    }

    when {
        uiState.isLoading -> {
            Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator()
            }
        }
        uiState.error != null -> {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(16.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.Center
            ) {
                Text(
                    uiState.error!!,
                    color = MaterialTheme.colorScheme.error
                )
                Spacer(modifier = Modifier.height(8.dp))
                Button(onClick = { viewModel.load() }) {
                    Text(if (isArabic) "إعادة المحاولة" else "Retry")
                }
            }
        }
        uiState.items.isEmpty() -> {
            Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    if (uiState.filter == InventoryFilter.LowStock) {
                        if (isArabic) "لا توجد عناصر منخفضة المخزون" else "No low stock items"
                    } else {
                        if (isArabic) "لا توجد عناصر مخزون" else "No inventory items"
                    }
                )
            }
        }
        else -> {
            LazyColumn(modifier = Modifier.fillMaxSize()) {
                items(uiState.items, key = { it.id }) { item ->
                    InventoryItemRow(
                        item = item,
                        currencyFormatter = currencyFormatter,
                        onAdjust = {
                            viewModel.openAction(item, InventoryAction.Adjust)
                        },
                        onWastage = {
                            viewModel.openAction(item, InventoryAction.Wastage)
                        }
                    )
                }
            }
        }
    }
}

@Composable
private fun IngredientsTabContent(
    uiState: InventoryUiState,
    currencyFormatter: java.text.NumberFormat,
    viewModel: InventoryViewModel
) {
    val isArabic = LocalIsArabic.current
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 8.dp),
        horizontalArrangement = Arrangement.End
    ) {
        Button(onClick = { viewModel.openCreateIngredient() }) {
            Icon(Icons.Default.Add, contentDescription = null, modifier = Modifier.size(18.dp))
            Spacer(modifier = Modifier.width(4.dp))
            Text(if (isArabic) "إضافة مكون" else "Add Ingredient")
        }
    }

    when {
        uiState.isIngredientsLoading -> {
            Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator()
            }
        }
        uiState.ingredientsError != null -> {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(16.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.Center
            ) {
                Text(
                    uiState.ingredientsError!!,
                    color = MaterialTheme.colorScheme.error
                )
                Spacer(modifier = Modifier.height(8.dp))
                Button(onClick = { viewModel.loadIngredients() }) {
                    Text(if (isArabic) "إعادة المحاولة" else "Retry")
                }
            }
        }
        uiState.ingredients.isEmpty() -> {
            Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                Text(if (isArabic) "لا توجد مكونات. اضغط إضافة مكون لإنشاء واحد." else "No ingredients. Tap Add Ingredient to create one.")
            }
        }
        else -> {
            LazyColumn(modifier = Modifier.fillMaxSize()) {
                items(uiState.ingredients, key = { it.id }) { ingredient ->
                    IngredientRow(
                        ingredient = ingredient,
                        currencyFormatter = currencyFormatter,
                        onEdit = { viewModel.openEditIngredient(ingredient) },
                        onDelete = { viewModel.confirmDelete(ingredient) }
                    )
                }
            }
        }
    }
}

@Composable
private fun IngredientRow(
    ingredient: IngredientDto,
    currencyFormatter: java.text.NumberFormat,
    onEdit: () -> Unit,
    onDelete: () -> Unit
) {
    val isArabic = LocalIsArabic.current
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 4.dp)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(12.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        ingredient.name,
                        style = MaterialTheme.typography.titleSmall,
                        fontWeight = FontWeight.Medium
                    )
                    if (!ingredient.isActive) {
                        Spacer(modifier = Modifier.width(8.dp))
                        Text(
                            if (isArabic) "غير نشط" else "Inactive",
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
                Text(
                    "${currencyFormatter.format(ingredient.costPerUnit)} / ${ingredient.unit}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                ingredient.description?.let { description ->
                    Text(
                        description,
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis
                    )
                }
            }
            IconButton(onClick = onEdit) {
                Icon(Icons.Default.Edit, contentDescription = if (isArabic) "تعديل" else "Edit")
            }
            IconButton(onClick = onDelete) {
                Icon(
                    Icons.Default.Clear,
                    contentDescription = if (isArabic) "حذف" else "Delete",
                    tint = MaterialTheme.colorScheme.error
                )
            }
        }
    }
}

@Composable
private fun IngredientDialog(
    ingredient: IngredientDto?,
    isSaving: Boolean,
    saveError: String?,
    onDismiss: () -> Unit,
    onSave: (String, String?, BigDecimal, String, Boolean) -> Unit
) {
    val isArabic = LocalIsArabic.current
    var nameInput by rememberSaveable { mutableStateOf("") }
    var descriptionInput by rememberSaveable { mutableStateOf("") }
    var costInput by rememberSaveable { mutableStateOf("") }
    var unitInput by rememberSaveable { mutableStateOf("") }
    var isActiveChecked by rememberSaveable { mutableStateOf(true) }
    var nameError by remember { mutableStateOf<String?>(null) }
    var costError by remember { mutableStateOf<String?>(null) }
    var unitError by remember { mutableStateOf<String?>(null) }

    LaunchedEffect(ingredient?.id) {
        nameInput = ingredient?.name.orEmpty()
        descriptionInput = ingredient?.description.orEmpty()
        costInput = ingredient?.costPerUnit?.stripTrailingZeros()?.toPlainString().orEmpty()
        unitInput = ingredient?.unit.orEmpty()
        isActiveChecked = ingredient?.isActive ?: true
        nameError = null
        costError = null
        unitError = null
    }

    AlertDialog(
        onDismissRequest = { if (!isSaving) onDismiss() },
        title = { Text(if (ingredient == null) (if (isArabic) "مكون جديد" else "New Ingredient") else (if (isArabic) "تعديل المكون" else "Edit Ingredient")) },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(
                    value = nameInput,
                    onValueChange = {
                        nameInput = it
                        nameError = null
                    },
                    label = { Text(if (isArabic) "الاسم" else "Name") },
                    isError = nameError != null,
                    supportingText = nameError?.let { err -> { Text(err) } },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = unitInput,
                    onValueChange = {
                        unitInput = it
                        unitError = null
                    },
                    label = { Text(if (isArabic) "الوحدة" else "Unit") },
                    placeholder = { Text("kg, L, pcs") },
                    isError = unitError != null,
                    supportingText = unitError?.let { err -> { Text(err) } },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = costInput,
                    onValueChange = {
                        costInput = it
                        costError = null
                    },
                    label = { Text(if (isArabic) "التكلفة لكل وحدة" else "Cost per unit") },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                    isError = costError != null,
                    supportingText = costError?.let { err -> { Text(err) } },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = descriptionInput,
                    onValueChange = { descriptionInput = it },
                    label = { Text(if (isArabic) "الوصف (اختياري)" else "Description (optional)") },
                    maxLines = 3,
                    modifier = Modifier.fillMaxWidth()
                )
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text(if (isArabic) "نشط" else "Active")
                    Switch(
                        checked = isActiveChecked,
                        onCheckedChange = { isActiveChecked = it }
                    )
                }
                if (saveError != null) {
                    Text(
                        saveError,
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodySmall
                    )
                }
            }
        },
        confirmButton = {
            Button(
                onClick = {
                    nameError = null
                    costError = null
                    unitError = null
                    val trimmedName = nameInput.trim()
                    when {
                        trimmedName.isBlank() -> nameError = if (isArabic) "الاسم مطلوب" else "Name is required"
                        trimmedName.length > 100 -> nameError = if (isArabic) "100 حرف كحد أقصى" else "Max 100 characters"
                        unitInput.isBlank() -> unitError = if (isArabic) "الوحدة مطلوبة" else "Unit is required"
                        unitInput.length > 20 -> unitError = if (isArabic) "20 حرفاً كحد أقصى" else "Max 20 characters"
                        else -> {
                            val cost = costInput.trim().toBigDecimalOrNull()
                            when {
                                cost == null -> costError = if (isArabic) "أدخل تكلفة صحيحة" else "Enter a valid cost"
                                cost < BigDecimal.ZERO -> costError = if (isArabic) "يجب أن تكون التكلفة ≥ 0" else "Cost must be ≥ 0"
                                else -> onSave(
                                    trimmedName,
                                    descriptionInput.trim(),
                                    cost,
                                    unitInput.trim(),
                                    isActiveChecked
                                )
                            }
                        }
                    }
                },
                enabled = !isSaving
            ) {
                if (isSaving) {
                    CircularProgressIndicator(modifier = Modifier.size(16.dp))
                } else {
                    Text(if (isArabic) "حفظ" else "Save")
                }
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss, enabled = !isSaving) {
                Text(if (isArabic) "إلغاء" else "Cancel")
            }
        }
    )
}

@Composable
private fun InventoryItemRow(
    item: InventoryItemDto,
    currencyFormatter: java.text.NumberFormat,
    onAdjust: () -> Unit,
    onWastage: () -> Unit
) {
    val isArabic = LocalIsArabic.current
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 4.dp)
    ) {
        Column(modifier = Modifier.padding(12.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        item.ingredientName,
                        style = MaterialTheme.typography.titleSmall,
                        fontWeight = FontWeight.Medium
                    )
                    Text(
                        "${item.currentStock.stripTrailingZeros().toPlainString()} ${item.ingredientUnit}",
                        style = MaterialTheme.typography.bodySmall
                    )
                    if (item.currentStock <= item.minimumStock) {
                        Text(
                            if (isArabic) "مخزون منخفض" else "Low stock",
                            color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.labelSmall
                        )
                    }
                }
                Column(horizontalAlignment = Alignment.End) {
                    Text(
                        "${if (isArabic) "الحد الأدنى" else "Min"}: ${item.minimumStock.stripTrailingZeros().toPlainString()}",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Text(
                        "${if (isArabic) "التكلفة" else "Cost"}: ${currencyFormatter.format(item.averageCost)}/${item.ingredientUnit}",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.End
            ) {
                TextButton(onClick = onWastage) {
                    Text(if (isArabic) "هدر" else "Wastage", color = MaterialTheme.colorScheme.error)
                }
                Spacer(modifier = Modifier.width(8.dp))
                TextButton(onClick = onAdjust) {
                    Text(if (isArabic) "تعديل" else "Adjust")
                }
            }
        }
    }
}

@Composable
private fun RecipesTabContent(
    recipeUiState: RecipeUiState,
    recipeViewModel: RecipeViewModel
) {
    val isArabic = LocalIsArabic.current
    Column(modifier = Modifier.fillMaxSize()) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 8.dp),
            horizontalArrangement = Arrangement.End
        ) {
            Button(onClick = { recipeViewModel.openCreateRecipe() }) {
                Icon(Icons.Default.Add, contentDescription = null, modifier = Modifier.size(18.dp))
                Spacer(modifier = Modifier.width(4.dp))
                Text(if (isArabic) "إضافة وصفة" else "Add Recipe")
            }
        }

        when {
            recipeUiState.isLoading -> {
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    CircularProgressIndicator()
                }
            }
            recipeUiState.error != null -> {
                Column(
                    modifier = Modifier.fillMaxSize().padding(16.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.Center
                ) {
                    Text(recipeUiState.error!!, color = MaterialTheme.colorScheme.error)
                    Spacer(modifier = Modifier.height(8.dp))
                    Button(onClick = { recipeViewModel.load() }) { Text(if (isArabic) "إعادة المحاولة" else "Retry") }
                }
            }
            recipeUiState.recipes.isEmpty() -> {
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    Text(if (isArabic) "لا توجد وصفات." else "No recipes found.", color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
            }
            else -> {
                LazyColumn(modifier = Modifier.fillMaxSize()) {
                    items(recipeUiState.recipes, key = { it.id }) { recipe ->
                        RecipeRow(
                            recipe = recipe,
                            isExpanded = recipeUiState.expandedRecipeId == recipe.id,
                            onToggleExpand = { recipeViewModel.toggleExpand(recipe.id) },
                            onEdit = { recipeViewModel.openEditRecipe(recipe) },
                            onDelete = { recipeViewModel.confirmDelete(recipe) }
                        )
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun RecipeRow(
    recipe: RecipeDto,
    isExpanded: Boolean,
    onToggleExpand: () -> Unit,
    onEdit: () -> Unit = {},
    onDelete: () -> Unit = {}
) {
    val isArabic = LocalIsArabic.current
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 4.dp),
        onClick = onToggleExpand
    ) {
        Column(modifier = Modifier.padding(12.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Text(
                            recipe.name,
                            style = MaterialTheme.typography.titleSmall,
                            fontWeight = FontWeight.Medium
                        )
                        if (!recipe.isActive) {
                            Spacer(modifier = Modifier.width(8.dp))
                            Text(
                                if (isArabic) "غير نشط" else "Inactive",
                                style = MaterialTheme.typography.labelSmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                    }
                    Text(
                        "${recipe.recipeLines.size} ${if (isArabic) "مكون (مكونات)" else "ingredient(s)"}",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    recipe.description?.let {
                        Text(
                            it,
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            maxLines = 1,
                            overflow = TextOverflow.Ellipsis
                        )
                    }
                }
                Icon(
                    imageVector = if (isExpanded) Icons.Default.KeyboardArrowUp
                                  else Icons.Default.KeyboardArrowDown,
                    contentDescription = if (isExpanded) (if (isArabic) "طي" else "Collapse") else (if (isArabic) "توسيع" else "Expand")
                )
                IconButton(onClick = onEdit) {
                    Icon(Icons.Default.Edit, contentDescription = if (isArabic) "تعديل الوصفة" else "Edit recipe")
                }
                IconButton(onClick = onDelete) {
                    Icon(
                        Icons.Default.Clear,
                        contentDescription = if (isArabic) "حذف الوصفة" else "Delete recipe",
                        tint = MaterialTheme.colorScheme.error
                    )
                }
            }

            if (isExpanded && recipe.recipeLines.isNotEmpty()) {
                Spacer(modifier = Modifier.height(8.dp))
                Divider()
                Spacer(modifier = Modifier.height(8.dp))
                recipe.recipeLines.forEach { line ->
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(vertical = 2.dp)
                    ) {
                        Text(
                            line.ingredientName,
                            modifier = Modifier.weight(1f),
                            style = MaterialTheme.typography.bodySmall
                        )
                        Text(
                            "${line.quantityPerItem.stripTrailingZeros().toPlainString()} ${line.unit}",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun RecipeFormDialog(
    recipeUiState: RecipeUiState,
    recipeViewModel: RecipeViewModel
) {
    val isArabic = LocalIsArabic.current
    val isEditing = recipeUiState.editingRecipe != null
    var nameError by remember { mutableStateOf<String?>(null) }
    var menuItemError by remember { mutableStateOf<String?>(null) }
    var lineErrors by remember { mutableStateOf<Map<String, String>>(emptyMap()) }
    var menuItemExpanded by remember { mutableStateOf(false) }

    // Resolve menu item name from current list
    val selectedMenuItemName = recipeUiState.menuItems
        .find { it.id == recipeUiState.dialogMenuItemId }?.name
        ?: recipeUiState.dialogMenuItemName.ifBlank { if (isArabic) "اختر عنصر القائمة" else "Select menu item" }

    Dialog(
        onDismissRequest = { if (!recipeUiState.isSaving) recipeViewModel.dismissRecipeDialog() },
        properties = DialogProperties(usePlatformDefaultWidth = false)
    ) {
        Card(
            modifier = Modifier
                .fillMaxWidth(0.95f)
                .fillMaxHeight(0.9f)
        ) {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(16.dp)
            ) {
                Text(
                    text = if (isEditing) (if (isArabic) "تعديل الوصفة" else "Edit Recipe") else (if (isArabic) "وصفة جديدة" else "New Recipe"),
                    style = MaterialTheme.typography.titleLarge
                )
                Spacer(modifier = Modifier.height(16.dp))

                Column(
                    modifier = Modifier
                        .weight(1f)
                        .verticalScroll(rememberScrollState()),
                    verticalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    // Name
                    OutlinedTextField(
                        value = recipeUiState.dialogName,
                        onValueChange = { recipeViewModel.updateDialogName(it); nameError = null },
                        label = { Text(if (isArabic) "اسم الوصفة" else "Recipe name") },
                        isError = nameError != null,
                        supportingText = nameError?.let { err -> { Text(err) } },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth()
                    )

                    // Description
                    OutlinedTextField(
                        value = recipeUiState.dialogDescription,
                        onValueChange = { recipeViewModel.updateDialogDescription(it) },
                        label = { Text(if (isArabic) "الوصف (اختياري)" else "Description (optional)") },
                        maxLines = 2,
                        modifier = Modifier.fillMaxWidth()
                    )

                    // Menu item picker
                    ExposedDropdownMenuBox(
                        expanded = menuItemExpanded,
                        onExpandedChange = { menuItemExpanded = it }
                    ) {
                        OutlinedTextField(
                            value = selectedMenuItemName,
                            onValueChange = {},
                            readOnly = true,
                            label = { Text(if (isArabic) "عنصر القائمة" else "Menu item") },
                            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = menuItemExpanded) },
                            isError = menuItemError != null,
                            supportingText = menuItemError?.let { err -> { Text(err) } },
                            modifier = Modifier.fillMaxWidth().menuAnchor()
                        )
                        ExposedDropdownMenu(
                            expanded = menuItemExpanded,
                            onDismissRequest = { menuItemExpanded = false }
                        ) {
                            if (recipeUiState.menuItems.isEmpty()) {
                                DropdownMenuItem(
                                    text = { Text(if (isArabic) "لا توجد عناصر — زامن أولاً" else "No menu items in Room — sync first") },
                                    onClick = { menuItemExpanded = false }
                                )
                            } else {
                                recipeUiState.menuItems.forEach { item ->
                                    DropdownMenuItem(
                                        text = { Text(item.name) },
                                        onClick = {
                                            recipeViewModel.selectMenuItem(item)
                                            menuItemError = null
                                            menuItemExpanded = false
                                        }
                                    )
                                }
                            }
                        }
                    }

                    // Lines header
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text(if (isArabic) "المكونات" else "Ingredients", style = MaterialTheme.typography.titleSmall)
                        TextButton(onClick = { recipeViewModel.addLine() }) {
                            Icon(Icons.Default.Add, null, Modifier.size(16.dp))
                            Spacer(Modifier.width(4.dp))
                            Text(if (isArabic) "إضافة" else "Add")
                        }
                    }

                    // Lines
                    recipeUiState.dialogLines.forEachIndexed { index, line ->
                        var ingredientExpanded by remember { mutableStateOf(false) }
                        val lineError = lineErrors[line.id]

                        Card(modifier = Modifier.fillMaxWidth()) {
                            Column(modifier = Modifier.padding(8.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                                Row(verticalAlignment = Alignment.CenterVertically) {
                                    Text(
                                        "${if (isArabic) "سطر" else "Line"} ${index + 1}",
                                        style = MaterialTheme.typography.labelMedium,
                                        modifier = Modifier.weight(1f)
                                    )
                                    if (recipeUiState.dialogLines.size > 1) {
                                        IconButton(
                                            onClick = { recipeViewModel.removeLine(line.id) },
                                            modifier = Modifier.size(24.dp)
                                        ) {
                                            Icon(Icons.Default.Clear, if (isArabic) "إزالة السطر" else "Remove line",
                                                tint = MaterialTheme.colorScheme.error,
                                                modifier = Modifier.size(16.dp))
                                        }
                                    }
                                }

                                // Ingredient picker
                                ExposedDropdownMenuBox(
                                    expanded = ingredientExpanded,
                                    onExpandedChange = { ingredientExpanded = it }
                                ) {
                                    OutlinedTextField(
                                        value = line.ingredientName.ifBlank { if (isArabic) "اختر مكوناً" else "Select ingredient" },
                                        onValueChange = {},
                                        readOnly = true,
                                        label = { Text(if (isArabic) "المكون" else "Ingredient") },
                                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = ingredientExpanded) },
                                        isError = lineError != null && line.ingredientId.isBlank(),
                                        singleLine = true,
                                        modifier = Modifier.fillMaxWidth().menuAnchor()
                                    )
                                    ExposedDropdownMenu(
                                        expanded = ingredientExpanded,
                                        onDismissRequest = { ingredientExpanded = false }
                                    ) {
                                        if (recipeUiState.ingredients.isEmpty()) {
                                            DropdownMenuItem(
                                                text = { Text(if (isArabic) "لا توجد مكونات محملة" else "No ingredients loaded") },
                                                onClick = { ingredientExpanded = false }
                                            )
                                        } else {
                                            recipeUiState.ingredients.forEach { ingredient ->
                                                DropdownMenuItem(
                                                    text = { Text("${ingredient.name} (${ingredient.unit})") },
                                                    onClick = {
                                                        recipeViewModel.updateLineIngredient(line.id, ingredient)
                                                        lineErrors = lineErrors - line.id
                                                        ingredientExpanded = false
                                                    }
                                                )
                                            }
                                        }
                                    }
                                }

                                // Quantity
                                OutlinedTextField(
                                    value = line.quantity,
                                    onValueChange = {
                                        recipeViewModel.updateLineQuantity(line.id, it)
                                        lineErrors = lineErrors - line.id
                                    },
                                    label = { Text(if (isArabic) "الكمية" else "Quantity") },
                                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                                    isError = lineError != null && line.ingredientId.isNotBlank(),
                                    supportingText = if (lineError != null && line.ingredientId.isNotBlank()) {
                                        { Text(lineError) }
                                    } else null,
                                    singleLine = true,
                                    modifier = Modifier.fillMaxWidth()
                                )
                            }
                        }
                    }

                    // Save error
                    if (recipeUiState.saveError != null) {
                        Text(
                            recipeUiState.saveError!!,
                            color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.bodySmall
                        )
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))

                // Buttons
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.End,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    TextButton(
                        onClick = { recipeViewModel.dismissRecipeDialog() },
                        enabled = !recipeUiState.isSaving
                    ) { Text(if (isArabic) "إلغاء" else "Cancel") }
                    Spacer(Modifier.width(8.dp))
                    Button(
                        onClick = {
                            // Client-side validation
                            var valid = true
                            nameError = null
                            menuItemError = null
                            lineErrors = emptyMap()

                            if (recipeUiState.dialogName.isBlank()) {
                                nameError = if (isArabic) "الاسم مطلوب" else "Name is required"; valid = false
                            } else if (recipeUiState.dialogName.length > 100) {
                                nameError = if (isArabic) "100 حرف كحد أقصى" else "Max 100 characters"; valid = false
                            }
                            if (recipeUiState.dialogMenuItemId.isBlank()) {
                                menuItemError = if (isArabic) "اختر عنصر قائمة" else "Select a menu item"; valid = false
                            }
                            val newLineErrors = mutableMapOf<String, String>()
                            recipeUiState.dialogLines.forEach { line ->
                                when {
                                    line.ingredientId.isBlank() ->
                                        newLineErrors[line.id] = if (isArabic) "اختر مكوناً" else "Select an ingredient"
                                    line.quantity.trim().toBigDecimalOrNull()?.let { it <= BigDecimal.ZERO } != false ->
                                        newLineErrors[line.id] = if (isArabic) "أدخل كمية > 0" else "Enter a quantity > 0"
                                }
                            }
                            if (newLineErrors.isNotEmpty()) {
                                lineErrors = newLineErrors; valid = false
                            }
                            if (valid) recipeViewModel.saveRecipe()
                        },
                        enabled = !recipeUiState.isSaving
                    ) {
                        if (recipeUiState.isSaving) CircularProgressIndicator(Modifier.size(16.dp))
                        else Text(if (isArabic) "حفظ" else "Save")
                    }
                }
            }
        }
    }
}

@Composable
private fun InventoryActionDialog(
    item: InventoryItemDto,
    actionType: InventoryAction,
    isSubmitting: Boolean,
    submitError: String?,
    onDismiss: () -> Unit,
    onSubmit: (BigDecimal, String) -> Unit
) {
    val isArabic = LocalIsArabic.current
    var quantityInput by rememberSaveable { mutableStateOf("") }
    var reasonInput by rememberSaveable { mutableStateOf("") }
    var quantityError by remember { mutableStateOf<String?>(null) }

    LaunchedEffect(item.id, actionType) {
        quantityInput = ""
        reasonInput = ""
        quantityError = null
    }

    AlertDialog(
        onDismissRequest = { if (!isSubmitting) onDismiss() },
        title = {
            Text(
                if (actionType == InventoryAction.Wastage)
                    (if (isArabic) "تسجيل هدر" else "Record Wastage")
                else
                    (if (isArabic) "تعديل المخزون" else "Adjust Stock")
            )
        },
        text = {
            Column {
                Text(item.ingredientName, style = MaterialTheme.typography.bodyMedium)
                Text(
                    "${if (isArabic) "الحالي" else "Current"}: ${item.currentStock.stripTrailingZeros().toPlainString()} ${item.ingredientUnit}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Spacer(modifier = Modifier.height(8.dp))
                OutlinedTextField(
                    value = quantityInput,
                    onValueChange = {
                        quantityInput = it
                        quantityError = null
                    },
                    label = {
                        Text(
                            if (actionType == InventoryAction.Wastage) {
                                if (isArabic) "الكمية المهدرة (${item.ingredientUnit})" else "Quantity wasted (${item.ingredientUnit})"
                            } else {
                                if (isArabic) "تغيير الكمية (${item.ingredientUnit})" else "Quantity change (${item.ingredientUnit})"
                            }
                        )
                    },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                    isError = quantityError != null,
                    supportingText = quantityError?.let { err -> { Text(err) } },
                    singleLine = true
                )
                Spacer(modifier = Modifier.height(8.dp))
                OutlinedTextField(
                    value = reasonInput,
                    onValueChange = { reasonInput = it },
                    label = { Text(if (isArabic) "السبب" else "Reason") },
                    maxLines = 2
                )
                if (submitError != null) {
                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        submitError,
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodySmall
                    )
                }
            }
        },
        confirmButton = {
            Button(
                onClick = {
                    val qty = quantityInput.trim().toBigDecimalOrNull()
                    when {
                        reasonInput.isBlank() -> quantityError = if (isArabic) "السبب مطلوب" else "Reason is required"
                        qty == null -> quantityError = if (isArabic) "أدخل كمية صحيحة" else "Enter a valid quantity"
                        actionType == InventoryAction.Wastage && qty <= BigDecimal.ZERO ->
                            quantityError = if (isArabic) "أدخل كمية موجبة" else "Enter a positive quantity"
                        actionType == InventoryAction.Adjust && qty == BigDecimal.ZERO ->
                            quantityError = if (isArabic) "أدخل كمية غير صفرية" else "Enter a non-zero quantity"
                        else -> onSubmit(qty, reasonInput.trim())
                    }
                },
                enabled = !isSubmitting
            ) {
                if (isSubmitting) {
                    CircularProgressIndicator(modifier = Modifier.size(16.dp))
                } else {
                    Text(if (actionType == InventoryAction.Wastage) (if (isArabic) "تسجيل" else "Record") else (if (isArabic) "تعديل" else "Adjust"))
                }
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss, enabled = !isSubmitting) {
                Text(if (isArabic) "إلغاء" else "Cancel")
            }
        }
    )
}
