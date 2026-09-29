# Service Runbook — Tannous POS

Operating instructions for a real service on the production build. One tablet, one server on the
restaurant's own Wi-Fi, direct APK install.

**This file is the source of truth.** A readable copy is published as an artifact for reading on a
phone during service; when behaviour changes, change this file and republish that copy, not the
other way round.

| | |
|---|---|
| Server | `http://192.168.10.231:7000/` (root, not `/swagger`) |
| App on the tablet | **Tannous POS** — not "Tannous POS (dev)" |
| Log on the tablet | `Android/data/com.tannous.pos/files/logs/pos-YYYY-MM-DD.log` |

---

## Before service

Each step depends on the one above it. Do them in order.

1. **Set the Wi-Fi to Private on the server machine.** Windows drops inbound connections on a
   network marked Public and the tablets hang for 30 seconds with no useful error. The setting is
   per network, so a Wi-Fi the machine has not joined before starts as Public. Administrator
   PowerShell:

   ```powershell
   Set-NetConnectionProfile -InterfaceAlias "Wi-Fi 2" -NetworkCategory Private
   ```

2. **Check the address is still `192.168.10.231`** (`ipconfig | Select-String IPv4`). The address is
   compiled into the APK via `API_BASE_URL` in `mobile/local.properties`, so a new lease means a
   rebuild and a reinstall on every tablet, not a setting change. A DHCP reservation on the router
   removes this failure mode.

3. **Start the database.** PostgreSQL runs in Docker, so Docker Desktop first, then:

   ```powershell
   cd C:\Users\user\Tannous.Pos
   .\scripts\start-db.ps1
   ```

   Wait for "Database is ready". The script polls container health rather than just starting it.

4. **Start the API and leave the window open.** That window is the server.

   ```powershell
   dotnet run --project Tannous.Pos.WebApi --launch-profile lan
   ```

   The `lan` profile binds `0.0.0.0:7000`. Plain `dotnet run` uses the `http` profile, which is
   localhost-only, and the tablets cannot reach it.

5. **Confirm it is listening:** `netstat -ano | Select-String ":7000"`.
   `0.0.0.0:7000 LISTENING` is correct. `127.0.0.1:7000` means the wrong profile. Nothing at all
   means the API failed to start, almost always because the database was not up.

6. **Open the server address in the tablet's browser.** This proves the network path before the app
   is involved, so a later failure points at the app rather than the Wi-Fi.

7. **Open the right app.** `com.tannous.pos.dev` is a separate application with its own database.
   Orders rung up there are invisible to the real one.

8. **Print a test receipt, then open the shift.** In that order. Opening a shift requires the
   server and is never queued offline, so a failure here means nothing else will work.

---

## During service

| Situation | What to do |
|---|---|
| **"No connection, and this order was never sent to the server. Reconnect and try again before taking payment."** | **Do not take the money.** The order reached the tablet but not the server. Nothing is lost; it is still on screen. Check the `dotnet run` window, then the tablet's Wi-Fi, then finalize the same order again. This refusal replaced a sale that printed a receipt, took the cash, and never reached the day's totals (Step 131). |
| Receipt number starts with `PENDING#` | **Carry on.** The order was already on the server and the payment is queued. It syncs when the connection returns and the real receipt number replaces this one. |
| Printer will not print | **Do not repeat the sale.** It is already recorded; running it again charges twice. Reprint from the receipt screen or share the text. |
| Shift will not open or close, or a customer will not save | **Not a bug.** These require the server and are never queued, deliberately: a shift the tablet believes is open with no server record makes the day's cash reconcile against nothing. |
| A total looks wrong | **Note the order number and finish the sale.** Re-entering an order under pressure turns one wrong figure into two, and the order number is enough to reconstruct it later. |
| App crashes or will not open | **Reopen it. Never uninstall.** Uninstalling deletes the local database and any queued sales with it. It is the one action on that tablet with no undo. |

---

## After service

- **Pull the log:** `adb pull /sdcard/Android/data/com.tannous.pos/files/logs`. It records WARN and
  above only, so a short or empty file is good news.
- **Check no receipt still reads `PENDING#`.** One that remains means a queued payment never
  synced, and the log is the only place that says why.
- **Reconcile cash against the day's total** and look up any order number noted during service.
- **Close the shift while the server is still running,** then stop the API and the database.

---

## Known limits as of Step 131

- **A failed sync operation is invisible in the app.** Nothing reads `OutboxStatus.FAILED`, so if
  the totals disagree the log file is the only trace. Worth its own step.
- **Orders cannot be created while the server is unreachable.** Deliberate; see
  `OfflineFinalizeGuard`. The alternative lost the sale silently after the money was taken.
- **The server address is compiled into the APK.** A settings screen for it is the better long-term
  answer if this is ever sold to another restaurant.
- **Traffic on the Wi-Fi is unencrypted, including staff logins.** Keep POS devices and the server
  off the guest network. The real fix is HTTPS on the backend; see `PLAY_STORE_READINESS.md`.
- **`MIGRATION_5_6` has never executed.** The prod database was created at v6, so it skips to
  `6_7`. The only v5 database that exists is in the dev app.
