package com.tannous.pos.feature.shifts

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.ShiftDto
import com.tannous.pos.core.data.repository.ShiftRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import timber.log.Timber
import java.math.BigDecimal
import javax.inject.Inject

@HiltViewModel
class ShiftViewModel @Inject constructor(
    private val shiftRepository: ShiftRepository
) : ViewModel() {
    
    private val _uiState = MutableStateFlow(ShiftUiState())
    val uiState: StateFlow<ShiftUiState> = _uiState.asStateFlow()
    
    init {
        loadActiveShift()
    }
    
    fun loadActiveShift() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            try {
                val shift = shiftRepository.getActiveShift()
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    activeShift = shift,
                    errorMessage = null
                )
            } catch (e: Exception) {
                Timber.e(e, "Error loading active shift")
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    errorMessage = "Failed to load shift: ${e.message}"
                )
            }
        }
    }
    
    fun openShift(openingBalance: BigDecimal, notes: String? = null) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            try {
                val result = shiftRepository.openShift(openingBalance, notes)
                result.fold(
                    onSuccess = { shift ->
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            activeShift = shift,
                            errorMessage = null
                        )
                    },
                    onFailure = { error ->
                        Timber.e(error, "Error opening shift")
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            errorMessage = error.message ?: "Failed to open shift"
                        )
                    }
                )
            } catch (e: Exception) {
                Timber.e(e, "Unexpected error opening shift")
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    errorMessage = "Failed to open shift: ${e.message}"
                )
            }
        }
    }
    
    fun closeShift(shiftId: String, closingCount: BigDecimal, note: String? = null) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            try {
                val result = shiftRepository.closeShift(shiftId, closingCount, note)
                result.fold(
                    onSuccess = { shift ->
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            activeShift = null, // Shift is now closed
                            errorMessage = null
                        )
                    },
                    onFailure = { error ->
                        Timber.e(error, "Error closing shift")
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            errorMessage = error.message ?: "Failed to close shift"
                        )
                    }
                )
            } catch (e: Exception) {
                Timber.e(e, "Unexpected error closing shift")
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    errorMessage = "Failed to close shift: ${e.message}"
                )
            }
        }
    }
    
    fun cashDrop(shiftId: String, amount: BigDecimal, note: String? = null) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            try {
                val result = shiftRepository.cashDrop(shiftId, amount, note)
                result.fold(
                    onSuccess = { shift ->
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            activeShift = shift,
                            errorMessage = null
                        )
                    },
                    onFailure = { error ->
                        Timber.e(error, "Error recording cash drop")
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            errorMessage = error.message ?: "Failed to record cash drop"
                        )
                    }
                )
            } catch (e: Exception) {
                Timber.e(e, "Unexpected error recording cash drop")
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    errorMessage = "Failed to record cash drop: ${e.message}"
                )
            }
        }
    }

    fun clearError() {
        _uiState.value = _uiState.value.copy(errorMessage = null)
    }
}

data class ShiftUiState(
    val isLoading: Boolean = false,
    val activeShift: ShiftDto? = null,
    val errorMessage: String? = null
)

