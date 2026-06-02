package com.tannous.pos.feature.inventory

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.local.dao.MenuItemDao
import com.tannous.pos.core.data.local.entity.MenuItemEntity
import com.tannous.pos.core.data.model.CreateRecipeLineRequest
import com.tannous.pos.core.data.model.CreateRecipeRequest
import com.tannous.pos.core.data.model.IngredientDto
import com.tannous.pos.core.data.model.RecipeDto
import com.tannous.pos.core.data.model.UpdateRecipeLineRequest
import com.tannous.pos.core.data.model.UpdateRecipeRequest
import com.tannous.pos.core.data.repository.InventoryRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.math.BigDecimal
import javax.inject.Inject

/** A single line in the recipe form — mutable draft before saving. */
data class RecipeLineDraft(
    val id: String = java.util.UUID.randomUUID().toString(), // local draft id
    val ingredientId: String = "",
    val ingredientName: String = "",
    val quantity: String = ""  // string for text field; validated on save
)

data class RecipeUiState(
    // List
    val recipes: List<RecipeDto> = emptyList(),
    val isLoading: Boolean = false,
    val error: String? = null,
    val expandedRecipeId: String? = null,

    // Pickers (loaded once for the form)
    val menuItems: List<MenuItemEntity> = emptyList(),
    val ingredients: List<IngredientDto> = emptyList(),

    // Create / edit dialog
    val showRecipeDialog: Boolean = false,
    val editingRecipe: RecipeDto? = null,       // null = creating
    val dialogName: String = "",
    val dialogDescription: String = "",
    val dialogMenuItemId: String = "",
    val dialogMenuItemName: String = "",
    val dialogLines: List<RecipeLineDraft> = listOf(RecipeLineDraft()),
    val isSaving: Boolean = false,
    val saveError: String? = null,

    // Delete
    val deletingRecipe: RecipeDto? = null,
    val showDeleteConfirm: Boolean = false,
    val showForceDeleteConfirm: Boolean = false,
    val isDeleting: Boolean = false,
    val deleteError: String? = null
)

