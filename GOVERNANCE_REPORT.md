# Governance Report — Architecture, Technical Debt & Long-Term Maintainability

**Role:** Staff-level governance assessment (no rewrites, no new frameworks, no broad refactors).  
**Authoritative baseline:** `ARCHITECTURE_RULES.md`, `ARCHITECTURE_SUMMARY.md`, `IMPROVEMENT_REPORT.md`.  
**Stack constraint (preserved):** ASP.NET Core, PostgreSQL, Android (Kotlin), Docker Compose, CQRS/MediatR, EF Core.

---

## Executive Summary

The repository has a **credible layered + CQRS spine** and several **production-minded controls** (JWT, policies, rate limiting, idempotency on sensitive REST paths, Serilog, correlation IDs, health checks, CI, Docker). **Governance risk is dominated by consistency drift**, not missing fundamentals: multiple controllers still **own data access and orchestration** (`PosDbContext` in WebApi), **API versioning and error contracts are uneven**, **sync and mobile contracts diverge**, and **financial/inventory rules can split across paths** (REST vs sync, finalize vs void, hardcoded tax vs settings).  

Without explicit enforcement (reviews, lint/architecture tests, and incremental migration rules), **new code will naturally follow the nearest existing pattern**—including the weaker ones—accelerating **entropy**.

---

## Current Engineering Maturity

| Dimension | Level | Notes |
|-----------|--------|--------|
| **Layer discipline** | **Medium** | Clear project boundaries; **implementation** frequently bypasses Application for reads/writes in WebApi. |
| **CQRS adoption** | **Partial** | Strong on Orders, Auth, Users, Catalog mutations, much of Inventory/Suppliers; **weak** on Settings, Customers, Reports, Sync, Admin slices. |
| **Contract governance** | **Medium–Low** | DTOs exist; versioning dual-routes help mobile but **Suppliers / Inventory / Reports / Devices** remain unversioned-only unless extended elsewhere. |
| **Operational safety** | **Medium** | Health, logging, rate limits, idempotency patterns; **degraded readiness semantics** and **sync idempotency** weaker than REST. |
| **Test signal** | **Medium** | Integration tests exist; **narrow surface** vs full API + sync + financial edge cases → **false confidence** risk. |
| **Security baseline** | **Medium** | Solid primitives; **policy sprawl / legacy names**, **CORS**, and **sync** remain governance hotspots. |

---

## Highest-Risk Technical Debt (Ranked)

1. **Sync correctness & contract drift (backend ↔ mobile)** — false success, mismatched pull shapes, replay without same guarantees as REST → **trust erosion** in money/inventory data. **Risk: Critical.** **Maintenance cost: Very high** (every release is a reconciliation incident without contract tests).
2. **Financial rule duplication / inconsistency** — hardcoded tax in finalize/create vs `BusinessSettings`; discount field vs finalize math; void paid without reversal paths documented in code as incomplete → **silent ledger drift**. **Risk: Critical.** **Cost: High** (support, audits, refunds).
3. **Direct `PosDbContext` in controllers** — `SyncController`, `SettingsController`, `CustomersController`, `ReportsController`, `AdminController`, `InventoryController`, `SuppliersController`, `ShiftsController` (partial) → **rules live in transport**, harder to test and reuse. **Risk: High.** **Cost: High** (each feature duplicates querying/authorization/validation patterns).
4. **API governance drift** — mixed versioned/unversioned routes, mixed error shapes (ProblemDetails vs ad-hoc JSON), pagination naming mobile vs server → **client breakage** and **support load**. **Risk: High.** **Cost: Medium–High**.
5. **Void / reversal incompleteness** — inventory reversal placeholder for paid void → **domain integrity** hole. **Risk: High.** **Cost: Medium** until void volume grows, then **high**.

---

## Architectural Drift Findings

### Controllers bypassing Application layer (or duplicating it)

**Evidence:** WebApi controllers inject `PosDbContext` directly: `SyncController`, `SettingsController`, `CustomersController`, `ReportsController`, `AdminController`, `InventoryController`, `SuppliersController`; `ShiftsController` mixes MediatR + context.

