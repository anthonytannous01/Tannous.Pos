package com.tannous.pos.feature.sell

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.LoyaltyAccountDto
import com.tannous.pos.core.data.remote.LoyaltyService
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import timber.log.Timber
import javax.inject.Inject

@HiltViewModel
class LoyaltyViewModel @Inject constructor(
    private val loyaltyService: LoyaltyService
) : ViewModel() {

    private val _uiState = MutableStateFlow(LoyaltyUiState())
    val uiState: StateFlow<LoyaltyUiState> = _uiState.asStateFlow()

    fun loadAccount(customerId: String) {
        _uiState.update { it.copy(isLoading = true, error = null, customerId = customerId) }
        viewModelScope.launch {
            try {
                val account = loyaltyService.getAccount(customerId)
                _uiState.update { it.copy(account = account, isLoading = false) }
            } catch (e: Exception) {
                val is404 = e.message?.contains("404") == true ||
                            e.javaClass.simpleName == "HttpException" &&
                            e.toString().contains("404")
                if (is404) {
                    _uiState.update { it.copy(account = null, isLoading = false, noAccount = true) }
                } else {
                    Timber.e(e, "Loyalty load failed")
                    _uiState.update { it.copy(error = "Failed to load loyalty account", isLoading = false) }
                }
            }
        }
    }

    fun redeem(points: Int, orderId: String? = null) {
        val customerId = _uiState.value.customerId ?: return
        _uiState.update { it.copy(isRedeeming = true, error = null, redeemSuccess = false) }
        viewModelScope.launch {
            try {
                val updated = loyaltyService.redeem(
                    customerId,
                    com.tannous.pos.core.data.model.RedeemPointsRequest(points, orderId)
                )
                _uiState.update {
                    it.copy(account = updated, isRedeeming = false, redeemSuccess = true)
                }
            } catch (e: Exception) {
                Timber.e(e, "Loyalty redeem failed")
                _uiState.update { it.copy(error = e.message ?: "Redemption failed", isRedeeming = false) }
            }
        }
    }

    fun clearError() = _uiState.update { it.copy(error = null, redeemSuccess = false) }
}

data class LoyaltyUiState(
    val isLoading: Boolean = false,
    val isRedeeming: Boolean = false,
    val account: LoyaltyAccountDto? = null,
    val noAccount: Boolean = false,
    val customerId: String? = null,
    val error: String? = null,
    val redeemSuccess: Boolean = false
)
