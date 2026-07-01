# Arabic i18n Phase 2 — Remaining Screens

## Context

Same pattern as Phase 1. The Tannous POS app uses a manual composition-local for Arabic:

```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
// At the top of each @Composable body:
val isArabic = LocalIsArabic.current
// Then: if (isArabic) "عربي" else "English"
```

**Rules (same as Phase 1 — must be followed exactly):**
- Do NOT touch ViewModels, repositories, navigation, or data layer.
- Do NOT add new files.
- Do NOT use `R.string.*` Android resources.
- Do NOT move `LocalIsArabic.current` inside nested lambdas — capture at the top of the composable function body only.
- In non-composable helper functions (like `statusLabel`), add an `isArabic: Boolean` parameter and pass it from the composable scope.

---

## Files to Update

### 1. `feature/auth/src/main/java/com/tannous/pos/feature/auth/LoginScreen.kt`

Add import:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

Add at the top of `LoginScreen` composable body (after `val uiState`):
```kotlin
val isArabic = LocalIsArabic.current
```

| English | Arabic |
|---------|--------|
| `label = { Text("Username") }` | `"اسم المستخدم"` |
| `label = { Text("Password") }` | `"كلمة المرور"` |
| `Text("Login")` (inside Button) | `"تسجيل الدخول"` |

Note: "Tannous POS" is a brand name — leave it in English regardless of language.

---

### 2. `feature/customers/src/main/java/com/tannous/pos/feature/customers/CustomersScreen.kt`

Add import:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

Add at the top of `CustomersScreen` composable body (after `val uiState`):
```kotlin
val isArabic = LocalIsArabic.current
```

| English | Arabic |
|---------|--------|
| TopAppBar `"Customers"` | `"العملاء"` |
| `placeholder = { Text("Search by name or phone") }` | `"بحث بالاسم أو رقم الهاتف"` |
| `title = { Text("New Customer") }` | `"عميل جديد"` |
| `label = { Text("First name *") }` (create dialog) | `"الاسم الأول *"` |
| `label = { Text("Last name *") }` (create dialog) | `"اسم العائلة *"` |
| `label = { Text("Phone") }` (create dialog) | `"الهاتف"` |
| `label = { Text("Email") }` (create dialog) | `"البريد الإلكتروني"` |
| `label = { Text("Notes") }` (create dialog) | `"ملاحظات"` |
| `Text("Create")` (create confirm button) | `"إنشاء"` |
| `Text("Cancel")` (create dismiss button) | `"إلغاء"` |
| `title = { Text("Edit Customer") }` | `"تعديل العميل"` |
| `label = { Text("First name *") }` (edit dialog) | `"الاسم الأول *"` |
| `label = { Text("Last name *") }` (edit dialog) | `"اسم العائلة *"` |
| `label = { Text("Phone") }` (edit dialog) | `"الهاتف"` |
| `label = { Text("Email") }` (edit dialog) | `"البريد الإلكتروني"` |
| `label = { Text("Notes") }` (edit dialog) | `"ملاحظات"` |
| `label = { Text("Allergies") }` (edit dialog) | `"الحساسية"` |
| `Text("Dismiss")` (edit allergies warning button) | `"حسنًا"` |
| `Text("Save")` (edit confirm button) | `"حفظ"` |
| `Text("Cancel")` (edit dismiss button) | `"إلغاء"` |
| `"Customer created"` (snackbar message in LaunchedEffect) | `"تم إنشاء العميل"` |

Also update the customer row label (if present):
- `"${customer.totalOrders} orders"` → `if (isArabic) "${customer.totalOrders} طلبات" else "${customer.totalOrders} orders"`

---

### 3. `feature/sell/src/main/java/com/tannous/pos/feature/sell/AddOnPickerDialog.kt`

Add import:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

Add at the top of `AddOnPickerDialog` composable body (after `var selectedIds`):
```kotlin
val isArabic = LocalIsArabic.current
```

