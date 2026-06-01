#Requires -Version 5.1
<#
.SYNOPSIS
  Lightweight governance debt scan (no external services).

.DESCRIPTION
  Prints counts useful when refreshing ARCHITECTURE_DEBT_REPORT.md and writes
  governance/debt-report.json (stable field order for CI and tooling).

  Run from repo root:
    powershell -NoProfile -ExecutionPolicy Bypass -File governance/scan-debt.ps1
    (or pwsh on Linux/macOS.)
#>

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$webControllers = Join-Path $root 'Tannous.Pos.WebApi\Controllers'
$webApiRoot = Join-Path $root 'Tannous.Pos.WebApi'
$programCs = Join-Path $root 'Tannous.Pos.WebApi\Program.cs'
$jsonOut = Join-Path $PSScriptRoot 'debt-report.json'

function Count-Lines([string[]]$files, [string]$regex) {
    $total = 0
    foreach ($f in $files) {
        if (-not (Test-Path $f)) { continue }
        $total += @(Select-String -Path $f -Pattern $regex).Count
    }
    return $total
}

$controllerFiles = @(
    Get-ChildItem -Path $webControllers -Filter '*Controller.cs' -File -ErrorAction SilentlyContinue |
    Sort-Object Name |
    ForEach-Object { $_.FullName }
)

$controllerCount = $controllerFiles.Count

$dbCtxHits = Count-Lines $controllerFiles 'PosDbContext'
$repoHits = Count-Lines $controllerFiles 'I\w+Repository'
$allowAnonHits = Count-Lines $controllerFiles '\[AllowAnonymous\]'

$unversionedRouteHits = 0
foreach ($f in $controllerFiles) {
    $txt = Get-Content $f -Raw
    if ($txt -match '\[Route\("api/\[controller\]"\)\]' -and $txt -notmatch 'version:apiVersion') {
        $unversionedRouteHits++
    }
}

$mutationVerbPattern = '\[Http(Post|Put|Patch|Delete)'
$mutationHits = Count-Lines $controllerFiles $mutationVerbPattern

$explicitDeviceCheckHits = Count-Lines $controllerFiles 'Device-Id header is required'

function Get-SourceCsFiles([string]$dir) {
    if (-not (Test-Path $dir)) { return @() }
    return @(Get-ChildItem -Path $dir -Recurse -Filter '*.cs' -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
        Sort-Object FullName |
        ForEach-Object { $_.FullName })
}

$orderDir = Join-Path $root 'Tannous.Pos.Application\Orders'
$orderCsFiles = @(Get-SourceCsFiles $orderDir)
$todoOrders = 0
if (Test-Path $orderDir) {
    $todoOrders = @(Get-ChildItem $orderDir -Recurse -Filter '*.cs' | Sort-Object FullName | ForEach-Object {
        Select-String -Path $_.FullName -Pattern '(?i)\bTODO\b' }).Count
}

$syncPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\SyncController.cs'
$ordersCtl = Join-Path $root 'Tannous.Pos.WebApi\Controllers\OrdersController.cs'
$syncTodo = 0
if (Test-Path $syncPath) {
    $syncTodo = @(Select-String -Path $syncPath -Pattern '(?i)\bTODO\b').Count
}

$ordersTodo = 0
if (Test-Path $ordersCtl) {
    $ordersTodo = @(Select-String -Path $ordersCtl -Pattern 'TODO|FIXME').Count
}

$fixmeCount = 0
$fixmeFiles = @()
if (Test-Path $orderDir) {
    $fixmeFiles += @(Get-ChildItem $orderDir -Recurse -Filter '*.cs' | ForEach-Object { $_.FullName })
}
foreach ($p in @($syncPath, $ordersCtl)) {
    if (Test-Path $p) { $fixmeFiles += $p }
}
$fixmeFiles = $fixmeFiles | Sort-Object -Unique
$fixmeCount = Count-Lines $fixmeFiles '(?i)\bFIXME\b'

$governanceRiskCommentCount = 0
if (Test-Path $webApiRoot) {
    $governanceRiskCommentCount = @(Get-ChildItem $webApiRoot -Recurse -Filter '*.cs' -ErrorAction SilentlyContinue |
        Sort-Object FullName |
        ForEach-Object { Select-String -Path $_.FullName -Pattern 'GOVERNANCE / RISK' }).Count
}

$governanceWarningLogCount = Count-Lines $orderCsFiles '(?i)\.LogWarning\s*\('

$appRoot = Join-Path $root 'Tannous.Pos.Application'
$infraRoot = Join-Path $root 'Tannous.Pos.Infrastructure'
$txFiles = @(@(Get-SourceCsFiles $appRoot) + @(Get-SourceCsFiles $infraRoot)) | Sort-Object -Unique
$explicitTransactionCount = Count-Lines $txFiles 'BeginTransactionAsync'

$placeholderProcessorCount = 0
$syncReplayRiskCommentCount = 0
$moneyAffectingPlaceholderProcessorCount = 0
$explicitReplayWarningCount = 0
if (Test-Path $syncPath) {
    $syncRaw = Get-Content -Path $syncPath -Raw -Encoding UTF8
    $placeholderProcessorCount = ([regex]::Matches($syncRaw, '(?i)Placeholder success')).Count
    $syncReplayRiskCommentCount = @(Select-String -Path $syncPath -Pattern '(?i)\b(replay|idempotency)\b').Count
    foreach ($proc in @('ProcessCreateOrder', 'ProcessFinalizeOrder', 'ProcessOpenShift', 'ProcessCashDrop')) {
        if ($syncRaw -match "(?s)$proc\b.*Placeholder success") {
            $moneyAffectingPlaceholderProcessorCount++
        }
    }
    $explicitReplayWarningCount = @(Select-String -Path $syncPath -Pattern '_logger\.LogWarning' | Where-Object {
            $_.Line -match '(?i)(duplicate operationId|operation missing operationId|partial application|replay|idempotency)'
        }).Count
}

$globalDeviceFilter = $false
if (Test-Path $programCs) {
    $globalDeviceFilter = (Select-String -Path $programCs -Pattern 'RequireDeviceIdFilter' -SimpleMatch -Quiet)
}

$openapiBaselinePath = Join-Path $PSScriptRoot 'openapi-schema-governance-baseline.json'
$OpenAPITrackedPathCount = 0
if (Test-Path $openapiBaselinePath) {
    $ob = Get-Content $openapiBaselinePath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($null -ne $ob.paths) {
        $OpenAPITrackedPathCount = @($ob.paths).Count
    }
}

$concurrencyBaselinePath = Join-Path $PSScriptRoot 'concurrency-entity-baseline.json'
$concurrencyTokenEntityCount = 0
if (Test-Path $concurrencyBaselinePath) {
    $cb = Get-Content $concurrencyBaselinePath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($e in $cb.entities) {
        $rel = $e.sourceFile -replace '/', '\'
        $fp = Join-Path $root $rel
        if (-not (Test-Path $fp)) { continue }
        $t = Get-Content $fp -Raw -Encoding UTF8
        if ($t -match '\[Timestamp\]' -or $t -match 'IsConcurrencyToken' -or $t -match '\bRowVersion\b') {
            $concurrencyTokenEntityCount++
        }
    }
}

$moneyPathTest = Join-Path $root 'tests\Tannous.Pos.Architecture.Tests\MoneyPathGovernanceSourceTests.cs'
$moneyPathGovernanceAnchorCount = 0
if (Test-Path $moneyPathTest) {
    $moneyPathGovernanceAnchorCount = @(Select-String -Path $moneyPathTest -Pattern 'Assert\.Contains').Count
}

$replaySensitiveProcessorCount = 0
if (Test-Path $syncPath) {
    $syncRaw = Get-Content -Path $syncPath -Raw -Encoding UTF8
    $procMatches = [regex]::Matches($syncRaw, 'private\s+(async\s+)?Task<OpResultDto>\s+Process(\w+)\s*\(')
    foreach ($m in $procMatches) {
        $n = $m.Groups[2].Value
        if ($n -match '^(CreateCustomer|CreateOrder|FinalizeOrder|OpenShift|CashDrop|RecordWastage|AdjustInventory)$') {
            $replaySensitiveProcessorCount++
        }
    }
}

$finalizePath = Join-Path $root 'Tannous.Pos.Application\Orders\Commands\FinalizeOrder\FinalizeOrderCommandHandler.cs'
$transactionBoundaryAnchorCount = 0
if (Test-Path $finalizePath) {
    $fh = Get-Content -Path $finalizePath -Raw -Encoding UTF8
    if ($fh -match 'BeginTransactionAsync') { $transactionBoundaryAnchorCount++ }
    if ($fh -match 'CommitAsync') { $transactionBoundaryAnchorCount++ }
    if ($fh -match 'RollbackAsync') { $transactionBoundaryAnchorCount++ }
}

$readinessPath = Join-Path $PSScriptRoot 'concurrency-migration-readiness-baseline.json'
$baseEntityPath = Join-Path $root 'Tannous.Pos.Domain\Common\BaseEntity.cs'
$baseEntityRaw = ''
if (Test-Path $baseEntityPath) {
    $baseEntityRaw = Get-Content -Path $baseEntityPath -Raw -Encoding UTF8
}
$concurrencyReadyEntityCount = 0
if (Test-Path $readinessPath) {
    $rb = Get-Content $readinessPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($e in $rb.entities) {
        $rel = $e.sourceFile -replace '/', '\'
        $fp = Join-Path $root $rel
        if (-not (Test-Path $fp)) { continue }
        $ent = Get-Content $fp -Raw -Encoding UTF8
        $useBase = $false
        if ($null -ne $e.includeBaseEntity -and $e.includeBaseEntity) { $useBase = $true }
        $combined = if ($useBase) { $ent + [Environment]::NewLine + $baseEntityRaw } else { $ent }
        $hasTs = $ent -match '\[Timestamp\]'
        $hasRv = $ent -match '\bRowVersion\b'
        $hasIc = $ent -match 'IsConcurrencyToken'
        $hasUpd = $combined -match '\b(UpdatedAt|LastUpdated|ModifiedAt)\b'
        if ($hasTs -or $hasRv -or $hasIc -or $hasUpd) {
            $concurrencyReadyEntityCount++
        }
    }
}

# Visibility only: known AutoMapper NU1903 advisory (do not upgrade in governance-only PRs).
$knownNugetAutoMapperAdvisoryCount = 1

# --- Sync replay / partial-batch observability counts (warning/reporting only; not hard budgets) ---
$moneyReplayRiskProcessorCount = 0
$partialBatchWarningCount = 0
if (Test-Path $syncPath) {
    $syncRaw = Get-Content -Path $syncPath -Raw -Encoding UTF8
    foreach ($proc in @('CreateOrder', 'FinalizeOrder', 'OpenShift', 'CashDrop', 'RecordWastage', 'AdjustInventory')) {
        $pattern = "(?s)private\s+(async\s+)?Task<OpResultDto>\s+Process$proc\s*\([^)]*\)\s*\{"
        $m = [regex]::Match($syncRaw, $pattern)
        if (-not $m.Success) { continue }
        $startIdx = $m.Index + $m.Length
        $rest = $syncRaw.Substring([Math]::Min($startIdx, $syncRaw.Length))
        $next = [regex]::Match($rest, 'private\s+(async\s+)?Task<OpResultDto>\s+Process\w+\s*\(')
        $len = if ($next.Success) { $next.Index } else { $rest.Length }
        $body = $rest.Substring(0, [Math]::Min($len, $rest.Length))
        $isMoney = $proc -match '^(CreateOrder|FinalizeOrder|OpenShift|CashDrop)$'
        $isInv = $proc -match '^(RecordWastage|AdjustInventory)$'
        if (-not ($isMoney -or $isInv)) { continue }
        if ($body -notmatch '(?i)(replay|idempotency)') { continue }
        if ($body -match '(?i)(Placeholder success|GOVERNANCE / RISK)') {
            $moneyReplayRiskProcessorCount++
        }
    }

    $partialBatchWarningCount = @([regex]::Matches($syncRaw, '(?is)_logger\.LogWarning\s*\([\s\S]{0,1200}?partial\s+(application|batch|failure|apply)')).Count
}

$idempotencyShortCircuitLogCount = 0
if (Test-Path $orderDir) {
    $orderCs = @(Get-ChildItem -Path $orderDir -Recurse -Filter '*.cs' -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })
    foreach ($f in $orderCs) {
        $chunk = Get-Content -Path $f.FullName -Raw -Encoding UTF8
        $idempotencyShortCircuitLogCount += @([regex]::Matches($chunk, '(?i)(already\s+paid|idempotent\s+short[\s-]?circuit|duplicate\s+finalize|already\s+finalized|short-circuit\s+on\s+paid|Finalize idempotency observability)')).Count
    }
}

$upgradePlanPath = Join-Path $PSScriptRoot 'concurrency-upgrade-plan.json'
$concurrencyUpgradePlannedEntityCount = 0
if (Test-Path $upgradePlanPath) {
    $upPlan = Get-Content $upgradePlanPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($null -ne $upPlan.entities) {
        $concurrencyUpgradePlannedEntityCount = @($upPlan.entities).Count
    }
}

$reconciliationWarningCount = 0
$missingDurableIdempotencyCommentCount = 0
$moneyPathReplayRiskCount = 0
$ordersAndSyncFiles = @()
if (Test-Path $orderDir) {
    $ordersAndSyncFiles += @(Get-SourceCsFiles $orderDir)
}
if (Test-Path $syncPath) {
    $ordersAndSyncFiles += $syncPath
}
$ordersAndSyncFiles = @($ordersAndSyncFiles | Sort-Object -Unique)
foreach ($f in $ordersAndSyncFiles) {
    if (-not (Test-Path $f)) { continue }
    $chunk = Get-Content -Path $f -Raw -Encoding UTF8
    $reconciliationWarningCount += @([regex]::Matches($chunk, '(?i)(reconciliation|manual review|investigate mismatch|replay risk)')).Count
    $missingDurableIdempotencyCommentCount += @([regex]::Matches($chunk, '(?i)(durable idempotency|replay persistence|operation replay risk)')).Count
}

if (Test-Path $syncPath) {
    $syncRawForReplay = Get-Content -Path $syncPath -Raw -Encoding UTF8
    foreach ($proc in @('CreateOrder', 'FinalizeOrder', 'OpenShift', 'CashDrop')) {
        $patternM = "(?s)private\s+(async\s+)?Task<OpResultDto>\s+Process$proc\s*\([^)]*\)\s*\{"
        $mM = [regex]::Match($syncRawForReplay, $patternM)
        if (-not $mM.Success) { continue }
        $startIdxM = $mM.Index + $mM.Length
        $restM = $syncRawForReplay.Substring([Math]::Min($startIdxM, $syncRawForReplay.Length))
        $nextM = [regex]::Match($restM, 'private\s+(async\s+)?Task<OpResultDto>\s+Process\w+\s*\(')
        $lenM = if ($nextM.Success) { $nextM.Index } else { $restM.Length }
        $bodyM = $restM.Substring(0, [Math]::Min($lenM, $restM.Length))
        $moneyPathReplayRiskCount += @([regex]::Matches($bodyM, '(?i)(replay|idempotency)')).Count
    }
}

$durableSyncReplayEntityPresent = 0
$snapshotPathReplay = Join-Path $root 'Tannous.Pos.Infrastructure\Migrations\PosDbContextModelSnapshot.cs'
if (Test-Path $snapshotPathReplay) {
    $snapReplayRaw = Get-Content $snapshotPathReplay -Raw -Encoding UTF8
    if ($snapReplayRaw -match 'SyncOperationReceipt') {
        $durableSyncReplayEntityPresent = 1
    }
}

$durableReplayProtectedProcessorCount = 0
$replayReceiptEntityCount = 0
$replayReceiptLookupCount = 0
$replayReceiptUniqueIndexCount = 0
if (Test-Path $syncPath) {
    $syncRawDr = Get-Content $syncPath -Raw -Encoding UTF8
    $durableReplayProtectedProcessorCount = @([regex]::Matches($syncRawDr, '_replayCoordinator\.ExecuteAsync\s*\(')).Count
}
$syncReceiptEntityPath = Join-Path $root 'Tannous.Pos.Domain\Entities\SyncOperationReceipt.cs'
if (Test-Path $syncReceiptEntityPath) {
    $replayReceiptEntityCount = 1
}
$durableCoordPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\DurableSyncReplayCoordinator.cs'
if (Test-Path $durableCoordPath) {
    $replayReceiptLookupCount = @(Select-String -Path $durableCoordPath -Pattern 'SyncOperationReceipts').Count
}
if (Test-Path $snapshotPathReplay) {
    $snapIxRaw = Get-Content $snapshotPathReplay -Raw -Encoding UTF8
    if ($snapIxRaw -match '(?s)Tannous\.Pos\.Domain\.Entities\.SyncOperationReceipt.*?HasIndex\("DeviceId", "OperationId"\).*?\.IsUnique\(\)') {
        $replayReceiptUniqueIndexCount = 1
    }
}

$globalExPath = Join-Path $root 'Tannous.Pos.WebApi\Middleware\GlobalExceptionHandler.cs'
$voidOrderPath = Join-Path $root 'Tannous.Pos.Application\Orders\Commands\VoidOrder\VoidOrderCommandHandler.cs'

$concurrencyScanRoots = @(
    (Join-Path $root 'Tannous.Pos.Application'),
    (Join-Path $root 'Tannous.Pos.WebApi'),
    (Join-Path $root 'Tannous.Pos.Infrastructure')
)
$concurrencyExceptionHandlingCount = 0
foreach ($sr in $concurrencyScanRoots) {
    if (-not (Test-Path $sr)) { continue }
    foreach ($cf in @(Get-SourceCsFiles $sr)) {
        $concurrencyExceptionHandlingCount += @(Select-String -Path $cf -Pattern 'DbUpdateConcurrencyException' -SimpleMatch -ErrorAction SilentlyContinue).Count
    }
}

$conflictProblemDetailsCount = 0
if (Test-Path $globalExPath) {
    $conflictProblemDetailsCount = @(Select-String -Path $globalExPath -Pattern 'StatusCodes.Status409Conflict' -SimpleMatch).Count
}

$optimisticConcurrencyEntityCount = 0
if (Test-Path $concurrencyBaselinePath) {
    $cbForOpt = Get-Content $concurrencyBaselinePath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($null -ne $cbForOpt.entities) {
        $optimisticConcurrencyEntityCount = @($cbForOpt.entities).Count
    }
}

$concurrencyWarningLogCount = 0
foreach ($p in @($globalExPath, $finalizePath, $voidOrderPath)) {
    if (-not (Test-Path $p)) { continue }
    $rawCw = Get-Content $p -Raw -Encoding UTF8
    $concurrencyWarningLogCount += @([regex]::Matches($rawCw, 'Money-path concurrency visibility: optimistic concurrency conflict|Optimistic concurrency conflict \(DbUpdateConcurrencyException\)')).Count
}

$idempotencyWarningLogCount = 0
$replaySensitiveMoneyProcessorCount = 0
$partialBatchReplayWarningCount = 0
$placeholderReplayGovernanceCount = 0
if (Test-Path $finalizePath) {
    $finalizeRawForIdem = Get-Content $finalizePath -Raw -Encoding UTF8
    $idempotencyWarningLogCount += @([regex]::Matches($finalizeRawForIdem, 'Finalize idempotency observability')).Count
}
if (Test-Path $syncPath) {
    $syncRawForReplayMetrics = Get-Content $syncPath -Raw -Encoding UTF8
    $idempotencyWarningLogCount += @([regex]::Matches($syncRawForReplayMetrics, 'Sync replay visibility: placeholder-only processor')).Count
    $replaySensitiveMoneyProcessorCount = @([regex]::Matches($syncRawForReplayMetrics, 'replay visibility classification=money-affecting')).Count
    $partialBatchReplayWarningCount = @([regex]::Matches($syncRawForReplayMetrics, 'Sync replay visibility: partial application')).Count
    $placeholderReplayGovernanceCount = @([regex]::Matches($syncRawForReplayMetrics, 'ReplayClass=placeholder-only')).Count
}

$orderCsAllForInv = @(Get-SourceCsFiles $orderDir)
$inventoryConsistencyWarningCount = 0
foreach ($f in $orderCsAllForInv) {
    if (-not (Test-Path $f)) { continue }
    $inventoryConsistencyWarningCount += @(Select-String -Path $f -Pattern 'Inventory consistency observability' -SimpleMatch).Count
}

$inventoryMovementObservabilityCount = 0
if (Test-Path $finalizePath) {
    $finalizeRawInv = Get-Content $finalizePath -Raw -Encoding UTF8
    $inventoryMovementObservabilityCount = @([regex]::Matches($finalizeRawInv, 'AddMovementAsync')).Count
}

$protectedTypesPathForInv = Join-Path $root 'Tannous.Pos.Application\Sync\DurableSyncReplayProtectedTypes.cs'
$inventoryReplayProtectedProcessorCount = 0
if (Test-Path $protectedTypesPathForInv) {
    $ptInvRaw = Get-Content $protectedTypesPathForInv -Raw -Encoding UTF8
    if ($ptInvRaw -match '"AdjustInventory"') { $inventoryReplayProtectedProcessorCount++ }
    if ($ptInvRaw -match '"RecordWastage"') { $inventoryReplayProtectedProcessorCount++ }
}

$replayProtectedInventoryProcessorCount = 0
if (Test-Path $syncPath) {
    $syncInvRaw = Get-Content $syncPath -Raw -Encoding UTF8
    if ($syncInvRaw -match '(?s)case\s+"AdjustInventory"\s*:[\s\S]{0,500}?_replayCoordinator\.ExecuteAsync') { $replayProtectedInventoryProcessorCount++ }
    if ($syncInvRaw -match '(?s)case\s+"RecordWastage"\s*:[\s\S]{0,500}?_replayCoordinator\.ExecuteAsync') { $replayProtectedInventoryProcessorCount++ }
}

$inventoryReplayReceiptCount = 0
$customerShiftReplayVisibilityCount = 0
if (Test-Path $durableCoordPath) {
    $coordInvRaw = Get-Content $durableCoordPath -Raw -Encoding UTF8
    $inventoryReplayReceiptCount = @([regex]::Matches($coordInvRaw, 'Inventory sync durable replay visibility:')).Count
    $customerShiftReplayVisibilityCount = @([regex]::Matches($coordInvRaw, 'Customer/shift sync durable replay visibility:')).Count
}

$customerShiftReplayProtectedProcessorCount = 0
if (Test-Path $protectedTypesPathForInv) {
    $ptCsRaw = Get-Content $protectedTypesPathForInv -Raw -Encoding UTF8
    if ($ptCsRaw -match '"OpenShift"') { $customerShiftReplayProtectedProcessorCount++ }
    if ($ptCsRaw -match '"CreateCustomer"') { $customerShiftReplayProtectedProcessorCount++ }
}

$replayProtectedCustomerShiftProcessorCount = 0
if (Test-Path $syncPath) {
    $syncCsRaw = Get-Content $syncPath -Raw -Encoding UTF8
    if ($syncCsRaw -match '(?s)case\s+"OpenShift"\s*:[\s\S]{0,500}?_replayCoordinator\.ExecuteAsync') { $replayProtectedCustomerShiftProcessorCount++ }
    if ($syncCsRaw -match '(?s)case\s+"CreateCustomer"\s*:[\s\S]{0,500}?_replayCoordinator\.ExecuteAsync') { $replayProtectedCustomerShiftProcessorCount++ }
}

$protectedPlaceholderProcessorCount = 0
if (Test-Path $syncPath) {
    $syncPhRaw = Get-Content $syncPath -Raw -Encoding UTF8
    if ($syncPhRaw -match '(?s)ProcessCreateCustomer[\s\S]{0,1200}?GOVERNANCE / RISK[\s\S]{0,800}?Placeholder success') { $protectedPlaceholderProcessorCount++ }
    if ($syncPhRaw -match '(?s)ProcessOpenShift[\s\S]{0,1200}?GOVERNANCE / RISK[\s\S]{0,800}?Placeholder success') { $protectedPlaceholderProcessorCount++ }
}

$replayReconciliationVisibilityCount = 0
$replayMixedBatchWarningCount = 0
if (Test-Path $syncPath) {
    $syncReconRaw = Get-Content $syncPath -Raw -Encoding UTF8
    $replayReconciliationVisibilityCount = @([regex]::Matches($syncReconRaw, 'Sync reconciliation visibility:')).Count
    $replayMixedBatchWarningCount = @([regex]::Matches($syncReconRaw, 'Sync reconciliation visibility: replay mixed with failed operations')).Count
}

$moneyInventoryReplayClassificationCount = 0
if (Test-Path $syncPath) {
    $syncRawClass = Get-Content $syncPath -Raw -Encoding UTF8
    $moneyInventoryReplayClassificationCount = @([regex]::Matches($syncRawClass, 'Replay sensitivity classification:')).Count
}

$transactionBoundaryLogAnchorCount = 0
if (Test-Path $finalizePath) {
    $finalizeRawTxLog = Get-Content $finalizePath -Raw -Encoding UTF8
    $transactionBoundaryLogAnchorCount = @([regex]::Matches($finalizeRawTxLog, '(?i)order finalization transaction|Rolling back transaction|rolled back successfully|Error during transaction rollback')).Count
}

