using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.OperationalAttention;
using Tannous.Pos.Application.OperationalBriefing;
using Tannous.Pos.Application.OperationalEquilibrium;
using Tannous.Pos.Application.OperationalHandoff;
using Tannous.Pos.Application.OperationalStrategy;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Operator handoff from bounded FIFO snapshot history + current briefing.
/// Reads full snapshot history (all stored snapshots, not just latest).
/// No cognition services injected — stores and briefing service only.
/// </summary>
public sealed class OperationalHandoffService : IOperationalHandoffService
{
    private readonly IOperationalEquilibriumSnapshotStore _equilibriumStore;
    private readonly IOperationalStrategySnapshotStore _strategyStore;
    private readonly IOperationalAttentionSnapshotStore _attentionStore;
    private readonly IOperationalBriefingService _briefingService;
    private readonly ILogger<OperationalHandoffService> _logger;

    public OperationalHandoffService(
        IOperationalEquilibriumSnapshotStore equilibriumStore,
        IOperationalStrategySnapshotStore strategyStore,
        IOperationalAttentionSnapshotStore attentionStore,
        IOperationalBriefingService briefingService,
        ILogger<OperationalHandoffService> logger)
    {
        _equilibriumStore = equilibriumStore;
        _strategyStore = strategyStore;
        _attentionStore = attentionStore;
        _briefingService = briefingService;
        _logger = logger;
    }

    public async Task<OperationalHandoffContinuityDto> GetHandoffContinuityAsync(
        CancellationToken cancellationToken = default)
    {
        var equilibriumSnapshots = _equilibriumStore.GetSnapshots();
        var strategySnapshots = _strategyStore.GetSnapshots();
        var attentionSnapshots = _attentionStore.GetSnapshots();
        var currentBriefing = await _briefingService
            .GetBriefingSummaryAsync(cancellationToken).ConfigureAwait(false);

        var continuity = OperationalHandoffAggregation.ComposeHandoffContinuity(
            equilibriumSnapshots, strategySnapshots, attentionSnapshots, currentBriefing);

        _logger.LogInformation(
            "Operational handoff observability: continuity composed. HandoffId={HandoffId}, CognitionAge={CognitionAge}, SnapshotWindowCount={SnapshotWindowCount}, EquilibriumTransition={EquilibriumTransition}, StrategyTransition={StrategyTransition}, AttentionTransition={AttentionTransition}",
            continuity.HandoffId,
            continuity.CognitionAge,
            continuity.SnapshotWindowCount,
            continuity.EquilibriumTransition,
            continuity.StrategyTransition,
            continuity.AttentionTransition);

        return continuity;
    }

    public async Task<OperationalHandoffSummaryDto> GetHandoffSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var equilibriumSnapshots = _equilibriumStore.GetSnapshots();
        var strategySnapshots = _strategyStore.GetSnapshots();
        var attentionSnapshots = _attentionStore.GetSnapshots();
        var currentBriefing = await _briefingService
            .GetBriefingSummaryAsync(cancellationToken).ConfigureAwait(false);

        var summary = OperationalHandoffAggregation.ComposeHandoffSummary(
            equilibriumSnapshots, strategySnapshots, attentionSnapshots, currentBriefing);

        _logger.LogInformation(
            "Operational handoff observability: summary composed. HandoffId={HandoffId}, CognitionAge={CognitionAge}, SnapshotWindowCount={SnapshotWindowCount}",
            summary.HandoffId,
            summary.CognitionAge,
            summary.SnapshotWindowCount);

        return summary;
    }
}
