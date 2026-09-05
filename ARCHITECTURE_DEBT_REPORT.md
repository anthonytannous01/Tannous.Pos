# Architecture & governance debt report

This document tracks **known** architectural drift and risky areas. It complements automated checks in `tests/Tannous.Pos.Architecture.Tests` and CI (`.github/workflows/ci.yml`). Update it when allowlists shrink or when new debt is accepted intentionally.

_Last reviewed: 2026-09-05 (debt scan refresh; anonymous endpoint rate limiting)_

## 1. WebApi direct `PosDbContext` usage — **CLOSED**

**`posDbContextInjectionCount: 0`** (scan 2026-09-05). The allowlist in
`WebApiControllerGovernanceTests` is empty and `governance/debt-baseline.json` now caps this at
**0**, so any reintroduction fails CI rather than being absorbed by slack in the budget.

This section previously listed eight controllers (`Sync`, `Settings`, `Customers`, `Reports`,
`Admin`, `Inventory`, `Suppliers`, `Shifts`). All were migrated behind MediatR; the last,
`SyncController`, completed at Step 61 (Direction A). The list survived here for months after
the code was fixed — a reminder that a debt report is only as good as its last scan.

## 2. WebApi direct domain repository usage — **CLOSED**

**`repositoryInjectionCount: 0`** (scan 2026-09-05). Previously `CatalogController`,
`InventoryController`, `SuppliersController` and `ShiftsController`. Baseline capped at **0**.

**Direction (unchanged):** repositories stay behind Application handlers for new features.

## 3. Unversioned controllers — **one remaining**

| Area | Detail |
|------|--------|
| `DevicesController` | `api/[controller]` only, no `v{version:apiVersion}` route. The last unversioned controller; allowlisted in `ControllerVersioningGovernanceTests`. |
| Dual `api/...` + `api/v{version}/...` | Several controllers expose both patterns deliberately, for mobile clients that predate versioned routes. Not debt; removing either side breaks a shipped client. |

**`unversionedControllerCount: 1`**, budget tightened from 4 to **1** so a second one cannot
appear unnoticed. Versioning `Devices` closes this section; the only cost is that any client
calling `api/Devices` must be updated or a dual route kept.

## 3a. Unauthenticated endpoints and rate limiting

**`allowAnonymousCount: 9`.** Every one was audited on 2026-09-05 and is anonymous by design:
`Auth` login and refresh, the QuickBooks OAuth callback, the HMAC-verified delivery webhook,
customer feedback submission, the kiosk controller, and the public QR menu controller.

Until that audit **none of them had a rate limit.** Rate limiting was applied only to
authenticated mutations, so `POST /kiosk/orders` would accept unlimited orders from any caller
that could reach the API, and those orders appear directly on the kitchen display. The
`MutationsPerDevice` policy would not have helped: it partitions on the `Device-Id` header,
which anonymous callers do not send, so they would have shared one `"unknown"` bucket that a
single client could exhaust for everyone.

Three IP-partitioned policies now cover them — `PublicRead` (120/min), `PublicWrite` (30/min),
`PublicWebhook` (300/min) — configurable under `RateLimiting` in `appsettings.json`.
**`AnonymousEndpointRateLimitGovernanceTests`** fails the build if any anonymous endpoint lacks
a policy; that test, not the count, is the protection. The budget threshold was raised 2 → 9
with the audit recorded in `check-debt-budget.ps1`.

Related fix: the `RateLimiting` configuration section was previously dead. Limits were hardcoded
and `appsettings.json` advertised an auth limit of 5 while the code enforced 10. Values are now
read from configuration with defaults equal to what was actually enforced.

## 4. Sync contract drift (documented)

- `PullResponseDto` wire shape vs Android `PullWorker` expectations is called out in XML docs on `PullResponseDto` (potential client/server mismatch).
- `SyncController` returns anonymous objects for some outcomes (e.g. `SyncData`, `GetSyncStatus`) instead of shared DTOs / ProblemDetails.
- Push path uses `OutboxOperationDto` with `[JsonPropertyName("operationId")]` — covered by `WireContractGovernanceTests` and `SyncAndPaginationContractGovernanceTests`.

