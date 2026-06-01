using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Application.OperationalConvergence;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTopology;

namespace Tannous.Pos.Application.OperationalResilience;

/// <summary>Deterministic operational resilience and survivability intelligence from bounded cognition continuity.</summary>
public static class OperationalResilienceAggregation
{
    public const int MaxSurvivabilityAnalyses = 8;
    public const int MaxFragilities = 8;
    public const int MaxContainmentDurabilities = 8;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;

    public const string AreaReplay = "Replay";
    public const string AreaRuntime = "Runtime";
    public const string AreaReconciliation = "Reconciliation";
    public const string AreaOperational = "Operational Stability";

    public static OperationalResilienceReportDto ComposeResilienceReport(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPatternSummaryDto patternSummary,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalIntegrityReportDto integrityReport,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        bool protectiveContainmentActive,
        IReadOnlyList<OperationalResilienceCognitionSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var dominantArea = NormalizeArea(topology.HighestInfluenceArea);
        if (string.IsNullOrWhiteSpace(dominantArea) || AreasMatch(dominantArea, AreaOperational))
            dominantArea = NormalizeArea(convergenceReport.HighestAmbiguityArea);

        var fragilities = ComposeFragilities(
            dominantArea,
            recovery,
            situationRoom,
            simulationSummary,
            patternSummary,
            evolutionTimeline,
            integrityReport,
            topology,
            convergenceReport,
            protectiveContainmentActive);

        var survivabilityAnalyses = ComposeSurvivabilityAnalyses(
            dominantArea,
            recovery,
            situationRoom,
            simulationSummary,
            evolutionTimeline,
            topology,
            convergenceReport,
            fragilities);

        var containmentDurabilities = ComposeContainmentDurabilities(
            dominantArea,
            recovery,
            situationRoom,
            evolutionTimeline,
            topology,
            protectiveContainmentActive);

        var continuity = ComposeResilienceContinuity(
            dominantArea,
            recovery,
            situationRoom,
            evolutionTimeline,
            convergenceReport,
            fragilities,
            priorSnapshots);

        var survivabilityState = ResolveSurvivabilityState(
            recovery,
            situationRoom,
            evolutionTimeline,
            integrityReport,
            topology,
            convergenceReport,
            fragilities);

        var stabilizationDurability = DescribeStabilizationDurability(
            recovery,
            situationRoom,
            evolutionTimeline,
            convergenceReport,
            integrityReport);

        var escalationFragility = DescribeEscalationFragility(
            situationRoom,
            evolutionTimeline,
            topology,
            convergenceReport,
            fragilities);

        var recoverySustainability = DescribeRecoverySustainability(
            recovery,
            convergenceReport,
            evolutionTimeline,
            fragilities);

        var containmentStrength = DescribeContainmentStrength(
            protectiveContainmentActive,
            situationRoom,
            containmentDurabilities);

        var highestFragility = fragilities
            .OrderByDescending(f => f.FragilitySeverity)
            .ThenBy(f => f.OperationalArea, StringComparer.Ordinal)
            .FirstOrDefault()?.OperationalArea
            ?? dominantArea;

        var operatorSummary = ComposeOperatorSummary(
            survivabilityState,
            stabilizationDurability,
            escalationFragility,
            recoverySustainability,
            highestFragility,
            priorSnapshots);

        return new OperationalResilienceReportDto
        {
            GeneratedAtUtc = generatedAtUtc,
            SurvivabilityState = survivabilityState,
            StabilizationDurability = stabilizationDurability,
            EscalationFragility = escalationFragility,
            RecoverySustainability = recoverySustainability,
            ContainmentStrength = containmentStrength,
            HighestFragilityArea = highestFragility,
            SurvivabilityAnalyses = survivabilityAnalyses,
            ContainmentDurabilities = containmentDurabilities,
            ResilienceContinuity = continuity,
            OperatorSummary = operatorSummary
        };
    }