| English | Arabic |
|---------|--------|
| `"Select add-ons (optional)"` | `"اختر الإضافات (اختياري)"` |
| `"No add-ons available"` | `"لا توجد إضافات متاحة"` |
| `Text("Cancel")` | `"إلغاء"` |
| `Text("Add to Cart")` | `"أضف إلى السلة"` |

The menu item name (`menuItem.name`) should use `nameAr` when Arabic if available. The `MenuItemEntity` has a `nameAr` field. Update the title Text to:
```kotlin
Text(
    text = if (isArabic) menuItem.nameAr?.takeIf { it.isNotBlank() } ?: menuItem.name else menuItem.name,
    ...
)
```

---

### 4. `feature/sell/src/main/java/com/tannous/pos/feature/sell/FeedbackPromptDialog.kt`

Add import:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

Add at the top of `FeedbackPromptDialog` composable body (after `val uiState`):
```kotlin
val isArabic = LocalIsArabic.current
```

| English | Arabic |
|---------|--------|
| `"Thank you! 🎉"` (submitted title) | `"شكرًا! 🎉"` |
| `"How was your experience?"` (title) | `"كيف كانت تجربتك؟"` |
| `"Your feedback has been recorded."` (submitted body) | `"تم تسجيل ملاحظاتك."` |
| `label = { Text("Comment (optional)") }` | `"تعليق (اختياري)"` |
| `Text("Submit")` (confirm button) | `"إرسال"` |
| `Text("Skip")` (dismiss button) | `"تخطي"` |

Note: The category labels come from the ViewModel (they are data-driven strings like "Food", "Service", "Ambience", "Complaint"). Do NOT translate those — they are server-defined or display-only. Leave them as-is.

---

### 5. `feature/sell/src/main/java/com/tannous/pos/feature/sell/LoyaltyScreen.kt`

Add import:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

Add at the top of the main `LoyaltyScreen` composable body:
```kotlin
val isArabic = LocalIsArabic.current
```

| English | Arabic |
|---------|--------|
| `title = { Text("Redeem Points") }` (redeem dialog) | `"استرداد النقاط"` |
| `"Available: ${...} points"` | `"المتاح: ${...} نقطة"` |
| `label = { Text("Points to redeem") }` | `"النقاط المراد استردادها"` |
| `Text("Redeem")` (redeem confirm) | `"استرداد"` |
| `Text("Cancel")` (redeem dismiss) | `"إلغاء"` |
| `title = { Text("Loyalty Account") }` (main dialog/card title) | `"حساب الولاء"` |
| `"No loyalty account yet"` | `"لا يوجد حساب ولاء بعد"` |
| `"Points will be created automatically on the next purchase."` | `"سيتم إنشاء النقاط تلقائيًا عند أول عملية شراء."` |
| `"available points"` | `"نقطة متاحة"` |
| `"Lifetime Earned"` | `"مجموع المكتسب"` |
| `"Lifetime Redeemed"` | `"مجموع المستهلك"` |
| `Text(if (isRedeeming) "Redeeming…" else "Redeem Points")` | `if (isArabic) (if (isRedeeming) "جارٍ الاسترداد…" else "استرداد النقاط") else (if (isRedeeming) "Redeeming…" else "Redeem Points")` |
| `"Recent Transactions"` | `"المعاملات الأخيرة"` |

---

### 6. `feature/sell/src/main/java/com/tannous/pos/feature/sell/TableMapScreen.kt`

Add import:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

Add at the top of `TableMapScreen` composable body (after `val uiState`):
```kotlin
val isArabic = LocalIsArabic.current
```

