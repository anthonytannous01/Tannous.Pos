package com.tannous.pos.feature.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.remote.ServerAddressStore
import com.tannous.pos.core.data.repository.AuthRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import timber.log.Timber
import javax.inject.Inject

@HiltViewModel
class LoginViewModel @Inject constructor(
    private val authRepository: AuthRepository,
    private val serverAddressStore: ServerAddressStore
) : ViewModel() {
    
    private val _uiState = MutableStateFlow(LoginUiState())
    val uiState: StateFlow<LoginUiState> = _uiState.asStateFlow()

    init {
        loadServerAddress()
    }

    // The server address is editable from this screen on purpose. A wrong address stops the app
    // reaching the server, which stops login, which would put the only fix behind the thing it
    // breaks. That happened once in testing and cost a server restart on the wrong port to undo.

    private fun loadServerAddress() {
        _uiState.value = _uiState.value.copy(
            serverAddress = serverAddressStore.effectiveAddress(),
            serverAddressIsOverridden = serverAddressStore.isOverridden(),
            serverAddressDefault = serverAddressStore.compiledAddress(),
            serverAddressError = null
        )
    }

    fun showServerSettings() {
        loadServerAddress()
        _uiState.value = _uiState.value.copy(showServerSettings = true)
    }

    fun dismissServerSettings() {
        _uiState.value = _uiState.value.copy(showServerSettings = false, serverAddressError = null)
    }

    fun setServerAddressInput(value: String) {
        _uiState.value = _uiState.value.copy(serverAddress = value, serverAddressError = null)
    }

    fun saveServerAddress() {
        if (serverAddressStore.save(_uiState.value.serverAddress)) {
            Timber.i("Server address changed from the login screen")
            loadServerAddress()
            _uiState.value = _uiState.value.copy(showServerSettings = false, error = null)
        } else {
            _uiState.value = _uiState.value.copy(
                serverAddressError = "Not a usable address. Example: 192.168.10.231:7000"
            )
        }
    }

    fun resetServerAddress() {
        serverAddressStore.save(null)
        Timber.i("Server address reset to the compiled-in default from the login screen")
        loadServerAddress()
        _uiState.value = _uiState.value.copy(error = null)
    }
    
    fun updateUsername(username: String) {
        _uiState.value = _uiState.value.copy(username = username)
    }
    
    fun updatePassword(password: String) {
        _uiState.value = _uiState.value.copy(password = password)
    }
    
    fun login() {
        val currentState = _uiState.value
        if (currentState.username.isBlank() || currentState.password.isBlank()) {
            return
        }
        
        viewModelScope.launch {
            _uiState.value = currentState.copy(isLoading = true, error = null)
            
            val result = authRepository.login(currentState.username, currentState.password)
            
            result.fold(
                onSuccess = { response ->
                    Timber.d("Login successful for user: ${response.user.username}")
                    _uiState.value = currentState.copy(
                        isLoading = false,
                        isLoggedIn = true
                    )
                },
                onFailure = { error ->
                    Timber.e(error, "Login failed")
                    _uiState.value = currentState.copy(
                        isLoading = false,
                        error = error.message ?: "Login failed. Please check your credentials."
                    )
                }
            )
        }
    }
}

data class LoginUiState(
    val username: String = "",
    val password: String = "",
    val isLoading: Boolean = false,
    val isLoggedIn: Boolean = false,
    val error: String? = null,
    val showServerSettings: Boolean = false,
    val serverAddress: String = "",
    val serverAddressIsOverridden: Boolean = false,
    val serverAddressDefault: String = "",
    val serverAddressError: String? = null
)
