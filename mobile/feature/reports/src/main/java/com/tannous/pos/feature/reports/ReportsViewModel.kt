package com.tannous.pos.feature.reports

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.CogsReportDto
import com.tannous.pos.core.data.model.EodReportDto
import com.tannous.pos.core.data.remote.ReportsService
import com.tannous.pos.core.data.repository.SettingsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import retrofit2.HttpException
import java.io.IOException
import java.time.LocalDate
import javax.inject.Inject

@HiltViewModel
class ReportsViewModel @Inject constructor(
    private val reportsService: ReportsService,
    private val settingsRepository: SettingsRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(ReportsUiState())
    val uiState: StateFlow<ReportsUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            _uiState.update { it.copy(currencyCode = settingsRepository.getCurrency()) }
        }
        loadReport()
    }

    fun loadReport(date: LocalDate = _uiState.value.selectedDate) {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val dateStr = if (date == LocalDate.now()) null else date.toString()
                val report = reportsService.getEodReport(dateStr)
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        report = report,
                        selectedDate = date
                    )
                }
            } catch (e: HttpException) {
                val msg = if (e.code() == 403) {
                    "Reports require owner access"
                } else {
                    "Server error: ${e.code()}"
                }
                _uiState.update { it.copy(isLoading = false, error = msg) }
            } catch (e: IOException) {
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        error = "No connection. Connect to load reports."
                    )
                }
            } catch (e: Exception) {
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        error = e.message ?: "Failed to load report"
                    )
                }
            }
        }
    }

    fun selectDate(date: LocalDate) {
        if (date != _uiState.value.selectedDate) {
            loadReport(date)
        }
    }

    fun selectTab(index: Int) {
        _uiState.update { it.copy(selectedTab = index) }
        if (index == 1 &&
            _uiState.value.cogsReport == null &&
            !_uiState.value.isCogsLoading
        ) {
            loadCogsReport()
        }
    }

    fun loadCogsReport(
        from: LocalDate = _uiState.value.cogsFromDate,
        to: LocalDate = _uiState.value.cogsToDate
    ) {
        viewModelScope.launch {
            _uiState.update { it.copy(isCogsLoading = true, cogsError = null) }
            try {
                val report = reportsService.getCogsReport(from.toString(), to.toString())
                _uiState.update {
                    it.copy(
                        isCogsLoading = false,
                        cogsReport = report,
                        cogsFromDate = from,
                        cogsToDate = to
                    )
                }
            } catch (e: HttpException) {
                val msg = if (e.code() == 403) {
                    "Reports require owner access"
                } else {
                    "Server error: ${e.code()}"
                }
                _uiState.update { it.copy(isCogsLoading = false, cogsError = msg) }
            } catch (e: IOException) {
                _uiState.update {
                    it.copy(
                        isCogsLoading = false,
                        cogsError = "No connection. Connect to load reports."
                    )
                }
            } catch (e: Exception) {
                _uiState.update {
                    it.copy(
                        isCogsLoading = false,
                        cogsError = e.message ?: "Failed to load COGS report"
                    )
                }
            }
        }
    }

    fun selectCogsRange(from: LocalDate, to: LocalDate) {
        if (from != _uiState.value.cogsFromDate || to != _uiState.value.cogsToDate) {
            loadCogsReport(from, to)
        }
    }

    fun clearError() {
        _uiState.update { it.copy(error = null) }
    }
}

data class ReportsUiState(
    val isLoading: Boolean = false,
    val error: String? = null,
    val report: EodReportDto? = null,
    val selectedDate: LocalDate = LocalDate.now(),
    val currencyCode: String = "USD",
    val selectedTab: Int = 0,
    val cogsFromDate: LocalDate = LocalDate.now().withDayOfMonth(1),
    val cogsToDate: LocalDate = LocalDate.now(),
    val cogsReport: CogsReportDto? = null,
    val isCogsLoading: Boolean = false,
    val cogsError: String? = null
)
