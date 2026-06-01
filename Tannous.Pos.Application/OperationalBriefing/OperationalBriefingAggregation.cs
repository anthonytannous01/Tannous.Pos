using Tannous.Pos.Application.OperationalAttention;
using Tannous.Pos.Application.OperationalEquilibrium;
using Tannous.Pos.Application.OperationalStrategy;

namespace Tannous.Pos.Application.OperationalBriefing;

/// <summary>
/// Deterministic briefing composition from top-level cognition snapshots.
/// No computation triggered — projects from existing stored snapshots only.
/// </summary>
public static class OperationalBriefingAggregation
{
    private const double FreshThresholdMinutes = 5.0;
    private const double WarmThresholdMinutes = 30.0;

    public static OperationalBriefingPackageDto ComposeBriefingPackage(
        OperationalEquilibriumSnapshot? equilibrium,
        OperationalStrategySnapshot? strategy,
        OperationalAttentionSnapshot? attention)
    {
        var now = DateTime.UtcNow;
        var age = ClassifyAge(now, equilibrium?.GeneratedAtUtc, strategy?.GeneratedAtUtc, attention?.GeneratedAtUtc);
        var sourceCount = CountSources(equilibrium, strategy, attention);
        var oldestAge = ComputeOldestAgeMinutes(now, equilibrium?.GeneratedAtUtc, strategy?.GeneratedAtUtc, attention?.GeneratedAtUtc);

        return new OperationalBriefingPackageDto
        {
            GeneratedAtUtc = now,
            CognitionAge = age,
            AvailableSourceCount = sourceCount,
            OldestSourceAgeMinutes = oldestAge,

            SystemicBalance = equilibrium?.EquilibriumState ?? default,
            SystemicStrainLevel = equilibrium?.SystemicStrainLevel ?? default,
            HighestImbalanceArea = equilibrium?.HighestImbalanceArea ?? string.Empty,
            ImbalanceCount = equilibrium?.ImbalanceCount ?? 0,

            StrategicPosture = strategy?.DominantOperationalPosture ?? default,
            OperationalAlignment = strategy?.OperationalAlignmentStrength ?? default,
            StrategicFocus = strategy?.DominantStrategicFocus ?? string.Empty,

            DominantPriority = attention?.DominantOperationalPriority ?? default,
            AttentionPressure = attention?.AttentionPressureLevel ?? default,
            HighestUrgencyArea = attention?.HighestUrgencyArea ?? string.Empty,
            PriorityCount = attention?.PriorityCount ?? 0,

            BriefingSummary = ComposeSummaryLine(age, equilibrium, strategy, attention)
        };
    }

    public static OperationalBriefingSummaryDto ComposeBriefingSummary(
        OperationalEquilibriumSnapshot? equilibrium,
        OperationalStrategySnapshot? strategy,
        OperationalAttentionSnapshot? attention)
    {
        var now = DateTime.UtcNow;
        var age = ClassifyAge(now, equilibrium?.GeneratedAtUtc, strategy?.GeneratedAtUtc, attention?.GeneratedAtUtc);

        return new OperationalBriefingSummaryDto
        {
            GeneratedAtUtc = now,
            CognitionAge = age,
            SystemicBalance = equilibrium?.EquilibriumState ?? default,
            StrategicPosture = strategy?.DominantOperationalPosture ?? default,
            DominantPriority = attention?.DominantOperationalPriority ?? default,
            HighestUrgencyArea = attention?.HighestUrgencyArea ?? string.Empty,
            BriefingSummary = ComposeSummaryLine(age, equilibrium, strategy, attention)
        };
    }

    private static string ComposeSummaryLine(
        BriefingCognitionAge age,
        OperationalEquilibriumSnapshot? equilibrium,
        OperationalStrategySnapshot? strategy,
        OperationalAttentionSnapshot? attention)
    {
        if (age == BriefingCognitionAge.NoData)
            return "No cognition data available — operational cognition APIs not yet called in this session";

        var parts = new List<string>(3);

        if (equilibrium is not null)
            parts.Add($"Balance: {equilibrium.EquilibriumState}");

        if (strategy is not null)
            parts.Add($"Posture: {strategy.DominantOperationalPosture}");

        if (attention is not null)
            parts.Add($"Priority: {attention.HighestUrgencyArea}");

        return parts.Count > 0
            ? string.Join(" | ", parts)
            : "Briefing data partially available";
    }

    private static BriefingCognitionAge ClassifyAge(
        DateTime now,
        DateTime? equilibriumAt,
        DateTime? strategyAt,
        DateTime? attentionAt)
    {
        var newest = new[] { equilibriumAt, strategyAt, attentionAt }
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .DefaultIfEmpty()
            .Max();

        if (newest == default)
            return BriefingCognitionAge.NoData;

        var ageMinutes = (now - newest).TotalMinutes;

        return ageMinutes < FreshThresholdMinutes
            ? BriefingCognitionAge.Fresh
            : ageMinutes < WarmThresholdMinutes
                ? BriefingCognitionAge.Warm
                : BriefingCognitionAge.Stale;
    }

    private static int CountSources(
        OperationalEquilibriumSnapshot? e,
        OperationalStrategySnapshot? s,
        OperationalAttentionSnapshot? a)
        => (e is not null ? 1 : 0) + (s is not null ? 1 : 0) + (a is not null ? 1 : 0);

    private static double? ComputeOldestAgeMinutes(
        DateTime now,
        DateTime? equilibriumAt,
        DateTime? strategyAt,
        DateTime? attentionAt)
    {
        var timestamps = new[] { equilibriumAt, strategyAt, attentionAt }
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        if (timestamps.Count == 0)
            return null;

        return (now - timestamps.Min()).TotalMinutes;
    }
}
