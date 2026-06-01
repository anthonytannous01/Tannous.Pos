package com.tannous.pos.core.data.remote.interceptor

import com.tannous.pos.core.data.local.dao.KeyValueDao
import okhttp3.Interceptor
import okhttp3.Response
import javax.inject.Inject
import kotlinx.coroutines.runBlocking

class EtagInterceptor @Inject constructor(
    private val keyValueDao: KeyValueDao
) : Interceptor {
    
    override fun intercept(chain: Interceptor.Chain): Response {
        val originalRequest = chain.request()
        
        // Only handle GET requests for master data
        if (originalRequest.method != "GET") {
            return chain.proceed(originalRequest)
        }
        
        val url = originalRequest.url.toString()
        val etagKey = "etag_${url.hashCode()}"
        
        // Check if we have a cached ETag for this URL
        val cachedEtag = runBlocking { keyValueDao.get(etagKey) }
        
        val requestBuilder = originalRequest.newBuilder()
        if (cachedEtag != null) {
            requestBuilder.header("If-None-Match", cachedEtag)
        }
        
        val response = chain.proceed(requestBuilder.build())
        
        // Store new ETag if provided
        val newEtag = response.header("ETag")
        if (newEtag != null) {
            runBlocking { 
                keyValueDao.set(com.tannous.pos.core.data.local.entity.KeyValueEntity(etagKey, newEtag))
            }
        }
        
        return response
    }
}
