using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Application.OperationalTrends;

/// <summary>Deterministic bounded trend comparison from operational summaries (heuristic only; no forecasting).</summary>
public static class OperationalTrendAggregation
{
    public const int MaxWindowSnapshots = 3;
    public const int MaxAttentionItems = 6;
    public const int MaxDeltaSignals = 5;

    public const string SignalPressureEscalation = "PressureEscalation";
    public const string SignalPressureRecovery = "PressureRecovery";
    public const string SignalDriftIncrease = "DriftIncrease";
    public const string SignalDriftDecrease = "DriftDecrease";
    public const string SignalReplayStabilization = "ReplayStabilization";
    public const string SignalReplayEscalation = "ReplayEscalation";
    public const string SignalReadinessImprovement = "ReadinessImprovement";
    public const string SignalReadinessDecline = "ReadinessDecline";
    public const string SignalFingerprintInstability = "FingerprintInstability";
    public const string SignalOperationalStable = "OperationalStable";

    public static OperationalTrendSnapshot BuildSnapshot(
        OperationalGovernanceFingerprintSnapshot fingerprint,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        OperationalDashboardSummaryDto dashboard,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench)
    {
        return new OperationalTrendSnapshot
        {
            CapturedAtUtc = DateTime.UtcNow,
            FingerprintId = NormalizeFingerprintId(fingerprint.FingerprintHash),
            FingerprintStability = NormalizeLabel(fingerprint.FingerprintStability),
            ReadinessState = NormalizeLabel(runtimeProtection.ReadinessState),
            PressureBand = ClassifyPressureBand(dashboard, replayPressure),
            HealthState = dashboard.Health.State.ToString(),
            UnresolvedReconciliationCount = dashboard.Activity.UnresolvedReconciliationCount,
            InventoryDriftConflictCount = inventoryWorkbench.DriftSummary.TotalInventoryDriftConflicts,
            ActiveReplayPressure = replayPressure.ActiveReplayPressure,
            ReplayInstabilityLevel = replayPressure.InstabilityLevel.ToString(),
            ProtectiveModeActive = dashboard.Pressure.ProtectiveModeActive
                || replayPressure.ProtectiveModeVisible
                || inventoryWorkbench.DriftSummary.ProtectiveModeActive,
            ActiveAlertCount = dashboard.Activity.ActiveAlertCount,
            ReplayStabilizationActive = replayStabilization.StabilizationActive,
            EscalatingConflictCount = reconciliationWorkbench.Queue.EscalatingConflicts
        };
    }

