using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator-facing dashboard aggregation from shared operational composition hub (read-only; no persistence).
/// </summary>
public sealed class OperationalDashboardService : IOperationalDashboardService
{
    private readonly IOperationalReadCompositionHub _compositionHub;
    private readonly ILogger<OperationalDashboardService> _logger;

    public OperationalDashboardService(
        IOperationalReadCompositionHub compositionHub,
        ILogger<OperationalDashboardService> logger)
    {
        _compositionHub = compositionHub;
        _logger = logger;
    }

    public async Task<OperationalDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _compositionHub.GetDashboardSummaryAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Operational dashboard observability: dashboard summary composed. Health={Health}, Risk={Risk}, Alerts={Alerts}, UnresolvedConflicts={UnresolvedConflicts}",
            summary.Health.State,
            summary.Risk.Level,
            summary.Activity.ActiveAlertCount,
            summary.Activity.UnresolvedReconciliationCount);

        return summary;
    }
}
