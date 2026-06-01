using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTrends;

using Tannous.Pos.Application.OperationalCognition;

namespace Tannous.Pos.Application.OperationalSimulation;

/// <summary>Deterministic hypothetical operational analysis from existing read models.</summary>
public static class OperationalSimulationAggregation
{
    public const int MaxScenarios = 8;
    public const int MaxStabilizationPaths = 8;
    public const int MaxDegradationPaths = 8;
    public const int MaxLeveragePoints = 8;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;
    public const int MaxImprovementSequenceSteps = 4;

    public const string AreaReplay = "Replay";
    public const string AreaInventory = "Inventory";
    public const string AreaReconciliation = "Reconciliation";
    public const string AreaRuntime = "Runtime";
    public const string AreaOperational = "Operational Stability";

    public const string ScenarioReplayStabilization = "scenario-replay-stabilization";
    public const string ScenarioRuntimeEscalation = "scenario-runtime-escalation";
    public const string ScenarioInventoryStabilization = "scenario-inventory-stabilization";
    public const string ScenarioReconciliationStabilization = "scenario-reconciliation-stabilization";
    public const string ScenarioReconciliationDegradation = "scenario-reconciliation-degradation";
    public const string ScenarioVolatilityDegradation = "scenario-volatility-degradation";
    public const string ScenarioRecoveryAcceleration = "scenario-recovery-acceleration";

    public static OperationalSimulationScenariosDto ComposeScenarios(
        OperationalDashboardSummaryDto dashboard,
        OperationalTrendSummaryDto trend,
        OperationalRecoveryPostureDto recovery,
        OperationalRecoveryOutlookDto recoveryOutlook,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalCausalChainsDto chains,
        OperationalSituationRoomDto situationRoom,
        IReadOnlyList<OperationalSimulationSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var scenarios = ComposeScenarioItems(
            trend,
            recovery,
            recoveryOutlook,
            incidentSummary,
            causalitySummary,
            propagation,
            replayPressureVisible: propagation.Propagations.Any(p =>
                string.Equals(p.SourceArea, AreaReplay, StringComparison.OrdinalIgnoreCase)),
            runtimePressureVisible: dashboard.Pressure.ProtectiveModeActive
                || dashboard.Pressure.RuntimeSaturationIndicated,
            inventoryDriftVisible: propagation.Propagations.Any(p =>
                string.Equals(p.SourceArea, AreaInventory, StringComparison.OrdinalIgnoreCase)),
            reconciliationPressureVisible: propagation.Propagations.Any(p =>
                string.Equals(p.SourceArea, AreaReconciliation, StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.TargetArea, AreaReconciliation, StringComparison.OrdinalIgnoreCase)));

        var stabilizationPaths = ComposeStabilizationPaths(
            recovery,
            recoveryOutlook,
            propagation,
            causalitySummary,
            situationRoom,
            chains);

        var degradationPaths = ComposeDegradationPaths(
            propagation,
            recovery,
            incidentSummary,
            causalitySummary);

        var leveragePoints = ComposeLeveragePoints(
            propagation,
            recovery,
            chains,
            incidentSummary,
            causalitySummary);

        if (priorSnapshots.Count >= 2)
        {
            var previous = priorSnapshots[^1];
            if (previous.DegradationScenarioCount > scenarios.Count(s =>
                    s.ScenarioType == OperationalSimulationScenarioType.Degradation))
            {
                scenarios = scenarios
                    .OrderByDescending(s => s.ScenarioType == OperationalSimulationScenarioType.Degradation)
                    .ThenBy(s => s.ScenarioId, StringComparer.Ordinal)
                    .Take(MaxScenarios)
                    .ToList();
            }
        }

        return new OperationalSimulationScenariosDto
        {
            GeneratedAtUtc = generatedAtUtc,
            ScenarioCount = scenarios.Count,
            StabilizationPathCount = stabilizationPaths.Count,
            DegradationPathCount = degradationPaths.Count,
            LeveragePointCount = leveragePoints.Count,
            Scenarios = scenarios,
            StabilizationPaths = stabilizationPaths,
            DegradationPaths = degradationPaths,
            LeveragePoints = leveragePoints
        };
    }

