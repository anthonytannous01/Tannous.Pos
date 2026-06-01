# Shift Management Flow

This document describes the shift management implementation in the Tannous POS Android application.

## Overview

Shift management allows Cashiers, Managers, and Owners to open and close shifts, which are required for order finalization. Shift operations require an active internet connection and are not queued for offline sync.

## Architecture

### Backend API

The shift management uses the following endpoints:

- `GET /api/v1.0/shifts/current` - Get the current active shift for the logged-in user
  - Returns `ShiftDto` or `404 Not Found` if no active shift exists
- `POST /api/v1.0/shifts/open` - Open a new shift
  - Request: `OpenShiftRequest { openingBalance: decimal, notes?: string }`
  - Returns: `ShiftDto`
- `POST /api/v1.0/shifts/{id}/close` - Close a shift
  - Request: `CloseShiftRequest { closingCount: decimal, note?: string }`
  - Returns: `ShiftDto`

All endpoints require authentication and the `CanManageShifts` policy (Owner, Manager, Cashier).

### Android Implementation

#### Components

1. **ShiftService** (`mobile/core/src/main/java/com/tannous/pos/core/data/remote/ApiServices.kt`)
   - Retrofit interface for shift API endpoints

2. **ShiftRepository** (`mobile/core/src/main/java/com/tannous/pos/core/data/repository/ShiftRepository.kt`)
   - Handles all shift-related API calls
   - Returns `ShiftDto` from the backend
   - **Offline Behavior**: Shift operations require internet connection. If offline, operations fail with a clear error message. Shifts are NOT queued for offline sync.

3. **ShiftViewModel** (`mobile/feature/shifts/src/main/java/com/tannous/pos/feature/shifts/ShiftViewModel.kt`)
   - Manages shift UI state
   - Handles loading active shift, opening, and closing shifts
   - State: `ShiftUiState(isLoading, activeShift, errorMessage)`

4. **ShiftsScreen** (`mobile/feature/shifts/src/main/java/com/tannous/pos/feature/shifts/ShiftsScreen.kt`)
   - Displays active shift details or "No Active Shift" message
   - Provides "Open Shift" and "Close Shift" buttons with dialogs
   - Shows loading states and error messages via Snackbar

#### Integration with Sell Flow

The Sell screen requires an active shift to finalize orders. `SellViewModel.finalizeOrder()` checks for an active shift before proceeding:

```kotlin
val activeShift = shiftRepository.getActiveShift()
if (activeShift == null) {
    // Shows error: "No active shift. Please open a shift first."
    return@launch
}
```

Users can navigate to the Shifts screen from the Sell screen top bar to open a shift if needed.

## User Flow

### Opening a Shift

1. User navigates to Shifts screen (from Sell screen top bar)
2. If no active shift exists, user sees "No Active Shift" message
3. User clicks "Open Shift" button
4. Dialog appears prompting for:
   - Opening Balance (required)
   - Notes (optional)
5. User enters opening balance and clicks "Open Shift"
6. Shift is created via API
7. Shifts screen updates to show shift details

### Closing a Shift

1. User navigates to Shifts screen
2. If active shift exists, shift details are displayed
3. User clicks "Close Shift" button
4. Dialog appears showing:
   - Expected Cash (read-only)
   - Actual Cash Count (input)
   - Variance (calculated, shown when actual cash is entered)
   - Note (optional)
5. User enters actual cash count and clicks "Close Shift"
6. Shift is closed via API
7. Shifts screen updates to show "No Active Shift"

### Order Finalization with Shift

1. User adds items to cart in Sell screen
2. User clicks "Finalize Order"
3. System checks for active shift
   - If no shift exists: Error message shown, user must open shift first
   - If shift exists: Order finalization proceeds using shift ID

## Testing on Emulator

### Prerequisites

1. Backend API running on `http://localhost:7000`
2. Android emulator configured (uses `http://10.0.2.2:7000/api/v1.0`)
3. User logged in with Cashier, Manager, or Owner role

### Test Steps

#### Test 1: Open Shift

1. Launch app and login
2. Navigate to Sell screen
3. Click shift icon in top bar (navigates to Shifts screen)
4. Verify "No Active Shift" message appears
5. Click "Open Shift" button
6. Enter opening balance (e.g., `100.00`)
7. Optionally add notes
8. Click "Open Shift" in dialog
9. Verify:
   - Loading indicator appears
   - Shift details display showing:
     - Shift Number
     - Opened time
     - Opening Balance
   - Success message (if configured)

#### Test 2: Close Shift

1. From Shifts screen with active shift
2. Click "Close Shift" button
3. Enter actual cash count (e.g., `105.50`)
4. Verify variance calculation shows (e.g., `+5.50`)
5. Optionally add note
6. Click "Close Shift" in dialog
7. Verify:
   - Loading indicator appears
   - "No Active Shift" message appears
   - Success message (if configured)

#### Test 3: Order Finalization Requires Active Shift

1. Ensure no active shift exists
2. Navigate to Sell screen
3. Add items to cart
4. Click "Finalize Order"
5. Verify error message: "No active shift. Please open a shift first."
6. Navigate to Shifts screen
7. Open shift
8. Return to Sell screen
9. Add items to cart and finalize
10. Verify order finalization succeeds

#### Test 4: Offline Behavior

1. Disable network on emulator (Settings > Network & internet > Airplane mode)
2. Navigate to Shifts screen
3. Try to open shift
4. Verify error message: "Shift actions require internet connection. Please check your network and try again."
5. Enable network
6. Try opening shift again
7. Verify shift opens successfully

## API Models

### ShiftDto

```kotlin
data class ShiftDto(
    val id: String,
    val shiftNumber: String,
    val startTime: String,
    val endTime: String?,
    val status: String,
    val openingBalance: BigDecimal,
    val closingBalance: BigDecimal?,
    val expectedCash: BigDecimal?,
    val actualCash: BigDecimal?,
    val cashDifference: BigDecimal?,
    val notes: String?,
    val userId: String,
    val createdAt: String
)
```

### OpenShiftRequest

```kotlin
data class OpenShiftRequest(
    val openingBalance: BigDecimal,
    val notes: String? = null
)
```

### CloseShiftRequest

```kotlin
data class CloseShiftRequest(
    @SerialName("closingCount")
    val closingCount: BigDecimal,
    @SerialName("note")
    val note: String? = null
)
```

## Error Handling

### Network Errors

When network is unavailable:
- `IOException` is caught and converted to user-friendly message
- Message: "Shift actions require internet connection. Please check your network and try again."
- No offline queuing occurs

### API Errors

- `404 Not Found` on `getCurrentShift()`: Expected when no active shift exists (not an error)
- `401 Unauthorized`: User must re-login
- `403 Forbidden`: User doesn't have permission (should not occur if policies are correct)
- `400 Bad Request`: Invalid request data (should not occur with proper validation)

All errors are displayed to the user via Snackbar in the ShiftsScreen.

## Future Enhancements

Potential improvements:
1. Show shift status indicator in Sell screen top bar
2. Add shift history view
3. Add cash drop functionality in UI
4. Show shift summary/reports
5. Add shift timer display

