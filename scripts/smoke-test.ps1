#!/usr/bin/env pwsh
# Tannous POS Smoke Test Script
# Tests the complete sales flow: Open Shift → Create Order → Finalize Order

param(
    [string]$ApiUrl = "http://localhost:7000",
    [string]$DeviceId = "Front-Register-001"
)

try {
    Write-Host "🚀 Starting Tannous POS Smoke Test..." -ForegroundColor Green
    Write-Host "API URL: $ApiUrl" -ForegroundColor Yellow
    Write-Host "Device ID: $DeviceId" -ForegroundColor Yellow
    Write-Host ""

    # Step 1: Login to get JWT token
    Write-Host "1️⃣ Logging in..." -ForegroundColor Cyan
    $loginBody = @{
        username = "admin"
        password = "admin123"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$ApiUrl/api/v1/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    $headers = @{
        "Authorization" = "Bearer $token"
        "Device-Id" = $DeviceId
        "Content-Type" = "application/json"
    }

    Write-Host "✅ Login successful" -ForegroundColor Green

    # Step 2: Get business settings
    Write-Host "2️⃣ Getting business settings..." -ForegroundColor Cyan
    $settingsResponse = Invoke-RestMethod -Uri "$ApiUrl/api/v1/settings" -Method GET -Headers $headers
    Write-Host "✅ Business settings retrieved: $($settingsResponse.storeName)" -ForegroundColor Green

    # Step 3: Get menu items
    Write-Host "3️⃣ Getting menu items..." -ForegroundColor Cyan
    $menuItemsResponse = Invoke-RestMethod -Uri "$ApiUrl/api/v1/catalog/menu-items" -Method GET -Headers $headers
    $zaatarManakish = $menuItemsResponse.items | Where-Object { $_.name -eq "Zaatar Manakish" } | Select-Object -First 1
    Write-Host "✅ Found menu item: $($zaatarManakish.name) - $($zaatarManakish.price)" -ForegroundColor Green

    # Step 4: Get add-ons
    Write-Host "4️⃣ Getting add-ons..." -ForegroundColor Cyan
    $addOnsResponse = Invoke-RestMethod -Uri "$ApiUrl/api/v1/catalog/addons" -Method GET -Headers $headers
    $extraCheese = $addOnsResponse.items | Where-Object { $_.name -eq "Extra Cheese" } | Select-Object -First 1
    Write-Host "✅ Found add-on: $($extraCheese.name) - $($extraCheese.price)" -ForegroundColor Green

    # Step 5: Open shift
    Write-Host "5️⃣ Opening shift..." -ForegroundColor Cyan
    $openShiftBody = @{
        openingFloat = 100.00
        notes = "Smoke test shift"
    } | ConvertTo-Json

    $shiftResponse = Invoke-RestMethod -Uri "$ApiUrl/api/v1/shifts/open" -Method POST -Body $openShiftBody -Headers $headers
    $shiftId = $shiftResponse.id
    Write-Host "✅ Shift opened: $shiftId" -ForegroundColor Green

    # Step 6: Create order
    Write-Host "6️⃣ Creating order..." -ForegroundColor Cyan
    $orderBody = @{
        shiftId = $shiftId
        items = @(
            @{
                menuItemId = $zaatarManakish.id
                quantity = 1
                unitPrice = $zaatarManakish.price
                addOns = @(
                    @{
                        addOnId = $extraCheese.id
                        quantity = 1
                        unitPrice = $extraCheese.price
                    }
                )
            }
        )
        paymentMethod = "Cash"
        customerId = $null
        notes = "Smoke test order"
    } | ConvertTo-Json -Depth 10

    $orderResponse = Invoke-RestMethod -Uri "$ApiUrl/api/v1/orders" -Method POST -Body $orderBody -Headers $headers
    $orderId = $orderResponse.id
    $totalAmount = $orderResponse.totalAmount
    Write-Host "✅ Order created: $orderId" -ForegroundColor Green
    Write-Host "   Total amount: $totalAmount" -ForegroundColor Yellow

    # Step 7: Finalize order
    Write-Host "7️⃣ Finalizing order..." -ForegroundColor Cyan
    $finalizeBody = @{
        paymentMethod = "Cash"
        amountReceived = $totalAmount
        change = 0.00
    } | ConvertTo-Json

    $finalizeResponse = Invoke-RestMethod -Uri "$ApiUrl/api/v1/orders/$orderId/finalize" -Method POST -Body $finalizeBody -Headers $headers
    $receiptNumber = $finalizeResponse.receiptNumber
    Write-Host "✅ Order finalized!" -ForegroundColor Green
    Write-Host "   Receipt number: $receiptNumber" -ForegroundColor Yellow

    # Step 8: Verify inventory movements
    Write-Host "8️⃣ Verifying inventory movements..." -ForegroundColor Cyan
    $inventoryResponse = Invoke-RestMethod -Uri "$ApiUrl/api/v1/inventory/movements" -Method GET -Headers $headers
    $saleMovements = $inventoryResponse.items | Where-Object { $_.type -eq "Sale" -and $_.orderId -eq $orderId }
    Write-Host "✅ Found $($saleMovements.Count) inventory movements for the order" -ForegroundColor Green

    # Step 9: Verify shift expected cash
    Write-Host "9️⃣ Verifying shift expected cash..." -ForegroundColor Cyan
    $shiftDetailsResponse = Invoke-RestMethod -Uri "$ApiUrl/api/v1/shifts/$shiftId" -Method GET -Headers $headers
    $expectedCash = $shiftDetailsResponse.expectedCash
    Write-Host "✅ Shift expected cash: $expectedCash" -ForegroundColor Green

    # Summary
    Write-Host ""
    Write-Host "🎉 SMOKE TEST COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host "Order ID: $orderId" -ForegroundColor Cyan
    Write-Host "Receipt Number: $receiptNumber" -ForegroundColor Cyan
    Write-Host "Total Amount: $totalAmount" -ForegroundColor Cyan
    Write-Host "Shift ID: $shiftId" -ForegroundColor Cyan
    Write-Host "Expected Cash: $expectedCash" -ForegroundColor Cyan
    Write-Host "Inventory Movements: $($saleMovements.Count)" -ForegroundColor Cyan
    Write-Host ""

    # Return success
    return @{
        Success = $true
        OrderId = $orderId
        ReceiptNumber = $receiptNumber
        TotalAmount = $totalAmount
        ShiftId = $shiftId
        ExpectedCash = $expectedCash
    }
}
catch {
    Write-Host ""
    Write-Host "❌ SMOKE TEST FAILED!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorResponse = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorResponse)
        $errorBody = $reader.ReadToEnd()
        Write-Host "Response: $errorBody" -ForegroundColor Red
    }
    Write-Host ""
    
    # Return failure
    return @{
        Success = $false
        Error = $_.Exception.Message
    }
}