    public static OperationalTrendSummaryDto ComposeSummary(
        OperationalTrendSnapshot current,
        IReadOnlyList<OperationalTrendSnapshot> priorSnapshots)
    {
        var prior = priorSnapshots.Count > 0 ? priorSnapshots[^1] : null;
        var delta = prior is null
            ? null
            : CompareSnapshots(current, prior);
        var attentionItems = ComposeAttentionItems(current, prior, delta);
        var overallDirection = delta?.OverallDirection ?? OperationalTrendDirection.Stable;
        var severity = delta?.Severity ?? ClassifySnapshotSeverity(current);

        return new OperationalTrendSummaryDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            OverallDirection = overallDirection,
            Severity = severity,
            Summary = DescribeOverallSummary(current, prior, overallDirection),
            Window = ComposeWindow(priorSnapshots.Count + 1, prior is not null),
            AttentionItems = attentionItems
        };
    }

    public static IReadOnlyList<OperationalTrendDeltaDto> ComposeDeltas(
        OperationalTrendSnapshot current,
        IReadOnlyList<OperationalTrendSnapshot> priorSnapshots)
    {
        if (priorSnapshots.Count == 0)
            return Array.Empty<OperationalTrendDeltaDto>();

        return priorSnapshots
            .OrderByDescending(s => s.CapturedAtUtc)
            .Select(prior => CompareSnapshots(current, prior))
            .ToList();
    }

    public static OperationalTrendWindowDto ComposeWindow(int snapshotCount, bool hasBaseline) =>
        new()
        {
            SnapshotCount = Math.Min(snapshotCount, MaxWindowSnapshots),
            MaxSnapshots = MaxWindowSnapshots,
            HasComparisonBaseline = hasBaseline
        };

    public static OperationalTrendDeltaDto CompareSnapshots(
        OperationalTrendSnapshot current,
        OperationalTrendSnapshot prior)
    {
        var signals = ComposeMovementSignals(current, prior);
        var direction = ClassifyDirection(current, prior, signals);
        var severity = ClassifyDeltaSeverity(current, prior, direction);

        return new OperationalTrendDeltaDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ComparedToUtc = prior.CapturedAtUtc,
            OverallDirection = direction,
            Severity = severity,
            Summary = DescribeDeltaSummary(current, prior, direction, signals),
            MovementSignals = signals
        };
    }

    public static IReadOnlyList<OperationalTrendAttentionDto> ComposeAttentionItems(
        OperationalTrendSnapshot current,
        OperationalTrendSnapshot? prior,
        OperationalTrendDeltaDto? latestDelta)
    {
        var items = new List<(int Priority, OperationalTrendAttentionDto Item)>();

        if (latestDelta is not null)
        {
            if (latestDelta.OverallDirection == OperationalTrendDirection.Degrading
                && latestDelta.MovementSignals.Contains(SignalPressureEscalation, StringComparer.Ordinal))
            {
                items.Add((1, new OperationalTrendAttentionDto
                {
                    Priority = 1,
                    Severity = OperationalTrendSeverity.High,
                    Direction = OperationalTrendDirection.Degrading,
                    Title = "Operational pressure increasing",
                    Detail = "Replay pressure and protective indicators are trending upward compared to the prior snapshot."
                }));
            }

            if (latestDelta.OverallDirection == OperationalTrendDirection.Improving
                && latestDelta.MovementSignals.Contains(SignalPressureRecovery, StringComparer.Ordinal))
            {
                items.Add((2, new OperationalTrendAttentionDto
                {
                    Priority = 2,
                    Severity = OperationalTrendSeverity.Moderate,
                    Direction = OperationalTrendDirection.Improving,
                    Title = "System stabilization improving",
                    Detail = "Conflict and replay pressure indicators are easing with stable runtime protection."
                }));
            }

            if (latestDelta.MovementSignals.Contains(SignalDriftIncrease, StringComparer.Ordinal))
            {
                items.Add((3, new OperationalTrendAttentionDto
                {
                    Priority = 3,
                    Severity = OperationalTrendSeverity.Elevated,
                    Direction = OperationalTrendDirection.Degrading,
                    Title = "Inventory reconciliation risk increasing",
                    Detail = "Inventory drift conflicts are rising relative to the prior short-window snapshot."
                }));
            }

            if (latestDelta.MovementSignals.Contains(SignalReplayStabilization, StringComparer.Ordinal))
            {
                items.Add((4, new OperationalTrendAttentionDto
                {
                    Priority = 4,
                    Severity = OperationalTrendSeverity.Moderate,
                    Direction = OperationalTrendDirection.Improving,
                    Title = "Replay stabilization progressing",
                    Detail = "Replay stabilization signals are improving compared to the prior snapshot."
                }));
            }

            if (latestDelta.MovementSignals.Contains(SignalFingerprintInstability, StringComparer.Ordinal))
            {
                items.Add((5, new OperationalTrendAttentionDto
                {
                    Priority = 5,
                    Severity = OperationalTrendSeverity.Elevated,
                    Direction = OperationalTrendDirection.Degrading,
                    Title = "Operational fingerprint shifting",
                    Detail = "Fingerprint stability is weakening, indicating changing operational conditions."
                }));
            }
        }

        if (current.ProtectiveModeActive)
        {
            items.Add((6, new OperationalTrendAttentionDto
            {
                Priority = 6,
                Severity = OperationalTrendSeverity.High,
                Direction = OperationalTrendDirection.Degrading,
                Title = "Protective mode active",
                Detail = "Runtime protection or protective containment is currently active."
            }));
        }

        if (prior is not null
            && latestDelta?.OverallDirection == OperationalTrendDirection.Stable
            && items.Count == 0)
        {
            items.Add((20, new OperationalTrendAttentionDto
            {
                Priority = 20,
                Severity = OperationalTrendSeverity.Nominal,
                Direction = OperationalTrendDirection.Stable,
                Title = "Operational state stable",
                Detail = "Short-window indicators remain consistent with the prior snapshot."
            }));
        }

        if (prior is null)
        {
            items.Add((30, new OperationalTrendAttentionDto
            {
                Priority = 30,
                Severity = ClassifySnapshotSeverity(current),
                Direction = OperationalTrendDirection.Stable,
                Title = "Trend baseline captured",
                Detail = "Initial short-window snapshot recorded for future comparison within this process."
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Item.Title, StringComparer.Ordinal)
            .Take(MaxAttentionItems)
            .Select(i => i.Item)
            .ToList();
    }

    private static IReadOnlyList<string> ComposeMovementSignals(
        OperationalTrendSnapshot current,
        OperationalTrendSnapshot prior)
    {
        var signals = new List<string>();

        if (current.ActiveReplayPressure > prior.ActiveReplayPressure
            || RankPressureBand(current.PressureBand) > RankPressureBand(prior.PressureBand))
        {
            signals.Add(SignalPressureEscalation);
        }

        if (current.ActiveReplayPressure < prior.ActiveReplayPressure
            && RankPressureBand(current.PressureBand) <= RankPressureBand(prior.PressureBand))
        {
            signals.Add(SignalPressureRecovery);
        }

        if (current.InventoryDriftConflictCount > prior.InventoryDriftConflictCount)
            signals.Add(SignalDriftIncrease);

        if (current.InventoryDriftConflictCount < prior.InventoryDriftConflictCount)
            signals.Add(SignalDriftDecrease);

        if (current.ReplayStabilizationActive && !prior.ReplayStabilizationActive)
            signals.Add(SignalReplayStabilization);

        if (RankReplayInstability(current.ReplayInstabilityLevel) > RankReplayInstability(prior.ReplayInstabilityLevel))
            signals.Add(SignalReplayEscalation);

        if (RankReadiness(current.ReadinessState) > RankReadiness(prior.ReadinessState))
            signals.Add(SignalReadinessImprovement);

        if (RankReadiness(current.ReadinessState) < RankReadiness(prior.ReadinessState))
            signals.Add(SignalReadinessDecline);

        if (!string.Equals(current.FingerprintStability, prior.FingerprintStability, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(current.FingerprintId)
                && !string.Equals(current.FingerprintId, prior.FingerprintId, StringComparison.Ordinal)))
        {
            signals.Add(SignalFingerprintInstability);
        }

        if (signals.Count == 0
            && current.UnresolvedReconciliationCount == prior.UnresolvedReconciliationCount
            && current.InventoryDriftConflictCount == prior.InventoryDriftConflictCount
            && current.ActiveReplayPressure == prior.ActiveReplayPressure
            && current.ProtectiveModeActive == prior.ProtectiveModeActive)
        {
            signals.Add(SignalOperationalStable);
        }

        return signals
            .Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .Take(MaxDeltaSignals)
            .ToList();
    }

    private static OperationalTrendDirection ClassifyDirection(
        OperationalTrendSnapshot current,
        OperationalTrendSnapshot prior,
        IReadOnlyList<string> signals)
    {
        var degradingScore = 0;
        var improvingScore = 0;

        if (signals.Contains(SignalPressureEscalation, StringComparer.Ordinal)) degradingScore += 2;
        if (signals.Contains(SignalDriftIncrease, StringComparer.Ordinal)) degradingScore += 2;
        if (signals.Contains(SignalReplayEscalation, StringComparer.Ordinal)) degradingScore += 2;
        if (signals.Contains(SignalFingerprintInstability, StringComparer.Ordinal)) degradingScore += 1;
        if (signals.Contains(SignalReadinessDecline, StringComparer.Ordinal)) degradingScore += 1;
        if (current.ProtectiveModeActive && !prior.ProtectiveModeActive) degradingScore += 2;

        if (signals.Contains(SignalPressureRecovery, StringComparer.Ordinal)) improvingScore += 2;
        if (signals.Contains(SignalDriftDecrease, StringComparer.Ordinal)) improvingScore += 2;
        if (signals.Contains(SignalReplayStabilization, StringComparer.Ordinal)) improvingScore += 2;
        if (signals.Contains(SignalReadinessImprovement, StringComparer.Ordinal)) improvingScore += 1;
        if (!current.ProtectiveModeActive && prior.ProtectiveModeActive) improvingScore += 2;

        if (current.UnresolvedReconciliationCount < prior.UnresolvedReconciliationCount) improvingScore += 1;
        if (current.UnresolvedReconciliationCount > prior.UnresolvedReconciliationCount) degradingScore += 1;

        if (degradingScore > improvingScore)
            return OperationalTrendDirection.Degrading;
        if (improvingScore > degradingScore)
            return OperationalTrendDirection.Improving;
        return OperationalTrendDirection.Stable;
    }

    private static OperationalTrendSeverity ClassifySnapshotSeverity(OperationalTrendSnapshot snapshot)
    {
        if (snapshot.ProtectiveModeActive || snapshot.ActiveReplayPressure >= 6)
            return OperationalTrendSeverity.Critical;
        if (snapshot.ActiveReplayPressure >= 4 || snapshot.InventoryDriftConflictCount >= 4)
            return OperationalTrendSeverity.High;
        if (snapshot.ActiveReplayPressure >= 2 || snapshot.UnresolvedReconciliationCount >= 3)
            return OperationalTrendSeverity.Elevated;
        if (snapshot.ActiveAlertCount > 0 || snapshot.UnresolvedReconciliationCount > 0)
            return OperationalTrendSeverity.Moderate;
        return OperationalTrendSeverity.Nominal;
    }

    private static OperationalTrendSeverity ClassifyDeltaSeverity(
        OperationalTrendSnapshot current,
        OperationalTrendSnapshot prior,
        OperationalTrendDirection direction)
    {
        var baseSeverity = ClassifySnapshotSeverity(current);
        if (direction == OperationalTrendDirection.Degrading && baseSeverity < OperationalTrendSeverity.Elevated)
            return (OperationalTrendSeverity)Math.Min((int)OperationalTrendSeverity.High, (int)baseSeverity + 1);
        if (direction == OperationalTrendDirection.Improving && baseSeverity > OperationalTrendSeverity.Nominal)
            return (OperationalTrendSeverity)Math.Max((int)OperationalTrendSeverity.Nominal, (int)baseSeverity - 1);
        return baseSeverity;
    }

    private static string ClassifyPressureBand(
        OperationalDashboardSummaryDto dashboard,
        OperationalReplayPressureSummaryDto replayPressure)
    {
        if (dashboard.Pressure.ProtectiveModeActive
            || replayPressure.InstabilityLevel == OperationalReplayPressureLevel.Critical)
            return "Critical";
        if (replayPressure.InstabilityLevel == OperationalReplayPressureLevel.High
            || dashboard.Pressure.ReplayStormRiskIndicated)
            return "High";
        if (replayPressure.InstabilityLevel == OperationalReplayPressureLevel.Elevated
            || dashboard.Pressure.ExportPressureIndicated)
            return "Elevated";
        if (replayPressure.ActiveReplayPressure > 0 || dashboard.Pressure.QueryPressureIndicated)
            return "Moderate";
        return "Nominal";
    }

    private static string DescribeOverallSummary(
        OperationalTrendSnapshot current,
        OperationalTrendSnapshot? prior,
        OperationalTrendDirection direction)
    {
        if (prior is null)
            return "Initial short-window operational snapshot captured for in-process trend comparison.";

        return direction switch
        {
            OperationalTrendDirection.Degrading when current.ActiveReplayPressure > prior.ActiveReplayPressure =>
                "Operational pressure increasing compared to the prior short-window snapshot.",
            OperationalTrendDirection.Improving when current.UnresolvedReconciliationCount < prior.UnresolvedReconciliationCount =>
                "System stabilization improving with reduced reconciliation backlog.",
            OperationalTrendDirection.Degrading when current.InventoryDriftConflictCount > prior.InventoryDriftConflictCount =>
                "Inventory reconciliation risk increasing relative to the prior snapshot.",
            OperationalTrendDirection.Stable =>
                "Operational state stable across the short-window comparison.",
            OperationalTrendDirection.Improving =>
                "Operational indicators are trending toward stabilization.",
            OperationalTrendDirection.Degrading =>
                "Operational indicators are trending toward increased pressure.",
            _ => "Short-window operational trend comparison available."
        };
    }

    private static string DescribeDeltaSummary(
        OperationalTrendSnapshot current,
        OperationalTrendSnapshot prior,
        OperationalTrendDirection direction,
        IReadOnlyList<string> signals)
    {
        if (signals.Contains(SignalOperationalStable, StringComparer.Ordinal))
            return "Operational state stable compared to the referenced snapshot.";

        if (signals.Contains(SignalPressureEscalation, StringComparer.Ordinal)
            && signals.Contains(SignalFingerprintInstability, StringComparer.Ordinal))
            return "Operational pressure increasing with shifting fingerprint stability.";

        if (signals.Contains(SignalPressureRecovery, StringComparer.Ordinal))
            return "Reduced replay pressure with stable runtime protection compared to the referenced snapshot.";

        if (signals.Contains(SignalDriftIncrease, StringComparer.Ordinal))
            return "Inventory drift conflicts increased compared to the referenced snapshot.";

        if (signals.Contains(SignalReplayStabilization, StringComparer.Ordinal))
            return "Replay stabilization movement observed compared to the referenced snapshot.";

        return direction switch
        {
            OperationalTrendDirection.Improving => "Operational indicators improved compared to the referenced snapshot.",
            OperationalTrendDirection.Degrading => "Operational indicators degraded compared to the referenced snapshot.",
            _ => "Operational indicators remained consistent with the referenced snapshot."
        };
    }

    private static string NormalizeFingerprintId(string fingerprintHash) =>
        string.IsNullOrWhiteSpace(fingerprintHash) ? string.Empty : fingerprintHash.Trim();

    private static string NormalizeLabel(string value) =>
        string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();

    private static int RankPressureBand(string band) => band switch
    {
        "Critical" => 4,
        "High" => 3,
        "Elevated" => 2,
        "Moderate" => 1,
        _ => 0
    };

    private static int RankReplayInstability(string level) => level switch
    {
        nameof(OperationalReplayPressureLevel.Critical) => 3,
        nameof(OperationalReplayPressureLevel.High) => 2,
        nameof(OperationalReplayPressureLevel.Elevated) => 1,
        _ => 0
    };

    private static int RankReadiness(string readiness) => readiness switch
    {
        "Ready" => 3,
        "Stabilizing" => 2,
        "Degraded" => 1,
        _ => 0
    };
}
