package com.tannous.pos.core.data.remote.interceptor

import com.tannous.pos.core.data.local.TokenManager
import com.tannous.pos.core.data.model.RefreshTokenRequest
import com.tannous.pos.core.data.remote.AuthService
import kotlinx.coroutines.runBlocking
import okhttp3.Authenticator
import okhttp3.Request
import okhttp3.Response
import okhttp3.Route
import timber.log.Timber
import javax.inject.Inject
import javax.inject.Named

class TokenAuthenticator @Inject constructor(
    private val tokenManager: TokenManager,
    @Named("AuthService") private val authService: AuthService
) : Authenticator {
    
    private val refreshMutex = Any()
    @Volatile
    private var isRefreshing = false
    
    override fun authenticate(route: Route?, response: Response): Request? {
        // Don't retry auth endpoints
        if (response.request.url.encodedPath.contains("/auth/")) {
            return null
        }
        
        // Check if we already tried to refresh
        if (responseCount(response) >= 2) {
            // Already tried refreshing, give up
            runBlocking {
                tokenManager.clearTokens()
            }
            return null
        }
        
        synchronized(refreshMutex) {
            val refreshToken = runBlocking { tokenManager.getRefreshToken() }
            
            if (refreshToken == null) {
                Timber.d("No refresh token available")
                runBlocking { tokenManager.clearTokens() }
                return null
            }
            
            // If we're already refreshing, wait a bit and retry with the original request
            if (isRefreshing) {
                Thread.sleep(100)
                val newAccessToken = runBlocking { tokenManager.getAccessToken() }
                return newAccessToken?.let {
                    response.request.newBuilder()
                        .header("Authorization", "Bearer $it")
                        .build()
                }
            }
            
            isRefreshing = true
            try {
                val refreshResponse = runBlocking {
                    authService.refreshToken(RefreshTokenRequest(refreshToken))
                }
                
                runBlocking {
                    tokenManager.saveTokens(
                        accessToken = refreshResponse.accessToken,
                        refreshToken = refreshResponse.refreshToken,
                        expiresIn = refreshResponse.expiresIn,
                        userId = refreshResponse.user.id,
                        role = refreshResponse.user.role
                    )
                }
                
                // Retry the original request with the new token
                return response.request.newBuilder()
                    .header("Authorization", "Bearer ${refreshResponse.accessToken}")
                    .build()
            } catch (e: Exception) {
                Timber.e(e, "Token refresh failed")
                runBlocking { tokenManager.clearTokens() }
                return null
            } finally {
                isRefreshing = false
            }
        }
    }
    
    private fun responseCount(response: Response): Int {
        var result = 1
        var current = response.priorResponse
        while (current != null) {
            result++
            current = current.priorResponse
        }
        return result
    }
}

