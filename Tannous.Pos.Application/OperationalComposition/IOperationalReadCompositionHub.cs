using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalWorkbench;
using Tannous.Pos.Application.Sync;

namespace Tannous.Pos.Application.OperationalComposition;

/// <summary>Request-scoped hub for memoized operational read composition (no persistence; no new cache layer).</summary>
public interface IOperationalReadCompositionHub
{
    OperationalReadCompositionContext Context { get; }

    Task<OperationalResilienceSummaryDto> GetResilienceSummaryAsync(CancellationToken cancellationToken = default);

    Task<ReconciliationSummaryDto> GetReconciliationSummaryAsync(CancellationToken cancellationToken = default);

    Task<OperationalIncidentSummaryDto> GetIncidentsSummaryAsync(CancellationToken cancellationToken = default);

    Task<OperationalAlertSummaryDto> GetAlertSummaryAsync(CancellationToken cancellationToken = default);

    Task<OperationalCacheGovernanceOverviewDto> GetGovernanceOverviewAsync(CancellationToken cancellationToken = default);

    Task<OperationalGovernanceRuntimeProtectionSnapshot> GetRuntimeProtectionSnapshotAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalGovernanceFingerprintSnapshot> GetFingerprintSnapshotAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);

    Task<OperationalReconciliationWorkbenchDto> GetReconciliationWorkbenchViewAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalInventoryWorkbenchDto> GetInventoryWorkbenchViewAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalReadCompositionSnapshot> BuildSnapshotAsync(CancellationToken cancellationToken = default);
}
