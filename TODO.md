# Tannous POS — Pending Validation & Testing Debt

Items that are **built but not yet tested** against a real device or third-party service.

---

## WhatsApp / SMS Notifications (Step 96)

**Status:** Built ✅ — Not tested against a real device ⏳

**To test when a device is available:**
1. Open WhatsApp → send `join <sandbox-keyword>` to `+1 415 523 8886`
   (find keyword at console.twilio.com → Messaging → Try it out)
2. Create an order with `customerPhone` set to your WhatsApp number
3. Finalize the order → confirm WhatsApp message arrives
4. Check delivery status in Twilio Console → Messaging → Logs

**To go production:** Apply for a WhatsApp Business sender, update `FromNumber` in `appsettings.Development.json`.

---

## Play Store Release / APK Signing

**Status:** Planned ⏳

ProGuard rules, release keystore, store listing, screenshots.

---

## Arabic/RTL — settled, no work pending

**Status:** Done ✅ — closed 2026-09-04.

**KDS is localized.** Titles, column headers, action buttons, empty state, elapsed-time
labels, station chips (`stationNameAr`) and item names (`menuItemNameAr`) all switch with
the language setting, and RTL mirroring is handled app-wide by `LocalLayoutDirection` in
`TannousPosApp`. An earlier note here claiming KDS was "not yet Arabic" was out of date.

**Add-on names stay English by decision.** `AddOn` has no `NameAr` in the domain model,
and adding one would need an entity change, a generated migration, DTO changes on both
sides, an admin UI field, and sync handling — all so operators could type Arabic that the
kitchen reads more slowly. Kitchen staff are comfortable in English, and add-on names are
free text, so Latin-script Lebanese ("bala toum") covers the need with no code at all. The
same applies to the order notes field. Do not build `AddOn.NameAr` without a new reason.

**Receipts are English-only by decision.** Thermal printers cannot shape Arabic text; the
bitmap-rendering workaround was removed deliberately. `ReceiptDto` still carries
`nameAr`/`footerMessageAr` for the app UI, but the printer ignores them. See PRINTING.md.

Known cosmetic gap, deliberately not fixed: the KDS order-type badge renders the raw
backend string (`DINE-IN`, `TAKEAWAY`) untranslated, and `DashboardScreen` duplicates its
own inline Arabic labels for the same values. Worth folding into a shared helper in `core`
if order-type labels are ever touched for another reason.

---

*Add new items here as they come up. Remove or tick off once validated.*
