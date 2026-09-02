# Tannous POS — Architecture Rules

The repository consistency contract. Every change, by anyone, must comply with these rules.
They replace the former `.cursorrules` and `CURSOR_RULES.md`, which were merged here when the
project moved from a prompt-relay workflow to direct implementation in the repository.

---

## 1) Architecture and Dependencies

- Preserve layered flow: `WebApi` (transport) → `Application` (use cases) → `Domain` (core).
  `Infrastructure` implements persistence/external concerns and may reference `Application` + `Domain`.
- `Domain` must not depend on `WebApi` or `Infrastructure`. `Application` must not depend on `WebApi`.
- Use CQRS (MediatR command/query handlers) in `Application` for new backend business logic.
- Controllers depend on MediatR/application contracts, never directly on EF `DbContext` for new work.
- Do not introduce new architectural styles or frameworks unless already established in the touched area.
- Cross-layer access must follow existing project references and DI wiring in `Program.cs`.

## 2) Controller / Service / Repository Responsibilities

- Keep controllers thin and transport-focused: parse HTTP concerns, call the use case, map the result.
- Business orchestration belongs in `Application` handlers/services, not controllers.
- Repository interfaces belong in `Domain`; implementations belong in `Infrastructure`.
- Service interfaces live in the appropriate abstraction layer; implementations in `Infrastructure`.
- Register services explicitly in the DI composition root.
- Use `UnitOfWork`/transactions for multi-step writes requiring atomicity.
- Before removing `using Tannous.Pos.Domain.Interfaces;` from a controller during a repository or
  DbContext migration, verify `IIdempotencyStore` and `IDeviceValidator` are not still constructor
  parameters. Both live in `Domain.Interfaces`, so the using survives a repository removal whenever
  mutation endpoints (which need idempotency/device validation) are retained.

## 3) Folder Placement

- `Domain`: entities, enums, core interfaces.
- `Application`: commands/queries/handlers, validators, DTOs, behaviors.
- `Infrastructure`: EF context/migrations, repositories, transactional/data services, auth implementations.
- `WebApi`: controllers, middleware, filters, API-specific constants/extensions.
- `mobile`: `core` holds shared infrastructure and data; `feature/*` modules hold screens and their
  view models. A feature module must not depend on another feature module — shared behavior moves to `core`.
- New files go in the matching layer and feature folder, never in generic misc folders.

## 4) DTO and API Contracts

- Use DTOs at API/application boundaries; never expose EF entities directly in API responses.
- Keep route/versioning consistent with existing versioned conventions for new and updated endpoints.
- Keep request/response and error shapes consistent for equivalent endpoints.
- Prefer existing pagination DTO conventions where pagination is required.
- Follow the existing mapping style in the touched area (currently mostly manual mapping). If
  introducing centralized mapping, do it consistently, not partially per file.

## 5) Validation and Error Handling

- Use FluentValidation validators in `Application` for request/business validation.
- Keep controller validation limited to transport checks (headers, route, body format).
- Prefer centralized exception-to-response handling over ad-hoc per-endpoint error shapes.
- Return consistent status codes and error payloads for equivalent failure categories.
- Do not duplicate reusable header checks when shared filters/middleware already exist.

## 6) Persistence

- Use the EF Core conventions already established in `PosDbContext`.
- Keep migration and schema changes inside the Infrastructure migration flow.
- **NEVER hand-write EF migration `.cs` files.** Migrations must be generated with
  `dotnet ef migrations add <Name>` so the `.Designer.cs` and `PosDbContextModelSnapshot.cs` stay in
  sync. Hand-written migrations (Steps 87–110) caused months of snapshot drift that broke the first
  generated migration in Aug 2026. When a step requires schema changes, modify the entities +
  `PosDbContext`, then run the EF CLI command — do not emit a migration file by hand. A migration
  commit without a Designer file and a snapshot update must be rejected in review.
  See `MIGRATION_SETUP.md`.

## 7) Security

- Keep JWT/auth settings in configuration/environment; never hardcode secrets.
- Use existing role/policy constants and authorization extensions; avoid inline role strings.
- Keep BCrypt password hashing for credential storage paths.
- Preserve rate-limiting, idempotency, and device validation patterns for write-sensitive endpoints.
- Keep CORS policies environment-appropriate; do not widen production exposure by default.

## 8) Logging and Observability

- Follow the configured Serilog conventions.
- Preserve correlation-id propagation and request enrichment middleware behavior.
- Log security/business-critical events at appropriate levels without leaking sensitive payloads.

## 9) Async, Naming, and Code Style

- Use async end-to-end for I/O paths and suffix async methods with `Async`.
- Pass cancellation tokens through to EF/HTTP calls when available.
- Avoid fake async wrappers around synchronous operations in new code.
- Naming: `PascalCase` types/methods/properties, `camelCase` locals/params, `I*` interfaces,
  `*Command` / `*Query` / `*Handler` / `*Validator` / `*Dto` suffixes.
- Prefer constructor injection and focused, single-responsibility classes.
- Follow the nullable reference conventions and explicit null handling used in this codebase.
- Comment only where logic is non-obvious; avoid redundant comments.

## 10) Test Alignment

- Keep tests aligned with current API contracts, enum names, and target framework versions.
- Add or adjust integration tests when changing endpoint behavior, auth, rate limiting, or
  transaction-sensitive flows.

## 11) Change Discipline

- Prefer incremental consistency improvements over broad rewrites.
- No massive refactors unless explicitly planned as their own step.
- Analyze downstream impact before introducing a new system; favor operational simplicity.
- Delete dead code as it is discovered rather than leaving parallel implementations alive.

## 12) Sustainability Governance (post–Step 45)

The operational cognition stack (Steps 29–45) is complete. Future steps are **sustainability-governed**,
not expansion-driven.

- **Before any step:** pass all four admission gates — Semantic, Complexity, Continuity, Operator
  (see `IMPROVEMENT_REPORT.md`).
- **Do not add cognition layers** without gate justification; prefer Direction A (consolidation),
  B (delivery surfaces), or C (historical continuity).
- **Consolidation only:** light, explicit shared primitives — no generic cognition engines, reflection,
  metadata-driven aggregation, or plugin systems.
- **Every step report** must include "Remaining Limitations & Architectural Pressure Assessment."
- **Hard constraints unchanged:** bounded FIFO stores, process-local, GET-only, sequential hub
  composition, inline aggregations, no synthesis-service recursion, advisory-only.

## 13) Git Push Discipline

After every step that is **validated and confirmed green** (build passes, architecture tests pass,
integration/compile checks done), push to GitHub before starting the next step. An untracked working
state between steps makes bisecting regressions much harder.

**Required commit message format:**

    <type>(<scope>): <short description> — Step <N>

    <body: what changed and why, one line per logical change>
    <deviations: any deviations from the agreed scope and the reason>
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
- If a step was validated in stages (backend green → Android green), a single commit after both are green is fine.
- If a step deviated from the agreed scope, document it in the commit body.
- Tag release-candidate commits: `git tag -a rc/<step-N> -m "Step <N> validated"` when backend and
  Android are fully green together.
- Do not force-push `main` — if a commit must be amended, use `git revert` plus a new commit.
