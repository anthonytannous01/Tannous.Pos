package com.tannous.pos.feature.reports

import android.content.Context
import android.content.Intent
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.CogsReportDto
import com.tannous.pos.core.data.model.EodReportDto
import com.tannous.pos.core.data.model.KdsPerformanceDto
import com.tannous.pos.core.data.model.SectionSalesReportDto
import com.tannous.pos.core.data.remote.ReportsService
import com.tannous.pos.core.data.repository.SettingsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import retrofit2.HttpException
import timber.log.Timber
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

    fun loadKdsPerformance(preset: String = _uiState.value.kdsPerformance.rangePreset) {
        viewModelScope.launch {
            _uiState.update {
                it.copy(kdsPerformance = it.kdsPerformance.copy(isLoading = true, error = null, rangePreset = preset))
            }
            try {
                val today = LocalDate.now()
                val (from, to) = when (preset) {
                    "today" -> today to today.plusDays(1)
                    "30d"   -> today.minusDays(30) to today.plusDays(1)
                    else    -> today.minusDays(7) to today.plusDays(1)  // "7d" default
                }
                val report = reportsService.getKdsPerformance(
                    from = from.toString() + "T00:00:00Z",
                    to   = to.toString()   + "T00:00:00Z"
                )
                _uiState.update {
                    it.copy(kdsPerformance = it.kdsPerformance.copy(isLoading = false, data = report, error = null))
                }
            } catch (e: HttpException) {
                val msg = if (e.code() == 403) "Reports require owner access"
                          else "Server error: ${e.code()}"
                _uiState.update {
                    it.copy(kdsPerformance = it.kdsPerformance.copy(isLoading = false, error = msg))
                }
            } catch (e: IOException) {
                _uiState.update {
                    it.copy(kdsPerformance = it.kdsPerformance.copy(
                        isLoading = false, error = "No connection. Connect to load reports."))
                }
            } catch (e: Exception) {
                _uiState.update {
                    it.copy(kdsPerformance = it.kdsPerformance.copy(
                        isLoading = false, error = e.message ?: "Failed to load kitchen report"))
                }
            }
        }
    }

    fun selectKdsPreset(preset: String) {
        if (preset != _uiState.value.kdsPerformance.rangePreset ||
            _uiState.value.kdsPerformance.data == null
        ) {
            loadKdsPerformance(preset)
        }
    }

    fun loadSectionSales(preset: String = _uiState.value.sectionSales.rangePreset) {
        viewModelScope.launch {
            _uiState.update {
                it.copy(sectionSales = it.sectionSales.copy(isLoading = true, error = null, rangePreset = preset))
            }
            try {
                val today = LocalDate.now()
                val (from, to) = when (preset) {
                    "today" -> today to today.plusDays(1)
                    "30d"   -> today.minusDays(30) to today.plusDays(1)
                    else    -> today.minusDays(7) to today.plusDays(1)
                }
                val report = reportsService.getSectionSales(
                    from = from.toString() + "T00:00:00Z",
                    to   = to.toString()   + "T00:00:00Z"
                )
                _uiState.update {
                    it.copy(sectionSales = it.sectionSales.copy(isLoading = false, data = report, error = null))
                }
            } catch (e: HttpException) {
                val msg = if (e.code() == 403) "Reports require owner access"
                          else "Server error: ${e.code()}"
                _uiState.update {
                    it.copy(sectionSales = it.sectionSales.copy(isLoading = false, error = msg))
                }
            } catch (e: IOException) {
                _uiState.update {
                    it.copy(sectionSales = it.sectionSales.copy(
                        isLoading = false, error = "No connection. Connect to load reports."))
                }
            } catch (e: Exception) {
                _uiState.update {
                    it.copy(sectionSales = it.sectionSales.copy(
                        isLoading = false, error = e.message ?: "Failed to load section sales"))
                }
            }
        }
    }

    fun selectSectionPreset(preset: String) {
        if (preset != _uiState.value.sectionSales.rangePreset ||
            _uiState.value.sectionSales.data == null
        ) {
            loadSectionSales(preset)
        }
    }

    fun clearSectionSalesError() {
        _uiState.update { it.copy(sectionSales = it.sectionSales.copy(error = null)) }
    }

    fun clearKdsError() {
        _uiState.update { it.copy(kdsPerformance = it.kdsPerformance.copy(error = null)) }
    }

    fun clearError() {
        _uiState.update { it.copy(error = null) }
    }

    fun exportCsv(context: Context) {
        val date = _uiState.value.selectedDate
        viewModelScope.launch {
            _uiState.update { it.copy(isExporting = true, exportError = null) }
            try {
                val dateStr = if (date == LocalDate.now()) null else date.toString()
                val response = reportsService.getEodCsv(dateStr)

                if (!response.isSuccessful) {
                    val msg = if (response.code() == 403) {
                        "Reports require owner access"
                    } else {
                        "Export failed: ${response.code()}"
                    }
                    _uiState.update { it.copy(isExporting = false, exportError = msg) }
                    return@launch
                }

                val csvText = response.body()?.use { it.string() }.orEmpty()
                if (csvText.isBlank()) {
                    _uiState.update {
                        it.copy(isExporting = false, exportError = "No data to export")
                    }
                    return@launch
                }

                val label = if (date == LocalDate.now()) "today" else date.toString()
                val shareIntent = Intent(Intent.ACTION_SEND).apply {
                    type = "text/csv"
                    putExtra(Intent.EXTRA_SUBJECT, "EOD Report $label")
                    putExtra(Intent.EXTRA_TEXT, csvText)
                }
                val chooserIntent = Intent.createChooser(shareIntent, "Export EOD Report")
                    .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
                context.startActivity(chooserIntent)

                _uiState.update { it.copy(isExporting = false) }
            } catch (e: IOException) {
                _uiState.update {
                    it.copy(isExporting = false, exportError = "No connection. Cannot export.")
                }
            } catch (e: Exception) {
                Timber.e(e, "CSV export error")
                _uiState.update {
                    it.copy(isExporting = false, exportError = e.message ?: "Export failed")
                }
            }
        }
    }

    fun clearExportError() {
        _uiState.update { it.copy(exportError = null) }
    }

    /** Export full sales CSV for a date range via the share sheet. */
    fun exportSalesCsv(context: Context, from: LocalDate, to: LocalDate) {
        viewModelScope.launch {
            _uiState.update { it.copy(isExporting = true, exportError = null) }
            try {
                val response = reportsService.getSalesCsv(
                    from = from.toString() + "T00:00:00Z",
                    to   = to.toString()   + "T23:59:59Z"
                )
                if (!response.isSuccessful) {
                    _uiState.update { it.copy(isExporting = false,
                        exportError = if (response.code() == 403) "Reports require owner access"
                                     else "Export failed: ${response.code()}") }
                    return@launch
                }
                val csv = response.body()?.use { it.string() }.orEmpty()
                if (csv.isBlank()) {
                    _uiState.update { it.copy(isExporting = false, exportError = "No data to export") }
                    return@launch
                }
                context.startActivity(Intent.createChooser(
                    Intent(Intent.ACTION_SEND).apply {
                        type = "text/csv"
                        putExtra(Intent.EXTRA_SUBJECT, "Sales $from to $to")
                        putExtra(Intent.EXTRA_TEXT, csv)
                    }, "Export Sales"
                ).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK))
                _uiState.update { it.copy(isExporting = false) }
            } catch (e: IOException) {
                _uiState.update { it.copy(isExporting = false, exportError = "No connection") }
            } catch (e: Exception) {
                Timber.e(e, "Sales CSV export error")
                _uiState.update { it.copy(isExporting = false, exportError = e.message ?: "Export failed") }
            }
        }
    }

    /** Export purchase orders CSV for a date range. */
    fun exportPurchasesCsv(context: Context, from: LocalDate, to: LocalDate) {
        viewModelScope.launch {
            _uiState.update { it.copy(isExporting = true, exportError = null) }
            try {
                val response = reportsService.getPurchasesCsv(
                    from = from.toString() + "T00:00:00Z",
                    to   = to.toString()   + "T23:59:59Z"
                )
                if (!response.isSuccessful) {
                    _uiState.update { it.copy(isExporting = false,
                        exportError = if (response.code() == 403) "Reports require owner access"
                                     else "Export failed: ${response.code()}") }
                    return@launch
                }
                val csv = response.body()?.use { it.string() }.orEmpty()
                if (csv.isBlank()) {
                    _uiState.update { it.copy(isExporting = false, exportError = "No data to export") }
                    return@launch
                }
                context.startActivity(Intent.createChooser(
                    Intent(Intent.ACTION_SEND).apply {
                        type = "text/csv"
                        putExtra(Intent.EXTRA_SUBJECT, "Purchases $from to $to")
                        putExtra(Intent.EXTRA_TEXT, csv)
                    }, "Export Purchases"
                ).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK))
                _uiState.update { it.copy(isExporting = false) }
            } catch (e: IOException) {
                _uiState.update { it.copy(isExporting = false, exportError = "No connection") }
            } catch (e: Exception) {
                Timber.e(e, "Purchases CSV export error")
                _uiState.update { it.copy(isExporting = false, exportError = e.message ?: "Export failed") }
            }
        }
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
    val cogsError: String? = null,
    val isExporting: Boolean = false,
    val exportError: String? = null,
    val kdsPerformance: KdsPerformanceState = KdsPerformanceState(),
    val sectionSales: SectionSalesState = SectionSalesState()
)

data class KdsPerformanceState(
    val isLoading: Boolean = false,
    val data: KdsPerformanceDto? = null,
    val error: String? = null,
    val rangePreset: String = "7d"
)

data class SectionSalesState(
    val isLoading: Boolean = false,
    val data: SectionSalesReportDto? = null,
    val error: String? = null,
    val rangePreset: String = "7d"
)
