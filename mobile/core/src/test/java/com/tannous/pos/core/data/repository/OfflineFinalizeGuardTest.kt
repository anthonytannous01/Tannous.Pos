package com.tannous.pos.core.data.repository

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Pins the rule that a payment is never queued for an order the server has never seen.
 *
 * The rule lives in a pure function precisely so it can be tested. Its previous form was an
 * implicit assumption inside a catch block in OrderRepository, where it was wrong and nothing
 * could notice.
 */
class OfflineFinalizeGuardTest {

    @Test
    fun `an order the server knows can be paid offline`() {
        // Common case: connection was up when the order was sent, then dropped before payment.
        // The outbox entry references a real server id, so replay applies it.
        assertEquals(
            OfflineFinalizeGuard.Decision.QueueForSync,
            OfflineFinalizeGuard.decide(orderExistsOnServer = true)
        )
    }

    @Test
    fun `an order the server has never seen cannot be paid offline`() {
        val decision = OfflineFinalizeGuard.decide(orderExistsOnServer = false)

        assertTrue(
            "Queuing a payment for an order the server cannot find loses the sale after the " +
                "money has been taken. This must refuse.",
            decision is OfflineFinalizeGuard.Decision.Refuse
        )
    }

    @Test
    fun `the refusal tells the cashier what to do, not what went wrong internally`() {
        val decision = OfflineFinalizeGuard.decide(orderExistsOnServer = false)
        val message = (decision as OfflineFinalizeGuard.Decision.Refuse).message

        // Someone is standing at the till holding cash. "Sync error" is not an instruction.
        assertTrue("Message should name the cause", message.contains("never sent", ignoreCase = true))
        assertTrue("Message should say what to do", message.contains("Reconnect", ignoreCase = true))
        assertTrue(
            "Message should warn before the money is taken",
            message.contains("before taking payment", ignoreCase = true)
        )
    }
}
