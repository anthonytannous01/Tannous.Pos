package com.tannous.pos.core.data.remote.interceptor

import okhttp3.Interceptor
import okhttp3.Response
import timber.log.Timber
import java.util.concurrent.TimeUnit

/**
 * Interceptor to handle 429 (Too Many Requests) responses with Retry-After header.
 * Implements exponential backoff for rate limiting.
 */
class RetryAfterInterceptor : Interceptor {
    
    companion object {
        private const val MAX_RETRIES = 3
        private const val INITIAL_DELAY_MS = 1000L
        private const val MAX_DELAY_MS = 30_000L
    }
    
    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request()
        var response: Response? = null
        var retryCount = 0
        
        while (retryCount < MAX_RETRIES) {
            try {
                response = chain.proceed(request)
                
                // If we get a 429, check for Retry-After header
                if (response.code == 429) {
                    val retryAfter = response.header("Retry-After")
                    val delayMs = if (retryAfter != null) {
                        try {
                            retryAfter.toLong() * 1000 // Convert seconds to milliseconds
                        } catch (e: NumberFormatException) {
                            Timber.w(e, "Invalid Retry-After header: $retryAfter")
                            calculateExponentialBackoff(retryCount)
                        }
                    } else {
                        calculateExponentialBackoff(retryCount)
                    }
                    
                    Timber.w("Rate limited (429). Retrying after ${delayMs}ms (attempt ${retryCount + 1}/$MAX_RETRIES)")
                    
                    // Close the current response
                    response.close()
                    
                    // Wait for the specified delay
                    safeSleep(delayMs.coerceIn(0L, MAX_DELAY_MS))
                    retryCount++
                    continue
                }
                
                // If not 429, return the response
                return response
                
            } catch (e: Exception) {
                Timber.e(e, "Network error during request")
                response?.close()
                
                if (retryCount < MAX_RETRIES - 1) {
                    val delayMs = calculateExponentialBackoff(retryCount)
                    Timber.w("Retrying after ${delayMs}ms due to network error (attempt ${retryCount + 1}/$MAX_RETRIES)")
                    safeSleep(delayMs.coerceIn(0L, MAX_DELAY_MS))
                    retryCount++
                    continue
                } else {
                    throw e
                }
            }
        }
        
        // If we've exhausted all retries, return the last response
        return response ?: throw IllegalStateException("No response available after $MAX_RETRIES retries")
    }
    
    private fun calculateExponentialBackoff(retryCount: Int): Long {
        return (INITIAL_DELAY_MS * (1L shl retryCount)).coerceAtMost(MAX_DELAY_MS) // 1s, 2s, 4s...
    }

    private fun safeSleep(delayMs: Long) {
        try {
            Thread.sleep(delayMs)
        } catch (e: InterruptedException) {
            Thread.currentThread().interrupt()
            throw e
        }
    }
}
