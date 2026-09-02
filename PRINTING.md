# Receipt Printing — Tannous POS

Receipts print to ESC/POS thermal printers over Bluetooth or TCP. There is exactly one
printing path; the former Android Print Framework path was removed.

## Architecture

```
ReceiptScreen (Print / Share)
        │
ReceiptViewModel
        │  reportsService.getReceipt(orderId) -> ReceiptDto   (server renders receipt content)
        ▼
PrinterService                     core/printing/PrinterService.kt
   ├─ ReceiptRenderer.rows(dto)    → layout-independent List<ReceiptRow>
   ├─ toEscPos(rows, charsPerLine) → printed via DantSu EscPosPrinter
   └─ toPlainText(rows, ...)       → text used by Share Receipt
        │
   BluetoothConnection(mac)  |  TcpConnection(host, port, 15s)
```

**Single source of truth.** Receipt *content* comes from the backend as `ReceiptDto`. The client
never composes its own totals. Print and Share render the same rows, so shared text always matches
the printed paper — including VAT, discount, stamp duty, and the LBP total.

The VAT **rate** shown is derived from the receipt's own subtotal and tax amount, not read from
current settings. A receipt must stay internally consistent, and a reprint issued after the store
changes its tax rate must not show the new rate against the old amount. When the rate cannot be
computed or rounds to zero, the line prints as `VAT` with no percentage — never `VAT (0%)`.

**Rendering is separated from transport.** `ReceiptRenderer` has no Android or printer dependencies,
so receipt layout is unit-testable on the JVM. `PrinterService` owns only connection and config.

## Components

| File | Responsibility |
|---|---|
| `core/printing/ReceiptRenderer.kt` | `ReceiptDto` → `List<ReceiptRow>` → ESC/POS markup or plain text |
| `core/printing/PrinterService.kt` | Connection, printer config, `PrintResult` |
| `core/printing/TestReceiptFactory.kt` | Representative receipt for the Settings test print |
| `core/data/model/PrinterConfig.kt` | Connection type, BT address, host/port, paper width |
| `feature/settings/printer/PrinterSettingsSection.kt` | Printer settings UI |
| `core/src/test/.../ReceiptRendererTest.kt` | Layout regression tests (58mm and 80mm) |

`PrinterService` lives in `core` because both `feature/sell` and `feature/settings` use it. Do not
move printing back into a feature module — that reintroduces a feature-to-feature dependency.

## Paper width

`ReceiptRenderer.charsPerLine()` maps paper width to characters per line: **58mm → 32**,
**80mm → 48**. Column placement is delegated to the printer's `[L]` / `[C]` / `[R]` parser rather
than hardcoded padding, and separator rules are generated at the configured width. Long item names
are truncated so they cannot collide with the price column on narrow paper.

Never reintroduce fixed-width padding strings — that was the bug that made the 58mm setting
non-functional.

`big` (the business name) is double-**width** as well as double-height, so it costs two columns
per character. `ReceiptRenderer.fitsAtDoubleWidth` drops back to normal width when the name would
overflow — on 58mm that means names longer than 16 characters. Verified from a byte capture:
the printer emits no centering padding for a line it cannot fit, so an over-wide name silently
loses its alignment on paper.

## Language

**Receipts are English only.** Thermal printers cannot shape Arabic text; the previous approach
rendered Arabic as bitmaps, which was removed deliberately. `ReceiptDto` still carries `nameAr`
and `footerMessageAr` for the app UI, but the printer ignores them. The app UI remains bilingual.

## Configuration

Settings → Receipt Printer:

- **Connection** — Bluetooth, LAN, or USB (not implemented)
- **Bluetooth** — picker over paired devices, requires `BLUETOOTH_CONNECT` / `BLUETOOTH_SCAN` on API 31+
- **LAN** — host and port (default **9100**)
- **Paper width** — 58mm or 80mm
- **Print Test Receipt** — prints `TestReceiptFactory.sample()` through the real path

## Testing without a printer

The LAN path needs no hardware. `printFormattedTextAndCut` writes and flushes without reading a
response, so a plain socket sink is indistinguishable from a printer.

1. Run a listener on a machine the device can reach:

   ```python
   import socket
   s = socket.socket(); s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
   s.bind(("0.0.0.0", 9100)); s.listen(1)
   c, a = s.accept()
   with open("receipt.bin", "wb") as f:
       while (d := c.recv(4096)): f.write(d)
   ```

2. Settings → Connection = **LAN**, Host = `10.0.2.2` (emulator → host machine) or the machine's
   LAN IP (physical device), Port = `9100`.
3. **Print Test Receipt**, then repeat from a real finalized order to exercise the full
   `ReceiptDto` → ESC/POS path.
4. Render the capture to see the paper output: `esc2html receipt.bin > out.html`
   (`receipt-print-hq/escpos-tools`).

Layout regressions are cheaper to catch in `ReceiptRendererTest` — it runs on the JVM with no
emulator and no printer. Add a case there before reaching for hardware.

Bluetooth cannot be meaningfully faked; validate it once on a real device.

## Known gaps

- **USB printing** is not implemented; the settings option is disabled.
- **No retry** on transport failure — a failed print surfaces the exception message and the
  cashier retries manually.
- **Kitchen/prep tickets** are not implemented; only customer receipts print.
- **No logo printing** — header is text only.

## Troubleshooting

| Symptom | Check |
|---|---|
| "No network printer host configured" | Host field empty in settings |
| "No Bluetooth printer configured" | No paired device selected, or adapter unavailable |
| Connection timeout | 15s TCP timeout; verify host/port reachable from the device's network |
| Text wraps or columns collide | Paper width setting does not match the physical printer |
| Print succeeds but nothing prints | Printer is not ESC/POS compatible, or expects a different codepage |
