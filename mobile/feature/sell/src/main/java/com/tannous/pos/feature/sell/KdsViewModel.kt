package com.tannous.pos.feature.sell

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.KdsStationDto
import com.tannous.pos.core.data.model.KdsTicketDto
import com.tannous.pos.core.data.model.UpdateKdsStatusRequest
import com.tannous.pos.core.data.remote.KdsService
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
class KdsViewModel @Inject constructor(
    private val kdsService: KdsService
) : ViewModel() {

    private val _uiState = MutableStateFlow(KdsUiState())
    val uiState: StateFlow<KdsUiState> = _uiState.asStateFlow()

    private var pollJob: Job? = null

    init {
        loadStations()
        startPolling()
    }

    fun loadStations() {
        viewModelScope.launch {
            try {
                val stations = kdsService.getStations()
                _uiState.update { it.copy(stations = stations) }
            } catch (e: Exception) {
                Timber.w(e, "KDS stations load failed")
            }
        }
    }

    fun selectStation(station: KdsStationDto?) {
        _uiState.update { it.copy(selectedStation = station) }
        startPolling()
    }

    /** Poll the server every 5 seconds for fresh ticket data. */
    private fun startPolling() {
        pollJob?.cancel()
        pollJob = viewModelScope.launch {
            while (isActive) {
                loadTickets()
                delay(POLL_INTERVAL_MS)
            }
        }
    }

    fun refresh() {
        viewModelScope.launch { loadTickets() }
    }

    private suspend fun loadTickets() {
        try {
            val stationId = _uiState.value.selectedStation?.id
            val tickets = kdsService.getTickets(stationId = stationId)
            _uiState.update { it.copy(tickets = tickets, error = null) }
        } catch (e: Exception) {
            Timber.w(e, "KDS poll failed")
            _uiState.update { it.copy(error = "Could not reach server — showing last known state") }
        }
    }

    fun advanceStatus(ticket: KdsTicketDto) {
        val nextStatus = when (ticket.kdsStatus) {
            KDS_PENDING     -> KDS_IN_PROGRESS
            KDS_IN_PROGRESS -> KDS_DONE
            else            -> return // Done / Cancelled — no-op
        }
        viewModelScope.launch {
            // Optimistic update
            _uiState.update { state ->
                state.copy(tickets = state.tickets.map { t ->
                    if (t.orderLineId == ticket.orderLineId) t.copy(kdsStatus = nextStatus) else t
                })
            }
            try {
                val updated = kdsService.updateStatus(
                    ticket.orderLineId,
                    UpdateKdsStatusRequest(status = nextStatus)
                )
                // Replace with server-confirmed state
                _uiState.update { state ->
                    val filtered = if (nextStatus == KDS_DONE) {
                        // Remove done items from the board immediately
                        state.tickets.filter { it.orderLineId != updated.orderLineId }
                    } else {
                        state.tickets.map { t ->
                            if (t.orderLineId == updated.orderLineId) updated else t
                        }
                    }
                    state.copy(tickets = filtered)
                }
            } catch (e: Exception) {
                Timber.e(e, "KDS status update failed for line ${ticket.orderLineId}")
                // Roll back optimistic update on failure
                loadTickets()
                _uiState.update { it.copy(error = "Update failed — refreshed") }
            }
        }
    }

    fun clearError() = _uiState.update { it.copy(error = null) }

    override fun onCleared() {
        super.onCleared()
        pollJob?.cancel()
    }

    companion object {
        const val POLL_INTERVAL_MS = 5_000L
        const val KDS_PENDING     = 0
        const val KDS_IN_PROGRESS = 1
        const val KDS_DONE        = 2
    }
}

data class KdsUiState(
    val tickets: List<KdsTicketDto> = emptyList(),
    val stations: List<KdsStationDto> = emptyList(),
    val selectedStation: KdsStationDto? = null,
    val error: String? = null
)
