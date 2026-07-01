# Arabic i18n Phase 1 — Core POS Workflow Screens

## Context

The Tannous POS app uses a manual `isArabic` pattern for localization — NOT Android string resources.
The pattern is established in many screens already:

```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
// ...
val isArabic = LocalIsArabic.current
// then: if (isArabic) "عربي" else "English"
```

`LocalIsArabic` is a `compositionLocalOf { false }` provided at root in `TannousPosApp.kt`.
RTL layout direction is also set at root when Arabic — no per-screen layout changes needed.

**Do NOT change any backend calls, data models, navigation, ViewModel logic, or imports beyond the `LocalIsArabic` import. This is a pure UI string change.**

---

## Files to Update

### 1. `feature/shifts/src/main/java/com/tannous/pos/feature/shifts/ShiftsScreen.kt`

Add to imports:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

At the top of `ShiftsScreen` composable body (after existing `val uiState`), add:
```kotlin
val isArabic = LocalIsArabic.current
```

Then replace every hardcoded English string as follows (use `if (isArabic) "AR" else "EN"` pattern):

| English | Arabic |
|---------|--------|
| `"Shift Management"` (TopAppBar title) | `"إدارة الوردية"` |
| `contentDescription = "Back"` | `"رجوع"` |
| `"No Active Shift"` | `"لا توجد وردية نشطة"` |
| `"Open a shift to begin processing sales"` | `"افتح وردية لبدء معالجة المبيعات"` |
| `"Open Shift"` (Button in no-shift state) | `"فتح وردية"` |
| `"Active Shift"` (Card title) | `"الوردية النشطة"` |
| `"Shift Number:"` | `"رقم الوردية:"` |
| `"Opened:"` | `"فُتحت:"` |
| `"Opening Balance:"` | `"رصيد الافتتاح:"` |
| `"Orders This Shift:"` | `"طلبات هذه الوردية:"` |
| `"Sales Total:"` | `"إجمالي المبيعات:"` |
| `"Expected Cash:"` | `"النقد المتوقع:"` |
| `"Notes: $notes"` | `"ملاحظات: $notes"` |
| `"Cash Drop"` (OutlinedButton) | `"إيداع نقدي"` |
| `"Close Shift"` (Button) | `"إغلاق الوردية"` |

---

### 2. `feature/shifts/src/main/java/com/tannous/pos/feature/shifts/ShiftManagementDialog.kt`

Add to imports:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

**`OpenShiftDialog` composable:** add `val isArabic = LocalIsArabic.current` at the top of the composable body.

| English | Arabic |
|---------|--------|
| `"Open Shift"` (Dialog title) | `"فتح وردية"` |
| `label = { Text("Opening Balance") }` | `"رصيد الافتتاح"` |
| `"Please enter a valid amount"` (supportingText) | `"يرجى إدخال مبلغ صحيح"` |
| `label = { Text("Notes (Optional)") }` | `"ملاحظات (اختياري)"` |
| `"Cancel"` (OutlinedButton) | `"إلغاء"` |
| `"Open Shift"` (confirm Button) | `"فتح وردية"` |

**`CashDropDialog` composable:** add `val isArabic = LocalIsArabic.current` at the top of the composable body.

| English | Arabic |
|---------|--------|
| `"Cash Drop"` (Dialog title) | `"إيداع نقدي"` |
| `label = { Text("Amount") }` | `"المبلغ"` |
| `"Please enter a valid amount"` (supportingText) | `"يرجى إدخال مبلغ صحيح"` |
| `label = { Text("Note (Optional)") }` | `"ملاحظة (اختياري)"` |
| `"Cancel"` | `"إلغاء"` |
| `"Record Drop"` (confirm Button) | `"تسجيل الإيداع"` |

**`CloseShiftDialog` composable:** add `val isArabic = LocalIsArabic.current` at the top of the composable body.

| English | Arabic |
|---------|--------|
| `"Close Shift"` (Dialog title) | `"إغلاق الوردية"` |
| `"Expected Cash: ${expectedCash}"` | `"النقد المتوقع: ${expectedCash}"` |
| `"Variance: ${variance}"` | `"الفرق: ${variance}"` |
| `label = { Text("Actual Cash Count") }` | `"عدد النقد الفعلي"` |
| `"Please enter a valid amount"` (supportingText) | `"يرجى إدخال مبلغ صحيح"` |
| `label = { Text("Note (Optional)") }` | `"ملاحظة (اختياري)"` |
| `"Cancel"` | `"إلغاء"` |
| `"Close Shift"` (confirm Button) | `"إغلاق الوردية"` |

---

### 3. `feature/sell/src/main/java/com/tannous/pos/feature/sell/PaymentSelectionDialog.kt`

Add to imports:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

At the top of `PaymentSelectionDialog` composable body (after existing `var selectedMethod`), add:
```kotlin
val isArabic = LocalIsArabic.current
```

| English | Arabic |
|---------|--------|
| `"Select Payment Method"` (title Text) | `"اختر طريقة الدفع"` |
| `"Total Amount"` (label in card) | `"المبلغ الإجمالي"` |
| `"Payment Method"` (section header) | `"طريقة الدفع"` |
| `"Cash"` (FilterChip label) | `"نقداً"` |
| `"Card"` (FilterChip label) | `"بطاقة"` |
| `"Other"` (FilterChip label) | `"أخرى"` |
| `label = { Text("Cash Amount") }` | `"مبلغ نقدي"` |
| `"Change: ${currencyFormatter.format(change)}"` | `"الباقي: ${currencyFormatter.format(change)}"` |
| `label = { Text("Card Amount") }` | `"مبلغ البطاقة"` |
| `label = { Text("Payment Method Name") }` | `"اسم طريقة الدفع"` |
| `label = { Text("Amount") }` (in OTHER branch) | `"المبلغ"` |
| `"Please select a payment method"` | `"يرجى اختيار طريقة دفع"` |
| `"Remaining: ${currencyFormatter.format(remaining)}"` | `"المتبقي: ${currencyFormatter.format(remaining)}"` |
| `"Cancel"` (OutlinedButton) | `"إلغاء"` |
| `"Finalize Order"` (Button) | `"إتمام الطلب"` |

