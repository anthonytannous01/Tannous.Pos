#Requires -Version 5.1
<#
.SYNOPSIS
  Enforces governance debt budgets using governance/debt-report.json.

.DESCRIPTION
  Run after governance/scan-debt.ps1. Compares the generated report to fixed rules
  and to committed ceilings in governance/debt-baseline.json.

  Actual counts are always read from debt-report.json (the latest scan output).
  posDbContextInjectionCount and repositoryInjectionCount must not exceed the
  maxima in debt-baseline.json — those maxima should only shrink over time as
  controllers move behind Application/MediatR.

  Fallback: if debt-baseline.json is missing, uses posDbContextInjectionCountMax=9999
  and repositoryInjectionCountMax=9999 so CI does not block, but prints a WARNING
  (create debt-baseline.json from current scan for real enforcement).

  Non-failing trend warnings (optional): if governance/debt-warning-trend.json exists and a
  metric in debt-report.json exceeds the corresponding value in that file, a [WARN] line is
  printed (exit code remains 0 unless a hard budget rule fails).

  Exit code 1 if any hard rule fails.
#>

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$reportPath = Join-Path $here 'debt-report.json'
$baselinePath = Join-Path $here 'debt-baseline.json'
$trendPath = Join-Path $here 'debt-warning-trend.json'

function Read-JsonFile([string]$path) {
    if (-not (Test-Path $path)) {
        throw "Missing required file: $path"
    }
    $raw = Get-Content -Path $path -Raw -Encoding UTF8
    return $raw | ConvertFrom-Json
}

if (-not (Test-Path $reportPath)) {
    Write-Host 'FAIL: debt-report.json not found. Run governance/scan-debt.ps1 first.' -ForegroundColor Red
    exit 1
}

$report = Read-JsonFile $reportPath

$posMax = 9999
$repoMax = 9999
if (Test-Path $baselinePath) {
    $baseline = Read-JsonFile $baselinePath
    $posMax = [int]$baseline.posDbContextInjectionCountMax
    $repoMax = [int]$baseline.repositoryInjectionCountMax
}
else {
    Write-Host 'WARNING: governance/debt-baseline.json missing; using loose fallback ceilings (9999). Add debt-baseline.json for real enforcement.' -ForegroundColor Yellow
}

$rules = @(
    @{
        # Raised from 2 to 9 on 2026-09-05 after auditing every anonymous endpoint:
        # auth login + refresh, QuickBooks OAuth callback, HMAC-verified delivery webhook,
        # customer feedback submit, kiosk controller, public QR menu controller. All are
        # unauthenticated by design. The real protection is not this count but
        # AnonymousEndpointRateLimitGovernanceTests, which fails the build if any anonymous
        # endpoint lacks a rate limit policy. Raise this only after the same audit.
        Name   = 'allowAnonymousCount (must be <= 9)'
        Pass   = ([int]$report.allowAnonymousCount -le 9)
        Detail = "allowAnonymousCount=$($report.allowAnonymousCount) (threshold: >9 fails)"
    }
    @{
        # Tightened from 4 to 1 on 2026-09-05: the scan reports 1, so a ceiling of 4 allowed
        # three more unversioned controllers to appear unnoticed. Lower this again as the last
        # one is versioned; raise it only with a reason recorded here.
        Name   = 'unversionedControllerCount (must be <= 1)'
        Pass   = ([int]$report.unversionedControllerCount -le 1)
        Detail = "unversionedControllerCount=$($report.unversionedControllerCount) (threshold: >1 fails)"
    }
    @{
        Name   = 'posDbContextInjectionCount (must be <= baseline max)'
        Pass   = ([int]$report.posDbContextInjectionCount -le $posMax)
        Detail = "posDbContextInjectionCount=$($report.posDbContextInjectionCount) (max: $posMax)"
    }
    @{
        Name   = 'repositoryInjectionCount (must be <= baseline max)'
        Pass   = ([int]$report.repositoryInjectionCount -le $repoMax)
        Detail = "repositoryInjectionCount=$($report.repositoryInjectionCount) (max: $repoMax)"
    }
)

