package com.tannous.pos.feature.reports

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.SalesSummaryDto
import com.tannous.pos.core.data.remote.ReportsService
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

@HiltViewModel
class DashboardViewModel @Inject constructor(
    private val reportsService: ReportsService
) : ViewModel() {

    private val _uiState = MutableStateFlow(DashboardUiState())
    val uiState: StateFlow<DashboardUiState> = _uiState.asStateFlow()

    private var pollJob: Job? = null

    init { startPolling() }

    private fun startPolling() {
        pollJob?.cancel()
        pollJob = viewModelScope.launch {
            while (isActive) {
                load()
                delay(REFRESH_INTERVAL_MS)
            }
        }
    }

    fun refresh() { viewModelScope.launch { load() } }

    private suspend fun load() {
        _uiState.update { it.copy(isLoading = it.summary == null) }
        try {
            val summary = reportsService.getSalesSummary()
            _uiState.update { it.copy(summary = summary, error = null, isLoading = false) }
        } catch (e: Exception) {
            Timber.w(e, "Dashboard load failed")
            _uiState.update {
                it.copy(
                    error = "Could not refresh — showing last data",
                    isLoading = false
                )
            }
        }
    }

    fun clearError() = _uiState.update { it.copy(error = null) }

    override fun onCleared() { super.onCleared(); pollJob?.cancel() }

    companion object {
        const val REFRESH_INTERVAL_MS = 30_000L  // 30 seconds for dashboard
    }
}

data class DashboardUiState(
    val isLoading: Boolean = false,
    val summary: SalesSummaryDto? = null,
    val error: String? = null
)
