# Receipt Printing in Tannous POS

This document describes the receipt printing implementation in the Android app, including the current implementation and future extension plans.

## Current Implementation

### Android Print Framework (SystemPrintPrinter)

The app currently uses **Android Print Framework** (`PrintManager`) to print receipts. This implementation:

- ✅ Works on Android emulator (no physical printer required)
- ✅ Opens system print preview dialog
- ✅ Allows saving as PDF
- ✅ Supports printing to physical printers via the system print service
- ✅ Works with any printer supported by Android

**Implementation Details:**
- Uses `WebView` to render HTML-formatted receipt
- Creates `PrintDocumentAdapter` from WebView
- Opens system print dialog via `PrintManager.print()`
- Receipt is formatted as HTML with CSS for proper printing

### Architecture

The printing system follows Clean Architecture principles:

```
┌─────────────────────────────────────┐
│         ReceiptScreen (UI)          │
│  ┌──────────┐  ┌──────────┐        │
│  │  Print   │  │  Share   │        │
│  └────┬─────┘  └────┬─────┘        │
└───────┼─────────────┼──────────────┘
        │             │
┌───────▼─────────────▼──────────────┐
│      ReceiptViewModel               │
│  - Handles print/share logic        │
│  - Manages print state              │
└───────┬─────────────────────────────┘
        │
┌───────▼─────────────────────────────┐
│      Printer (Interface)            │
│  - printReceipt(receipt): Result    │
└───────┬─────────────────────────────┘
        │
┌───────▼─────────────────────────────┐
│   SystemPrintPrinter (Current)      │
│   - Android Print Framework         │
│                                     │
│   Future implementations:           │
│   - EscPosBluetoothPrinter          │
│   - EscPosNetworkPrinter            │
└─────────────────────────────────────┘
```

### Key Components

#### 1. Printer Interface (`Printer.kt`)

Abstract interface for all printer implementations:

```kotlin
interface Printer {
    suspend fun printReceipt(receipt: ReceiptToPrint): PrintResult
}
```

#### 2. ReceiptToPrint Model

Data class containing all receipt information:

```kotlin
data class ReceiptToPrint(
    val orderNumber: String?,
    val receiptNumber: String?,
    val dateTime: String,
    val items: List<ReceiptItem>?, // Null if not available (offline)
    val subtotal: String,
    val tax: String,
    val total: String,
    val payments: List<ReceiptPayment>,
    val footerText: String? = null
)
```

#### 3. PrintResult

Sealed class for print operation results:

```kotlin
sealed class PrintResult {
    data object Success : PrintResult()
    data class Failed(val message: String) : PrintResult()
}
```

#### 4. ReceiptFormatter

Utility for converting `OrderDto` to `ReceiptToPrint`:

```kotlin
object ReceiptFormatter {
    fun formatReceipt(
        order: OrderDto,
        items: List<ReceiptItem>?,
        payments: List<ReceiptPayment>
    ): ReceiptToPrint
}
```

## Testing on Emulator

### Steps to Test Printing

1. **Finalize an order** from the Sell screen
2. **Navigate to ReceiptScreen** (automatically shown after finalization)
3. **Tap "Print" button**
4. **System print dialog appears** with:
   - Print preview
   - Options to save as PDF
   - Options to select printer (if available)
   - Print settings (pages, color, etc.)

### Expected Behavior

- ✅ Print dialog opens successfully
- ✅ Receipt preview shows correctly formatted content
- ✅ Can save receipt as PDF
- ✅ Print job can be cancelled
- ✅ Loading state shows while generating print job
- ✅ Success/error messages displayed via Snackbar

### Share Receipt

The "Share" button allows sharing the receipt as plain text:

1. Tap "Share" button on ReceiptScreen
2. Android share sheet appears
3. Select app (WhatsApp, Email, etc.)
4. Receipt text is shared

## Offline Behavior

When an order is finalized offline:

- Receipt can still be printed with available data
- Items list may be `null` (shows "Minimal receipt" notice)
- Order number, totals, and payment info are available
- Full receipt available after sync

## Future Implementation: Thermal Printers (ESC/POS)

### Planned Architecture

The current `Printer` interface is designed to support multiple printer types:

```kotlin
// Current implementation
class SystemPrintPrinter : Printer { ... }

// Future implementations
class EscPosBluetoothPrinter : Printer { ... }
class EscPosNetworkPrinter : Printer { ... }
```

### ESC/POS Implementation Plan

