# Tannous POS — Project Status

_Assessed 2026-09-05, after Step 128._

A snapshot of what is built, what is genuinely outstanding, and what is merely stale
documentation. Update this when the picture changes; do not let it rot like the reports
it corrects.

---

## Short answer

The system is feature-rich and unusually clean, but it has never met reality. It has not
run a service, has not been used by staff who did not build it, has not been installed on
a second device, and has not shipped. Every remaining risk sits in that gap, not in the
code.

---

## Code health

| Signal | Value |
|---|---|
| TODO / FIXME / HACK in backend (1,448 `.cs` files) | **1** |
| TODO / FIXME / HACK in mobile (116 `.kt` files) | **0** |
| `posDbContextInjectionCount` | 2 (budget 16) |
| `repositoryInjectionCount` | 10 (budget 46) |
| Unit tests in the mobile project | 21 (`ReceiptRendererTest` 12, `LogFileWriterTest` 9) |

Essentially no debt is hiding in comments. The governance discipline worked.

---

## What is actually outstanding

### 1. Tax configuration — **closed 2026-09-05**

Was: the Android **Enable Tax** toggle was ignored by the backend, order creation applied a
hardcoded 10%, and the kiosk path had a third copy of the tax rule. Finalize was already
settings-driven, so finalized totals were correct while pre-finalize displays were not.

Now: `BusinessSettings.TaxEnabled` is a real persisted column and `BusinessSettings.TaxApplies`
(`TaxEnabled && TaxRate > 0`) is the single rule. All four order paths — create, finalize,
kiosk, and the receipt label — go through `OrderFinancialGovernance.ComputeTaxOnSubtotal`.
The rate is preserved while the switch is off, so toggling back on restores it.

The migration `20260903235611_AddTaxEnabledToBusinessSettings` is generated and applied.
Verified by use: the cart, the receipt and finalize now agree on the same figure.

**Outstanding:** `ARCHITECTURE_DEBT_REPORT.md` §8 still describes the old split and needs
correcting on the next debt review.

### 2. Governance tooling — **refreshed 2026-09-05**

The scan was re-run and the report rewritten. Findings:

- `posDbContextInjectionCount` and `repositoryInjectionCount` are both **0**; the report had been
  listing eight and four controllers respectively. Ceilings were 16 and 46 — decoration rather
  than guardrails — and are now 0, so any reintroduction fails CI.
- `unversionedControllerCount` is **1** (`DevicesController`); ceiling tightened 4 → 1.
- `allowAnonymousCount` is **9**, all legitimate, but **none had a rate limit**. Fixed; see §3a
  of the debt report.
- Trend baselines dated from May and had been exceeded for months, so CI printed warnings nobody
  read. Re-anchored to the current scan.

Controllers grew 14 → 29 over the same period while coupling went to zero.

### 3. Security advisories — **closed 2026-09-05**

Five, not the two that were visible. `AutoMapper` (removed, entirely unused),
`Microsoft.Extensions.Caching.Memory` 8.0.1, `Npgsql` 8.0.3, and `System.Text.Json` 8.0.5 covering
two CVEs. The Npgsql one was an integer overflow enabling SQL injection in the database driver.

Three of the five were invisible until `NuGetAuditMode` was set to `all`, because NuGet audits
only direct references by default and all three arrived transitively. `NuGetAudit` now fails CI
builds on moderate-and-above advisories; the previous only gate was Trivy on the Docker image,
which does not inspect NuGet packages.

### 4. Controller versioning — **closed 2026-09-05**

`DevicesController` was the last unversioned controller and now carries both routes. The
governance allowlist is empty and the budget ceiling is 0.

Worth noting how it was found: the allowlist named four controllers, but three had been versioned
long ago. A stale exemption is indistinguishable from a live one, and would have let any of those
three regress unnoticed.

### 5. Two sync processors are placeholders — **closed 2026-09-05 (Step 127)**

`ProcessOpenShift` and `ProcessCreateCustomer` persisted nothing and returned `Success = true`
with "Shift opened successfully" and "Customer created successfully". A client receiving that
clears the operation from its outbox: nothing written server-side, nothing left client-side.

It was never live. The shipped Android client enqueues only `AdjustInventory`, `RecordWastage`
and `FinalizeOrder`; shift and customer actions go straight to the API and are refused honestly
when offline. So this was a trap for whoever next tried to make shifts work offline, not a defect
in service today.