**Why dangerous long-term:** Business rules and query shapes become **copy/paste across actions**, bypass FluentValidation pipeline consistency, and **cannot be unit-tested** at the same granularity as handlers. New engineers ship “controller-sized features” that violate `ARCHITECTURE_RULES.md` §2 without noticing.

**Risk:** **High**  
**Future maintenance cost:** **High** (every change needs full-stack reasoning; refactors touch HTTP + EF together).

### Inconsistent CQRS adoption

**Evidence:** Orders/Users/Auth path is handler-centric; reporting/customers/settings/sync are not.

**Why dangerous:** The codebase **signals two valid styles**. AI and humans pick the faster path; **entropy wins**.

**Risk:** **Medium–High**  
**Cost:** **Medium** recurring (code review debate, partial migrations).

### Mixed validation & transaction boundaries

**Evidence:** `ValidationBehavior` + per-controller header/idempotency checks; some flows use explicit transactions in handlers (e.g. finalize), others use `UnitOfWork` without the same visibility in controller-direct paths.

**Why dangerous:** Invariants enforced in **different layers** by feature → **subtle production bugs** when one path is updated.

**Risk:** **Medium–High**  
**Cost:** **Medium**.

### Duplicated business rules

**Evidence (baseline + code):** Tax rate hardcoded in order flows vs settings elsewhere; COGS / EOD logic in controllers vs MediatR; sync push stubs vs real order pipeline.

**Why dangerous:** **Divergent truth** — the worst class of bug: each path looks “correct” locally.

**Risk:** **Critical** for money-adjacent paths; **High** overall.  
**Cost:** **Very high** when reconciling reports vs bank vs inventory.

---

## Domain Boundary Integrity Risks

| Topic | Issue | Silent divergence risk |
|-------|--------|-------------------------|
| **Aggregate ownership** | Order/payment/inventory tightly coupled on finalize transaction; other paths (sync void stubs, void without reversal) **not equivalent**. | **High** |
| **Sync vs REST** | Sync mutates inventory for some ops; order ops can report success without persistence. | **Critical** |
| **Invariants** | Negative stock explicitly allowed in finalize path comments; may be policy-by-accident. | **Medium** (OK if documented as business rule) |
| **Mobile-only rules** | IMPROVEMENT_REPORT notes tax/total divergence risk cross-platform. | **High** if mobile computes payable amounts independently |

**Where rules can silently diverge:** finalize vs create totals; void vs finalize inventory; sync push vs `OrdersController`; reports using `CreatedAt` vs business “business day”; COGS average cost timing vs sale-time cost.

---

## API Governance Risks

| Finding | Impact |
|---------|--------|
| **Dual routes** (`api/...` + `api/v1.0/...`) on several controllers | Reduces 404 risk for mobile; **increases** duplicate OpenAPI surface and “which is canonical?” drift. |
| **Unversioned-only** | `SuppliersController`, `InventoryController`, `ReportsController`, `DevicesController` — mobile `BASE_URL` includes `/api/v1.0/` → **historical breakage risk** for any client that only calls versioned prefix. |
| **Response shapes** | ProblemDetails for unhandled paths; auth/errors still `{ message }` patterns in places; sync batch results per op. | Client error parsers **branchy**. |
| **Pagination** | Server `PaginatedResponseDto` vs Kotlin field names historically misaligned; `search` vs `q` partially addressed. | **Contract test gap**. |
| **Sync stability** | Pull DTO vs Android `SyncPullResponse` mismatch documented in `PullResponseDto` comments. | **Highest mobile break risk**. |

**Endpoints most likely to break mobile compatibility:** anything under **sync pull/push**, **pagination-heavy lists**, **settings field names**, and **any unversioned-only** controller if the client base URL is versioned.

---

## Scalability Hotspots

