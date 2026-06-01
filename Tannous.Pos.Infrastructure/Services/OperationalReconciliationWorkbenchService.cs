using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator-facing reconciliation workbench via shared operational composition hub (read-only; no nested workbench calls).
/// </summary>
public sealed class OperationalReconciliationWorkbenchService : IOperationalReconciliationWorkbenchService
{
    private readonly IOperationalReadCompositionHub _compositionHub;
    private readonly ILogger<OperationalReconciliationWorkbenchService> _logger;

    public OperationalReconciliationWorkbenchService(
        IOperationalReadCompositionHub compositionHub,
        ILogger<OperationalReconciliationWorkbenchService> logger)
    {
        _compositionHub = compositionHub;
        _logger = logger;
    }

    public async Task<OperationalReconciliationWorkbenchDto> GetReconciliationWorkbenchAsync(
        CancellationToken cancellationToken = default)
    {
        var workbench = await _compositionHub
            .GetReconciliationWorkbenchViewAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Operational workbench observability: reconciliation workbench composed. ActiveConflicts={ActiveConflicts}, Hotspots={Hotspots}, AttentionItems={AttentionItems}, ReplayInstability={ReplayInstability}",
            workbench.Queue.ActiveConflicts,
            workbench.Hotspots.Count,
            workbench.AttentionItems.Count,
            workbench.ReplayRisk.InstabilityLevel);

        return workbench;
    }
}
