# Authorization Policies

This document describes the authorization policies implemented in the Tannous POS backend API.

## Overview

The Tannous POS backend uses policy-based authorization to control access to API endpoints. Policies are defined centrally and applied consistently across all controllers.

## Roles

The system defines the following roles (matching the `Role` enum in `Tannous.Pos.Domain.Enums`):

- **Owner**: Full administrative access. Acts as the system administrator.
- **Manager**: Management-level access. Can manage operations but not users or system settings.
- **Cashier**: Point-of-sale operations. Can process sales and manage customer records.
- **Kitchen**: Kitchen staff (currently not used in policies).
- **Waiter**: Waiter staff (currently not used in policies).

## Policies

### CanSell
**Allowed Roles:** Owner, Manager, Cashier

**Purpose:** Allows users to process sales transactions and access point-of-sale functionality.

**Applied To:**
- OrdersController (all endpoints)
- CatalogController (read endpoints: GET categories, menu-items, addons)
- PrintingController (all endpoints)
- SyncController (all endpoints)
- SettingsController (GET settings)

### CanManageShifts
**Allowed Roles:** Owner, Manager, Cashier

**Purpose:** Allows users to open/close shifts, manage cash drawers, and perform shift-related operations.

**Applied To:**
- ShiftsController (all endpoints)

### CanManageCatalog
**Allowed Roles:** Owner, Manager

**Purpose:** Allows users to create, update, and delete catalog items (categories, menu items, add-ons).

**Applied To:**
- CatalogController (mutation endpoints: POST, PUT, DELETE categories, menu-items, addons)

### CanManageCustomers
**Allowed Roles:** Owner, Manager, Cashier

**Purpose:** Allows users to create, update, and view customer records.

**Applied To:**
- CustomersController (all endpoints)

### CanViewReports
**Allowed Roles:** Owner, Manager

**Purpose:** Allows users to view business reports and analytics.

**Applied To:**
- ReportsController (all endpoints)

### CanManageUsers
**Allowed Roles:** Owner

**Purpose:** Allows users to create, update, delete, and manage user accounts. Also used for administrative functions.

**Applied To:**
- UsersController (all endpoints)
- InventoryController (all endpoints)
- SuppliersController (all endpoints)
- DevicesController (POST register)
- AdminController (all endpoints)

### CanManageSettings
**Allowed Roles:** Owner

**Purpose:** Allows users to modify business settings and configuration.

**Applied To:**
- SettingsController (PUT settings)

### CanViewReportsOrApiKey
**Allowed:** Owner, Manager (JWT) — OR a valid, active, non-expired third-party API key presented
via the `X-Api-Key` header.

**Purpose:** Lets a third-party integrator (e.g. an accounting export tool, a franchise reporting
dashboard) pull business reports without a staff login, while staff access is completely
unchanged. Implemented as `RequireAssertion` in `AuthorizationExtensions`, not a role — API keys
authenticate as role `ApiIntegrator`, which no write policy recognizes.

