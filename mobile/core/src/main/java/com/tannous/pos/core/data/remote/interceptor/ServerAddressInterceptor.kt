package com.tannous.pos.core.data.remote.interceptor

import com.tannous.pos.core.data.remote.ServerAddressStore
import okhttp3.Interceptor
import okhttp3.Response
import javax.inject.Inject

/**
 * Sends every request to the address configured on this tablet, when one is set.
 *
 * Retrofit fixes its base URL when the instance is built, and both instances are singletons, so
 * changing the server without this would mean rebuilding the dependency graph. Rewriting the URL
 * per request is smaller and takes effect on the next call with no restart.
 *
 * Scheme, host and port only. The path, query and headers are untouched, so the API version prefix
 * Retrofit was built with survives regardless of what was typed into the setting.
 */
class ServerAddressInterceptor @Inject constructor(
    private val serverAddressStore: ServerAddressStore
) : Interceptor {

    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request()
        val override = serverAddressStore.overrideUrl() ?: return chain.proceed(request)

        val rewritten = request.url.newBuilder()
            .scheme(override.scheme)
            .host(override.host)
            .port(override.port)
            .build()

        return chain.proceed(request.newBuilder().url(rewritten).build())
    }
}