Write-Host '=== Governance debt budget ===' -ForegroundColor Cyan
Write-Host "Report: $reportPath"
Write-Host "lastGeneratedUtc: $($report.lastGeneratedUtc)"
Write-Host "controllerCount: $($report.controllerCount)"
Write-Host "posDbContextInjectionCount: $($report.posDbContextInjectionCount)"
Write-Host "repositoryInjectionCount: $($report.repositoryInjectionCount)"
Write-Host "allowAnonymousCount: $($report.allowAnonymousCount)"
Write-Host "unversionedControllerCount: $($report.unversionedControllerCount)"
Write-Host "todoCountInOrders: $($report.todoCountInOrders) | todoCountInSync: $($report.todoCountInSync) | FIXMECount: $($report.FIXMECount)"
Write-Host "governanceRiskCommentCount: $($report.governanceRiskCommentCount)"
Write-Host "governanceWarningLogCount: $($report.governanceWarningLogCount)"
Write-Host "placeholderProcessorCount: $($report.placeholderProcessorCount)"
Write-Host "syncReplayRiskCommentCount: $($report.syncReplayRiskCommentCount)"
Write-Host "explicitTransactionCount: $($report.explicitTransactionCount)"
Write-Host "moneyAffectingPlaceholderProcessorCount: $($report.moneyAffectingPlaceholderProcessorCount)"
Write-Host "explicitReplayWarningCount: $($report.explicitReplayWarningCount)"
Write-Host "OpenAPITrackedPathCount: $($report.OpenAPITrackedPathCount)"
Write-Host "concurrencyReadyEntityCount: $($report.concurrencyReadyEntityCount)"
Write-Host "concurrencyTokenEntityCount: $($report.concurrencyTokenEntityCount)"
Write-Host "concurrencyUpgradePlannedEntityCount: $($report.concurrencyUpgradePlannedEntityCount)"
Write-Host "moneyPathGovernanceAnchorCount: $($report.moneyPathGovernanceAnchorCount)"
Write-Host "replaySensitiveProcessorCount: $($report.replaySensitiveProcessorCount)"
Write-Host "transactionBoundaryAnchorCount: $($report.transactionBoundaryAnchorCount)"
Write-Host "moneyReplayRiskProcessorCount: $($report.moneyReplayRiskProcessorCount)"
Write-Host "moneyPathReplayRiskCount: $($report.moneyPathReplayRiskCount)"
Write-Host "missingDurableIdempotencyCommentCount: $($report.missingDurableIdempotencyCommentCount)"
Write-Host "reconciliationWarningCount: $($report.reconciliationWarningCount)"
Write-Host "partialBatchWarningCount: $($report.partialBatchWarningCount)"
Write-Host "idempotencyShortCircuitLogCount: $($report.idempotencyShortCircuitLogCount)"
Write-Host "knownNugetAutoMapperAdvisoryCount: $($report.knownNugetAutoMapperAdvisoryCount)"
Write-Host ''

$failed = $false
foreach ($r in $rules) {
    if ($r.Pass) {
        Write-Host "[PASS] $($r.Name)" -ForegroundColor Green
        Write-Host "       $($r.Detail)"
    }
    else {
        Write-Host "[FAIL] $($r.Name)" -ForegroundColor Red
        Write-Host "       $($r.Detail)"
        $failed = $true
    }
}

Write-Host ''
if ($failed) {
    Write-Host 'RESULT: FAIL (debt budget exceeded)' -ForegroundColor Red
    exit 1
}

Write-Host 'RESULT: PASS (within governance budget)' -ForegroundColor Green

# --- Optional non-failing trend warnings (growth visibility) ---
if (Test-Path $trendPath) {
    Write-Host ''
    Write-Host '=== Governance trend warnings (non-failing) ===' -ForegroundColor DarkCyan
    $trend = Read-JsonFile $trendPath
    foreach ($k in @('FIXMECount', 'governanceRiskCommentCount', 'placeholderProcessorCount')) {
        if (-not ($trend.PSObject.Properties.Name -contains $k)) { continue }
        $cur = [int]$report.($k)
        $base = [int]$trend.$k
        if ($cur -gt $base) {
            Write-Host "[WARN] $k growth: current=$cur > trend baseline=$base (update debt-warning-trend.json if intentional)." -ForegroundColor Yellow
        }
        else {
            Write-Host "[OK]   $k within trend baseline (current=$cur, baseline=$base)." -ForegroundColor DarkGray
        }
    }
    if ($null -ne $trend.governanceWarningLogCount) {
        $cw = [int]$report.governanceWarningLogCount
        $bw = [int]$trend.governanceWarningLogCount
        if ($cw -gt $bw) {
            Write-Host "[WARN] governanceWarningLogCount growth: current=$cw > trend baseline=$bw." -ForegroundColor Yellow
        }
        else {
            Write-Host "[OK]   governanceWarningLogCount within trend (current=$cw, baseline=$bw)." -ForegroundColor DarkGray
        }
    }
    if ($null -ne $trend.syncReplayRiskCommentCount) {
        $cs = [int]$report.syncReplayRiskCommentCount
        $bs = [int]$trend.syncReplayRiskCommentCount
        if ($cs -gt $bs) {
            Write-Host "[WARN] syncReplayRiskCommentCount growth: current=$cs > trend baseline=$bs." -ForegroundColor Yellow
        }
        else {
            Write-Host "[OK]   syncReplayRiskCommentCount within trend (current=$cs, baseline=$bs)." -ForegroundColor DarkGray
        }
    }
}
else {
    Write-Host ''
    Write-Host 'INFO: governance/debt-warning-trend.json not found; skipping non-failing trend warnings.' -ForegroundColor DarkGray
}

exit 0
