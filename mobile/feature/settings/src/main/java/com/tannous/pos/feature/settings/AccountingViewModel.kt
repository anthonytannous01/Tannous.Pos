package com.tannous.pos.feature.settings

import android.content.Context
import android.content.Intent
import android.net.Uri
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.AccountingConnectionStatusDto
import com.tannous.pos.core.data.remote.AccountingService
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import timber.log.Timber
import javax.inject.Inject

@HiltViewModel
class AccountingViewModel @Inject constructor(
    private val accountingService: AccountingService
) : ViewModel() {

    private val _uiState = MutableStateFlow(AccountingUiState())
    val uiState: StateFlow<AccountingUiState> = _uiState.asStateFlow()

    init {
        loadStatus()
    }

    fun loadStatus() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val connections = accountingService.getStatus()
                _uiState.update { it.copy(isLoading = false, connections = connections) }
            } catch (e: Exception) {
                Timber.w(e, "Failed to load accounting status")
                _uiState.update {
                    it.copy(isLoading = false, error = e.message ?: "Failed to load accounting status")
                }
            }
        }
    }

    fun connectQuickBooks(context: Context) {
        viewModelScope.launch {
            try {
                val response = accountingService.getQuickBooksConnectUrl()
                if (response.authorizationUrl.isNotBlank()) {
                    val intent = Intent(Intent.ACTION_VIEW, Uri.parse(response.authorizationUrl))
                    context.startActivity(intent)
                } else {
                    _uiState.update { it.copy(error = "QuickBooks connect URL was empty") }
                }
            } catch (e: Exception) {
                Timber.e(e, "QuickBooks connect failed")
                _uiState.update { it.copy(error = e.message ?: "Could not start QuickBooks connection") }
            }
        }
    }

    fun triggerSync() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null, syncMessage = null) }
            try {
                val result = accountingService.triggerSync()
                val message = if (result.errors.isEmpty()) {
                    "Synced ${result.synced} connection(s)"
                } else {
                    "Synced ${result.synced}; ${result.errors.size} error(s)"
                }
                _uiState.update { it.copy(isLoading = false, syncMessage = message) }
                loadStatus()
            } catch (e: Exception) {
                Timber.e(e, "Manual accounting sync failed")
                _uiState.update {
                    it.copy(isLoading = false, error = e.message ?: "Sync failed")
                }
            }
        }
    }

    fun disconnect(provider: String) {
        viewModelScope.launch {
            try {
                accountingService.disconnect(provider)
                loadStatus()
            } catch (e: Exception) {
                Timber.e(e, "Disconnect failed for $provider")
                _uiState.update { it.copy(error = e.message ?: "Disconnect failed") }
            }
        }
    }

    fun clearError() = _uiState.update { it.copy(error = null) }

    fun clearSyncMessage() = _uiState.update { it.copy(syncMessage = null) }
}

data class AccountingUiState(
    val isLoading: Boolean = false,
    val connections: List<AccountingConnectionStatusDto> = emptyList(),
    val error: String? = null,
    val syncMessage: String? = null
)
