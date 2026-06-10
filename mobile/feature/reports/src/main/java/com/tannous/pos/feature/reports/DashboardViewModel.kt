package com.tannous.pos.feature.reports

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.BranchDto
import com.tannous.pos.core.data.model.DemandForecastDto
import com.tannous.pos.core.data.model.SalesSummaryDto
import com.tannous.pos.core.data.remote.ReportsService
import com.tannous.pos.core.data.repository.BranchRepository
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
    private val reportsService: ReportsService,
    private val branchRepository: BranchRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(DashboardUiState())
    val uiState: StateFlow<DashboardUiState> = _uiState.asStateFlow()

    private var pollJob: Job? = null

    init {
        viewModelScope.launch { loadBranches() }
        startPolling()
    }

    private suspend fun loadBranches() {
        val branches = branchRepository.getBranches()
        val selected = branchRepository.getDefaultBranch()
        _uiState.update { it.copy(branches = branches, selectedBranch = selected) }
    }

    private fun startPolling() {
        pollJob?.cancel()
        pollJob = viewModelScope.launch {
            while (isActive) {
                load()
                loadForecast()
                delay(REFRESH_INTERVAL_MS)
            }
        }
    }

    fun refresh() { viewModelScope.launch { load(); loadForecast() } }

    /** Switch the dashboard to a different branch. Triggers an immediate refresh. */
    fun selectBranch(branch: BranchDto?) {
        _uiState.update { it.copy(selectedBranch = branch) }
        viewModelScope.launch { load(); loadForecast() }
    }

    private suspend fun load() {
        _uiState.update { it.copy(isLoading = it.summary == null) }
        try {
            val branchId = _uiState.value.selectedBranch?.id
            val summary = reportsService.getSalesSummary(branchId = branchId)
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

    /** Forecasts change slowly — refreshed on the same 30s cycle as the summary. Failures are non-fatal. */
    private suspend fun loadForecast() {
        _uiState.update { it.copy(isForecastLoading = it.forecast == null) }
        try {
            val branchId = _uiState.value.selectedBranch?.id
            val forecast = reportsService.getForecast(branchId = branchId)
            _uiState.update { it.copy(forecast = forecast, isForecastLoading = false) }
        } catch (e: Exception) {
            Timber.w(e, "Forecast load failed")
            _uiState.update { it.copy(isForecastLoading = false) }
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
    val branches: List<BranchDto> = emptyList(),
    val selectedBranch: BranchDto? = null,
    val error: String? = null,
    val forecast: DemandForecastDto? = null,
    val isForecastLoading: Boolean = false
)