| Hotspot | ~10x traffic failure mode |
|---------|----------------------------|
| **Sync pull** | Large `Include` graphs, multiple lists, shared offset token → **memory spikes**, slow TTFB, **DB pressure**, client timeouts → partial apply + cursor confusion. |
| **Controller EF queries** | Unbounded or wide queries in reports/admin paths → **connection pool saturation**, **CPU** on materialization. |
| **N+1 patterns** | COGS-style loops that hit repository per ingredient line (pattern exists) → **latency explosion** as catalog grows. |
| **Serialization** | Large DTO payloads on sync and catalog → **GC pressure**, mobile battery/network. |
| **Concurrency** | Same inventory rows under multi-register finalize without optimistic tokens everywhere → **last-write-wins** / lost updates (database may serialize, but app logic may not expect retries). |

---

## Operational Risks

| Area | Gap |
|------|-----|
| **Silent production failure** | Degraded DB health still HTTP 200 unless consumer reads JSON body; **migrations pending** unnoticed. |
| **Startup** | Seeding failures logged as warnings; app continues → **“works” but unusable** state. |
| **Backup/restore** | Script-based DR; confidence requires **routine restore drills** (process maturity, not code). |
| **CI vs local** | `global.json` vs `8.0.x` in CI → **subtle “works on my machine”** drift. |
| **Docker** | Root user, `curl` in image (tradeoff), secrets via env — **acceptable** with ops discipline. |

---

## Security Governance

| Risk | Why it worsens over time |
|------|---------------------------|
| **Legacy policy names** (`Owner`, `Admin` aliases) | New endpoints may pick **inline strings** or wrong policy → **authorization drift**. |
| **CORS** | Permissive dev policy must stay **strictly environment-gated**; one mis-deploy = **broad exposure**. |
| **Sync idempotency** | REST mutations use **`IIdempotencyStore`**; sync **push** now persists **`SyncOperationReceipt`** for **CreateOrder**, **FinalizeOrder**, **CashDrop**, **AdjustInventory**, **RecordWastage** (deviceId + operationId). Other push operations (**OpenShift**, **CreateCustomer**, …) remain **without** durable replay; placeholder processors still omit full MediatR pipelines. |
| **Secrets** | Placeholder appsettings safe only if **100% env injection** in every environment. |
| **Tenant isolation** | Single-tenant assumptions; any future multi-tenant work **without** boundary enforcement → **data leakage** class bugs. |

**Most dangerous future security risk:** **trusted sync channel** treated like internal admin API without the **same idempotency, audit, and authorization rigor** as REST — combined with **mobile bearer token** theft or device compromise.

---

## Testing Gaps

| Gap | False confidence |
|-----|------------------|
| **Integration tests** | Cover slice (orders, rate limits); **not** full matrix of controllers, sync, reports, void. |
| **Contract tests** | Missing for **mobile JSON** vs server DTOs (pull/push, pagination). |
| **Concurrency** | Few/no tests for **double finalize**, **parallel inventory**, **idempotent replay** under race. |
| **Financial edge** | Overpay, discount, tax change, void-paid — **under-tested** vs code warnings/comments. |
| **Failure paths** | Partial sync apply, mid-batch HTTP failure, **WorkManager retry** + server state. |

---

## Team / Productivity Risks

| Pain (multi-developer) | Cause |
|-------------------------|--------|
| **“Where does logic go?”** | Two valid homes (controller+EF vs handler). |
| **Review fatigue** | Mixed patterns mean reviewers must **re-derive rules** each PR. |
| **Onboarding** | Must learn **both** CQRS purity and **pragmatic** exceptions. |
| **Discoverability** | Business rules scattered: handlers, controllers, sync, mobile. |
| **Documentation split** | Architecture docs vs code drift unless **governed** in PR checklist. |

---

## Recommended Priorities (Governance, Not Rewrites)

