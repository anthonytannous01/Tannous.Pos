package com.tannous.pos.core.data.local

import com.tannous.pos.core.data.local.dao.KeyValueDao
import com.tannous.pos.core.data.local.entity.KeyValueEntity
import kotlinx.coroutines.flow.firstOrNull
import timber.log.Timber
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class TokenManager @Inject constructor(
    private val keyValueDao: KeyValueDao
) {
    
    companion object {
        private const val KEY_ACCESS_TOKEN = "access_token"
        private const val KEY_REFRESH_TOKEN = "refresh_token"
        private const val KEY_TOKEN_EXPIRES_AT = "token_expires_at"
        private const val KEY_USER_ID = "user_id"
        private const val KEY_USER_ROLE = "user_role"
    }
    
    suspend fun getAccessToken(): String? {
        return try {
            keyValueDao.get(KEY_ACCESS_TOKEN)
        } catch (e: Exception) {
            Timber.e(e, "Error getting access token")
            null
        }
    }
    
    suspend fun getRefreshToken(): String? {
        return try {
            keyValueDao.get(KEY_REFRESH_TOKEN)
        } catch (e: Exception) {
            Timber.e(e, "Error getting refresh token")
            null
        }
    }
    
    suspend fun getTokenExpiresAt(): Long? {
        return try {
            keyValueDao.get(KEY_TOKEN_EXPIRES_AT)?.toLongOrNull()
        } catch (e: Exception) {
            Timber.e(e, "Error getting token expiry")
            null
        }
    }
    
    suspend fun getUserId(): String? {
        return try {
            keyValueDao.get(KEY_USER_ID)
        } catch (e: Exception) {
            Timber.e(e, "Error getting user ID")
            null
        }
    }
    
    suspend fun getUserRole(): String? {
        return try {
            keyValueDao.get(KEY_USER_ROLE)
        } catch (e: Exception) {
            Timber.e(e, "Error getting user role")
            null
        }
    }
    
    suspend fun saveTokens(
        accessToken: String,
        refreshToken: String,
        expiresIn: Int, // seconds
        userId: String,
        role: String
    ) {
        try {
            val expiresAt = System.currentTimeMillis() + (expiresIn * 1000L)
            
            keyValueDao.set(KeyValueEntity(KEY_ACCESS_TOKEN, accessToken))
            keyValueDao.set(KeyValueEntity(KEY_REFRESH_TOKEN, refreshToken))
            keyValueDao.set(KeyValueEntity(KEY_TOKEN_EXPIRES_AT, expiresAt.toString()))
            keyValueDao.set(KeyValueEntity(KEY_USER_ID, userId))
            keyValueDao.set(KeyValueEntity(KEY_USER_ROLE, role))
            
            Timber.d("Tokens saved successfully")
        } catch (e: Exception) {
            Timber.e(e, "Error saving tokens")
            throw e
        }
    }
    
    suspend fun clearTokens() {
        try {
            keyValueDao.delete(KEY_ACCESS_TOKEN)
            keyValueDao.delete(KEY_REFRESH_TOKEN)
            keyValueDao.delete(KEY_TOKEN_EXPIRES_AT)
            keyValueDao.delete(KEY_USER_ID)
            keyValueDao.delete(KEY_USER_ROLE)
            
            Timber.d("Tokens cleared successfully")
        } catch (e: Exception) {
            Timber.e(e, "Error clearing tokens")
        }
    }
    
    suspend fun isTokenValid(): Boolean {
        val expiresAt = getTokenExpiresAt()
        return expiresAt != null && expiresAt > System.currentTimeMillis()
    }
    
    suspend fun hasTokens(): Boolean {
        return getAccessToken() != null && getRefreshToken() != null
    }
}

