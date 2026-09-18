package com.tannous.pos.core.data.repository

/**
 * Decides whether a payment may be queued for an order while the device is offline.
 *
 * The rule exists because of a real data-loss path. Offline, an order is created only in the
 * tablet's own database and no CreateOrder operation is queued, so the server never learns it
 * exists. Finalize would then queue a FinalizeOrder pointing at that local id, mark the order PAID,
 * print a receipt with a PENDING receipt number, and report success to the cashier. On replay the
 * server looks the id up, does not find it, and returns a failure that nothing in the app reads.
 * The money is taken, the customer has a receipt, and the sale exists on one tablet only.
 *
 * Refusing is worse for the cashier and better for the restaurant: a sale that cannot be taken is
 * a problem you find out about immediately, and a sale that silently is not recorded is one you
 * find at the end of the month with no way to reconstruct it.
 *
 * This deliberately does not refuse every offline finalize. An order created while the connection
 * was up already has a server id, so queuing its payment is safe and keeps working - which is the
 * common case when Wi-Fi drops between ordering and paying.
 *
 * The proper fix is to queue CreateOrder offline and re-key the local order from the ServerId the
 * push response already returns. The server side of that exists and works (SyncController's
 * ProcessCreateOrder dispatches the real command); only the client half was never written. Until
 * that is built and tested, this guard keeps the failure in front of a person.
 */
object OfflineFinalizeGuard {

    const val ORDER_NOT_ON_SERVER_MESSAGE: String =
        "No connection, and this order was never sent to the server. " +
            "Reconnect and try again before taking payment."

    sealed interface Decision {
        /** Safe: the server knows this order, so a queued payment will apply on replay. */
        data object QueueForSync : Decision

        /** Unsafe: queuing would accept money for an order the server cannot find. */
        data class Refuse(val message: String) : Decision
    }

    fun decide(orderExistsOnServer: Boolean): Decision =
        if (orderExistsOnServer) Decision.QueueForSync
        else Decision.Refuse(ORDER_NOT_ON_SERVER_MESSAGE)
}
