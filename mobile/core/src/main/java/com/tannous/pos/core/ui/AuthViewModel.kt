package com.tannous.pos.core.ui

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.repository.AuthRepository
import com.tannous.pos.core.data.repository.AuthState
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import timber.log.Timber
import javax.inject.Inject

@HiltViewModel
class AuthViewModel @Inject constructor(
    private val authRepository: AuthRepository
) : ViewModel() {
    
    private val _authState = MutableStateFlow<AuthState>(AuthState.Loading)
    val authState: StateFlow<AuthState> = _authState.asStateFlow()
    
    init {
        checkAuthState()
    }
    
    private fun checkAuthState() {
        viewModelScope.launch {
            _authState.value = AuthState.Loading
            val isLoggedIn = authRepository.isLoggedIn()
            _authState.value = if (isLoggedIn) {
                val userId = authRepository.getCurrentUserId() ?: ""
                val role = authRepository.getCurrentUserRole() ?: ""
                AuthState.LoggedIn(userId, role)
            } else {
                AuthState.LoggedOut
            }
        }
    }
    
    fun logout() {
        viewModelScope.launch {
            _authState.value = AuthState.Loading
            authRepository.logout()
            _authState.value = AuthState.LoggedOut
        }
    }
    
    fun refreshAuthState() {
        checkAuthState()
    }
}