**Applied To:**
- ReportsController (all endpoints — every action here is a read-only query, so applying this at
  the controller level is safe; there's no write action that could inherit it by accident)

**Authentication schemes required:** `Bearer,ApiKey` (both must be listed on the `[Authorize]`
attribute, or the ApiKey handler never gets a chance to run for that request)

### CanViewCustomersOrApiKey
**Allowed:** Owner, Manager, Cashier (JWT) — OR a valid API key.

**Purpose:** Same idea as above, scoped to customer records. Deliberately **not** applied at the
controller level, because `CustomersController` mixes reads and writes — ASP.NET Core combines
class- and method-level `[Authorize]` attributes with logical AND, so a looser class-level policy
here could never be tightened back up per-action. Instead:

**Applied To:**
- `GET /customers`, `GET /customers/{id}` only

**Explicitly NOT applied to:** `POST /customers`, `PUT /customers/{id}`,
`PUT /customers/orders/{orderId}/customer` — these keep requiring `CanManageCustomers` alone, so
an API key can never create, update, or attach a customer.

## Controller-to-Policy Mapping

| Controller | Endpoint(s) | Policy | Notes |
|------------|-------------|--------|-------|
| **AuthController** | POST /login, POST /refresh, POST /logout | None | Public endpoints |
| **AuthController** | GET /profile | `[Authorize]` | Any authenticated user |
| **UsersController** | All endpoints | `CanManageUsers` | Owner only |
| **OrdersController** | All endpoints | `CanSell` | Owner, Manager, Cashier |
| **ShiftsController** | All endpoints | `CanManageShifts` | Owner, Manager, Cashier |
| **CatalogController** | GET categories, menu-items, addons | `CanSell` | Read access for all sellers |
| **CatalogController** | POST/PUT/DELETE categories, menu-items, addons | `CanManageCatalog` | Write access for Owner, Manager |
| **CustomersController** | All endpoints | `CanManageCustomers` | Owner, Manager, Cashier |
| **ReportsController** | All endpoints | `CanViewReports` | Owner, Manager |
| **SettingsController** | GET settings | `CanSell` | Read access for all sellers |
| **SettingsController** | PUT settings | `CanManageSettings` | Owner only |
| **InventoryController** | All endpoints | `CanManageUsers` | Owner only |
| **SuppliersController** | All endpoints | `CanManageUsers` | Owner only |
| **DevicesController** | POST register | `CanManageUsers` | Owner only |
| **PrintingController** | All endpoints | `CanSell` | Owner, Manager, Cashier |
| **SyncController** | All endpoints | `CanSell` | Owner, Manager, Cashier |
| **AdminController** | All endpoints | `CanManageUsers` | Owner only |
| **ReportsController** | All endpoints | `CanViewReportsOrApiKey` | Owner, Manager, OR API key |
| **CustomersController** | GET (list, by id) | `CanViewCustomersOrApiKey` | Owner, Manager, Cashier, OR API key |
| **CustomersController** | POST, PUT | `CanManageCustomers` | Owner, Manager, Cashier only — no API key |

## Third-Party API Keys (X-Api-Key)

Separate from staff JWTs. Created by an Owner via `POST /api/v1/apikeys` (Swagger — no in-app
creation UI yet). The raw key (`tnp_...`) is shown exactly once at creation; only its SHA-256 hash
and an 8-character prefix are persisted (`ApiKey.KeyHash`/`KeyPrefix`).

Presented on a request via the `X-Api-Key` header. `ApiKeyAuthenticationHandler` (registered as the
`"ApiKey"` authentication scheme in `Program.cs`) hashes the presented key, looks it up, checks
`IsActive` and `ExpiresAt`, stamps `LastUsedAt`, and issues a principal with role `ApiIntegrator` —
a role no write policy (`CanManageCustomers`, `CanManageCatalog`, etc.) recognizes, so a key can
never be used to write data, only to satisfy the two `*OrApiKey` policies above.

An endpoint only accepts a key if its `[Authorize]` attribute explicitly lists both schemes, e.g.
`AuthenticationSchemes = "Bearer,ApiKey"` — a plain `[Authorize(Policy = "CanViewReportsOrApiKey")]`
without the scheme list would only ever try the default (Bearer/JWT) scheme.

### Known limitations

- **Branch scoping is not enforced.** `ApiKey.BranchId` is stored, but no query handler
  (`GetCustomersQueryHandler`, the Reports query handlers, etc.) currently filters by it — there's
  no `ICurrentUserService`-style mechanism reading branch claims anywhere in the Application layer
  yet. A key created with a specific `BranchId` still sees all-branch data today. Flag as the next
  step if/when multi-branch data isolation for integrators actually matters.
- **Webhook subscriptions and API keys can only be created via Swagger/raw API calls** — there is
  no in-app (mobile) creation form for either, only view/test/delete of existing ones
  (`IntegrationsScreen.kt`). This is an intentional gap for now (target audience is technical
  integrators), not a bug — revisit if non-technical staff ever need to self-serve these.

## Implementation Details

### Policy Definition

Policies are defined in `Tannous.Pos.WebApi.Extensions.AuthorizationExtensions` using the `AddPosAuthorizationPolicies()` extension method, which is called in `Program.cs`:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPosAuthorizationPolicies();
    // ... legacy policies for backward compatibility
});
```

### Role Constants

Role names are centralized in `Tannous.Pos.WebApi.Constants.RoleConstants` to ensure consistency and avoid hardcoded strings.

### Policy Constants

Policy names are centralized in `Tannous.Pos.WebApi.Constants.PolicyConstants` to ensure consistency across controllers.

### JWT Claims

Roles are included in JWT tokens as claims using `ClaimTypes.Role` (standard claim type: `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`). The role value matches the string representation of the `Role` enum (e.g., "Owner", "Manager", "Cashier").

## Response Codes

- **401 Unauthorized**: Returned when the request is not authenticated (no valid JWT token).
- **403 Forbidden**: Returned when the request is authenticated but the user's role does not satisfy the required policy.

## Public Endpoints

The following endpoints do not require authentication:

- `POST /api/v1.0/auth/login`
- `POST /api/v1.0/auth/refresh`
- `POST /api/v1.0/auth/logout`
- `GET /health/ready` (health check)
- `GET /health/live` (health check)
- Swagger UI (in Development environment)

All other endpoints require authentication and appropriate authorization policies.

## Migration Notes

Legacy policies (`Owner`, `Cashier`, `CashierOrOwner`, `OwnerOnly`, `Admin`, `AdminOrManager`) are still defined for backward compatibility but should not be used in new code. All new code should use the policy constants from `PolicyConstants`.

## Testing

When testing authorization:

1. **Unauthenticated requests** should return 401 Unauthorized.
2. **Authenticated requests with insufficient permissions** should return 403 Forbidden.
3. **Authenticated requests with sufficient permissions** should succeed (200, 201, etc.).

### Example Test Scenarios

- Cashier cannot access `CanManageUsers` endpoints (expect 403)
- Manager can access `CanSell` but not `CanManageUsers` (expect 403 on user management)
- Owner can access all endpoints (expect success)

## Future Enhancements

Potential future policy additions:

- `CanManageInventory`: Separate policy for inventory management (currently uses `CanManageUsers`)
- `CanManageSuppliers`: Separate policy for supplier management (currently uses `CanManageUsers`)
- `CanViewInventory`: Read-only access to inventory data
- Role-specific policies for Kitchen and Waiter roles when those features are implemented