$inventoryReversalMovementCount = 0
$reversalObservabilityAnchorCount = 0
$paidVoidReversalProtectionCount = 0
$reversalTransactionBoundaryCount = 0
$reversalConcurrencyHandlingCount = 0
$refundConsistencyAnchorCount = 0
$refundPersistenceCount = 0
$refundIdempotencyProtectionCount = 0
$overpaymentObservabilityCount = 0
$taxDivergenceGovernanceCount = 0
if (Test-Path $voidOrderPath) {
    $voidRevRaw = Get-Content $voidOrderPath -Raw -Encoding UTF8
    $inventoryReversalMovementCount = @([regex]::Matches($voidRevRaw, 'InventoryMovementType\.Return')).Count
    $reversalObservabilityAnchorCount = @([regex]::Matches($voidRevRaw, 'Inventory reversal observability:')).Count
    $paidVoidReversalProtectionCount = @([regex]::Matches($voidRevRaw, 'reversal already completed')).Count
    $reversalTransactionBoundaryCount = @([regex]::Matches($voidRevRaw, 'BeginTransactionAsync')).Count
    $reversalConcurrencyHandlingCount = @([regex]::Matches($voidRevRaw, 'Inventory reversal observability: concurrency conflict during reversal')).Count
    $refundConsistencyAnchorCount = @([regex]::Matches($voidRevRaw, 'Refund consistency observability:')).Count
    $refundPersistenceCount = @([regex]::Matches($voidRevRaw, 'PaymentRefund')).Count
    $refundIdempotencyProtectionCount = @([regex]::Matches($voidRevRaw, 'Refund consistency observability: refund already exists')).Count
}
$settlementObservabilityCount = 0
$changeDueProtectionCount = 0
$netCapturedRefundCount = 0
$overpaymentSettlementCount = 0
if (Test-Path $finalizePath) {
    $finalizeRefundRaw = Get-Content $finalizePath -Raw -Encoding UTF8
    $overpaymentObservabilityCount = @([regex]::Matches($finalizeRefundRaw, 'Financial consistency observability: overpayment detected')).Count
    $settlementObservabilityCount = @([regex]::Matches($finalizeRefundRaw, 'Settlement consistency observability:')).Count
    $overpaymentSettlementCount = @([regex]::Matches($finalizeRefundRaw, 'Settlement consistency observability: overpayment with change due')).Count
    $changeDueProtectionCount = @([regex]::Matches($finalizeRefundRaw, 'order\.ChangeDue')).Count
}
if (Test-Path $voidOrderPath) {
    $voidSettleRaw = Get-Content $voidOrderPath -Raw -Encoding UTF8
    $netCapturedRefundCount = @([regex]::Matches($voidSettleRaw, 'ResolveNetCapturedAmountForRefund')).Count
    $settlementObservabilityCount += @([regex]::Matches($voidSettleRaw, 'Settlement consistency observability:')).Count
}
$taxGovPath = Join-Path $root 'Tannous.Pos.Application\Orders\OrderFinancialTaxGovernance.cs'
$printingPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\Printing\PrintingService.cs'
$orderFinPath = Join-Path $root 'Tannous.Pos.Application\Orders\OrderFinancialGovernance.cs'
if (Test-Path $taxGovPath) { $taxDivergenceGovernanceCount += @([regex]::Matches((Get-Content $taxGovPath -Raw -Encoding UTF8), 'GOVERNANCE')).Count }
if (Test-Path $printingPath) { $taxDivergenceGovernanceCount += @([regex]::Matches((Get-Content $printingPath -Raw -Encoding UTF8), 'GOVERNANCE / RISK')).Count }
if (Test-Path $orderFinPath) { $taxDivergenceGovernanceCount += @([regex]::Matches((Get-Content $orderFinPath -Raw -Encoding UTF8), 'GOVERNANCE / RISK')).Count }
if (Test-Path $voidOrderPath) { $taxDivergenceGovernanceCount += @([regex]::Matches((Get-Content $voidOrderPath -Raw -Encoding UTF8), 'OrderFinancialTaxGovernance')).Count }

$syncBatchClassificationCount = 0
$replayShortCircuitClassificationCount = 0
$partialBatchObservabilityAnchorCount = 0
$placeholderClassificationCount = 0
if (Test-Path $syncPath) {
    $syncBatchRaw = Get-Content $syncPath -Raw -Encoding UTF8
    $syncBatchClassificationCount = @([regex]::Matches($syncBatchRaw, 'Sync batch observability:')).Count
    $partialBatchObservabilityAnchorCount = @([regex]::Matches($syncBatchRaw, 'Sync batch observability: partial batch classification')).Count
}
$classifierPath = Join-Path $root 'Tannous.Pos.Application\Sync\SyncOperationOutcomeClassifier.cs'
if (Test-Path $classifierPath) {
    $classifierRaw = Get-Content $classifierPath -Raw -Encoding UTF8
    $placeholderClassificationCount = @([regex]::Matches($classifierRaw, 'PlaceholderOperation')).Count
}
if (Test-Path $durableCoordPath) {
    $coordBatchRaw = Get-Content $durableCoordPath -Raw -Encoding UTF8
    $replayShortCircuitClassificationCount = @([regex]::Matches($coordBatchRaw, 'MarkReplayShortCircuited')).Count
}

$syncConflictRecordCount = 0
$reconciliationObservabilityCount = 0
$inventoryDriftConflictCount = 0
$lifecycleConflictCount = 0
$replayMismatchConflictCount = 0
$syncConflictRecorderPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\SyncConflictRecorder.cs'
$syncConflictTypesPath = Join-Path $root 'Tannous.Pos.Application\Sync\SyncConflictTypes.cs'
$syncConflictEntityPath = Join-Path $root 'Tannous.Pos.Domain\Entities\SyncConflictRecord.cs'
if (Test-Path $syncConflictEntityPath) {
    $syncConflictRecordCount = 1
}
if (Test-Path $syncConflictRecorderPath) {
    $recorderRaw = Get-Content $syncConflictRecorderPath -Raw -Encoding UTF8
    $reconciliationObservabilityCount = @([regex]::Matches($recorderRaw, 'Sync reconciliation observability:')).Count
}
if (Test-Path $syncConflictTypesPath) {
    $typesRaw = Get-Content $syncConflictTypesPath -Raw -Encoding UTF8
    $inventoryDriftConflictCount = @([regex]::Matches($typesRaw, 'InventoryDriftRisk')).Count
    $lifecycleConflictCount = @([regex]::Matches($typesRaw, 'LifecycleStateConflict')).Count
    $replayMismatchConflictCount = @([regex]::Matches($typesRaw, 'ReplayMismatch')).Count
}
if (Test-Path $finalizePath) {
    $finalizeReconRaw = Get-Content $finalizePath -Raw -Encoding UTF8
    $inventoryDriftConflictCount += @([regex]::Matches($finalizeReconRaw, 'SyncConflictTypes\.InventoryDriftRisk')).Count
    $lifecycleConflictCount += @([regex]::Matches($finalizeReconRaw, 'SyncConflictTypes\.LifecycleStateConflict|SyncConflictTypes\.StaleOfflineMutation')).Count
}
if (Test-Path $voidOrderPath) {
    $voidReconRaw = Get-Content $voidOrderPath -Raw -Encoding UTF8
    $lifecycleConflictCount += @([regex]::Matches($voidReconRaw, 'SyncConflictTypes\.LifecycleStateConflict')).Count
}
if (Test-Path $durableCoordPath) {
    $coordReconRaw = Get-Content $durableCoordPath -Raw -Encoding UTF8
    $replayMismatchConflictCount += @([regex]::Matches($coordReconRaw, 'SyncConflictTypes\.ReplayMismatch')).Count
}

$operationalAuditRecordCount = 0
$auditObservabilityAnchorCount = 0
$timelineReconstructionCount = 0
$financialAuditAnchorCount = 0
$reconciliationAuditAnchorCount = 0
$operationalAuditEntityPath = Join-Path $root 'Tannous.Pos.Domain\Entities\OperationalAuditRecord.cs'
$operationalAuditRecorderPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalAuditRecorder.cs'
$operationalAuditTimelinePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalAuditTimelineService.cs'
if (Test-Path $operationalAuditEntityPath) {
    $operationalAuditRecordCount = 1
}
if (Test-Path $operationalAuditRecorderPath) {
    $auditRecorderRaw = Get-Content $operationalAuditRecorderPath -Raw -Encoding UTF8
    $auditObservabilityAnchorCount = @([regex]::Matches($auditRecorderRaw, 'Operational audit observability:')).Count
}
if (Test-Path $operationalAuditTimelinePath) {
    $timelineRaw = Get-Content $operationalAuditTimelinePath -Raw -Encoding UTF8
    $timelineReconstructionCount = @([regex]::Matches($timelineRaw, 'GetBy\w+Async')).Count
}
foreach ($auditHandlerPath in @($finalizePath, $voidOrderPath)) {
    if (Test-Path $auditHandlerPath) {
        $handlerAuditRaw = Get-Content $auditHandlerPath -Raw -Encoding UTF8
        $financialAuditAnchorCount += @([regex]::Matches($handlerAuditRaw, 'OperationalAuditActions\.(FinalizeSuccess|VoidSuccess|RefundPersisted|SettlementOverpayment|SettlementUnderpaymentRejected|ReversalMovementPersisted)')).Count
    }
}
foreach ($reconAuditPath in @($durableCoordPath, $syncPath, $finalizePath, $voidOrderPath)) {
    if (Test-Path $reconAuditPath) {
        $reconAuditRaw = Get-Content $reconAuditPath -Raw -Encoding UTF8
        $reconciliationAuditAnchorCount += @([regex]::Matches($reconAuditRaw, 'OperationalAuditActions\.(ReplayMismatch|StaleOfflineMutation|LifecycleStateConflict|PartialBatchReconciliation|MixedBatchOutcomes|DurableReplayShortCircuit|ConcurrencyConflict|PlaceholderOperationExecuted)')).Count
    }
}
if (Test-Path (Join-Path $root 'Tannous.Pos.WebApi\Middleware\GlobalExceptionHandler.cs')) {
    $globalExRaw = Get-Content (Join-Path $root 'Tannous.Pos.WebApi\Middleware\GlobalExceptionHandler.cs') -Raw -Encoding UTF8
    $reconciliationAuditAnchorCount += @([regex]::Matches($globalExRaw, 'OperationalAuditActions\.ConcurrencyConflict')).Count
}

$operationalAuditEndpointCount = 0
$operationalTimelineQueryCount = 0
$conflictDiagnosticsCount = 0
$auditPaginationProtectionCount = 0
$internalDiagnosticsAuthorizationCount = 0
$operationalAuditDiagnosticsPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditDiagnosticsController.cs'
$operationalAuditQueryPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalAuditQueryService.cs'
$operationalAuditQueryConstantsPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalAuditQueryConstants.cs'
$operationalAuditConflictActionsPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalAuditConflictActions.cs'
if (Test-Path $operationalAuditDiagnosticsPath) {
    $diagRaw = Get-Content $operationalAuditDiagnosticsPath -Raw -Encoding UTF8
    $operationalAuditEndpointCount = @([regex]::Matches($diagRaw, '\[HttpGet\(')).Count
    $internalDiagnosticsAuthorizationCount = @([regex]::Matches($diagRaw, 'Authorize\(Policy = "Admin"\)')).Count
}
if (Test-Path $operationalAuditQueryPath) {
    $queryRaw = Get-Content $operationalAuditQueryPath -Raw -Encoding UTF8
    $operationalTimelineQueryCount = @([regex]::Matches($queryRaw, 'Get\w+TimelineAsync|GetRecentConflictsAsync')).Count
    $conflictDiagnosticsCount = @([regex]::Matches($queryRaw, 'Operational audit diagnostics: conflict query executed')).Count
    $auditPaginationProtectionCount = @([regex]::Matches($queryRaw, 'pagination limit enforced')).Count
}
if (Test-Path $operationalAuditQueryConstantsPath) {
    $constantsRaw = Get-Content $operationalAuditQueryConstantsPath -Raw -Encoding UTF8
    $auditPaginationProtectionCount += @([regex]::Matches($constantsRaw, 'MaxPageSize')).Count
}
if (Test-Path $operationalAuditConflictActionsPath) {
    $conflictActionsRaw = Get-Content $operationalAuditConflictActionsPath -Raw -Encoding UTF8
    $conflictDiagnosticsCount += @([regex]::Matches($conflictActionsRaw, 'OperationalAuditActions\.')).Count
}

$reconciliationWorkflowEndpointCount = 0
$reconciliationStatusTransitionCount = 0
$reconciliationAuditActionCount = 0
$unresolvedConflictQueryCount = 0
$reconciliationSummaryCount = 0
$reconciliationControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditReconciliationController.cs'
$reconciliationServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\SyncConflictReconciliationService.cs'
$reconciliationAuditActionsPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalAuditReconciliationActions.cs'
if (Test-Path $reconciliationControllerPath) {
    $reconCtlRaw = Get-Content $reconciliationControllerPath -Raw -Encoding UTF8
    $reconciliationWorkflowEndpointCount = @([regex]::Matches($reconCtlRaw, '\[Http(Get|Post)\(')).Count
    $internalDiagnosticsAuthorizationCount += @([regex]::Matches($reconCtlRaw, 'Authorize\(Policy = "Admin"\)')).Count
}
if (Test-Path $reconciliationServicePath) {
    $reconSvcRaw = Get-Content $reconciliationServicePath -Raw -Encoding UTF8
    $reconciliationStatusTransitionCount = @([regex]::Matches($reconSvcRaw, 'TransitionAsync|reconciliation status changed')).Count
    $unresolvedConflictQueryCount = @([regex]::Matches($reconSvcRaw, 'Operational reconciliation observability: unresolved conflict query executed')).Count
    $reconciliationSummaryCount = @([regex]::Matches($reconSvcRaw, 'Operational reconciliation observability: reconciliation summary query executed')).Count
}
if (Test-Path $reconciliationAuditActionsPath) {
    $reconActionsRaw = Get-Content $reconciliationAuditActionsPath -Raw -Encoding UTF8
    $reconciliationAuditActionCount = @([regex]::Matches($reconActionsRaw, 'public const string')).Count
}
if (Test-Path $operationalAuditQueryPath) {
    $queryReconRaw = Get-Content $operationalAuditQueryPath -Raw -Encoding UTF8
    $unresolvedConflictQueryCount += @([regex]::Matches($queryReconRaw, 'UnresolvedOnly|ReconciliationStatus')).Count
}

$forensicExportEndpointCount = 0
$forensicSnapshotGenerationCount = 0
$forensicMetadataSanitizationCount = 0
$forensicTimelineAggregationCount = 0
$forensicAuthorizationProtectionCount = 0
$forensicExportControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditForensicExportController.cs'
$forensicServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalForensicSnapshotService.cs'
if (Test-Path $forensicExportControllerPath) {
    $forensicCtlRaw = Get-Content $forensicExportControllerPath -Raw -Encoding UTF8
    $forensicExportEndpointCount = @([regex]::Matches($forensicCtlRaw, '\[HttpGet\(')).Count
    $forensicAuthorizationProtectionCount = @([regex]::Matches($forensicCtlRaw, 'Authorize\(Policy = "Admin"\)')).Count
    $forensicAuthorizationProtectionCount += @([regex]::Matches($forensicCtlRaw, 'forensic authorization path')).Count
}
if (Test-Path $forensicServicePath) {
    $forensicSvcRaw = Get-Content $forensicServicePath -Raw -Encoding UTF8
    $forensicSnapshotGenerationCount = @([regex]::Matches($forensicSvcRaw, 'forensic snapshot generated')).Count
    $forensicMetadataSanitizationCount = @([regex]::Matches($forensicSvcRaw, 'OperationalAuditMetadataProjection\.Project|forensic metadata sanitized')).Count
    $forensicTimelineAggregationCount = @([regex]::Matches($forensicSvcRaw, 'forensic timeline aggregation executed|forensic conflict export executed')).Count
}