1. **PR / AI checklist** literally derived from `ARCHITECTURE_RULES.md` (one page): “new business logic → Application handler”, “no new direct DbContext in controllers”, “versioning rule for touched controller”.
2. **Contract tests** for **sync** and **pagination** (golden JSON or consumer-driven fixtures) — highest ROI anti-drift.
3. **Incremental extraction** of highest-risk controller-EF modules (**Sync**, **Customers**, **Reports**) into Application commands/queries **one endpoint cluster at a time**.
4. **Single source of truth** for **tax/totals** (shared calculator or explicit “server authoritative” rule documented and enforced).
5. **Void / paid reversal** design doc + minimal implementation when product priority allows — **domain integrity** anchor.
6. **Architecture tests** (NetArchTest or similar) optional: **fail build** if new `PosDbContext` injections appear in `WebApi` (allowlist existing files until migrated).

---

## Safe Incremental Refactor Plan (12–18 Month Horizon)

| Quarter | Scope | Exit criteria |
|---------|--------|----------------|
| **Q1** | Freeze **new** direct-`PosDbContext` usages; add checklist + 1–2 contract tests (sync pull shape, customers pagination). | No new drift; tests green. |
| **Q2** | Migrate **Settings + Customers read paths** to Application queries; keep routes stable. | Controllers thin; behavior parity via integration tests. |
| **Q3** | Migrate **Reports EOD** to MediatR query; align timezone semantics in DTO/docs. | Single reporting pipeline. |
| **Q4** | **Sync** — replace stubs with delegation to existing commands **or** explicit `501`/failure contract; add idempotency strategy. | No false-success paths for money ops. |

**Throughout:** small PRs, feature flags only if needed, preserve API routes unless versioning bump agreed.

---

## Dangerous Refactors To Avoid

- **Big-bang** “all controllers to MediatR” in one release.  
- **Introducing** event sourcing, Kafka, microservices, or new mapper frameworks **without** org capacity.  
- **Rewriting sync** as greenfield before contracts and tests exist.  
- **Changing tax/total semantics** without coordinated mobile + data migration plan.  
- **Removing** legacy routes without **deprecation window** and client telemetry.

---

## Production Readiness Scorecard

| Pillar | Score (1–5) | Rationale |
|--------|----------------|-----------|
| **Architecture clarity** | **3** | Good intent; **uneven execution**. |
| **Domain integrity** | **2–3** | Strong finalize path; **void/sync gaps**. |
| **API contract stability** | **2–3** | Dual routes help; **sync + naming** fragile. |
| **Scalability headroom** | **3** | OK for small footprint; **sync/report** hotspots. |
| **Security governance** | **3** | Strong primitives; **sync + policy consistency** weaker. |
| **Operability** | **3–4** | Logging/health/CI/Docker solid; **runbook + readiness semantics** thinner. |
| **Test governance** | **2–3** | Exists; **narrow vs reality**. |
| **Team scalability** | **2–3** | Works small team; **entropy risk** as headcount grows. |

**Overall:** **~3 / 5** — production-capable with **governance debt** that scales **superlinearly** with features and contributors unless arrested incrementally.

---

## Important Constraints (Non-Negotiables for This Codebase)

- Preserve **layer intent** even when migrating: `WebApi` = transport, `Application` = use cases, `Domain` = model/contracts, `Infrastructure` = persistence.  
- **No new architectural styles** beyond established hybrid (per `ARCHITECTURE_RULES.md`).  
- **CQRS for new business logic** — treat as default law for AI and humans.  
- **Do not weaken** idempotency, device validation, or rate limiting on sensitive writes without explicit risk acceptance.  
- **Mobile + Docker Compose** remain first-class; governance must include **cross-tier contracts**.  
- Prefer **documentation + tests + small moves** over heroic rewrites.

---

## Closing

This system is **not architecturally broken** — it is **governance-sensitive**: the gap between **documented ideals** (`ARCHITECTURE_RULES.md`, architecture summaries) and **allowed shortcuts in code** is wide enough that **entropy is the default outcome** unless enforcement becomes as boring and automatic as CI compilation. The highest leverage investments are **contract tests**, **freezing drift** (checklist + optional arch tests), and **incremental relocation** of the worst boundary violations—**especially sync and money paths**—without changing the stack or delivery model.
