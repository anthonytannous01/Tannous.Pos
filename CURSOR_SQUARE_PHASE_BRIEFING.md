# Cursor Briefing — Square POS Phase
## Context & Codebase State for Upcoming Feature Sprint

---

## Who You Are Working With

This is **Tannous POS** — a full-stack, production-targeting restaurant POS system built for the **Lebanese market**. The system consists of:

- A **.NET 8 / ASP.NET Core backend** (Clean Architecture + CQRS/MediatR + PostgreSQL via EF Core)
- A **Kotlin Android mobile app** (modular MVVM, Hilt DI, Room, Retrofit, offline-first)

The codebase is at **step 101** — everything below has been built and validated.

---

## What Has Already Been Built (Step 101 State)

### Backend — Domain Entities (all in `Tannous.Pos.Domain/Entities/`)
Every entity below is persisted, migrated, and has working API endpoints:

| Entity | What It Does |
|---|---|
| `Order`, `OrderLine`, `OrderLineAddOn` | Full order lifecycle — create, update, finalize, void |
| `Payment`, `PaymentRefund` | Multi-method payments with refunds |
| `MenuItem`, `Category`, `AddOn` | Bilingual catalog (`Name` + `NameAr`) |
| `Customer` | Customer database |
| `LoyaltyAccount`, `LoyaltyTransaction` | Points accrual and redemption |
| `Ingredient`, `Recipe`, `RecipeLine` | Multi-level recipe management |
| `InventoryItem`, `InventoryMovement`, `WastageRecord` | Inventory tracking |
| `PurchaseOrder`, `PurchaseOrderLine`, `GoodsReceipt`, `GoodsReceiptLine` | Purchasing flow |
| `Supplier` | Supplier management |
| `Shift`, `CashDrawerEvent` | Shift management |
| `Table`, `FloorPlan` | Table management with floor plans |
| `Reservation` | Table reservation system |
| `FeedbackSubmission` | Customer feedback |
| `Branch` | Multi-branch support |
| `Device`, `Printer` | Hardware management |
| `BusinessSettings` | Central config (tax rate, currency, LBP/USD exchange rate, stamp duty, loyalty settings, Arabic name) |
| `User`, `RefreshToken` | Authentication (JWT + OpenIddict) |
| `AuditEvent`, `OperationalAuditRecord` | Comprehensive audit trail |
| `SyncConflictRecord`, `SyncCursor`, `SyncOperationReceipt` | Offline-sync infrastructure |
| `PriceChangeLog` | Price history |
| `DeliveryInfo` | Delivery module |

### Backend — API Controllers (all in `Tannous.Pos.WebApi/Controllers/`)
Working REST endpoints exist for: Auth, Admin, Branches, Catalog, Customers, Delivery, Devices, Feedback, Inventory, KDS, Kiosk, Loyalty, Menu (public QR), Orders, Printing, Reports (COGS + EOD + Sales/Purchases CSV export + Menu Engineering), Reservations, Settings, Shifts, Suppliers, Sync, Tables, Users.

### Backend — Lebanese Market Compliance (all built)
- **VAT 11%** — applied on order finalization, appears on receipts
- **USD Stamp Duty ($2)** — toggle in `BusinessSettings`, applies per-receipt on USD transactions
- **Dual currency LBP/USD** — configurable exchange rate, receipts show LBP equivalent
- **Arabic receipts** — `ReceiptFormatter.formatReceiptText(isArabic: bool)` switches all labels and item names
- **Lebanon Quick Setup preset** — one-tap: VAT=11%, LBP display ON, stamp duty ON

### Backend — Notifications
- **WhatsApp via Twilio** — order confirmation on finalize. Built but untested against a real device (sandbox only). `INotificationService` abstraction with a Twilio implementation.

### Android App — Modules (`mobile/feature/`)
| Module | What It Has |
|---|---|
| `feature/sell` | SellScreen (cart, bilingual), ReceiptScreen (bilingual item names), KdsScreen (bilingual, status advancement), KioskScreen (fullscreen self-order, configurable exit PIN), OrderReceiptViewModel |
| `feature/settings` | SettingsScreen (all business settings, language toggle, LBP/USD config, Lebanon preset, **kiosk exit PIN field**) |
| `feature/reports` | ReportsScreen (COGS, EOD, export tabs) |
| `feature/inventory` | InventoryScreen |
| `feature/printing` | PrintingPreviewScreen |
| `feature/auth` | LoginScreen |
| `feature/customers` | CustomersScreen |
| `feature/shifts` | ShiftsScreen |

