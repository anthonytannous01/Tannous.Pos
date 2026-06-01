using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalInventoryWorkbench;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator-facing inventory drift workbench via shared operational composition hub (read-only; no nested workbench calls).
/// </summary>
public sealed class OperationalInventoryWorkbenchService : IOperationalInventoryWorkbenchService
{
    private readonly IOperationalReadCompositionHub _compositionHub;
    private readonly ILogger<OperationalInventoryWorkbenchService> _logger;

    public OperationalInventoryWorkbenchService(
        IOperationalReadCompositionHub compositionHub,
        ILogger<OperationalInventoryWorkbenchService> logger)
    {
        _compositionHub = compositionHub;
        _logger = logger;
    }

    public async Task<OperationalInventoryWorkbenchDto> GetDriftWorkbenchAsync(
        CancellationToken cancellationToken = default)
    {
        var workbench = await _compositionHub.GetInventoryWorkbenchViewAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Operational inventory workbench observability: drift workbench composed. TotalDrift={TotalDrift}, Hotspots={Hotspots}, AttentionItems={AttentionItems}, ResolutionState={ResolutionState}",
            workbench.DriftSummary.TotalInventoryDriftConflicts,
            workbench.Hotspots.Count,
            workbench.AttentionItems.Count,
            workbench.ResolutionReadiness.ResolutionState);

        return workbench;
    }
}