    public static OperationalSimulationSummaryDto ComposeSummary(
        OperationalSimulationScenariosDto scenarios,
        OperationalSituationRoomDto situationRoom,
        OperationalRecoveryPostureDto recovery,
        OperationalPropagationAnalysisDto propagation,
        DateTime generatedAtUtc)
    {
        var stabilizationCount = scenarios.Scenarios.Count(s =>
            s.ScenarioType == OperationalSimulationScenarioType.Stabilization
            || s.ScenarioType == OperationalSimulationScenarioType.RecoveryAcceleration);
        var degradationCount = scenarios.Scenarios.Count(s =>
            s.ScenarioType == OperationalSimulationScenarioType.Degradation);

        var highestLeverage = scenarios.LeveragePoints
            .OrderByDescending(l => l.LeverageStrength)
            .ThenBy(l => l.Area, StringComparer.Ordinal)
            .FirstOrDefault()?.Area
            ?? situationRoom.RecommendedOperationalFocus;

        var dominantConstraint = propagation.StabilizationBlockers
            .OrderByDescending(b => b.PreventingRecovery)
            .ThenBy(b => b.Area, StringComparer.Ordinal)
            .FirstOrDefault()?.Description
            ?? situationRoom.OutlookDetail.DominantConstraint;

        var acceleration = scenarios.StabilizationPaths
            .OrderByDescending(p => p.RecoveryAccelerationPotential)
            .ThenBy(p => p.PathId, StringComparer.Ordinal)
            .FirstOrDefault()?.RecoveryAccelerationPotential
            ?? OperationalLeverageStrength.Minimal;

        var summary =
            $"{scenarios.ScenarioCount} hypothetical scenario(s) evaluated. " +
            $"Highest leverage area: {highestLeverage.ToLowerInvariant()}. " +
            $"Dominant constraint: {dominantConstraint.ToLowerInvariant()}. " +
            $"Recovery confidence remains {recovery.OverallConfidence.ToString().ToLowerInvariant()}.";

        return new OperationalSimulationSummaryDto
        {
            GeneratedAtUtc = generatedAtUtc,
            ActiveSimulationCount = scenarios.ScenarioCount,
            StabilizationScenarioCount = stabilizationCount,
            DegradationScenarioCount = degradationCount,
            HighestLeverageArea = highestLeverage,
            DominantOperationalConstraint = dominantConstraint,
            RecoveryAccelerationPotential = acceleration,
            OperatorAttentionLevel = situationRoom.AttentionLevel,
            Summary = summary
        };
    }

    public static OperationalSimulationOutlookDto ComposeOutlook(
        OperationalSimulationScenariosDto scenarios,
        OperationalSituationRoomDto situationRoom,
        OperationalRecoveryPostureDto recovery,
        OperationalPropagationAnalysisDto propagation)
    {
        var mostLikelyStabilization = scenarios.StabilizationPaths
            .OrderByDescending(p => p.StabilizationConfidence)
            .ThenByDescending(p => p.RecoveryAccelerationPotential)
            .ThenBy(p => p.PathId, StringComparer.Ordinal)
            .FirstOrDefault()?.OperatorSummary
            ?? "No active stabilization path identified";

        var highestRiskDegradation = scenarios.DegradationPaths
            .OrderByDescending(p => p.OperationalSeverity)
            .ThenByDescending(p => p.EscalationRisk)
            .ThenBy(p => p.PathId, StringComparer.Ordinal)
            .FirstOrDefault()?.OperatorSummary
            ?? "No active degradation path identified";

        var dominantConstraint = propagation.StabilizationBlockers
            .OrderByDescending(b => b.PreventingRecovery)
            .ThenBy(b => b.Area, StringComparer.Ordinal)
            .FirstOrDefault()?.Description
            ?? situationRoom.OutlookDetail.DominantConstraint;

        var strongestLeverage = scenarios.LeveragePoints
            .OrderByDescending(l => l.LeverageStrength)
            .ThenBy(l => l.Area, StringComparer.Ordinal)
            .FirstOrDefault()?.Area
            ?? AreaOperational;

        var trajectory = MapRecoveryTrajectory(situationRoom.StabilizationDirection, recovery);

        return new OperationalSimulationOutlookDto
        {
            MostLikelyStabilizationPath = mostLikelyStabilization,
            HighestRiskDegradationPath = highestRiskDegradation,
            DominantConstraint = dominantConstraint,
            StrongestLeveragePoint = strongestLeverage,
            PlatformRecoveryTrajectory = trajectory,
            OperationalConfidence = recovery.OverallConfidence
        };
    }