1. **Create ESC/POS Printer Implementations**

   ```kotlin
   class EscPosBluetoothPrinter(
       private val bluetoothAdapter: BluetoothAdapter
   ) : Printer {
       override suspend fun printReceipt(receipt: ReceiptToPrint): PrintResult {
           // Connect to Bluetooth printer
           // Send ESC/POS commands
           // Handle printer responses
       }
   }
   
   class EscPosNetworkPrinter(
       private val host: String,
       private val port: Int
   ) : Printer {
       override suspend fun printReceipt(receipt: ReceiptToPrint): PrintResult {
           // Connect to network printer
           // Send ESC/POS commands via TCP/IP
           // Handle printer responses
       }
   }
   ```

2. **ESC/POS Command Formatting**

   - Convert `ReceiptToPrint` to ESC/POS commands
   - Handle printer initialization
   - Format text with proper alignment, fonts, sizes
   - Send paper cut commands

3. **Printer Selection**

   Use Dependency Injection with qualifiers or factory pattern:

   ```kotlin
   @Provides
   @Singleton
   @Named("BluetoothPrinter")
   fun provideBluetoothPrinter(): Printer { ... }
   
   @Provides
   @Singleton
   @Named("NetworkPrinter")
   fun provideNetworkPrinter(): Printer { ... }
   ```

   Or use a factory pattern:

   ```kotlin
   @Provides
   @Singleton
   fun providePrinter(
       printerType: PrinterType
   ): Printer {
       return when (printerType) {
           PrinterType.SYSTEM -> SystemPrintPrinter(context)
           PrinterType.BLUETOOTH -> EscPosBluetoothPrinter(...)
           PrinterType.NETWORK -> EscPosNetworkPrinter(...)
       }
   }
   ```

4. **Printer Configuration**

   Add printer settings in the app:
   - Printer type selection (System/Bluetooth/Network)
   - Bluetooth device selection
   - Network printer IP/port configuration
   - Printer-specific settings (paper width, character encoding, etc.)

5. **Existing Infrastructure**

   The codebase already has some ESC/POS infrastructure:
   - `PrintingManager.kt` - Contains ESC/POS command constants
   - `ReceiptPrintManager.kt` - Has receipt formatting logic
   - These can be refactored to work with the new `Printer` interface

### Migration Path

1. Keep `SystemPrintPrinter` as default
2. Add ESC/POS implementations alongside
3. Add printer selection UI in settings
4. Allow users to switch between printer types
5. Deprecate old `PrintingManager`/`ReceiptPrintManager` if needed

## Receipt Format

### Current Format (HTML)

- Header: "TANNOUS POS"
- Order/Receipt numbers
- Date/Time
- Items list (if available)
- Subtotal, Tax, Total
- Payment methods
- Footer message

### Future ESC/POS Format

Will follow similar structure but use ESC/POS commands:
- ESC/POS initialization
- Header formatting (double height, bold, center)
- Item lines with proper alignment
- Totals section
- Payment section
- Paper feed and cut

## Dependencies

### Current
- Android Print Framework (built-in)
- WebView (for HTML rendering)

### Future (ESC/POS)
- Bluetooth permissions and APIs (for Bluetooth printers)
- Network/TCP sockets (for network printers)
- ESC/POS command library (optional, can implement manually)

## Error Handling

### Print Errors

- **Print service unavailable**: Shows error message
- **WebView rendering failure**: Shows error message
- **User cancellation**: Silently handled
- **Network errors (future)**: Retry logic for network printers

### Offline Scenarios

- Receipt printing works with available data
- Shows warning if full receipt data not available
- Full receipt printed after sync

## Code Locations

- **Printer Interface**: `mobile/core/src/main/java/com/tannous/pos/core/printing/Printer.kt`
- **SystemPrintPrinter**: `mobile/core/src/main/java/com/tannous/pos/core/printing/SystemPrintPrinter.kt`
- **ReceiptFormatter**: `mobile/core/src/main/java/com/tannous/pos/core/printing/ReceiptFormatter.kt`
- **ReceiptViewModel**: `mobile/feature/sell/src/main/java/com/tannous/pos/feature/sell/ReceiptViewModel.kt`
- **ReceiptScreen**: `mobile/feature/sell/src/main/java/com/tannous/pos/feature/sell/ReceiptScreen.kt`
- **DI Module**: `mobile/core/src/main/java/com/tannous/pos/core/di/PrintingModule.kt`

## Troubleshooting

### Print dialog doesn't open

- Check if PrintManager service is available
- Ensure app has proper context
- Check logs for WebView errors

### Receipt formatting issues

- Check HTML generation in `generateReceiptHtml()`
- Verify CSS styles for print media
- Test on different screen sizes

### Share not working

- Check if Intent.ACTION_SEND is available
- Verify receipt text generation
- Check device share capabilities


