# Inventory Deduction on Order Finalization

This document describes how inventory deduction works when an order is finalized in the Tannous POS system.

## Overview

When an order is finalized (status changes to `Paid`), the system automatically deducts inventory stock based on the menu items' recipes. This deduction is **fully transactional** - it happens within the same database transaction as payment processing and order status updates, ensuring atomicity.

## How Deduction is Computed

### Step 1: Recipe Lookup
For each menu item in the order:
1. System loads the active recipe(s) for the menu item
2. If a menu item has multiple recipes, the first active recipe is used
3. If no recipe exists, that menu item is skipped (no inventory deduction)

### Step 2: Quantity Calculation
For each order line:
- For each recipe line in the menu item's recipe:
  - **Total Quantity = RecipeLine.QuantityPerItem × OrderLine.Quantity**
  - Example: If recipe requires 0.5 kg per item and order line has quantity 2, total = 1.0 kg

### Step 3: Aggregation
- Quantities are aggregated per ingredient across all order lines
- Example: If order has 2 menu items both using the same ingredient, quantities are summed
- This creates **one inventory movement per ingredient** (not one per order line)

### Step 4: Stock Update and Movement Creation
For each ingredient:
1. Load or create `InventoryItem` for the ingredient
2. Update `CurrentStock = CurrentStock - aggregatedQuantity`
3. Create `InventoryMovement` record with:
   - `MovementType = Sale` (enum value 2)
   - `Quantity = -aggregatedQuantity` (negative for deduction)
   - `Reference = "Order-{OrderNumber}"` (links movement to order)
   - `Notes = "Sale deduction for order {OrderNumber}"`

## What Items Are Included

### ✅ Included (v1)
- **Menu Items with Recipes**: Only menu items that have active recipes are processed
- **Recipe Ingredients**: All ingredients listed in the recipe's recipe lines

### ❌ Not Included (v1)
- **Add-ons**: Add-ons do not affect inventory in v1
- **Menu Items without Recipes**: Items without recipes are skipped (no error)
- **Menu Items with Inactive Recipes**: Only active recipes are considered

### Future Enhancements
- Add-on inventory tracking (if add-ons have recipes/ingredients)
- Multiple recipe support (currently uses first active recipe)
- Unit conversion handling (currently assumes same units)

## Stock Validation Rules

**Current Behavior: System Allows Negative Stock**

The system does **NOT** block finalization when stock would go negative. Stock can go below zero, and the movement is still recorded. This allows:
- Backorders to be fulfilled
- Emergency sales when stock is low
- Manual stock adjustments to be made later

**No Validation Blocking:**
- No check for `CurrentStock >= quantityToDeduct`
- No 409 Conflict or 400 Bad Request for insufficient stock
- Finalization proceeds even if stock becomes negative

If you need to enforce stock validation in the future, add a check before creating movements:
```csharp
if (inventoryItem.CurrentStock - quantityToDeduct < 0)
{
    throw new InvalidOperationException($"Insufficient stock for ingredient {ingredientId}");
}
```

## Transaction Safety

All inventory operations are within the same transaction as:
- Order status update
- Payment creation
- Receipt number assignment

**If any operation fails:**
- All changes roll back (order, payments, inventory movements, stock updates)
- No partial state is persisted
- System remains consistent

## Performance Considerations

### Batch Loading (Avoids N+1 Queries)
- Recipes are loaded in a single query for all menu items: `WHERE MenuItemId IN (...)`
- Inventory items are loaded in a single query: `WHERE IngredientId IN (...)`
- Uses dictionaries for O(1) lookups during aggregation

### Aggregation
- Quantities are aggregated in memory before creating movements
- Creates minimal movement records (one per ingredient, not per order line)

## SQL Verification Queries

### Check Inventory Movements for an Order
```sql
SELECT 
    im.Id,
    im.MovementType,
    im.Quantity,
    im.UnitCost,
    im.TotalCost,
    im.Reference,
    im.MovementDate,
    i.Name as IngredientName,
    ii.CurrentStock as CurrentStock
FROM "InventoryMovements" im
INNER JOIN "Ingredients" i ON i.Id = im."IngredientId"
INNER JOIN "InventoryItems" ii ON ii.Id = im."InventoryItemId"
WHERE im.Reference LIKE 'Order-%'
  AND im."MovementType" = 2  -- Sale
ORDER BY im."MovementDate" DESC;
```

