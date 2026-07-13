package com.tannous.pos.feature.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.local.entity.CategoryEntity
import com.tannous.pos.core.data.local.entity.MenuItemEntity
import com.tannous.pos.core.data.repository.CatalogRepository
import com.tannous.pos.core.data.repository.DeleteMenuItemResult
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import timber.log.Timber
import java.math.BigDecimal
import javax.inject.Inject

data class MenuManagementUiState(
    val categories: List<CategoryEntity> = emptyList(),
    val menuItems: List<MenuItemEntity> = emptyList(),
    val selectedCategoryId: String? = null,
    val isLoading: Boolean = false,
    val error: String? = null,
    val successMessage: String? = null,
    /** Set when a delete was refused because the item has order history;
     *  the UI should offer to archive instead. */
    val archivePrompt: ArchivePrompt? = null
)

data class ArchivePrompt(val itemId: String, val itemName: String)

@HiltViewModel
class MenuManagementViewModel @Inject constructor(
    private val catalogRepository: CatalogRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(MenuManagementUiState())
    val uiState: StateFlow<MenuManagementUiState> = _uiState.asStateFlow()

    init {
        observeCategories()
        observeMenuItems()
        refresh()
    }

    private fun observeCategories() {
        viewModelScope.launch {
            catalogRepository.getAllCategories().collect { cats ->
                _uiState.update { it.copy(categories = cats) }
            }
        }
    }

    private fun observeMenuItems() {
        viewModelScope.launch {
            catalogRepository.getAllMenuItems().collect { items ->
                _uiState.update { it.copy(menuItems = items) }
            }
        }
    }

    fun refresh() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                catalogRepository.refreshAllCatalogData()
            } catch (e: Exception) {
                Timber.e(e, "Failed to refresh catalog")
                _uiState.update { it.copy(error = "Failed to load: ${e.message}") }
            } finally {
                _uiState.update { it.copy(isLoading = false) }
            }
        }
    }

    fun selectCategory(categoryId: String?) {
        _uiState.update { it.copy(selectedCategoryId = categoryId) }
    }

    // Category mutations
    fun createCategory(name: String, description: String?) {
        if (name.isBlank()) return
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                catalogRepository.createCategory(name.trim(), description?.takeIf { it.isNotBlank() })
                _uiState.update { it.copy(successMessage = "Category \"$name\" created") }
            } catch (e: Exception) {
                Timber.e(e, "Failed to create category")
                _uiState.update { it.copy(error = "Failed to create category: ${e.message}") }
            } finally {
                _uiState.update { it.copy(isLoading = false) }
            }
        }
    }

    fun updateCategory(id: String, name: String, description: String?) {
        if (name.isBlank()) return
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                catalogRepository.updateCategory(id, name.trim(), description?.takeIf { it.isNotBlank() })
                _uiState.update { it.copy(successMessage = "Category updated") }
            } catch (e: Exception) {
                Timber.e(e, "Failed to update category")
                _uiState.update { it.copy(error = "Failed to update: ${e.message}") }
            } finally {
                _uiState.update { it.copy(isLoading = false) }
            }
        }
    }

    fun deleteCategory(id: String) {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                catalogRepository.deleteCategory(id)
                if (_uiState.value.selectedCategoryId == id) {
                    _uiState.update { it.copy(selectedCategoryId = null) }
                }
                _uiState.update { it.copy(successMessage = "Category deleted") }
            } catch (e: Exception) {
                Timber.e(e, "Failed to delete category")
                _uiState.update { it.copy(error = "Cannot delete: ${e.message}") }
            } finally {
                _uiState.update { it.copy(isLoading = false) }
            }
        }
    }

    // Menu item mutations
    fun createMenuItem(name: String, nameAr: String?, priceText: String, categoryId: String, description: String?) {
        val price = priceText.toBigDecimalOrNull() ?: run {
            _uiState.update { it.copy(error = "Invalid price") }
            return
        }
        if (name.isBlank() || categoryId.isBlank()) return
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                catalogRepository.createMenuItem(
                    name = name.trim(),
                    nameAr = nameAr?.takeIf { it.isNotBlank() },
                    price = price,
                    categoryId = categoryId,
                    description = description?.takeIf { it.isNotBlank() }
                )
                _uiState.update { it.copy(successMessage = "\"$name\" added to menu") }
            } catch (e: Exception) {
                Timber.e(e, "Failed to create menu item")
                _uiState.update { it.copy(error = "Failed to add item: ${e.message}") }
            } finally {
                _uiState.update { it.copy(isLoading = false) }
            }
        }
    }

    fun updateMenuItem(id: String, name: String, nameAr: String?, priceText: String, categoryId: String, description: String?) {
        val price = priceText.toBigDecimalOrNull() ?: run {
            _uiState.update { it.copy(error = "Invalid price") }
            return
        }
        if (name.isBlank()) return
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                catalogRepository.updateMenuItem(
                    id = id,
                    name = name.trim(),
                    nameAr = nameAr?.takeIf { it.isNotBlank() },
                    price = price,
                    categoryId = categoryId,
                    description = description?.takeIf { it.isNotBlank() }
                )
                _uiState.update { it.copy(successMessage = "Item updated") }
            } catch (e: Exception) {
                Timber.e(e, "Failed to update menu item")
                _uiState.update { it.copy(error = "Failed to update: ${e.message}") }
            } finally {
                _uiState.update { it.copy(isLoading = false) }
            }
        }
    }

    fun deleteMenuItem(id: String, name: String) {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                when (catalogRepository.deleteMenuItem(id)) {
                    DeleteMenuItemResult.Deleted ->
                        _uiState.update { it.copy(successMessage = "Item deleted") }
                    DeleteMenuItemResult.HasOrderHistory ->
                        // Hard delete refused — surface the archive option instead of an error.
                        _uiState.update { it.copy(archivePrompt = ArchivePrompt(id, name)) }
                    DeleteMenuItemResult.Archived ->
                        _uiState.update { it.copy(successMessage = "Item archived") }
                }
            } catch (e: Exception) {
                Timber.e(e, "Failed to delete menu item")
                _uiState.update { it.copy(error = "Cannot delete: ${e.message}") }
            } finally {
                _uiState.update { it.copy(isLoading = false) }
            }
        }
    }

    /** Confirms the pending archive prompt: retries the delete with force=true,
     *  which deactivates the item server-side while preserving order history. */
    fun archiveMenuItem() {
        val prompt = _uiState.value.archivePrompt ?: return
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null, archivePrompt = null) }
            try {
                catalogRepository.deleteMenuItem(prompt.itemId, force = true)
                _uiState.update { it.copy(successMessage = "\"${prompt.itemName}\" archived") }
            } catch (e: Exception) {
                Timber.e(e, "Failed to archive menu item")
                _uiState.update { it.copy(error = "Cannot archive: ${e.message}") }
            } finally {
                _uiState.update { it.copy(isLoading = false) }
            }
        }
    }

    fun dismissArchivePrompt() {
        _uiState.update { it.copy(archivePrompt = null) }
    }

    fun clearMessage() {
        _uiState.update { it.copy(error = null, successMessage = null) }
    }
}
