package com.tannous.pos.feature.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.CreateFloorPlanRequest
import com.tannous.pos.core.data.model.CreateTableRequest
import com.tannous.pos.core.data.model.FloorPlanDto
import com.tannous.pos.core.data.remote.TableService
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import timber.log.Timber
import javax.inject.Inject

@HiltViewModel
class TableManagementViewModel @Inject constructor(
    private val tableService: TableService
) : ViewModel() {

    private val _uiState = MutableStateFlow(TableManagementUiState())
    val uiState: StateFlow<TableManagementUiState> = _uiState.asStateFlow()

    init { load() }

    fun load() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val plans = tableService.getFloorPlans()
                _uiState.update { it.copy(floorPlans = plans, isLoading = false) }
            } catch (e: Exception) {
                Timber.e(e, "Failed to load floor plans")
                _uiState.update { it.copy(error = e.message ?: "Failed to load", isLoading = false) }
            }
        }
    }

    fun createFloorPlan(name: String, description: String?) {
        if (name.isBlank()) return
        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true, error = null) }
            try {
                tableService.createFloorPlan(
                    CreateFloorPlanRequest(
                        name        = name.trim(),
                        description = description?.trim()?.ifBlank { null }
                    )
                )
                _uiState.update { it.copy(isSaving = false) }
                load()
            } catch (e: Exception) {
                Timber.e(e, "Failed to create floor plan")
                _uiState.update { it.copy(error = e.message ?: "Failed to create floor plan", isSaving = false) }
            }
        }
    }

    fun createTable(floorPlanId: String, tableNumber: String, capacity: Int, label: String?) {
        if (tableNumber.isBlank()) return
        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true, error = null) }
            try {
                tableService.createTable(
                    CreateTableRequest(
                        tableNumber = tableNumber.trim(),
                        label       = label?.trim()?.ifBlank { null },
                        capacity    = capacity,
                        floorPlanId = floorPlanId
                    )
                )
                _uiState.update { it.copy(isSaving = false) }
                load()
            } catch (e: Exception) {
                Timber.e(e, "Failed to create table")
                _uiState.update { it.copy(error = e.message ?: "Failed to create table", isSaving = false) }
            }
        }
    }

    fun deleteTable(tableId: String) {
        viewModelScope.launch {
            try {
                tableService.deleteTable(tableId)
                load()
            } catch (e: Exception) {
                Timber.e(e, "Failed to delete table")
                _uiState.update { it.copy(error = e.message ?: "Failed to delete table") }
            }
        }
    }

    fun clearError() = _uiState.update { it.copy(error = null) }
}

data class TableManagementUiState(
    val isLoading: Boolean  = false,
    val isSaving: Boolean   = false,
    val floorPlans: List<FloorPlanDto> = emptyList(),
    val error: String?      = null
)
