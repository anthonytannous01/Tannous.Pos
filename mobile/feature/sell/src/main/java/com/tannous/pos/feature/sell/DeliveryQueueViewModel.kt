package com.tannous.pos.feature.sell

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.DeliveryDto
import com.tannous.pos.core.data.model.UpdateDeliveryStatusRequest
import com.tannous.pos.core.data.remote.DeliveryService
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

// Delivery status constants matching backend DeliveryStatus enum
const val DELIVERY_PENDING   = 0
const val DELIVERY_ASSIGNED  = 1
const val DELIVERY_PICKED_UP = 2
const val DELIVERY_ON_WAY    = 3
const val DELIVERY_DELIVERED = 4
const val DELIVERY_FAILED    = 5
const val DELIVERY_CANCELLED = 6

val deliveryChannelLabels = mapOf(
    0 to "Own", 1 to "Toters", 2 to "Talabat", 3 to "Wolt", 4 to "Other"
)

val deliveryChannelLabelsAr = mapOf(
    0 to "خاص", 1 to "توترز", 2 to "طلبات", 3 to "وولت", 4 to "أخرى"
)

val deliveryStatusLabels = mapOf(
    DELIVERY_PENDING   to "Pending",
    DELIVERY_ASSIGNED  to "Assigned",
    DELIVERY_PICKED_UP to "Picked Up",
    DELIVERY_ON_WAY    to "On the Way",
    DELIVERY_DELIVERED to "Delivered",
    DELIVERY_FAILED    to "Failed",
    DELIVERY_CANCELLED to "Cancelled"
)

@HiltViewModel
class DeliveryQueueViewModel @Inject constructor(
    private val deliveryService: DeliveryService
) : ViewModel() {

    private val _uiState = MutableStateFlow(DeliveryQueueUiState())
    val uiState: StateFlow<DeliveryQueueUiState> = _uiState.asStateFlow()

    private var pollJob: Job? = null

    init { startPolling() }

    private fun startPolling() {
        pollJob?.cancel()
        pollJob = viewModelScope.launch {
            while (isActive) {
                load()
                delay(15_000L) // refresh every 15s
            }
        }
    }

    fun refresh() { viewModelScope.launch { load() } }

    private suspend fun load() {
        try {
            val queue = deliveryService.getQueue()
            _uiState.update { it.copy(deliveries = queue, error = null, isLoading = false) }
        } catch (e: Exception) {
            Timber.w(e, "Failed to load delivery queue")
            _uiState.update { it.copy(isLoading = false, error = "Could not load delivery queue") }
        }
    }

    fun updateStatus(id: String, status: Int, driverName: String? = null, driverPhone: String? = null) {
        viewModelScope.launch {
            try {
                deliveryService.updateStatus(id, UpdateDeliveryStatusRequest(
                    status = status,
                    driverName = driverName,
                    driverPhone = driverPhone
                ))
                load()
            } catch (e: Exception) {
                Timber.w(e, "Failed to update delivery status")
                _uiState.update { it.copy(error = "Could not update delivery") }
            }
        }
    }

    fun filterByChannel(channel: Int?) =
        _uiState.update { it.copy(selectedChannel = channel) }

    fun clearError() = _uiState.update { it.copy(error = null) }

    override fun onCleared() { super.onCleared(); pollJob?.cancel() }
}

data class DeliveryQueueUiState(
    val deliveries:      List<DeliveryDto> = emptyList(),
    val isLoading:       Boolean           = true,
    val error:           String?           = null,
    val selectedChannel: Int?              = null
)
