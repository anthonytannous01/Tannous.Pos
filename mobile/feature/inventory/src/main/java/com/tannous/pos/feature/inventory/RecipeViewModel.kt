package com.tannous.pos.feature.inventory

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.RecipeDto
import com.tannous.pos.core.data.repository.InventoryRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class RecipeUiState(
    val recipes: List<RecipeDto> = emptyList(),
    val isLoading: Boolean = false,
    val error: String? = null,
    val expandedRecipeId: String? = null
)

@HiltViewModel
class RecipeViewModel @Inject constructor(
    private val inventoryRepository: InventoryRepository
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
}
