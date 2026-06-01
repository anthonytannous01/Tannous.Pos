using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Tannous.Pos.Application.OperationalWorkbench;

namespace Tannous.Pos.Application.OperationalRecovery;

/// <summary>Deterministic operator recovery posture from existing operational read models.</summary>
public static class OperationalRecoveryAggregation
{
    public const int MaxSignals = 8;
    public const int MaxRecommendations = 8;
    public const int MaxAttentionItems = 8;
    public const int MaxConvergenceItems = 8;

    public const string RouteDashboard = "dashboard";
    public const string RouteReconciliationWorkbench = "workbench/reconciliation";
    public const string RouteInventoryWorkbench = "inventory-workbench/drift";
    public const string RouteReplayWorkbench = "replay-workbench/pressure";
    public const string RouteTrendSummary = "trends/summary";
    public const string RouteTimeline = "timeline";
    public const string RouteTriage = "triage";
    public const string RouteNavigation = "navigation";

    public const string SectionReplayRecovery = "replay-recovery";
    public const string SectionInventoryStabilization = "inventory-stabilization";
    public const string SectionReconciliationRecovery = "reconciliation-recovery";
    public const string SectionRuntimePressure = "runtime-pressure-outlook";
    public const string SectionOperationalStability = "operational-stability";

    public static OperationalRecoveryPostureDto ComposePosture(
        OperationalTrendSummaryDto trend,
        OperationalTimelineDto timeline,
        IReadOnlyList<OperationalTimelineCorrelationDto> timelineCorrelations,
        OperationalTriageQueueDto triage,
        OperationalDashboardSummaryDto dashboard,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        OperationalGovernanceFingerprintSnapshot fingerprint,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive)
    {
        var signals = ComposeSignals(
            trend,
            timeline,
            replayPressure,
            replayStabilization,
            replayRecoveryConfidence,
            reconciliationWorkbench,
            inventoryWorkbench,
            runtimeProtection,
            fingerprint,
            runtimeSaturationIndicated,
            protectiveModeActive);

        var convergence = ComposeConvergence(
            trend,
            replayStabilization,
            replayRecoveryConfidence,
            reconciliationWorkbench,
            inventoryWorkbench,
            fingerprint,
            runtimeSaturationIndicated);

        var attention = ComposeAttention(
            triage,
            replayPressure,
            replayStabilization,
            inventoryWorkbench,
            runtimeProtection,
            protectiveModeActive);

        var recommendations = ComposeRecommendations(
            trend,
            triage,
            replayPressure,
            replayStabilization,
            replayRecoveryConfidence,
            reconciliationWorkbench,
            inventoryWorkbench,
            runtimeSaturationIndicated,
            protectiveModeActive);

        var overallState = ClassifyOverallState(
            trend,
            triage,
            replayPressure,
            replayStabilization,
            inventoryWorkbench,
            runtimeSaturationIndicated,
            protectiveModeActive,
            fingerprint);

        var overallDirection = ClassifyOverallDirection(
            trend,
            replayStabilization,
            replayRecoveryConfidence,
            fingerprint);

        var overallConfidence = ClassifyOverallConfidence(
            trend,
            replayRecoveryConfidence,
            replayStabilization,
            convergence);

        var overallSeverity = ClassifyOverallSeverity(
            overallState,
            triage,
            replayPressure,
            inventoryWorkbench,
            runtimeSaturationIndicated);

        return new OperationalRecoveryPostureDto
        {
            OverallState = overallState,
            OverallDirection = overallDirection,
            OverallConfidence = overallConfidence,
            OverallSeverity = overallSeverity,
            Summary = DescribeOverallSummary(overallState, overallDirection, overallConfidence),
            SignalCount = signals.Count,
            RecommendationCount = recommendations.Count,
            AttentionCount = attention.Count,
            Signals = signals,
            Convergence = convergence,
            Attention = attention,
            Recommendations = recommendations
        };
    }

    public static OperationalRecoveryOutlookDto ComposeOutlook(
        OperationalTrendSummaryDto trend,
        OperationalTimelineDto timeline,
        OperationalTriageQueueDto triage,
        OperationalDashboardSummaryDto dashboard,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        OperationalGovernanceFingerprintSnapshot fingerprint,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive)
    {
        var sections = new[]
        {
            ComposeReplayRecoverySection(replayPressure, replayStabilization, replayRecoveryConfidence),
            ComposeInventoryStabilizationSection(inventoryWorkbench, replayStabilization, trend),
            ComposeReconciliationRecoverySection(reconciliationWorkbench, replayStabilization, trend),
            ComposeRuntimePressureSection(runtimeProtection, dashboard, runtimeSaturationIndicated, protectiveModeActive),
            ComposeOperationalStabilitySection(trend, triage, fingerprint, dashboard)
        };

        var convergence = ComposeConvergence(
            trend,
            replayStabilization,
            replayRecoveryConfidence,
            reconciliationWorkbench,
            inventoryWorkbench,
            fingerprint,
            runtimeSaturationIndicated);

        var overallState = ClassifyOverallState(
            trend,
            triage,
            replayPressure,
            replayStabilization,
            inventoryWorkbench,
            runtimeSaturationIndicated,
            protectiveModeActive,
            fingerprint);

        var overallDirection = ClassifyOverallDirection(
            trend,
            replayStabilization,
            replayRecoveryConfidence,
            fingerprint);

        var overallConfidence = ClassifyOverallConfidence(
            trend,
            replayRecoveryConfidence,
            replayStabilization,
            convergence);

        var overallSeverity = ClassifyOverallSeverity(
            overallState,
            triage,
            replayPressure,
            inventoryWorkbench,
            runtimeSaturationIndicated);

        return new OperationalRecoveryOutlookDto
        {
            OverallState = overallState,
            OverallDirection = overallDirection,
            OverallConfidence = overallConfidence,
            OverallSeverity = overallSeverity,
            Summary = DescribeOutlookSummary(overallState, overallDirection, sections),
            SectionCount = sections.Length,
            ConvergenceCount = convergence.Count,
            Sections = sections,
            Convergence = convergence
        };
    }