$operationalRetentionProtectionCount = 0
$forensicTruncationObservabilityCount = 0
$retentionSummaryEndpointCount = 0
$agedConflictClassificationCount = 0
$exportSurvivabilityMetadataCount = 0
$queryClampProtectionCount = 0
$retentionGovernancePath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalRetentionGovernance.cs'
$retentionConstantsPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalRetentionConstants.cs'
$queryProtectionPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalQueryProtection.cs'
$lifecycleClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalConflictLifecycleClassifier.cs'
$retentionControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditRetentionController.cs'
$retentionSummaryServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalRetentionSummaryService.cs'
$forensicSnapshotDtoPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalForensicSnapshotDto.cs'
if (Test-Path $retentionGovernancePath) {
    $rgRaw = Get-Content $retentionGovernancePath -Raw -Encoding UTF8
    $operationalRetentionProtectionCount = @([regex]::Matches($rgRaw, 'no automatic pruning|ClassifyRetention')).Count
}
if (Test-Path $retentionConstantsPath) {
    $rcRaw = Get-Content $retentionConstantsPath -Raw -Encoding UTF8
    $operationalRetentionProtectionCount += @([regex]::Matches($rcRaw, 'HotOperationalWindowDays|MaxQueryDateRangeDays')).Count
}
if (Test-Path $queryProtectionPath) {
    $qpRaw = Get-Content $queryProtectionPath -Raw -Encoding UTF8
    $queryClampProtectionCount = @([regex]::Matches($qpRaw, 'NormalizeDateRange|NormalizePageSize|DateRangeClamped')).Count
}
if (Test-Path (Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalAuditQueryService.cs')) {
    $auditQRaw = Get-Content (Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalAuditQueryService.cs') -Raw -Encoding UTF8
    $queryClampProtectionCount += @([regex]::Matches($auditQRaw, 'Operational query protection:')).Count
}
if (Test-Path (Join-Path $root 'Tannous.Pos.Infrastructure\Services\SyncConflictReconciliationService.cs')) {
    $reconRaw = Get-Content (Join-Path $root 'Tannous.Pos.Infrastructure\Services\SyncConflictReconciliationService.cs') -Raw -Encoding UTF8
    $queryClampProtectionCount += @([regex]::Matches($reconRaw, 'Operational query protection:')).Count
    $agedConflictClassificationCount = @([regex]::Matches($reconRaw, 'AgingSeverity|EscalationRecommendation|OperationalConflictLifecycleClassifier')).Count
}
if (Test-Path $lifecycleClassifierPath) {
    $lcRaw = Get-Content $lifecycleClassifierPath -Raw -Encoding UTF8
    $agedConflictClassificationCount += @([regex]::Matches($lcRaw, 'ClassifyAgingSeverity|GetEscalationRecommendation')).Count
}
if (Test-Path $retentionControllerPath) {
    $retCtlRaw = Get-Content $retentionControllerPath -Raw -Encoding UTF8
    $retentionSummaryEndpointCount = @([regex]::Matches($retCtlRaw, '\[HttpGet\(')).Count
}
if (Test-Path $retentionSummaryServicePath) {
    $retSvcRaw = Get-Content $retentionSummaryServicePath -Raw -Encoding UTF8
    $operationalRetentionProtectionCount += @([regex]::Matches($retSvcRaw, 'Operational retention observability:')).Count
}
if (Test-Path $forensicServicePath) {
    $forensicSvcRaw2 = Get-Content $forensicServicePath -Raw -Encoding UTF8
    $forensicTruncationObservabilityCount = @([regex]::Matches($forensicSvcRaw2, 'Operational export survivability:|forensic snapshot truncated')).Count
    $exportSurvivabilityMetadataCount = @([regex]::Matches($forensicSvcRaw2, 'SnapshotSchemaVersion|ExportSource|RetentionClassification|TruncationFlags')).Count
}
if (Test-Path $forensicSnapshotDtoPath) {
    $fsDtoRaw = Get-Content $forensicSnapshotDtoPath -Raw -Encoding UTF8
    $exportSurvivabilityMetadataCount += @([regex]::Matches($fsDtoRaw, 'SnapshotGeneratedUtc|SnapshotSchemaVersion|TruncationFlags|RetentionClassification')).Count
    $exportSurvivabilityMetadataCount += @([regex]::Matches($fsDtoRaw, 'ExportPressureClassification|TruncationSeverity|ExportSurvivabilityWarning')).Count
}

$degradedModeClassificationCount = 0
$resilienceEndpointCount = 0
$replayStormVisibilityCount = 0
$exportPressureClassificationCount = 0
$auditPersistenceResilienceCount = 0
$operationalPressureIndicatorCount = 0
$backpressureObservabilityCount = 0
$resilienceGovernancePath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalResilienceGovernance.cs'
$resilienceClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalDegradedModeClassifier.cs'
$resilienceControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditResilienceController.cs'
$resilienceServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalResilienceDiagnosticsService.cs'
$auditRecorderPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalAuditRecorder.cs'
if (Test-Path $resilienceClassifierPath) {
    $rcRaw = Get-Content $resilienceClassifierPath -Raw -Encoding UTF8
    $degradedModeClassificationCount = @([regex]::Matches($rcRaw, 'ClassifyPrimary|ClassifyActiveModes|ClassifyExportTruncationSeverity')).Count
}
if (Test-Path $resilienceControllerPath) {
    $resCtlRaw = Get-Content $resilienceControllerPath -Raw -Encoding UTF8
    $resilienceEndpointCount = @([regex]::Matches($resCtlRaw, '\[HttpGet\(')).Count
}
if (Test-Path $resilienceServicePath) {
    $resSvcRaw = Get-Content $resilienceServicePath -Raw -Encoding UTF8
    $replayStormVisibilityCount = @([regex]::Matches($resSvcRaw, 'replay storm risk|ReplayStormRisk')).Count
    $operationalPressureIndicatorCount = @([regex]::Matches($resSvcRaw, 'pressure indicators aggregated|BuildPressureIndicators')).Count
    $backpressureObservabilityCount = @([regex]::Matches($resSvcRaw, 'Operational backpressure visibility:')).Count
}
if (Test-Path $auditRecorderPath) {
    $arRaw = Get-Content $auditRecorderPath -Raw -Encoding UTF8
    $auditPersistenceResilienceCount = @([regex]::Matches($arRaw, 'audit persistence failure classified|audit persistence pressure|IOperationalAuditPersistenceTelemetry')).Count
}
if (Test-Path $forensicServicePath) {
    $forensicSvcResRaw = Get-Content $forensicServicePath -Raw -Encoding UTF8
    $exportPressureClassificationCount = @([regex]::Matches($forensicSvcResRaw, 'ExportPressureClassification|export pressure classification')).Count
    $backpressureObservabilityCount += @([regex]::Matches($forensicSvcResRaw, 'Operational backpressure visibility:')).Count
}
if (Test-Path $resilienceGovernancePath) {
    $rgResRaw = Get-Content $resilienceGovernancePath -Raw -Encoding UTF8
    $degradedModeClassificationCount += @([regex]::Matches($rgResRaw, 'GetSurvivabilityAssumption|degraded')).Count
}

$incidentCorrelationEndpointCount = 0
$causalityObservabilityCount = 0
$correlatedRiskClassificationCount = 0
$cascadingDegradationCount = 0
$forensicIncidentEnrichmentCount = 0
$replayIncidentAggregationCount = 0
$incidentSeverityClassificationCount = 0
$incidentCorrelationServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalIncidentCorrelationService.cs'
$incidentControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditIncidentsController.cs'
$incidentRiskClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalIncidentRiskClassifier.cs'
$forensicDtoPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalForensicSnapshotDto.cs'
if (Test-Path $incidentControllerPath) {
    $incCtlRaw = Get-Content $incidentControllerPath -Raw -Encoding UTF8
    $incidentCorrelationEndpointCount = @([regex]::Matches($incCtlRaw, '\[HttpGet\(')).Count
}
if (Test-Path $incidentCorrelationServicePath) {
    $incSvcRaw = Get-Content $incidentCorrelationServicePath -Raw -Encoding UTF8
    $causalityObservabilityCount = @([regex]::Matches($incSvcRaw, 'Operational causality visibility:')).Count
    $correlatedRiskClassificationCount = @([regex]::Matches($incSvcRaw, 'Operational correlation risk:|OperationalIncidentRiskClassifier')).Count
    $cascadingDegradationCount = @([regex]::Matches($incSvcRaw, 'cascading degradation|CascadingDegradation')).Count
    $replayIncidentAggregationCount = @([regex]::Matches($incSvcRaw, 'ReplayIncident|ReplayMismatch')).Count
}
if (Test-Path $incidentRiskClassifierPath) {
    $ircRaw = Get-Content $incidentRiskClassifierPath -Raw -Encoding UTF8
    $incidentSeverityClassificationCount = @([regex]::Matches($ircRaw, 'ClassifySeverity|ClassifyCorrelatedRisk')).Count
}
if (Test-Path $forensicDtoPath) {
    $forensicDtoRaw = Get-Content $forensicDtoPath -Raw -Encoding UTF8
    $forensicIncidentEnrichmentCount = @([regex]::Matches($forensicDtoRaw, 'CorrelatedIncidentRisk|IncidentCorrelationSummary')).Count
}
if (Test-Path (Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalForensicSnapshotService.cs')) {
    $forensicIncRaw = Get-Content (Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalForensicSnapshotService.cs') -Raw -Encoding UTF8
    $forensicIncidentEnrichmentCount += @([regex]::Matches($forensicIncRaw, 'BuildForensicCorrelation')).Count
}

$operationalAlertSignalCount = 0
$criticalAlertVisibilityCount = 0
$alertEscalationObservabilityCount = 0
$replayPressureAlertCount = 0
$inventoryRiskAlertCount = 0
$alertDiagnosticsEndpointCount = 0
$alertServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalAlertSignalService.cs'
$alertControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAlertDiagnosticsController.cs'
$alertTypesPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalAlertTypes.cs'
if (Test-Path $alertTypesPath) {
    $alertTypesRaw = Get-Content $alertTypesPath -Raw -Encoding UTF8
    $operationalAlertSignalCount = @([regex]::Matches($alertTypesRaw, 'public const string')).Count
}
if (Test-Path $alertServicePath) {
    $alertSvcRaw = Get-Content $alertServicePath -Raw -Encoding UTF8
    $criticalAlertVisibilityCount = @([regex]::Matches($alertSvcRaw, 'Operational escalation visibility:')).Count
    $alertEscalationObservabilityCount = @([regex]::Matches($alertSvcRaw, 'Operational alert visibility:|Operational pressure escalation:')).Count
    $replayPressureAlertCount = @([regex]::Matches($alertSvcRaw, 'OperationalAlertTypes\.ReplayStormRisk')).Count
    $inventoryRiskAlertCount = @([regex]::Matches($alertSvcRaw, 'OperationalAlertTypes\.InventoryDriftEscalation')).Count
}
if (Test-Path $alertControllerPath) {
    $alertCtlRaw = Get-Content $alertControllerPath -Raw -Encoding UTF8
    $alertDiagnosticsEndpointCount = @([regex]::Matches($alertCtlRaw, '\[HttpGet\(')).Count
}
if (Test-Path $forensicDtoPath) {
    $forensicDtoRaw = Get-Content $forensicDtoPath -Raw -Encoding UTF8
    $forensicIncidentEnrichmentCount += @([regex]::Matches($forensicDtoRaw, 'AlertSignals|AlertSummary|EscalationRisk')).Count
}

$cacheHitObservabilityCount = 0
$cacheMissObservabilityCount = 0
$cacheTtlGovernanceCount = 0
$staleRiskVisibilityCount = 0
$cacheServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalDiagnosticsCacheService.cs'
$cacheGovernancePath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalDiagnosticsCacheGovernance.cs'
$cacheConstantsPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalDiagnosticsCacheConstants.cs'
if (Test-Path $cacheServicePath) {
    $cacheSvcRaw = Get-Content $cacheServicePath -Raw -Encoding UTF8
    $cacheHitObservabilityCount = @([regex]::Matches($cacheSvcRaw, 'Operational cache observability: cache hit')).Count
    $cacheMissObservabilityCount = @([regex]::Matches($cacheSvcRaw, 'Operational cache observability: cache miss')).Count
    $staleRiskVisibilityCount = @([regex]::Matches($cacheSvcRaw, 'Operational stale snapshot risk:')).Count
}
if (Test-Path $cacheGovernancePath) {
    $cacheGovRaw = Get-Content $cacheGovernancePath -Raw -Encoding UTF8
    $cacheTtlGovernanceCount += @([regex]::Matches($cacheGovRaw, 'GOVERNANCE')).Count
}
if (Test-Path $cacheConstantsPath) {
    $cacheConstRaw = Get-Content $cacheConstantsPath -Raw -Encoding UTF8
    $cacheTtlGovernanceCount += @([regex]::Matches($cacheConstRaw, 'TtlSeconds')).Count
}

$resilienceCacheReuseCount = 0
$cachePressureBypassCount = 0
$staleSnapshotRiskCount = 0
$resilienceServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalResilienceDiagnosticsService.cs'
$reconciliationCacheReuseCount = 0
$incidentCacheReuseCount = 0
$incidentGroupReuseCount = 0
if (Test-Path $reconciliationServicePath) {
    $reconSvcRaw = Get-Content $reconciliationServicePath -Raw -Encoding UTF8
    $reconciliationCacheReuseCount = @([regex]::Matches($reconSvcRaw, 'GetSummaryCachedAsync')).Count
    $cachePressureBypassCount += @([regex]::Matches($reconSvcRaw, 'Operational cache pressure escalation:')).Count
}
$incidentCorrelationServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalIncidentCorrelationService.cs'
if (Test-Path $incidentCorrelationServicePath) {
    $incidentSvcRaw = Get-Content $incidentCorrelationServicePath -Raw -Encoding UTF8
    $incidentCacheReuseCount = @([regex]::Matches($incidentSvcRaw, 'GetIncidentGroupsCachedAsync')).Count
    $incidentGroupReuseCount = $incidentCacheReuseCount
}
if (Test-Path $resilienceServicePath) {
    $resilienceSvcRaw = Get-Content $resilienceServicePath -Raw -Encoding UTF8
    $resilienceCacheReuseCount = @([regex]::Matches($resilienceSvcRaw, 'GetMetricsSnapshotCachedAsync')).Count
    $cachePressureBypassCount += @([regex]::Matches($resilienceSvcRaw, 'Operational cache pressure escalation:')).Count
}
if (Test-Path $cacheServicePath) {
    $cacheSvcRaw2 = Get-Content $cacheServicePath -Raw -Encoding UTF8
    $staleSnapshotRiskCount = @([regex]::Matches($cacheSvcRaw2, 'Operational stale snapshot risk:')).Count
}

$alertCacheReuseCount = 0
$forensicCompactSummaryCount = 0
$forensicLiveExportProtectionCount = 0
$cachedOperationalCompositionCount = 0
$alertServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalAlertSignalService.cs'
$forensicSnapshotServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalForensicSnapshotService.cs'
$forensicSummaryDtoPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalForensicSnapshotSummaryDto.cs'
if (Test-Path $alertServicePath) {
    $alertSvcRaw = Get-Content $alertServicePath -Raw -Encoding UTF8
    $alertCacheReuseCount = @([regex]::Matches($alertSvcRaw, 'LoadCachedUpstreamDiagnosticsAsync')).Count
    $cachedOperationalCompositionCount += @([regex]::Matches($alertSvcRaw, 'LoadCachedUpstreamDiagnosticsAsync|cached upstream')).Count
}
if (Test-Path $forensicSnapshotServicePath) {
    $forensicSvcRaw = Get-Content $forensicSnapshotServicePath -Raw -Encoding UTF8
    $forensicCompactSummaryCount = @([regex]::Matches($forensicSvcRaw, 'BuildCompactForensicSummary|OperationalForensicSnapshotSummaryDto')).Count
    $forensicLiveExportProtectionCount = @([regex]::Matches($forensicSvcRaw, 'source-of-truth|Live forensic exports|no caching of')).Count
    $cachedOperationalCompositionCount += @([regex]::Matches($forensicSvcRaw, 'GetSummaryAsync|cached upstream')).Count
}
if (Test-Path $forensicSummaryDtoPath) {
    $forensicSummaryDtoRaw = Get-Content $forensicSummaryDtoPath -Raw -Encoding UTF8
    $forensicCompactSummaryCount += @([regex]::Matches($forensicSummaryDtoRaw, 'GOVERNANCE:')).Count
}

$cacheDiagnosticsEndpointCount = 0
$cacheEffectivenessVisibilityCount = 0
$cacheMetadataProjectionCount = 0
$cachePressureVisibilityCount = 0
$cacheDiagnosticsControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditCacheDiagnosticsController.cs'
$cacheDiagnosticsServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalDiagnosticsCacheDiagnosticsService.cs'
if (Test-Path $cacheDiagnosticsControllerPath) {
    $cacheDiagCtrlRaw = Get-Content $cacheDiagnosticsControllerPath -Raw -Encoding UTF8
    $cacheDiagnosticsEndpointCount = @([regex]::Matches($cacheDiagCtrlRaw, '\[HttpGet\(')).Count
}
if (Test-Path $cacheDiagnosticsServicePath) {
    $cacheDiagSvcRaw = Get-Content $cacheDiagnosticsServicePath -Raw -Encoding UTF8
    $cacheEffectivenessVisibilityCount = @([regex]::Matches($cacheDiagSvcRaw, 'Operational cache effectiveness:')).Count
    $cachePressureVisibilityCount = @([regex]::Matches($cacheDiagSvcRaw, 'Operational cache pressure visibility:')).Count
}
if (Test-Path $cacheServicePath) {
    $cacheMetadataProjectionCount = @([regex]::Matches((Get-Content $cacheServicePath -Raw -Encoding UTF8), 'GetDiagnosticsEntryMetadata|CacheKeyAlias')).Count
}

$cacheInvalidationCount = 0
$scopedCacheKeyCount = 0
$alertCacheLayerCount = 0
$cacheFreshnessRecoveryCount = 0
$targetedInvalidationHookCount = 0
$keyFactoryPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalDiagnosticsCacheKeyFactory.cs'
$invalidatorPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalDiagnosticsCacheInvalidator.cs'
$alertServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalAlertSignalService.cs'
if (Test-Path $cacheServicePath) {
    $cacheInvalidationCount += @([regex]::Matches((Get-Content $cacheServicePath -Raw -Encoding UTF8), 'Operational cache invalidation:')).Count
}
if (Test-Path $keyFactoryPath) {
    $scopedCacheKeyCount = @([regex]::Matches((Get-Content $keyFactoryPath -Raw -Encoding UTF8), 'Build\w+')).Count
}
if (Test-Path $alertServicePath) {
    $alertCacheLayerCount = @([regex]::Matches((Get-Content $alertServicePath -Raw -Encoding UTF8), 'GetSignalsCachedAsync|Operational alert cache reuse:')).Count
}
if (Test-Path $invalidatorPath) {
    $targetedInvalidationHookCount = @([regex]::Matches((Get-Content $invalidatorPath -Raw -Encoding UTF8), 'InvalidateAfter')).Count
    $cacheFreshnessRecoveryCount = @([regex]::Matches((Get-Content $invalidatorPath -Raw -Encoding UTF8), 'RemoveByPrefix|RemoveAllDiagnosticsCaches')).Count
}

$adaptiveTtlReductionCount = 0
$warmCandidateVisibilityCount = 0
$cacheReadinessSignalCount = 0
$cacheStabilityClassificationCount = 0
$predictiveWarmRecommendationCount = 0
$adaptiveClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheAdaptiveTtlClassifier.cs'
$adaptiveDiagnosticsPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalDiagnosticsCacheDiagnosticsService.cs'
$adaptiveHelperPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalCacheAdaptiveTtlHelper.cs'
$stabilityClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheStabilityClassifier.cs'
if (Test-Path $adaptiveHelperPath) {
    $adaptiveTtlReductionCount = @([regex]::Matches((Get-Content $adaptiveHelperPath -Raw -Encoding UTF8), 'Operational adaptive TTL reduction:')).Count
}
if (Test-Path $adaptiveDiagnosticsPath) {
    $warmCandidateVisibilityCount = @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational cache warming visibility:')).Count
    $cacheReadinessSignalCount = @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'ReadinessState|OperationalCacheReadinessState')).Count
}
if (Test-Path $stabilityClassifierPath) {
    $cacheStabilityClassificationCount = @([regex]::Matches((Get-Content $stabilityClassifierPath -Raw -Encoding UTF8), 'StabilityClassification|"Stable"|"Recovering"|"Degraded"|"Unstable"')).Count
}
if (Test-Path $adaptiveClassifierPath) {
    $predictiveWarmRecommendationCount = @([regex]::Matches((Get-Content (Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheAdaptiveInsights.cs') -Raw -Encoding UTF8), 'WarmCandidate|WarmingRecommended|WarmRecommendations')).Count
}

$cacheCardinalityGovernanceCount = 0
$cacheGovernanceOverviewCount = 0
$cachePressureClassificationCount = 0
$cacheDegradationVisibilityCount = 0
$scopedCacheSurvivabilityCount = 0
$cacheScopeChurnVisibilityCount = 0
$cardinalityClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheCardinalityClassifier.cs'
$governanceProjectionPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheGovernanceProjectionBuilder.cs'
$pressureClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCachePressureClassifier.cs'
$degradationClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheDegradationClassifier.cs'
$scopeSurvivabilityPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheScopeSurvivabilityBuilder.cs'
if (Test-Path $cardinalityClassifierPath) {
    $cacheCardinalityGovernanceCount = @([regex]::Matches((Get-Content $cardinalityClassifierPath -Raw -Encoding UTF8), 'OperationalCacheCardinality')).Count
}
if (Test-Path $adaptiveDiagnosticsPath) {
    $cacheGovernanceOverviewCount = @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational cache governance overview:|GetGovernanceOverviewAsync')).Count
    $cacheDegradationVisibilityCount = @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational cache degradation:')).Count
    $scopedCacheSurvivabilityCount = @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational cache survivability:')).Count
    $cachePressureClassificationCount = @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational cache pressure classification:')).Count
}
if (Test-Path $scopeSurvivabilityPath) {
    $cacheScopeChurnVisibilityCount = @([regex]::Matches((Get-Content $scopeSurvivabilityPath -Raw -Encoding UTF8), 'ScopeChurnRatio|InvalidationsByScopedKeys')).Count
}

$governanceAuditProjectionCount = 0
$governanceDriftVisibilityCount = 0
$diagnosticsConsistencyVisibilityCount = 0
$survivabilityClassificationCount = 0
$operatorRecommendationVisibilityCount = 0
$cacheExplainabilitySignalCount = 0
$auditBuilderPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheGovernanceAuditBuilder.cs'
$driftDetectorPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheGovernanceDriftDetector.cs'
$consistencyValidatorPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheGovernanceConsistencyValidator.cs'
$survivabilityClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheSurvivabilityClassifier.cs'
$explainabilityBuilderPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheExplainabilityBuilder.cs'
$recommendationBuilderPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheGovernanceRecommendationBuilder.cs'
if (Test-Path $auditBuilderPath) {
    $governanceAuditProjectionCount = @([regex]::Matches((Get-Content $auditBuilderPath -Raw -Encoding UTF8), 'OperationalCacheGovernanceAuditDto|Build\(')).Count
}
if (Test-Path $driftDetectorPath) {
    $governanceDriftVisibilityCount = @([regex]::Matches((Get-Content $driftDetectorPath -Raw -Encoding UTF8), 'DriftDetected|DriftSignals')).Count
}
if (Test-Path $consistencyValidatorPath) {
    $diagnosticsConsistencyVisibilityCount = @([regex]::Matches((Get-Content $consistencyValidatorPath -Raw -Encoding UTF8), 'IsConsistent|InconsistencySignals')).Count
}
if (Test-Path $survivabilityClassifierPath) {
    $survivabilityClassificationCount = @([regex]::Matches((Get-Content $survivabilityClassifierPath -Raw -Encoding UTF8), 'SurvivabilityClassification|SurvivabilityScore')).Count
}
if (Test-Path $recommendationBuilderPath) {
    $operatorRecommendationVisibilityCount = @([regex]::Matches((Get-Content $recommendationBuilderPath -Raw -Encoding UTF8), 'OperationalCacheGovernanceRecommendationDto')).Count
}
if (Test-Path $explainabilityBuilderPath) {
    $cacheExplainabilitySignalCount = @([regex]::Matches((Get-Content $explainabilityBuilderPath -Raw -Encoding UTF8), 'ReasonCodes|TriggerSignals|HighBypassRatio|FrequentColdMisses')).Count
}
if (Test-Path $adaptiveDiagnosticsPath) {
    $governanceAuditProjectionCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational cache governance audit:')).Count
    $governanceDriftVisibilityCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational governance drift visibility:')).Count
    $diagnosticsConsistencyVisibilityCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational diagnostics consistency:')).Count
    $survivabilityClassificationCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational cache survivability scoring:')).Count
    $operatorRecommendationVisibilityCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational operator guidance:')).Count
}

$cacheInvalidationAuditCount = 0
$freshnessRecoveryVisibilityCount = 0
$crossCategoryInvalidationCount = 0
$cacheRecoveryGuidanceCount = 0
$invalidationDriftVisibilityCount = 0
$invalidationProjectionPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheInvalidationProjectionBuilder.cs'
$invalidationExplainabilityPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheInvalidationExplainabilityBuilder.cs'
$invalidationDriftPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheInvalidationDriftDetector.cs'
if (Test-Path $invalidationProjectionPath) {
    $cacheInvalidationAuditCount = @([regex]::Matches((Get-Content $invalidationProjectionPath -Raw -Encoding UTF8), 'OperationalCacheInvalidationAuditDto|BuildAudit')).Count
}
if (Test-Path $invalidationExplainabilityPath) {
    $crossCategoryInvalidationCount = @([regex]::Matches((Get-Content $invalidationExplainabilityPath -Raw -Encoding UTF8), 'CrossCategoryCascade|HighScopedInvalidationChurn|FrequentFreshnessRecovery')).Count
    $freshnessRecoveryVisibilityCount = @([regex]::Matches((Get-Content $invalidationExplainabilityPath -Raw -Encoding UTF8), 'FreshnessRecovery|RecoveryState')).Count
}
if (Test-Path $invalidationDriftPath) {
    $invalidationDriftVisibilityCount = @([regex]::Matches((Get-Content $invalidationDriftPath -Raw -Encoding UTF8), 'InvalidationDrift|InconsistencySignals')).Count
}
if (Test-Path $adaptiveDiagnosticsPath) {
    $cacheInvalidationAuditCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational invalidation governance:')).Count
    $freshnessRecoveryVisibilityCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational freshness recovery:')).Count
    $invalidationDriftVisibilityCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational invalidation drift:')).Count
    $cacheRecoveryGuidanceCount = @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational cache recovery guidance:')).Count
    $crossCategoryInvalidationCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'CrossCategoryInvalidations|RecordCrossCategoryInvalidation')).Count
}

$consistencyRecoveryVisibilityCount = 0
$containmentGovernanceCount = 0
$propagationDiagnosticsCount = 0
$consistencyConfidenceVisibilityCount = 0
$recoveryStabilizationSignalCount = 0
$consistencyProjectionPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheConsistencyProjectionBuilder.cs'
$consistencyExplainabilityPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCacheConsistencyExplainabilityBuilder.cs'
$propagationDetectorPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalCachePropagationDetector.cs'
if (Test-Path $consistencyProjectionPath) {
    $consistencyRecoveryVisibilityCount = @([regex]::Matches((Get-Content $consistencyProjectionPath -Raw -Encoding UTF8), 'OperationalCacheConsistencyRecoveryDto|BuildRecovery')).Count
    $containmentGovernanceCount = @([regex]::Matches((Get-Content $consistencyProjectionPath -Raw -Encoding UTF8), 'OperationalCacheContainmentAuditDto|BuildContainmentAudit')).Count
    $propagationDiagnosticsCount = @([regex]::Matches((Get-Content $consistencyProjectionPath -Raw -Encoding UTF8), 'OperationalCachePropagationDiagnosticsDto|BuildPropagationDiagnostics')).Count
}
if (Test-Path $consistencyExplainabilityPath) {
    $consistencyConfidenceVisibilityCount = @([regex]::Matches((Get-Content $consistencyExplainabilityPath -Raw -Encoding UTF8), 'ConfidenceDropDetected|RecoveryWindowExtended|PropagationEscalated')).Count
}
if (Test-Path $propagationDetectorPath) {
    $propagationDiagnosticsCount += @([regex]::Matches((Get-Content $propagationDetectorPath -Raw -Encoding UTF8), 'PropagationSeverity|MultiCategoryExposure')).Count
}
if (Test-Path $adaptiveDiagnosticsPath) {
    $consistencyRecoveryVisibilityCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational consistency recovery:')).Count
    $containmentGovernanceCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational containment governance:')).Count
    $propagationDiagnosticsCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational propagation visibility:')).Count
    $consistencyConfidenceVisibilityCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational consistency confidence:')).Count
    $recoveryStabilizationSignalCount = @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational recovery stabilization:')).Count
}

$pressureLifecycleGovernanceCount = 0
$pressureRecoveryGovernanceCount = 0
$pressureConvergenceGovernanceCount = 0
$pressureResetCoordinatorCount = 0
$pressureStabilizationResetCount = 0
$pressureProjectionPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalPressureGovernanceProjectionBuilder.cs'
$pressureResetPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalDiagnosticsPressureResetCoordinator.cs'
if (Test-Path $pressureProjectionPath) {
    $pressureLifecycleGovernanceCount = @([regex]::Matches((Get-Content $pressureProjectionPath -Raw -Encoding UTF8), 'OperationalPressureLifecycleDto|BuildLifecycle')).Count
    $pressureRecoveryGovernanceCount = @([regex]::Matches((Get-Content $pressureProjectionPath -Raw -Encoding UTF8), 'OperationalPressureRecoveryDto|BuildRecovery')).Count
    $pressureConvergenceGovernanceCount = @([regex]::Matches((Get-Content $pressureProjectionPath -Raw -Encoding UTF8), 'OperationalPressureConvergenceDto|BuildConvergence')).Count
}
if (Test-Path $pressureResetPath) {
    $pressureResetCoordinatorCount = @([regex]::Matches((Get-Content $pressureResetPath -Raw -Encoding UTF8), 'IOperationalDiagnosticsPressureResetCoordinator|ResetGovernanceState')).Count
    $pressureStabilizationResetCount = @([regex]::Matches((Get-Content $pressureResetPath -Raw -Encoding UTF8), 'Operational pressure governance reset:')).Count
}
if (Test-Path $adaptiveDiagnosticsPath) {
    $pressureLifecycleGovernanceCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational pressure lifecycle:')).Count
    $pressureRecoveryGovernanceCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational pressure recovery:')).Count
    $pressureConvergenceGovernanceCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational pressure convergence:')).Count
    $recoveryStabilizationSignalCount += @([regex]::Matches((Get-Content $adaptiveDiagnosticsPath -Raw -Encoding UTF8), 'Operational recovery stabilization:')).Count
}

$governanceExplainabilityComposerCount = 0
$governanceCompositionContextCount = 0
$governanceProjectionCollaboratorCount = 0
$governanceSurfaceBudgetCount = 0
$governanceThresholdEvaluatorCount = 0
$composerPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalGovernanceExplainabilityComposer.cs'
$contextPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalGovernanceCompositionContextBuilder.cs'
$budgetPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalGovernanceSurfaceBudget.cs'
$thresholdPath = Join-Path $root 'Tannous.Pos.Application\Audit\OperationalGovernanceThresholdEvaluator.cs'
$collaboratorGlob = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalDiagnosticsProjections\*.cs'
if (Test-Path $composerPath) {
    $governanceExplainabilityComposerCount = @([regex]::Matches((Get-Content $composerPath -Raw -Encoding UTF8), 'OperationalGovernanceExplainabilityComposer|Compose\(')).Count
}
if (Test-Path $contextPath) {
    $governanceCompositionContextCount = @([regex]::Matches((Get-Content $contextPath -Raw -Encoding UTF8), 'OperationalGovernanceCompositionContext|Build\(')).Count
}
if (Test-Path $budgetPath) {
    $governanceSurfaceBudgetCount = @([regex]::Matches((Get-Content $budgetPath -Raw -Encoding UTF8), 'OperationalGovernanceSurfaceBudget|MaxCacheDiagnosticsGetEndpoints')).Count
}
if (Test-Path $thresholdPath) {
    $governanceThresholdEvaluatorCount = @([regex]::Matches((Get-Content $thresholdPath -Raw -Encoding UTF8), 'OperationalGovernanceThresholdEvaluator|ComputeHitRatio|ComputeBypassRatio')).Count
}
if (Test-Path (Split-Path $collaboratorGlob)) {
    $governanceProjectionCollaboratorCount = @(Get-ChildItem $collaboratorGlob -ErrorAction SilentlyContinue).Count
}

$governanceModuleRegistryCount = 0
$governancePipelineStageCount = 0
$governanceConventionsCount = 0
$governanceComplexityBudgetCount = 0
$governanceModulePath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\Modules\OperationalGovernanceModuleRegistry.cs'
$pipelinePath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceProjectionPipeline.cs'
$conventionsPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceConventions.cs'
$complexityPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceComplexityMetrics.cs'
if (Test-Path $governanceModulePath) {
    $governanceModuleRegistryCount = @([regex]::Matches((Get-Content $governanceModulePath -Raw -Encoding UTF8), 'OperationalGovernanceModuleRegistry|IOperationalGovernanceModule')).Count
}
if (Test-Path $pipelinePath) {
    $governancePipelineStageCount = @([regex]::Matches((Get-Content $pipelinePath -Raw -Encoding UTF8), 'OperationalGovernanceProjectionStages|StageOrder')).Count
    $governanceCompositionContextCount += @([regex]::Matches((Get-Content $pipelinePath -Raw -Encoding UTF8), 'OperationalGovernanceCompositionContext')).Count
}
if (Test-Path $conventionsPath) {
    $governanceConventionsCount = @([regex]::Matches((Get-Content $conventionsPath -Raw -Encoding UTF8), 'OperationalGovernanceConventions|NamingStandards|NonGoals')).Count
}
if (Test-Path $complexityPath) {
    $governanceComplexityBudgetCount = @([regex]::Matches((Get-Content $complexityPath -Raw -Encoding UTF8), 'OperationalGovernanceComplexityMetrics|IsWithinBudget')).Count
}

$governanceRuntimeProtectionCount = 0
$telemetrySaturationVisibilityCount = 0
$governanceFailsafeVisibilityCount = 0
$runtimeBudgetEnforcementCount = 0
$projectionComplexityClassificationCount = 0
$runtimeBudgetPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceRuntimeBudget.cs'
$runtimeProtectionBuilderPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceRuntimeProtectionBuilder.cs'
$runtimeProtectionCollaboratorPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalDiagnosticsProjections\OperationalDiagnosticsCacheRuntimeProtectionProjectionCollaborator.cs'
$complexityClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceProjectionComplexityClassifier.cs'
$failsafeClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceFailsafeClassifier.cs'
$telemetrySaturationClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceTelemetrySaturationClassifier.cs'
if (Test-Path $runtimeProtectionBuilderPath) {
    $governanceRuntimeProtectionCount = @([regex]::Matches((Get-Content $runtimeProtectionBuilderPath -Raw -Encoding UTF8), 'OperationalGovernanceRuntimeProtection|Build\(')).Count
}
if (Test-Path $telemetrySaturationClassifierPath) {
    $telemetrySaturationVisibilityCount = @([regex]::Matches((Get-Content $telemetrySaturationClassifierPath -Raw -Encoding UTF8), 'OperationalGovernanceTelemetrySaturation|Classify|Saturation')).Count
}
if (Test-Path $failsafeClassifierPath) {
    $governanceFailsafeVisibilityCount = @([regex]::Matches((Get-Content $failsafeClassifierPath -Raw -Encoding UTF8), 'OperationalGovernanceFailsafe|Failsafe|IsFailsafeActive')).Count
}
if (Test-Path $runtimeBudgetPath) {
    $runtimeBudgetEnforcementCount = @([regex]::Matches((Get-Content $runtimeBudgetPath -Raw -Encoding UTF8), 'OperationalGovernanceRuntimeBudget|ClampOrdered|MaxExplainabilitySignals')).Count
}
if (Test-Path $complexityClassifierPath) {
    $projectionComplexityClassificationCount = @([regex]::Matches((Get-Content $complexityClassifierPath -Raw -Encoding UTF8), 'OperationalGovernanceProjectionComplexity|Classify')).Count
}
if (Test-Path $runtimeProtectionCollaboratorPath) {
    $governanceRuntimeProtectionCount += @([regex]::Matches((Get-Content $runtimeProtectionCollaboratorPath -Raw -Encoding UTF8), 'runtime protection|GetRuntimeProtection')).Count
}

$governanceSnapshotReuseCount = 0
$projectionReuseVisibilityCount = 0
$snapshotConsistencyVisibilityCount = 0
$governanceSnapshotFreshnessCount = 0
$projectionReuseEfficiencyCount = 0
$snapshotBuilderPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceSnapshotBuilder.cs'
$snapshotStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalDiagnosticsProjections\OperationalGovernanceSnapshotStore.cs'
$snapshotFreshnessPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceSnapshotFreshnessClassifier.cs'
$snapshotReuseClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceProjectionReuseClassifier.cs'
$snapshotConsistencyClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceProjectionConsistencyClassifier.cs'
$snapshotCollaboratorPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalDiagnosticsProjections\OperationalGovernanceSnapshotProjectionCollaborator.cs'
if (Test-Path $snapshotBuilderPath) {
    $governanceSnapshotReuseCount = @([regex]::Matches((Get-Content $snapshotBuilderPath -Raw -Encoding UTF8), 'OperationalGovernanceSnapshot|BuildComposition|BuildSnapshotDto')).Count
}
if (Test-Path $snapshotStorePath) {
    $governanceSnapshotReuseCount += @([regex]::Matches((Get-Content $snapshotStorePath -Raw -Encoding UTF8), 'OperationalGovernanceSnapshotStore|Acquire|InvalidateAll')).Count
}
if (Test-Path $snapshotFreshnessPath) {
    $governanceSnapshotFreshnessCount = @([regex]::Matches((Get-Content $snapshotFreshnessPath -Raw -Encoding UTF8), 'OperationalGovernanceSnapshotFreshness|Classify|Freshness')).Count
}
if (Test-Path $snapshotReuseClassifierPath) {
    $projectionReuseVisibilityCount = @([regex]::Matches((Get-Content $snapshotReuseClassifierPath -Raw -Encoding UTF8), 'OperationalGovernanceProjectionReuse|Classify|ReuseLevel')).Count
}
if (Test-Path $snapshotConsistencyClassifierPath) {
    $snapshotConsistencyVisibilityCount = @([regex]::Matches((Get-Content $snapshotConsistencyClassifierPath -Raw -Encoding UTF8), 'OperationalGovernanceProjectionConsistency|ConsistencyLevel|Classify')).Count
}
if (Test-Path $snapshotCollaboratorPath) {
    $projectionReuseEfficiencyCount = @([regex]::Matches((Get-Content $snapshotCollaboratorPath -Raw -Encoding UTF8), 'projection reuse|GetProjectionReuse|GetGovernanceSnapshot')).Count
}

$governanceFingerprintVisibilityCount = 0
$governanceDriftAnalysisCount = 0
$replayConsistencyVisibilityCount = 0
$governanceSignatureTransitionCount = 0
$projectionFingerprintDeterminismCount = 0
$fingerprintBuilderPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceFingerprintBuilder.cs'
$fingerprintComparerPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceFingerprintComparer.cs'
$fingerprintCollaboratorPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalDiagnosticsProjections\OperationalGovernanceFingerprintProjectionCollaborator.cs'
$fingerprintHistoryPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalDiagnosticsProjections\OperationalGovernanceFingerprintHistoryStore.cs'
$driftAnalysisBuilderPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceDriftAnalysisBuilder.cs'
if (Test-Path $fingerprintBuilderPath) {
    $governanceFingerprintVisibilityCount = @([regex]::Matches((Get-Content $fingerprintBuilderPath -Raw -Encoding UTF8), 'OperationalGovernanceFingerprint|ComputeHash|BuildFingerprintParts')).Count
    $projectionFingerprintDeterminismCount = @([regex]::Matches((Get-Content $fingerprintBuilderPath -Raw -Encoding UTF8), 'NormalizedSignature|SignatureSegments|SHA256')).Count
}
if (Test-Path $fingerprintComparerPath) {
    $governanceDriftAnalysisCount = @([regex]::Matches((Get-Content $fingerprintComparerPath -Raw -Encoding UTF8), 'OperationalGovernanceFingerprintComparer|DivergentSegment|DriftDirection')).Count
}
if (Test-Path $driftAnalysisBuilderPath) {
    $governanceDriftAnalysisCount += @([regex]::Matches((Get-Content $driftAnalysisBuilderPath -Raw -Encoding UTF8), 'OperationalGovernanceDriftAnalysis|DriftSignals')).Count
}
if (Test-Path $fingerprintCollaboratorPath) {
    $replayConsistencyVisibilityCount = @([regex]::Matches((Get-Content $fingerprintCollaboratorPath -Raw -Encoding UTF8), 'GetReplayConsistency|ReplayConsistency|Operational replay consistency')).Count
    $governanceFingerprintVisibilityCount += @([regex]::Matches((Get-Content $fingerprintCollaboratorPath -Raw -Encoding UTF8), 'GetGovernanceFingerprint|governance fingerprint')).Count
}
if (Test-Path $fingerprintHistoryPath) {
    $governanceSignatureTransitionCount = @([regex]::Matches((Get-Content $fingerprintHistoryPath -Raw -Encoding UTF8), 'RecordBuild|FingerprintTransition|PreviousFingerprint')).Count
}

$runtimeBaselineVisibilityCount = 0
$governanceComplexityReductionCount = 0
$governanceExecutionBudgetCount = 0
$governanceProductionReadinessCount = 0
$governanceFreezeRecommendationCount = 0
$runtimeBaselineBuilderPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceRuntimeBaselineBuilder.cs'
$executionBudgetClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceExecutionBudgetClassifier.cs'
$productionReadinessClassifierPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceProductionReadinessClassifier.cs'
$projectionMemoizerPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalDiagnosticsProjections\OperationalGovernanceProjectionMemoizer.cs'
$ceilingMeasurementPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceCeilingMeasurement.cs'
if (Test-Path $runtimeBaselineBuilderPath) {
    $runtimeBaselineVisibilityCount = @([regex]::Matches((Get-Content $runtimeBaselineBuilderPath -Raw -Encoding UTF8), 'OperationalGovernanceRuntimeBaseline|TimingBand|BuildElapsedMilliseconds')).Count
}
if (Test-Path $executionBudgetClassifierPath) {
    $governanceExecutionBudgetCount = @([regex]::Matches((Get-Content $executionBudgetClassifierPath -Raw -Encoding UTF8), 'OperationalGovernanceExecutionBudget|ClassifyTimingBand|Classify\(')).Count
}
if (Test-Path $productionReadinessClassifierPath) {
    $governanceProductionReadinessCount = @([regex]::Matches((Get-Content $productionReadinessClassifierPath -Raw -Encoding UTF8), 'OperationalGovernanceProductionReadiness|ReadinessState|Classify\(')).Count
}
if (Test-Path $projectionMemoizerPath) {
    $governanceComplexityReductionCount = @([regex]::Matches((Get-Content $projectionMemoizerPath -Raw -Encoding UTF8), 'OperationalGovernanceProjectionMemoizer|Acquire|GetTelemetry')).Count
}
if (Test-Path $ceilingMeasurementPath) {
    $governanceFreezeRecommendationCount = @([regex]::Matches((Get-Content $ceilingMeasurementPath -Raw -Encoding UTF8), 'OperationalGovernanceCeilingMeasurement|IsWithinBudget|MaxCacheDiagnosticsGetEndpoints')).Count
}

$governanceFreezeEnforcementCount = 0
$governanceDeadSurfaceVisibilityCount = 0
$governanceDeterminismAuditCount = 0
$governanceRuntimeConsistencyCount = 0
$governanceOwnershipBoundaryCount = 0
$freezePolicyPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceFreezePolicy.cs'
$expansionGuardPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceExpansionGuard.cs'
$surfaceAuditPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceSurfaceAudit.cs'
$deadSurfaceDetectorPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceDeadSurfaceDetector.cs'
$determinismAuditPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceDeterminismAudit.cs'
$runtimeConsistencyGuardPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceRuntimeConsistencyGuard.cs'
$ownershipBoundariesPath = Join-Path $root 'Tannous.Pos.Application\Audit\Governance\OperationalGovernanceOwnershipBoundaries.cs'
if (Test-Path $freezePolicyPath) {
    $governanceFreezeEnforcementCount = @([regex]::Matches((Get-Content $freezePolicyPath -Raw -Encoding UTF8), 'OperationalGovernanceFreezePolicy|FrozenModuleCount|ApprovedExtensionPolicy')).Count
}
if (Test-Path $expansionGuardPath) {
    $governanceFreezeEnforcementCount += @([regex]::Matches((Get-Content $expansionGuardPath -Raw -Encoding UTF8), 'OperationalGovernanceExpansionGuard|IsFrozenCompliant|Violations')).Count
}
if (Test-Path $surfaceAuditPath) {
    $governanceFreezeEnforcementCount += @([regex]::Matches((Get-Content $surfaceAuditPath -Raw -Encoding UTF8), 'OperationalGovernanceSurfaceAudit|IsFreezeCompliant')).Count
}
if (Test-Path $deadSurfaceDetectorPath) {
    $governanceDeadSurfaceVisibilityCount = @([regex]::Matches((Get-Content $deadSurfaceDetectorPath -Raw -Encoding UTF8), 'OperationalGovernanceDeadSurfaceDetector|OrphanServiceMethod|Findings')).Count
}
if (Test-Path $determinismAuditPath) {
    $governanceDeterminismAuditCount = @([regex]::Matches((Get-Content $determinismAuditPath -Raw -Encoding UTF8), 'OperationalGovernanceDeterminismAudit|ExplainabilityOrdering|IsDeterministic')).Count
}
if (Test-Path $runtimeConsistencyGuardPath) {
    $governanceRuntimeConsistencyCount = @([regex]::Matches((Get-Content $runtimeConsistencyGuardPath -Raw -Encoding UTF8), 'OperationalGovernanceRuntimeConsistencyGuard|StaleSnapshotReuse|IsConsistent')).Count
}
if (Test-Path $ownershipBoundariesPath) {
    $governanceOwnershipBoundaryCount = @([regex]::Matches((Get-Content $ownershipBoundariesPath -Raw -Encoding UTF8), 'OperationalGovernanceOwnershipBoundaries|MaintenanceGuidance|OperationalExpectations')).Count
}

$operationalDashboardAggregationCount = 0
$operationalDashboardHealthVisibilityCount = 0
$operationalDashboardRecommendationCount = 0
$operationalDashboardReadModelCount = 0
$dashboardAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalDashboard\OperationalDashboardAggregation.cs'
$dashboardServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalDashboardService.cs'
$dashboardControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditDashboardController.cs'
$dashboardSummaryDtoPath = Join-Path $root 'Tannous.Pos.Application\OperationalDashboard\OperationalDashboardSummaryDto.cs'
if (Test-Path $dashboardAggregationPath) {
    $operationalDashboardAggregationCount = @([regex]::Matches((Get-Content $dashboardAggregationPath -Raw -Encoding UTF8), 'OperationalDashboardAggregation|ComposeHealth|ComposeRisk|ComposePressure')).Count
}
if (Test-Path $dashboardServicePath) {
    $operationalDashboardAggregationCount += @([regex]::Matches((Get-Content $dashboardServicePath -Raw -Encoding UTF8), 'OperationalDashboardService|GetSummaryAsync|Operational dashboard observability')).Count
}
if (Test-Path $dashboardControllerPath) {
    $operationalDashboardHealthVisibilityCount = @([regex]::Matches((Get-Content $dashboardControllerPath -Raw -Encoding UTF8), 'OperationalAuditDashboardController|OperationalDashboardSummaryDto|internal/operational-audit/dashboard')).Count
}
if (Test-Path $dashboardSummaryDtoPath) {
    $dashboardDtoPaths = @(
        (Join-Path $root 'Tannous.Pos.Application\OperationalDashboard\OperationalDashboardSummaryDto.cs'),
        (Join-Path $root 'Tannous.Pos.Application\OperationalDashboard\OperationalDashboardHealthDto.cs'),
        (Join-Path $root 'Tannous.Pos.Application\OperationalDashboard\OperationalDashboardRiskDto.cs'),
        (Join-Path $root 'Tannous.Pos.Application\OperationalDashboard\OperationalDashboardPressureDto.cs'),
        (Join-Path $root 'Tannous.Pos.Application\OperationalDashboard\OperationalDashboardActivityDto.cs')
    )
    foreach ($dtoPath in $dashboardDtoPaths) {
        if (Test-Path $dtoPath) {
            $operationalDashboardReadModelCount += @([regex]::Matches((Get-Content $dtoPath -Raw -Encoding UTF8), 'public sealed class OperationalDashboard')).Count
        }
    }
}
if (Test-Path $dashboardAggregationPath) {
    $operationalDashboardRecommendationCount = @([regex]::Matches((Get-Content $dashboardAggregationPath -Raw -Encoding UTF8), 'ComposeRecommendations|MaxRecommendations|Review reconciliation backlog|Investigate replay pressure')).Count
}

$operationalWorkbenchAggregationCount = 0
$operationalWorkbenchAttentionVisibilityCount = 0
$operationalWorkbenchReplayRiskVisibilityCount = 0
$operationalWorkbenchInventoryDriftCount = 0
$workbenchAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalWorkbench\OperationalReconciliationWorkbenchAggregation.cs'
$workbenchServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalReconciliationWorkbenchService.cs'
$workbenchControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditWorkbenchController.cs'
if (Test-Path $workbenchAggregationPath) {
    $operationalWorkbenchAggregationCount = @([regex]::Matches((Get-Content $workbenchAggregationPath -Raw -Encoding UTF8), 'OperationalReconciliationWorkbenchAggregation|ComposeQueue|ComposeHotspots|ComposeReplayRisk')).Count
}
if (Test-Path $workbenchServicePath) {
    $operationalWorkbenchAggregationCount += @([regex]::Matches((Get-Content $workbenchServicePath -Raw -Encoding UTF8), 'OperationalReconciliationWorkbenchService|GetReconciliationWorkbenchAsync|Operational workbench observability')).Count
}
if (Test-Path $workbenchControllerPath) {
    $operationalWorkbenchAttentionVisibilityCount = @([regex]::Matches((Get-Content $workbenchControllerPath -Raw -Encoding UTF8), 'OperationalAuditWorkbenchController|OperationalReconciliationWorkbenchDto|internal/operational-audit/workbench')).Count
}
if (Test-Path $workbenchAggregationPath) {
    $operationalWorkbenchAttentionVisibilityCount += @([regex]::Matches((Get-Content $workbenchAggregationPath -Raw -Encoding UTF8), 'ComposeAttentionItems|MaxAttentionItems|Review unresolved reconciliation backlog')).Count
    $operationalWorkbenchReplayRiskVisibilityCount = @([regex]::Matches((Get-Content $workbenchAggregationPath -Raw -Encoding UTF8), 'ComposeReplayRisk|InstabilityLevel|ProtectiveModeActive|ReplayEscalationObserved|StabilizationRecovering')).Count
    $operationalWorkbenchInventoryDriftCount = @([regex]::Matches((Get-Content $workbenchAggregationPath -Raw -Encoding UTF8), 'ComposeInventoryDrift|InventoryDrift|ManualReviewRecommended|ActiveInventoryMismatchCount')).Count
}

$operationalInventoryWorkbenchAggregationCount = 0
$operationalInventoryDriftVisibilityCount = 0
$operationalInventoryResolutionVisibilityCount = 0
$operationalInventoryHotspotCount = 0
$inventoryWorkbenchAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalInventoryWorkbench\OperationalInventoryWorkbenchAggregation.cs'
$inventoryWorkbenchServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalInventoryWorkbenchService.cs'
$inventoryWorkbenchControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditInventoryWorkbenchController.cs'
if (Test-Path $inventoryWorkbenchAggregationPath) {
    $operationalInventoryWorkbenchAggregationCount = @([regex]::Matches((Get-Content $inventoryWorkbenchAggregationPath -Raw -Encoding UTF8), 'OperationalInventoryWorkbenchAggregation|ComposeDriftSummary|ComposeHotspots|ComposeResolutionReadiness')).Count
}
if (Test-Path $inventoryWorkbenchServicePath) {
    $operationalInventoryWorkbenchAggregationCount += @([regex]::Matches((Get-Content $inventoryWorkbenchServicePath -Raw -Encoding UTF8), 'OperationalInventoryWorkbenchService|GetDriftWorkbenchAsync|Operational inventory workbench observability')).Count
}
if (Test-Path $inventoryWorkbenchControllerPath) {
    $operationalInventoryDriftVisibilityCount = @([regex]::Matches((Get-Content $inventoryWorkbenchControllerPath -Raw -Encoding UTF8), 'OperationalAuditInventoryWorkbenchController|OperationalInventoryWorkbenchDto|internal/operational-audit/inventory-workbench')).Count
}
if (Test-Path $inventoryWorkbenchAggregationPath) {
    $operationalInventoryDriftVisibilityCount += @([regex]::Matches((Get-Content $inventoryWorkbenchAggregationPath -Raw -Encoding UTF8), 'ComposeDriftSummary|TotalInventoryDriftConflicts|DriftSeverity|InventoryCountMismatch')).Count
    $operationalInventoryResolutionVisibilityCount = @([regex]::Matches((Get-Content $inventoryWorkbenchAggregationPath -Raw -Encoding UTF8), 'ComposeResolutionReadiness|ReadyForOperatorReview|BlockedByReplayPressure|ManualReconciliationRecommended')).Count
    $operationalInventoryHotspotCount = @([regex]::Matches((Get-Content $inventoryWorkbenchAggregationPath -Raw -Encoding UTF8), 'ComposeHotspots|MaxHotspots|ReplayLinkedInventoryConflicts|CascadingDegradationVisibility')).Count
}

$operationalReplayWorkbenchAggregationCount = 0
$operationalReplayPressureVisibilityCount = 0
$operationalReplayRecoveryConfidenceCount = 0
$operationalReplayHotspotCount = 0
$replayWorkbenchAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalReplayWorkbench\OperationalReplayWorkbenchAggregation.cs'
$replayWorkbenchServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalReplayWorkbenchService.cs'
$replayWorkbenchControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditReplayWorkbenchController.cs'
if (Test-Path $replayWorkbenchAggregationPath) {
    $operationalReplayWorkbenchAggregationCount = @([regex]::Matches((Get-Content $replayWorkbenchAggregationPath -Raw -Encoding UTF8), 'OperationalReplayWorkbenchAggregation|ComposePressureSummary|ComposeStabilization|ComposeRecoveryConfidence')).Count
}
if (Test-Path $replayWorkbenchServicePath) {
    $operationalReplayWorkbenchAggregationCount += @([regex]::Matches((Get-Content $replayWorkbenchServicePath -Raw -Encoding UTF8), 'OperationalReplayWorkbenchService|GetPressureWorkbenchAsync|Operational replay workbench observability')).Count
}
if (Test-Path $replayWorkbenchControllerPath) {
    $operationalReplayPressureVisibilityCount = @([regex]::Matches((Get-Content $replayWorkbenchControllerPath -Raw -Encoding UTF8), 'OperationalAuditReplayWorkbenchController|OperationalReplayWorkbenchDto|internal/operational-audit/replay-workbench')).Count
}
if (Test-Path $replayWorkbenchAggregationPath) {
    $operationalReplayPressureVisibilityCount += @([regex]::Matches((Get-Content $replayWorkbenchAggregationPath -Raw -Encoding UTF8), 'ComposePressureSummary|InstabilityLevel|ReplayEscalationVisible|ProtectiveModeVisible')).Count
    $operationalReplayRecoveryConfidenceCount = @([regex]::Matches((Get-Content $replayWorkbenchAggregationPath -Raw -Encoding UTF8), 'ComposeRecoveryConfidence|RecoveryConfidence|Recovering|Fragile|Uncertain')).Count
    $operationalReplayHotspotCount = @([regex]::Matches((Get-Content $replayWorkbenchAggregationPath -Raw -Encoding UTF8), 'ComposeHotspots|MaxHotspots|ReplayEscalation|CascadingReplayDegradation')).Count
}

$operationalCompositionHubCount = 0
$operationalCompositionReuseCount = 0
$operationalCompositionDepthReductionCount = 0
$compositionHubPath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalComposition\OperationalReadCompositionHub.cs'
$compositionInterfacePath = Join-Path $root 'Tannous.Pos.Application\OperationalComposition\IOperationalReadCompositionHub.cs'
$compositionContextPath = Join-Path $root 'Tannous.Pos.Application\OperationalComposition\OperationalReadCompositionContext.cs'
if (Test-Path $compositionHubPath) {
    $operationalCompositionHubCount = @([regex]::Matches((Get-Content $compositionHubPath -Raw -Encoding UTF8), 'OperationalReadCompositionHub|GetOrLoadAsync|GetDashboardSummaryAsync|GetReconciliationWorkbenchViewAsync|GetInventoryWorkbenchViewAsync')).Count
}
if (Test-Path $compositionInterfacePath) {
    $operationalCompositionHubCount += @([regex]::Matches((Get-Content $compositionInterfacePath -Raw -Encoding UTF8), 'IOperationalReadCompositionHub|BuildSnapshotAsync|OperationalReadCompositionContext')).Count
}
if (Test-Path $compositionContextPath) {
    $operationalCompositionReuseCount = @([regex]::Matches((Get-Content $compositionContextPath -Raw -Encoding UTF8), 'CompositionReuseHits|CompositionReuseMisses|CompositionReuseRatio|RecordReuseHit')).Count
}
$reconciliationWorkbenchServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalReconciliationWorkbenchService.cs'
$inventoryWorkbenchServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalInventoryWorkbenchService.cs'
$replayWorkbenchServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalReplayWorkbenchService.cs'
foreach ($servicePath in @($reconciliationWorkbenchServicePath, $inventoryWorkbenchServicePath, $replayWorkbenchServicePath)) {
    if (Test-Path $servicePath) {
        $text = Get-Content $servicePath -Raw -Encoding UTF8
        if ($text -match 'IOperationalReadCompositionHub' -and $text -notmatch 'IOperationalDashboardService|IOperationalReconciliationWorkbenchService|IOperationalInventoryWorkbenchService') {
            $operationalCompositionDepthReductionCount += 1
        }
    }
}
if (Test-Path $compositionHubPath) {
    $operationalCompositionDepthReductionCount += @([regex]::Matches((Get-Content $compositionHubPath -Raw -Encoding UTF8), 'RecordNestedReadAvoidance|GetReconciliationWorkbenchViewAsync|GetInventoryWorkbenchViewAsync')).Count
}

$operationalTrendAggregationCount = 0
$operationalTrendDeltaVisibilityCount = 0
$operationalTrendWindowCount = 0
$operationalTrendAttentionCount = 0
$trendAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalTrends\OperationalTrendAggregation.cs'
$trendServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalTrendService.cs'
$trendWindowStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalTrends\OperationalTrendWindowStore.cs'
$trendControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditTrendController.cs'
if (Test-Path $trendAggregationPath) {
    $operationalTrendAggregationCount = @([regex]::Matches((Get-Content $trendAggregationPath -Raw -Encoding UTF8), 'OperationalTrendAggregation|BuildSnapshot|CompareSnapshots|ComposeSummary|ComposeDeltas')).Count
    $operationalTrendAttentionCount = @([regex]::Matches((Get-Content $trendAggregationPath -Raw -Encoding UTF8), 'ComposeAttentionItems|MaxAttentionItems|OperationalTrendAttentionDto')).Count
}
if (Test-Path $trendServicePath) {
    $operationalTrendAggregationCount += @([regex]::Matches((Get-Content $trendServicePath -Raw -Encoding UTF8), 'OperationalTrendService|GetSummaryAsync|GetDeltasAsync|Operational trend observability')).Count
}
if (Test-Path $trendWindowStorePath) {
    $operationalTrendWindowCount = @([regex]::Matches((Get-Content $trendWindowStorePath -Raw -Encoding UTF8), 'OperationalTrendWindowStore|MaxWindowSnapshots|Queue|Dequeue|Append')).Count
}
if (Test-Path $trendControllerPath) {
    $operationalTrendDeltaVisibilityCount = @([regex]::Matches((Get-Content $trendControllerPath -Raw -Encoding UTF8), 'OperationalAuditTrendController|GetDeltas|GetSummary|internal/operational-audit/trends')).Count
}
if (Test-Path $trendAggregationPath) {
    $operationalTrendDeltaVisibilityCount += @([regex]::Matches((Get-Content $trendAggregationPath -Raw -Encoding UTF8), 'OperationalTrendDeltaDto|ComposeDeltas|MovementSignals|OverallDirection')).Count
}

$operationalNavigationAggregationCount = 0
$operationalNavigationRouteCount = 0
$operationalNavigationAttentionCount = 0
$operationalNavigationRecommendationCount = 0
$navigationAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalNavigation\OperationalNavigationAggregation.cs'
$navigationServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalNavigationService.cs'
$navigationControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditNavigationController.cs'
if (Test-Path $navigationAggregationPath) {
    $operationalNavigationAggregationCount = @([regex]::Matches((Get-Content $navigationAggregationPath -Raw -Encoding UTF8), 'OperationalNavigationAggregation|ComposeIndex|ComposeSections|ComposeRoutes')).Count
    $operationalNavigationRouteCount = @([regex]::Matches((Get-Content $navigationAggregationPath -Raw -Encoding UTF8), 'OperationalNavigationRouteDto|RelativeRoute|RouteDashboard|RouteReplayWorkbench')).Count
    $operationalNavigationAttentionCount = @([regex]::Matches((Get-Content $navigationAggregationPath -Raw -Encoding UTF8), 'ComposeAttentionItems|MaxAttentionItems|OperationalNavigationAttentionDto')).Count
    $operationalNavigationRecommendationCount = @([regex]::Matches((Get-Content $navigationAggregationPath -Raw -Encoding UTF8), 'ComposeRecommendations|MaxRecommendations|OperationalNavigationRecommendationDto')).Count
}
if (Test-Path $navigationServicePath) {
    $operationalNavigationAggregationCount += @([regex]::Matches((Get-Content $navigationServicePath -Raw -Encoding UTF8), 'OperationalNavigationService|GetNavigationIndexAsync|GetNavigationRoutesAsync|Operational navigation observability')).Count
}
if (Test-Path $navigationControllerPath) {
    $operationalNavigationRouteCount += @([regex]::Matches((Get-Content $navigationControllerPath -Raw -Encoding UTF8), 'OperationalAuditNavigationController|GetRoutes|internal/operational-audit/navigation')).Count
}

$operationalTimelineAggregationCount = 0
$operationalTimelineCorrelationCount = 0
$operationalTimelineRetentionCount = 0
$operationalTimelineAttentionCount = 0
$timelineAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalTimeline\OperationalTimelineAggregation.cs'
$timelineServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalTimelineService.cs'
$timelineWindowStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalTimeline\OperationalTimelineWindowStore.cs'
$timelineControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditTimelineController.cs'
if (Test-Path $timelineAggregationPath) {
    $operationalTimelineAggregationCount = @([regex]::Matches((Get-Content $timelineAggregationPath -Raw -Encoding UTF8), 'OperationalTimelineAggregation|DetectTransitionEvents|ComposeTimeline|BuildCaptureSnapshot')).Count
    $operationalTimelineCorrelationCount = @([regex]::Matches((Get-Content $timelineAggregationPath -Raw -Encoding UTF8), 'ComposeCorrelations|OperationalTimelineCorrelationDto|CorrelationReplayThenProtection')).Count
    $operationalTimelineAttentionCount = @([regex]::Matches((Get-Content $timelineAggregationPath -Raw -Encoding UTF8), 'ComposeAttentionItems|MaxAttentionItems|OperationalTimelineAttentionDto')).Count
}
if (Test-Path $timelineServicePath) {
    $operationalTimelineAggregationCount += @([regex]::Matches((Get-Content $timelineServicePath -Raw -Encoding UTF8), 'OperationalTimelineService|GetTimelineAsync|GetCorrelationsAsync|Operational timeline observability')).Count
}
if (Test-Path $timelineWindowStorePath) {
    $operationalTimelineRetentionCount = @([regex]::Matches((Get-Content $timelineWindowStorePath -Raw -Encoding UTF8), 'OperationalTimelineWindowStore|MaxTimelineEvents|Queue|Dequeue|Append')).Count
}
if (Test-Path $timelineControllerPath) {
    $operationalTimelineCorrelationCount += @([regex]::Matches((Get-Content $timelineControllerPath -Raw -Encoding UTF8), 'OperationalAuditTimelineController|GetCorrelations|internal/operational-audit/timeline')).Count
}

$operationalTriageAggregationCount = 0
$operationalTriagePriorityCount = 0
$operationalTriageCorrelationCount = 0
$operationalTriageRecommendationCount = 0
$triageAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalTriage\OperationalTriageAggregation.cs'
$triageServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalTriageService.cs'
$triageControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditTriageController.cs'
if (Test-Path $triageAggregationPath) {
    $operationalTriageAggregationCount = @([regex]::Matches((Get-Content $triageAggregationPath -Raw -Encoding UTF8), 'OperationalTriageAggregation|ComposeQueue|ComposeItems|ComposeRecommendations')).Count
    $operationalTriagePriorityCount = @([regex]::Matches((Get-Content $triageAggregationPath -Raw -Encoding UTF8), 'OperationalTriagePriority|PriorityBand|InvestigationRequired|MaxTriageItems')).Count
    $operationalTriageCorrelationCount = @([regex]::Matches((Get-Content $triageAggregationPath -Raw -Encoding UTF8), 'ComposeCorrelations|OperationalTriageCorrelationDto|CorrelationReplayTrend')).Count
    $operationalTriageRecommendationCount = @([regex]::Matches((Get-Content $triageAggregationPath -Raw -Encoding UTF8), 'OperationalTriageRecommendationDto|MaxRecommendations|SuggestedOperatorAction')).Count
}
if (Test-Path $triageServicePath) {
    $operationalTriageAggregationCount += @([regex]::Matches((Get-Content $triageServicePath -Raw -Encoding UTF8), 'OperationalTriageService|GetTriageQueueAsync|GetRecommendationsAsync|Operational triage observability')).Count
}
if (Test-Path $triageControllerPath) {
    $operationalTriageRecommendationCount += @([regex]::Matches((Get-Content $triageControllerPath -Raw -Encoding UTF8), 'OperationalAuditTriageController|GetRecommendations|internal/operational-audit/triage')).Count
}

$operationalRecoveryAggregationCount = 0
$operationalRecoveryConvergenceCount = 0
$operationalRecoveryOutlookCount = 0
$operationalRecoveryConfidenceCount = 0
$recoveryAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalRecovery\OperationalRecoveryAggregation.cs'
$recoveryServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalRecoveryService.cs'
$recoveryControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditRecoveryController.cs'
if (Test-Path $recoveryAggregationPath) {
    $operationalRecoveryAggregationCount = @([regex]::Matches((Get-Content $recoveryAggregationPath -Raw -Encoding UTF8), 'OperationalRecoveryAggregation|ComposePosture|ComposeOutlook|ComposeSignals')).Count
    $operationalRecoveryConvergenceCount = @([regex]::Matches((Get-Content $recoveryAggregationPath -Raw -Encoding UTF8), 'ComposeConvergence|OperationalRecoveryConvergenceDto|Converging|Diverging')).Count
    $operationalRecoveryOutlookCount = @([regex]::Matches((Get-Content $recoveryAggregationPath -Raw -Encoding UTF8), 'OperationalRecoveryOutlookSectionDto|ComposeReplayRecoverySection|SectionOperationalStability')).Count
    $operationalRecoveryConfidenceCount = @([regex]::Matches((Get-Content $recoveryAggregationPath -Raw -Encoding UTF8), 'OperationalRecoveryConfidence|ClassifyOverallConfidence|MapReplayConfidence|recovery confidence')).Count
}
if (Test-Path $recoveryServicePath) {
    $operationalRecoveryAggregationCount += @([regex]::Matches((Get-Content $recoveryServicePath -Raw -Encoding UTF8), 'OperationalRecoveryService|GetRecoveryPostureAsync|GetRecoveryOutlookAsync|Operational recovery observability')).Count
}
if (Test-Path $recoveryControllerPath) {
    $operationalRecoveryOutlookCount += @([regex]::Matches((Get-Content $recoveryControllerPath -Raw -Encoding UTF8), 'OperationalAuditRecoveryController|GetOutlook|internal/operational-audit/recovery')).Count
}

$operationalIncidentAggregationCount = 0
$operationalIncidentRecurrenceCount = 0
$operationalIncidentInvestigationCount = 0
$operationalIncidentRetentionCount = 0
$incidentAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalIncidents\OperationalIncidentAggregation.cs'
$incidentServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalIncidentService.cs'
$incidentStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalIncidents\OperationalIncidentCaseStore.cs'
$incidentControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditIncidentCasesController.cs'
if (Test-Path $incidentAggregationPath) {
    $operationalIncidentAggregationCount = @([regex]::Matches((Get-Content $incidentAggregationPath -Raw -Encoding UTF8), 'OperationalIncidentAggregation|ComposeCases|ComposeSummary|ComposeDetails')).Count
    $operationalIncidentRecurrenceCount = @([regex]::Matches((Get-Content $incidentAggregationPath -Raw -Encoding UTF8), 'DetectRecurrence|IsRecurring|OperationalIncidentCaseSnapshot')).Count
    $operationalIncidentInvestigationCount = @([regex]::Matches((Get-Content $incidentAggregationPath -Raw -Encoding UTF8), 'OperationalInvestigationContextDto|OperationalIncidentOutlookDto|RecommendedOperatorFocus')).Count
}
if (Test-Path $incidentServicePath) {
    $operationalIncidentAggregationCount += @([regex]::Matches((Get-Content $incidentServicePath -Raw -Encoding UTF8), 'OperationalIncidentService|GetIncidentCasesAsync|GetIncidentSummaryAsync|GetIncidentDetailsAsync|Operational incident observability')).Count
}
if (Test-Path $incidentStorePath) {
    $operationalIncidentRetentionCount = @([regex]::Matches((Get-Content $incidentStorePath -Raw -Encoding UTF8), 'OperationalIncidentCaseStore|MaxStoredSnapshots|Queue|Dequeue|Append')).Count
}
if (Test-Path $incidentControllerPath) {
    $operationalIncidentInvestigationCount += @([regex]::Matches((Get-Content $incidentControllerPath -Raw -Encoding UTF8), 'OperationalAuditIncidentCasesController|GetDetails|internal/operational-audit/incident-cases')).Count
}

$operationalCausalityAggregationCount = 0
$operationalCausalityPropagationCount = 0
$operationalCausalityBlockerCount = 0
$operationalCausalityContinuityCount = 0
$causalityAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalCausality\OperationalCausalityAggregation.cs'
$causalityServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalCausalityService.cs'
$causalityStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalCausality\OperationalCausalitySnapshotStore.cs'
$causalityControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditCausalityController.cs'
if (Test-Path $causalityAggregationPath) {
    $operationalCausalityAggregationCount = @([regex]::Matches((Get-Content $causalityAggregationPath -Raw -Encoding UTF8), 'OperationalCausalityAggregation|ComposeChains|ComposeSummary|ComposePropagationAnalysis')).Count
    $operationalCausalityPropagationCount = @([regex]::Matches((Get-Content $causalityAggregationPath -Raw -Encoding UTF8), 'OperationalPressurePropagationDto|ComposePropagations|IsEscalating|IsCollapsing')).Count
    $operationalCausalityBlockerCount = @([regex]::Matches((Get-Content $causalityAggregationPath -Raw -Encoding UTF8), 'OperationalStabilizationBlockerDto|ComposeStabilizationBlockers|PreventingRecovery')).Count
}
if (Test-Path $causalityServicePath) {
    $operationalCausalityAggregationCount += @([regex]::Matches((Get-Content $causalityServicePath -Raw -Encoding UTF8), 'OperationalCausalityService|GetCausalChainsAsync|GetCausalitySummaryAsync|GetPropagationAnalysisAsync|Operational causality observability')).Count
}
if (Test-Path $causalityStorePath) {
    $operationalCausalityContinuityCount = @([regex]::Matches((Get-Content $causalityStorePath -Raw -Encoding UTF8), 'OperationalCausalitySnapshotStore|BoundedFifoSnapshotStore|OperationalCognitionSnapshotLimits')).Count
}
if (Test-Path $causalityControllerPath) {
    $operationalCausalityPropagationCount += @([regex]::Matches((Get-Content $causalityControllerPath -Raw -Encoding UTF8), 'OperationalAuditCausalityController|GetPropagation|internal/operational-audit/causality')).Count
}

$operationalSituationRoomAggregationCount = 0
$operationalExecutiveBriefingCount = 0
$operationalNarrativeSynthesisCount = 0
$operationalAttentionCount = 0
$situationRoomAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalSituationRoom\OperationalSituationRoomAggregation.cs'
$situationRoomServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalSituationRoomService.cs'
$situationRoomStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalSituationRoom\OperationalSituationSnapshotStore.cs'
$situationRoomControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditSituationRoomController.cs'
if (Test-Path $situationRoomAggregationPath) {
    $operationalSituationRoomAggregationCount = @([regex]::Matches((Get-Content $situationRoomAggregationPath -Raw -Encoding UTF8), 'OperationalSituationRoomAggregation|ComposeSituationRoom|ComposeExecutiveBriefing|ComposeSituationSummary')).Count
    $operationalExecutiveBriefingCount = @([regex]::Matches((Get-Content $situationRoomAggregationPath -Raw -Encoding UTF8), 'OperationalExecutiveBriefingDto|ComposeExecutiveBriefing|ExecutiveSummary|Headline')).Count
    $operationalNarrativeSynthesisCount = @([regex]::Matches((Get-Content $situationRoomAggregationPath -Raw -Encoding UTF8), 'OperationalNarrativeDto|ComposeNarratives|OperatorInterpretation|NarrativeType')).Count
    $operationalAttentionCount = @([regex]::Matches((Get-Content $situationRoomAggregationPath -Raw -Encoding UTF8), 'OperationalAttentionLevel|ResolveAttentionLevel|OperatorAttentionRequired|AttentionLevel')).Count
}
if (Test-Path $situationRoomServicePath) {
    $operationalSituationRoomAggregationCount += @([regex]::Matches((Get-Content $situationRoomServicePath -Raw -Encoding UTF8), 'OperationalSituationRoomService|GetSituationRoomAsync|GetExecutiveBriefingAsync|GetSituationSummaryAsync|Operational situation room observability')).Count
}
if (Test-Path $situationRoomStorePath) {
    $operationalAttentionCount += @([regex]::Matches((Get-Content $situationRoomStorePath -Raw -Encoding UTF8), 'OperationalSituationSnapshotStore|BoundedFifoSnapshotStore|OperationalCognitionSnapshotLimits')).Count
}
if (Test-Path $situationRoomControllerPath) {
    $operationalExecutiveBriefingCount += @([regex]::Matches((Get-Content $situationRoomControllerPath -Raw -Encoding UTF8), 'OperationalAuditSituationRoomController|GetExecutiveBriefing|internal/operational-audit/situation-room')).Count
}

$operationalSimulationAggregationCount = 0
$operationalStabilizationScenarioCount = 0
$operationalDegradationScenarioCount = 0
$operationalLeverageInterpretationCount = 0
$simulationAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalSimulation\OperationalSimulationAggregation.cs'
$simulationServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalSimulationService.cs'
$simulationStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalSimulation\OperationalSimulationSnapshotStore.cs'
$simulationControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditSimulationController.cs'
if (Test-Path $simulationAggregationPath) {
    $operationalSimulationAggregationCount = @([regex]::Matches((Get-Content $simulationAggregationPath -Raw -Encoding UTF8), 'OperationalSimulationAggregation|ComposeScenarios|ComposeSummary|ComposeOutlook')).Count
    $operationalStabilizationScenarioCount = @([regex]::Matches((Get-Content $simulationAggregationPath -Raw -Encoding UTF8), 'OperationalStabilizationPathDto|ComposeStabilizationPaths|StabilizationScenario|RecoveryAcceleration')).Count
    $operationalDegradationScenarioCount = @([regex]::Matches((Get-Content $simulationAggregationPath -Raw -Encoding UTF8), 'OperationalDegradationPathDto|ComposeDegradationPaths|DegradationPath|EscalationRisk')).Count
    $operationalLeverageInterpretationCount = @([regex]::Matches((Get-Content $simulationAggregationPath -Raw -Encoding UTF8), 'OperationalLeveragePointDto|ComposeLeveragePoints|LeverageStrength|OperatorPriorityReason')).Count
}
if (Test-Path $simulationServicePath) {
    $operationalSimulationAggregationCount += @([regex]::Matches((Get-Content $simulationServicePath -Raw -Encoding UTF8), 'OperationalSimulationService|GetSimulationScenariosAsync|GetSimulationSummaryAsync|GetSimulationOutlookAsync|Operational simulation observability')).Count
}
if (Test-Path $simulationStorePath) {
    $operationalLeverageInterpretationCount += @([regex]::Matches((Get-Content $simulationStorePath -Raw -Encoding UTF8), 'OperationalSimulationSnapshotStore|BoundedFifoSnapshotStore|OperationalCognitionSnapshotLimits')).Count
}
if (Test-Path $simulationControllerPath) {
    $operationalStabilizationScenarioCount += @([regex]::Matches((Get-Content $simulationControllerPath -Raw -Encoding UTF8), 'OperationalAuditSimulationController|GetSimulationScenarios|internal/operational-audit/simulation')).Count
}

$operationalPlaybookAggregationCount = 0
$operationalStabilizationGuidanceCount = 0
$operationalEscalationGuidanceCount = 0
$operationalResponseSequencingCount = 0
$playbookAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalPlaybooks\OperationalPlaybookAggregation.cs'
$playbookServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalPlaybookService.cs'
$playbookStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalPlaybooks\OperationalPlaybookSnapshotStore.cs'
$playbookControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditPlaybookController.cs'
if (Test-Path $playbookAggregationPath) {
    $operationalPlaybookAggregationCount = @([regex]::Matches((Get-Content $playbookAggregationPath -Raw -Encoding UTF8), 'OperationalPlaybookAggregation|ComposePlaybooks|ComposeSummary|ComposeStabilizationGuidance')).Count
    $operationalStabilizationGuidanceCount = @([regex]::Matches((Get-Content $playbookAggregationPath -Raw -Encoding UTF8), 'OperationalStabilizationGuidanceDto|ComposeStabilizationGuidance|RecommendedRecoveryOrder|StabilizationLikelihood')).Count
    $operationalEscalationGuidanceCount = @([regex]::Matches((Get-Content $playbookAggregationPath -Raw -Encoding UTF8), 'OperationalEscalationGuidanceDto|ComposeEscalationGuidance|EscalationType|ContainmentPriority')).Count
    $operationalResponseSequencingCount = @([regex]::Matches((Get-Content $playbookAggregationPath -Raw -Encoding UTF8), 'OperationalResponseStepDto|ComposeResponseSteps|SequenceOrder|RecommendedSequence|OperatorInstruction')).Count
}
if (Test-Path $playbookServicePath) {
    $operationalPlaybookAggregationCount += @([regex]::Matches((Get-Content $playbookServicePath -Raw -Encoding UTF8), 'OperationalPlaybookService|GetOperationalPlaybooksAsync|GetPlaybookSummaryAsync|GetStabilizationGuidanceAsync|Operational playbook observability')).Count
}
if (Test-Path $playbookStorePath) {
    $operationalResponseSequencingCount += @([regex]::Matches((Get-Content $playbookStorePath -Raw -Encoding UTF8), 'OperationalPlaybookSnapshotStore|BoundedFifoSnapshotStore|OperationalCognitionSnapshotLimits')).Count
}
if (Test-Path $playbookControllerPath) {
    $operationalStabilizationGuidanceCount += @([regex]::Matches((Get-Content $playbookControllerPath -Raw -Encoding UTF8), 'OperationalAuditPlaybookController|GetStabilizationGuidance|internal/operational-audit/playbooks')).Count
}

$operationalPatternAggregationCount = 0
$operationalArchetypeRecognitionCount = 0
$operationalStabilizationPatternCount = 0
$operationalEscalationPatternCount = 0
$patternAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalPatterns\OperationalPatternAggregation.cs'
$patternServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalPatternService.cs'
$patternStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalPatterns\OperationalPatternSnapshotStore.cs'
$patternControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditPatternController.cs'
if (Test-Path $patternAggregationPath) {
    $operationalPatternAggregationCount = @([regex]::Matches((Get-Content $patternAggregationPath -Raw -Encoding UTF8), 'OperationalPatternAggregation|ComposePatterns|ComposeSummary|ComposeArchetypes')).Count
    $operationalArchetypeRecognitionCount = @([regex]::Matches((Get-Content $patternAggregationPath -Raw -Encoding UTF8), 'OperationalStabilizationArchetypeDto|ComposeArchetypes|ArchetypeType|OperatorInterpretation')).Count
    $operationalStabilizationPatternCount = @([regex]::Matches((Get-Content $patternAggregationPath -Raw -Encoding UTF8), 'OperationalPatternSequenceDto|ComposeSequences|StabilizationArchetype|StabilizationPattern')).Count
    $operationalEscalationPatternCount = @([regex]::Matches((Get-Content $patternAggregationPath -Raw -Encoding UTF8), 'EscalationCycle|EscalationFlow|EscalationPattern|EscalationBehavior')).Count
}
if (Test-Path $patternServicePath) {
    $operationalPatternAggregationCount += @([regex]::Matches((Get-Content $patternServicePath -Raw -Encoding UTF8), 'OperationalPatternService|GetOperationalPatternsAsync|GetPatternSummaryAsync|GetStabilizationArchetypesAsync|Operational pattern observability')).Count
}
if (Test-Path $patternStorePath) {
    $operationalArchetypeRecognitionCount += @([regex]::Matches((Get-Content $patternStorePath -Raw -Encoding UTF8), 'OperationalPatternSnapshotStore|BoundedFifoSnapshotStore|OperationalCognitionSnapshotLimits')).Count
}
if (Test-Path $patternControllerPath) {
    $operationalStabilizationPatternCount += @([regex]::Matches((Get-Content $patternControllerPath -Raw -Encoding UTF8), 'OperationalAuditPatternController|GetArchetypes|internal/operational-audit/patterns')).Count
}

$operationalIntegrityAggregationCount = 0
$operationalContradictionDetectionCount = 0
$operationalNarrativeAlignmentCount = 0
$operationalCoherenceCount = 0
$integrityAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalIntegrity\OperationalIntegrityAggregation.cs'
$integrityServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalIntegrityService.cs'
$integrityStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalIntegrity\OperationalIntegritySnapshotStore.cs'
$integrityControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditIntegrityController.cs'
if (Test-Path $integrityAggregationPath) {
    $operationalIntegrityAggregationCount = @([regex]::Matches((Get-Content $integrityAggregationPath -Raw -Encoding UTF8), 'OperationalIntegrityAggregation|ComposeIntegrityReport|ComposeSummary|ComposeContradictions')).Count
    $operationalContradictionDetectionCount = @([regex]::Matches((Get-Content $integrityAggregationPath -Raw -Encoding UTF8), 'OperationalContradictionDto|ComposeContradictions|ContradictionType|RecommendedOperatorReview')).Count
    $operationalNarrativeAlignmentCount = @([regex]::Matches((Get-Content $integrityAggregationPath -Raw -Encoding UTF8), 'OperationalNarrativeConsistencyDto|OperationalInterpretationAlignmentDto|NarrativeAgreement|DominantNarrative')).Count
    $operationalCoherenceCount = @([regex]::Matches((Get-Content $integrityAggregationPath -Raw -Encoding UTF8), 'OperationalIntegrityState|ConsistencyScore|AlignmentState|OperationalIntegrityWarningDto')).Count
}
if (Test-Path $integrityServicePath) {
    $operationalIntegrityAggregationCount += @([regex]::Matches((Get-Content $integrityServicePath -Raw -Encoding UTF8), 'OperationalIntegrityService|GetIntegrityReportAsync|GetIntegritySummaryAsync|GetContradictionsAsync|Operational integrity observability')).Count
}
if (Test-Path $integrityStorePath) {
    $operationalCoherenceCount += @([regex]::Matches((Get-Content $integrityStorePath -Raw -Encoding UTF8), 'OperationalIntegritySnapshotStore|BoundedFifoSnapshotStore|OperationalCognitionSnapshotLimits')).Count
}
if (Test-Path $integrityControllerPath) {
    $operationalNarrativeAlignmentCount += @([regex]::Matches((Get-Content $integrityControllerPath -Raw -Encoding UTF8), 'OperationalAuditIntegrityController|GetContradictions|internal/operational-audit/integrity')).Count
}

$operationalExperienceGraphAggregationCount = 0
$operationalTraversalGenerationCount = 0
$operationalContextualNavigationCount = 0
$operationalRelationshipCount = 0
$experienceGraphAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalExperienceGraph\OperationalExperienceGraphAggregation.cs'
$experienceGraphServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalExperienceGraphService.cs'
$experienceGraphStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalExperienceGraph\OperationalExperienceSnapshotStore.cs'
$experienceGraphControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditExperienceGraphController.cs'
if (Test-Path $experienceGraphAggregationPath) {
    $operationalExperienceGraphAggregationCount = @([regex]::Matches((Get-Content $experienceGraphAggregationPath -Raw -Encoding UTF8), 'OperationalExperienceGraphAggregation|ComposeExperienceGraph|ComposeTraversalPaths|ComposeContextualNavigation')).Count
    $operationalTraversalGenerationCount = @([regex]::Matches((Get-Content $experienceGraphAggregationPath -Raw -Encoding UTF8), 'OperationalTraversalPathDto|ComposeTraversalPaths|TraversalPriority|RecommendedSequence')).Count
    $operationalContextualNavigationCount = @([regex]::Matches((Get-Content $experienceGraphAggregationPath -Raw -Encoding UTF8), 'OperationalContextualNavigationDto|RecommendedNextSurface|CurrentOperationalFocus|OperatorInterpretation')).Count
    $operationalRelationshipCount = @([regex]::Matches((Get-Content $experienceGraphAggregationPath -Raw -Encoding UTF8), 'OperationalRelationshipDto|ComposeRelationships|RelationshipType|TraversalReason')).Count
}
if (Test-Path $experienceGraphServicePath) {
    $operationalExperienceGraphAggregationCount += @([regex]::Matches((Get-Content $experienceGraphServicePath -Raw -Encoding UTF8), 'OperationalExperienceGraphService|GetExperienceGraphAsync|GetTraversalPathsAsync|GetContextualNavigationAsync|Operational experience graph observability')).Count
}
if (Test-Path $experienceGraphStorePath) {
    $operationalRelationshipCount += @([regex]::Matches((Get-Content $experienceGraphStorePath -Raw -Encoding UTF8), 'OperationalExperienceSnapshotStore|BoundedFifoSnapshotStore|OperationalCognitionSnapshotLimits')).Count
}
if (Test-Path $experienceGraphControllerPath) {
    $operationalContextualNavigationCount += @([regex]::Matches((Get-Content $experienceGraphControllerPath -Raw -Encoding UTF8), 'OperationalAuditExperienceGraphController|GetContextualNavigation|internal/operational-audit/experience-graph')).Count
}

$operationalDigestAggregationCount = 0
$operationalHighlightCount = 0
$operationalExecutiveDigestCount = 0
$operationalNavigationHighlightCount = 0
$digestAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalDigest\OperationalDigestAggregation.cs'
$digestServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalDigestService.cs'
$digestStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalDigest\OperationalDigestSnapshotStore.cs'
$digestControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditDigestController.cs'
if (Test-Path $digestAggregationPath) {
    $operationalDigestAggregationCount = @([regex]::Matches((Get-Content $digestAggregationPath -Raw -Encoding UTF8), 'OperationalDigestAggregation|ComposeOperationalDigest|ComposeExecutiveDigest|ComposeDigestSummary')).Count
    $operationalHighlightCount = @([regex]::Matches((Get-Content $digestAggregationPath -Raw -Encoding UTF8), 'OperationalHighlightDto|ComposeHighlights|HighlightType|OperatorInterpretation')).Count
    $operationalExecutiveDigestCount = @([regex]::Matches((Get-Content $digestAggregationPath -Raw -Encoding UTF8), 'OperationalExecutiveDigestDto|ExecutivePriorities|LeadershipAttentionRequired|Headline')).Count
    $operationalNavigationHighlightCount = @([regex]::Matches((Get-Content $digestAggregationPath -Raw -Encoding UTF8), 'OperationalNavigationHighlightDto|ComposeNavigationHighlights|RecommendedSurface|NavigationReason')).Count
}
if (Test-Path $digestServicePath) {
    $operationalDigestAggregationCount += @([regex]::Matches((Get-Content $digestServicePath -Raw -Encoding UTF8), 'OperationalDigestService|GetOperationalDigestAsync|GetExecutiveDigestAsync|GetDigestSummaryAsync|Operational digest observability')).Count
}
if (Test-Path $digestStorePath) {
    $operationalHighlightCount += @([regex]::Matches((Get-Content $digestStorePath -Raw -Encoding UTF8), 'OperationalDigestSnapshotStore|BoundedFifoSnapshotStore|OperationalCognitionSnapshotLimits')).Count
}
if (Test-Path $digestControllerPath) {
    $operationalExecutiveDigestCount += @([regex]::Matches((Get-Content $digestControllerPath -Raw -Encoding UTF8), 'OperationalAuditDigestController|GetExecutiveDigest|internal/operational-audit/digest')).Count
}

$operationalEvolutionAggregationCount = 0
$operationalTransitionCount = 0
$operationalMomentumInterpretationCount = 0
$operationalEvolutionContinuityCount = 0
$evolutionAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalEvolution\OperationalEvolutionAggregation.cs'
$evolutionServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalEvolutionService.cs'
$evolutionStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalEvolution\OperationalEvolutionSnapshotStore.cs'
$evolutionControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditEvolutionController.cs'
if (Test-Path $evolutionAggregationPath) {
    $operationalEvolutionAggregationCount = @([regex]::Matches((Get-Content $evolutionAggregationPath -Raw -Encoding UTF8), 'OperationalEvolutionAggregation|ComposeEvolutionTimeline|ComposeEvolutionSummary|ComposeMomentumAnalysis')).Count
    $operationalTransitionCount = @([regex]::Matches((Get-Content $evolutionAggregationPath -Raw -Encoding UTF8), 'OperationalTransitionDto|ComposeTransitions|TransitionType|OperatorInterpretation')).Count
    $operationalMomentumInterpretationCount = @([regex]::Matches((Get-Content $evolutionAggregationPath -Raw -Encoding UTF8), 'OperationalMomentumAnalysisDto|RecoveryMomentum|EscalationMomentum|StabilizationMomentum')).Count
    $operationalEvolutionContinuityCount = @([regex]::Matches((Get-Content $evolutionAggregationPath -Raw -Encoding UTF8), 'OperationalEvolutionContinuityDto|DominantNarrativeTransition|RepeatingOperationalFlow|StabilizationConsistency')).Count
}
if (Test-Path $evolutionServicePath) {
    $operationalEvolutionAggregationCount += @([regex]::Matches((Get-Content $evolutionServicePath -Raw -Encoding UTF8), 'OperationalEvolutionService|GetEvolutionTimelineAsync|GetEvolutionSummaryAsync|GetMomentumAnalysisAsync|Operational evolution observability')).Count
}
if (Test-Path $evolutionStorePath) {
    $operationalEvolutionContinuityCount += @([regex]::Matches((Get-Content $evolutionStorePath -Raw -Encoding UTF8), 'OperationalEvolutionSnapshotStore|BoundedFifoSnapshotStore|OperationalCognitionSnapshotLimits')).Count
}
if (Test-Path $evolutionControllerPath) {
    $operationalMomentumInterpretationCount += @([regex]::Matches((Get-Content $evolutionControllerPath -Raw -Encoding UTF8), 'OperationalAuditEvolutionController|GetMomentumAnalysis|internal/operational-audit/evolution')).Count
}

$operationalTopologyAggregationCount = 0
$operationalDependencyChainCount = 0
$operationalInfluenceInterpretationCount = 0
$operationalTopologyContinuityCount = 0
$topologyAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalTopology\OperationalTopologyAggregation.cs'
$topologyServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalTopologyService.cs'
$topologyStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalTopology\OperationalTopologySnapshotStore.cs'
$topologyControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditTopologyController.cs'
if (Test-Path $topologyAggregationPath) {
    $operationalTopologyAggregationCount = @([regex]::Matches((Get-Content $topologyAggregationPath -Raw -Encoding UTF8), 'OperationalTopologyAggregation|ComposeOperationalTopology|ComposeTopologySummary|ComposeDependencyChains')).Count
    $operationalDependencyChainCount = @([regex]::Matches((Get-Content $topologyAggregationPath -Raw -Encoding UTF8), 'OperationalDependencyChainDto|ComposeDependencyChains|DependencySequence|EscalationRisk')).Count
    $operationalInfluenceInterpretationCount = @([regex]::Matches((Get-Content $topologyAggregationPath -Raw -Encoding UTF8), 'OperationalInfluenceDto|ComposeInfluences|UpstreamInfluenceStrength|DownstreamInfluenceStrength')).Count
    $operationalTopologyContinuityCount = @([regex]::Matches((Get-Content $topologyAggregationPath -Raw -Encoding UTF8), 'OperationalTopologyContinuityDto|DominantTopologyShift|DependencyStability|EscalationTopologyConsistency')).Count
}
if (Test-Path $topologyServicePath) {
    $operationalTopologyAggregationCount += @([regex]::Matches((Get-Content $topologyServicePath -Raw -Encoding UTF8), 'OperationalTopologyService|GetOperationalTopologyAsync|GetTopologySummaryAsync|GetDependencyChainsAsync|Operational topology observability')).Count
}
if (Test-Path $topologyStorePath) {
    $operationalTopologyContinuityCount += @([regex]::Matches((Get-Content $topologyStorePath -Raw -Encoding UTF8), 'OperationalTopologySnapshotStore|BoundedFifoSnapshotStore|OperationalCognitionSnapshotLimits')).Count
}
if (Test-Path $topologyControllerPath) {
    $operationalDependencyChainCount += @([regex]::Matches((Get-Content $topologyControllerPath -Raw -Encoding UTF8), 'OperationalAuditTopologyController|GetDependencyChains|internal/operational-audit/topology')).Count
}

$operationalConvergenceAggregationCount = 0
$operationalReinforcementInterpretationCount = 0
$operationalAmbiguityAnalysisCount = 0
$operationalDivergenceInterpretationCount = 0
$convergenceAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalConvergence\OperationalConvergenceAggregation.cs'
$convergenceServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalConvergenceService.cs'
$convergenceStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalConvergence\OperationalConvergenceSnapshotStore.cs'
$convergenceControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditConvergenceController.cs'
if (Test-Path $convergenceAggregationPath) {
    $operationalConvergenceAggregationCount = @([regex]::Matches((Get-Content $convergenceAggregationPath -Raw -Encoding UTF8), 'OperationalConvergenceAggregation|ComposeConvergenceReport|ComposeConvergenceSummary|ComposeDivergences')).Count
    $operationalReinforcementInterpretationCount = @([regex]::Matches((Get-Content $convergenceAggregationPath -Raw -Encoding UTF8), 'OperationalSignalReinforcementDto|ComposeReinforcements|ReinforcingLayers|ReinforcementStrength')).Count
    $operationalAmbiguityAnalysisCount = @([regex]::Matches((Get-Content $convergenceAggregationPath -Raw -Encoding UTF8), 'OperationalAmbiguityAnalysisDto|ComposeAmbiguities|AmbiguitySource|SignalAgreementLevel')).Count
    $operationalDivergenceInterpretationCount = @([regex]::Matches((Get-Content $convergenceAggregationPath -Raw -Encoding UTF8), 'OperationalDivergenceDto|ComposeDivergences|DivergenceType|ConflictingLayers')).Count
}
if (Test-Path $convergenceServicePath) {
    $operationalConvergenceAggregationCount += @([regex]::Matches((Get-Content $convergenceServicePath -Raw -Encoding UTF8), 'OperationalConvergenceService|GetConvergenceReportAsync|GetConvergenceSummaryAsync|GetOperationalDivergenceAsync|Operational convergence observability')).Count
}
if (Test-Path $convergenceStorePath) {
    $operationalAmbiguityAnalysisCount += @([regex]::Matches((Get-Content $convergenceStorePath -Raw -Encoding UTF8), 'OperationalConvergenceSnapshotStore|BoundedFifoSnapshotStore|OperationalCognitionSnapshotLimits')).Count
}
if (Test-Path $convergenceControllerPath) {
    $operationalDivergenceInterpretationCount += @([regex]::Matches((Get-Content $convergenceControllerPath -Raw -Encoding UTF8), 'OperationalAuditConvergenceController|GetOperationalDivergence|internal/operational-audit/convergence')).Count
}

$operationalResilienceCognitionAggregationCount = 0
$operationalSurvivabilityInterpretationCount = 0
$operationalFragilityAnalysisCount = 0
$operationalContainmentDurabilityCount = 0
$resilienceCognitionAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalResilience\OperationalResilienceAggregation.cs'
$resilienceCognitionServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalResilienceCognitionService.cs'
$resilienceCognitionStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalResilience\OperationalResilienceCognitionSnapshotStore.cs'
$resilienceControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditResilienceController.cs'
if (Test-Path $resilienceCognitionAggregationPath) {
    $operationalResilienceCognitionAggregationCount = @([regex]::Matches((Get-Content $resilienceCognitionAggregationPath -Raw -Encoding UTF8), 'OperationalResilienceAggregation|ComposeResilienceReport|ComposeResilienceSummary|ComposeFragilities')).Count
    $operationalSurvivabilityInterpretationCount = @([regex]::Matches((Get-Content $resilienceCognitionAggregationPath -Raw -Encoding UTF8), 'OperationalSurvivabilityAnalysisDto|ComposeSurvivabilityAnalyses|SurvivabilityStrength|StabilizationResistance')).Count
    $operationalFragilityAnalysisCount = @([regex]::Matches((Get-Content $resilienceCognitionAggregationPath -Raw -Encoding UTF8), 'OperationalFragilityDto|ComposeFragilities|FragilityType|CollapseSensitivity')).Count
    $operationalContainmentDurabilityCount = @([regex]::Matches((Get-Content $resilienceCognitionAggregationPath -Raw -Encoding UTF8), 'OperationalContainmentDurabilityDto|ComposeContainmentDurabilities|DurabilityStrength|EscalationContainmentStrength')).Count
}
if (Test-Path $resilienceCognitionServicePath) {
    $operationalResilienceCognitionAggregationCount += @([regex]::Matches((Get-Content $resilienceCognitionServicePath -Raw -Encoding UTF8), 'OperationalResilienceCognitionService|GetResilienceReportAsync|GetResilienceSummaryAsync|GetOperationalFragilityAsync|Operational resilience observability')).Count
}
if (Test-Path $resilienceCognitionStorePath) {
    $operationalContainmentDurabilityCount += @([regex]::Matches((Get-Content $resilienceCognitionStorePath -Raw -Encoding UTF8), 'OperationalResilienceCognitionSnapshotStore|BoundedFifoSnapshotStore|OperationalCognitionSnapshotLimits')).Count
}
if (Test-Path $resilienceControllerPath) {
    $operationalFragilityAnalysisCount += @([regex]::Matches((Get-Content $resilienceControllerPath -Raw -Encoding UTF8), 'GetOperationalFragility|GetResilienceReport|posture/summary|internal/operational-audit/resilience')).Count
}

$operationalAttentionAggregationCount = 0
$operationalPriorityInterpretationCount = 0
$operationalAttentionCoordinationCount = 0
$operationalEmphasisCount = 0
$attentionAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalAttention\OperationalAttentionAggregation.cs'
$attentionServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalAttentionService.cs'
$attentionStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalAttention\OperationalAttentionSnapshotStore.cs'
$attentionControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditAttentionController.cs'
if (Test-Path $attentionAggregationPath) {
    $operationalAttentionAggregationCount = @([regex]::Matches((Get-Content $attentionAggregationPath -Raw -Encoding UTF8), 'OperationalAttentionAggregation|ComposeAttentionReport|ComposeAttentionSummary|ComposePriorities')).Count
    $operationalPriorityInterpretationCount = @([regex]::Matches((Get-Content $attentionAggregationPath -Raw -Encoding UTF8), 'OperationalPriorityDto|ComposePriorities|PriorityType|OperationalUrgency')).Count
    $operationalAttentionCoordinationCount = @([regex]::Matches((Get-Content $attentionAggregationPath -Raw -Encoding UTF8), 'OperationalAttentionCoordinationDto|ComposeAttentionCoordination|AttentionRouting|EscalationWeight')).Count
    $operationalEmphasisCount = @([regex]::Matches((Get-Content $attentionAggregationPath -Raw -Encoding UTF8), 'OperationalEmphasisDto|ComposeOperationalEmphasis|EmphasisStrength|ReinforcingSignals')).Count
}
if (Test-Path $attentionServicePath) {
    $operationalAttentionAggregationCount += @([regex]::Matches((Get-Content $attentionServicePath -Raw -Encoding UTF8), 'OperationalAttentionService|GetAttentionReportAsync|GetAttentionSummaryAsync|GetOperationalPrioritiesAsync|Operational attention observability')).Count
}
if (Test-Path $attentionStorePath) {
    $operationalAttentionCoordinationCount += @([regex]::Matches((Get-Content $attentionStorePath -Raw -Encoding UTF8), 'OperationalAttentionSnapshotStore|BoundedFifoSnapshotStore|OperationalCognitionSnapshotLimits')).Count
}
if (Test-Path $attentionControllerPath) {
    $operationalPriorityInterpretationCount += @([regex]::Matches((Get-Content $attentionControllerPath -Raw -Encoding UTF8), 'GetOperationalPriorities|GetAttentionReport|internal/operational-audit/attention')).Count
}

$strategyAggregationCount = 0
$strategicAlignmentCount = 0
$coordinationInterpretationCount = 0
$strategyContinuityCount = 0
$strategyAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalStrategy\OperationalStrategyAggregation.cs'
$strategyServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalStrategyService.cs'
$strategyStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalStrategy\OperationalStrategySnapshotStore.cs'
$strategyControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditStrategyController.cs'
if (Test-Path $strategyAggregationPath) {
    $strategyAggregationCount = @([regex]::Matches((Get-Content $strategyAggregationPath -Raw -Encoding UTF8), 'OperationalStrategyAggregation|ComposeStrategyReport|ComposeStrategySummary|ComposeOperationalCoordination')).Count
    $strategicAlignmentCount = @([regex]::Matches((Get-Content $strategyAggregationPath -Raw -Encoding UTF8), 'OperationalStrategicAlignmentDto|ComposeStrategicAlignments|AlignmentStrength|StrategicConsistency')).Count
    $coordinationInterpretationCount = @([regex]::Matches((Get-Content $strategyAggregationPath -Raw -Encoding UTF8), 'OperationalCoordinationDto|ComposeOperationalCoordination|CoordinationStrength|StabilizationCoordination')).Count
    $strategyContinuityCount = @([regex]::Matches((Get-Content $strategyAggregationPath -Raw -Encoding UTF8), 'OperationalStrategyContinuityDto|ComposeStrategyContinuity|DominantStrategicShift|CoordinationConsistency')).Count
}
if (Test-Path $strategyServicePath) {
    $strategyAggregationCount += @([regex]::Matches((Get-Content $strategyServicePath -Raw -Encoding UTF8), 'OperationalStrategyService|GetStrategyReportAsync|GetStrategySummaryAsync|GetOperationalCoordinationAsync|Operational strategy observability')).Count
}
if (Test-Path $strategyStorePath) {
    $strategyContinuityCount += @([regex]::Matches((Get-Content $strategyStorePath -Raw -Encoding UTF8), 'OperationalStrategySnapshotStore|BoundedFifoSnapshotStore|OperationalCognitionSnapshotLimits')).Count
}
if (Test-Path $strategyControllerPath) {
    $coordinationInterpretationCount += @([regex]::Matches((Get-Content $strategyControllerPath -Raw -Encoding UTF8), 'GetOperationalCoordination|GetStrategyReport|internal/operational-audit/strategy')).Count
}

$equilibriumAggregationCount = 0
$imbalanceInterpretationCount = 0
$pressureDistributionCount = 0
$equilibriumContinuityCount = 0
$equilibriumAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalEquilibrium\OperationalEquilibriumAggregation.cs'
$equilibriumServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalEquilibriumService.cs'
$equilibriumStorePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalEquilibrium\OperationalEquilibriumSnapshotStore.cs'
$equilibriumControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditEquilibriumController.cs'
if (Test-Path $equilibriumAggregationPath) {
    $equilibriumAggregationCount = @([regex]::Matches((Get-Content $equilibriumAggregationPath -Raw -Encoding UTF8), 'OperationalEquilibriumAggregation|ComposeEquilibriumReport|ComposeEquilibriumSummary|ComposeImbalances')).Count
    $imbalanceInterpretationCount = @([regex]::Matches((Get-Content $equilibriumAggregationPath -Raw -Encoding UTF8), 'OperationalImbalanceDto|ComposeImbalances|ImbalanceType|StrainConcentration')).Count
    $pressureDistributionCount = @([regex]::Matches((Get-Content $equilibriumAggregationPath -Raw -Encoding UTF8), 'OperationalPressureDistributionDto|ComposePressureDistributions|PressureWeight|EscalationDistribution')).Count
    $equilibriumContinuityCount = @([regex]::Matches((Get-Content $equilibriumAggregationPath -Raw -Encoding UTF8), 'OperationalEquilibriumContinuityDto|ComposeEquilibriumContinuity|DominantEquilibriumShift|StabilizationBalanceConsistency')).Count
}
if (Test-Path $equilibriumServicePath) {
    $equilibriumAggregationCount += @([regex]::Matches((Get-Content $equilibriumServicePath -Raw -Encoding UTF8), 'OperationalEquilibriumService|GetEquilibriumReportAsync|GetEquilibriumSummaryAsync|GetOperationalImbalancesAsync|Operational equilibrium observability')).Count
}
if (Test-Path $equilibriumStorePath) {
    $equilibriumContinuityCount += @([regex]::Matches((Get-Content $equilibriumStorePath -Raw -Encoding UTF8), 'OperationalEquilibriumSnapshotStore|BoundedFifoSnapshotStore|OperationalCognitionSnapshotLimits')).Count
}
if (Test-Path $equilibriumControllerPath) {
    $imbalanceInterpretationCount += @([regex]::Matches((Get-Content $equilibriumControllerPath -Raw -Encoding UTF8), 'GetOperationalImbalances|GetEquilibriumReport|internal/operational-audit/equilibrium')).Count
}

# Step 46 consolidation adoption metrics
$cognitionBoundedFifoAdoptionCount = 0
$continuityPhrasingUsageCount = 0
$boundedCollectionUsageCount = 0

$cognitionStoreDir = Join-Path $root 'Tannous.Pos.Infrastructure\Services'
if (Test-Path $cognitionStoreDir) {
    $cognitionStoreFiles = Get-ChildItem -Path $cognitionStoreDir -Recurse -Filter '*SnapshotStore.cs' |
        Where-Object { $_.Name -notlike '*GovernanceSnapshotStore*' -and $_.Name -notlike 'BoundedFifoSnapshotStore*' }
    foreach ($f in $cognitionStoreFiles) {
        $t = Get-Content $f.FullName -Raw -Encoding UTF8
        if ($t -match 'BoundedFifoSnapshotStore') { $cognitionBoundedFifoAdoptionCount++ }
    }
}

$cognitionAppDir = Join-Path $root 'Tannous.Pos.Application'
if (Test-Path $cognitionAppDir) {
    $continuityPhrasingUsageCount = @(Get-ChildItem -Path $cognitionAppDir -Recurse -Filter '*.cs' |
        Where-Object { (Get-Content $_.FullName -Raw -Encoding UTF8) -match 'OperationalContinuityPhrasing' }).Count
    $boundedCollectionUsageCount = @(Get-ChildItem -Path $cognitionAppDir -Recurse -Filter '*.cs' |
        Where-Object { (Get-Content $_.FullName -Raw -Encoding UTF8) -match 'OperationalBoundedCollections' }).Count
}

# Step 47 briefing package metrics
$operationalBriefingPackageCount = 0
$briefingAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalBriefing\OperationalBriefingAggregation.cs'
$briefingServicePath = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalBriefingService.cs'
$briefingControllerPath = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditBriefingController.cs'
if (Test-Path $briefingAggregationPath) {
    $operationalBriefingPackageCount = @([regex]::Matches(
        (Get-Content $briefingAggregationPath -Raw -Encoding UTF8),
        'ComposeBriefingPackage|ComposeBriefingSummary|BriefingCognitionAge|ClassifyAge')).Count
}
if (Test-Path $briefingServicePath) {
    $operationalBriefingPackageCount += @([regex]::Matches(
        (Get-Content $briefingServicePath -Raw -Encoding UTF8),
        'OperationalBriefingService|GetBriefingPackageAsync|GetBriefingSummaryAsync|Operational briefing observability')).Count
}
if (Test-Path $briefingControllerPath) {
    $operationalBriefingPackageCount += @([regex]::Matches(
        (Get-Content $briefingControllerPath -Raw -Encoding UTF8),
        'GetBriefingPackage|GetBriefingSummary|internal/operational-audit/briefing')).Count
}

# Step 48 handoff continuity metrics
$operationalHandoffContinuityCount = 0
$handoffAggregationPath = Join-Path $root 'Tannous.Pos.Application\OperationalHandoff\OperationalHandoffAggregation.cs'
$handoffServicePath     = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalHandoffService.cs'
$handoffControllerPath  = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditHandoffController.cs'
if (Test-Path $handoffAggregationPath) {
    $operationalHandoffContinuityCount = @([regex]::Matches(
        (Get-Content $handoffAggregationPath -Raw -Encoding UTF8),
        'ComposeHandoffContinuity|ComposeHandoffSummary|ClassifyTransition|HandoffContinuityTransition|OperationalContinuityPhrasing')).Count
}
if (Test-Path $handoffServicePath) {
    $operationalHandoffContinuityCount += @([regex]::Matches(
        (Get-Content $handoffServicePath -Raw -Encoding UTF8),
        'OperationalHandoffService|GetHandoffContinuityAsync|GetHandoffSummaryAsync|Operational handoff observability')).Count
}
if (Test-Path $handoffControllerPath) {
    $operationalHandoffContinuityCount += @([regex]::Matches(
        (Get-Content $handoffControllerPath -Raw -Encoding UTF8),
        'GetHandoffContinuity|GetHandoffSummary|internal/operational-audit/handoff')).Count
}

# Step 49 entity status metrics
$operationalEntityStatusCount = 0
$entityStatusAggregationPath  = Join-Path $root 'Tannous.Pos.Application\OperationalEntityStatus\OperationalEntityStatusAggregation.cs'
$entityStatusServicePath      = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalEntityStatusService.cs'
$entityStatusControllerPath   = Join-Path $root 'Tannous.Pos.WebApi\Controllers\Internal\OperationalAuditEntityStatusController.cs'
$auditQueryServicePath        = Join-Path $root 'Tannous.Pos.Infrastructure\Services\OperationalAuditQueryService.cs'
if (Test-Path $entityStatusAggregationPath) {
    $operationalEntityStatusCount = @([regex]::Matches(
        (Get-Content $entityStatusAggregationPath -Raw -Encoding UTF8),
        'ClassifyHealth|ComposeOrderNarrative|ComposeDeviceNarrative|EntityHealthClassification')).Count
}
if (Test-Path $entityStatusServicePath) {
    $operationalEntityStatusCount += @([regex]::Matches(
        (Get-Content $entityStatusServicePath -Raw -Encoding UTF8),
        'OperationalEntityStatusService|GetOrderStatusAsync|GetDeviceStatusAsync|Operational entity status observability')).Count
}
if (Test-Path $entityStatusControllerPath) {
    $operationalEntityStatusCount += @([regex]::Matches(
        (Get-Content $entityStatusControllerPath -Raw -Encoding UTF8),
        'GetOrderStatus|GetDeviceStatus|internal/operational-audit/entity-status')).Count
}
if (Test-Path $auditQueryServicePath) {
    $operationalEntityStatusCount += @([regex]::Matches(
        (Get-Content $auditQueryServicePath -Raw -Encoding UTF8),
        'GetOrderAuditSummaryAsync|GetDeviceAuditSummaryAsync|SyncConflictRecords|SyncOperationReceipts')).Count
}

# Step 50 — Operational Order Investigation View
$operationalInvestigationCount = (Get-ChildItem -Recurse -Filter "*.cs" |
    Select-String -Pattern "OperationalInvestigation" -SimpleMatch).Count

$investigationServiceCount = (Get-ChildItem -Recurse -Filter "*.cs" |
    Select-String -Pattern "IOperationalInvestigationService" -SimpleMatch).Count

$orderAuditHighlightsCount = (Get-ChildItem -Recurse -Filter "*.cs" |
    Select-String -Pattern "GetOrderAuditHighlightsAsync" -SimpleMatch).Count

# Step 51 — Device Investigation View
$deviceInvestigationCount = (Get-ChildItem -Recurse -Filter "*.cs" |
    Select-String -Pattern "GetDeviceInvestigationAsync" -SimpleMatch).Count

$deviceAuditHighlightsCount = (Get-ChildItem -Recurse -Filter "*.cs" |
    Select-String -Pattern "GetDeviceAuditHighlightsAsync" -SimpleMatch).Count

# Step 52 — Operational Reconciliation System View
$operationalReconciliationSystemCount = (Get-ChildItem -Recurse -Filter "*.cs" |
    Select-String -Pattern "OperationalReconciliationSystem" -SimpleMatch).Count

$reconciliationSystemHealthCount = (Get-ChildItem -Recurse -Filter "*.cs" |
    Select-String -Pattern "ReconciliationSystemHealth" -SimpleMatch).Count

$reconciliationSystemSummaryCount = (Get-ChildItem -Recurse -Filter "*.cs" |
    Select-String -Pattern "GetReconciliationSystemSummaryAsync" -SimpleMatch).Count

$lastGeneratedUtc = [DateTime]::UtcNow.ToString('o')

# JSON: alphabetically ordered keys (deterministic)
$report = [ordered]@{
    allowAnonymousCount                     = $allowAnonHits
    conflictProblemDetailsCount             = $conflictProblemDetailsCount
    concurrencyExceptionHandlingCount       = $concurrencyExceptionHandlingCount
    concurrencyReadyEntityCount             = $concurrencyReadyEntityCount
    concurrencyTokenEntityCount             = $concurrencyTokenEntityCount
    concurrencyUpgradePlannedEntityCount  = $concurrencyUpgradePlannedEntityCount
    changeDueProtectionCount                  = $changeDueProtectionCount
    concurrencyWarningLogCount              = $concurrencyWarningLogCount
    controllerCount                         = $controllerCount
    coordinationInterpretationCount = $coordinationInterpretationCount
    customerShiftReplayProtectedProcessorCount = $customerShiftReplayProtectedProcessorCount
    customerShiftReplayVisibilityCount      = $customerShiftReplayVisibilityCount
    durableReplayProtectedProcessorCount    = $durableReplayProtectedProcessorCount
    durableSyncReplayEntityPresent          = $durableSyncReplayEntityPresent
    equilibriumAggregationCount = $equilibriumAggregationCount
    equilibriumContinuityCount = $equilibriumContinuityCount
    cognitionBoundedFifoAdoptionCount       = $cognitionBoundedFifoAdoptionCount
    continuityPhrasingUsageCount            = $continuityPhrasingUsageCount
    boundedCollectionUsageCount             = $boundedCollectionUsageCount
    explicitReplayWarningCount              = $explicitReplayWarningCount
    explicitTransactionCount                = $explicitTransactionCount
    FIXMECount                              = $fixmeCount
    governanceRiskCommentCount              = $governanceRiskCommentCount
    governanceWarningLogCount               = $governanceWarningLogCount
    idempotencyShortCircuitLogCount         = $idempotencyShortCircuitLogCount
    idempotencyWarningLogCount              = $idempotencyWarningLogCount
    imbalanceInterpretationCount = $imbalanceInterpretationCount
    inventoryConsistencyWarningCount        = $inventoryConsistencyWarningCount
    inventoryMovementObservabilityCount     = $inventoryMovementObservabilityCount
    inventoryReplayProtectedProcessorCount  = $inventoryReplayProtectedProcessorCount
    inventoryReplayReceiptCount             = $inventoryReplayReceiptCount
    inventoryDriftConflictCount             = $inventoryDriftConflictCount
    inventoryReversalMovementCount          = $inventoryReversalMovementCount
    knownNugetAutoMapperAdvisoryCount       = $knownNugetAutoMapperAdvisoryCount
    lifecycleConflictCount                  = $lifecycleConflictCount
    lastGeneratedUtc                        = $lastGeneratedUtc
    missingDurableIdempotencyCommentCount   = $missingDurableIdempotencyCommentCount
    netCapturedRefundCount                  = $netCapturedRefundCount
    operationalAuditRecordCount             = $operationalAuditRecordCount
    operationalAuditEndpointCount           = $operationalAuditEndpointCount
    operationalBriefingPackageCount         = $operationalBriefingPackageCount
    operationalHandoffContinuityCount       = $operationalHandoffContinuityCount
    operationalEntityStatusCount            = $operationalEntityStatusCount
    operationalDashboardAggregationCount    = $operationalDashboardAggregationCount
    operationalDashboardHealthVisibilityCount = $operationalDashboardHealthVisibilityCount
    operationalDashboardRecommendationCount = $operationalDashboardRecommendationCount
    operationalDashboardReadModelCount      = $operationalDashboardReadModelCount
    operationalWorkbenchAggregationCount    = $operationalWorkbenchAggregationCount
    operationalWorkbenchAttentionVisibilityCount = $operationalWorkbenchAttentionVisibilityCount
    operationalWorkbenchReplayRiskVisibilityCount = $operationalWorkbenchReplayRiskVisibilityCount
    operationalWorkbenchInventoryDriftCount = $operationalWorkbenchInventoryDriftCount
    operationalInventoryWorkbenchAggregationCount = $operationalInventoryWorkbenchAggregationCount
    operationalInventoryDriftVisibilityCount = $operationalInventoryDriftVisibilityCount
    operationalInventoryResolutionVisibilityCount = $operationalInventoryResolutionVisibilityCount
    operationalInventoryHotspotCount = $operationalInventoryHotspotCount
    operationalReplayWorkbenchAggregationCount = $operationalReplayWorkbenchAggregationCount
    operationalReplayPressureVisibilityCount = $operationalReplayPressureVisibilityCount
    operationalReplayRecoveryConfidenceCount = $operationalReplayRecoveryConfidenceCount
    operationalReplayHotspotCount = $operationalReplayHotspotCount
    operationalCompositionHubCount = $operationalCompositionHubCount
    operationalCompositionReuseCount = $operationalCompositionReuseCount
    operationalCompositionDepthReductionCount = $operationalCompositionDepthReductionCount
    operationalTrendAggregationCount = $operationalTrendAggregationCount
    operationalTrendDeltaVisibilityCount = $operationalTrendDeltaVisibilityCount
    operationalTrendWindowCount = $operationalTrendWindowCount
    operationalTrendAttentionCount = $operationalTrendAttentionCount
    operationalNavigationAggregationCount = $operationalNavigationAggregationCount
    operationalNavigationRouteCount = $operationalNavigationRouteCount
    operationalNavigationAttentionCount = $operationalNavigationAttentionCount
    operationalNavigationRecommendationCount = $operationalNavigationRecommendationCount
    operationalTimelineAggregationCount = $operationalTimelineAggregationCount
    operationalTimelineCorrelationCount = $operationalTimelineCorrelationCount
    operationalTimelineRetentionCount = $operationalTimelineRetentionCount
    operationalTimelineAttentionCount = $operationalTimelineAttentionCount
    operationalTriageAggregationCount = $operationalTriageAggregationCount
    operationalTriagePriorityCount = $operationalTriagePriorityCount
    operationalTriageCorrelationCount = $operationalTriageCorrelationCount
    operationalTriageRecommendationCount = $operationalTriageRecommendationCount
    operationalRecoveryAggregationCount = $operationalRecoveryAggregationCount
    operationalRecoveryConvergenceCount = $operationalRecoveryConvergenceCount
    operationalRecoveryOutlookCount = $operationalRecoveryOutlookCount
    operationalRecoveryConfidenceCount = $operationalRecoveryConfidenceCount
    operationalIncidentAggregationCount = $operationalIncidentAggregationCount
    operationalIncidentRecurrenceCount = $operationalIncidentRecurrenceCount
    operationalIncidentInvestigationCount = $operationalIncidentInvestigationCount
    operationalIncidentRetentionCount = $operationalIncidentRetentionCount
    operationalCausalityAggregationCount = $operationalCausalityAggregationCount
    operationalCausalityPropagationCount = $operationalCausalityPropagationCount
    operationalCausalityBlockerCount = $operationalCausalityBlockerCount
    operationalCausalityContinuityCount = $operationalCausalityContinuityCount
    operationalSituationRoomAggregationCount = $operationalSituationRoomAggregationCount
    operationalExecutiveBriefingCount = $operationalExecutiveBriefingCount
    operationalNarrativeSynthesisCount = $operationalNarrativeSynthesisCount
    operationalAttentionCount = $operationalAttentionCount
    operationalSimulationAggregationCount = $operationalSimulationAggregationCount
    operationalStabilizationScenarioCount = $operationalStabilizationScenarioCount
    operationalDegradationScenarioCount = $operationalDegradationScenarioCount
    operationalLeverageInterpretationCount = $operationalLeverageInterpretationCount
    operationalPlaybookAggregationCount = $operationalPlaybookAggregationCount
    operationalPriorityInterpretationCount = $operationalPriorityInterpretationCount
    operationalStabilizationGuidanceCount = $operationalStabilizationGuidanceCount
    operationalEscalationGuidanceCount = $operationalEscalationGuidanceCount
    operationalResponseSequencingCount = $operationalResponseSequencingCount
    operationalPatternAggregationCount = $operationalPatternAggregationCount
    operationalArchetypeRecognitionCount = $operationalArchetypeRecognitionCount
    operationalStabilizationPatternCount = $operationalStabilizationPatternCount
    operationalEscalationPatternCount = $operationalEscalationPatternCount
    operationalIntegrityAggregationCount = $operationalIntegrityAggregationCount
    operationalContradictionDetectionCount = $operationalContradictionDetectionCount
    operationalNarrativeAlignmentCount = $operationalNarrativeAlignmentCount
    operationalCoherenceCount = $operationalCoherenceCount
    operationalExperienceGraphAggregationCount = $operationalExperienceGraphAggregationCount
    operationalTraversalGenerationCount = $operationalTraversalGenerationCount
    operationalContextualNavigationCount = $operationalContextualNavigationCount
    operationalRelationshipCount = $operationalRelationshipCount
    operationalDigestAggregationCount = $operationalDigestAggregationCount
    operationalHighlightCount = $operationalHighlightCount
    operationalExecutiveDigestCount = $operationalExecutiveDigestCount
    operationalNavigationHighlightCount = $operationalNavigationHighlightCount
    operationalEvolutionAggregationCount = $operationalEvolutionAggregationCount
    operationalTransitionCount = $operationalTransitionCount
    operationalMomentumInterpretationCount = $operationalMomentumInterpretationCount
    operationalEvolutionContinuityCount = $operationalEvolutionContinuityCount
    operationalTopologyAggregationCount = $operationalTopologyAggregationCount
    operationalDependencyChainCount = $operationalDependencyChainCount
    operationalInfluenceInterpretationCount = $operationalInfluenceInterpretationCount
    operationalTopologyContinuityCount = $operationalTopologyContinuityCount
    operationalConvergenceAggregationCount = $operationalConvergenceAggregationCount
    operationalReinforcementInterpretationCount = $operationalReinforcementInterpretationCount
    operationalAmbiguityAnalysisCount = $operationalAmbiguityAnalysisCount
    operationalAttentionAggregationCount = $operationalAttentionAggregationCount
    operationalAttentionCoordinationCount = $operationalAttentionCoordinationCount
    operationalDivergenceInterpretationCount = $operationalDivergenceInterpretationCount
    operationalEmphasisCount = $operationalEmphasisCount
    operationalResilienceCognitionAggregationCount = $operationalResilienceCognitionAggregationCount
    operationalSurvivabilityInterpretationCount = $operationalSurvivabilityInterpretationCount
    operationalFragilityAnalysisCount = $operationalFragilityAnalysisCount
    operationalContainmentDurabilityCount = $operationalContainmentDurabilityCount
    strategicAlignmentCount = $strategicAlignmentCount
    strategyAggregationCount = $strategyAggregationCount
    strategyContinuityCount = $strategyContinuityCount
    operationalTimelineQueryCount           = $operationalTimelineQueryCount
    conflictDiagnosticsCount                = $conflictDiagnosticsCount
    auditPaginationProtectionCount          = $auditPaginationProtectionCount
    internalDiagnosticsAuthorizationCount   = $internalDiagnosticsAuthorizationCount
    auditObservabilityAnchorCount           = $auditObservabilityAnchorCount
    timelineReconstructionCount             = $timelineReconstructionCount
    financialAuditAnchorCount               = $financialAuditAnchorCount
    reconciliationAuditAnchorCount          = $reconciliationAuditAnchorCount
    reconciliationWorkflowEndpointCount     = $reconciliationWorkflowEndpointCount
    reconciliationStatusTransitionCount     = $reconciliationStatusTransitionCount
    reconciliationAuditActionCount          = $reconciliationAuditActionCount
    unresolvedConflictQueryCount            = $unresolvedConflictQueryCount
    reconciliationSummaryCount              = $reconciliationSummaryCount
    forensicExportEndpointCount             = $forensicExportEndpointCount
    forensicSnapshotGenerationCount         = $forensicSnapshotGenerationCount
    forensicMetadataSanitizationCount       = $forensicMetadataSanitizationCount
    forensicTimelineAggregationCount        = $forensicTimelineAggregationCount
    forensicAuthorizationProtectionCount    = $forensicAuthorizationProtectionCount
    operationalRetentionProtectionCount     = $operationalRetentionProtectionCount
    forensicTruncationObservabilityCount    = $forensicTruncationObservabilityCount
    retentionSummaryEndpointCount           = $retentionSummaryEndpointCount
    agedConflictClassificationCount         = $agedConflictClassificationCount
    exportSurvivabilityMetadataCount        = $exportSurvivabilityMetadataCount
    queryClampProtectionCount               = $queryClampProtectionCount
    degradedModeClassificationCount         = $degradedModeClassificationCount
    resilienceEndpointCount                 = $resilienceEndpointCount
    replayStormVisibilityCount              = $replayStormVisibilityCount
    exportPressureClassificationCount       = $exportPressureClassificationCount
    auditPersistenceResilienceCount         = $auditPersistenceResilienceCount
    operationalPressureIndicatorCount       = $operationalPressureIndicatorCount
    backpressureObservabilityCount          = $backpressureObservabilityCount
    incidentCorrelationEndpointCount        = $incidentCorrelationEndpointCount
    causalityObservabilityCount             = $causalityObservabilityCount
    correlatedRiskClassificationCount       = $correlatedRiskClassificationCount
    cascadingDegradationCount               = $cascadingDegradationCount
    forensicIncidentEnrichmentCount         = $forensicIncidentEnrichmentCount
    replayIncidentAggregationCount          = $replayIncidentAggregationCount
    incidentSeverityClassificationCount     = $incidentSeverityClassificationCount
    operationalAlertSignalCount             = $operationalAlertSignalCount
    criticalAlertVisibilityCount            = $criticalAlertVisibilityCount
    alertEscalationObservabilityCount       = $alertEscalationObservabilityCount
    replayPressureAlertCount                = $replayPressureAlertCount
    inventoryRiskAlertCount                 = $inventoryRiskAlertCount
    alertDiagnosticsEndpointCount           = $alertDiagnosticsEndpointCount
    cacheHitObservabilityCount              = $cacheHitObservabilityCount
    cacheMissObservabilityCount             = $cacheMissObservabilityCount
    cacheTtlGovernanceCount                 = $cacheTtlGovernanceCount
    staleRiskVisibilityCount                = $staleRiskVisibilityCount
    resilienceCacheReuseCount               = $resilienceCacheReuseCount
    cachePressureBypassCount              = $cachePressureBypassCount
    staleSnapshotRiskCount                  = $staleSnapshotRiskCount
    reconciliationCacheReuseCount         = $reconciliationCacheReuseCount
    incidentCacheReuseCount                 = $incidentCacheReuseCount
    incidentGroupReuseCount                 = $incidentGroupReuseCount
    alertCacheReuseCount                    = $alertCacheReuseCount
    forensicCompactSummaryCount             = $forensicCompactSummaryCount
    forensicLiveExportProtectionCount       = $forensicLiveExportProtectionCount
    cachedOperationalCompositionCount       = $cachedOperationalCompositionCount
    cacheDiagnosticsEndpointCount           = $cacheDiagnosticsEndpointCount
    cacheEffectivenessVisibilityCount       = $cacheEffectivenessVisibilityCount
    cacheMetadataProjectionCount            = $cacheMetadataProjectionCount
    cachePressureVisibilityCount            = $cachePressureVisibilityCount
    cacheInvalidationCount                  = $cacheInvalidationCount
    scopedCacheKeyCount                     = $scopedCacheKeyCount
    alertCacheLayerCount                    = $alertCacheLayerCount
    cacheFreshnessRecoveryCount             = $cacheFreshnessRecoveryCount
    targetedInvalidationHookCount           = $targetedInvalidationHookCount
    adaptiveTtlReductionCount               = $adaptiveTtlReductionCount
    warmCandidateVisibilityCount            = $warmCandidateVisibilityCount
    cacheReadinessSignalCount               = $cacheReadinessSignalCount
    cacheStabilityClassificationCount       = $cacheStabilityClassificationCount
    predictiveWarmRecommendationCount       = $predictiveWarmRecommendationCount
    cacheCardinalityGovernanceCount         = $cacheCardinalityGovernanceCount
    cacheGovernanceOverviewCount            = $cacheGovernanceOverviewCount
    cachePressureClassificationCount        = $cachePressureClassificationCount
    cacheDegradationVisibilityCount         = $cacheDegradationVisibilityCount
    scopedCacheSurvivabilityCount           = $scopedCacheSurvivabilityCount
    cacheScopeChurnVisibilityCount          = $cacheScopeChurnVisibilityCount
    governanceAuditProjectionCount          = $governanceAuditProjectionCount
    governanceDriftVisibilityCount          = $governanceDriftVisibilityCount
    governanceDriftAnalysisCount            = $governanceDriftAnalysisCount
    diagnosticsConsistencyVisibilityCount = $diagnosticsConsistencyVisibilityCount
    survivabilityClassificationCount        = $survivabilityClassificationCount
    operatorRecommendationVisibilityCount = $operatorRecommendationVisibilityCount
    cacheExplainabilitySignalCount          = $cacheExplainabilitySignalCount
    cacheInvalidationAuditCount             = $cacheInvalidationAuditCount
    freshnessRecoveryVisibilityCount        = $freshnessRecoveryVisibilityCount
    crossCategoryInvalidationCount          = $crossCategoryInvalidationCount
    cacheRecoveryGuidanceCount              = $cacheRecoveryGuidanceCount
    invalidationDriftVisibilityCount        = $invalidationDriftVisibilityCount
    consistencyRecoveryVisibilityCount      = $consistencyRecoveryVisibilityCount
    containmentGovernanceCount              = $containmentGovernanceCount
    propagationDiagnosticsCount             = $propagationDiagnosticsCount
    consistencyConfidenceVisibilityCount    = $consistencyConfidenceVisibilityCount
    recoveryStabilizationSignalCount        = $recoveryStabilizationSignalCount
    pressureLifecycleGovernanceCount        = $pressureLifecycleGovernanceCount
    pressureRecoveryGovernanceCount         = $pressureRecoveryGovernanceCount
    pressureConvergenceGovernanceCount      = $pressureConvergenceGovernanceCount
    pressureResetCoordinatorCount           = $pressureResetCoordinatorCount
    pressureStabilizationResetCount         = $pressureStabilizationResetCount
    governanceExplainabilityComposerCount = $governanceExplainabilityComposerCount
    governanceExecutionBudgetCount        = $governanceExecutionBudgetCount
    governanceFreezeEnforcementCount    = $governanceFreezeEnforcementCount
    governanceDeadSurfaceVisibilityCount = $governanceDeadSurfaceVisibilityCount
    governanceDeterminismAuditCount     = $governanceDeterminismAuditCount
    governanceRuntimeConsistencyCount   = $governanceRuntimeConsistencyCount
    governanceOwnershipBoundaryCount    = $governanceOwnershipBoundaryCount
    governanceComplexityReductionCount    = $governanceComplexityReductionCount
    governanceCompositionContextCount     = $governanceCompositionContextCount
    governanceProjectionCollaboratorCount = $governanceProjectionCollaboratorCount
    governanceSurfaceBudgetCount            = $governanceSurfaceBudgetCount
    governanceThresholdEvaluatorCount       = $governanceThresholdEvaluatorCount
    governanceModuleRegistryCount           = $governanceModuleRegistryCount
    governancePipelineStageCount            = $governancePipelineStageCount
    governanceConventionsCount              = $governanceConventionsCount
    governanceComplexityBudgetCount         = $governanceComplexityBudgetCount
    governanceRuntimeProtectionCount        = $governanceRuntimeProtectionCount
    telemetrySaturationVisibilityCount      = $telemetrySaturationVisibilityCount
    governanceFailsafeVisibilityCount       = $governanceFailsafeVisibilityCount
    governanceFingerprintVisibilityCount  = $governanceFingerprintVisibilityCount
    governanceFreezeRecommendationCount   = $governanceFreezeRecommendationCount
    governanceProductionReadinessCount  = $governanceProductionReadinessCount
    runtimeBudgetEnforcementCount           = $runtimeBudgetEnforcementCount
    runtimeBaselineVisibilityCount          = $runtimeBaselineVisibilityCount
    projectionComplexityClassificationCount = $projectionComplexityClassificationCount
    projectionFingerprintDeterminismCount   = $projectionFingerprintDeterminismCount
    governanceSnapshotReuseCount            = $governanceSnapshotReuseCount
    projectionReuseVisibilityCount          = $projectionReuseVisibilityCount
    snapshotConsistencyVisibilityCount      = $snapshotConsistencyVisibilityCount
    governanceSnapshotFreshnessCount        = $governanceSnapshotFreshnessCount
    governanceSignatureTransitionCount      = $governanceSignatureTransitionCount
    projectionReuseEfficiencyCount          = $projectionReuseEfficiencyCount
    moneyAffectingPlaceholderProcessorCount = $moneyAffectingPlaceholderProcessorCount
    moneyInventoryReplayClassificationCount = $moneyInventoryReplayClassificationCount
    moneyPathGovernanceAnchorCount          = $moneyPathGovernanceAnchorCount
    moneyPathReplayRiskCount                = $moneyPathReplayRiskCount
    moneyReplayRiskProcessorCount           = $moneyReplayRiskProcessorCount
    OpenAPITrackedPathCount                 = $OpenAPITrackedPathCount
    optimisticConcurrencyEntityCount        = $optimisticConcurrencyEntityCount
    overpaymentObservabilityCount           = $overpaymentObservabilityCount
    overpaymentSettlementCount              = $overpaymentSettlementCount
    partialBatchReplayWarningCount          = $partialBatchReplayWarningCount
    partialBatchWarningCount                = $partialBatchWarningCount
    placeholderProcessorCount               = $placeholderProcessorCount
    pressureDistributionCount = $pressureDistributionCount
    placeholderReplayGovernanceCount         = $placeholderReplayGovernanceCount
    protectedPlaceholderProcessorCount      = $protectedPlaceholderProcessorCount
    posDbContextInjectionCount              = $dbCtxHits
    reconciliationWarningCount              = $reconciliationWarningCount
    refundConsistencyAnchorCount            = $refundConsistencyAnchorCount
    refundIdempotencyProtectionCount        = $refundIdempotencyProtectionCount
    refundPersistenceCount                  = $refundPersistenceCount
    partialBatchObservabilityAnchorCount    = $partialBatchObservabilityAnchorCount
    placeholderClassificationCount          = $placeholderClassificationCount
    repositoryInjectionCount                = $repoHits
    replayProtectedCustomerShiftProcessorCount = $replayProtectedCustomerShiftProcessorCount
    replayProtectedInventoryProcessorCount  = $replayProtectedInventoryProcessorCount
    replayMismatchConflictCount             = $replayMismatchConflictCount
    replayMixedBatchWarningCount            = $replayMixedBatchWarningCount
    replayReconciliationVisibilityCount     = $replayReconciliationVisibilityCount
    replayConsistencyVisibilityCount        = $replayConsistencyVisibilityCount
    reconciliationObservabilityCount        = $reconciliationObservabilityCount
    replayShortCircuitClassificationCount   = $replayShortCircuitClassificationCount
    reversalConcurrencyHandlingCount        = $reversalConcurrencyHandlingCount
    reversalObservabilityAnchorCount        = $reversalObservabilityAnchorCount
    reversalTransactionBoundaryCount        = $reversalTransactionBoundaryCount
    settlementObservabilityCount            = $settlementObservabilityCount
    replayReceiptEntityCount                = $replayReceiptEntityCount
    paidVoidReversalProtectionCount         = $paidVoidReversalProtectionCount
    replayReceiptLookupCount                = $replayReceiptLookupCount
    replayReceiptUniqueIndexCount           = $replayReceiptUniqueIndexCount
    replaySensitiveMoneyProcessorCount      = $replaySensitiveMoneyProcessorCount
    replaySensitiveProcessorCount           = $replaySensitiveProcessorCount
    syncBatchClassificationCount            = $syncBatchClassificationCount
    syncConflictRecordCount                 = $syncConflictRecordCount
    syncReplayRiskCommentCount              = $syncReplayRiskCommentCount
    taxDivergenceGovernanceCount            = $taxDivergenceGovernanceCount
    todoCountInOrders                       = $todoOrders
    todoCountInSync                         = $syncTodo
    transactionBoundaryAnchorCount          = $transactionBoundaryAnchorCount
    transactionBoundaryLogAnchorCount       = $transactionBoundaryLogAnchorCount
    unversionedControllerCount              = $unversionedRouteHits
}

$jsonText = ($report | ConvertTo-Json -Compress)
[System.IO.File]::WriteAllText($jsonOut, $jsonText + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))

Write-Host '=== Governance debt scan ===' -ForegroundColor Cyan
Write-Host "Controller .cs files: $controllerCount"
Write-Host "Durable sync replay entity in EF snapshot (0/1): $durableSyncReplayEntityPresent"
Write-Host "PosDbContext references (all occurrences in controllers): $dbCtxHits"
Write-Host "I*Repository references in controllers: $repoHits"
Write-Host "[AllowAnonymous] occurrences in controllers: $allowAnonHits"
Write-Host "Controllers with api/[controller] route but no version token in file: $unversionedRouteHits (heuristic)"
Write-Host "HTTP mutation attributes ([HttpPost|Put|Patch|Delete]) in controllers: $mutationHits"
Write-Host "Explicit 'Device-Id header is required' string checks in controllers: $explicitDeviceCheckHits"
Write-Host "Program.cs registers global RequireDeviceIdFilter: $globalDeviceFilter"
Write-Host "TODO (word) in Tannous.Pos.Application/Orders (recursive): $todoOrders"
Write-Host "TODO (word) in SyncController.cs: $syncTodo"
Write-Host "TODO|FIXME in OrdersController.cs: $ordersTodo"
Write-Host "FIXME (Orders + SyncController + OrdersController paths): $fixmeCount"
Write-Host "GOVERNANCE / RISK comments under WebApi: $governanceRiskCommentCount"
Write-Host "LogWarning calls under Tannous.Pos.Application/Orders (recursive): $governanceWarningLogCount"
Write-Host "BeginTransactionAsync in Application + Infrastructure (excl. bin/obj): $explicitTransactionCount"
Write-Host "SyncController 'Placeholder success' occurrences: $placeholderProcessorCount"
Write-Host "SyncController replay|idempotency token matches (lines): $syncReplayRiskCommentCount"
Write-Host "OpenAPI baseline tracked paths: $OpenAPITrackedPathCount"
Write-Host "Domain entities with concurrency token markers (baseline list): $concurrencyTokenEntityCount"
Write-Host "Money-affecting placeholder Process* blocks: $moneyAffectingPlaceholderProcessorCount"
Write-Host "SyncController explicit replay/idempotency LogWarning lines: $explicitReplayWarningCount"
Write-Host "Money-path governance Assert.Contains anchors: $moneyPathGovernanceAnchorCount"
Write-Host "Replay-sensitive Process* methods (placeholder|inventory): $replaySensitiveProcessorCount"
Write-Host "Finalize transaction boundary anchors (begin/commit/rollback): $transactionBoundaryAnchorCount"
Write-Host "Concurrency-ready entities (readiness baseline): $concurrencyReadyEntityCount"
Write-Host "Concurrency upgrade plan entity count: $concurrencyUpgradePlannedEntityCount"
Write-Host "DbUpdateConcurrencyException token matches (Application+WebApi+Infrastructure): $concurrencyExceptionHandlingCount"
Write-Host "Inventory durable replay protected type entries (Adjust+Waste): $inventoryReplayProtectedProcessorCount"
Write-Host "Inventory sync durable replay visibility log anchors in coordinator: $inventoryReplayReceiptCount"
Write-Host "SyncController durable replay wraps for AdjustInventory+RecordWastage: $replayProtectedInventoryProcessorCount"
Write-Host "Customer/shift durable replay protected type entries (OpenShift+CreateCustomer): $customerShiftReplayProtectedProcessorCount"
Write-Host "SyncController durable replay wraps for OpenShift+CreateCustomer: $replayProtectedCustomerShiftProcessorCount"
Write-Host "Customer/shift sync durable replay visibility log anchors in coordinator: $customerShiftReplayVisibilityCount"
Write-Host "Replay-protected placeholder Process* blocks (GOVERNANCE+RISK+Placeholder success): $protectedPlaceholderProcessorCount"
Write-Host "Sync reconciliation visibility log anchors: $replayReconciliationVisibilityCount"
Write-Host "Sync reconciliation replay-mixed-with-failure anchors: $replayMixedBatchWarningCount"
Write-Host "Durable replay coordinator ExecuteAsync wrappers in SyncController: $durableReplayProtectedProcessorCount"
Write-Host "SyncOperationReceipt entity file present (0/1): $replayReceiptEntityCount"
Write-Host "DurableSyncReplayCoordinator SyncOperationReceipts references: $replayReceiptLookupCount"
Write-Host "EF snapshot SyncOperationReceipt unique (DeviceId, OperationId) index (0/1): $replayReceiptUniqueIndexCount"
Write-Host "GlobalExceptionHandler Status409Conflict lines: $conflictProblemDetailsCount"
Write-Host "Optimistic concurrency governance log templates (finalize/void/global): $concurrencyWarningLogCount"
Write-Host "Optimistic concurrency entity rows (entity baseline JSON): $optimisticConcurrencyEntityCount"
Write-Host "Known NU1903 AutoMapper advisory (visibility count): $knownNugetAutoMapperAdvisoryCount"
Write-Host "Money replay risk Process* (money|inv + replay wording + placeholder/governance): $moneyReplayRiskProcessorCount"
Write-Host "Money-path replay|idempotency token matches (Create/Finalize/OpenShift/CashDrop bodies): $moneyPathReplayRiskCount"
Write-Host "Missing durable idempotency / replay persistence comment tokens (Orders + SyncController): $missingDurableIdempotencyCommentCount"
Write-Host "Reconciliation / manual review / mismatch / replay-risk tokens (Orders + SyncController): $reconciliationWarningCount"
Write-Host "SyncController partial-batch style LogWarning lines: $partialBatchWarningCount"
Write-Host "Orders subtree idempotency / short-circuit log token matches: $idempotencyShortCircuitLogCount"
Write-Host "Finalize + sync placeholder idempotency/replay LogWarning anchors: $idempotencyWarningLogCount"
Write-Host "Sync money-affecting replay visibility classification logs: $replaySensitiveMoneyProcessorCount"
Write-Host "Sync partial-batch replay visibility LogWarning anchors: $partialBatchReplayWarningCount"
Write-Host "Sync placeholder replay governance (ReplayClass=placeholder-only): $placeholderReplayGovernanceCount"
Write-Host "Inventory consistency observability token matches (Application/Orders): $inventoryConsistencyWarningCount"
Write-Host "Finalize AddMovementAsync occurrences (movement persistence anchor): $inventoryMovementObservabilityCount"
Write-Host "Paid void Return movement tokens in VoidOrderCommandHandler: $inventoryReversalMovementCount"
Write-Host "Inventory reversal observability log anchors (VoidOrderCommandHandler): $reversalObservabilityAnchorCount"
Write-Host "Paid void reversal idempotent short-circuit anchors: $paidVoidReversalProtectionCount"
Write-Host "Void reversal transaction boundary anchors (BeginTransactionAsync): $reversalTransactionBoundaryCount"
Write-Host "Void reversal concurrency observability anchors: $reversalConcurrencyHandlingCount"
Write-Host "Refund consistency observability anchors (VoidOrderCommandHandler): $refundConsistencyAnchorCount"
Write-Host "PaymentRefund persistence tokens in VoidOrderCommandHandler: $refundPersistenceCount"
Write-Host "Refund idempotent short-circuit anchors: $refundIdempotencyProtectionCount"
Write-Host "Finalize overpayment observability anchors: $overpaymentObservabilityCount"
Write-Host "Tax divergence GOVERNANCE anchors (order/receipt/refund docs): $taxDivergenceGovernanceCount"
Write-Host "Settlement consistency observability anchors (finalize+void): $settlementObservabilityCount"
Write-Host "Finalize change-due field assignments (order.ChangeDue): $changeDueProtectionCount"
Write-Host "Void net-captured refund resolver anchors: $netCapturedRefundCount"
Write-Host "Finalize overpayment settlement anchors: $overpaymentSettlementCount"
Write-Host "Sync Replay sensitivity classification comment lines: $moneyInventoryReplayClassificationCount"
Write-Host "Sync batch observability log anchors (Sync batch observability:): $syncBatchClassificationCount"
Write-Host "Sync partial batch classification log anchors: $partialBatchObservabilityAnchorCount"
Write-Host "Sync replay short-circuit scope markers (MarkReplayShortCircuited): $replayShortCircuitClassificationCount"
Write-Host "Sync placeholder classification tokens in classifier: $placeholderClassificationCount"
Write-Host "Finalize transaction-related structured log anchors: $transactionBoundaryLogAnchorCount"
Write-Host "SyncConflictRecord entity present (0/1): $syncConflictRecordCount"
Write-Host "Sync reconciliation observability log anchors (recorder): $reconciliationObservabilityCount"
Write-Host "Inventory drift conflict anchors (types+finalize): $inventoryDriftConflictCount"
Write-Host "Lifecycle/stale offline conflict anchors (types+handlers): $lifecycleConflictCount"
Write-Host "Replay mismatch conflict anchors (types+coordinator): $replayMismatchConflictCount"
Write-Host "OperationalAuditRecord entity present (0/1): $operationalAuditRecordCount"
Write-Host "Operational audit diagnostics GET endpoints: $operationalAuditEndpointCount"
Write-Host "Operational audit timeline/conflict query methods: $operationalTimelineQueryCount"
Write-Host "Operational audit conflict diagnostics anchors: $conflictDiagnosticsCount"
Write-Host "Operational audit pagination protection anchors: $auditPaginationProtectionCount"
Write-Host "Internal diagnostics Admin authorization anchors: $internalDiagnosticsAuthorizationCount"
Write-Host "Operational audit observability log anchors: $auditObservabilityAnchorCount"
Write-Host "Operational audit timeline query methods: $timelineReconstructionCount"
Write-Host "Financial operational audit anchors (finalize+void): $financialAuditAnchorCount"
Write-Host "Reconciliation/replay operational audit anchors: $reconciliationAuditAnchorCount"
Write-Host "Reconciliation workflow internal endpoints (GET+POST): $reconciliationWorkflowEndpointCount"
Write-Host "Reconciliation status transition anchors: $reconciliationStatusTransitionCount"
Write-Host "Reconciliation workflow audit action constants: $reconciliationAuditActionCount"
Write-Host "Unresolved conflict query anchors: $unresolvedConflictQueryCount"
Write-Host "Reconciliation summary query anchors: $reconciliationSummaryCount"
Write-Host "Forensic export GET endpoints: $forensicExportEndpointCount"
Write-Host "Forensic snapshot generation anchors: $forensicSnapshotGenerationCount"
Write-Host "Forensic metadata sanitization anchors: $forensicMetadataSanitizationCount"
Write-Host "Forensic timeline aggregation anchors: $forensicTimelineAggregationCount"
Write-Host "Forensic Admin authorization anchors: $forensicAuthorizationProtectionCount"
Write-Host "Operational retention protection anchors: $operationalRetentionProtectionCount"
Write-Host "Forensic truncation observability anchors: $forensicTruncationObservabilityCount"
Write-Host "Retention summary GET endpoints: $retentionSummaryEndpointCount"
Write-Host "Aged conflict classification anchors: $agedConflictClassificationCount"
Write-Host "Export survivability metadata anchors: $exportSurvivabilityMetadataCount"
Write-Host "Operational query clamp protection anchors: $queryClampProtectionCount"
Write-Host "Degraded mode classification anchors: $degradedModeClassificationCount"
Write-Host "Resilience internal GET endpoints: $resilienceEndpointCount"
Write-Host "Replay storm visibility anchors: $replayStormVisibilityCount"
Write-Host "Export pressure classification anchors: $exportPressureClassificationCount"
Write-Host "Audit persistence resilience anchors: $auditPersistenceResilienceCount"
Write-Host "Operational pressure indicator anchors: $operationalPressureIndicatorCount"
Write-Host "Backpressure observability anchors: $backpressureObservabilityCount"
Write-Host "Incident correlation GET endpoints: $incidentCorrelationEndpointCount"
Write-Host "Causality observability anchors: $causalityObservabilityCount"
Write-Host "Correlated risk classification anchors: $correlatedRiskClassificationCount"
Write-Host "Cascading degradation anchors: $cascadingDegradationCount"
Write-Host "Forensic incident enrichment anchors: $forensicIncidentEnrichmentCount"
Write-Host "Replay incident aggregation anchors: $replayIncidentAggregationCount"
Write-Host "Incident severity classification anchors: $incidentSeverityClassificationCount"
Write-Host "Operational alert signal type anchors: $operationalAlertSignalCount"
Write-Host "Critical alert escalation visibility anchors: $criticalAlertVisibilityCount"
Write-Host "Alert escalation observability anchors: $alertEscalationObservabilityCount"
Write-Host "Replay pressure alert derivation anchors: $replayPressureAlertCount"
Write-Host "Inventory risk alert derivation anchors: $inventoryRiskAlertCount"
Write-Host "Alert diagnostics GET endpoints: $alertDiagnosticsEndpointCount"
Write-Host "Cache hit observability anchors: $cacheHitObservabilityCount"
Write-Host "Cache miss observability anchors: $cacheMissObservabilityCount"
Write-Host "Cache TTL governance anchors: $cacheTtlGovernanceCount"
Write-Host "Stale risk visibility anchors: $staleRiskVisibilityCount"
Write-Host "Resilience cache reuse anchors: $resilienceCacheReuseCount"
Write-Host "Cache pressure bypass anchors: $cachePressureBypassCount"
Write-Host "Stale snapshot risk anchors: $staleSnapshotRiskCount"
Write-Host "Reconciliation cache reuse anchors: $reconciliationCacheReuseCount"
Write-Host "Incident cache reuse anchors: $incidentCacheReuseCount"
Write-Host "Incident group reuse anchors: $incidentGroupReuseCount"
Write-Host "Alert cache reuse anchors: $alertCacheReuseCount"
Write-Host "Forensic compact summary anchors: $forensicCompactSummaryCount"
Write-Host "Forensic live export protection anchors: $forensicLiveExportProtectionCount"
Write-Host "Cached operational composition anchors: $cachedOperationalCompositionCount"
Write-Host "Cache diagnostics endpoint anchors: $cacheDiagnosticsEndpointCount"
Write-Host "Cache effectiveness visibility anchors: $cacheEffectivenessVisibilityCount"
Write-Host "Cache metadata projection anchors: $cacheMetadataProjectionCount"
Write-Host "Cache pressure visibility anchors: $cachePressureVisibilityCount"
Write-Host "Cache invalidation anchors: $cacheInvalidationCount"
Write-Host "Scoped cache key anchors: $scopedCacheKeyCount"
Write-Host "Alert cache layer anchors: $alertCacheLayerCount"
Write-Host "Cache freshness recovery anchors: $cacheFreshnessRecoveryCount"
Write-Host "Targeted invalidation hook anchors: $targetedInvalidationHookCount"
Write-Host "Adaptive TTL reduction anchors: $adaptiveTtlReductionCount"
Write-Host "Warm candidate visibility anchors: $warmCandidateVisibilityCount"
Write-Host "Cache readiness signal anchors: $cacheReadinessSignalCount"
Write-Host "Cache stability classification anchors: $cacheStabilityClassificationCount"
Write-Host "Predictive warm recommendation anchors: $predictiveWarmRecommendationCount"
Write-Host "Cache cardinality governance anchors: $cacheCardinalityGovernanceCount"
Write-Host "Cache governance overview anchors: $cacheGovernanceOverviewCount"
Write-Host "Cache pressure classification anchors: $cachePressureClassificationCount"
Write-Host "Cache degradation visibility anchors: $cacheDegradationVisibilityCount"
Write-Host "Scoped cache survivability anchors: $scopedCacheSurvivabilityCount"
Write-Host "Cache scope churn visibility anchors: $cacheScopeChurnVisibilityCount"
Write-Host "Governance audit projection anchors: $governanceAuditProjectionCount"
Write-Host "Governance drift visibility anchors: $governanceDriftVisibilityCount"
Write-Host "Diagnostics consistency visibility anchors: $diagnosticsConsistencyVisibilityCount"
Write-Host "Survivability classification anchors: $survivabilityClassificationCount"
Write-Host "Operator recommendation visibility anchors: $operatorRecommendationVisibilityCount"
Write-Host "Cache explainability signal anchors: $cacheExplainabilitySignalCount"
Write-Host "Cache invalidation audit anchors: $cacheInvalidationAuditCount"
Write-Host "Freshness recovery visibility anchors: $freshnessRecoveryVisibilityCount"
Write-Host "Cross-category invalidation anchors: $crossCategoryInvalidationCount"
Write-Host "Cache recovery guidance anchors: $cacheRecoveryGuidanceCount"
Write-Host "Invalidation drift visibility anchors: $invalidationDriftVisibilityCount"
Write-Host "Consistency recovery visibility anchors: $consistencyRecoveryVisibilityCount"
Write-Host "Containment governance anchors: $containmentGovernanceCount"
Write-Host "Propagation diagnostics anchors: $propagationDiagnosticsCount"
Write-Host "Consistency confidence visibility anchors: $consistencyConfidenceVisibilityCount"
Write-Host "Recovery stabilization signal anchors: $recoveryStabilizationSignalCount"
Write-Host "Pressure lifecycle governance anchors: $pressureLifecycleGovernanceCount"
Write-Host "Pressure recovery governance anchors: $pressureRecoveryGovernanceCount"
Write-Host "Pressure convergence governance anchors: $pressureConvergenceGovernanceCount"
Write-Host "Pressure reset coordinator anchors: $pressureResetCoordinatorCount"
Write-Host "Pressure stabilization reset anchors: $pressureStabilizationResetCount"
Write-Host "Governance explainability composer anchors: $governanceExplainabilityComposerCount"
Write-Host "Governance composition context anchors: $governanceCompositionContextCount"
Write-Host "Governance projection collaborator files: $governanceProjectionCollaboratorCount"
Write-Host "Governance surface budget anchors: $governanceSurfaceBudgetCount"
Write-Host "Governance threshold evaluator anchors: $governanceThresholdEvaluatorCount"
Write-Host "Governance module registry anchors: $governanceModuleRegistryCount"
Write-Host "Governance pipeline stage anchors: $governancePipelineStageCount"
Write-Host "Governance conventions anchors: $governanceConventionsCount"
Write-Host "Governance complexity budget anchors: $governanceComplexityBudgetCount"
Write-Host "Governance runtime protection anchors: $governanceRuntimeProtectionCount"
Write-Host "Telemetry saturation visibility anchors: $telemetrySaturationVisibilityCount"
Write-Host "Governance failsafe visibility anchors: $governanceFailsafeVisibilityCount"
Write-Host "Runtime budget enforcement anchors: $runtimeBudgetEnforcementCount"
Write-Host "Projection complexity classification anchors: $projectionComplexityClassificationCount"
Write-Host "Governance snapshot reuse anchors: $governanceSnapshotReuseCount"
Write-Host "Projection reuse visibility anchors: $projectionReuseVisibilityCount"
Write-Host "Snapshot consistency visibility anchors: $snapshotConsistencyVisibilityCount"
Write-Host "Governance snapshot freshness anchors: $governanceSnapshotFreshnessCount"
Write-Host "Projection reuse efficiency anchors: $projectionReuseEfficiencyCount"
Write-Host "Governance fingerprint visibility anchors: $governanceFingerprintVisibilityCount"
Write-Host "Governance drift analysis anchors: $governanceDriftAnalysisCount"
Write-Host "Replay consistency visibility anchors: $replayConsistencyVisibilityCount"
Write-Host "Governance signature transition anchors: $governanceSignatureTransitionCount"
Write-Host "Projection fingerprint determinism anchors: $projectionFingerprintDeterminismCount"
Write-Host "Runtime baseline visibility anchors: $runtimeBaselineVisibilityCount"
Write-Host "Governance complexity reduction anchors: $governanceComplexityReductionCount"
Write-Host "Governance execution budget anchors: $governanceExecutionBudgetCount"
Write-Host "Governance production readiness anchors: $governanceProductionReadinessCount"
Write-Host "Governance freeze recommendation anchors: $governanceFreezeRecommendationCount"
Write-Host "Governance freeze enforcement anchors: $governanceFreezeEnforcementCount"
Write-Host "Governance dead surface visibility anchors: $governanceDeadSurfaceVisibilityCount"
Write-Host "Governance determinism audit anchors: $governanceDeterminismAuditCount"
Write-Host "Governance runtime consistency anchors: $governanceRuntimeConsistencyCount"
Write-Host "Governance ownership boundary anchors: $governanceOwnershipBoundaryCount"
Write-Host "Operational dashboard aggregation anchors: $operationalDashboardAggregationCount"
Write-Host "Operational dashboard health visibility anchors: $operationalDashboardHealthVisibilityCount"
Write-Host "Operational dashboard recommendation anchors: $operationalDashboardRecommendationCount"
Write-Host "Operational dashboard read model anchors: $operationalDashboardReadModelCount"
Write-Host "Operational workbench aggregation anchors: $operationalWorkbenchAggregationCount"
Write-Host "Operational workbench attention visibility anchors: $operationalWorkbenchAttentionVisibilityCount"
Write-Host "Operational workbench replay-risk visibility anchors: $operationalWorkbenchReplayRiskVisibilityCount"
Write-Host "Operational workbench inventory drift anchors: $operationalWorkbenchInventoryDriftCount"
Write-Host "Operational inventory workbench aggregation anchors: $operationalInventoryWorkbenchAggregationCount"
Write-Host "Operational inventory drift visibility anchors: $operationalInventoryDriftVisibilityCount"
Write-Host "Operational inventory resolution visibility anchors: $operationalInventoryResolutionVisibilityCount"
Write-Host "Operational inventory hotspot anchors: $operationalInventoryHotspotCount"
Write-Host "Operational replay workbench aggregation anchors: $operationalReplayWorkbenchAggregationCount"
Write-Host "Operational replay pressure visibility anchors: $operationalReplayPressureVisibilityCount"
Write-Host "Operational replay recovery confidence anchors: $operationalReplayRecoveryConfidenceCount"
Write-Host "Operational replay hotspot anchors: $operationalReplayHotspotCount"
Write-Host "Operational composition hub anchors: $operationalCompositionHubCount"
Write-Host "Operational composition reuse anchors: $operationalCompositionReuseCount"
Write-Host "Operational composition depth reduction anchors: $operationalCompositionDepthReductionCount"
Write-Host "Operational trend aggregation anchors: $operationalTrendAggregationCount"
Write-Host "Operational trend delta visibility anchors: $operationalTrendDeltaVisibilityCount"
Write-Host "Operational trend window anchors: $operationalTrendWindowCount"
Write-Host "Operational trend attention anchors: $operationalTrendAttentionCount"
Write-Host "Operational navigation aggregation anchors: $operationalNavigationAggregationCount"
Write-Host "Operational navigation route anchors: $operationalNavigationRouteCount"
Write-Host "Operational navigation attention anchors: $operationalNavigationAttentionCount"
Write-Host "Operational navigation recommendation anchors: $operationalNavigationRecommendationCount"
Write-Host "Operational timeline aggregation anchors: $operationalTimelineAggregationCount"
Write-Host "Operational timeline correlation anchors: $operationalTimelineCorrelationCount"
Write-Host "Operational timeline retention anchors: $operationalTimelineRetentionCount"
Write-Host "Operational timeline attention anchors: $operationalTimelineAttentionCount"
Write-Host "Operational triage aggregation anchors: $operationalTriageAggregationCount"
Write-Host "Operational triage priority anchors: $operationalTriagePriorityCount"
Write-Host "Operational triage correlation anchors: $operationalTriageCorrelationCount"
Write-Host "Operational triage recommendation anchors: $operationalTriageRecommendationCount"
Write-Host "Operational recovery aggregation anchors: $operationalRecoveryAggregationCount"
Write-Host "Operational recovery convergence anchors: $operationalRecoveryConvergenceCount"
Write-Host "Operational recovery outlook anchors: $operationalRecoveryOutlookCount"
Write-Host "Operational recovery confidence anchors: $operationalRecoveryConfidenceCount"
Write-Host "Operational incident aggregation anchors: $operationalIncidentAggregationCount"
Write-Host "Operational incident recurrence anchors: $operationalIncidentRecurrenceCount"
Write-Host "Operational incident investigation anchors: $operationalIncidentInvestigationCount"
Write-Host "Operational incident retention anchors: $operationalIncidentRetentionCount"
Write-Host "Operational causality aggregation anchors: $operationalCausalityAggregationCount"
Write-Host "Operational causality propagation anchors: $operationalCausalityPropagationCount"
Write-Host "Operational causality blocker anchors: $operationalCausalityBlockerCount"
Write-Host "Operational causality continuity anchors: $operationalCausalityContinuityCount"
Write-Host "Operational situation room aggregation anchors: $operationalSituationRoomAggregationCount"
Write-Host "Operational executive briefing anchors: $operationalExecutiveBriefingCount"
Write-Host "Operational narrative synthesis anchors: $operationalNarrativeSynthesisCount"
Write-Host "Operational attention anchors: $operationalAttentionCount"
Write-Host "Operational simulation aggregation anchors: $operationalSimulationAggregationCount"
Write-Host "Operational stabilization scenario anchors: $operationalStabilizationScenarioCount"
Write-Host "Operational degradation scenario anchors: $operationalDegradationScenarioCount"
Write-Host "Operational leverage interpretation anchors: $operationalLeverageInterpretationCount"
Write-Host "Operational playbook aggregation anchors: $operationalPlaybookAggregationCount"
Write-Host "Operational stabilization guidance anchors: $operationalStabilizationGuidanceCount"
Write-Host "Operational escalation guidance anchors: $operationalEscalationGuidanceCount"
Write-Host "Operational response sequencing anchors: $operationalResponseSequencingCount"
Write-Host "Operational pattern aggregation anchors: $operationalPatternAggregationCount"
Write-Host "Operational archetype recognition anchors: $operationalArchetypeRecognitionCount"
Write-Host "Operational stabilization pattern anchors: $operationalStabilizationPatternCount"
Write-Host "Operational escalation pattern anchors: $operationalEscalationPatternCount"
Write-Host "Operational integrity aggregation anchors: $operationalIntegrityAggregationCount"
Write-Host "Operational contradiction detection anchors: $operationalContradictionDetectionCount"
Write-Host "Operational narrative alignment anchors: $operationalNarrativeAlignmentCount"
Write-Host "Operational coherence anchors: $operationalCoherenceCount"
Write-Host "Operational experience graph aggregation anchors: $operationalExperienceGraphAggregationCount"
Write-Host "Operational traversal generation anchors: $operationalTraversalGenerationCount"
Write-Host "Operational contextual navigation anchors: $operationalContextualNavigationCount"
Write-Host "Operational relationship anchors: $operationalRelationshipCount"
Write-Host "Operational digest aggregation anchors: $operationalDigestAggregationCount"
Write-Host "Operational highlight anchors: $operationalHighlightCount"
Write-Host "Operational executive digest anchors: $operationalExecutiveDigestCount"
Write-Host "Operational navigation highlight anchors: $operationalNavigationHighlightCount"
Write-Host "Operational evolution aggregation anchors: $operationalEvolutionAggregationCount"
Write-Host "Operational transition anchors: $operationalTransitionCount"
Write-Host "Operational momentum interpretation anchors: $operationalMomentumInterpretationCount"
Write-Host "Operational evolution continuity anchors: $operationalEvolutionContinuityCount"
Write-Host "Operational topology aggregation anchors: $operationalTopologyAggregationCount"
Write-Host "Operational dependency chain anchors: $operationalDependencyChainCount"
Write-Host "Operational influence interpretation anchors: $operationalInfluenceInterpretationCount"
Write-Host "Operational topology continuity anchors: $operationalTopologyContinuityCount"
Write-Host "Operational convergence aggregation anchors: $operationalConvergenceAggregationCount"
Write-Host "Operational reinforcement interpretation anchors: $operationalReinforcementInterpretationCount"
Write-Host "Operational ambiguity analysis anchors: $operationalAmbiguityAnalysisCount"
Write-Host "Operational divergence interpretation anchors: $operationalDivergenceInterpretationCount"
Write-Host "Operational resilience cognition aggregation anchors: $operationalResilienceCognitionAggregationCount"
Write-Host "Operational survivability interpretation anchors: $operationalSurvivabilityInterpretationCount"
Write-Host "Operational fragility analysis anchors: $operationalFragilityAnalysisCount"
Write-Host "Operational containment durability anchors: $operationalContainmentDurabilityCount"
Write-Host "Operational attention aggregation anchors: $operationalAttentionAggregationCount"
Write-Host "Operational priority interpretation anchors: $operationalPriorityInterpretationCount"
Write-Host "Operational attention coordination anchors: $operationalAttentionCoordinationCount"
Write-Host "Operational emphasis anchors: $operationalEmphasisCount"
Write-Host "Strategy aggregation anchors: $strategyAggregationCount"
Write-Host "Strategic alignment anchors: $strategicAlignmentCount"
Write-Host "Coordination interpretation anchors: $coordinationInterpretationCount"
Write-Host "Strategy continuity anchors: $strategyContinuityCount"
Write-Host "Equilibrium aggregation anchors: $equilibriumAggregationCount"
Write-Host "Imbalance interpretation anchors: $imbalanceInterpretationCount"
Write-Host "Pressure distribution anchors: $pressureDistributionCount"
Write-Host "Equilibrium continuity anchors: $equilibriumContinuityCount"
Write-Host "Step 46 - Bounded FIFO store adoption (layer wrappers): $cognitionBoundedFifoAdoptionCount"
Write-Host "Step 46 - Continuity phrasing usage (Application files): $continuityPhrasingUsageCount"
Write-Host "Step 46 - Bounded collection usage (Application files): $boundedCollectionUsageCount"
Write-Host "Step 47 - Briefing package anchors: $operationalBriefingPackageCount"
Write-Host "Step 48 - Handoff continuity anchors: $operationalHandoffContinuityCount"
Write-Host "Step 49 - Entity status anchors: $operationalEntityStatusCount"
Write-Host "operationalInvestigationCount: $operationalInvestigationCount"
Write-Host "investigationServiceCount: $investigationServiceCount"
Write-Host "orderAuditHighlightsCount: $orderAuditHighlightsCount"
Write-Host "deviceInvestigationCount: $deviceInvestigationCount"
Write-Host "deviceAuditHighlightsCount: $deviceAuditHighlightsCount"
Write-Host "operationalReconciliationSystemCount: $operationalReconciliationSystemCount"
Write-Host "reconciliationSystemHealthCount: $reconciliationSystemHealthCount"
Write-Host "reconciliationSystemSummaryCount: $reconciliationSystemSummaryCount"
Write-Host "Wrote: $jsonOut" -ForegroundColor Green
Write-Host "`nNote: global RequireDeviceIdFilter covers mutations unless filter pipeline changes." -ForegroundColor DarkGray
Write-Host "(Constructor allowlists live in architecture tests.)" -ForegroundColor DarkGray