    public static OperationalResiliencePostureSummaryDto ComposeResilienceSummary(
        OperationalResilienceReportDto report,
        OperationalSituationRoomDto situationRoom,
        IReadOnlyList<OperationalFragilityDto> fragilities,
        DateTime generatedAtUtc)
    {
        var dominantArea = report.SurvivabilityAnalyses
            .OrderByDescending(a => a.SurvivabilityStrength)
            .FirstOrDefault()?.OperationalArea
            ?? report.HighestFragilityArea;

        var highestFragilityPressure = fragilities
            .OrderByDescending(f => f.FragilitySeverity)
            .FirstOrDefault()?.CollapseSensitivity
            ?? report.EscalationFragility;

        var strongestContainment = report.ContainmentDurabilities
            .OrderByDescending(c => c.DurabilityStrength)
            .FirstOrDefault()?.OperatorInterpretation
            ?? report.ContainmentStrength;

        var weakestRecovery = report.SurvivabilityAnalyses
            .OrderBy(a => a.SurvivabilityStrength)
            .FirstOrDefault()?.RecoveryDurability
            ?? report.RecoverySustainability;

        var summary =
            $"Survivability is {report.SurvivabilityState.ToString().ToLowerInvariant()} with stabilization durability " +
            $"{report.StabilizationDurability.ToLowerInvariant()}. Highest fragility: {report.HighestFragilityArea.ToLowerInvariant()}.";

        return new OperationalResiliencePostureSummaryDto
        {
            GeneratedAtUtc = generatedAtUtc,
            DominantResilienceArea = dominantArea,
            HighestFragilityPressure = highestFragilityPressure,
            StrongestContainmentZone = strongestContainment,
            WeakestRecoveryDurability = weakestRecovery,
            OperationalSurvivabilityState = report.SurvivabilityState,
            OperatorAttentionLevel = situationRoom.AttentionLevel.ToString(),
            Summary = summary
        };
    }

    public static IReadOnlyList<OperationalFragilityDto> ComposeFragilities(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPatternSummaryDto patternSummary,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalIntegrityReportDto integrityReport,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        bool protectiveContainmentActive)
    {
        var fragilities = new List<OperationalFragilityDto>();

        if (topology.TopologyState == OperationalTopologyState.EscalationDominant
            || topology.TopologyState == OperationalTopologyState.Concentrated)
        {
            fragilities.Add(new OperationalFragilityDto
            {
                FragilityId = "fragility-dependency-concentration",
                OperationalArea = topology.HighestInfluenceArea,
                FragilityType = OperationalFragilityType.DependencyConcentration,
                FragilitySeverity = OperationalDurabilityStrength.Weak,
                CollapseSensitivity = "Dependency topology concentrated with elevated collapse sensitivity",
                EscalationExposure = topology.EscalationPropagationStrength,
                RecommendedOperatorFocus = "Review upstream critical dependencies before further stabilization"
            });
        }

        if (AreasMatch(dominantArea, AreaRuntime)
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Escalating
                or OperationalSituationDirection.Degrading)
        {
            fragilities.Add(new OperationalFragilityDto
            {
                FragilityId = "fragility-runtime-recurrence",
                OperationalArea = AreaRuntime,
                FragilityType = OperationalFragilityType.EscalationRecurrence,
                FragilitySeverity = OperationalDurabilityStrength.Brittle,
                CollapseSensitivity = "Runtime escalation recurring with survivability pressure",
                EscalationExposure = situationRoom.EscalationSeverity.ToString(),
                RecommendedOperatorFocus = "Stabilize runtime containment before downstream recovery expansion"
            });
        }

        if (protectiveContainmentActive
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Escalating
                or OperationalSituationDirection.Degrading)
        {
            fragilities.Add(new OperationalFragilityDto
            {
                FragilityId = "fragility-containment-instability",
                OperationalArea = dominantArea,
                FragilityType = OperationalFragilityType.ContainmentInstability,
                FragilitySeverity = OperationalDurabilityStrength.Weak,
                CollapseSensitivity = "Protective containment active while stabilization degrading",
                EscalationExposure = "Containment may not sustain continued escalation pressure",
                RecommendedOperatorFocus = "Validate containment durability under current escalation load"
            });
        }

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            && convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Weak
                or OperationalConvergenceStrength.Fragmented)
        {
            fragilities.Add(new OperationalFragilityDto
            {
                FragilityId = "fragility-recovery-brittleness",
                OperationalArea = dominantArea,
                FragilityType = OperationalFragilityType.RecoveryBrittleness,
                FragilitySeverity = OperationalDurabilityStrength.Brittle,
                CollapseSensitivity = "Recovery improving while signal convergence weak or fragmented",
                EscalationExposure = convergenceReport.EscalationConfidence,
                RecommendedOperatorFocus = "Treat recovery as brittle until convergence strengthens"
            });
        }

