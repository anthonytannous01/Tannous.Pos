# Architecture Summary

## High-Level Architecture

This repository uses a **hybrid architecture** centered on a Clean/Layered separation with CQRS patterns:

- `Tannous.Pos.Domain`: core entities, enums, and interfaces (business core)
- `Tannous.Pos.Application`: use cases (commands/queries), validators, DTOs, MediatR behaviors
- `Tannous.Pos.Infrastructure`: EF Core persistence, repository implementations, auth/services, seeders
- `Tannous.Pos.WebApi`: HTTP entrypoint, middleware pipeline, controllers, DI composition root
- `mobile`: separate Android client (modular MVVM architecture)

At the project reference level, dependency direction is mostly inward (WebApi -> Application/Infrastructure -> Domain).  
In implementation, there is a mixed style: many endpoints use CQRS/MediatR as intended, while some controllers access `PosDbContext` directly.

## Responsibility Separation

- **Domain**
  - Defines business model shape and shared contracts (`IRepository<T>`, aggregate-specific interfaces).
  - Stays free from framework concerns.

- **Application**
  - Encapsulates use-case orchestration in MediatR handlers.
  - Performs validation through FluentValidation + pipeline behavior.
  - Returns DTO-focused outputs to caller layers.

- **Infrastructure**
  - Owns data access via EF Core/Npgsql (`PosDbContext`, migrations, repository implementations).
  - Implements operational services (JWT auth, idempotency store, auditing, device validation).
  - Provides Unit of Work and transaction coordination.

- **WebApi**
  - Hosts HTTP surface, authz policies, filters, middleware, rate limiting, health checks.
  - Maps requests to MediatR use cases for many features.
  - Also contains some direct data-access endpoints (architectural inconsistency).

- **Mobile**
  - Android modules (`app`, `core`, `feature/*`) with Hilt DI, Room local DB, Retrofit network layer, WorkManager sync.
  - Follows MVVM and offline-first/outbox patterns in selected flows.

## Main Runtime Flows

## 1) API Request Flow

1. Request enters ASP.NET Core pipeline in `Program.cs`.
2. Cross-cutting middleware executes (HTTPS, CORS, correlation/log enrichment, authn/authz, rate limiting).
3. Controller action runs.
4. Action either:
   - dispatches MediatR command/query to Application handlers, or
   - directly uses `PosDbContext` (in some controllers).
5. Persistence executes through EF Core repositories/context against PostgreSQL.
6. Response is returned with endpoint-specific status and payload shape.

## 2) Validation Flow

- FluentValidation validators run through `ValidationBehavior<TRequest,TResponse>`.
- Some controllers add manual validation (headers/claims/domain checks), causing mixed validation placement.

## 3) Auth Flow

- JWT bearer auth is configured in WebApi.
- Login/refresh handlers call Infrastructure auth service to issue/rotate tokens.
- Role/policy authorization is applied via constants + authorization extension methods.

## 4) Persistence Flow

- EF Core context defines aggregates/relationships/indexes.
- Generic + specialized repositories are used by handlers/services.
- UnitOfWork supports explicit transaction boundaries for multi-step operations.
- Migrations exist under Infrastructure.

## Important Engineering Decisions Present in Code

- **MediatR + CQRS adopted** for substantial use-case handling.
- **FluentValidation pipeline behavior** used for request validation.
- **EF Core with PostgreSQL** chosen as persistence baseline.
- **Repository + UnitOfWork abstractions** used alongside direct DbContext access in some API modules.
- **JWT + role-based policies** for security.
- **Serilog + correlation id + request enrichment** for observability.
- **Rate limiting + Device-Id header checks + idempotency store** for operational safety.
- **API versioning configured**, but not uniformly applied across all controllers.

## API and Contract Conventions Observed

- Controllers typically use `api/[controller]` or versioned routes (`api/v{version:apiVersion}/...`) depending on module.
- DTOs are common in Application and many endpoints.
- Mapping is mostly manual object construction.
- Response/error shape is not fully standardized across endpoints.
- Pagination DTO conventions exist (`PaginatedRequestDto`, `PaginatedResponseDto`) and are used in parts of the API.

## Security Posture Observed

- JWT bearer auth with refresh tokens and BCrypt password hashing.
- Role/policy authorization constants and extension registration.
- Request protection patterns include rate limiting and idempotency support.
- CORS includes a permissive mobile policy (`AllowAnyOrigin`) that should remain environment-controlled.

## Frontend/Mobile Architecture Snapshot

- Kotlin Android app with modular packaging:
  - `core` for shared infra/data/sync
  - `feature/*` for vertical feature UI modules
  - `app` host module
- Uses Hilt, Room, Retrofit, WorkManager, coroutines/Flow.
- Includes offline sync logic patterns (outbox-style repository behavior).

## OCR / OpenCV / Tesseract

No OCR/OpenCV/Tesseract integration was found in the scanned backend or mobile code paths.

