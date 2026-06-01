# Tannous POS - End-to-End Test Report
**Date:** 2025-01-27  
**Tester:** QA/Dev Engineer  
**Status:** ⚠️ BUILD FAILURES DETECTED - Testing Blocked

---

## PART 0: BUILD VERIFICATION

### Android Build
**Status:** ❌ FAIL  
**Command:** `./gradlew assembleDebug --stacktrace`  
**Error:** 
```
Could not create an instance of type org.jetbrains.kotlin.gradle.plugin.mpp.KotlinAndroidTarget
Caused by: java.lang.NoClassDefFoundError: com/android/build/gradle/api/BaseVariant
```

**Root Cause:** Version incompatibility between Kotlin plugin (1.9.22) and Android Gradle Plugin (8.2.2). The Kotlin plugin version is too old for AGP 8.2.2.

**Minimal Fix Required:**
- Update Kotlin version in `mobile/gradle/libs.versions.toml` from `1.9.22` to `2.0.0` or compatible version
- Or downgrade AGP to `8.1.0` if Kotlin 1.9.22 must be kept

**Files to Change:**
- `mobile/gradle/libs.versions.toml` (line 3: `kotlin = "2.0.0"` or compatible)

---

### Backend Build
**Status:** ❌ FAIL  
**Command:** `dotnet build`  
**Error:**
```
error CS0234: The type or namespace name 'EntityFrameworkCore' does not exist
error CS0234: The type or namespace name 'Infrastructure' does not exist
error CS0246: The type or namespace name 'PosDbContext' could not be found
```

**Root Cause:** 
1. Missing package references in `Tannous.Pos.Application.csproj`:
   - `Microsoft.EntityFrameworkCore`
   - `Microsoft.Extensions.Logging.Abstractions`
2. Architectural issue: `FinalizeOrderCommandHandler` directly uses `PosDbContext` from Infrastructure, but Application cannot reference Infrastructure (circular dependency: Infrastructure → Application → Infrastructure).

**Minimal Fixes Applied:**
- ✅ Added `Microsoft.EntityFrameworkCore` package reference
- ✅ Added `Microsoft.Extensions.Logging.Abstractions` package reference
- ❌ Cannot add Infrastructure project reference (circular dependency)

**Remaining Issue:**
The handler uses `PosDbContext` directly (line 25, 33, 52, 206, 268, 288). This violates clean architecture but is already in the codebase.

**Options:**
1. **Quick Fix (Hacky):** Change `PosDbContext` to `DbContext` in handler, lose type safety
2. **Proper Fix:** Refactor to use `IUnitOfWork` interface + repository pattern (more work)
3. **Accept:** Document as known architectural debt, proceed with testing if backend is already running

**Files Needing Changes:**
- `Tannous.Pos.Application/Orders/Commands/FinalizeOrder/FinalizeOrderCommandHandler.cs` (remove `using Tannous.Pos.Infrastructure.Data;`, change `PosDbContext` to `DbContext`)

---

## PART 1: BACKEND API TESTS (Assumes Backend Running)

### Environment Setup
**Base URL:** `http://localhost:5000` (or configured port)  
**API Version:** `v1.0`  
**Full Base:** `http://localhost:5000/api/v1.0`

**Required Environment Variables:**
- `DB_CONNECTION_STRING` - PostgreSQL connection string
- `JWT__Key` - JWT signing key (min 32 chars)
- `JWT__Issuer` - JWT issuer (default: TannousPOS)
- `JWT__Audience` - JWT audience (default: TannousPOS)
- `SEED_ADMIN_EMAIL` - (Dev only) Admin user email for seeding
- `SEED_ADMIN_PASSWORD` - (Dev only) Admin user password
- `SEED_ADMIN_FIRSTNAME` - (Dev only) Admin first name
- `SEED_ADMIN_LASTNAME` - (Dev only) Admin last name

---

### Test Checklist