        if (evolutionTimeline.EscalationMomentum.Contains("expanding", StringComparison.OrdinalIgnoreCase)
            && evolutionTimeline.RecoveryMomentum.Contains("slowing", StringComparison.OrdinalIgnoreCase))
        {
            fragilities.Add(new OperationalFragilityDto
            {
                FragilityId = "fragility-stabilization-weakness",
                OperationalArea = dominantArea,
                FragilityType = OperationalFragilityType.StabilizationWeakness,
                FragilitySeverity = OperationalDurabilityStrength.Weak,
                CollapseSensitivity = "Recovery momentum slowing while escalation expanding",
                EscalationExposure = evolutionTimeline.EscalationMomentum,
                RecommendedOperatorFocus = "Reinforce stabilization before escalation propagation expands"
            });
        }

        if (integrityReport.ContradictionCount >= 2)
        {
            fragilities.Add(new OperationalFragilityDto
            {
                FragilityId = "fragility-integrity-fragmentation",
                OperationalArea = dominantArea,
                FragilityType = OperationalFragilityType.TopologyCollapseSensitivity,
                FragilitySeverity = OperationalDurabilityStrength.Weak,
                CollapseSensitivity = $"{integrityReport.ContradictionCount} integrity contradiction(s) weaken collapse resistance",
                EscalationExposure = integrityReport.AlignmentState,
                RecommendedOperatorFocus = "Resolve cross-layer contradictions to restore resilience coherence"
            });
        }

        if (simulationSummary.DegradationScenarioCount > simulationSummary.StabilizationScenarioCount)
        {
            fragilities.Add(new OperationalFragilityDto
            {
                FragilityId = "fragility-simulation-degradation",
                OperationalArea = NormalizeArea(simulationSummary.HighestLeverageArea),
                FragilityType = OperationalFragilityType.StabilizationWeakness,
                FragilitySeverity = OperationalDurabilityStrength.Moderate,
                CollapseSensitivity = "Simulation degradation paths outnumber stabilization paths",
                EscalationExposure = simulationSummary.Summary,
                RecommendedOperatorFocus = "Review simulation leverage before assuming stabilization durability"
            });
        }

        if (patternSummary.RecurringPatternCount > 0
            && patternSummary.EscalationPatternStrength >= patternSummary.RecoveryPatternStrength)
        {
            fragilities.Add(new OperationalFragilityDto
            {
                FragilityId = "fragility-pattern-recurrence",
                OperationalArea = NormalizeArea(patternSummary.HighestRiskPattern),
                FragilityType = OperationalFragilityType.EscalationRecurrence,
                FragilitySeverity = OperationalDurabilityStrength.Moderate,
                CollapseSensitivity = "Recurring escalation pattern exceeds recovery pattern strength",
                EscalationExposure = patternSummary.DominantArchetype,
                RecommendedOperatorFocus = "Address recurring pattern before assuming sustained recovery"
            });
        }