### Check Stock Changes for a Specific Order
```sql
SELECT 
    o."OrderNumber",
    im.Reference,
    i.Name as IngredientName,
    im.Quantity as DeductedQuantity,
    im."MovementDate",
    ii."CurrentStock" as StockAfterDeduction
FROM "Orders" o
INNER JOIN "InventoryMovements" im ON im.Reference = CONCAT('Order-', o."OrderNumber")
INNER JOIN "Ingredients" i ON i.Id = im."IngredientId"
INNER JOIN "InventoryItems" ii ON ii.Id = im."InventoryItemId"
WHERE o.Id = '<ORDER_ID>';
```

### Verify Aggregation (Multiple Order Lines, Same Ingredient)
```sql
-- Should show one movement per ingredient, even if multiple order lines use it
SELECT 
    im."IngredientId",
    i.Name as IngredientName,
    COUNT(*) as MovementCount,
    SUM(im.Quantity) as TotalDeductedQuantity
FROM "InventoryMovements" im
INNER JOIN "Ingredients" i ON i.Id = im."IngredientId"
WHERE im.Reference = 'Order-<ORDER_NUMBER>'
GROUP BY im."IngredientId", i.Name;
-- Expected: MovementCount = 1 per ingredient
```

### Check for Negative Stock After Finalization
```sql
SELECT 
    i.Name as IngredientName,
    ii."CurrentStock",
    ii."MinimumStock",
    CASE 
        WHEN ii."CurrentStock" < 0 THEN 'NEGATIVE'
        WHEN ii."CurrentStock" < ii."MinimumStock" THEN 'BELOW_MINIMUM'
        ELSE 'OK'
    END as StockStatus
FROM "InventoryItems" ii
INNER JOIN "Ingredients" i ON i.Id = ii."IngredientId"
WHERE ii."CurrentStock" < 0 OR ii."CurrentStock" < ii."MinimumStock"
ORDER BY ii."CurrentStock" ASC;
```

## Manual Test Steps

### Test 1: Basic Inventory Deduction

**Steps**:
1. Create an ingredient with initial stock (e.g., 100 kg)
2. Create a menu item
3. Create a recipe for the menu item with recipe lines (e.g., 0.5 kg per item)
4. Create an order with the menu item (quantity 2)
5. Finalize the order
6. Verify:
   - Inventory movement created with `MovementType = Sale`
   - Quantity = -1.0 kg (0.5 × 2)
   - Stock reduced to 99 kg
   - Movement `Reference` contains order number

**SQL Verification**:
```sql
SELECT * FROM "InventoryMovements" WHERE "Reference" LIKE 'Order-%' ORDER BY "CreatedAt" DESC LIMIT 1;
SELECT "CurrentStock" FROM "InventoryItems" WHERE "IngredientId" = '<INGREDIENT_ID>';
```

---

### Test 2: Aggregation Across Multiple Order Lines

**Steps**:
1. Create an ingredient with stock
2. Create 2 menu items that both use the same ingredient (different recipes)
3. Create an order with both menu items
4. Finalize the order
5. Verify:
   - Only **one** inventory movement created for the ingredient
   - Quantity = sum of all deductions (aggregated)
   - Stock updated correctly

**Expected**: One movement per ingredient, not one per order line.

---

### Test 3: Negative Stock Allowed

**Steps**:
1. Create an ingredient with low stock (e.g., 1.0 kg)
2. Create a menu item with recipe requiring more than available (e.g., 2.0 kg per item)
3. Create and finalize an order
4. Verify:
   - Finalization succeeds (no error)
   - Stock goes negative (e.g., -1.0 kg)
   - Movement is still created

**Expected**: System allows negative stock. No validation blocking.

---

### Test 4: Menu Item Without Recipe

