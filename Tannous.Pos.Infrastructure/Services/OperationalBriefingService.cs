using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalAttention;
using Tannous.Pos.Application.OperationalBriefing;
using Tannous.Pos.Application.OperationalEquilibrium;
using Tannous.Pos.Application.OperationalStrategy;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator briefing from existing top-level cognition snapshot stores.
/// No service injection — reads stored snapshots only. No recomputation triggered.
/// </summary>
public sealed class OperationalBriefingService : IOperationalBriefingService
{
    private readonly IOperationalEquilibriumSnapshotStore _equilibriumStore;
    private readonly IOperationalStrategySnapshotStore _strategyStore;
    private readonly IOperationalAttentionSnapshotStore _attentionStore;
    private readonly ILogger<OperationalBriefingService> _logger;

    public OperationalBriefingService(
        IOperationalEquilibriumSnapshotStore equilibriumStore,
        IOperationalStrategySnapshotStore strategyStore,
        IOperationalAttentionSnapshotStore attentionStore,
        ILogger<OperationalBriefingService> logger)
    {
        _equilibriumStore = equilibriumStore;
        _strategyStore = strategyStore;
        _attentionStore = attentionStore;
        _logger = logger;
    }

    public Task<OperationalBriefingPackageDto> GetBriefingPackageAsync(CancellationToken cancellationToken = default)
    {
        var equilibrium = _equilibriumStore.GetSnapshots().LastOrDefault();
        var strategy = _strategyStore.GetSnapshots().LastOrDefault();
        var attention = _attentionStore.GetSnapshots().LastOrDefault();

        var briefing = OperationalBriefingAggregation.ComposeBriefingPackage(
            equilibrium, strategy, attention);

        _logger.LogInformation(
            "Operational briefing observability: package composed. BriefingId={BriefingId}, CognitionAge={CognitionAge}, AvailableSourceCount={AvailableSourceCount}, SystemicBalance={SystemicBalance}",
            briefing.BriefingId,
            briefing.CognitionAge,
            briefing.AvailableSourceCount,
            briefing.SystemicBalance);

        return Task.FromResult(briefing);
    }

    public Task<OperationalBriefingSummaryDto> GetBriefingSummaryAsync(CancellationToken cancellationToken = default)
    {
        var equilibrium = _equilibriumStore.GetSnapshots().LastOrDefault();
        var strategy = _strategyStore.GetSnapshots().LastOrDefault();
        var attention = _attentionStore.GetSnapshots().LastOrDefault();

        var summary = OperationalBriefingAggregation.ComposeBriefingSummary(
            equilibrium, strategy, attention);

        _logger.LogInformation(
            "Operational briefing observability: summary composed. BriefingId={BriefingId}, CognitionAge={CognitionAge}, SystemicBalance={SystemicBalance}",
            summary.BriefingId,
            summary.CognitionAge,
            summary.SystemicBalance);

        return Task.FromResult(summary);
    }
}