@HiltViewModel
class RecipeViewModel @Inject constructor(
    private val inventoryRepository: InventoryRepository,
    private val menuItemDao: MenuItemDao
) : ViewModel() {

    private val _uiState = MutableStateFlow(RecipeUiState())
    val uiState: StateFlow<RecipeUiState> = _uiState.asStateFlow()

    fun load() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            inventoryRepository.getRecipes().fold(
                onSuccess = { list ->
                    _uiState.update { it.copy(isLoading = false, recipes = list) }
                },
                onFailure = { e ->
                    _uiState.update { it.copy(isLoading = false, error = e.message) }
                }
            )
        }
    }

    fun toggleExpand(recipeId: String) {
        _uiState.update { state ->
            val newId = if (state.expandedRecipeId == recipeId) null else recipeId
            state.copy(expandedRecipeId = newId)
        }
    }

    // ── Dialog helpers ────────────────────────────────────────────────────────

    private fun loadFormData() {
        viewModelScope.launch {
            // Menu items from Room (already synced)
            val items = menuItemDao.getAllActive().first()
            _uiState.update { it.copy(menuItems = items) }
        }
        viewModelScope.launch {
            // Ingredients from network (reuse existing repo method)
            inventoryRepository.getIngredients().fold(
                onSuccess = { list -> _uiState.update { it.copy(ingredients = list) } },
                onFailure = { /* non-blocking; pickers just show empty */ }
            )
        }
    }

    fun openCreateRecipe() {
        loadFormData()
        _uiState.update {
            it.copy(
                showRecipeDialog = true,
                editingRecipe = null,
                dialogName = "",
                dialogDescription = "",
                dialogMenuItemId = "",
                dialogMenuItemName = "",
                dialogLines = listOf(RecipeLineDraft()),
                saveError = null
            )
        }
    }

    fun openEditRecipe(recipe: RecipeDto) {
        loadFormData()
        val lines = if (recipe.recipeLines.isNotEmpty()) {
            recipe.recipeLines.map { line ->
                RecipeLineDraft(
                    ingredientId = line.ingredientId,
                    ingredientName = line.ingredientName,
                    quantity = line.quantityPerItem.stripTrailingZeros().toPlainString()
                )
            }
        } else {
            listOf(RecipeLineDraft())
        }
        _uiState.update {
            it.copy(
                showRecipeDialog = true,
                editingRecipe = recipe,
                dialogName = recipe.name,
                dialogDescription = recipe.description.orEmpty(),
                dialogMenuItemId = recipe.menuItemId,
                dialogMenuItemName = "", // resolved in UI from menuItems list
                dialogLines = lines,
                saveError = null
            )
        }
    }

    fun dismissRecipeDialog() {
        _uiState.update {
            it.copy(showRecipeDialog = false, editingRecipe = null, saveError = null)
        }
    }

    fun updateDialogName(value: String) = _uiState.update { it.copy(dialogName = value) }
    fun updateDialogDescription(value: String) = _uiState.update { it.copy(dialogDescription = value) }
    fun selectMenuItem(item: MenuItemEntity) = _uiState.update {
        it.copy(dialogMenuItemId = item.id, dialogMenuItemName = item.name)
    }

    fun addLine() {
        _uiState.update { it.copy(dialogLines = it.dialogLines + RecipeLineDraft()) }
    }

    fun removeLine(draftId: String) {
        _uiState.update { state ->
            val updated = state.dialogLines.filter { it.id != draftId }
            state.copy(dialogLines = updated.ifEmpty { listOf(RecipeLineDraft()) })
        }
    }

    fun updateLineIngredient(draftId: String, ingredient: IngredientDto) {
        _uiState.update { state ->
            state.copy(dialogLines = state.dialogLines.map { line ->
                if (line.id == draftId) line.copy(
                    ingredientId = ingredient.id,
                    ingredientName = ingredient.name
                ) else line
            })
        }
    }

    fun updateLineQuantity(draftId: String, quantity: String) {
        _uiState.update { state ->
            state.copy(dialogLines = state.dialogLines.map { line ->
                if (line.id == draftId) line.copy(quantity = quantity) else line
            })
        }
    }

    fun saveRecipe() {
        val state = _uiState.value
        val name = state.dialogName.trim()
        val menuItemId = state.dialogMenuItemId

        if (name.isBlank() || menuItemId.isBlank() || state.dialogLines.isEmpty()) return

        val lineRequests = state.dialogLines.mapNotNull { draft ->
            val qty = draft.quantity.trim().toBigDecimalOrNull() ?: return@mapNotNull null
            if (draft.ingredientId.isBlank() || qty <= BigDecimal.ZERO) return@mapNotNull null
            draft.ingredientId to qty
        }
        if (lineRequests.size != state.dialogLines.size) return // validation failed; UI handles

        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true, saveError = null) }
            val description = state.dialogDescription.trim().takeIf { it.isNotBlank() }
            val editing = state.editingRecipe

            val result = if (editing == null) {
                inventoryRepository.createRecipe(
                    CreateRecipeRequest(
                        name = name,
                        description = description,
                        menuItemId = menuItemId,
                        lines = lineRequests.map { (id, qty) ->
                            CreateRecipeLineRequest(ingredientId = id, quantityPerItem = qty)
                        }
                    )
                )
            } else {
                inventoryRepository.updateRecipe(
                    editing.id,
                    UpdateRecipeRequest(
                        name = name,
                        description = description,
                        menuItemId = menuItemId,
                        lines = lineRequests.map { (id, qty) ->
                            UpdateRecipeLineRequest(ingredientId = id, quantityPerItem = qty)
                        }
                    )
                )
            }

            result.fold(
                onSuccess = {
                    load()
                    _uiState.update { it.copy(isSaving = false, showRecipeDialog = false, editingRecipe = null) }
                },
                onFailure = { e ->
                    _uiState.update { it.copy(isSaving = false, saveError = e.message) }
                }
            )
        }
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    fun confirmDelete(recipe: RecipeDto) {
        _uiState.update {
            it.copy(deletingRecipe = recipe, showDeleteConfirm = true, deleteError = null)
        }
    }

    fun dismissDelete() {
        _uiState.update {
            it.copy(
                deletingRecipe = null,
                showDeleteConfirm = false,
                showForceDeleteConfirm = false,
                deleteError = null
            )
        }
    }

    fun deleteRecipe(force: Boolean = false) {
        val recipe = _uiState.value.deletingRecipe ?: return
        viewModelScope.launch {
            _uiState.update { it.copy(isDeleting = true, deleteError = null) }
            inventoryRepository.deleteRecipe(recipe.id, force).fold(
                onSuccess = {
                    load()
                    _uiState.update {
                        it.copy(
                            isDeleting = false,
                            deletingRecipe = null,
                            showDeleteConfirm = false,
                            showForceDeleteConfirm = false
                        )
                    }
                },
                onFailure = { e ->
                    val msg = e.message.orEmpty()
                    if (msg.startsWith("MENU_ITEM_CONFLICT:")) {
                        _uiState.update {
                            it.copy(
                                isDeleting = false,
                                showDeleteConfirm = false,
                                showForceDeleteConfirm = true
                            )
                        }
                    } else {
                        _uiState.update { it.copy(isDeleting = false, deleteError = msg) }
                    }
                }
            )
        }
    }
}
