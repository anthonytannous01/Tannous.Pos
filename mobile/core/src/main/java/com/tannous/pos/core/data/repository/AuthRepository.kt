package com.tannous.pos.core.data.repository

import com.tannous.pos.core.data.local.TokenManager
import com.tannous.pos.core.data.model.LoginRequest
import com.tannous.pos.core.data.model.LoginResponse
import com.tannous.pos.core.data.model.RefreshTokenRequest
import com.tannous.pos.core.data.remote.AuthService
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import timber.log.Timber
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AuthRepository @Inject constructor(
    private val authService: AuthService,
    private val tokenManager: TokenManager
) {
    
    suspend fun login(username: String, password: String): Result<LoginResponse> {
        return try {
            val request = LoginRequest(username = username, password = password)
            val response = authService.login(request)
            
            // Store tokens and user info
            tokenManager.saveTokens(
                accessToken = response.accessToken,
                refreshToken = response.refreshToken,
                expiresIn = response.expiresIn,
                userId = response.user.id,
                role = response.user.role
            )
            
            Timber.d("Login successful for user: ${response.user.username}")
            Result.success(response)
        } catch (e: Exception) {
            Timber.e(e, "Login failed for user: $username")
            Result.failure(e)
        }
    }
    
    suspend fun logout() {
        try {
            val refreshToken = tokenManager.getRefreshToken()
            if (refreshToken != null) {
                try {
                    authService.logout(RefreshTokenRequest(refreshToken))
                } catch (e: Exception) {
                    Timber.w(e, "Logout API call failed, clearing tokens anyway")
                }
            }
            tokenManager.clearTokens()
            Timber.d("User logged out successfully")
        } catch (e: Exception) {
            Timber.e(e, "Error during logout")
            // Clear tokens anyway
            tokenManager.clearTokens()
        }
    }
    
    suspend fun refreshToken(): Result<Unit> {
        return try {
            val refreshToken = tokenManager.getRefreshToken()
                ?: return Result.failure(IllegalStateException("No refresh token available"))
            
            val response = authService.refreshToken(RefreshTokenRequest(refreshToken))
            
            tokenManager.saveTokens(
                accessToken = response.accessToken,
                refreshToken = response.refreshToken,
                expiresIn = response.expiresIn,
                userId = response.user.id,
                role = response.user.role
            )
            
            Timber.d("Token refreshed successfully")
            Result.success(Unit)
        } catch (e: Exception) {
            Timber.e(e, "Token refresh failed")
            tokenManager.clearTokens()
            Result.failure(e)
        }
    }
    
    suspend fun isLoggedIn(): Boolean {
        return tokenManager.hasTokens() && tokenManager.isTokenValid()
    }
    
    suspend fun getCurrentUserId(): String? {
        return tokenManager.getUserId()
    }
    
    suspend fun getCurrentUserRole(): String? {
        return tokenManager.getUserRole()
    }
    
    suspend fun getAuthState(): AuthState {
        val hasTokens = tokenManager.hasTokens()
        val isValid = tokenManager.isTokenValid()
        
        return if (hasTokens && isValid) {
            AuthState.LoggedIn(
                userId = tokenManager.getUserId() ?: "",
                role = tokenManager.getUserRole() ?: ""
            )
        } else {
            AuthState.LoggedOut
        }
    }
}

sealed class AuthState {
    object LoggedOut : AuthState()
    data class LoggedIn(val userId: String, val role: String) : AuthState()
    object Loading : AuthState()
}