### Android App — Core Infrastructure (`mobile/core/`)
- Room local DB with full schema
- Retrofit network layer (`ApiServices.kt` — all endpoints wired)
- `SettingsRepository` — key-value store for language (`"en"`/`"ar"`), currency, exchange rate, kiosk PIN
- `SettingsRepository.KEY_KIOSK_PIN` — configurable exit PIN (default `"1234"`)
- `LocalIsArabic` — Compose composition local that flows language state to all screens
- Offline-first outbox sync (`SyncWorker`, `OutboxDao`, `OutboxRepository`)
- `ReceiptPrintManager` + `ReceiptFormatter` — print + share receipt with bilingual support
- `KioskViewModel` — injects `SettingsRepository`, exposes `exitPin: StateFlow<String>`

### Android App — Navigation (`TannousPosApp.kt`)
Routes: `sell`, `receipt/{orderId}`, `settings`, `reports`, `inventory`, `kds`, `printing`, `customers`, `shifts`, `dashboard`, `menu-engineering`, `tables`, `qr-menu`, `reservations`, `delivery`, `kiosk`

---

## Architecture Rules (Must Follow)

These are enforced via `CURSOR_RULES.md` and architecture tests:

1. **Clean Architecture layers** — `WebApi` → `Application` → `Domain`. `Infrastructure` implements persistence.
2. **CQRS / MediatR** — business use cases go in `*Command` / `*Query` + `*Handler` classes in `Application`. Controllers stay thin.
3. **FluentValidation** — every command/query that takes user input has a `*Validator`.
4. **DTOs at boundaries** — never expose EF entities from API endpoints.
5. **Naming** — `*Command`, `*Query`, `*Handler`, `*Validator`, `*Dto`, `I*` for interfaces. Async methods end with `Async`.
6. **No direct `DbContext` in controllers** for new work — go through MediatR handlers.
7. **Android: Hilt DI** — inject everything; no manual `new` on ViewModels or repositories.
8. **Android: `StateFlow` + `collectAsStateWithLifecycle()`** — standard ViewModel → UI pattern.
9. **Android: `LocalIsArabic.current`** — always check this in Composables that display text content. Use `nameAr?.takeIf { it.isNotBlank() } ?: name` pattern for bilingual display.
10. **Commit discipline** — `feat|fix|refactor|chore(scope): description — Step N` format. Push after every green step.

---

## Gap Analysis Context — Why This Phase Exists

We completed a full **Square POS gap analysis** (`Square_POS_Gap_Analysis.docx` in repo root).

### The Key Finding
Square is the world's benchmark mobile POS — praised for clean UX, deep ecosystem, and AI features. **But Square is not available in Lebanon.** Payment processing is restricted to 8 countries (US, Canada, Australia, Japan, UK, Ireland, France, Spain). No Arabic, no LBP/USD, no VAT compliance.

**Tannous POS is therefore the Square-quality POS for Lebanon.** The goal of this phase is to close the remaining feature gaps between Tannous and Square's best capabilities, while keeping every Lebanese-market advantage we've already built.

### Where Tannous Already Beats Square
| Feature | Square | Tannous |
|---|---|---|
| Available in Lebanon | ❌ | ✅ |
| Arabic language support | ❌ | ✅ Full bilingual |
| Dual currency LBP/USD | ❌ | ✅ |
| Lebanese tax compliance | ❌ | ✅ |
| WhatsApp notifications | ❌ | ✅ (built, untested) |
| Self-ordering kiosk | Requires extra hardware | ✅ Built-in |
| Menu engineering analytics | AI-assisted (paid tier) | ✅ Stars/Plowhorses matrix |
| Per-transaction fees | 2.6%+ always | ✅ None |

### Where Square Beats Tannous (The Gaps We Will Close)

**Tier 1 — Highest Impact (Build Next)**

1. **Loyalty CRM campaigns & customer analytics**
   - What Square has: customer segmentation by behaviour, automated email/SMS campaigns, purchase history dashboard, loyalty promotion performance tracking
   - What Tannous has: `LoyaltyAccount` + `LoyaltyTransaction` entities, points accrual/redemption, `LoyaltyController` endpoint
   - What's missing: purchase segmentation query, campaign entity, WhatsApp campaign dispatch, customer analytics dashboard on Android
   - Architecture hook: extend `Loyalty` bounded context; add `GetCustomerSegmentsQuery`, `LoyaltyCampaign` entity; trigger WhatsApp via existing `INotificationService`

2. **Delivery channel API — Toters & Talabat integration**
   - What Square has: DoorDash, Uber Eats, Grubhub via Deliverect/Cuboh — all orders unified on one POS screen and KDS
   - What Tannous has: `DeliveryInfo` entity, `DeliveryController`, delivery module on Android
   - What's missing: `IDeliveryChannelAdapter` abstraction, Toters API implementation, Talabat API implementation, webhook ingest for incoming orders
   - Architecture hook: new `DeliveryIntegration` bounded context; `IncomingDeliveryOrderCommand` flows into existing Order creation

