package com.tannous.pos.core.data.remote.interceptor

import okhttp3.Interceptor
import okhttp3.Response
import java.util.UUID
import javax.inject.Inject

class IdempotencyKeyInterceptor @Inject constructor() : Interceptor {
    
    override fun intercept(chain: Interceptor.Chain): Response {
        val originalRequest = chain.request()
        
        // Add idempotency key for any mutating request. DELETE was previously excluded here,
        // but several backend controllers (Catalog, Inventory, Suppliers, Orders, Shifts) require
        // Idempotency-Key on DELETE too — that mismatch made every DELETE call from this app fail
        // with 400 "Idempotency-Key header is required", regardless of the target resource.
        if (originalRequest.method in listOf("POST", "PUT", "DELETE")) {
            val idempotencyKey = UUID.randomUUID().toString()
            val requestWithKey = originalRequest.newBuilder()
                .header("Idempotency-Key", idempotencyKey)
                .build()
            return chain.proceed(requestWithKey)
        }
        
        return chain.proceed(originalRequest)
    }
}
