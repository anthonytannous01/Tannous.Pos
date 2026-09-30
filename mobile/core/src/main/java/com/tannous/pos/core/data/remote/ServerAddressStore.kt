package com.tannous.pos.core.data.remote

import android.content.Context
import com.tannous.pos.core.BuildConfig
import dagger.hilt.android.qualifiers.ApplicationContext
import okhttp3.HttpUrl
import okhttp3.HttpUrl.Companion.toHttpUrlOrNull
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Where the app looks for the server, when the compiled-in address is not where it is.
 *
 * `BuildConfig.BASE_URL` is fixed at build time from API_BASE_URL in local.properties. On a
 * restaurant LAN that address is a DHCP lease, and when it moves every tablet stops at once with no
 * way to fix it short of rebuilding and reinstalling the APK. This lets someone change it on the
 * tablet instead.
 *
 * Backed by SharedPreferences rather than Room or DataStore for one reason: [ServerAddressInterceptor]
 * reads it on an OkHttp thread for every request, where a suspending read would have to block.
 *
 * Only scheme, host and port are ever taken from the override. The path stays whatever Retrofit was
 * built with, so entering "http://192.168.1.5:7000" and "http://192.168.1.5:7000/api/v1.0/" mean the
 * same thing and neither can break the API path.
 */
@Singleton
class ServerAddressStore @Inject constructor(
    @ApplicationContext context: Context
) {
    private val prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    /** The address in use, whether that is the override or the compiled-in default. */
    fun effectiveAddress(): String = overrideRaw() ?: BuildConfig.BASE_URL

    /** True when a tablet-local override is in force, so the UI can say so. */
    fun isOverridden(): Boolean = overrideRaw() != null

    /** The compiled-in address, shown as the value that "Reset" returns to. */
    fun compiledAddress(): String = BuildConfig.BASE_URL

    /** Parsed override, or null when there is none or it no longer parses. */
    fun overrideUrl(): HttpUrl? = overrideRaw()?.toHttpUrlOrNull()

    /**
     * Stores an override, or clears it when [value] is blank. Returns false and stores nothing if
     * the text is not a usable http(s) URL, so a typo cannot silently cut the tablet off from the
     * server with no way back.
     */
    fun save(value: String?): Boolean = when (val parsed = parse(value)) {
        is Parsed.Clear -> {
            prefs.edit().remove(KEY_BASE_URL).apply()
            true
        }
        is Parsed.Valid -> {
            prefs.edit().putString(KEY_BASE_URL, parsed.url).apply()
            true
        }
        is Parsed.Invalid -> false
    }

    private fun overrideRaw(): String? = prefs.getString(KEY_BASE_URL, null)?.takeIf { it.isNotBlank() }

    /** Outcome of reading what someone typed into the setting. */
    sealed interface Parsed {
        /** Blank: remove the override and go back to the compiled-in address. */
        data object Clear : Parsed
        /** Usable, normalised to a full URL. */
        data class Valid(val url: String) : Parsed
        /** Not a usable address; store nothing and tell the person. */
        data object Invalid : Parsed
    }

    companion object {
        const val PREFS_NAME = "tannous_server_prefs"
        const val KEY_BASE_URL = "server_base_url"

        /**
         * Reads what someone typed at a till. Bare host and host:port are accepted and assumed to
         * be http, because that is what a restaurant LAN address looks like and nobody should have
         * to type a scheme to fix a broken POS. Anything unusable is rejected rather than stored:
         * a saved typo would cut the tablet off from the server with the setting screen as the only
         * way back.
         */
        fun parse(value: String?): Parsed {
            val trimmed = value?.trim().orEmpty()
            if (trimmed.isEmpty()) return Parsed.Clear

            val withScheme =
                if (trimmed.startsWith("http://", ignoreCase = true) ||
                    trimmed.startsWith("https://", ignoreCase = true)
                ) trimmed else "http://" + trimmed

            val parsed = withScheme.toHttpUrlOrNull() ?: return Parsed.Invalid
            if (parsed.host.isBlank()) return Parsed.Invalid
            return Parsed.Valid(parsed.toString())
        }
    }
}
