# Order Finalization Transaction Testing Guide

This document describes manual testing steps to verify that order finalization is fully transactional and safe.

## Overview

The `FinalizeOrderCommandHandler` has been updated to use explicit EF Core transactions to ensure atomicity. All operations (order status update, payment creation, receipt number assignment, and future inventory movements) either all commit together or nothing commits.

## Transaction Safety Features

1. **Explicit Transaction Wrapping**: All operations are wrapped in `BeginTransactionAsync()` / `CommitAsync()` / `RollbackAsync()`
2. **Idempotency**: If an order is already finalized (status = Paid), the handler returns the existing state without error
3. **Error Handling**: Any exception triggers automatic rollback to prevent partial state
4. **Structured Logging**: All transaction lifecycle events are logged with structured data

## Manual Test Steps

### Test 1: Successful Finalization (Happy Path)

**Objective**: Verify that a normal finalization commits all changes atomically.

**Steps**:
1. Create an order via `POST /api/v1.0/orders` with at least one order line
2. Finalize the order via `POST /api/v1.0/orders/{id}/finalize` with sufficient payment
3. Verify in database:
   - Order status is `Paid`
   - Order has a `ReceiptNumber` assigned
   - Order has `ClosedAt` timestamp
   - Payments are created and linked to the order
   - All changes are persisted

**Expected Result**: All changes are committed atomically. Order is fully finalized.

---

### Test 2: Idempotency - Duplicate Finalize Request

**Objective**: Verify that finalizing an already-finalized order returns existing state without error.

**Steps**:
1. Create and finalize an order (use same idempotency key)
2. Attempt to finalize the same order again with the same idempotency key
3. Verify:
   - API returns 200 OK (not an error)
   - Response contains the same order data as first finalization
   - Database shows order is still `Paid` (not changed)
   - No duplicate payments created
   - Receipt number unchanged

**Expected Result**: Handler detects order is already finalized and returns existing state. No duplicate operations.

---

### Test 3: Transaction Rollback on Payment Validation Failure

**Objective**: Verify that insufficient payment triggers rollback of all changes.

**Steps**:
1. Create an order with total amount > $10 (e.g., order with $20 total)
2. Attempt to finalize with insufficient payment (e.g., $1.00)
3. Verify in database:
   - Order status is still `Open` (not `Paid`)
   - Order has NO `ReceiptNumber` assigned
   - Order has NO `ClosedAt` timestamp
   - NO payments are created
   - Receipt sequence number was NOT incremented (if using sequence)

**Expected Result**: All changes are rolled back. Order remains in original state.

**Note**: This test verifies that the transaction rollback prevents partial state even when payment validation fails after other operations have been performed.

---

### Test 4: Transaction Rollback on Database Error

**Objective**: Verify that database errors trigger rollback.

**Steps** (requires DEBUG build or controlled failure):
1. Create an order
2. Set up a scenario that will cause a database error during finalization:
   - Option A: Temporarily modify handler to throw exception after creating payments but before SaveChanges
   - Option B: Use database constraints (e.g., invalid foreign key, unique constraint violation)
3. Attempt to finalize the order
4. Verify in database:
   - Order status is still `Open`
   - NO payments are created
   - NO receipt number assigned
   - All changes rolled back

**Expected Result**: Transaction rolls back completely. No partial state.

**Implementation Note**: To test this, you can temporarily add a `#if DEBUG` block in the handler:
```csharp
#if DEBUG
if (Environment.GetEnvironmentVariable("SIMULATE_FAILURE") == "true")
{
    throw new InvalidOperationException("Simulated failure for testing");
}
#endif
```

Then set environment variable `SIMULATE_FAILURE=true` before running the test.

---

### Test 5: Concurrent Finalization Attempts

**Objective**: Verify that concurrent finalization attempts are handled correctly.

**Steps**:
1. Create an order
2. Send two finalize requests simultaneously (same order, different idempotency keys)
3. Verify:
   - One request succeeds (200 OK)
   - One request either:
     - Returns 200 OK with same result (if idempotency check happens first)
     - Returns error indicating order is not in Open status (if status check happens first)
   - Database shows order is finalized exactly once
   - Only one set of payments exists
   - Only one receipt number assigned

**Expected Result**: Database constraints and transaction isolation prevent double-finalization. One succeeds, one fails gracefully.

---

### Test 6: Receipt Number Generation Within Transaction

**Objective**: Verify that receipt number generation is part of the transaction.

**Steps**:
1. Note the current receipt sequence number
2. Create and finalize an order
3. Verify receipt number was generated and assigned
4. Simulate a failure before transaction commit (see Test 4)
5. Verify receipt sequence was NOT incremented (rolled back)

**Expected Result**: Receipt number generation is transactional. If finalization fails, sequence is not consumed.

---

## Verification Queries

Use these SQL queries to verify transaction behavior:

```sql
-- Check order status and related data
SELECT 
    o.Id,
    o.Status,
    o.ReceiptNumber,
    o.ClosedAt,
    o.TotalAmount,
    COUNT(p.Id) as PaymentCount,
    SUM(p.Amount) as TotalPayments
FROM "Orders" o
LEFT JOIN "Payments" p ON p."OrderId" = o.Id
WHERE o.Id = '<ORDER_ID>'
GROUP BY o.Id, o.Status, o.ReceiptNumber, o.ClosedAt, o.TotalAmount;

-- Check receipt sequence (if using sequences)
SELECT "SequenceType", "CurrentNumber", "NextNumber", "LastUsed"
FROM "ReceiptSequences"
WHERE "SequenceType" = 'Receipt';

-- Check for orphaned payments (should be 0)
SELECT COUNT(*) as OrphanedPayments
FROM "Payments" p
LEFT JOIN "Orders" o ON o.Id = p."OrderId"
WHERE o.Id IS NULL;
```

## Logging Verification

Check application logs for structured log entries:

1. **Transaction Start**: 
   ```
   Starting order finalization transaction. OrderId: {OrderId}, IdempotencyKey: {IdempotencyKey}
   ```

2. **Idempotency Detection**:
   ```
   Order already finalized. Returning existing state. OrderId: {OrderId}, ReceiptNumber: {ReceiptNumber}
   ```

3. **Transaction Success**:
   ```
   Order finalization completed successfully. OrderId: {OrderId}, ReceiptNumber: {ReceiptNumber}, TotalAmount: {TotalAmount}
   ```

4. **Transaction Rollback**:
   ```
   Error during order finalization. Rolling back transaction. OrderId: {OrderId}
   Transaction rolled back successfully. OrderId: {OrderId}
   ```

## Integration Tests

Automated integration tests are available in `Tannous.Pos.Integration/OrderFinalizationTests.cs`:

- `OrderFinalization_ShouldBeIdempotent_WhenOrderAlreadyFinalized`: Tests idempotency
- `OrderFinalization_ShouldRollback_OnPaymentValidationFailure`: Tests rollback on validation failure

Run tests with:
```bash
dotnet test Tannous.Pos.Integration
```

## Notes

- All database operations within the transaction use the same `PosDbContext` instance
- The `ReceiptNumberService` also uses the same context, so its operations are part of the transaction
- Idempotency is handled at two levels:
  1. Handler level: Checks if order is already `Paid` and returns existing state
  2. Controller level: Uses idempotency key to return cached response for duplicate requests
- Transaction isolation level is database default (typically READ COMMITTED for PostgreSQL)