    private static IReadOnlyList<OperationalRecoverySignalDto> ComposeSignals(
        OperationalTrendSummaryDto trend,
        OperationalTimelineDto timeline,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        OperationalGovernanceFingerprintSnapshot fingerprint,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive)
    {
        var candidates = new List<(int Priority, OperationalRecoverySignalDto Signal)>();

        if (replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.High
            || replayStabilization.ReplayPressureEscalating
            || replayStabilization.ReplayRecoveryImproving
            || replayStabilization.StabilizationActive)
        {
            var state = ClassifyReplayState(replayPressure, replayStabilization, replayRecoveryConfidence);
            candidates.Add((1, CreateSignal(
                "replay-pressure",
                "Replay",
                state,
                MapReplayDirection(replayStabilization, replayRecoveryConfidence),
                MapReplayConfidence(replayRecoveryConfidence),
                MapReplaySeverity(replayPressure.InstabilityLevel),
                DescribeReplaySignal(replayPressure, replayStabilization, replayRecoveryConfidence),
                RouteReplayWorkbench)));
        }

        if (inventoryWorkbench.DriftSummary.DriftSeverity >= OperationalInventoryDriftSeverity.Elevated
            || inventoryWorkbench.DriftSummary.EscalatingDriftConflicts > 0)
        {
            var state = ClassifyInventoryState(inventoryWorkbench, trend);
            candidates.Add((2, CreateSignal(
                "inventory-drift",
                "Inventory",
                state,
                MapInventoryDirection(inventoryWorkbench, trend),
                MapInventoryConfidence(inventoryWorkbench, trend),
                MapInventorySeverity(inventoryWorkbench.DriftSummary.DriftSeverity),
                DescribeInventorySignal(inventoryWorkbench, trend),
                RouteInventoryWorkbench)));
        }

        if (reconciliationWorkbench.Queue.EscalatingConflicts > 0
            || reconciliationWorkbench.ReplayRisk.StabilizationRecovering
            || reconciliationWorkbench.Queue.UnresolvedConflicts > 0)
        {
            var state = ClassifyReconciliationState(reconciliationWorkbench, replayStabilization);
            candidates.Add((3, CreateSignal(
                "reconciliation-recovery",
                "Reconciliation",
                state,
                MapReconciliationDirection(reconciliationWorkbench, replayStabilization),
                MapReconciliationConfidence(reconciliationWorkbench, replayStabilization),
                MapReconciliationSeverity(reconciliationWorkbench),
                DescribeReconciliationSignal(reconciliationWorkbench, replayStabilization),
                RouteReconciliationWorkbench)));
        }

        if (runtimeSaturationIndicated || protectiveModeActive || runtimeProtection.FailsafeActive)
        {
            var state = ClassifyRuntimeState(runtimeSaturationIndicated, protectiveModeActive, runtimeProtection);
            candidates.Add((4, CreateSignal(
                "runtime-pressure",
                "Runtime",
                state,
                MapRuntimeDirection(runtimeSaturationIndicated, protectiveModeActive, replayStabilization),
                MapRuntimeConfidence(runtimeSaturationIndicated, protectiveModeActive),
                MapRuntimeSeverity(runtimeSaturationIndicated, protectiveModeActive, runtimeProtection),
                DescribeRuntimeSignal(runtimeProtection, runtimeSaturationIndicated, protectiveModeActive),
                RouteDashboard)));
        }

        if (trend.OverallDirection != OperationalTrendDirection.Stable || trend.AttentionItems.Count > 0)
        {
            candidates.Add((5, CreateSignal(
                "operational-trend",
                "Operational Trend",
                MapTrendToRecoveryState(trend.OverallDirection, fingerprint),
                MapTrendDirection(trend.OverallDirection),
                MapTrendConfidence(trend),
                MapTrendSeverity(trend.Severity),
                trend.Summary,
                RouteTrendSummary)));
        }

        if (fingerprint.FingerprintChanged || !string.IsNullOrWhiteSpace(fingerprint.FingerprintStability))
        {
            var volatileFingerprint = IsVolatileFingerprint(fingerprint);
            candidates.Add((6, CreateSignal(
                "stability-transition",
                "Operational Stability",
                volatileFingerprint ? OperationalRecoveryState.Volatile : OperationalRecoveryState.Stable,
                MapFingerprintDirection(fingerprint, replayStabilization),
                MapFingerprintConfidence(fingerprint, replayRecoveryConfidence),
                volatileFingerprint ? OperationalRecoverySeverity.Elevated : OperationalRecoverySeverity.Nominal,
                DescribeFingerprintSignal(fingerprint),
                RouteTimeline)));
        }

        if (timeline.EventCount > 0)
        {
            candidates.Add((7, CreateSignal(
                "timeline-activity",
                "Timeline",
                timeline.AttentionItems.Count > 0
                    ? OperationalRecoveryState.Volatile
                    : OperationalRecoveryState.Stable,
                timeline.AttentionItems.Count > 0
                    ? OperationalRecoveryDirection.Diverging
                    : OperationalRecoveryDirection.Stable,
                OperationalRecoveryConfidence.Moderate,
                timeline.AttentionItems.Count > 0
                    ? OperationalRecoverySeverity.Moderate
                    : OperationalRecoverySeverity.Nominal,
                timeline.Summary,
                RouteTimeline)));
        }

        return candidates
            .OrderBy(c => c.Priority)
            .ThenBy(c => c.Signal.Domain, StringComparer.Ordinal)
            .Take(MaxSignals)
            .Select(c => c.Signal)
            .ToList();
    }

