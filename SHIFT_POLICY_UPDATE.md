# CanManageShifts Policy Update - Cashier Access

## Summary

The `CanManageShifts` policy has been updated to include **Cashier** role, allowing cashiers to open and close shifts.

## Changes Made

1. **AuthorizationExtensions.cs**: Updated `CanManageShifts` policy to include `RoleConstants.Cashier`
2. **AUTHORIZATION_POLICIES.md**: Updated documentation to reflect Cashier access

## Endpoints Now Accessible to Cashier

All endpoints in `ShiftsController` now allow Cashier access:

- `POST /api/shifts/open` - Open a new shift
- `GET /api/shifts/current` - Get the current open shift for the user
- `GET /api/shifts` - Get all shifts (with optional date filters)
- `POST /api/shifts/{id}/close` - Close a shift
- `POST /api/shifts/{id}/cash-drop` - Record a cash drop
- `POST /api/shifts/cashdrawer/kick` - Trigger cash drawer kick event

## Rationale

Cashiers need to be able to:
- Open shifts at the start of their workday
- Close shifts at the end of their workday
- Manage cash drawer operations (cash drops, drawer events)
- View their current shift status

This change enables cashiers to perform essential shift management operations without requiring Manager or Owner intervention.