**Steps**:
1. Create a menu item **without** a recipe
2. Create an order with that menu item
3. Finalize the order
4. Verify:
   - Finalization succeeds
   - No inventory movements created
   - Log shows "No active recipes found" message

**Expected**: Menu items without recipes are skipped gracefully.

---

### Test 5: Transaction Rollback on Payment Failure

**Steps**:
1. Create an order with menu items that have recipes
2. Attempt to finalize with insufficient payment
3. Verify:
   - Finalization fails
   - **No** inventory movements created
   - Stock **not** updated
   - Order status remains `Open`

**Expected**: Transaction rollback prevents partial state.

---

### Test 6: Multiple Ingredients in Recipe

**Steps**:
1. Create 2 ingredients with stock
2. Create a menu item with a recipe using both ingredients
3. Create and finalize an order
4. Verify:
   - **Two** inventory movements created (one per ingredient)
   - Both stocks updated correctly
   - Both movements linked to same order via `Reference`

**Expected**: One movement per ingredient in the recipe.

## Logging

The system logs inventory deduction operations:

**Information Level**:
- `"Created {Count} inventory movement(s) for order finalization. OrderId: {OrderId}"`

**Debug Level**:
- `"Created inventory deduction. IngredientId: {IngredientId}, Quantity: {Quantity}, NewStock: {NewStock}, OrderId: {OrderId}"`

**Warning Level**:
- `"Inventory item not found for ingredient {IngredientId}. Creating new inventory item. OrderId: {OrderId}"`
- `"Ingredient {IngredientId} not found. Skipping inventory deduction. OrderId: {OrderId}"`

## Data Model Reference

### Key Entities

- **Order**: Contains `OrderLines` with `MenuItemId` and `Quantity`
- **MenuItem**: Can have multiple `Recipes`
- **Recipe**: Contains `RecipeLines` with `IngredientId` and `QuantityPerItem`
- **RecipeLine**: Links recipe to ingredient with quantity per item
- **Ingredient**: Represents a raw material
- **InventoryItem**: Tracks current stock for an ingredient
- **InventoryMovement**: Records all stock changes (purchases, sales, adjustments, etc.)

### Movement Types

- `Purchase = 1`: Stock increase from supplier
- `Sale = 2`: Stock decrease from order finalization (used here)
- `Adjustment = 3`: Manual stock adjustments
- `Wastage = 4`: Stock loss from waste/spoilage
- `Transfer = 5`: Stock transfers between locations
- `Return = 6`: Stock returns

## Integration Tests

Automated tests are available in `Tannous.Pos.Integration/OrderFinalizationTests.cs`:

- `FinalizeOrder_ShouldCreateInventoryMovements_ForRecipeIngredients`: Basic deduction test
- `FinalizeOrder_ShouldDeductCorrectQuantities_ForMultipleOrderLines`: Aggregation test
- `FinalizeOrder_ShouldAllowNegativeStock_WhenDeductingBelowZero`: Negative stock test

Run tests with:
```bash
dotnet test Tannous.Pos.Integration
```

## Troubleshooting

### No Movements Created
- Check if menu items have active recipes
- Verify recipes have recipe lines with ingredients
- Check logs for "No active recipes found" messages

### Wrong Quantities
- Verify `RecipeLine.QuantityPerItem` values
- Check `OrderLine.Quantity` values
- Ensure aggregation logic is working (should sum quantities per ingredient)

### Stock Not Updated
- Verify transaction committed (check logs)
- Check if inventory item exists for ingredient
- Ensure `InventoryItem.CurrentStock` is being updated

### Negative Stock Unexpected
- This is expected behavior (system allows negative stock)
- To prevent: Add validation check before creating movements
- To monitor: Use SQL query to find negative stock items

## Future Enhancements

1. **Add-on Inventory**: Track inventory for add-ons if they have recipes
2. **Stock Validation**: Optional flag to block finalization when stock insufficient
3. **Unit Conversion**: Handle different units (e.g., recipe in grams, stock in kilograms)
4. **Multiple Recipes**: Support for menu items with multiple recipes (currently uses first)
5. **Recipe Versions**: Support for recipe versioning/history
6. **Cost Tracking**: Enhanced cost calculation for COGS reports


