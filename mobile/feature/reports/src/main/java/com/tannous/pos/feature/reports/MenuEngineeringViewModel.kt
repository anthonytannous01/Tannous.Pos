package com.tannous.pos.feature.reports

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.MenuEngineeringReportDto
import com.tannous.pos.core.data.remote.ReportsService
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import timber.log.Timber
import java.time.LocalDate
import java.time.format.DateTimeFormatter
import javax.inject.Inject

@HiltViewModel
class MenuEngineeringViewModel @Inject constructor(
    private val reportsService: ReportsService
) : ViewModel() {

    private val _uiState = MutableStateFlow(MenuEngineeringUiState())
    val uiState: StateFlow<MenuEngineeringUiState> = _uiState.asStateFlow()

    private val fmt = DateTimeFormatter.ISO_LOCAL_DATE_TIME

    init {
        // Default: last 30 days
        val to   = LocalDate.now().atTime(23, 59, 59)
        val from = to.minusDays(30).toLocalDate().atStartOfDay()
        _uiState.update { it.copy(from = from.format(fmt), to = to.format(fmt)) }
        load()
    }

    fun setRange(from: String, to: String) {
        _uiState.update { it.copy(from = from, to = to) }
        load()
    }

    fun load() {
        val state = _uiState.value
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val report = reportsService.getMenuEngineering(state.from, state.to)
                _uiState.update { it.copy(report = report, isLoading = false) }
            } catch (e: retrofit2.HttpException) {
                val body = runCatching { e.response()?.errorBody()?.string() }.getOrNull()
                Timber.e(e, "Menu engineering HTTP %d: %s", e.code(), body)
                _uiState.update { it.copy(error = "HTTP ${e.code()}: $body", isLoading = false) }
            } catch (e: Exception) {
                Timber.e(e, "Menu engineering load failed")
                _uiState.update { it.copy(error = e.message ?: "Load failed", isLoading = false) }
            }
        }
    }

    fun clearError() = _uiState.update { it.copy(error = null) }
}

data class MenuEngineeringUiState(
    val isLoading: Boolean = false,
    val report: MenuEngineeringReportDto? = null,
    val error: String? = null,
    val from: String = "",
    val to: String = ""
)
