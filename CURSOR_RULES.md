# Cursor Rules (Repository Consistency Contract)

These rules define how future AI-generated code must align with existing patterns in this codebase.

## 1) Architecture Rules

- Keep layered boundaries: `WebApi` (transport) -> `Application` (use cases) -> `Domain` (core).
- `Infrastructure` implements persistence/external concerns and may reference `Application` + `Domain`.
- Prefer CQRS + MediatR handlers for business use cases; do not put non-trivial business logic in controllers.
- Do not introduce new architectural styles unless already present in the same bounded context.

## 2) Dependency Rules

- Domain must not depend on WebApi/Infrastructure.
- Application must not depend on WebApi.
- Controllers should depend on MediatR/application contracts, not directly on EF `DbContext` for new work.
- Cross-layer access must follow existing project references and DI wiring in `Program.cs`.

## 3) Folder Placement Rules

- `Domain`: entities, enums, core interfaces.
- `Application`: commands/queries/handlers, validators, DTOs, behaviors.
- `Infrastructure`: EF context/migrations, repositories, transactional/data services, auth implementations.
- `WebApi`: controllers, middleware, filters, API-specific constants/extensions.
- New files must be placed in the matching layer and feature folder, not in generic misc folders.

## 4) Naming Rules

- Use `PascalCase` for types/methods/properties; `camelCase` for locals/parameters.
- Suffix patterns must stay consistent:
  - Commands: `*Command`
  - Queries: `*Query`
  - Handlers: `*Handler`
  - Validators: `*Validator`
  - DTOs: `*Dto`
  - Interfaces: `I*`
- Async methods must end with `Async`.

## 5) Controller Rules

- Keep controllers thin: parse transport concerns, call use case, return HTTP response.
- Use versioned routing convention consistently for new endpoints when versioning is enabled.
- Use authorization attributes with established policy/role constants.
- Avoid duplicating header validation logic when shared filters/middleware already exist.
- Before removing `using Tannous.Pos.Domain.Interfaces;` from a controller during a repository/DbContext migration, verify `IIdempotencyStore` and `IDeviceValidator` are not still constructor parameters. Both interfaces live in `Domain.Interfaces`, so the using survives a repository removal whenever mutation endpoints (which need idempotency/device validation) are retained.

## 6) DTO and Mapping Rules

- Use DTOs for API contracts and application boundaries.
- Do not expose EF entities directly from API endpoints.
- Follow existing mapping style in the touched area (currently mostly manual mapping).
- If introducing centralized mapping, do it consistently (not partially per file).

## 7) Service Rules

- Place domain orchestration in Application handlers/services, infra concerns in Infrastructure services.
- Keep service interfaces in appropriate abstraction layer and implementations in Infrastructure where relevant.
- Register services explicitly in DI composition root.

## 8) Async Rules

- Use async end-to-end for I/O operations.
- Avoid fake async wrappers around synchronous query execution for new code.
- Pass cancellation tokens through to EF/HTTP calls when available.

## 9) Validation Rules

- Use FluentValidation validators for request models/commands.
- Keep validation close to use cases (Application layer).
- Reserve controller-level validation for transport-specific concerns (headers, route/body shape).

## 10) Error Handling Rules

- Return consistent status codes and error payloads for equivalent failure categories.
- Prefer centralized exception-to-response handling for cross-cutting exceptions.
- Avoid one-off ad-hoc error object shapes unless endpoint-specific contract requires it.

## 11) Persistence Rules

- Use EF Core conventions already established in `PosDbContext`.
- Repository interfaces stay in Domain; implementations in Infrastructure.
- Use UnitOfWork/transactions for multi-step write operations requiring atomicity.
- Keep migration and schema changes in Infrastructure migration flow.
- **NEVER hand-write EF migration `.cs` files.** Migrations must be generated with
  `dotnet ef migrations add <Name>` so the `.Designer.cs` and `PosDbContextModelSnapshot.cs`
  stay in sync. Hand-written migrations (Steps 87–110) caused months of snapshot drift that
  broke the first generated migration in Aug 2026. When a step requires schema changes,
  modify the entities + `PosDbContext`, then instruct the human to run the EF CLI command —
  do not emit a migration file. A migration commit without a Designer file and a snapshot
  update must be rejected in review. See MIGRATION_SETUP.md.

## 12) Security Rules

- Keep JWT/auth settings externalized in configuration/environment variables.
- Use role/policy constants and extension registration; do not hardcode role strings inline.
- Keep BCrypt password hashing for credential storage paths.
- Maintain rate limiting/idempotency/device validation patterns for write-sensitive endpoints.
- Ensure CORS policy strictness is environment-appropriate (avoid permissive production defaults).

## 13) Logging and Observability Rules

- Use Serilog conventions already configured.
- Preserve correlation-id propagation and request enrichment middleware behavior.
- Log security/business-critical events at appropriate levels without leaking sensitive payloads.

## 14) Code Style Rules

- Follow nullable reference type conventions and explicit null checks where needed.
- Keep methods focused and class responsibilities single-purpose within each layer.
- Prefer constructor injection for dependencies.
- Add comments only when logic is non-obvious; avoid redundant comments.

## 15) Test Alignment Rules

- Keep tests aligned with current API contracts, enum names, and target framework versions.
- Add/adjust integration tests when changing endpoint behavior, auth, rate limiting, or transaction-sensitive flows.

## 16) GitHub Push Discipline

After every step that is **validated and confirmed green** (build passes, architecture tests pass, integration/compile checks done), immediately push to GitHub before starting the next step. No exceptions — an untracked working state between steps makes bisecting regressions much harder.

**Required commit message format:**

    <type>(<scope>): <short description> — Step <N>

    <body: what changed and why, one line per logical change>
    <deviations: any prompt deviations and the reason>
    <validation: what was verified (build, tests, compile tasks)>

Types: `feat` (new capability), `fix` (bug), `refactor` (restructure), `chore` (housekeeping/docs).
Scope: layer or module (e.g. `android`, `backend`, `sync`, `catalog`, `shifts`).

**Standard push sequence after a validated step:**

    git add -A
    git status --short          # review what is staged
    git commit -m "<message>"
    git push origin main

**Rules:**
- Never batch multiple unvalidated steps into one commit.
- If a step was validated in stages (e.g. backend green → Android green), a single commit after both are green is fine.
- If a step introduced a deviation from the prompt, document it in the commit body.
- Tag release-candidate commits: `git tag -a rc/<step-N> -m "Step <N> validated"` when both backend and Android are fully green together.
- Do not force-push `main` — if a commit must be amended, use `git revert` + a new commit.