## 5. Legacy authorization policies (`Program.cs`)

Inline role policies (`Owner`, `Cashier`, `Admin`, etc.) remain registered in `Program.cs` alongside `AuthorizationExtensions` (`PolicyConstants` / `RoleConstants`). New code should prefer **policy constants**, not new inline `RequireRole("...")` strings in controllers.

## 6. CQRS bypass hotspots — **CLOSED**

Sections 1 and 2 are both at zero, so there are no remaining EF or repository hotspots in WebApi.
New endpoints default to **MediatR** + Application validators, and the baselines now fail CI on
any reintroduction rather than tolerating a budget of 16 and 46.

## 7. ProblemDetails vs ad-hoc JSON

- `GlobalExceptionHandler` emits RFC 7807 + `correlationId` extension + HTTP `status` (governed by source tests). Raw **`DbUpdateConcurrencyException`** maps to **409** with **title** `"Concurrency conflict"`, stable **detail** (no EF internals), and **Warning** logs including **`AffectedEntityTypes`** (operator telemetry only). **`InvalidOperationException`** from finalize/void concurrency translation still maps to **409** with **title** `"Conflict"` and the existing safe client message.
- Some controller paths still return `BadRequest(new { ... })` or anonymous objects (e.g. parts of `SyncController`). Tightening these is incremental work; do not weaken mobile contracts without coordination.

## 8. Domain / money / inventory risk comments