| English | Arabic |
|---------|--------|
| `"Select Table"` (in TopAppBar title conditional) | `"اختر طاولة"` |
| `"Table Map"` (in TopAppBar title conditional) | `"خريطة الطاولات"` |
| `contentDescription = "Back"` | `"رجوع"` |
| `contentDescription = "Refresh"` | `"تحديث"` |
| `"No floor plans configured.\nAdd tables in settings."` | `"لا توجد مخططات طوابق.\nأضف طاولات في الإعدادات."` |

For `TableStatusDialog` — this is a `@Composable`, so add `isArabic: Boolean` parameter and pass it from the call site:

| English | Arabic |
|---------|--------|
| `"Capacity: ... \| Current: ..."` | `"السعة: ... \| الحالي: ..."` |
| `"Mark Available"` | `"تعيين كمتاح"` |
| `"Mark Occupied"` | `"تعيين كمشغول"` |
| `"Mark Reserved"` | `"تعيين كمحجوز"` |
| `"Mark Cleaning"` | `"تعيين قيد التنظيف"` |
| `Text("Cancel")` | `"إلغاء"` |

For `statusLabel` non-composable helper function — add `isArabic: Boolean` parameter and update all calls to pass it:

```kotlin
private fun statusLabel(status: Int, isArabic: Boolean): String = when (status) {
    TABLE_AVAILABLE -> if (isArabic) "متاح" else "Available"
    TABLE_OCCUPIED  -> if (isArabic) "مشغول" else "Occupied"
    TABLE_RESERVED  -> if (isArabic) "محجوز" else "Reserved"
    TABLE_CLEANING  -> if (isArabic) "قيد التنظيف" else "Cleaning"
    else            -> if (isArabic) "غير معروف" else "Unknown"
}
```

---

### 7. `feature/settings/src/main/java/com/tannous/pos/feature/settings/ReservationsScreen.kt`

Add import:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

Add at the top of the main `ReservationsScreen` composable body (after `val uiState`):
```kotlin
val isArabic = LocalIsArabic.current
```

| English | Arabic |
|---------|--------|
| TopAppBar `"Reservations"` | `"الحجوزات"` |
| `contentDescription = "Back"` | `"رجوع"` |
| `contentDescription = "New reservation"` | `"حجز جديد"` |
| `contentDescription = "Previous day"` | `"اليوم السابق"` |
| `contentDescription = "Next day"` | `"اليوم التالي"` |
| `"No reservations"` | `"لا توجد حجوزات"` |
| `"Tap + to add one"` | `"اضغط + للإضافة"` |
| `"Table $it"` | `if (isArabic) "طاولة $it" else "Table $it"` |
| `Text("Confirm")` (reservation action) | `"تأكيد"` |
| `Text("Seat")` (reservation action) | `"تخصيص طاولة"` |
| `Text("Cancel")` (reservation action) | `"إلغاء"` |
| `Text("No Show")` (reservation action) | `"لم يحضر"` |
| `title = { Text("New Reservation") }` | `"حجز جديد"` |
| `label = { Text("Customer Name *") }` | `"اسم العميل *"` |
| `label = { Text("Phone") }` | `"الهاتف"` |
| `label = { Text("Guests") }` | `"عدد الضيوف"` |
| `label = { Text("Time (HH:mm)") }` | `"الوقت (HH:mm)"` |
| `label = { Text("Date (YYYY-MM-DD)") }` | `"التاريخ (YYYY-MM-DD)"` |
| `Text("Check Available Tables")` | `"تحقق من الطاولات المتاحة"` |
| `"Select table:"` | `"اختر طاولة:"` |
| `label = { Text("Notes") }` | `"ملاحظات"` |
| `Text("Create")` (confirm) | `"إنشاء"` |
| `Text("Cancel")` (dismiss) | `"إلغاء"` |

Note: The table chip label `"${t.tableNumber} (${t.floorPlan}, cap ${t.capacity})"` — leave as-is (numbers and abbreviations are universal).

---

### 8. `feature/reports/src/main/java/com/tannous/pos/feature/reports/MenuEngineeringScreen.kt`

Add import:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