| Test ID | Test Case | Expected | Actual | Status | Evidence |
|---------|-----------|----------|--------|--------|----------|
| **PART 1A: Health/Swagger** |
| 1A.1 | GET /swagger | 200, Swagger UI loads | ⏸️ NOT TESTED | Build failure blocks testing |
| 1A.2 | GET /health/ready | 200, `{"status":"Healthy"}` | ⏸️ NOT TESTED | Build failure blocks testing |
| 1A.3 | GET /health/live | 200 | ⏸️ NOT TESTED | Build failure blocks testing |
| **PART 1B: Authentication** |
| 1B.1 | POST /api/v1.0/auth/login (valid) | 200, `{accessToken, refreshToken}` | ⏸️ NOT TESTED | Build failure blocks testing |
| 1B.2 | POST /api/v1.0/auth/login (invalid) | 401 | ⏸️ NOT TESTED | Build failure blocks testing |
| 1B.3 | GET /api/v1.0/auth/profile (no token) | 401 | ⏸️ NOT TESTED | Build failure blocks testing |
| 1B.4 | GET /api/v1.0/auth/profile (valid token) | 200, user data | ⏸️ NOT TESTED | Build failure blocks testing |
| 1B.5 | POST /api/v1.0/auth/refresh | 200, new tokens | ⏸️ NOT TESTED | Build failure blocks testing |
| 1B.6 | POST /api/v1.0/auth/logout | 200 | ⏸️ NOT TESTED | Build failure blocks testing |
| 1B.7 | POST /api/v1.0/auth/refresh (after logout) | 401/403 | ⏸️ NOT TESTED | Build failure blocks testing |
| **PART 1C: User Management (Policy Checks)** |
| 1C.1 | POST /api/v1.0/users (as Owner) | 201, user created | ⏸️ NOT TESTED | Build failure blocks testing |
| 1C.2 | POST /api/v1.0/users (as Cashier) | 403 Forbidden | ⏸️ NOT TESTED | Build failure blocks testing |
| 1C.3 | GET /api/v1.0/users (as Manager) | 200 or 403 (verify policy) | ⏸️ NOT TESTED | Build failure blocks testing |
| **PART 1D: Shifts (Option A: Cashier Allowed)** |
| 1D.1 | POST /api/v1.0/shifts/open (as Cashier) | 200, shift created | ⏸️ NOT TESTED | Build failure blocks testing |
| 1D.2 | GET /api/v1.0/shifts/active (as Cashier) | 200, active shift | ⏸️ NOT TESTED | Build failure blocks testing |
| 1D.3 | POST /api/v1.0/shifts/{id}/close (as Cashier) | 200, shift closed | ⏸️ NOT TESTED | Build failure blocks testing |
| **PART 1E: Orders/Finalization** |
| 1E.1 | POST /api/v1.0/orders (create order) | 201, order created | ⏸️ NOT TESTED | Build failure blocks testing |
| 1E.2 | POST /api/v1.0/orders/{id}/finalize | 200, order finalized | ⏸️ NOT TESTED | Build failure blocks testing |
| 1E.3 | POST /api/v1.0/orders/{id}/finalize (idempotent - same order) | 200, same receipt number | ⏸️ NOT TESTED | Build failure blocks testing |
| 1E.4 | POST /api/v1.0/orders/{id}/finalize (validation failure) | 400/500, transaction rollback | ⏸️ NOT TESTED | Build failure blocks testing |
| **PART 1F: Inventory Deduction** |
| 1F.1 | Verify InventoryMovement created after finalize | MovementType.Sale exists | ⏸️ NOT TESTED | Build failure blocks testing |
| 1F.2 | Verify stock decreased | Stock reduced by recipe quantities | ⏸️ NOT TESTED | Build failure blocks testing |
| 1F.3 | Finalize with insufficient stock | 200, negative stock allowed | ⏸️ NOT TESTED | Build failure blocks testing |

---

## PART 2: DATABASE VERIFICATION (SQL Queries)

**Database:** PostgreSQL  
**Connection:** As per `DB_CONNECTION_STRING`

### Verification Queries

```sql
-- 1. Users table structure and indexes
SELECT 
    column_name, 
    data_type, 
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'Users'
ORDER BY ordinal_position;

SELECT 
    indexname, 
    indexdef
FROM pg_indexes
WHERE tablename = 'Users';

-- 2. RefreshTokens - verify rotation/revocation
SELECT 
    token_hash,
    user_id,
    expires_at,
    revoked_at,
    created_at
FROM "RefreshTokens"
WHERE user_id = '<test_user_id>'
ORDER BY created_at DESC
LIMIT 5;

-- 3. Orders - verify finalization state
SELECT 
    id,
    order_number,
    status,
    receipt_number,
    total_amount,
    created_at,
    closed_at
FROM "Orders"
WHERE id = '<test_order_id>';

-- 4. Payments - verify creation
SELECT 
    id,
    order_id,
    amount,
    payment_method,
    created_at
FROM "Payments"
WHERE order_id = '<test_order_id>';

-- 5. InventoryMovements - verify deduction
SELECT 
    id,
    ingredient_id,
    movement_type,
    quantity,
    reference,
    movement_date
FROM "InventoryMovements"
WHERE reference LIKE 'Order-%'
ORDER BY movement_date DESC
LIMIT 10;

-- 6. InventoryItems - verify stock changes
SELECT 
    id,
    ingredient_id,
    current_stock,
    last_updated
FROM "InventoryItems"
WHERE ingredient_id IN (
    SELECT DISTINCT ingredient_id 
    FROM "InventoryMovements" 
    WHERE reference LIKE 'Order-%'
    ORDER BY movement_date DESC
    LIMIT 5
);
```

**Status:** ⏸️ NOT TESTED (Build failure blocks database access verification)

---

## PART 3: ANDROID MANUAL TEST PLAN