| Location | Risk |
|----------|------|
| `VoidOrderCommandHandler` | ~~Paid-order void had no inventory reversal~~ — **implemented and closed.** `ReverseFinalizeInventoryDeductionsAsync` creates idempotent offsetting `Return` movements (keyed on void reference), restores `CurrentStock`, and records audit per movement inside the Serializable paid-void transaction. Reversal is from finalize `Sale` movements only — **no recipe recompute** — correct by design. Observability: **`Inventory reversal observability`** logs. The `Inventory consistency observability` structured warning now lives in **`FinalizeOrderCommandHandler`** (negative-stock-after-sale deduction — different concern). See §11 Paid void row. |
| `FinalizeOrderCommandHandler` | Discount vs payment math mismatch logged as warning; idempotency for `Paid` is implemented. **Subtotal drift** warning if persisted `SubTotal` differs from line recomputation before finalize. **Inventory consistency observability** **Information**/**Warning** logs cover finalize deduction pass, persisted movement summary, and **negative stock after sale deduction** (still allowed by domain rules; observability only). |
| `OrderFinancialGovernance` | Single place for **legacy fixed 10%** tax on create/finalize subtotal (same numeric behavior as before). **Not** wired to `BusinessSettings.TaxRate` — receipt printing still uses settings % independently (see comment in `PrintingService`). |

## 9. Security automation gaps (manual / future)

The following are **not** fully automated yet (see roadmap in `GOVERNANCE_AUTOMATION_SUMMARY.md`):

- Device-Id on every mutation: **`RequireDeviceIdFilter`** is registered globally in `Program.cs` (governance test); `scan-debt.ps1` counts explicit in-controller Device-Id messages. **`MutationDeviceIdFilterGovernanceTests`** lists mutating actions and asserts MVC filter metadata includes `RequireDeviceIdFilter` (except ignored Auth login/refresh / health). Per-endpoint bypass review remains manual if filters change.
- CORS policy misuse across environments (relies on `Program.cs` review).
- **OpenAPI drift:** CI gates **critical path keys** in `governance/critical-openapi-paths.txt` plus **`OpenApiSchemaGovernanceTests`** against `governance/openapi-schema-governance-baseline.json` (selected sync/order/auth schema property names). Full exhaustive schema diff is still manual.
- **Optimistic concurrency:** `ConcurrencyGovernanceTests` + `concurrency-entity-baseline.json` provide visibility on `Order` / `InventoryItem` / `Shift` (and document `InventoryStock` as non-domain); hard failure only if a future baseline lists expected members that disappear. **`ConcurrencyMigrationReadinessGovernanceTests`** + **`concurrency-migration-readiness-baseline.json`** extend readiness to **`Payment`** and **`InventoryMovement`** (UpdatedAt-style via `BaseEntity` when applicable); fails only if **`expectRetentionSubstrings`** is violated.
- **Money-path source anchors:** **`MoneyPathGovernanceSourceTests`** (finalize status gate, Paid short-circuit, explicit transaction boundaries, subtotal drift log, snapshot/tax governance references, paid-void warning + persist, legacy tax rounding anchor).

## 10. Observability

- Correlation ID: `CorrelationIdMiddleware` + `GlobalExceptionHandler` extensions (source-level tests).
- Structured request logging: `UseSerilogRequestLogging` (source-level test).
- Health JSON writer: `HealthCheckResponseWriter` (source-level test).

## 11. Concurrency, replay, and transaction boundaries (documented)

| Area | Risk | Notes / direction |
|------|------|-------------------|
| **Hotspot inventory (governance metadata)** | Same LWW / race themes as below, but now **listed explicitly** in `governance/concurrency-hotspots.json` with `hotPaths`, `risk`, and **`hasConcurrencyToken`** for **Order**, **InventoryItem**, **Shift** (`true` where **`byte[] RowVersion`** exists — **Payment** / **InventoryMovement** unchanged). | **`ConcurrencyHotspotGovernanceTests`** fails CI if the inventory drifts from real sources or from **`concurrency-migration-readiness-baseline.json`**. |
| **RowVersion / EF concurrency** | **`Order`**, **`InventoryItem`**, **`Shift`** expose **`[Timestamp] byte[] RowVersion`** (PostgreSQL **bytea**); migration **`AddRowVersionConcurrencyToOrdersInventoryItemsShifts`** adds columns with deterministic defaults for existing rows. **Finalize** / **void** handlers catch **`DbUpdateConcurrencyException`**, log **warnings** with **money-path concurrency visibility** templates + **`AffectedEntityTypes`**, map to **`InvalidOperationException`** → HTTP **409** via **`GlobalExceptionHandler`**. Raw **`DbUpdateConcurrencyException`** at the pipeline edge maps to **409** ProblemDetails (**`Concurrency conflict`** title, **`correlationId`**, safe detail; **no** stack/EF leakage to clients). | Does **not** add automatic **retry** or **merge** resolution; **no** tokens on **Payment** or **InventoryMovement**; sync push semantics unchanged. Debt scan reports **`conflictProblemDetailsCount`**, **`concurrencyExceptionHandlingCount`**, **`concurrencyWarningLogCount`**, **`optimisticConcurrencyEntityCount`** (visibility-only). |
| **Concurrency upgrade plan (governance metadata)** | **`governance/concurrency-upgrade-plan.json`** records **`recommendedToken`**, **`migrationComplexity`**, and **`notes`** per aggregate (aligned with **Order** / **InventoryItem** / **Shift** RowVersion rollout where applicable). | **`ConcurrencyUpgradeGovernanceTests`** validates entities, `hotPaths`, no duplicate names, and alignment with **hotspots** or **readiness** baseline. |
| **Order finalization** | **RowVersion** on **`Order`** detects concurrent updates during **`SaveChanges`** in the finalize transaction. **`FinalizeOrderCommandHandler`** joins an **ambient** EF transaction when **`Database.CurrentTransaction`** is already set (sync durable replay outer scope) so finalize + **`SyncOperationReceipt`** commit together; otherwise it opens its own transaction as before. | Handler keeps explicit EF transaction pattern and **Paid** short-circuit; **`DbUpdateConcurrencyException`** → logged warning + **`InvalidOperationException`** → HTTP **409** with ProblemDetails. Parallel **Open** finalizes that race still surface as conflicts instead of silent LWW on the order row. |
| **Inventory / stock** | **`InventoryItem.RowVersion`** guards stock mutations in the same finalize transaction (conflicts if another writer updated the row). | **InventoryMovement** rows remain non-tokenized; sync processors unchanged. |
| **Shift closing** | **`Shift.RowVersion`** guards concurrent shift mutations (close / cash-drop paths). | Same HTTP **409** mapping via **`DbUpdateConcurrencyException`** when applicable. |
| **Paid void / inventory / refund** | **Paid void** in one EF transaction: internal **`PaymentRefund`** for **`NetCapturedAmount`** (not raw tendered — **change due** excluded), finalize **Sale** reversals, order **Void**. **Settlement (finalize):** **`AmountTendered`**, **`ChangeDue`**, **`NetCapturedAmount`** on **`Order`**; **Settlement consistency observability** for exact/over/under payment. **Tax:** legacy 10% on order row vs receipt **`TaxRate`** — **`OrderFinancialTaxGovernance`** (not unified). | **Not** external processors; **not** ledger/double-entry; **not** cash-drawer hardware; no multi-currency; pre-settlement orders use refund resolver fallback. |
| **Sync reconciliation visibility** | Internal **`SyncConflictRecord`** + **`ISyncConflictRecorder`** (best-effort persistence; secondary failures swallowed). Records: **`DbUpdateConcurrencyException`**, replay **operationId** type mismatch, stale finalize on void, invalid void lifecycle, negative stock after finalize, partial sync batches with inventory failures. Logs: **`Sync reconciliation observability:`** (no payload bodies). | **Not** automatic conflict resolution, CRDTs, event sourcing, distributed locking, cross-device merge, mobile API exposure, or background reconciliation workers. Replay/idempotency still does **not** guarantee semantic multi-master consistency. |
| **Operational audit trail (forensics)** | Append-only **`OperationalAuditRecord`** + migration; **`IOperationalAuditRecorder`** / **`OperationalAuditRecorder`** (isolated EF scope; **best-effort** — audit failures never fail business operations). **`IOperationalAuditTimelineService`** + **`IOperationalAuditQueryService`** for chronological reconstruction. **Internal diagnostics API** (`/api/v1.0/internal/operational-audit`, **Admin/Owner** policy, GET-only): order/device/operation/entity timelines + recent conflicts; paginated; projects **Summary** as Message and bounded metadata map only. Hooks: finalize/void settlement & lifecycle, inventory deduction/reversal, durable replay mismatch/short-circuit, sync partial/mixed batches, placeholder processors, global concurrency. Logs: **`Operational audit observability:`** and **`Operational audit diagnostics:`**. | **Not** event sourcing, immutable ledger guarantees, SIEM, customer/mobile audit API, real-time streaming, distributed audit aggregation, or payload/stack exposure. Records are operational diagnostics — **not** authoritative financial truth. |
| **Operational reconciliation workflow (operator-driven)** | **`ReconciliationResolutionStatus`** on **`SyncConflictRecord`** (additive migration; existing rows valid). **`ISyncConflictReconciliationService`** + **`OperationalAuditReconciliationController`** at **`/api/v1.0/internal/operational-audit/reconciliation`** (**Admin** policy): unresolved/by-status/summary queries; POST acknowledge / investigate / resolve / ignore (append-only status transitions + **`OperationalAuditRecord`** under **`ReconciliationWorkflow`** with **ConflictAcknowledged**, **InvestigationStarted**, **ConflictResolved**, **ConflictIgnored**). Notes capped (**512** chars). **`IOperationalAuditQueryService`** extended with reconciliation-status / conflict-type / unresolved-only filters. Logs: **`Operational reconciliation observability:`** (status changed, unresolved/summary queries, audit persisted, notes truncated). | **Not** automated healing, distributed coordination, financial ledger guarantees, background workers, or mobile/customer API exposure. Operators manually triage — no delete endpoints. |
| **Operational forensic snapshot export (read-only)** | **`IOperationalForensicSnapshotService`** + **`OperationalAuditForensicExportController`** at **`/api/v1.0/internal/operational-audit/export`** (**Admin** policy, GET-only): compact **`OperationalForensicSnapshotDto`** by conflict/order/operation/device — chronological audit timeline, related **`SyncConflictRecord`** rows (with **ResolutionStatus**), **`SyncOperationReceipt`** replay summaries in safe **Metadata** (reuses **`OperationalAuditMetadataProjection`**). Survivability fields: **`SnapshotGeneratedUtc`**, **`SnapshotSchemaVersion`**, **`ExportSource`**, **`TruncationFlags`**, **`RetentionClassification`**. Caps: **500** audit / **100** conflicts / **50** receipts per snapshot; truncation logged via **`Operational export survivability:`**. Logs: **`Operational forensic observability:`**. | **Not** legal evidence system, immutable blockchain/ledger, external compliance archive, BI analytics, customer exports, real-time streaming, or external integrations. Append-only audit semantics preserved; export is portability for incident/support review only. |
| **Operational retention & query lifecycle (governance only)** | **`OperationalRetentionConstants`** / **`OperationalRetentionCategories`** / **`OperationalRetentionGovernance`** document hot (**7d**), warm (**30d**), forensic (**365d**) windows and **`MaxQueryDateRangeDays` (90)** — **no automatic deletion**. **`OperationalQueryProtection`** clamps pagination and oversized date ranges on internal diagnostics/reconciliation queries. **`OperationalConflictLifecycleClassifier`** enriches **`SyncConflictItemDto`** with **`AgingSeverity`** / **`EscalationRecommendation`** (no workers/notifications). **`GET /api/v1.0/internal/operational-audit/retention/summary`** returns safe volume/aged-conflict metrics. Logs: **`Operational retention observability:`**, **`Operational query protection:`**, **`Operational export survivability:`**. | **Not** automatic pruning, physical archival (S3/Azure), immutable compliance vault, background cleanup workers, or customer-visible retention APIs. |
| **Operational resilience & degraded-mode visibility** | **`OperationalResilienceConstants`** / **`OperationalDegradedModeTypes`** / **`OperationalResilienceGovernance`** classify informational degraded modes (**Normal**, **ElevatedQueryPressure**, **ReconciliationPressure**, **ExportPressure**, **AuditPersistencePressure**, **ReplayStormRisk**). **`IOperationalResilienceDiagnosticsService`** + **`GET /api/v1.0/internal/operational-audit/resilience/*`** (summary, degraded-modes, pressure-indicators, replay-risk-summary). In-process **`IOperationalAuditPersistenceTelemetry`** + **`IOperationalResiliencePressureState`** track audit failures and query/export pressure (no queues). Forensic exports add **`ExportPressureClassification`** / **`TruncationSeverity`**. Retention summary enriched with degraded-mode fields. Logs: **`Operational resilience observability:`**, **`Operational degraded mode:`**, **`Operational backpressure visibility:`**. | **Not** distributed circuit breakers, external queueing, auto failover, autoscaling, request shedding, throttling, or retries on audit. Business/replay/reconciliation semantics unchanged; audit remains best-effort. |
| **Operational incident correlation (dynamic)** | **`OperationalIncidentSeverity`** / **`OperationalIncidentTypes`** / **`OperationalIncidentGovernance`** + **`IOperationalIncidentCorrelationService`** compute heuristic incident groups from conflicts, audit signals, and resilience pressure (no persistent incident tables). **`GET /api/v1.0/internal/operational-audit/incidents/*`** (summary, high-risk, by-order/device/operation, cascading-degradation). Forensic exports add **`CorrelatedIncidentRisk`**, **`CorrelatedSubsystems`**, **`IncidentCorrelationSummary`**. Grouping: operationId → deviceId → orderId → entityId. Logs: **`Operational incident observability:`**, **`Operational causality visibility:`**, **`Operational correlation risk:`**. | **Not** PagerDuty/alerting, OpenTelemetry, distributed tracing, automatic remediation, or legal/incident ticketing integration. Correlation is in-process and heuristic only. |
| **Operational dashboard (operator read model)** | **Step 20:** **`IOperationalDashboardService`** + **`GET /api/v1.0/internal/operational-audit/dashboard`** — aggregates existing resilience, reconciliation, incident, alert, governance overview, runtime protection, and fingerprint summaries into operator-friendly health/risk/pressure/activity/recommendations. Governance freeze remains active; no new cache diagnostics routes. | **Not** deployment gating, authoritative business truth, mobile exposure, auto-remediation, or governance subsystem expansion. |
| **Operational diagnostics cache (in-process)** | Steps 1–17: through fingerprinting. **Step 18:** simplification + production readiness. **Step 19:** formal governance freeze. **Step 20:** operator dashboard consumes frozen diagnostics — no expansion. | **Not** persistence, deployment gating, auto-remediation, new cache diagnostics routes, or business semantic changes. |
| **Operational alert signals (escalation visibility)** | **`OperationalAlertGovernance`** + **`IOperationalAlertSignalService`** derive query-time **`OperationalAlertSignalDto`** rows from reconciliation summaries, resilience/degraded-mode pressure, replay risk, incident risk, and audit persistence telemetry — **no alert persistence**. **`GET /api/v1.0/internal/operational-audit/alerts/*`** (summary, current, critical, replay-pressure, inventory-risk). Forensic exports add **`AlertSignals`**, **`AlertSummary`**, **`EscalationRisk`**, **`OperationalPressureSummary`**. Logs: **`Operational alert visibility:`**, **`Operational escalation visibility:`**, **`Operational pressure escalation:`**. | **Not** email/SMS/push/webhooks, paging/on-call, guaranteed delivery, auto-remediation, queues, or distributed alert infrastructure. Signals reset on process restart; operators must query diagnostics manually. |
| **Payment settlement** | **Amount owed** = **`TotalAmount`**; **tendered** = sum of payments; **change due** = max(tendered − owed, 0); **net captured** = tendered − change due. Overpayment allowed with explicit fields. Refunds on void use **net captured** only. | Change is modeled on the order row, not as separate cash-drawer events; **OrderDto** wire unchanged (fields server-internal unless clients read DB). |
| **Sync push** | **Durable replay:** **`SyncOperationReceipt`** + **`IDurableSyncReplayCoordinator`** protects all seven known push types in **`DurableSyncReplayProtectedTypes`** — **CreateOrder**, **FinalizeOrder**, **CashDrop**, **AdjustInventory**, **RecordWastage**, **OpenShift**, **CreateCustomer** (Serializable EF transaction; finalize can join ambient transaction). **Placeholder processors** (**OpenShift**, **CreateCustomer**) still return placeholder success but duplicate **`operationId`** replays short-circuit via receipt. **Batch semantics unchanged:** each operation succeeds/fails independently; HTTP **200** with mixed **`results`** is normal. **Internal classification** labels outcomes for logs only. **Reconciliation visibility (logs only):** **`Sync reconciliation visibility:`** when replay short-circuit coexists with failure/conflict, plus mixed placeholder/inventory replay batch warnings — **no** background reconciliation jobs or persistence tables. Unknown operation types remain **without** durable replay. | **Not** distributed replay guarantees across devices/servers; **no** saga/workers/locks. Classification does **not** change wire contracts. Metrics include **`customerShiftReplayProtectedProcessorCount`**, **`replayReconciliationVisibilityCount`**, **`replayMixedBatchWarningCount`**, **`protectedPlaceholderProcessorCount`**. Tests: **`DurableSyncReplayGovernanceTests`**, **`SyncReplayReconciliationGovernanceTests`**, **`SyncDurableReplayIntegrationTests`**, **`SyncBatchClassificationIntegrationTests`** (Docker). |
| **Finalize payments** | **Overpayment** is allowed (warning only); **underpayment** throws. | No “change due” model; clients should send exact totals or accept overpayment as recorded. |

**Operational guidance:** Treat duplicate finalize and sync retries as normal; use **correlation id** + **idempotency key** logs for incident triage. Run integration tests in an environment with **Docker** for finalize/inventory assertions.

## 12. Package advisory visibility (two open advisories)

- **`AutoMapper` 12.0.1** — **NU1903** ([GHSA-rvv3-g6hj-g44x](https://github.com/advisories/GHSA-rvv3-g6hj-g44x)).
- **`Microsoft.Extensions.Caching.Memory` 8.0.0** — **NU1903** ([GHSA-qj66-m88j-hmgj](https://github.com/advisories/GHSA-qj66-m88j-hmgj)). Not previously recorded here; it appears only in build output, and `scan-debt.ps1` counts the AutoMapper one but not this. Both are high severity.

  Accepted for now in governance-only workstreams: no silent package bump without coordinated regression and mobile impact review.
- **Visibility:** `governance/scan-debt.ps1` emits **`knownNugetAutoMapperAdvisoryCount: 1`** in `debt-report.json` and the grouped CI summary calls it out under **Advisory visibility** so the count is not “invisible noise” only in build logs.
- **Future:** Plan an explicit upgrade + transitive dependency review; until then treat the warning as known operational debt.

---

**Regenerating counts:** `governance/scan-debt.ps1` → `governance/debt-report.json` (includes **paid void reversal/refund** metrics (**`inventoryReversalMovementCount`**, **`reversalObservabilityAnchorCount`**, **`refundConsistencyAnchorCount`**, **`refundPersistenceCount`**, **`refundIdempotencyProtectionCount`**, **`overpaymentObservabilityCount`**, **`taxDivergenceGovernanceCount`**, **`paidVoidReversalProtectionCount`**, **`reversalTransactionBoundaryCount`**, **`reversalConcurrencyHandlingCount`**), sync, transaction, placeholder, replay, OpenAPI, **concurrency token / readiness / upgrade-plan count**, **money-path anchor**, **moneyPathReplayRiskCount**, **missingDurableIdempotencyCommentCount**, **reconciliationWarningCount**, **replayReconciliationVisibilityCount**, **replayMixedBatchWarningCount**, **AutoMapper advisory visibility**, **409 / concurrency visibility** metrics, **replay/idempotency visibility** metrics (**`idempotencyWarningLogCount`**, **`replaySensitiveMoneyProcessorCount`**, **`partialBatchReplayWarningCount`**, **`placeholderReplayGovernanceCount`**, **`conflictProblemDetailsCount`**, **`concurrencyExceptionHandlingCount`**, **`concurrencyWarningLogCount`**, **`optimisticConcurrencyEntityCount`**), **financial/inventory consistency visibility** metrics (**`inventoryConsistencyWarningCount`**, **`inventoryMovementObservabilityCount`**, **`moneyInventoryReplayClassificationCount`**, **`transactionBoundaryLogAnchorCount`**), **durable replay reporting** metrics (**`durableReplayProtectedProcessorCount`**, **`replayReceiptEntityCount`**, **`replayReceiptLookupCount`**, **`replayReceiptUniqueIndexCount`**), **inventory durable replay visibility** metrics (**`inventoryReplayProtectedProcessorCount`**, **`inventoryReplayReceiptCount`**, **`replayProtectedInventoryProcessorCount`**), **customer/shift durable replay** metrics (**`customerShiftReplayProtectedProcessorCount`**, **`replayProtectedCustomerShiftProcessorCount`**, **`customerShiftReplayVisibilityCount`**, **`protectedPlaceholderProcessorCount`**), and **sync batch classification visibility** metrics (**`syncBatchClassificationCount`**, **`replayShortCircuitClassificationCount`**, **`partialBatchObservabilityAnchorCount`**, **`placeholderClassificationCount`**)). **`governance/check-debt-budget.ps1`** for hard budgets + optional soft growth warnings vs **`debt-warning-trend.json`**. Narrative sections still need human review when behavior changes.