Add at the top of the main `MenuEngineeringScreen` composable body:
```kotlin
val isArabic = LocalIsArabic.current
```

| English | Arabic |
|---------|--------|
| `"Menu Engineering"` (TopAppBar/title Text) | `"هندسة القائمة"` |
| `"No sales data for this period."` | `"لا توجد بيانات مبيعات لهذه الفترة."` |
| `"Classification Guide"` | `"دليل التصنيف"` |
| `"⭐ Stars — high popularity + high margin → protect"` | `"⭐ النجوم — شعبية عالية + هامش عالٍ → حافظ عليها"` |
| `"🐴 Plowhorses — popular but low margin → reduce cost or reprice"` | `"🐴 الحصان الجاد — شعبي لكن هامش منخفض → قلل التكلفة أو أعد التسعير"` |
| `"🧩 Puzzles — high margin but unpopular → reposition or bundle"` | `"🧩 الألغاز — هامش عالٍ لكن غير شعبي → أعد تموضعه أو جمّعه"` |
| `"🐶 Dogs — low popularity + low margin → remove or overhaul"` | `"🐶 الكلاب — شعبية منخفضة + هامش منخفض → أزله أو أعد هيكلته"` |
| `"${item.unitsSold} sold"` | `if (isArabic) "${item.unitsSold} مبيع" else "${item.unitsSold} sold"` |
| `"${item.popularityIndex}% of sales"` | `if (isArabic) "${item.popularityIndex}% من المبيعات" else "${item.popularityIndex}% of sales"` |

The `"$label ($count)"` category header uses a computed label string — update the category label mapping to be bilingual:

The existing code has a when/map of category strings. Find where `label` is assigned for Stars/Plowhorses/Puzzles/Dogs and make it:
- "Stars" → `if (isArabic) "نجوم" else "Stars"`
- "Plowhorses" → `if (isArabic) "حصان جاد" else "Plowhorses"`
- "Puzzles" → `if (isArabic) "ألغاز" else "Puzzles"`
- "Dogs" → `if (isArabic) "كلاب" else "Dogs"`

---

### 9. `feature/printing/src/main/java/com/tannous/pos/feature/printing/PrintingPreviewScreen.kt`

Add import:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

Add at the top of the main `PrintingPreviewScreen` composable body:
```kotlin
val isArabic = LocalIsArabic.current
```

| English | Arabic |
|---------|--------|
| TopAppBar `"Printing Preview"` | `"معاينة الطباعة"` |
| `contentDescription = "Back"` | `"رجوع"` |
| `Text("Print Receipt")` | `"طباعة الفاتورة"` |
| `Text("Test Print")` | `"طباعة تجريبية"` |

---

### 10. `feature/inventory/src/main/java/com/tannous/pos/feature/inventory/InventoryScreen.kt`

Add import:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

Add at the top of the main `InventoryScreen` composable body (after `val uiState`):
```kotlin
val isArabic = LocalIsArabic.current
```

