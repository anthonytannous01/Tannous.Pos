# Tannous POS — Project Status

_Assessed 2026-09-04, after Step 123._

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
| Unit tests in the mobile project | 12 (all in `ReceiptRendererTest`) |

Essentially no debt is hiding in comments. The governance discipline worked.

---

## What is actually outstanding

### 1. Tax configuration — **fixed in Step 119, pending migration**

Was: the Android **Enable Tax** toggle was ignored by the backend, order creation applied a
hardcoded 10%, and the kiosk path had a third copy of the tax rule. Finalize was already
settings-driven, so finalized totals were correct while pre-finalize displays were not.

Now: `BusinessSettings.TaxEnabled` is a real persisted column and `BusinessSettings.TaxApplies`
(`TaxEnabled && TaxRate > 0`) is the single rule. All four order paths — create, finalize,
kiosk, and the receipt label — go through `OrderFinancialGovernance.ComputeTaxOnSubtotal`.
The rate is preserved while the switch is off, so toggling back on restores it.

**Outstanding:** the EF migration has not been generated or applied. Until it is, the backend
will not build against the database. `ARCHITECTURE_DEBT_REPORT.md` §8 still describes the old
split and needs correcting on the next debt review.

### 2. Governance tooling has gone stale

`governance/debt-report.json` was generated **2026-05-29**; `ARCHITECTURE_DEBT_REPORT.md`
was last reviewed **2026-05-16**. Kiosk, dual-currency drawer, delivery, loyalty and the
printing rework all landed after that. §1 of the debt report lists eight controllers with
direct `PosDbContext` access; the scan reports 2 and `IMPROVEMENT_REPORT` records the
allowlist reaching zero at Step 61.

`syncReplayRiskCommentCount` is 36 against a soft baseline of 27, so CI is emitting
warnings nobody is reading. Re-run `governance/scan-debt.ps1` and re-baseline.

### 3. Two known security advisories accepted but never revisited

- **AutoMapper 12.0.1** — NU1903, GHSA-rvv3-g6hj-g44x. Documented as accepted debt in §12
  with an upgrade "planned". Still 12.0.1.
- **Microsoft.Extensions.Caching.Memory 8.0.0** — NU1903, GHSA-qj66-m88j-hmgj. Not recorded
  anywhere; it surfaces only in build output. The stale debt report tracks the first and not
  the second.

Both are high severity. Worth one dependency-upgrade step with a regression pass.

### 4. Four controllers outside the versioning convention

`Suppliers`, `Inventory`, `Reports` and `Devices` expose unversioned routes only. A client
assuming `/api/v1.0/` breaks against them. Allowlisted in
`ControllerVersioningGovernanceTests`, so CI will not catch drift here.

### 5. Two sync processors are placeholders

`OpenShift` and `CreateCustomer` return placeholder success in the sync push path. Durable
replay protects them from duplicate application, but confirm they actually apply the
operation rather than silently accepting it.

### 6. Built but never tested against reality

- **WhatsApp / SMS notifications** (Step 96): built, never tested against a real device.
- **Play Store**: nothing done. ~25 unchecked items — screenshots, feature graphic, privacy
  policy, data-collection disclosure, terms, device and screen-size testing, pre-launch
  report. Calendar-bound work that cannot be compressed at the end.

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

1. **Finish tax configuration.** It is small, it is money, and it blocks selling to any
   business with different tax rules.
2. **Re-run the debt scan** and correct `ARCHITECTURE_DEBT_REPORT.md`. Cheap, and it
   re-anchors every other judgement on this list.
3. **Play Store preparation.** Start early; screenshots, policy pages and review cycles are
   slow in a way code is not.
4. AutoMapper upgrade, controller versioning, placeholder processors — real but not urgent.
