package com.tannous.pos.feature.inventory

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.text.KeyboardOptions
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
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.IngredientDto
import com.tannous.pos.core.data.model.InventoryItemDto
import com.tannous.pos.core.data.model.RecipeDto
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
                title = { Text("Inventory") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = "Back")
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
                        Icon(Icons.Default.Refresh, contentDescription = "Refresh")
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
                    text = { Text("Stock") }
                )
                Tab(
                    selected = uiState.selectedTab == 1,
                    onClick = { viewModel.selectTab(1) },
                    text = { Text("Ingredients") }
                )
                Tab(
                    selected = uiState.selectedTab == 2,
                    onClick = { viewModel.selectTab(2) },
                    text = { Text("Recipes") }
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
        AlertDialog(
            onDismissRequest = {
                if (!uiState.isDeletingIngredient) {
                    viewModel.dismissDelete()
                }
            },
            title = { Text("Delete Ingredient") },
            text = {
                Column {
                    Text("Delete \"${uiState.deletingIngredient?.name}\"?")
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
                        Text("Delete")
                    }
                }
            },
            dismissButton = {
                TextButton(
                    onClick = { viewModel.dismissDelete() },
                    enabled = !uiState.isDeletingIngredient
                ) {
                    Text("Cancel")
                }
            }
        )
    }

    if (uiState.showForceDeleteConfirm) {
        AlertDialog(
            onDismissRequest = {
                if (!uiState.isDeletingIngredient) {
                    viewModel.dismissDelete()
                }
            },
            title = { Text("Ingredient In Use") },
            text = {
                Text(
                    "\"${uiState.deletingIngredient?.name}\" is used in active recipes. " +
                        "Force deleting will deactivate those recipes. This cannot be undone."
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
                        Text("Force Delete")
                    }
                }
            },
            dismissButton = {
                TextButton(
                    onClick = { viewModel.dismissDelete() },
                    enabled = !uiState.isDeletingIngredient
                ) {
                    Text("Cancel")
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
    Row(
        modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)
    ) {
        FilterChip(
            selected = uiState.filter == InventoryFilter.All,
            onClick = { viewModel.setFilter(InventoryFilter.All) },
            label = { Text("All") }
        )
        Spacer(modifier = Modifier.width(8.dp))
        FilterChip(
            selected = uiState.filter == InventoryFilter.LowStock,
            onClick = { viewModel.setFilter(InventoryFilter.LowStock) },
            label = { Text("Low Stock") }
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
                    Text("Retry")
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
                        "No low stock items"
                    } else {
                        "No inventory items"
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
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 8.dp),
        horizontalArrangement = Arrangement.End
    ) {
        Button(onClick = { viewModel.openCreateIngredient() }) {
            Icon(Icons.Default.Add, contentDescription = null, modifier = Modifier.size(18.dp))
            Spacer(modifier = Modifier.width(4.dp))
            Text("Add Ingredient")
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
                    Text("Retry")
                }
            }
        }
        uiState.ingredients.isEmpty() -> {
            Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                Text("No ingredients. Tap Add Ingredient to create one.")
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
                            "Inactive",
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
                Icon(Icons.Default.Edit, contentDescription = "Edit")
            }
            IconButton(onClick = onDelete) {
                Icon(
                    Icons.Default.Clear,
                    contentDescription = "Delete",
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
        title = { Text(if (ingredient == null) "New Ingredient" else "Edit Ingredient") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(
                    value = nameInput,
                    onValueChange = {
                        nameInput = it
                        nameError = null
                    },
                    label = { Text("Name") },
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
                    label = { Text("Unit") },
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
                    label = { Text("Cost per unit") },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                    isError = costError != null,
                    supportingText = costError?.let { err -> { Text(err) } },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = descriptionInput,
                    onValueChange = { descriptionInput = it },
                    label = { Text("Description (optional)") },
                    maxLines = 3,
                    modifier = Modifier.fillMaxWidth()
                )
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text("Active")
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
                        trimmedName.isBlank() -> nameError = "Name is required"
                        trimmedName.length > 100 -> nameError = "Max 100 characters"
                        unitInput.isBlank() -> unitError = "Unit is required"
                        unitInput.length > 20 -> unitError = "Max 20 characters"
                        else -> {
                            val cost = costInput.trim().toBigDecimalOrNull()
                            when {
                                cost == null -> costError = "Enter a valid cost"
                                cost < BigDecimal.ZERO -> costError = "Cost must be ≥ 0"
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
                    Text("Save")
                }
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss, enabled = !isSaving) {
                Text("Cancel")
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
                            "Low stock",
                            color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.labelSmall
                        )
                    }
                }
                Column(horizontalAlignment = Alignment.End) {
                    Text(
                        "Min: ${item.minimumStock.stripTrailingZeros().toPlainString()}",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Text(
                        "Cost: ${currencyFormatter.format(item.averageCost)}/${item.ingredientUnit}",
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
                    Text("Wastage", color = MaterialTheme.colorScheme.error)
                }
                Spacer(modifier = Modifier.width(8.dp))
                TextButton(onClick = onAdjust) {
                    Text("Adjust")
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
                Button(onClick = { recipeViewModel.load() }) { Text("Retry") }
            }
        }
        recipeUiState.recipes.isEmpty() -> {
            Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                Text("No recipes found.", color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
        else -> {
            LazyColumn(modifier = Modifier.fillMaxSize()) {
                items(recipeUiState.recipes, key = { it.id }) { recipe ->
                    RecipeRow(
                        recipe = recipe,
                        isExpanded = recipeUiState.expandedRecipeId == recipe.id,
                        onToggleExpand = { recipeViewModel.toggleExpand(recipe.id) }
                    )
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
    onToggleExpand: () -> Unit
) {
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
                                "Inactive",
                                style = MaterialTheme.typography.labelSmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                    }
                    Text(
                        "${recipe.recipeLines.size} ingredient(s)",
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
                    contentDescription = if (isExpanded) "Collapse" else "Expand"
                )
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

@Composable
private fun InventoryActionDialog(
    item: InventoryItemDto,
    actionType: InventoryAction,
    isSubmitting: Boolean,
    submitError: String?,
    onDismiss: () -> Unit,
    onSubmit: (BigDecimal, String) -> Unit
) {
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
                if (actionType == InventoryAction.Wastage) "Record Wastage" else "Adjust Stock"
            )
        },
        text = {
            Column {
                Text(item.ingredientName, style = MaterialTheme.typography.bodyMedium)
                Text(
                    "Current: ${item.currentStock.stripTrailingZeros().toPlainString()} ${item.ingredientUnit}",
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
                                "Quantity wasted (${item.ingredientUnit})"
                            } else {
                                "Quantity change (${item.ingredientUnit})"
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
                    label = { Text("Reason") },
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
                        reasonInput.isBlank() -> quantityError = "Reason is required"
                        qty == null -> quantityError = "Enter a valid quantity"
                        actionType == InventoryAction.Wastage && qty <= BigDecimal.ZERO ->
                            quantityError = "Enter a positive quantity"
                        actionType == InventoryAction.Adjust && qty == BigDecimal.ZERO ->
                            quantityError = "Enter a non-zero quantity"
                        else -> onSubmit(qty, reasonInput.trim())
                    }
                },
                enabled = !isSubmitting
            ) {
                if (isSubmitting) {
                    CircularProgressIndicator(modifier = Modifier.size(16.dp))
                } else {
                    Text(if (actionType == InventoryAction.Wastage) "Record" else "Adjust")
                }
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss, enabled = !isSubmitting) {
                Text("Cancel")
            }
        }
    )
}