| English | Arabic |
|---------|--------|
| TopAppBar `"Inventory"` | `"المخزون"` |
| tab `"Stock"` | `"المخزون"` |
| tab `"Ingredients"` | `"المكونات"` |
| tab `"Recipes"` | `"الوصفات"` |
| `title = { Text("Delete Ingredient") }` | `"حذف مكوّن"` |
| `"Delete \"${...}\"?"` (ingredient) | `if (isArabic) "حذف \"${...}\"؟" else "Delete \"${...}\"?"` |
| `Text("Delete")` (ingredient confirm) | `"حذف"` |
| `Text("Cancel")` (ingredient dismiss) | `"إلغاء"` |
| `title = { Text("Ingredient In Use") }` | `"المكوّن قيد الاستخدام"` |
| `Text("Force Delete")` | `"حذف قسري"` |
| `Text("Cancel")` (ingredient in use dismiss) | `"إلغاء"` |
| `title = { Text("Delete Recipe") }` | `"حذف وصفة"` |
| `"Delete \"${...}\"?"` (recipe) | `if (isArabic) "حذف \"${...}\"؟" else "Delete \"${...}\"?"` |
| `Text("Delete")` (recipe confirm) | `"حذف"` |
| `Text("Cancel")` (recipe dismiss) | `"إلغاء"` |
| `title = { Text("Recipe In Use") }` | `"الوصفة قيد الاستخدام"` |
| `Text("Force Delete")` (recipe in use) | `"حذف قسري"` |
| `Text("Cancel")` (recipe in use dismiss) | `"إلغاء"` |
| filter chip `"All"` | `"الكل"` |
| filter chip `"Low Stock"` | `"مخزون منخفض"` |
| `Text("Retry")` (error button) | `"إعادة المحاولة"` |
| `Text("Add Ingredient")` (FAB/button) | `"إضافة مكوّن"` |
| `"No ingredients. Tap Add Ingredient to create one."` | `"لا توجد مكونات. اضغط إضافة مكوّن لإنشاء واحد."` |
| ingredient form `label = { Text("Name") }` | `"الاسم"` |
| ingredient form `label = { Text("Unit") }` | `"الوحدة"` |
| ingredient form `placeholder = { Text("kg, L, pcs") }` | `"كغ، ل، قطعة"` |
| ingredient form `label = { Text("Cost per unit") }` | `"التكلفة لكل وحدة"` |
| ingredient form `label = { Text("Description (optional)") }` | `"الوصف (اختياري)"` |
| `Text("Active")` (toggle label) | `"نشط"` |
| ingredient form `Text("Save")` | `"حفظ"` |
| ingredient form `Text("Cancel")` | `"إلغاء"` |
| `Text("Wastage", color = ...)` (action button) | `"هدر"` |
| `Text("Adjust")` (action button) | `"تعديل"` |
| `Text("Add Recipe")` (FAB/button) | `"إضافة وصفة"` |
| `Text("Retry")` (recipes error) | `"إعادة المحاولة"` |
| `"No recipes found."` | `"لا توجد وصفات."` |
| recipe form `label = { Text("Recipe name") }` | `"اسم الوصفة"` |
| recipe form `label = { Text("Description (optional)") }` | `"الوصف (اختياري)"` |
| recipe form `label = { Text("Menu item") }` | `"عنصر القائمة"` |

---

### 11. `feature/settings/src/main/java/com/tannous/pos/feature/settings/QrMenuScreen.kt`

Add import:
```kotlin
import com.tannous.pos.core.ui.LocalIsArabic
```

Add at the top of the main composable body:
```kotlin
val isArabic = LocalIsArabic.current
```

| English | Arabic |
|---------|--------|
| TopAppBar `"Digital Menu QR"` | `"QR القائمة الرقمية"` |
| `contentDescription = "Back"` | `"رجوع"` |
| `contentDescription = "Share"` | `"مشاركة"` |
| `"Scan to view the menu"` | `"امسح للاطلاع على القائمة"` |
| `contentDescription = "QR code for digital menu"` | `"رمز QR للقائمة الرقمية"` |
| `"Print or display this QR code on tables so customers can browse the menu on their phone."` | `"اطبع أو اعرض رمز QR هذا على الطاولات لتمكين العملاء من تصفح القائمة على هواتفهم."` |

---

## Summary of Pattern

Every file follows this structure:
```kotlin
// 1. Import (at top of file with other imports)
import com.tannous.pos.core.ui.LocalIsArabic

// 2. Inside @Composable function body, near the top
val isArabic = LocalIsArabic.current

// 3. Every user-visible string
Text(if (isArabic) "عربي" else "English")

// 4. For non-composable helpers that need translation, add parameter
private fun myHelper(x: Int, isArabic: Boolean): String = ...
// and call with: myHelper(x, isArabic)
```
