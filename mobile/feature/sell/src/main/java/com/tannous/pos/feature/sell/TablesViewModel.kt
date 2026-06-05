package com.tannous.pos.feature.sell

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.FloorPlanDto
import com.tannous.pos.core.data.model.TableDto
import com.tannous.pos.core.data.model.UpdateTableStatusRequest
import com.tannous.pos.core.data.remote.TableService
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import timber.log.Timber
import javax.inject.Inject

// Status constants matching backend TableStatus enum
const val TABLE_AVAILABLE = 0
const val TABLE_OCCUPIED  = 1
const val TABLE_RESERVED  = 2
const val TABLE_CLEANING  = 3

@HiltViewModel
class TablesViewModel @Inject constructor(
    private val tableService: TableService
) : ViewModel() {

    private val _uiState = MutableStateFlow(TablesUiState())
    val uiState: StateFlow<TablesUiState> = _uiState.asStateFlow()

    private var pollJob: Job? = null

    init { startPolling() }

    private fun startPolling() {
        pollJob?.cancel()
        pollJob = viewModelScope.launch {
            while (isActive) {
                load()
                delay(10_000L) // refresh every 10s
            }
        }
    }

    fun refresh() { viewModelScope.launch { load() } }

    private suspend fun load() {
        try {
            val plans = tableService.getFloorPlans()
            val selectedPlan = _uiState.value.selectedFloorPlanId
                ?: plans.firstOrNull()?.id
            _uiState.update { it.copy(floorPlans = plans, selectedFloorPlanId = selectedPlan, error = null) }
        } catch (e: Exception) {
            Timber.w(e, "Tables load failed")
            _uiState.update { it.copy(error = "Could not load tables") }
        }
    }

    fun selectFloorPlan(id: String) = _uiState.update { it.copy(selectedFloorPlanId = id) }

    fun updateStatus(tableId: String, status: Int) {
        viewModelScope.launch {
            // Optimistic
            _uiState.update { state ->
                state.copy(floorPlans = state.floorPlans.map { fp ->
                    fp.copy(tables = fp.tables.map { t ->
                        if (t.id == tableId) t.copy(status = status) else t
                    })
                })
            }
            try {
                tableService.updateStatus(tableId, UpdateTableStatusRequest(status))
            } catch (e: Exception) {
                Timber.e(e, "Table status update failed")
                load() // roll back
                _uiState.update { it.copy(error = "Update failed") }
            }
        }
    }

    fun clearError() = _uiState.update { it.copy(error = null) }

    override fun onCleared() { super.onCleared(); pollJob?.cancel() }
}

data class TablesUiState(
    val floorPlans: List<FloorPlanDto> = emptyList(),
    val selectedFloorPlanId: String? = null,
    val error: String? = null
) {
    val selectedFloorPlan: FloorPlanDto?
        get() = floorPlans.firstOrNull { it.id == selectedFloorPlanId }
}
