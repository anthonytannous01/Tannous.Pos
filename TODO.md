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

## Arabic/RTL — KDS screen (partial)

**Status:** Partial ✅ — SellScreen + QR menu done. KDS not yet Arabic.

**Receipts are English-only by decision, not by omission.** Thermal printers cannot
shape Arabic text; the previous bitmap-rendering approach was removed deliberately.
`ReceiptDto` still carries `nameAr`/`footerMessageAr` for the app UI, but the printer
ignores them. See PRINTING.md. Do not re-add Arabic to receipts.

KDS remains a follow-up.

---

*Add new items here as they come up. Remove or tick off once validated.*
