using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalWorkbench;
using Tannous.Pos.Application.Sync;

namespace Tannous.Pos.Application.OperationalComposition;

/// <summary>Memoized upstream operational summaries for a single request scope.</summary>
public sealed class OperationalReadCompositionSnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalResilienceSummaryDto Resilience { get; init; } = new();
    public ReconciliationSummaryDto Reconciliation { get; init; } = new();
    public OperationalIncidentSummaryDto Incidents { get; init; } = new();
    public OperationalAlertSummaryDto Alerts { get; init; } = new();
    public OperationalCacheGovernanceOverviewDto GovernanceOverview { get; init; } = new();
    public OperationalGovernanceRuntimeProtectionSnapshot RuntimeProtection { get; init; } = new();
    public OperationalGovernanceFingerprintSnapshot Fingerprint { get; init; } = new();
    public OperationalDashboardSummaryDto? Dashboard { get; init; }
    public OperationalReconciliationWorkbenchDto? ReconciliationWorkbench { get; init; }
    public OperationalInventoryWorkbenchDto? InventoryWorkbench { get; init; }
    public OperationalReadCompositionContext CompositionContext { get; init; } = new();
}

/// <summary>Operator-facing runtime protection signals (no governance pipeline exposure).</summary>
public sealed class OperationalGovernanceRuntimeProtectionSnapshot
{
    public bool FailsafeActive { get; init; }
    public string TelemetrySaturationLevel { get; init; } = string.Empty;
    public string ReadinessState { get; init; } = string.Empty;
}

/// <summary>Operator-facing governance fingerprint signals (no governance pipeline exposure).</summary>
public sealed class OperationalGovernanceFingerprintSnapshot
{
    public bool FingerprintChanged { get; init; }
    public bool HasPreviousFingerprint { get; init; }
    public string FingerprintHash { get; init; } = string.Empty;
    public string FingerprintStability { get; init; } = string.Empty;
}
