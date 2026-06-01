using Tannous.Pos.Application.OperationalAttention;
using Tannous.Pos.Application.OperationalBriefing;
using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Application.OperationalEquilibrium;
using Tannous.Pos.Application.OperationalStrategy;

namespace Tannous.Pos.Application.OperationalHandoff;

/// <summary>
/// Deterministic handoff continuity composition from bounded snapshot history.
/// Compares first vs. most recent snapshot per store. No health-ordering judgments.
/// </summary>
public static class OperationalHandoffAggregation
{
    public static OperationalHandoffContinuityDto ComposeHandoffContinuity(
        IReadOnlyList<OperationalEquilibriumSnapshot> equilibriumSnapshots,
        IReadOnlyList<OperationalStrategySnapshot> strategySnapshots,
        IReadOnlyList<OperationalAttentionSnapshot> attentionSnapshots,
        OperationalBriefingSummaryDto currentBriefing)
    {
        var now = DateTime.UtcNow;

        var eqTransition = ClassifyTransition(
            equilibriumSnapshots, s => s.EquilibriumState);
        var stTransition = ClassifyTransition(
            strategySnapshots, s => s.DominantOperationalPosture);
        var atTransition = ClassifyTransition(
            attentionSnapshots, s => s.DominantOperationalPriority);

        var eqFirst = equilibriumSnapshots.Count > 0 ? equilibriumSnapshots[0].EquilibriumState : default;
        var eqLast = equilibriumSnapshots.Count > 0 ? equilibriumSnapshots[^1].EquilibriumState : default;
        var stFirst = strategySnapshots.Count > 0 ? strategySnapshots[0].DominantOperationalPosture : default;
        var stLast = strategySnapshots.Count > 0 ? strategySnapshots[^1].DominantOperationalPosture : default;
        var atFirst = attentionSnapshots.Count > 0 ? attentionSnapshots[0].DominantOperationalPriority : default;
        var atLast = attentionSnapshots.Count > 0 ? attentionSnapshots[^1].DominantOperationalPriority : default;
        var urgencyArea = attentionSnapshots.Count > 0 ? attentionSnapshots[^1].HighestUrgencyArea : string.Empty;

        var totalCount = equilibriumSnapshots.Count + strategySnapshots.Count + attentionSnapshots.Count;

        var allTimestamps = equilibriumSnapshots.Select(s => s.GeneratedAtUtc)
            .Concat(strategySnapshots.Select(s => s.GeneratedAtUtc))
            .Concat(attentionSnapshots.Select(s => s.GeneratedAtUtc))
            .ToList();

        DateTime? windowStart = allTimestamps.Count > 0 ? allTimestamps.Min() : null;
        DateTime? windowEnd = allTimestamps.Count > 0 ? allTimestamps.Max() : null;
        double? windowMinutes = windowStart.HasValue && windowEnd.HasValue
            ? (windowEnd.Value - windowStart.Value).TotalMinutes
            : null;

        var age = currentBriefing.CognitionAge;

        var narrative = ComposeNarrative(
            eqTransition, stTransition, atTransition,
            eqFirst, eqLast, stFirst, stLast, atFirst, atLast,
            totalCount);

        return new OperationalHandoffContinuityDto
        {
            GeneratedAtUtc = now,
            CognitionAge = age,
            SnapshotWindowCount = totalCount,
            WindowStartUtc = windowStart,
            WindowEndUtc = windowEnd,
            WindowDurationMinutes = windowMinutes,
            EquilibriumTransition = eqTransition,
            EquilibriumAtWindowStart = eqFirst,
            EquilibriumAtWindowEnd = eqLast,
            StrategyTransition = stTransition,
            StrategyAtWindowStart = stFirst,
            StrategyAtWindowEnd = stLast,
            AttentionTransition = atTransition,
            AttentionAtWindowStart = atFirst,
            AttentionAtWindowEnd = atLast,
            HighestUrgencyArea = urgencyArea,
            CurrentBriefing = currentBriefing,
            HandoffNarrative = narrative
        };
    }

