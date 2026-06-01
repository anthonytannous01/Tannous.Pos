package com.tannous.pos.core.data.remote.interceptor

import okhttp3.Interceptor
import okhttp3.Response
import java.util.UUID
import javax.inject.Inject

class IdempotencyKeyInterceptor @Inject constructor() : Interceptor {
    
    override fun intercept(chain: Interceptor.Chain): Response {
        val originalRequest = chain.request()
        
        // Only add idempotency key for POST and PUT requests
        if (originalRequest.method in listOf("POST", "PUT")) {
            val idempotencyKey = UUID.randomUUID().toString()
            val requestWithKey = originalRequest.newBuilder()
                .header("Idempotency-Key", idempotencyKey)
                .build()
            return chain.proceed(requestWithKey)
        }
        
        return chain.proceed(originalRequest)
    }
}
