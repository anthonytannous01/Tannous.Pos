package com.tannous.pos.core.data.remote.interceptor

import android.content.Context
import com.tannous.pos.core.data.local.dao.KeyValueDao
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.runBlocking
import okhttp3.Interceptor
import okhttp3.Response
import java.util.UUID
import javax.inject.Inject

class DeviceIdInterceptor @Inject constructor(
    @ApplicationContext private val context: Context,
    private val keyValueDao: KeyValueDao
) : Interceptor {
    
    override fun intercept(chain: Interceptor.Chain): Response {
        val originalRequest = chain.request()
        
        val deviceId = runBlocking { 
            keyValueDao.get("device_id") ?: generateAndStoreDeviceId()
        }
        
        val requestWithDeviceId = originalRequest.newBuilder()
            .header("Device-Id", deviceId)
            .build()
        
        return chain.proceed(requestWithDeviceId)
    }
    
    private suspend fun generateAndStoreDeviceId(): String {
        val deviceId = UUID.randomUUID().toString()
        keyValueDao.set(com.tannous.pos.core.data.local.entity.KeyValueEntity("device_id", deviceId))
        return deviceId
    }
}
