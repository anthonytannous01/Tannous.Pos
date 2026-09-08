# Tannous POS — Pending Validation & Testing Debt

Items that are **built but not yet tested** against a real device or third-party service.

---

## WhatsApp Notifications (Step 96)

**Status:** Built, WhatsApp only. Never tested against a real phone.

SMS was removed in Step 129. The channel switch and the four copied send blocks are gone; there
is one path to Twilio, in `TwilioNotificationService.SendAsync`.

### Read this before testing

WhatsApp does not let a business message whoever it likes. Outside a 24-hour window that opens
only when the *customer* messages the business first, WhatsApp delivers **pre-approved message
templates** and nothing else. Every message this system sends is business-initiated: order
confirmation, points earned, reservation confirmation, loyalty campaign.

Twilio's sandbox hides this completely, because sending `join <keyword>` opens that 24-hour
window for you. So the sandbox test below will pass, and the same code can be rejected in
production until each message has an approved template. Budget for template review, and do not
read a green sandbox test as "WhatsApp works".

### Sandbox test

1. Set `Notifications:Enabled` to true and fill `Notifications:Twilio` in
   `appsettings.Development.json`. `FromNumber` is the bare number, e.g. `+14155238886`; the
   `whatsapp:` prefix is added in code.
2. From your own WhatsApp, send `join <sandbox-keyword>` to `+1 415 523 8886`
   (keyword at console.twilio.com, Messaging, Try it out).
3. Create an order with `customerPhone` set to that same WhatsApp number.
4. Finalize it. The message should arrive within seconds.
5. Check Twilio Console, Messaging, Logs for the delivery status.

If nothing arrives, the server log carries the reason. Look for `Notification observability:`
(channel disabled, credentials missing, or Twilio rejected it) and for the raw Twilio error body,
which names the code: 63016 is the no-approved-template case, 63007 a bad sender, 21211 a
malformed number.

### Going to production

Apply for a WhatsApp Business sender through Twilio, get templates approved for the four message
types, then set `FromNumber` to the approved sender. Note this costs per conversation.

### Known limitation: the app cannot tell you whether it sent

`SendOrderConfirmationAsync` returns a bool, and finalize now logs a warning when it is false, but
that result never reaches the tablet: the finalize response is an `OrderDto` with no room for it.
The receipt screen therefore says "WhatsApp confirmation to <phone>", describing what was
attempted, not what was delivered. It used to say "Confirmation sent", which was a claim it could
not support and which would have read as success even with notifications switched off entirely.

Plumbing the real result through would mean a finalize result type that carries more than the
order. Worth doing if WhatsApp becomes something the restaurant relies on; not worth it before it
has been tested once.

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
