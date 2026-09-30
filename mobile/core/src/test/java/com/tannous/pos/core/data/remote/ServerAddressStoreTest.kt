package com.tannous.pos.core.data.remote

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import okhttp3.HttpUrl.Companion.toHttpUrlOrNull
import org.junit.Test

/**
 * The parsing half of the server address setting, which is the half that can cut a tablet off from
 * the server if it gets this wrong.
 */
class ServerAddressStoreTest {

    private fun valid(input: String): String {
        val parsed = ServerAddressStore.parse(input)
        assertTrue("Expected $input to be usable, got $parsed", parsed is ServerAddressStore.Parsed.Valid)
        return (parsed as ServerAddressStore.Parsed.Valid).url
    }

    @Test
    fun `a bare host and port is assumed to be http`() {
        // What someone actually types when the POS is down and the server moved.
        assertEquals("http://192.168.1.5:7000/", valid("192.168.1.5:7000"))
    }

    @Test
    fun `a full url is kept as given`() {
        assertEquals("http://192.168.10.231:7000/api/v1.0/", valid("http://192.168.10.231:7000/api/v1.0/"))
    }

    @Test
    fun `https is preserved rather than downgraded`() {
        assertTrue(valid("https://pos.example.com").startsWith("https://"))
    }

    @Test
    fun `surrounding whitespace is forgiven`() {
        assertEquals("http://192.168.1.5:7000/", valid("  192.168.1.5:7000  "))
    }

    @Test
    fun `blank clears the override`() {
        assertEquals(ServerAddressStore.Parsed.Clear, ServerAddressStore.parse(""))
        assertEquals(ServerAddressStore.Parsed.Clear, ServerAddressStore.parse("   "))
        assertEquals(ServerAddressStore.Parsed.Clear, ServerAddressStore.parse(null))
    }

    @Test
    fun `text with no host is rejected instead of stored`() {
        // Storing this would point the tablet at nothing, and the setting screen would be the only
        // way back.
        assertEquals(ServerAddressStore.Parsed.Invalid, ServerAddressStore.parse("http://"))
        assertEquals(ServerAddressStore.Parsed.Invalid, ServerAddressStore.parse("http://:7000"))
    }

    @Test
    fun `whatever is accepted always has a host`() {
        // Weaker than listing every bad input, and the property that actually matters: nothing is
        // ever stored that the interceptor cannot route to.
        listOf("192.168.1.5", "192.168.1.5:7000", "https://pos.example.com", "ftp://192.168.1.5", "://x")
            .forEach { input ->
                val parsed = ServerAddressStore.parse(input)
                if (parsed is ServerAddressStore.Parsed.Valid) {
                    assertTrue(
                        "Accepted \"" + input + "\" as " + parsed.url + " with no usable host",
                        parsed.url.toHttpUrlOrNull()?.host?.isNotBlank() == true
                    )
                }
            }
    }
}