    public static OperationalSimulationSnapshot CreateSnapshot(
        OperationalSimulationScenariosDto scenarios,
        OperationalSimulationSummaryDto summary)
    {
        return new OperationalSimulationSnapshot
        {
            GeneratedAtUtc = summary.GeneratedAtUtc,
            ScenarioCount = scenarios.ScenarioCount,
            StabilizationScenarioCount = summary.StabilizationScenarioCount,
            DegradationScenarioCount = summary.DegradationScenarioCount,
            HighestLeverageArea = summary.HighestLeverageArea,
            DominantConstraint = summary.DominantOperationalConstraint,
            OperatorSummary = summary.Summary
        };
    }

    private static List<OperationalSimulationScenarioDto> ComposeScenarioItems(
        OperationalTrendSummaryDto trend,
        OperationalRecoveryPostureDto recovery,
        OperationalRecoveryOutlookDto recoveryOutlook,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        bool replayPressureVisible,
        bool runtimePressureVisible,
        bool inventoryDriftVisible,
        bool reconciliationPressureVisible)
    {
        var items = new List<(int Priority, OperationalSimulationScenarioDto Scenario)>();

        if (replayPressureVisible)
        {
            var collapsing = propagation.Propagations.Any(p =>
                p.IsCollapsing && string.Equals(p.SourceArea, AreaReplay, StringComparison.OrdinalIgnoreCase));

            items.Add((1, new OperationalSimulationScenarioDto
            {
                ScenarioId = ScenarioReplayStabilization,
                ScenarioType = OperationalSimulationScenarioType.Stabilization,
                TargetArea = AreaReplay,
                TriggerCondition = "Replay pressure decreases and propagation collapses",
                ExpectedOperationalDirection = collapsing
                    ? OperationalSimulationDirection.Stabilizing
                    : OperationalSimulationDirection.Improving,
                RecoveryImpact = "Reconciliation pressure likely improves; runtime survivability likely stabilizes",
                EscalationImpact = "Escalating propagation likely reduces across downstream areas",
                StabilizationLikelihood = collapsing
                    ? OperationalSimulationSeverity.High
                    : OperationalSimulationSeverity.Elevated,
                Confidence = MapRecoveryConfidence(recovery.OverallConfidence),
                OperatorInterpretation =
                    "If replay pressure stabilizes, reconciliation visibility and runtime survivability likely improve next",
                RecommendedOperatorFocus = "Replay workbench stabilization monitoring"
            }));
        }

        if (runtimePressureVisible)
        {
            var escalating = propagation.Propagations.Any(p =>
                p.IsEscalating
                && (string.Equals(p.SourceArea, AreaRuntime, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(p.TargetArea, AreaRuntime, StringComparison.OrdinalIgnoreCase)));

            items.Add((2, new OperationalSimulationScenarioDto
            {
                ScenarioId = ScenarioRuntimeEscalation,
                ScenarioType = OperationalSimulationScenarioType.Degradation,
                TargetArea = AreaRuntime,
                TriggerCondition = "Protective mode worsens and survivability declines",
                ExpectedOperationalDirection = OperationalSimulationDirection.Escalating,
                RecoveryImpact = "Recovery confidence likely degrades across platform domains",
                EscalationImpact = "Replay instability likely spreads; incident escalation increases",
                StabilizationLikelihood = OperationalSimulationSeverity.Normal,
                Confidence = OperationalSimulationConfidence.Elevated,
                OperatorInterpretation =
                    "If runtime pressure escalates, replay instability likely spreads and recovery confidence degrades",
                RecommendedOperatorFocus = "Runtime protection review and survivability monitoring"
            }));

            if (!escalating)
            {
                items.Add((3, new OperationalSimulationScenarioDto
                {
                    ScenarioId = "scenario-runtime-stabilization",
                    ScenarioType = OperationalSimulationScenarioType.Stabilization,
                    TargetArea = AreaRuntime,
                    TriggerCondition = "Runtime saturation eases and protective containment relaxes",
                    ExpectedOperationalDirection = OperationalSimulationDirection.Stabilizing,
                    RecoveryImpact = "Platform recovery confidence likely improves",
                    EscalationImpact = "Downstream escalation pressure likely reduces",
                    StabilizationLikelihood = OperationalSimulationSeverity.Elevated,
                    Confidence = MapRecoveryConfidence(recovery.OverallConfidence),
                    OperatorInterpretation =
                        "If runtime survivability stabilizes, downstream replay and reconciliation pressure likely eases",
                    RecommendedOperatorFocus = "Runtime survivability monitoring"
                }));
            }
        }

        if (inventoryDriftVisible)
        {
            items.Add((4, new OperationalSimulationScenarioDto
            {
                ScenarioId = ScenarioInventoryStabilization,
                ScenarioType = OperationalSimulationScenarioType.Stabilization,
                TargetArea = AreaInventory,
                TriggerCondition = "Drift hotspots collapse and reconciliation alignment improves",
                ExpectedOperationalDirection = OperationalSimulationDirection.Improving,
                RecoveryImpact = "Stabilization outlook improves; operational volatility decreases",
                EscalationImpact = "Inventory-to-reconciliation escalation likely reduces",
                StabilizationLikelihood = OperationalSimulationSeverity.Elevated,
                Confidence = MapRecoveryConfidence(recovery.OverallConfidence),
                OperatorInterpretation =
                    "If inventory drift stabilizes, reconciliation alignment and operational volatility likely improve",
                RecommendedOperatorFocus = "Inventory drift hotspot resolution"
            }));
        }

        if (reconciliationPressureVisible)
        {
            items.Add((5, new OperationalSimulationScenarioDto
            {
                ScenarioId = ScenarioReconciliationStabilization,
                ScenarioType = OperationalSimulationScenarioType.Stabilization,
                TargetArea = AreaReconciliation,
                TriggerCondition = "Escalating conflicts reduce and queue pressure eases",
                ExpectedOperationalDirection = OperationalSimulationDirection.Stabilizing,
                RecoveryImpact = "Replay recovery alignment likely improves",
                EscalationImpact = "Cross-domain escalation likely moderates",
                StabilizationLikelihood = OperationalSimulationSeverity.Elevated,
                Confidence = MapRecoveryConfidence(recovery.OverallConfidence),
                OperatorInterpretation =
                    "If reconciliation pressure stabilizes, replay recovery alignment likely improves",
                RecommendedOperatorFocus = "Reconciliation queue triage"
            }));

            items.Add((6, new OperationalSimulationScenarioDto
            {
                ScenarioId = ScenarioReconciliationDegradation,
                ScenarioType = OperationalSimulationScenarioType.Degradation,
                TargetArea = AreaReconciliation,
                TriggerCondition = "Escalating conflicts increase and queue pressure expands",
                ExpectedOperationalDirection = OperationalSimulationDirection.Degrading,
                RecoveryImpact = "Recovery convergence likely slows across operational domains",
                EscalationImpact = "Replay and inventory pressure may amplify",
                StabilizationLikelihood = OperationalSimulationSeverity.Normal,
                Confidence = OperationalSimulationConfidence.Moderate,
                OperatorInterpretation =
                    "If reconciliation pressure worsens, replay and inventory instability may amplify",
                RecommendedOperatorFocus = "Reconciliation escalation review"
            }));
        }

        if (trend.OverallDirection == OperationalTrendDirection.Degrading
            || incidentSummary.EscalatingIncidentCount >= 2)
        {
            items.Add((7, new OperationalSimulationScenarioDto
            {
                ScenarioId = ScenarioVolatilityDegradation,
                ScenarioType = OperationalSimulationScenarioType.Degradation,
                TargetArea = AreaOperational,
                TriggerCondition = "Operational volatility cycle continues without convergence",
                ExpectedOperationalDirection = OperationalSimulationDirection.Degrading,
                RecoveryImpact = "Platform recovery confidence likely degrades",
                EscalationImpact = "Incident escalation and cross-domain propagation likely increase",
                StabilizationLikelihood = OperationalSimulationSeverity.Normal,
                Confidence = OperationalSimulationConfidence.Moderate,
                OperatorInterpretation =
                    "If operational volatility persists, incident escalation and cross-domain propagation likely increase",
                RecommendedOperatorFocus = "Cross-domain operational monitoring"
            }));
        }

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving or OperationalRecoveryDirection.Converging)
        {
            items.Add((8, new OperationalSimulationScenarioDto
            {
                ScenarioId = ScenarioRecoveryAcceleration,
                ScenarioType = OperationalSimulationScenarioType.RecoveryAcceleration,
                TargetArea = causalitySummary.DominantOperationalArea,
                TriggerCondition = "Dominant constraint eases and convergence signals align",
                ExpectedOperationalDirection = OperationalSimulationDirection.Improving,
                RecoveryImpact = recoveryOutlook.Summary,
                EscalationImpact = "Escalation pressure likely collapses across active propagations",
                StabilizationLikelihood = OperationalSimulationSeverity.High,
                Confidence = OperationalSimulationConfidence.High,
                OperatorInterpretation =
                    "If the dominant constraint eases, platform recovery acceleration likely improves",
                RecommendedOperatorFocus = situationRoomFocus(recoveryOutlook.Summary)
            }));
        }

        if (items.Count == 0)
        {
            items.Add((99, new OperationalSimulationScenarioDto
            {
                ScenarioId = "scenario-stable-baseline",
                ScenarioType = OperationalSimulationScenarioType.LeverageReview,
                TargetArea = AreaOperational,
                TriggerCondition = "Platform remains within advisory stability bounds",
                ExpectedOperationalDirection = OperationalSimulationDirection.Stable,
                RecoveryImpact = "Recovery posture likely remains stable",
                EscalationImpact = "Escalation pressure likely remains contained",
                StabilizationLikelihood = OperationalSimulationSeverity.Normal,
                Confidence = OperationalSimulationConfidence.Moderate,
                OperatorInterpretation = "Platform operating within expected bounds; no active hypothetical escalation",
                RecommendedOperatorFocus = "Routine operational monitoring"
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Scenario.ScenarioId, StringComparer.Ordinal)
            .Take(MaxScenarios)
            .Select(i => i.Scenario)
            .ToList();
    }

    private static string situationRoomFocus(string recoveryOutlookSummary) =>
        string.IsNullOrWhiteSpace(recoveryOutlookSummary)
            ? "Stabilization monitoring"
            : "Stabilization monitoring aligned with recovery outlook";

    private static List<OperationalStabilizationPathDto> ComposeStabilizationPaths(
        OperationalRecoveryPostureDto recovery,
        OperationalRecoveryOutlookDto recoveryOutlook,
        OperationalPropagationAnalysisDto propagation,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalSituationRoomDto situationRoom,
        OperationalCausalChainsDto chains)
    {
        var items = new List<(int Priority, OperationalStabilizationPathDto Path)>();

        TryAddStabilizationPath(
            items,
            1,
            "path-replay-stabilization",
            AreaReplay,
            new[] { AreaReplay, AreaReconciliation, AreaRuntime, AreaOperational },
            propagation,
            recovery,
            "Replay pressure remains the upstream constraint",
            OperationalLeverageStrength.Strong,
            "Replay stabilization likely accelerates reconciliation and runtime recovery");

        TryAddStabilizationPath(
            items,
            2,
            "path-runtime-stabilization",
            AreaRuntime,
            new[] { AreaRuntime, AreaReplay, AreaOperational },
            propagation,
            recovery,
            "Runtime survivability limits downstream recovery",
            OperationalLeverageStrength.Critical,
            "Runtime stabilization likely improves platform survivability and replay alignment");

        TryAddStabilizationPath(
            items,
            3,
            "path-inventory-stabilization",
            AreaInventory,
            new[] { AreaInventory, AreaReconciliation, AreaOperational },
            propagation,
            recovery,
            "Inventory drift hotspots remain unresolved",
            OperationalLeverageStrength.Moderate,
            "Inventory drift resolution likely reduces reconciliation escalation");

        TryAddStabilizationPath(
            items,
            4,
            "path-reconciliation-stabilization",
            AreaReconciliation,
            new[] { AreaReconciliation, AreaReplay, AreaOperational },
            propagation,
            recovery,
            "Reconciliation queue pressure remains elevated",
            OperationalLeverageStrength.Moderate,
            "Reconciliation stabilization likely moderates cross-domain escalation");

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving or OperationalRecoveryDirection.Converging)
        {
            items.Add((5, new OperationalStabilizationPathDto
            {
                PathId = "path-recovery-acceleration",
                DominantArea = causalitySummary.DominantOperationalArea,
                ExpectedImprovementSequence = new[]
                {
                    causalitySummary.DominantOperationalArea,
                    AreaReconciliation,
                    AreaOperational
                }.Take(MaxImprovementSequenceSteps).ToList(),
                BlockingConstraint = situationRoom.OutlookDetail.DominantConstraint,
                RecoveryAccelerationPotential = OperationalLeverageStrength.Strong,
                StabilizationConfidence = OperationalSimulationConfidence.High,
                EstimatedOperationalImpact = recoveryOutlook.Summary,
                OperatorSummary = "Recovery acceleration path appears viable if dominant constraint eases"
            }));
        }

        if (chains.ChainCount == 0 && items.Count == 0)
        {
            items.Add((99, new OperationalStabilizationPathDto
            {
                PathId = "path-stable-monitoring",
                DominantArea = AreaOperational,
                ExpectedImprovementSequence = new[] { AreaOperational },
                BlockingConstraint = "No active blocker identified",
                RecoveryAccelerationPotential = OperationalLeverageStrength.Minimal,
                StabilizationConfidence = OperationalSimulationConfidence.Moderate,
                EstimatedOperationalImpact = "Routine monitoring sufficient",
                OperatorSummary = "No active stabilization path required; continue routine monitoring"
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Path.PathId, StringComparer.Ordinal)
            .Take(MaxStabilizationPaths)
            .Select(i => i.Path)
            .ToList();
    }

    private static void TryAddStabilizationPath(
        List<(int Priority, OperationalStabilizationPathDto Path)> items,
        int priority,
        string pathId,
        string area,
        IReadOnlyList<string> sequence,
        OperationalPropagationAnalysisDto propagation,
        OperationalRecoveryPostureDto recovery,
        string defaultConstraint,
        OperationalLeverageStrength leverage,
        string impact)
    {
        var relevant = propagation.Propagations.Any(p =>
            string.Equals(p.SourceArea, area, StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.TargetArea, area, StringComparison.OrdinalIgnoreCase));

        var blocker = propagation.StabilizationBlockers
            .FirstOrDefault(b => string.Equals(b.Area, area, StringComparison.OrdinalIgnoreCase))
            ?.Description
            ?? defaultConstraint;

        if (!relevant && recovery.Convergence.All(c =>
                !string.Equals(c.Domain, area, StringComparison.OrdinalIgnoreCase)))
            return;

        items.Add((priority, new OperationalStabilizationPathDto
        {
            PathId = pathId,
            DominantArea = area,
            ExpectedImprovementSequence = sequence.Take(MaxImprovementSequenceSteps).ToList(),
            BlockingConstraint = blocker,
            RecoveryAccelerationPotential = leverage,
            StabilizationConfidence = MapRecoveryConfidence(recovery.OverallConfidence),
            EstimatedOperationalImpact = impact,
            OperatorSummary = $"Stabilizing {area.ToLowerInvariant()} appears to be a high-leverage recovery path"
        }));
    }

    private static List<OperationalDegradationPathDto> ComposeDegradationPaths(
        OperationalPropagationAnalysisDto propagation,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary)
    {
        var items = new List<(int Priority, OperationalDegradationPathDto Path)>();

        foreach (var p in propagation.Propagations.Where(x => x.IsEscalating).OrderBy(x => x.SourceArea, StringComparer.Ordinal))
        {
            items.Add((1, new OperationalDegradationPathDto
            {
                PathId = $"degrade-{p.SourceArea.ToLowerInvariant()}-{p.TargetArea.ToLowerInvariant()}",
                SourceArea = p.SourceArea,
                ExpectedPropagation = p.OperatorInterpretation,
                EscalationRisk = OperationalSimulationSeverity.High,
                RecoveryRisk = recovery.OverallSeverity >= OperationalRecoverySeverity.High
                    ? OperationalSimulationSeverity.High
                    : OperationalSimulationSeverity.Elevated,
                DownstreamAreas = new[] { p.TargetArea, AreaOperational }.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                OperationalSeverity = OperationalSimulationSeverity.High,
                OperatorSummary = $"Escalation from {p.SourceArea.ToLowerInvariant()} toward {p.TargetArea.ToLowerInvariant()} may degrade recovery"
            }));
        }

        if (incidentSummary.EscalatingIncidentCount >= 1)
        {
            items.Add((2, new OperationalDegradationPathDto
            {
                PathId = "degrade-incident-escalation",
                SourceArea = causalitySummary.DominantOperationalArea,
                ExpectedPropagation = "Active incident escalation may spread operational pressure",
                EscalationRisk = OperationalSimulationSeverity.High,
                RecoveryRisk = OperationalSimulationSeverity.Elevated,
                DownstreamAreas = new[] { AreaOperational, AreaReconciliation },
                OperationalSeverity = MapIncidentSeverity(incidentSummary.HighestSeverity),
                OperatorSummary = "Incident escalation may amplify cross-domain operational pressure"
            }));
        }

        if (recovery.OverallDirection is OperationalRecoveryDirection.Degrading or OperationalRecoveryDirection.Diverging)
        {
            items.Add((3, new OperationalDegradationPathDto
            {
                PathId = "degrade-recovery-divergence",
                SourceArea = AreaOperational,
                ExpectedPropagation = "Recovery divergence may expand across active operational domains",
                EscalationRisk = OperationalSimulationSeverity.Elevated,
                RecoveryRisk = OperationalSimulationSeverity.High,
                DownstreamAreas = new[] { AreaReplay, AreaReconciliation, AreaRuntime },
                OperationalSeverity = OperationalSimulationSeverity.Elevated,
                OperatorSummary = "Recovery divergence may expand instability across active domains"
            }));
        }

        if (items.Count == 0)
        {
            items.Add((99, new OperationalDegradationPathDto
            {
                PathId = "degrade-none-active",
                SourceArea = AreaOperational,
                ExpectedPropagation = "No active degradation propagation detected",
                EscalationRisk = OperationalSimulationSeverity.Normal,
                RecoveryRisk = OperationalSimulationSeverity.Normal,
                DownstreamAreas = Array.Empty<string>(),
                OperationalSeverity = OperationalSimulationSeverity.Normal,
                OperatorSummary = "No active degradation path identified under current conditions"
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Path.PathId, StringComparer.Ordinal)
            .Take(MaxDegradationPaths)
            .Select(i => i.Path)
            .ToList();
    }

    private static List<OperationalLeveragePointDto> ComposeLeveragePoints(
        OperationalPropagationAnalysisDto propagation,
        OperationalRecoveryPostureDto recovery,
        OperationalCausalChainsDto chains,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary)
    {
        var areas = new[] { AreaReplay, AreaRuntime, AreaInventory, AreaReconciliation, AreaOperational };
        var items = new List<OperationalLeveragePointDto>();

        foreach (var area in areas)
        {
            var escalating = propagation.Propagations.Count(p =>
                (string.Equals(p.SourceArea, area, StringComparison.OrdinalIgnoreCase)
                 || string.Equals(p.TargetArea, area, StringComparison.OrdinalIgnoreCase))
                && p.IsEscalating);
            var chainCount = chains.Chains.Count(c =>
                string.Equals(c.DominantArea, area, StringComparison.OrdinalIgnoreCase));
            var convergence = recovery.Convergence.FirstOrDefault(c =>
                string.Equals(c.Domain, area, StringComparison.OrdinalIgnoreCase));

            var strength = ResolveLeverageStrength(area, escalating, chainCount, recovery, incidentSummary);
            if (strength == OperationalLeverageStrength.Minimal && area != AreaOperational && escalating == 0 && chainCount == 0)
                continue;

            items.Add(new OperationalLeveragePointDto
            {
                Area = area,
                LeverageStrength = strength,
                RecoveryInfluence = DescribeRecoveryInfluence(area, convergence, recovery),
                StabilizationInfluence = DescribeStabilizationInfluence(area, escalating, propagation),
                EscalationInfluence = escalating >= 2
                    ? "Strong escalation influence across downstream areas"
                    : escalating == 1
                        ? "Moderate escalation influence on downstream areas"
                        : "Limited escalation influence under current conditions",
                DownstreamImpact = DescribeDownstreamImpact(area, propagation),
                OperatorPriorityReason = DescribePriorityReason(area, strength, causalitySummary)
            });
        }

        return items
            .OrderByDescending(l => l.LeverageStrength)
            .ThenBy(l => l.Area, StringComparer.Ordinal)
            .Take(MaxLeveragePoints)
            .ToList();
    }

    private static OperationalLeverageStrength ResolveLeverageStrength(
        string area,
        int escalating,
        int chainCount,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary)
    {
        if (string.Equals(area, AreaRuntime, StringComparison.Ordinal)
            && recovery.OverallSeverity >= OperationalRecoverySeverity.Critical)
            return OperationalLeverageStrength.Critical;

        if (string.Equals(area, AreaReplay, StringComparison.Ordinal) && (escalating >= 1 || chainCount >= 1))
            return OperationalLeverageStrength.Strong;

        if (escalating >= 2 || chainCount >= 2)
            return OperationalLeverageStrength.Strong;

        if (escalating >= 1 || chainCount >= 1 || incidentSummary.ActiveIncidentCount >= 1)
            return OperationalLeverageStrength.Moderate;

        return OperationalLeverageStrength.Minimal;
    }

    private static string DescribeRecoveryInfluence(
        string area,
        OperationalRecoveryConvergenceDto? convergence,
        OperationalRecoveryPostureDto recovery)
    {
        if (convergence != null && !string.IsNullOrWhiteSpace(convergence.Summary))
            return convergence.Summary;

        return string.Equals(area, AreaReplay, StringComparison.Ordinal)
            ? "Replay pressure strongly influences downstream recovery"
            : string.Equals(area, AreaRuntime, StringComparison.Ordinal)
                ? "Runtime pressure heavily affects survivability and recovery confidence"
                : "Moderate recovery influence under current conditions";
    }

    private static string DescribeStabilizationInfluence(
        string area,
        int escalating,
        OperationalPropagationAnalysisDto propagation)
    {
        var collapsing = propagation.Propagations.Any(p =>
            p.IsCollapsing
            && string.Equals(p.SourceArea, area, StringComparison.OrdinalIgnoreCase));

        if (collapsing)
            return $"{area} stabilization likely collapses active escalation pressure";

        if (string.Equals(area, AreaReconciliation, StringComparison.Ordinal))
            return "Reconciliation stabilization moderately reduces escalation";

        return escalating >= 1
            ? $"{area} stabilization required to reduce active propagation"
            : $"{area} stabilization contributes to platform recovery alignment";
    }

    private static string DescribeDownstreamImpact(
        string area,
        OperationalPropagationAnalysisDto propagation)
    {
        var targets = propagation.Propagations
            .Where(p => string.Equals(p.SourceArea, area, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.TargetArea)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

        return targets.Count == 0
            ? "Limited downstream propagation under current conditions"
            : $"Downstream impact likely on {string.Join(", ", targets).ToLowerInvariant()}";
    }

    private static string DescribePriorityReason(
        string area,
        OperationalLeverageStrength strength,
        OperationalCausalitySummaryDto causalitySummary)
    {
        if (string.Equals(area, causalitySummary.DominantOperationalArea, StringComparison.OrdinalIgnoreCase))
            return $"{area} is the dominant operational area with {strength.ToString().ToLowerInvariant()} leverage";

        return strength >= OperationalLeverageStrength.Strong
            ? $"{area} appears to be a high-leverage stabilization point"
            : $"{area} contributes to platform stabilization with bounded leverage";
    }

    private static OperationalSimulationDirection MapRecoveryTrajectory(
        OperationalSituationDirection direction,
        OperationalRecoveryPostureDto recovery)
    {
        if (direction == OperationalSituationDirection.Improving
            || recovery.OverallDirection is OperationalRecoveryDirection.Improving or OperationalRecoveryDirection.Converging)
            return OperationalSimulationDirection.Improving;

        if (direction == OperationalSituationDirection.Escalating)
            return OperationalSimulationDirection.Escalating;

        if (direction == OperationalSituationDirection.Degrading
            || recovery.OverallDirection is OperationalRecoveryDirection.Degrading or OperationalRecoveryDirection.Diverging)
            return OperationalSimulationDirection.Degrading;

        if (direction == OperationalSituationDirection.Stabilizing)
            return OperationalSimulationDirection.Stabilizing;

        return OperationalSimulationDirection.Stable;
    }

    private static OperationalSimulationConfidence MapRecoveryConfidence(OperationalRecoveryConfidence confidence) =>
        confidence switch
        {
            OperationalRecoveryConfidence.High => OperationalSimulationConfidence.High,
            OperationalRecoveryConfidence.Elevated => OperationalSimulationConfidence.Elevated,
            OperationalRecoveryConfidence.Moderate => OperationalSimulationConfidence.Moderate,
            _ => OperationalSimulationConfidence.Low
        };

    private static OperationalSimulationSeverity MapIncidentSeverity(OperationalIncidentSeverity severity) =>
        severity switch
        {
            OperationalIncidentSeverity.Critical => OperationalSimulationSeverity.Critical,
            OperationalIncidentSeverity.High => OperationalSimulationSeverity.High,
            OperationalIncidentSeverity.Elevated => OperationalSimulationSeverity.Elevated,
            _ => OperationalSimulationSeverity.Normal
        };
}