Also update the `notes` string inside the `payments.add(PaymentDto(...))` for cash change:
```kotlin
notes = if (change > BigDecimal.ZERO) 
    (if (isArabic) "الباقي: ${currencyFormatter.format(change)}" else "Change: ${currencyFormatter.format(change)}")
    else null
```

---

### 4. `feature/sell/src/main/java/com/tannous/pos/feature/sell/CashPaymentDialog.kt`

Add to imports:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

At the top of `CashPaymentDialog` composable body, add:
```kotlin
val isArabic = LocalIsArabic.current
```

| English | Arabic |
|---------|--------|
| `"Cash Payment"` (title) | `"الدفع نقداً"` |
| `"Total Amount"` (card label) | `"المبلغ الإجمالي"` |
| `label = { Text("Cash Received") }` | `"النقد المستلم"` |
| `"Amount must be greater than or equal to total"` (supportingText) | `"يجب أن يكون المبلغ أكبر من أو يساوي الإجمالي"` |
| `"Change Due"` (card title) | `"الباقي"` |
| `"Cancel"` | `"إلغاء"` |
| `"Finalize Order"` | `"إتمام الطلب"` |

---

### 5. `feature/sell/src/main/java/com/tannous/pos/feature/sell/OrderHistoryScreen.kt`

Add to imports:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

At the top of `OrderHistoryScreen` composable body (after existing `val uiState`), add:
```kotlin
val isArabic = LocalIsArabic.current
```

**Update the void dialog strings (already inside the composable scope, so `isArabic` is accessible):**

| English | Arabic |
|---------|--------|
| `"Order History"` (TopAppBar title) | `"سجل الطلبات"` |
| `contentDescription = "Back"` | `"رجوع"` |
| `contentDescription = "Refresh"` | `"تحديث"` |
| `"No orders found"` | `"لا توجد طلبات"` |
| `title = { Text("Void Order") }` | `"إلغاء الطلب"` |
| `Text("Enter a reason to void this order.")` | `"أدخل سببًا لإلغاء هذا الطلب."` |
| `label = { Text("Reason") }` | `"السبب"` |
| `Text("Void", color = ...)` (confirmButton) | `"إلغاء"` |
| `Text("Cancel")` (dismissButton) | `"غلق"` |

**Update `orderHistoryFilterLabel` function** — add `isArabic: Boolean` parameter and pass `isArabic` from the composable scope when calling it in `items { filter -> FilterChip(label = { Text(orderHistoryFilterLabel(filter, isArabic)) }) }`:

```kotlin
private fun orderHistoryFilterLabel(filter: OrderHistoryFilter, isArabic: Boolean): String = when (filter) {
    OrderHistoryFilter.All -> if (isArabic) "الكل" else "All"
    OrderHistoryFilter.Paid -> if (isArabic) "مدفوع" else "Paid"
    OrderHistoryFilter.Open -> if (isArabic) "مفتوح" else "Open"
    OrderHistoryFilter.Voided -> if (isArabic) "ملغى" else "Voided"
    OrderHistoryFilter.PendingSync -> if (isArabic) "معلق" else "Pending"
}
```

**Update `orderStatusLabel` function** — add `isArabic: Boolean` parameter. This function is called from `OrderHistoryRow` which is a `@Composable`, so also add `isArabic: Boolean` parameter to `OrderHistoryRow` and pass `isArabic = isArabic` from the parent composable when calling `OrderHistoryRow(...)`.

```kotlin
private fun orderStatusLabel(order: OrderEntity, isArabic: Boolean): String = when {
    order.receiptNumber?.startsWith("PENDING") == true -> if (isArabic) "في الانتظار" else "Queued"
    order.status.isAlreadyVoidedStatus() -> if (isArabic) "ملغى" else "Voided"
    order.status in setOf("6", "Paid", "PAID") -> if (isArabic) "مدفوع" else "Paid"
    order.status in setOf("1", "Open", "OPEN") -> if (isArabic) "مفتوح" else "Open"
    else -> order.status
}
```

Also update the status label strings inside `OrderHistoryRow`:
- `"Voided"` (Text for already voided) → `if (isArabic) "ملغى" else "Voided"`
- `"Pending"` (Text for pending sync) → `if (isArabic) "معلق" else "معلق"` (same in Arabic)
- `"Void"` (TextButton for voidable) → `if (isArabic) "إلغاء" else "Void"`

---

## Constraints

- Do NOT modify ViewModel, repository, navigation, or data layer code.
- Do NOT add any new files.
- Do NOT use Android string resources (`R.string.*`) — the entire codebase uses hardcoded strings with `if (isArabic)` conditionals.
- Preserve all existing code structure, formatting, and logic exactly. Only string literals change.
- The `LocalIsArabic.current` call must be at the top of the composable function body, not inside lambdas or nested composables (Compose composition local rules).
- In non-composable helper functions (`orderHistoryFilterLabel`, `orderStatusLabel`), receive `isArabic: Boolean` as a plain parameter.