### Prerequisites
- Android emulator running
- Backend API running at `http://10.0.2.2:7000/api/v1.0/` (emulator maps 10.0.2.2 to host localhost)
- APK installed (requires successful build)

### Test Steps

| Step | Action | Expected Result | Status |
|------|--------|----------------|--------|
| 1 | Launch app | App opens, login screen shown | ⏸️ BLOCKED (Build failure) |
| 2 | Login with valid credentials | Navigate to main screen | ⏸️ BLOCKED |
| 3 | Kill app, reopen | Still logged in (token persisted) | ⏸️ BLOCKED |
| 4 | Open shift (as Cashier) | Shift opened successfully | ⏸️ BLOCKED |
| 5 | Sell: Add items to cart | Items appear in cart | ⏸️ BLOCKED |
| 6 | Finalize order with cash payment | Receipt screen shown | ⏸️ BLOCKED |
| 7 | Tap Print button | System print preview opens | ⏸️ BLOCKED |
| 8 | Tap Share button | Share sheet opens | ⏸️ BLOCKED |
| 9 | Corrupt stored access token (Database Inspector) | Token invalid | ⏸️ BLOCKED |
| 10 | Call protected endpoint | Auto-refresh triggers, retry succeeds | ⏸️ BLOCKED |
| 11 | Disable emulator network | Network unavailable | ⏸️ BLOCKED |
| 12 | Finalize order offline | Order queued to outbox | ⏸️ BLOCKED |
| 13 | Re-enable network, trigger sync | Order appears in backend | ⏸️ BLOCKED |

---

## BUGS FOUND

### Critical (Blocks Testing)
1. **Android Build Failure**
   - **Severity:** Critical
   - **Repro:** Run `./gradlew assembleDebug`
   - **Root Cause:** Kotlin 1.9.22 incompatible with AGP 8.2.2
   - **Fix:** Update Kotlin to 2.0.0+ in `mobile/gradle/libs.versions.toml`

2. **Backend Build Failure - Circular Dependency**
   - **Severity:** Critical
   - **Repro:** Run `dotnet build`
   - **Root Cause:** Application references Infrastructure (circular: Infrastructure → Application → Infrastructure)
   - **Fix:** Refactor `FinalizeOrderCommandHandler` to use `IUnitOfWork` + repositories instead of `PosDbContext` directly

### High (Architectural Debt)
3. **Direct DbContext Usage in Application Layer**
   - **Severity:** High (Architectural)
   - **Location:** `FinalizeOrderCommandHandler.cs`
   - **Issue:** Violates clean architecture (Application should not know about Infrastructure)
   - **Impact:** Creates circular dependency, makes testing harder
   - **Fix:** Use `IUnitOfWork` interface + repository pattern

---

## NEXT ACTIONS (Priority Order)

1. **Fix Android Build** ⚠️ CRITICAL
   - Update Kotlin version in `mobile/gradle/libs.versions.toml`
   - Run `./gradlew assembleDebug` to verify
   - **Estimated Time:** 5 minutes

2. **Fix Backend Build** ⚠️ CRITICAL
   - Option A (Quick): Change `PosDbContext` to `DbContext` in handler, remove Infrastructure using
   - Option B (Proper): Refactor to use `IUnitOfWork` + repositories
   - Run `dotnet build` to verify
   - **Estimated Time:** 15-30 minutes (depending on approach)

3. **Start Backend API**
   - Ensure PostgreSQL is running
   - Set environment variables
   - Run `dotnet run --project Tannous.Pos.WebApi`
   - Verify health endpoint responds

4. **Execute Backend API Tests (PART 1)**
   - Use curl/Postman to test all endpoints
   - Document actual vs expected results
   - Capture request/response examples

5. **Execute Database Verification (PART 2)**
   - Connect to PostgreSQL
   - Run verification queries
   - Document findings

6. **Build and Install Android APK**
   - After build fix, generate APK
   - Install on emulator
   - Verify app launches

7. **Execute Android Manual Tests (PART 3)**
   - Follow test plan step-by-step
   - Document results with screenshots
   - Test offline scenarios

8. **Generate Final Test Report**
   - Update checklist with actual results
   - Document any additional bugs found
   - Provide pass/fail summary

---

## SUMMARY

**Current Status:** 🔴 **BLOCKED - Build Failures**

- **Android:** ❌ Build fails (Kotlin/AGP version mismatch)
- **Backend:** ❌ Build fails (circular dependency)
- **API Tests:** ⏸️ Cannot execute (backend not built)
- **Android Tests:** ⏸️ Cannot execute (APK not built)
- **Database Tests:** ⏸️ Cannot verify (backend not running)

**Blockers:**
1. Android Gradle plugin compatibility issue
2. Backend circular dependency architectural issue

**Recommendation:**
Fix build issues first (estimated 20-35 minutes), then proceed with full test execution. The fixes are minimal and well-defined.

---

**Report Generated:** 2025-01-27  
**Next Review:** After build fixes applied