        return fragilities
            .OrderByDescending(f => f.FragilitySeverity)
            .ThenBy(f => f.FragilityId, StringComparer.Ordinal)
            .Take(MaxFragilities)
            .ToList();
    }

    public static OperationalResilienceCognitionSnapshot CreateSnapshot(
        OperationalResilienceReportDto report,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        return new OperationalResilienceCognitionSnapshot
        {
            GeneratedAtUtc = report.GeneratedAtUtc,
            SurvivabilityState = report.SurvivabilityState,
            StabilizationDurability = report.StabilizationDurability,
            EscalationFragility = report.EscalationFragility,
            HighestFragilityArea = report.HighestFragilityArea,
            FragilityCount = fragilities.Count
        };
    }

    private static IReadOnlyList<OperationalSurvivabilityAnalysisDto> ComposeSurvivabilityAnalyses(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        var analyses = new List<OperationalSurvivabilityAnalysisDto>
        {
            BuildSurvivabilityAnalysis(
                dominantArea,
                recovery,
                situationRoom,
                evolutionTimeline,
                topology,
                convergenceReport,
                fragilities.Count(f => AreasMatch(f.OperationalArea, dominantArea)))
        };

        if (!AreasMatch(dominantArea, AreaReplay))
        {
            analyses.Add(BuildSurvivabilityAnalysis(
                AreaReplay,
                recovery,
                situationRoom,
                evolutionTimeline,
                topology,
                convergenceReport,
                fragilities.Count(f => AreasMatch(f.OperationalArea, AreaReplay))));
        }

        if (!AreasMatch(dominantArea, AreaRuntime))
        {
            analyses.Add(BuildSurvivabilityAnalysis(
                AreaRuntime,
                recovery,
                situationRoom,
                evolutionTimeline,
                topology,
                convergenceReport,
                fragilities.Count(f => AreasMatch(f.OperationalArea, AreaRuntime))));
        }

        if (!string.IsNullOrWhiteSpace(simulationSummary.HighestLeverageArea))
        {
            analyses.Add(BuildSurvivabilityAnalysis(
                NormalizeArea(simulationSummary.HighestLeverageArea),
                recovery,
                situationRoom,
                evolutionTimeline,
                topology,
                convergenceReport,
                fragilities.Count(f => AreasMatch(f.OperationalArea, simulationSummary.HighestLeverageArea))));
        }

        return analyses
            .OrderByDescending(a => a.SurvivabilityStrength)
            .ThenBy(a => a.OperationalArea, StringComparer.Ordinal)
            .Take(MaxSurvivabilityAnalyses)
            .ToList();
    }

    private static OperationalSurvivabilityAnalysisDto BuildSurvivabilityAnalysis(
        string area,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        int localFragilityCount)
    {
        var strength = localFragilityCount switch
        {
            0 when convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Strong
                => OperationalDurabilityStrength.Strong,
            0 => OperationalDurabilityStrength.Moderate,
            1 => OperationalDurabilityStrength.Weak,
            _ => OperationalDurabilityStrength.Brittle
        };

        var stabilizationResistance = evolutionTimeline.StabilizationMomentum.Contains("accelerating", StringComparison.OrdinalIgnoreCase)
            ? "Stabilization resistance strengthening"
            : "Stabilization resistance within bounded continuity";

        var escalationResistance = evolutionTimeline.EscalationMomentum.Contains("collapsing", StringComparison.OrdinalIgnoreCase)
            ? "Escalation resistance improving"
            : situationRoom.EscalatingPropagationCount > 0
                ? "Escalation resistance strained by active propagation"
                : "Escalation resistance stable";

        return new OperationalSurvivabilityAnalysisDto
        {
            OperationalArea = area,
            SurvivabilityStrength = strength,
            StabilizationResistance = stabilizationResistance,
            EscalationResistance = escalationResistance,
            DependencyDurability = topology.StabilizationDependencyStrength,
            RecoveryDurability = recovery.OverallDirection.ToString(),
            OperatorInterpretation =
                $"Survivability in {area.ToLowerInvariant()} is {strength.ToString().ToLowerInvariant()} with {localFragilityCount} local fragility signal(s)"
        };
    }

    private static IReadOnlyList<OperationalContainmentDurabilityDto> ComposeContainmentDurabilities(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        bool protectiveContainmentActive)
    {
        var durabilities = new List<OperationalContainmentDurabilityDto>();

        var containmentState = protectiveContainmentActive
            ? situationRoom.StabilizationDirection is OperationalSituationDirection.Escalating
                or OperationalSituationDirection.Degrading
                ? OperationalDurabilityStrength.Weak
                : OperationalDurabilityStrength.Moderate
            : OperationalDurabilityStrength.Strong;

        durabilities.Add(new OperationalContainmentDurabilityDto
        {
            ContainmentArea = dominantArea,
            DurabilityStrength = containmentState,
            StabilizationConsistency = situationRoom.StabilizationDirection.ToString(),
            EscalationContainmentStrength = protectiveContainmentActive
                ? "Protective containment engaged"
                : "Containment within normal operational bounds",
            RecoverySupportStrength = recovery.OverallDirection.ToString(),
            OperatorInterpretation =
                $"Containment durability in {dominantArea.ToLowerInvariant()} is {containmentState.ToString().ToLowerInvariant()}"
        });

        if (AreasMatch(dominantArea, AreaRuntime) || topology.TopologyState == OperationalTopologyState.EscalationDominant)
        {
            durabilities.Add(new OperationalContainmentDurabilityDto
            {
                ContainmentArea = AreaRuntime,
                DurabilityStrength = situationRoom.StabilizationDirection is OperationalSituationDirection.Escalating
                    ? OperationalDurabilityStrength.Brittle
                    : OperationalDurabilityStrength.Moderate,
                StabilizationConsistency = evolutionTimeline.StabilizationMomentum,
                EscalationContainmentStrength = topology.EscalationPropagationStrength,
                RecoverySupportStrength = "Runtime containment governs downstream survivability",
                OperatorInterpretation = "Runtime containment zone durability governs platform survivability posture"
            });
        }

        if (protectiveContainmentActive)
        {
            durabilities.Add(new OperationalContainmentDurabilityDto
            {
                ContainmentArea = AreaOperational,
                DurabilityStrength = OperationalDurabilityStrength.Moderate,
                StabilizationConsistency = "Protective containment continuity active",
                EscalationContainmentStrength = "Failsafe containment supporting escalation absorption",
                RecoverySupportStrength = recovery.OverallDirection.ToString(),
                OperatorInterpretation = "Platform-wide protective containment supporting operational survivability"
            });
        }

        return durabilities
            .OrderByDescending(d => d.DurabilityStrength)
            .ThenBy(d => d.ContainmentArea, StringComparer.Ordinal)
            .Take(MaxContainmentDurabilities)
            .ToList();
    }

    private static OperationalResilienceContinuityDto ComposeResilienceContinuity(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalConvergenceReportDto convergenceReport,
        IReadOnlyList<OperationalFragilityDto> fragilities,
        IReadOnlyList<OperationalResilienceCognitionSnapshot> priorSnapshots)
    {
        var prior = priorSnapshots.LastOrDefault();
        var currentState = ResolveSurvivabilityState(
            recovery,
            situationRoom,
            evolutionTimeline,
            new OperationalIntegrityReportDto(),
            new OperationalTopologyDto(),
            convergenceReport,
            fragilities);

        var resilienceShift = prior != null && prior.SurvivabilityState != currentState
            ? OperationalContinuityPhrasing.MovedFromTo(
                "Survivability",
                prior.SurvivabilityState.ToString().ToLowerInvariant(),
                currentState.ToString().ToLowerInvariant())
            : OperationalContinuityPhrasing.ConsistentAcrossBoundedWindow("Survivability posture");

        var survivabilityConsistency = convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Strong
                or OperationalConvergenceStrength.Moderate
            ? "Survivability signals aligned with convergence continuity"
            : "Survivability strained by convergence fragmentation";

        var fragilityConsistency = OperationalContinuityPhrasing.SignalCountSustainedInBoundedWindow(
            fragilities.Count,
            "fragility");

        var recoveryAlignment = OperationalContinuityPhrasing.RecoveryAlignment(
            recovery,
            "Recovery durability alignment improving",
            "Recovery durability requires upstream stabilization");

        var escalationResistance = OperationalContinuityPhrasing.EscalationMomentumAlignment(
            evolutionTimeline.EscalationMomentum,
            "Escalation resistance alignment strengthening",
            "Escalation resistance alignment weakening",
            "Escalation resistance alignment stable");

        return new OperationalResilienceContinuityDto
        {
            DominantResilienceShift = resilienceShift,
            SurvivabilityConsistency = survivabilityConsistency,
            FragilityConsistency = fragilityConsistency,
            RecoveryDurabilityAlignment = recoveryAlignment,
            EscalationResistanceAlignment = escalationResistance,
            OperatorInterpretation =
                $"Resilience continuity in {dominantArea.ToLowerInvariant()} with {fragilities.Count} fragility signal(s) " +
                $"and convergence {convergenceReport.ConvergenceStrength.ToString().ToLowerInvariant()}"
        };
    }

    private static OperationalSurvivabilityState ResolveSurvivabilityState(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalIntegrityReportDto integrityReport,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        if (fragilities.Any(f => f.FragilitySeverity == OperationalDurabilityStrength.Brittle))
            return OperationalSurvivabilityState.Critical;

        if (fragilities.Count >= 3
            || convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Fragmented)
            return OperationalSurvivabilityState.Fragile;

        if (situationRoom.StabilizationDirection is OperationalSituationDirection.Escalating
            || topology.TopologyState == OperationalTopologyState.EscalationDominant)
            return OperationalSurvivabilityState.Strained;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Converging
            && convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Strong
            && evolutionTimeline.EscalationMomentum.Contains("collapsing", StringComparison.OrdinalIgnoreCase)
            && integrityReport.ContradictionCount == 0)
            return OperationalSurvivabilityState.Strong;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
            or OperationalRecoveryDirection.Converging)
            return OperationalSurvivabilityState.Stable;

        return OperationalSurvivabilityState.Strained;
    }

    private static string DescribeStabilizationDurability(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalConvergenceReportDto convergenceReport,
        OperationalIntegrityReportDto integrityReport)
    {
        if (recovery.OverallDirection is OperationalRecoveryDirection.Converging
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Stabilizing
                or OperationalSituationDirection.Improving
            && evolutionTimeline.StabilizationMomentum.Contains("accelerating", StringComparison.OrdinalIgnoreCase)
            && convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Strong)
            return "Strong stabilization durability with converging signals";

        if (integrityReport.ContradictionCount > 0)
            return "Stabilization durability reduced by integrity contradictions";

        return "Stabilization durability within moderate bounded continuity";
    }

    private static string DescribeEscalationFragility(
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        if (fragilities.Any(f => f.FragilityType == OperationalFragilityType.EscalationRecurrence))
            return "Elevated escalation fragility from recurring escalation signals";

        if (situationRoom.EscalatingPropagationCount >= 2
            || topology.TopologyState == OperationalTopologyState.EscalationDominant)
            return "Escalation fragility concentrated in propagation topology";

        if (evolutionTimeline.EscalationMomentum.Contains("collapsing", StringComparison.OrdinalIgnoreCase))
            return "Escalation fragility decreasing as propagation collapses";

        return convergenceReport.EscalationConfidence;
    }

    private static string DescribeRecoverySustainability(
        OperationalRecoveryPostureDto recovery,
        OperationalConvergenceReportDto convergenceReport,
        OperationalEvolutionTimelineDto evolutionTimeline,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        if (fragilities.Any(f => f.FragilityType == OperationalFragilityType.RecoveryBrittleness))
            return "Recovery sustainability weak; convergence does not reinforce recovery";

        if (recovery.OverallDirection is OperationalRecoveryDirection.Converging
            && convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Strong
            && evolutionTimeline.RecoveryMomentum.Contains("accelerating", StringComparison.OrdinalIgnoreCase))
            return "Strong recovery sustainability with reinforcing convergence";

        if (recovery.OverallDirection is OperationalRecoveryDirection.Degrading
            or OperationalRecoveryDirection.Diverging)
            return "Recovery sustainability degrading";

        return "Recovery sustainability within bounded continuity";
    }

    private static string DescribeContainmentStrength(
        bool protectiveContainmentActive,
        OperationalSituationRoomDto situationRoom,
        IReadOnlyList<OperationalContainmentDurabilityDto> durabilities)
    {
        if (protectiveContainmentActive
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Stabilizing
                or OperationalSituationDirection.Improving)
            return "Containment strength strong with protective mode and stabilizing posture";

        if (protectiveContainmentActive)
            return "Containment engaged but stabilization posture requires monitoring";

        var strongest = durabilities.OrderByDescending(d => d.DurabilityStrength).FirstOrDefault();
        return strongest != null
            ? $"Containment strength {strongest.DurabilityStrength.ToString().ToLowerInvariant()} in {strongest.ContainmentArea.ToLowerInvariant()}"
            : "Containment strength within normal bounds";
    }

    private static string ComposeOperatorSummary(
        OperationalSurvivabilityState state,
        string stabilizationDurability,
        string escalationFragility,
        string recoverySustainability,
        string highestFragility,
        IReadOnlyList<OperationalResilienceCognitionSnapshot> priorSnapshots)
    {
        var continuity = string.Empty;
        if (priorSnapshots.Count > 0)
        {
            var prior = priorSnapshots[^1];
            if (prior.SurvivabilityState != state)
            {
                continuity =
                    $" Survivability moved from {prior.SurvivabilityState.ToString().ToLowerInvariant()} " +
                    $"to {state.ToString().ToLowerInvariant()}.";
            }
        }

        return
            $"Operational survivability is {state.ToString().ToLowerInvariant()}. " +
            $"Stabilization durability: {stabilizationDurability.ToLowerInvariant()}. " +
            $"Escalation fragility: {escalationFragility.ToLowerInvariant()}. " +
            $"Recovery sustainability: {recoverySustainability.ToLowerInvariant()}. " +
            $"Highest fragility: {highestFragility.ToLowerInvariant()}.{continuity}";
    }

    private static string NormalizeArea(string area)
    {
        return string.IsNullOrWhiteSpace(area) ? AreaOperational : area.Trim();
    }

    private static bool AreasMatch(string left, string right)
    {
        return string.Equals(NormalizeArea(left), NormalizeArea(right), StringComparison.OrdinalIgnoreCase);
    }
}