3. **AI Demand Forecasting**
   - What Square has: predicts ingredient usage, staffing needs, busy periods using historical + external data (weather, events)
   - What Tannous has: full order history, recipe-to-ingredient mapping, COGS data
   - What's missing: `GetDemandForecastQuery`, a forecasting engine (start rule-based: day-of-week × time-of-day × rolling average), surface as "Smart Suggestions" card on owner dashboard

**Tier 2 — Next Quarter**

4. **Employee scheduling & time tracking**
   - What Square has: drag-and-drop shift builder, clock-in/out, tip pooling, Square Team app, sales-vs-labour reports
   - What Tannous has: `Shift` entity (shift-level), `User` management
   - What's missing: `Schedule` entity (planned shifts), `TimeEntry` entity (actual clock-in/out), tip distribution rules, Android scheduling screen

5. **Kitchen performance analytics**
   - What Square has: completed ticket count, average ticket time per station, throughput per hour
   - What Tannous has: `KdsStatus` timestamps on `OrderLine` (`KdsAcknowledgedAt`, `KdsDoneAt`), `KdsController`
   - What's missing: `GetKdsPerformanceQuery` — compute avg ticket time, P90 ticket time, throughput per hour. Zero new entities needed.

6. **KDS station routing**
   - What Square has: route specific menu items to specific kitchen stations (grill, cold prep, fry)
   - What Tannous has: single KDS view, `KdsTicketDto` with `MenuItemName`/`MenuItemNameAr`
   - What's missing: `KdsStation` entity linked to `MenuItem`; KDS screen filtered by station

7. **WhatsApp loyalty & reservation notifications**
   - What Square has: SMS/email on loyalty events and reservation confirmations
   - What Tannous has: `INotificationService` + Twilio impl, wired for order confirmations
   - What's missing: trigger on `LoyaltyTransaction` (points earned notification), trigger on `Reservation` creation (confirmation message)

8. **Accounting software sync (QuickBooks / Xero)**
   - What Square has: native QuickBooks + Xero integration — daily sales auto-pushed
   - What Tannous has: CSV export endpoints (`/reports/export/sales.csv`, `/reports/export/purchases.csv`)
   - What's missing: `IAccountingSync` abstraction, QuickBooks Online OAuth2 connector, daily push job

**Tier 3 — Future Sprints**

9. **Open API / webhook connector layer** — expose `WebhookSubscription` entity and event dispatcher so third parties can integrate without Tannous building each connector
10. **Supplier intelligence** — from recipe + sales forecast → predicted ingredient demand → purchase order suggestions
11. **Section sales reporting** — revenue breakdown by `FloorPlan` section (dining room, bar, patio) using existing `Table` + `FloorPlan` entities
12. **ESC/POS printer certification** — validate against Epson TM-T88 and Star TSP100 (most common in Lebanon)

---

## What This Phase Is NOT

- Not rebuilding anything that works. Every feature at step 101 is production-quality.
- Not changing the Clean Architecture or Android MVVM patterns — extend them.
- Not adding payment processing (Tannous uses hardware terminals; no per-transaction fees by design).
- Not building Square's hardware ecosystem — Tannous is Android-tablet-first.

---

## Files to Know

| File | Why It Matters |
|---|---|
| `CURSOR_RULES.md` | Architecture contract — read before writing any code |
| `Tannous.Pos.Domain/Entities/BusinessSettings.cs` | Central config entity — new toggles go here |
| `Tannous.Pos.Domain/Entities/Order.cs` | Core aggregate — handle with care |
| `Tannous.Pos.Domain/Entities/LoyaltyAccount.cs` | Loyalty aggregate to extend |
| `Tannous.Pos.Infrastructure/PosDbContext.cs` | EF Core context — all new entities registered here |
| `mobile/core/.../data/repository/SettingsRepository.kt` | Local key-value store — language, PIN, currency |
| `mobile/core/.../data/model/ApiModels.kt` | All Retrofit DTOs — add new endpoint models here |
| `mobile/core/.../ui/LocalIsArabic.kt` | Bilingual state — always use in Composables |
| `mobile/feature/sell/KioskScreen.kt` | Self-order kiosk — reads `exitPin` from ViewModel |
| `Square_POS_Gap_Analysis.docx` | Full research document — reference for feature specs |
| `Omega_POS_Gap_Analysis.docx` | Lebanese market competitive analysis — reference for priorities |

---

## The North Star

> Tannous POS should be the system a Lebanese restaurant operator chooses when they see Square reviews and think "I wish I could have that — but it works in Lebanon, costs less per transaction, and speaks Arabic."

Every feature added in this phase should make that sentence more true.

---

*Briefing generated June 2026 — Step 101 baseline. Next step: Step 102.*