Both now return `Success = false` naming the cause, and audit at Warning rather than Information.
`PlaceholderProcessorGovernanceTests` fails the build if either claims success again. The real
`OpenShiftCommand` dispatch was deliberately not written: no client needs it, and failing loudly
removes the risk class at no cost.

### 6. Release builds logged nothing — **closed 2026-09-05 (Step 128)**

`TannousPosApplication` planted a Timber tree only under `BuildConfig.DEBUG`, so in production
every `Timber.w` and `Timber.e` was discarded. A tablet failing mid-service left no trace, which
is why the Step 121-123 defects needed someone watching the screen.

Release builds now plant `FileLogTree`: WARN and above to `Android/data/com.tannous.pos/files/logs`,
one file per day, 7-day retention, 2 MB cap per file. A file rather than an endpoint on purpose —
the failures worth diagnosing are offline sync and printer faults, and a sink that needs an API
call cannot report that it could not reach the API.

Crashlytics was evaluated and rejected for one restaurant with one tablet. Firebase was then
removed from the project entirely: two dead Kotlin files that imported it and were referenced
nowhere, three SDKs compiled into the APK doing nothing, and six version-catalog entries. It had
been inert since the initial commit and looked configured.

### 7. Built but never tested against reality

- **WhatsApp / SMS notifications** (Step 96): built, never tested against a real device.
- **Play Store**: deferred by decision on 2026-09-05, not outstanding. The app is installed
  as a signed APK on tablets the restaurant owns, so the store's screenshots, privacy policy,
  data-safety disclosure and review cycles buy nothing yet. The developer account is paid for
  (one-time) and kept; `mobile/PLAY_STORE_READINESS.md` holds the route for when the POS is sold
  to a second restaurant.

---

## Settled, do not reopen

See `TODO.md` for the full statements.

- **Receipts print English only.** Thermal printers cannot shape Arabic.
- **Add-on names and order notes stay English.** Latin-script Lebanese ("bala toum") covers
  the need without an `AddOn.NameAr` migration.
- **KDS is already localized.** An older TODO claiming otherwise was wrong.
- **Printing has exactly one path.** `core/printing`: `ReceiptRenderer` builds rows from the
  server's `ReceiptDto`, `PrinterService` owns transport only. See `PRINTING.md`.

---

## Found by using the app (Steps 121-123)

Three defects surfaced in a single evening of manual testing, all the same shape: a rule that
existed in more than one place, where one copy was wrong and nothing compared them.

- **Cart total excluded tax**, so the cashier collected the pre-tax amount and finalize
  rejected it. The receipt had the rule right; the cart did not.
- **Tax rounded at 28 decimal places**, so an order total of 1.665 could not be tendered at
  all. Exact payment was impossible; only overpaying and taking change completed a sale.
- **Split bill had never worked.** `OrderStatus.Open` is assigned nowhere, yet the split
  query, split payment and both void gates tested for it alone. Finalize, KDS and floor plans
  all tested `Open || Pending` correctly.
- **Finalizing a fully-paid split was rejected** because the request validator demanded at
  least one payment, while the split flow had already recorded them all individually.

Each is now behind a single named rule — `BusinessSettings.TaxApplies`,
`OrderFinancialGovernance.ComputeTaxOnSubtotal`, `OrderStatus.IsUnsettled()` — with governance
tests that fail the build if the rule forks again.

The lesson for planning: these are integration seams, invisible to unit tests and to code
review, and they were found by operating the till. That is an argument for prioritising real
use over more analysis.

## Suggested order

Items 1 through 5 above are closed. What is left is the part that was never about code.

1. **Secure the release keystore.** `mobile/keystore/tannous-pos-release.jks` (alias
   `tannous-pos-key`) signs every build, and Android will not replace an installed app with one
   signed by a different key: losing it means wiping and reinstalling every tablet, and losing the
   local database with it. Two things to do — keep a backup off this machine, and move the file
   out of the working tree. It is gitignored, but it sits inside the repo where a `git clean -xdf`
   would delete it. This is the only unrecoverable mistake currently available.
2. **Run a real service on it.** Every defect worth having found so far came from operating the
   till, not from reading it. Nothing on this list will teach as much as one full evening.
3. **Test WhatsApp against a real phone** (Step 96). Built, never once exercised — and read the
   template-approval note in `TODO.md` first, because a passing sandbox test proves less than it
   looks like it does.
