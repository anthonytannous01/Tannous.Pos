package com.tannous.pos.feature.inventory

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.CreateIngredientRequest
import com.tannous.pos.core.data.model.IngredientDto
import com.tannous.pos.core.data.model.InventoryItemDto
import com.tannous.pos.core.data.model.UpdateIngredientRequest
import com.tannous.pos.core.data.repository.InventoryRepository
import com.tannous.pos.core.data.repository.SettingsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.math.BigDecimal
import javax.inject.Inject

enum class InventoryFilter {
    All,
    LowStock
}

enum class InventoryAction {
    Adjust,
    Wastage
}

@HiltViewModel
class InventoryViewModel @Inject constructor(
    private val inventoryRepository: InventoryRepository,
    private val settingsRepository: SettingsRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(InventoryUiState())
    val uiState: StateFlow<InventoryUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            val currency = settingsRepository.getCurrency()
            _uiState.update { it.copy(currencyCode = currency) }
        }
        load()
    }

    fun selectTab(index: Int) {
        _uiState.update { it.copy(selectedTab = index) }
        if (index == 1 &&
            _uiState.value.ingredients.isEmpty() &&
            !_uiState.value.isIngredientsLoading
        ) {
            loadIngredients()
        }
        // index 2 (Recipes): auto-load triggered from InventoryScreen via
        // LaunchedEffect(selectedTab), keeping RecipeViewModel independent.
    }

    fun load(filter: InventoryFilter = _uiState.value.filter) {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            val result = inventoryRepository.getInventoryItems(
                lowStockOnly = filter == InventoryFilter.LowStock
            )
            result.fold(
                onSuccess = { items ->
                    _uiState.update {
                        it.copy(isLoading = false, items = items, filter = filter)
                    }
                },
                onFailure = { e ->
                    _uiState.update {
                        it.copy(isLoading = false, error = e.message ?: "Failed to load inventory")
                    }
                }
            )
        }
    }

    fun loadIngredients() {
        viewModelScope.launch {
            _uiState.update { it.copy(isIngredientsLoading = true, ingredientsError = null) }
            inventoryRepository.getIngredients().fold(
                onSuccess = { list ->
                    _uiState.update {
                        it.copy(isIngredientsLoading = false, ingredients = list)
                    }
                },
                onFailure = { e ->
                    _uiState.update {
                        it.copy(
                            isIngredientsLoading = false,
                            ingredientsError = e.message
                        )
                    }
                }
            )
        }
    }

    fun setFilter(filter: InventoryFilter) {
        load(filter)
    }

    fun openAction(item: InventoryItemDto, action: InventoryAction) {
        _uiState.update {
            it.copy(
                actionItem = item,
                actionType = action,
                submitError = null,
                submitSuccess = null
            )
        }
    }

    fun dismissAction() {
        _uiState.update {
            it.copy(
                actionItem = null,
                actionType = null,
                submitError = null,
                submitSuccess = null
            )
        }
    }

    fun submitAction(quantity: BigDecimal, reason: String) {
        val item = _uiState.value.actionItem ?: return
        val action = _uiState.value.actionType ?: return
        val filter = _uiState.value.filter

        viewModelScope.launch {
            _uiState.update { it.copy(isSubmitting = true, submitError = null) }
            try {
                when (action) {
                    InventoryAction.Adjust ->
                        inventoryRepository.adjustStock(item.ingredientId, quantity, reason)
                    InventoryAction.Wastage ->
                        inventoryRepository.recordWastage(item.ingredientId, quantity, reason)
                }
                val label = if (action == InventoryAction.Adjust) "Adjustment" else "Wastage"
                _uiState.update {
                    it.copy(
                        isSubmitting = false,
                        actionItem = null,
                        actionType = null,
                        submitSuccess = "$label queued for sync"
                    )
                }
                load(filter)
            } catch (e: Exception) {
                _uiState.update {
                    it.copy(
                        isSubmitting = false,
                        submitError = e.message ?: "Failed to queue operation"
                    )
                }
            }
        }
    }

    fun clearSubmitSuccess() {
        _uiState.update { it.copy(submitSuccess = null) }
    }

    fun openCreateIngredient() {
        _uiState.update {
            it.copy(
                editingIngredient = null,
                showIngredientDialog = true,
                ingredientSaveError = null
            )
        }
    }

    fun openEditIngredient(ingredient: IngredientDto) {
        _uiState.update {
            it.copy(
                editingIngredient = ingredient,
                showIngredientDialog = true,
                ingredientSaveError = null
            )
        }
    }

    fun dismissIngredientDialog() {
        _uiState.update {
            it.copy(
                showIngredientDialog = false,
                editingIngredient = null,
                ingredientSaveError = null
            )
        }
    }

    fun saveIngredient(
        name: String,
        description: String?,
        costPerUnit: BigDecimal,
        unit: String,
        isActive: Boolean
    ) {
        val editing = _uiState.value.editingIngredient
        viewModelScope.launch {
            _uiState.update { it.copy(isSavingIngredient = true, ingredientSaveError = null) }
            val result = if (editing == null) {
                inventoryRepository.createIngredient(
                    CreateIngredientRequest(
                        name = name,
                        description = description?.takeIf { it.isNotBlank() },
                        costPerUnit = costPerUnit,
                        unit = unit,
                        isActive = isActive
                    )
                )
            } else {
                inventoryRepository.updateIngredient(
                    editing.id,
                    UpdateIngredientRequest(
                        name = name,
                        description = description?.takeIf { it.isNotBlank() },
                        costPerUnit = costPerUnit,
                        unit = unit,
                        isActive = isActive
                    )
                )
            }
            result.fold(
                onSuccess = {
                    loadIngredients()
                    _uiState.update {
                        it.copy(
                            isSavingIngredient = false,
                            showIngredientDialog = false,
                            editingIngredient = null
                        )
                    }
                },
                onFailure = { e ->
                    _uiState.update {
                        it.copy(
                            isSavingIngredient = false,
                            ingredientSaveError = e.message
                        )
                    }
                }
            )
        }
    }

    fun confirmDelete(ingredient: IngredientDto) {
        _uiState.update {
            it.copy(
                deletingIngredient = ingredient,
                showDeleteConfirm = true,
                showForceDeleteConfirm = false,
                deleteError = null
            )
        }
    }

    fun dismissDelete() {
        _uiState.update {
            it.copy(
                deletingIngredient = null,
                showDeleteConfirm = false,
                showForceDeleteConfirm = false,
                deleteError = null
            )
        }
    }

    fun deleteIngredient(force: Boolean = false) {
        val ingredient = _uiState.value.deletingIngredient ?: return
        viewModelScope.launch {
            _uiState.update { it.copy(isDeletingIngredient = true, deleteError = null) }
            inventoryRepository.deleteIngredient(ingredient.id, force).fold(
                onSuccess = {
                    loadIngredients()
                    _uiState.update {
                        it.copy(
                            isDeletingIngredient = false,
                            deletingIngredient = null,
                            showDeleteConfirm = false,
                            showForceDeleteConfirm = false
                        )
                    }
                },
                onFailure = { e ->
                    val msg = e.message.orEmpty()
                    if (msg.startsWith("RECIPE_CONFLICT:")) {
                        _uiState.update {
                            it.copy(
                                isDeletingIngredient = false,
                                showDeleteConfirm = false,
                                showForceDeleteConfirm = true,
                                deleteError = null
                            )
                        }
                    } else {
                        _uiState.update {
                            it.copy(
                                isDeletingIngredient = false,
                                deleteError = msg
                            )
                        }
                    }
                }
            )
        }
    }
}

data class InventoryUiState(
    val selectedTab: Int = 0,
    val items: List<InventoryItemDto> = emptyList(),
    val filter: InventoryFilter = InventoryFilter.All,
    val isLoading: Boolean = false,
    val error: String? = null,
    val currencyCode: String = "USD",
    val actionItem: InventoryItemDto? = null,
    val actionType: InventoryAction? = null,
    val isSubmitting: Boolean = false,
    val submitError: String? = null,
    val submitSuccess: String? = null,
    val ingredients: List<IngredientDto> = emptyList(),
    val isIngredientsLoading: Boolean = false,
    val ingredientsError: String? = null,
    val editingIngredient: IngredientDto? = null,
    val showIngredientDialog: Boolean = false,
    val isSavingIngredient: Boolean = false,
    val ingredientSaveError: String? = null,
    val deletingIngredient: IngredientDto? = null,
    val showDeleteConfirm: Boolean = false,
    val showForceDeleteConfirm: Boolean = false,
    val isDeletingIngredient: Boolean = false,
    val deleteError: String? = null
)
