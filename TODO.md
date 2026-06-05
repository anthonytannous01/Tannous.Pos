# Tannous POS — Pending Validation & Testing Debt

Items that are **built but not yet tested** against a real device or third-party service.
Check off each item once validated.

---

## WhatsApp / SMS Notifications (Step 96)

**Status:** Built ✅ — Not tested against a real device ⏳

**What was built:**
- `TwilioNotificationService` fires after order finalization if customer phone is set
- Credentials configured in `appsettings.Development.json` (off git)
- Provider set to `WhatsApp`, sandbox number `+14155238886`

**To test when a device is available:**
1. Open WhatsApp on your phone and send `join <sandbox-keyword>` to `+1 415 523 8886`
   (find the keyword at console.twilio.com → Messaging → Try it out → Send a WhatsApp message)
2. Create an order in the app with `customerPhone` = your WhatsApp number (e.g. `+96103XXXXXX`)
3. Finalize the order
4. Confirm you receive a WhatsApp message from Twilio
5. Verify in Twilio Console → Messaging → Logs → Messages

**To switch to a real WhatsApp Business number (production):**
- Apply for a WhatsApp Business sender at console.twilio.com
- Update `FromNumber` in `appsettings.Development.json` (or environment variable)
- No code changes needed

**Relevant files:**
- `Tannous.Pos.Infrastructure/Services/Notifications/TwilioNotificationService.cs`
- `Tannous.Pos.Infrastructure/Services/Notifications/NotificationSettings.cs`
- `Tannous.Pos.WebApi/appsettings.Development.json` (credentials — not in git)
- `Tannous.Pos.Application/Orders/Commands/FinalizeOrder/FinalizeOrderCommandHandler.cs` (trigger point)

---

## Table Reservation (Tier 3 — not started)

**Status:** Planned ⏳

Builds on Table Management (Step 91). Needs `Reservation` entity, availability query, SMS confirmation on booking.

---

## Arabic Language / RTL Support (Lebanese market requirement)

**Status:** Planned ⏳

Required for: receipts, KDS screens, customer-facing menu (QR menu HTML page).
Gap analysis flagged this as non-negotiable for the Lebanese market.

---

## Play Store Release / APK Signing (Tier 4)

**Status:** Planned ⏳

ProGuard rules, release keystore, store listing, screenshots.

---

*Add new items here as they come up. Remove or check off once validated.*