    public static OperationalHandoffSummaryDto ComposeHandoffSummary(
        IReadOnlyList<OperationalEquilibriumSnapshot> equilibriumSnapshots,
        IReadOnlyList<OperationalStrategySnapshot> strategySnapshots,
        IReadOnlyList<OperationalAttentionSnapshot> attentionSnapshots,
        OperationalBriefingSummaryDto currentBriefing)
    {
        var now = DateTime.UtcNow;
        var eqTransition = ClassifyTransition(equilibriumSnapshots, s => s.EquilibriumState);
        var stTransition = ClassifyTransition(strategySnapshots, s => s.DominantOperationalPosture);
        var atTransition = ClassifyTransition(attentionSnapshots, s => s.DominantOperationalPriority);
        var totalCount = equilibriumSnapshots.Count + strategySnapshots.Count + attentionSnapshots.Count;

        var eqFirst = equilibriumSnapshots.Count > 0 ? equilibriumSnapshots[0].EquilibriumState : default;
        var eqLast = equilibriumSnapshots.Count > 0 ? equilibriumSnapshots[^1].EquilibriumState : default;
        var stFirst = strategySnapshots.Count > 0 ? strategySnapshots[0].DominantOperationalPosture : default;
        var stLast = strategySnapshots.Count > 0 ? strategySnapshots[^1].DominantOperationalPosture : default;
        var atFirst = attentionSnapshots.Count > 0 ? attentionSnapshots[0].DominantOperationalPriority : default;
        var atLast = attentionSnapshots.Count > 0 ? attentionSnapshots[^1].DominantOperationalPriority : default;

        return new OperationalHandoffSummaryDto
        {
            GeneratedAtUtc = now,
            CognitionAge = currentBriefing.CognitionAge,
            EquilibriumTransition = eqTransition,
            StrategyTransition = stTransition,
            AttentionTransition = atTransition,
            SnapshotWindowCount = totalCount,
            CurrentBriefingSummary = currentBriefing.BriefingSummary,
            HandoffNarrative = ComposeNarrative(
                eqTransition, stTransition, atTransition,
                eqFirst, eqLast, stFirst, stLast, atFirst, atLast,
                totalCount)
        };
    }

    private static HandoffContinuityTransition ClassifyTransition<TSnapshot, TState>(
        IReadOnlyList<TSnapshot> snapshots,
        Func<TSnapshot, TState> stateSelector)
        where TState : struct
    {
        if (snapshots.Count < 2)
            return HandoffContinuityTransition.Insufficient;

        var first = stateSelector(snapshots[0]);
        var last = stateSelector(snapshots[^1]);
        return EqualityComparer<TState>.Default.Equals(first, last)
            ? HandoffContinuityTransition.Consistent
            : HandoffContinuityTransition.Shifted;
    }

    private static string ComposeNarrative(
        HandoffContinuityTransition eqTransition,
        HandoffContinuityTransition stTransition,
        HandoffContinuityTransition atTransition,
        OperationalEquilibriumState eqFirst,
        OperationalEquilibriumState eqLast,
        OperationalStrategicPostureType stFirst,
        OperationalStrategicPostureType stLast,
        OperationalPriorityType atFirst,
        OperationalPriorityType atLast,
        int totalSnapshotCount)
    {
        if (totalSnapshotCount == 0)
            return "No cognition data in bounded window — call cognition APIs before shift handoff";

        var parts = new List<string>(3);

        parts.Add(eqTransition switch
        {
            HandoffContinuityTransition.Consistent =>
                OperationalContinuityPhrasing.ConsistentAcrossBoundedWindow("Equilibrium"),
            HandoffContinuityTransition.Shifted =>
                OperationalContinuityPhrasing.StateShift("Equilibrium", eqFirst.ToString(), eqLast.ToString()),
            _ => "Equilibrium continuity insufficient"
        });

        parts.Add(stTransition switch
        {
            HandoffContinuityTransition.Consistent =>
                OperationalContinuityPhrasing.ConsistentAcrossBoundedWindow("Strategy"),
            HandoffContinuityTransition.Shifted =>
                OperationalContinuityPhrasing.MovedFromTo("Strategy", stFirst.ToString(), stLast.ToString()),
            _ => "Strategy continuity insufficient"
        });

        parts.Add(atTransition switch
        {
            HandoffContinuityTransition.Consistent =>
                OperationalContinuityPhrasing.ConsistentAcrossBoundedWindow("Attention"),
            HandoffContinuityTransition.Shifted =>
                OperationalContinuityPhrasing.StateShift("Attention", atFirst.ToString(), atLast.ToString()),
            _ => "Attention continuity insufficient"
        });

        return string.Join("; ", parts);
    }
}