    private static IReadOnlyList<OperationalRecoveryConvergenceDto> ComposeConvergence(
        OperationalTrendSummaryDto trend,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalGovernanceFingerprintSnapshot fingerprint,
        bool runtimeSaturationIndicated)
    {
        var items = new List<OperationalRecoveryConvergenceDto>
        {
            new()
            {
                Domain = "Replay",
                Direction = MapReplayDirection(replayStabilization, replayRecoveryConfidence),
                Confidence = MapReplayConfidence(replayRecoveryConfidence),
                Summary = replayStabilization.ReplayRecoveryImproving
                    ? "Replay pressure stabilizing"
                    : replayStabilization.ReplayPressureEscalating
                        ? "Replay pressure diverging"
                        : "Replay conditions holding steady"
            },
            new()
            {
                Domain = "Inventory",
                Direction = MapInventoryDirection(inventoryWorkbench, trend),
                Confidence = MapInventoryConfidence(inventoryWorkbench, trend),
                Summary = inventoryWorkbench.DriftSummary.EscalatingDriftConflicts > 0
                    ? "Inventory drift divergence detected"
                    : inventoryWorkbench.DriftSummary.UnresolvedDriftConflicts == 0
                        ? "Inventory drift convergence detected"
                        : "Inventory conditions stabilizing"
            },
            new()
            {
                Domain = "Reconciliation",
                Direction = MapReconciliationDirection(reconciliationWorkbench, replayStabilization),
                Confidence = MapReconciliationConfidence(reconciliationWorkbench, replayStabilization),
                Summary = reconciliationWorkbench.ReplayRisk.StabilizationRecovering
                    ? "Reconciliation recovery convergence detected"
                    : reconciliationWorkbench.Queue.EscalatingConflicts > 0
                        ? "Reconciliation backlog diverging"
                        : "Reconciliation visibility holding steady"
            },
            new()
            {
                Domain = "Runtime",
                Direction = runtimeSaturationIndicated
                    ? OperationalRecoveryDirection.Diverging
                    : replayStabilization.ReplayRecoveryImproving
                        ? OperationalRecoveryDirection.Converging
                        : OperationalRecoveryDirection.Stable,
                Confidence = runtimeSaturationIndicated
                    ? OperationalRecoveryConfidence.Low
                    : OperationalRecoveryConfidence.Moderate,
                Summary = runtimeSaturationIndicated
                    ? "Runtime pressure remains elevated"
                    : replayStabilization.ProtectiveContainmentActive
                        ? "Runtime protection easing toward stability"
                        : "Runtime pressure converging"
            },
            new()
            {
                Domain = "Operational Stability",
                Direction = MapFingerprintDirection(fingerprint, replayStabilization),
                Confidence = MapFingerprintConfidence(fingerprint, replayRecoveryConfidence),
                Summary = IsVolatileFingerprint(fingerprint)
                    ? "Operational conditions remain volatile"
                    : trend.OverallDirection == OperationalTrendDirection.Improving
                        ? "Operational recovery confidence improving"
                        : "Operational stability holding steady"
            }
        };

        return items
            .OrderBy(i => i.Domain, StringComparer.Ordinal)
            .Take(MaxConvergenceItems)
            .ToList();
    }

    private static IReadOnlyList<OperationalRecoveryAttentionDto> ComposeAttention(
        OperationalTriageQueueDto triage,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        bool protectiveModeActive)
    {
        var items = new List<(int Priority, OperationalRecoveryAttentionDto Item)>();

        foreach (var attention in triage.AttentionItems.Take(MaxAttentionItems))
        {
            items.Add((attention.Priority, new OperationalRecoveryAttentionDto
            {
                AttentionId = $"triage-{attention.Priority}-{attention.Category}",
                Domain = DescribeTriageCategory(attention.Category),
                Severity = MapTriagePrioritySeverity(attention.PriorityBand),
                Summary = string.IsNullOrWhiteSpace(attention.Detail) ? attention.Title : attention.Detail,
                RecommendedRoute = NormalizeRoute(attention.RecommendedRoute)
            }));
        }

        if (replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.High)
        {
            items.Add((1, new OperationalRecoveryAttentionDto
            {
                AttentionId = "replay-instability",
                Domain = "Replay",
                Severity = MapReplaySeverity(replayPressure.InstabilityLevel),
                Summary = "Replay instability remains elevated",
                RecommendedRoute = RouteReplayWorkbench
            }));
        }

        if (inventoryWorkbench.DriftSummary.DriftSeverity >= OperationalInventoryDriftSeverity.High)
        {
            items.Add((2, new OperationalRecoveryAttentionDto
            {
                AttentionId = "inventory-drift",
                Domain = "Inventory",
                Severity = MapInventorySeverity(inventoryWorkbench.DriftSummary.DriftSeverity),
                Summary = "Inventory drift requires stabilization review",
                RecommendedRoute = RouteInventoryWorkbench
            }));
        }

        if (runtimeProtection.FailsafeActive || protectiveModeActive)
        {
            items.Add((3, new OperationalRecoveryAttentionDto
            {
                AttentionId = "runtime-protection",
                Domain = "Runtime",
                Severity = OperationalRecoverySeverity.High,
                Summary = "Runtime protection remains active — recovery outlook may be constrained",
                RecommendedRoute = RouteDashboard
            }));
        }

        if (replayStabilization.ReplayRecoveryStalled)
        {
            items.Add((4, new OperationalRecoveryAttentionDto
            {
                AttentionId = "replay-stalled",
                Domain = "Replay",
                Severity = OperationalRecoverySeverity.Elevated,
                Summary = "Replay recovery progress has stalled",
                RecommendedRoute = RouteReplayWorkbench
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Item.AttentionId, StringComparer.Ordinal)
            .Take(MaxAttentionItems)
            .Select(i => i.Item)
            .ToList();
    }

    private static IReadOnlyList<OperationalRecoveryRecommendationDto> ComposeRecommendations(
        OperationalTrendSummaryDto trend,
        OperationalTriageQueueDto triage,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive)
    {
        var items = new List<(int Priority, OperationalRecoveryRecommendationDto Item)>();

        if (replayStabilization.ReplayRecoveryImproving)
        {
            items.Add((1, CreateRecommendation(
                "replay-stabilizing",
                "Replay",
                OperationalRecoverySeverity.Moderate,
                "Review replay stabilization progress and confirm pressure is easing",
                RouteReplayWorkbench)));
        }
        else if (replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.High)
        {
            items.Add((1, CreateRecommendation(
                "replay-pressure",
                "Replay",
                MapReplaySeverity(replayPressure.InstabilityLevel),
                "Investigate replay pressure before conditions diverge further",
                RouteReplayWorkbench)));
        }

        if (inventoryWorkbench.DriftSummary.EscalatingDriftConflicts > 0)
        {
            items.Add((2, CreateRecommendation(
                "inventory-drift",
                "Inventory",
                MapInventorySeverity(inventoryWorkbench.DriftSummary.DriftSeverity),
                "Review inventory drift escalation and linked replay pressure",
                RouteInventoryWorkbench)));
        }
        else if (inventoryWorkbench.DriftSummary.UnresolvedDriftConflicts == 0
                 && trend.OverallDirection == OperationalTrendDirection.Improving)
        {
            items.Add((2, CreateRecommendation(
                "inventory-stable",
                "Inventory",
                OperationalRecoverySeverity.Nominal,
                "Inventory conditions stabilizing — monitor for sustained convergence",
                RouteInventoryWorkbench)));
        }

        if (reconciliationWorkbench.Queue.EscalatingConflicts > 0)
        {
            items.Add((3, CreateRecommendation(
                "reconciliation-backlog",
                "Reconciliation",
                OperationalRecoverySeverity.Elevated,
                "Address escalating reconciliation backlog to support recovery",
                RouteReconciliationWorkbench)));
        }
        else if (reconciliationWorkbench.ReplayRisk.StabilizationRecovering)
        {
            items.Add((3, CreateRecommendation(
                "reconciliation-recovering",
                "Reconciliation",
                OperationalRecoverySeverity.Moderate,
                "Reconciliation recovery in progress — verify backlog is clearing",
                RouteReconciliationWorkbench)));
        }

        if (runtimeSaturationIndicated || protectiveModeActive)
        {
            items.Add((4, CreateRecommendation(
                "runtime-pressure",
                "Runtime",
                OperationalRecoverySeverity.High,
                "Review runtime pressure and protective conditions affecting recovery outlook",
                RouteDashboard)));
        }

        if (trend.OverallDirection == OperationalTrendDirection.Degrading)
        {
            items.Add((5, CreateRecommendation(
                "trend-degrading",
                "Operational Trend",
                MapTrendSeverity(trend.Severity),
                "Operational trend is degrading — correlate with timeline and triage priorities",
                RouteTrendSummary)));
        }
        else if (trend.OverallDirection == OperationalTrendDirection.Improving)
        {
            items.Add((5, CreateRecommendation(
                "trend-improving",
                "Operational Trend",
                OperationalRecoverySeverity.Nominal,
                "Operational recovery confidence improving — continue monitoring convergence",
                RouteTrendSummary)));
        }

        if (replayRecoveryConfidence.Confidence == OperationalReplayRecoveryConfidence.Fragile)
        {
            items.Add((6, CreateRecommendation(
                "recovery-fragile",
                "Recovery Confidence",
                OperationalRecoverySeverity.High,
                "Recovery confidence remains fragile — prioritize stabilization review",
                RouteTriage)));
        }

        if (triage.ItemCount > 0)
        {
            items.Add((7, CreateRecommendation(
                "triage-follow-up",
                "Investigation",
                MapTriagePrioritySeverity(triage.OverallPriority),
                "Review triage priorities to align recovery actions with active concerns",
                RouteTriage)));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Item.RecommendationId, StringComparer.Ordinal)
            .Take(MaxRecommendations)
            .Select(i => i.Item)
            .ToList();
    }

    private static OperationalRecoveryOutlookSectionDto ComposeReplayRecoverySection(
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence)
    {
        var state = ClassifyReplayState(replayPressure, replayStabilization, replayRecoveryConfidence);
        return new OperationalRecoveryOutlookSectionDto
        {
            SectionId = SectionReplayRecovery,
            Title = "Replay Recovery",
            State = state,
            Direction = MapReplayDirection(replayStabilization, replayRecoveryConfidence),
            Confidence = MapReplayConfidence(replayRecoveryConfidence),
            Severity = MapReplaySeverity(replayPressure.InstabilityLevel),
            Summary = replayStabilization.ReplayRecoveryImproving
                ? "Replay pressure stabilizing"
                : replayStabilization.ReplayPressureEscalating
                    ? "Replay instability remains elevated"
                    : replayRecoveryConfidence.Summary,
            RecommendedRoute = RouteReplayWorkbench
        };
    }

    private static OperationalRecoveryOutlookSectionDto ComposeInventoryStabilizationSection(
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalTrendSummaryDto trend)
    {
        return new OperationalRecoveryOutlookSectionDto
        {
            SectionId = SectionInventoryStabilization,
            Title = "Inventory Stabilization",
            State = ClassifyInventoryState(inventoryWorkbench, trend),
            Direction = MapInventoryDirection(inventoryWorkbench, trend),
            Confidence = MapInventoryConfidence(inventoryWorkbench, trend),
            Severity = MapInventorySeverity(inventoryWorkbench.DriftSummary.DriftSeverity),
            Summary = inventoryWorkbench.DriftSummary.EscalatingDriftConflicts > 0
                ? "Inventory drift escalation detected"
                : inventoryWorkbench.DriftSummary.UnresolvedDriftConflicts == 0
                    ? "Inventory conditions stabilizing"
                    : inventoryWorkbench.DriftSummary.Summary,
            RecommendedRoute = RouteInventoryWorkbench
        };
    }

    private static OperationalRecoveryOutlookSectionDto ComposeReconciliationRecoverySection(
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalTrendSummaryDto trend)
    {
        return new OperationalRecoveryOutlookSectionDto
        {
            SectionId = SectionReconciliationRecovery,
            Title = "Reconciliation Recovery",
            State = ClassifyReconciliationState(reconciliationWorkbench, replayStabilization),
            Direction = MapReconciliationDirection(reconciliationWorkbench, replayStabilization),
            Confidence = MapReconciliationConfidence(reconciliationWorkbench, replayStabilization),
            Severity = MapReconciliationSeverity(reconciliationWorkbench),
            Summary = reconciliationWorkbench.ReplayRisk.StabilizationRecovering
                ? "Reconciliation recovery convergence detected"
                : reconciliationWorkbench.Queue.EscalatingConflicts > 0
                    ? "Reconciliation backlog diverging from recovery"
                    : reconciliationWorkbench.Queue.Summary,
            RecommendedRoute = RouteReconciliationWorkbench
        };
    }

    private static OperationalRecoveryOutlookSectionDto ComposeRuntimePressureSection(
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        OperationalDashboardSummaryDto dashboard,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive)
    {
        var state = ClassifyRuntimeState(runtimeSaturationIndicated, protectiveModeActive, runtimeProtection);
        return new OperationalRecoveryOutlookSectionDto
        {
            SectionId = SectionRuntimePressure,
            Title = "Runtime Pressure Outlook",
            State = state,
            Direction = MapRuntimeDirection(runtimeSaturationIndicated, protectiveModeActive, new OperationalReplayStabilizationDto()),
            Confidence = MapRuntimeConfidence(runtimeSaturationIndicated, protectiveModeActive),
            Severity = MapRuntimeSeverity(runtimeSaturationIndicated, protectiveModeActive, runtimeProtection),
            Summary = runtimeSaturationIndicated
                ? "Runtime pressure remains elevated"
                : protectiveModeActive
                    ? "Runtime protection active — pressure easing toward stability"
                    : dashboard.Pressure.Summary,
            RecommendedRoute = RouteDashboard
        };
    }

    private static OperationalRecoveryOutlookSectionDto ComposeOperationalStabilitySection(
        OperationalTrendSummaryDto trend,
        OperationalTriageQueueDto triage,
        OperationalGovernanceFingerprintSnapshot fingerprint,
        OperationalDashboardSummaryDto dashboard)
    {
        var volatileConditions = IsVolatileFingerprint(fingerprint)
            || trend.OverallDirection == OperationalTrendDirection.Degrading
            || triage.OverallPriority <= OperationalTriagePriority.Elevated;

        return new OperationalRecoveryOutlookSectionDto
        {
            SectionId = SectionOperationalStability,
            Title = "Operational Stability",
            State = volatileConditions
                ? OperationalRecoveryState.Volatile
                : trend.OverallDirection == OperationalTrendDirection.Improving
                    ? OperationalRecoveryState.Recovering
                    : OperationalRecoveryState.Stable,
            Direction = MapTrendDirection(trend.OverallDirection),
            Confidence = MapTrendConfidence(trend),
            Severity = volatileConditions
                ? OperationalRecoverySeverity.Elevated
                : MapTrendSeverity(trend.Severity),
            Summary = volatileConditions
                ? "Operational conditions remain volatile"
                : trend.OverallDirection == OperationalTrendDirection.Improving
                    ? "Operational recovery confidence improving"
                    : dashboard.ReadinessSummary,
            RecommendedRoute = RouteNavigation
        };
    }

    private static OperationalRecoveryState ClassifyOverallState(
        OperationalTrendSummaryDto trend,
        OperationalTriageQueueDto triage,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive,
        OperationalGovernanceFingerprintSnapshot fingerprint)
    {
        if (runtimeSaturationIndicated && (protectiveModeActive || replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.Critical))
            return OperationalRecoveryState.Saturated;

        if (IsVolatileFingerprint(fingerprint)
            || (trend.OverallDirection == OperationalTrendDirection.Degrading && triage.OverallPriority <= OperationalTriagePriority.High))
            return OperationalRecoveryState.Volatile;

        if (trend.OverallDirection == OperationalTrendDirection.Degrading
            || replayStabilization.ReplayPressureEscalating
            || inventoryWorkbench.DriftSummary.EscalatingDriftConflicts > 0)
            return OperationalRecoveryState.Degrading;

        if (replayStabilization.StabilizationActive
            || replayStabilization.ReplayRecoveryImproving
            || (replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.Elevated && !replayStabilization.ReplayPressureEscalating))
            return OperationalRecoveryState.Stabilizing;

        if (trend.OverallDirection == OperationalTrendDirection.Improving
            || replayStabilization.ReplayRecoveryImproving)
            return OperationalRecoveryState.Recovering;

        return OperationalRecoveryState.Stable;
    }

    private static OperationalRecoveryDirection ClassifyOverallDirection(
        OperationalTrendSummaryDto trend,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence,
        OperationalGovernanceFingerprintSnapshot fingerprint)
    {
        if (replayStabilization.ReplayRecoveryImproving || trend.OverallDirection == OperationalTrendDirection.Improving)
            return OperationalRecoveryDirection.Improving;

        if (IsVolatileFingerprint(fingerprint))
            return OperationalRecoveryDirection.Diverging;

        if (replayStabilization.ReplayPressureEscalating || trend.OverallDirection == OperationalTrendDirection.Degrading)
            return OperationalRecoveryDirection.Degrading;

        if (replayStabilization.StabilizationActive)
            return OperationalRecoveryDirection.Converging;

        return OperationalRecoveryDirection.Stable;
    }

    private static OperationalRecoveryConfidence ClassifyOverallConfidence(
        OperationalTrendSummaryDto trend,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence,
        OperationalReplayStabilizationDto replayStabilization,
        IReadOnlyList<OperationalRecoveryConvergenceDto> convergence)
    {
        var baseConfidence = MapReplayConfidence(replayRecoveryConfidence);
        var convergingCount = convergence.Count(c =>
            c.Direction == OperationalRecoveryDirection.Converging
            || c.Direction == OperationalRecoveryDirection.Improving);

        if (baseConfidence == OperationalRecoveryConfidence.High && convergingCount >= 3)
            return OperationalRecoveryConfidence.High;

        if (baseConfidence >= OperationalRecoveryConfidence.Elevated || trend.OverallDirection == OperationalTrendDirection.Improving)
            return OperationalRecoveryConfidence.Elevated;

        if (replayStabilization.ReplayRecoveryStalled || replayRecoveryConfidence.Confidence == OperationalReplayRecoveryConfidence.Fragile)
            return OperationalRecoveryConfidence.Low;

        return OperationalRecoveryConfidence.Moderate;
    }

    private static OperationalRecoverySeverity ClassifyOverallSeverity(
        OperationalRecoveryState state,
        OperationalTriageQueueDto triage,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        bool runtimeSaturationIndicated)
    {
        if (state == OperationalRecoveryState.Saturated || triage.OverallPriority == OperationalTriagePriority.Critical)
            return OperationalRecoverySeverity.Critical;

        if (state == OperationalRecoveryState.Volatile || replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.Critical)
            return OperationalRecoverySeverity.High;

        if (state == OperationalRecoveryState.Degrading
            || inventoryWorkbench.DriftSummary.DriftSeverity >= OperationalInventoryDriftSeverity.High
            || runtimeSaturationIndicated)
            return OperationalRecoverySeverity.Elevated;

        if (state == OperationalRecoveryState.Stabilizing || state == OperationalRecoveryState.Recovering)
            return OperationalRecoverySeverity.Moderate;

        return OperationalRecoverySeverity.Nominal;
    }

    private static OperationalRecoveryState ClassifyReplayState(
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence)
    {
        if (replayStabilization.ReplayRecoveryImproving)
            return OperationalRecoveryState.Recovering;

        if (replayStabilization.StabilizationActive)
            return OperationalRecoveryState.Stabilizing;

        if (replayStabilization.ReplayPressureEscalating || replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.High)
            return OperationalRecoveryState.Degrading;

        if (replayRecoveryConfidence.Confidence == OperationalReplayRecoveryConfidence.Fragile)
            return OperationalRecoveryState.Volatile;

        return OperationalRecoveryState.Stable;
    }

    private static OperationalRecoveryState ClassifyInventoryState(
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalTrendSummaryDto trend)
    {
        if (inventoryWorkbench.DriftSummary.EscalatingDriftConflicts > 0)
            return OperationalRecoveryState.Degrading;

        if (inventoryWorkbench.DriftSummary.UnresolvedDriftConflicts == 0 && trend.OverallDirection != OperationalTrendDirection.Degrading)
            return OperationalRecoveryState.Stabilizing;

        if (trend.OverallDirection == OperationalTrendDirection.Improving)
            return OperationalRecoveryState.Recovering;

        return OperationalRecoveryState.Stable;
    }

    private static OperationalRecoveryState ClassifyReconciliationState(
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalReplayStabilizationDto replayStabilization)
    {
        if (reconciliationWorkbench.ReplayRisk.StabilizationRecovering || replayStabilization.ReplayRecoveryImproving)
            return OperationalRecoveryState.Recovering;

        if (reconciliationWorkbench.Queue.EscalatingConflicts > 0)
            return OperationalRecoveryState.Degrading;

        if (replayStabilization.StabilizationActive)
            return OperationalRecoveryState.Stabilizing;

        return OperationalRecoveryState.Stable;
    }

    private static OperationalRecoveryState ClassifyRuntimeState(
        bool runtimeSaturationIndicated,
        bool protectiveModeActive,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection)
    {
        if (runtimeSaturationIndicated && (protectiveModeActive || runtimeProtection.FailsafeActive))
            return OperationalRecoveryState.Saturated;

        if (runtimeSaturationIndicated || protectiveModeActive)
            return OperationalRecoveryState.Degrading;

        if (runtimeProtection.FailsafeActive)
            return OperationalRecoveryState.Volatile;

        return OperationalRecoveryState.Stable;
    }

    private static OperationalRecoveryDirection MapReplayDirection(
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence)
    {
        if (replayStabilization.ReplayRecoveryImproving
            || replayRecoveryConfidence.Confidence == OperationalReplayRecoveryConfidence.Recovering)
            return OperationalRecoveryDirection.Converging;

        if (replayStabilization.ReplayPressureEscalating)
            return OperationalRecoveryDirection.Diverging;

        if (replayStabilization.ReplayRecoveryStalled)
            return OperationalRecoveryDirection.Degrading;

        return OperationalRecoveryDirection.Stable;
    }

    private static OperationalRecoveryDirection MapInventoryDirection(
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalTrendSummaryDto trend)
    {
        if (inventoryWorkbench.DriftSummary.EscalatingDriftConflicts > 0)
            return OperationalRecoveryDirection.Diverging;

        if (inventoryWorkbench.DriftSummary.UnresolvedDriftConflicts == 0 || trend.OverallDirection == OperationalTrendDirection.Improving)
            return OperationalRecoveryDirection.Converging;

        return OperationalRecoveryDirection.Stable;
    }

    private static OperationalRecoveryDirection MapReconciliationDirection(
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalReplayStabilizationDto replayStabilization)
    {
        if (reconciliationWorkbench.ReplayRisk.StabilizationRecovering || replayStabilization.ReplayRecoveryImproving)
            return OperationalRecoveryDirection.Converging;

        if (reconciliationWorkbench.Queue.EscalatingConflicts > 0)
            return OperationalRecoveryDirection.Diverging;

        return OperationalRecoveryDirection.Stable;
    }

    private static OperationalRecoveryDirection MapRuntimeDirection(
        bool runtimeSaturationIndicated,
        bool protectiveModeActive,
        OperationalReplayStabilizationDto replayStabilization)
    {
        if (runtimeSaturationIndicated || protectiveModeActive)
            return OperationalRecoveryDirection.Diverging;

        if (replayStabilization.ReplayRecoveryImproving)
            return OperationalRecoveryDirection.Converging;

        return OperationalRecoveryDirection.Stable;
    }

    private static OperationalRecoveryDirection MapTrendDirection(OperationalTrendDirection trendDirection) =>
        trendDirection switch
        {
            OperationalTrendDirection.Improving => OperationalRecoveryDirection.Improving,
            OperationalTrendDirection.Degrading => OperationalRecoveryDirection.Degrading,
            _ => OperationalRecoveryDirection.Stable
        };

    private static OperationalRecoveryDirection MapFingerprintDirection(
        OperationalGovernanceFingerprintSnapshot fingerprint,
        OperationalReplayStabilizationDto replayStabilization)
    {
        if (IsVolatileFingerprint(fingerprint))
            return OperationalRecoveryDirection.Diverging;

        if (replayStabilization.ReplayRecoveryImproving && !fingerprint.FingerprintChanged)
            return OperationalRecoveryDirection.Converging;

        return OperationalRecoveryDirection.Stable;
    }

    private static OperationalRecoveryConfidence MapReplayConfidence(
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence) =>
        replayRecoveryConfidence.Confidence switch
        {
            OperationalReplayRecoveryConfidence.Stable => OperationalRecoveryConfidence.High,
            OperationalReplayRecoveryConfidence.Recovering => OperationalRecoveryConfidence.Elevated,
            OperationalReplayRecoveryConfidence.Uncertain => OperationalRecoveryConfidence.Moderate,
            _ => OperationalRecoveryConfidence.Low
        };

    private static OperationalRecoveryConfidence MapInventoryConfidence(
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalTrendSummaryDto trend)
    {
        if (inventoryWorkbench.DriftSummary.UnresolvedDriftConflicts == 0)
            return OperationalRecoveryConfidence.Elevated;

        if (inventoryWorkbench.DriftSummary.EscalatingDriftConflicts > 0 || trend.OverallDirection == OperationalTrendDirection.Degrading)
            return OperationalRecoveryConfidence.Low;

        return OperationalRecoveryConfidence.Moderate;
    }

    private static OperationalRecoveryConfidence MapReconciliationConfidence(
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalReplayStabilizationDto replayStabilization)
    {
        if (reconciliationWorkbench.ReplayRisk.StabilizationRecovering && !replayStabilization.ReplayPressureEscalating)
            return OperationalRecoveryConfidence.Elevated;

        if (reconciliationWorkbench.Queue.EscalatingConflicts > 0)
            return OperationalRecoveryConfidence.Low;

        return OperationalRecoveryConfidence.Moderate;
    }

    private static OperationalRecoveryConfidence MapRuntimeConfidence(
        bool runtimeSaturationIndicated,
        bool protectiveModeActive)
    {
        if (runtimeSaturationIndicated || protectiveModeActive)
            return OperationalRecoveryConfidence.Low;

        return OperationalRecoveryConfidence.Moderate;
    }

    private static OperationalRecoveryConfidence MapTrendConfidence(OperationalTrendSummaryDto trend) =>
        trend.OverallDirection switch
        {
            OperationalTrendDirection.Improving => OperationalRecoveryConfidence.Elevated,
            OperationalTrendDirection.Degrading => OperationalRecoveryConfidence.Low,
            _ => OperationalRecoveryConfidence.Moderate
        };

    private static OperationalRecoveryConfidence MapFingerprintConfidence(
        OperationalGovernanceFingerprintSnapshot fingerprint,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence)
    {
        if (IsVolatileFingerprint(fingerprint))
            return OperationalRecoveryConfidence.Low;

        return MapReplayConfidence(replayRecoveryConfidence);
    }

    private static OperationalRecoveryState MapTrendToRecoveryState(
        OperationalTrendDirection trendDirection,
        OperationalGovernanceFingerprintSnapshot fingerprint)
    {
        if (IsVolatileFingerprint(fingerprint))
            return OperationalRecoveryState.Volatile;

        return trendDirection switch
        {
            OperationalTrendDirection.Improving => OperationalRecoveryState.Recovering,
            OperationalTrendDirection.Degrading => OperationalRecoveryState.Degrading,
            _ => OperationalRecoveryState.Stable
        };
    }

    private static OperationalRecoverySeverity MapReplaySeverity(OperationalReplayPressureLevel level) =>
        level switch
        {
            OperationalReplayPressureLevel.Critical => OperationalRecoverySeverity.Critical,
            OperationalReplayPressureLevel.High => OperationalRecoverySeverity.High,
            OperationalReplayPressureLevel.Elevated => OperationalRecoverySeverity.Elevated,
            _ => OperationalRecoverySeverity.Nominal
        };

    private static OperationalRecoverySeverity MapInventorySeverity(OperationalInventoryDriftSeverity severity) =>
        severity switch
        {
            OperationalInventoryDriftSeverity.Critical => OperationalRecoverySeverity.Critical,
            OperationalInventoryDriftSeverity.High => OperationalRecoverySeverity.High,
            OperationalInventoryDriftSeverity.Elevated => OperationalRecoverySeverity.Elevated,
            _ => OperationalRecoverySeverity.Nominal
        };

    private static OperationalRecoverySeverity MapReconciliationSeverity(
        OperationalReconciliationWorkbenchDto reconciliationWorkbench)
    {
        var score = reconciliationWorkbench.Queue.EscalatingConflicts + reconciliationWorkbench.Queue.UnresolvedConflicts;
        if (score >= 6)
            return OperationalRecoverySeverity.High;

        if (score >= 3)
            return OperationalRecoverySeverity.Elevated;

        if (score >= 1)
            return OperationalRecoverySeverity.Moderate;

        return OperationalRecoverySeverity.Nominal;
    }

    private static OperationalRecoverySeverity MapRuntimeSeverity(
        bool runtimeSaturationIndicated,
        bool protectiveModeActive,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection)
    {
        if (runtimeSaturationIndicated && runtimeProtection.FailsafeActive)
            return OperationalRecoverySeverity.Critical;

        if (runtimeSaturationIndicated || protectiveModeActive)
            return OperationalRecoverySeverity.High;

        if (runtimeProtection.FailsafeActive)
            return OperationalRecoverySeverity.Elevated;

        return OperationalRecoverySeverity.Nominal;
    }

    private static OperationalRecoverySeverity MapTrendSeverity(OperationalTrendSeverity severity) =>
        severity switch
        {
            OperationalTrendSeverity.Critical => OperationalRecoverySeverity.Critical,
            OperationalTrendSeverity.High => OperationalRecoverySeverity.High,
            OperationalTrendSeverity.Elevated => OperationalRecoverySeverity.Elevated,
            OperationalTrendSeverity.Moderate => OperationalRecoverySeverity.Moderate,
            _ => OperationalRecoverySeverity.Nominal
        };

    private static string DescribeTriageCategory(OperationalTriageCategory category) =>
        category switch
        {
            OperationalTriageCategory.ReplayInstability => "Replay",
            OperationalTriageCategory.RuntimeProtection => "Runtime",
            OperationalTriageCategory.InventoryDrift => "Inventory",
            OperationalTriageCategory.ReconciliationBacklog => "Reconciliation",
            OperationalTriageCategory.Stabilization => "Stabilization",
            OperationalTriageCategory.TrendMovement => "Operational Trend",
            _ => "System Monitoring"
        };

    private static OperationalRecoverySeverity MapTriagePrioritySeverity(OperationalTriagePriority priority) =>
        priority switch
        {
            OperationalTriagePriority.Critical => OperationalRecoverySeverity.Critical,
            OperationalTriagePriority.High => OperationalRecoverySeverity.High,
            OperationalTriagePriority.Elevated => OperationalRecoverySeverity.Elevated,
            OperationalTriagePriority.Moderate => OperationalRecoverySeverity.Moderate,
            _ => OperationalRecoverySeverity.Nominal
        };

    private static bool IsVolatileFingerprint(OperationalGovernanceFingerprintSnapshot fingerprint) =>
        string.Equals(fingerprint.FingerprintStability, "Volatile", StringComparison.OrdinalIgnoreCase)
        || (fingerprint.FingerprintChanged && fingerprint.HasPreviousFingerprint);

    private static string DescribeOverallSummary(
        OperationalRecoveryState state,
        OperationalRecoveryDirection direction,
        OperationalRecoveryConfidence confidence) =>
        state switch
        {
            OperationalRecoveryState.Recovering =>
                direction == OperationalRecoveryDirection.Improving
                    ? "Operational recovery confidence improving"
                    : "Operational conditions recovering",
            OperationalRecoveryState.Stabilizing => "Operational conditions stabilizing across active domains",
            OperationalRecoveryState.Degrading => "Operational conditions degrading — review recovery outlook sections",
            OperationalRecoveryState.Volatile => "Operational conditions remain volatile",
            OperationalRecoveryState.Saturated => "Operational recovery constrained by elevated runtime saturation",
            _ when confidence >= OperationalRecoveryConfidence.Elevated =>
                "Operational stability holding with improving recovery confidence",
            _ => "Operational recovery posture stable — continue monitoring convergence"
        };

    private static string DescribeOutlookSummary(
        OperationalRecoveryState state,
        OperationalRecoveryDirection direction,
        IReadOnlyList<OperationalRecoveryOutlookSectionDto> sections)
    {
        var degradingSections = sections.Count(s =>
            s.State == OperationalRecoveryState.Degrading || s.State == OperationalRecoveryState.Volatile);

        if (state == OperationalRecoveryState.Recovering || direction == OperationalRecoveryDirection.Converging)
            return "Stabilization outlook indicates recovery convergence across operational domains";

        if (degradingSections >= 2)
            return "Multiple operational domains diverging — recovery outlook requires operator review";

        return "Recovery outlook composed from existing operational diagnostics — advisory guidance only";
    }

    private static string DescribeReplaySignal(
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence)
    {
        if (replayStabilization.ReplayRecoveryImproving)
            return "Replay pressure stabilizing";

        if (replayStabilization.ReplayPressureEscalating)
            return "Replay instability remains elevated";

        return replayRecoveryConfidence.Summary;
    }

    private static string DescribeInventorySignal(
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalTrendSummaryDto trend)
    {
        if (inventoryWorkbench.DriftSummary.EscalatingDriftConflicts > 0)
            return "Inventory drift divergence detected";

        if (inventoryWorkbench.DriftSummary.UnresolvedDriftConflicts == 0)
            return "Inventory drift convergence detected";

        return trend.OverallDirection == OperationalTrendDirection.Improving
            ? "Inventory conditions stabilizing"
            : inventoryWorkbench.DriftSummary.Summary;
    }

    private static string DescribeReconciliationSignal(
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalReplayStabilizationDto replayStabilization)
    {
        if (reconciliationWorkbench.ReplayRisk.StabilizationRecovering)
            return "Reconciliation recovery convergence detected";

        if (reconciliationWorkbench.Queue.EscalatingConflicts > 0)
            return "Reconciliation backlog diverging from recovery";

        return replayStabilization.StabilizationActive
            ? "Reconciliation visibility stabilizing"
            : reconciliationWorkbench.Queue.Summary;
    }

    private static string DescribeRuntimeSignal(
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive)
    {
        if (runtimeSaturationIndicated)
            return "Runtime pressure remains elevated";

        if (protectiveModeActive || runtimeProtection.FailsafeActive)
            return "Runtime protection active — recovery outlook may be constrained";

        return "Runtime pressure converging";
    }

    private static string DescribeFingerprintSignal(OperationalGovernanceFingerprintSnapshot fingerprint) =>
        IsVolatileFingerprint(fingerprint)
            ? "Operational stability transitions detected — conditions remain volatile"
            : fingerprint.FingerprintChanged
                ? "Operational stability transition observed — monitor convergence"
                : "Operational stability holding steady";

    private static OperationalRecoverySignalDto CreateSignal(
        string signalId,
        string domain,
        OperationalRecoveryState state,
        OperationalRecoveryDirection direction,
        OperationalRecoveryConfidence confidence,
        OperationalRecoverySeverity severity,
        string summary,
        string route) =>
        new()
        {
            SignalId = signalId,
            Domain = domain,
            State = state,
            Direction = direction,
            Confidence = confidence,
            Severity = severity,
            Summary = summary,
            RecommendedRoute = route
        };

    private static OperationalRecoveryRecommendationDto CreateRecommendation(
        string recommendationId,
        string domain,
        OperationalRecoverySeverity severity,
        string summary,
        string route) =>
        new()
        {
            RecommendationId = recommendationId,
            Domain = domain,
            Severity = severity,
            Summary = summary,
            RecommendedRoute = route
        };

    private static string NormalizeRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
            return RouteDashboard;

        var trimmed = route.Trim().TrimStart('/');
        return trimmed switch
        {
            RouteDashboard => RouteDashboard,
            RouteReconciliationWorkbench => RouteReconciliationWorkbench,
            RouteInventoryWorkbench => RouteInventoryWorkbench,
            RouteReplayWorkbench => RouteReplayWorkbench,
            RouteTrendSummary => RouteTrendSummary,
            RouteTimeline => RouteTimeline,
            RouteTriage => RouteTriage,
            RouteNavigation => RouteNavigation,
            _ => trimmed
        };
    }
}
