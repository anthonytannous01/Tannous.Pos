using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalExperienceGraph;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;

using Tannous.Pos.Application.OperationalCognition;

namespace Tannous.Pos.Application.OperationalDigest;

/// <summary>Deterministic operational condensation and executive digest synthesis.</summary>
public static class OperationalDigestAggregation
{
    public const int MaxHighlights = 5;
    public const int MaxNavigationHighlights = 5;
    public const int MaxExecutivePriorities = 5;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;

    public const string AreaReplay = "Replay";
    public const string AreaRuntime = "Runtime";
    public const string AreaInventory = "Inventory";
    public const string AreaReconciliation = "Reconciliation";
    public const string AreaOperational = "Operational Stability";

    public static OperationalDigestDto ComposeOperationalDigest(
        OperationalTrendSummaryDto trend,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalPatternSummaryDto patternSummary,
        OperationalIntegrityReportDto integrityReport,
        OperationalExperienceGraphDto experienceGraph,
        OperationalContextualNavigationDto contextualNavigation,
        OperationalExperienceTraversalPathsDto traversalPaths,
        IReadOnlyList<OperationalDigestSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var dominantArea = NormalizeArea(causalitySummary.DominantOperationalArea);
        var focus = ComposeFocusSummary(
            dominantArea,
            recovery,
            incidentSummary,
            propagation,
            situationRoom,
            simulationSummary,
            playbooks,
            patternSummary,
            integrityReport,
            experienceGraph,
            traversalPaths);

        var highlights = ComposeHighlights(
            dominantArea,
            recovery,
            incidentSummary,
            causalitySummary,
            propagation,
            situationRoom,
            simulationSummary,
            playbooks,
            patternSummary,
            integrityReport,
            experienceGraph,
            triage);

        var navigationHighlights = ComposeNavigationHighlights(
            experienceGraph,
            contextualNavigation,
            traversalPaths,
            integrityReport,
            triage);

        var digestState = ResolveDigestState(recovery, situationRoom, integrityReport, triage);
        var dominantStory = ComposeDominantStory(dominantArea, recovery, situationRoom, patternSummary, integrityReport);
        var executiveText = ComposeExecutiveDigestText(dominantArea, recovery, situationRoom, focus, highlights);
        var operatorText = ComposeOperatorDigestText(dominantArea, recovery, focus, highlights, navigationHighlights);

        return new OperationalDigestDto
        {
            GeneratedAtUtc = generatedAtUtc,
            DigestState = digestState,
            DominantOperationalStory = dominantStory,
            DominantRiskArea = ResolveDominantRiskArea(dominantArea, situationRoom, incidentSummary, patternSummary),
            RecoveryDirection = recovery.OverallDirection.ToString(),
            StabilizationPriority = focus.HighestPriorityArea,
            EscalationPressure = ComposeEscalationPressure(situationRoom, propagation, incidentSummary),
            IntegrityState = integrityReport.OverallIntegrityState.ToString(),
            RecommendedOperatorFocus = focus.HighestPriorityArea,
            ExecutiveDigest = executiveText,
            OperatorDigest = operatorText,
            FocusSummary = focus,
            OperationalHighlights = highlights,
            NavigationHighlights = navigationHighlights
        };
    }

    public static OperationalExecutiveDigestDto ComposeExecutiveDigest(
        OperationalTrendSummaryDto trend,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalPatternSummaryDto patternSummary,
        OperationalIntegrityReportDto integrityReport,
        OperationalExperienceGraphDto experienceGraph,
        OperationalExperienceTraversalPathsDto traversalPaths,
        DateTime generatedAtUtc)
    {
        var dominantArea = NormalizeArea(causalitySummary.DominantOperationalArea);
        var focus = ComposeFocusSummary(
            dominantArea,
            recovery,
            incidentSummary,
            propagation,
            situationRoom,
            simulationSummary,
            playbooks,
            patternSummary,
            integrityReport,
            experienceGraph,
            traversalPaths);

        var priorities = ComposeExecutivePriorities(
            dominantArea,
            recovery,
            situationRoom,
            simulationSummary,
            integrityReport,
            experienceGraph,
            focus);

        var headline = ComposeExecutiveHeadline(dominantArea, recovery, situationRoom, integrityReport);
        var narrative = ComposeDominantStory(dominantArea, recovery, situationRoom, patternSummary, integrityReport);
        var primaryRisk = focus.HighestRiskEscalation;
        var recoveryOutlook = ComposeRecoveryOutlook(recovery, patternSummary, situationRoom);
        var escalationSummary = ComposeEscalationPressure(situationRoom, propagation, incidentSummary);
        var stabilizationSummary = focus.StabilizationConfidence;
        var leadershipAttention = ResolveLeadershipAttention(situationRoom, integrityReport, incidentSummary);
        var recommendedPriority = priorities.FirstOrDefault() ?? focus.HighestPriorityArea;

        return new OperationalExecutiveDigestDto
        {
            GeneratedAtUtc = generatedAtUtc,
            Headline = headline,
            DominantNarrative = narrative,
            PrimaryOperationalRisk = primaryRisk,
            RecoveryOutlook = recoveryOutlook,
            EscalationSummary = escalationSummary,
            StabilizationSummary = stabilizationSummary,
            LeadershipAttentionRequired = leadershipAttention,
            RecommendedPriority = recommendedPriority,
            ExecutivePriorities = priorities
        };
    }

    public static OperationalDigestSummaryDto ComposeDigestSummary(
        OperationalDigestDto digest,
        OperationalSituationRoomDto situationRoom,
        OperationalIntegrityReportDto integrityReport,
        DateTime generatedAtUtc)
    {
        var escalationAlignment = situationRoom.StabilizationDirection == OperationalSituationDirection.Escalating
            ? "Escalation pressure active across situation room interpretation"
            : "Escalation pressure remains aligned with condensed digest narrative";

        var recoveryAlignment = digest.RecoveryDirection is nameof(OperationalRecoveryDirection.Improving)
                or nameof(OperationalRecoveryDirection.Converging)
            ? "Recovery direction supports condensed stabilization narrative"
            : "Recovery direction requires upstream stabilization focus first";

        var integrityAlignment = integrityReport.ContradictionCount == 0
            ? "Cross-layer integrity remains coherent in condensed digest"
            : "Integrity contradictions require review before trusting condensed guidance";

        var attention = situationRoom.AttentionLevel.ToString();
        var summary =
            $"Operational digest state is {digest.DigestState.ToString().ToLowerInvariant()}. " +
            $"Dominant story: {digest.DominantOperationalStory.ToLowerInvariant()}. " +
            $"{digest.OperationalHighlights.Count} highlight(s), {digest.NavigationHighlights.Count} navigation highlight(s).";

        return new OperationalDigestSummaryDto
        {
            GeneratedAtUtc = generatedAtUtc,
            OperationalState = digest.DigestState,
            DominantNarrative = digest.DominantOperationalStory,
            EscalationAlignment = escalationAlignment,
            RecoveryAlignment = recoveryAlignment,
            IntegrityAlignment = integrityAlignment,
            OperatorAttentionLevel = attention,
            Summary = summary
        };
    }

    public static OperationalDigestSnapshot CreateSnapshot(OperationalDigestDto digest)
    {
        return new OperationalDigestSnapshot
        {
            GeneratedAtUtc = digest.GeneratedAtUtc,
            DigestState = digest.DigestState,
            DominantOperationalStory = digest.DominantOperationalStory,
            DominantRiskArea = digest.DominantRiskArea,
            RecommendedOperatorFocus = digest.RecommendedOperatorFocus,
            HighlightCount = digest.OperationalHighlights.Count
        };
    }

    private static OperationalFocusSummaryDto ComposeFocusSummary(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalPatternSummaryDto patternSummary,
        OperationalIntegrityReportDto integrityReport,
        OperationalExperienceGraphDto experienceGraph,
        OperationalExperienceTraversalPathsDto traversalPaths)
    {
        var highestPriority = ResolveHighestPriorityArea(
            dominantArea,
            simulationSummary,
            playbooks,
            situationRoom);

        var highestRisk = ResolveHighestRiskEscalation(
            dominantArea,
            situationRoom,
            incidentSummary,
            propagation,
            patternSummary);

        var strongestRecovery = ResolveStrongestRecoverySignal(recovery, patternSummary, situationRoom);
        var dominantConstraint = simulationSummary.DominantOperationalConstraint;
        if (string.IsNullOrWhiteSpace(dominantConstraint))
            dominantConstraint = situationRoom.OutlookDetail.DominantConstraint;

        var sequence = traversalPaths.TraversalPaths
            .OrderBy(p => p.TraversalPriority)
            .FirstOrDefault()?.OperatorSummary
            ?? experienceGraph.RecommendedTraversalPath;

        var stabilizationConfidence = integrityReport.OverallIntegrityState switch
        {
            OperationalIntegrityState.Coherent => "Strong stabilization confidence across condensed layers",
            OperationalIntegrityState.MostlyCoherent => "Moderate stabilization confidence; minor reconciliation advised",
            OperationalIntegrityState.Fragmented => "Reduced stabilization confidence due to fragmented interpretations",
            _ => "Low stabilization confidence until integrity contradictions are reviewed"
        };

        return new OperationalFocusSummaryDto
        {
            HighestPriorityArea = highestPriority,
            HighestRiskEscalation = highestRisk,
            StrongestRecoverySignal = strongestRecovery,
            DominantConstraint = string.IsNullOrWhiteSpace(dominantConstraint)
                ? "No dominant constraint identified"
                : dominantConstraint,
            RecommendedOperatorSequence = sequence,
            StabilizationConfidence = stabilizationConfidence
        };
    }

    private static IReadOnlyList<OperationalHighlightDto> ComposeHighlights(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalPatternSummaryDto patternSummary,
        OperationalIntegrityReportDto integrityReport,
        OperationalExperienceGraphDto experienceGraph,
        OperationalTriageQueueDto triage)
    {
        var highlights = new List<OperationalHighlightDto>();

        if (AreasMatch(dominantArea, AreaReplay))
        {
            highlights.Add(new OperationalHighlightDto
            {
                HighlightType = OperationalHighlightType.Risk,
                Title = "Replay instability dominant",
                Description = "Replay remains the dominant upstream operational driver",
                Severity = OperationalDigestSeverity.High,
                RelatedArea = AreaReplay,
                RecommendedAttention = "Validate replay containment before downstream stabilization",
                OperatorInterpretation =
                    "Replay instability remains dominant upstream driver; strongest operator priority is replay containment validation"
            });
        }

        if (AreasMatch(dominantArea, AreaRuntime)
            || patternSummary.DominantArchetype.Contains("runtime", StringComparison.OrdinalIgnoreCase))
        {
            highlights.Add(new OperationalHighlightDto
            {
                HighlightType = OperationalHighlightType.Escalation,
                Title = "Runtime escalation risk",
                Description = "Runtime survivability degradation remains highest operational risk",
                Severity = OperationalDigestSeverity.High,
                RelatedArea = AreaRuntime,
                RecommendedAttention = "Prioritize runtime containment traversal",
                OperatorInterpretation =
                    "Runtime survivability degradation remains highest operational risk; prioritize runtime containment traversal"
            });
        }

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
            or OperationalRecoveryDirection.Converging)
        {
            highlights.Add(new OperationalHighlightDto
            {
                HighlightType = OperationalHighlightType.Recovery,
                Title = "Recovery convergence",
                Description = "Stabilization convergence improving across active operational areas",
                Severity = OperationalDigestSeverity.Normal,
                RelatedArea = dominantArea,
                RecommendedAttention = "Verify recovery signals against integrity consistency",
                OperatorInterpretation =
                    "Stabilization convergence improving; strongest recovery signal identified in active operational movement"
            });
        }

        if (AreasMatch(dominantArea, AreaReconciliation)
            && recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging)
        {
            highlights.Add(new OperationalHighlightDto
            {
                HighlightType = OperationalHighlightType.Recovery,
                Title = "Reconciliation recovery improving",
                Description = "Reconciliation recovery movement supports condensed stabilization narrative",
                Severity = OperationalDigestSeverity.Normal,
                RelatedArea = AreaReconciliation,
                RecommendedAttention = "Confirm reconciliation recovery against propagation pressure",
                OperatorInterpretation = "Reconciliation recovery improving alongside broader stabilization convergence"
            });
        }

        if (situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.High)
        {
            highlights.Add(new OperationalHighlightDto
            {
                HighlightType = OperationalHighlightType.Escalation,
                Title = "High escalation pressure",
                Description = situationRoom.OperatorSummary,
                Severity = OperationalDigestSeverity.Critical,
                RelatedArea = dominantArea,
                RecommendedAttention = "Review situation room briefing before stabilization actions",
                OperatorInterpretation = "Escalation pressure elevated; leadership and operator attention required"
            });
        }

        var topContradiction = integrityReport.ContradictionCount > 0
            ? "Cross-layer contradictions require operator review"
            : null;

        if (topContradiction != null)
        {
            highlights.Add(new OperationalHighlightDto
            {
                HighlightType = OperationalHighlightType.Integrity,
                Title = "Integrity contradiction detected",
                Description = topContradiction,
                Severity = integrityReport.ContradictionCount >= 2
                    ? OperationalDigestSeverity.High
                    : OperationalDigestSeverity.Elevated,
                RelatedArea = dominantArea,
                RecommendedAttention = "Reconcile integrity findings before trusting stabilization guidance",
                OperatorInterpretation =
                    "Operational contradiction detected; review integrity layer before acting on condensed guidance"
            });
        }

        if (playbooks.PlaybookCount > 0)
        {
            var topPlaybook = playbooks.Playbooks
                .OrderByDescending(p => p.Severity)
                .FirstOrDefault();

            if (topPlaybook != null)
            {
                highlights.Add(new OperationalHighlightDto
                {
                    HighlightType = OperationalHighlightType.Stabilization,
                    Title = "Stabilization guidance available",
                    Description = topPlaybook.StabilizationObjective,
                    Severity = OperationalDigestSeverity.Elevated,
                    RelatedArea = NormalizeArea(topPlaybook.DominantArea),
                    RecommendedAttention = "Follow playbook sequencing for prioritized stabilization",
                    OperatorInterpretation = topPlaybook.OperatorSummary
                });
            }
        }

        if (simulationSummary.HighestLeverageArea.Contains("replay", StringComparison.OrdinalIgnoreCase)
            && !AreasMatch(dominantArea, AreaReplay))
        {
            highlights.Add(new OperationalHighlightDto
            {
                HighlightType = OperationalHighlightType.Risk,
                Title = "Simulation leverage on replay",
                Description = "Simulation indicates replay as highest leverage stabilization point",
                Severity = OperationalDigestSeverity.Elevated,
                RelatedArea = AreaReplay,
                RecommendedAttention = "Review simulation leverage before playbook execution",
                OperatorInterpretation = "Simulation indicates downstream replay expansion risk despite non-replay dominant area"
            });
        }

        if (triage.Items.Count > 0)
        {
            highlights.Add(new OperationalHighlightDto
            {
                HighlightType = OperationalHighlightType.Navigation,
                Title = "Triage investigation pending",
                Description = triage.Items[0].Summary,
                Severity = OperationalDigestSeverity.Elevated,
                RelatedArea = dominantArea,
                RecommendedAttention = "Start from triage queue for prioritized investigation",
                OperatorInterpretation = "Active triage items require immediate operator navigation attention"
            });
        }

        if (CountEscalatingPropagations(propagation) >= 2
            && recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging)
        {
            highlights.Add(new OperationalHighlightDto
            {
                HighlightType = OperationalHighlightType.Risk,
                Title = "Propagation-recovery tension",
                Description = "Escalating propagation persists while recovery posture reports improvement",
                Severity = OperationalDigestSeverity.High,
                RelatedArea = dominantArea,
                RecommendedAttention = "Validate propagation analysis against recovery outlook",
                OperatorInterpretation = "Condensed digest flags propagation-recovery interpretation tension"
            });
        }

        return highlights
            .OrderByDescending(h => h.Severity)
            .ThenBy(h => h.HighlightType)
            .ThenBy(h => h.Title, StringComparer.Ordinal)
            .Take(MaxHighlights)
            .ToList();
    }

    private static IReadOnlyList<OperationalNavigationHighlightDto> ComposeNavigationHighlights(
        OperationalExperienceGraphDto experienceGraph,
        OperationalContextualNavigationDto contextualNavigation,
        OperationalExperienceTraversalPathsDto traversalPaths,
        OperationalIntegrityReportDto integrityReport,
        OperationalTriageQueueDto triage)
    {
        var highlights = new List<OperationalNavigationHighlightDto>();

        highlights.Add(new OperationalNavigationHighlightDto
        {
            RecommendedSurface = contextualNavigation.RecommendedNextSurface,
            NavigationReason = contextualNavigation.DominantReason,
            RelatedOperationalTheme = experienceGraph.DominantOperationalContext.ToString(),
            InvestigationPriority = MapNavigationPriority(contextualNavigation.InvestigationPriority),
            ExpectedOperatorOutcome = "Continue contextual investigation with reduced fragmentation",
            OperatorInterpretation = contextualNavigation.OperatorInterpretation
        });

        highlights.Add(new OperationalNavigationHighlightDto
        {
            RecommendedSurface = experienceGraph.RecommendedEntryPoint,
            NavigationReason = "Recommended entry point for dominant operational context",
            RelatedOperationalTheme = experienceGraph.DominantOperationalContext.ToString(),
            InvestigationPriority = OperationalAttentionPriority.High,
            ExpectedOperatorOutcome = "Establish operational context before deep investigation",
            OperatorInterpretation = experienceGraph.OperatorSummary
        });

        foreach (var path in traversalPaths.TraversalPaths
                     .OrderBy(p => p.TraversalPriority)
                     .Take(3))
        {
            highlights.Add(new OperationalNavigationHighlightDto
            {
                RecommendedSurface = path.StartingSurface,
                NavigationReason = path.OperatorSummary,
                RelatedOperationalTheme = path.DominantOperationalFocus,
                InvestigationPriority = MapTraversalPriority(path.TraversalPriority),
                ExpectedOperatorOutcome = path.ExpectedOperatorOutcome,
                OperatorInterpretation =
                    $"Traversal path {path.PathId}: {string.Join(" → ", path.RecommendedSequence)}"
            });
        }

        if (integrityReport.ContradictionCount > 0)
        {
            highlights.Add(new OperationalNavigationHighlightDto
            {
                RecommendedSurface = OperationalExperienceGraphAggregation.SurfaceIntegrity,
                NavigationReason = "Integrity contradictions require reconciliation before stabilization",
                RelatedOperationalTheme = "Integrity reconciliation",
                InvestigationPriority = OperationalAttentionPriority.Immediate,
                ExpectedOperatorOutcome = "Reconcile contradictory operational interpretations",
                OperatorInterpretation = "Highest-value navigation when integrity contradictions are present"
            });
        }

        if (triage.Items.Count > 0)
        {
            highlights.Add(new OperationalNavigationHighlightDto
            {
                RecommendedSurface = OperationalExperienceGraphAggregation.SurfaceTriage,
                NavigationReason = "Active triage queue items require prioritized investigation",
                RelatedOperationalTheme = "Investigation priority",
                InvestigationPriority = OperationalAttentionPriority.Immediate,
                ExpectedOperatorOutcome = "Address highest-priority triage investigation item",
                OperatorInterpretation = triage.Items[0].Summary
            });
        }

        return highlights
            .OrderBy(h => h.InvestigationPriority)
            .ThenBy(h => h.RecommendedSurface, StringComparer.Ordinal)
            .Take(MaxNavigationHighlights)
            .ToList();
    }

    private static IReadOnlyList<string> ComposeExecutivePriorities(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalIntegrityReportDto integrityReport,
        OperationalExperienceGraphDto experienceGraph,
        OperationalFocusSummaryDto focus)
    {
        var priorities = new List<string>();

        if (integrityReport.ContradictionCount >= 2)
            priorities.Add("Reconcile integrity contradictions before stabilization actions");

        if (situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.High)
            priorities.Add("Review situation room escalation before downstream traversal");

        priorities.Add($"Focus stabilization on {focus.HighestPriorityArea.ToLowerInvariant()}");

        if (AreasMatch(dominantArea, AreaReplay))
            priorities.Add("Validate replay containment as dominant upstream driver");

        if (AreasMatch(dominantArea, AreaRuntime))
            priorities.Add("Prioritize runtime containment traversal");

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
            or OperationalRecoveryDirection.Converging)
            priorities.Add("Verify recovery convergence against propagation pressure");

        priorities.Add($"Follow recommended entry point: {experienceGraph.RecommendedEntryPoint.ToLowerInvariant()}");

        if (!string.IsNullOrWhiteSpace(simulationSummary.HighestLeverageArea))
            priorities.Add($"Review simulation leverage at {simulationSummary.HighestLeverageArea.ToLowerInvariant()}");

        return priorities
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxExecutivePriorities)
            .ToList();
    }

    private static OperationalDigestState ResolveDigestState(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalIntegrityReportDto integrityReport,
        OperationalTriageQueueDto triage)
    {
        if (integrityReport.OverallIntegrityState == OperationalIntegrityState.Contradictory)
            return OperationalDigestState.Fragmented;

        if (situationRoom.StabilizationDirection == OperationalSituationDirection.Escalating
            || situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.High)
            return OperationalDigestState.Escalating;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
            or OperationalRecoveryDirection.Converging)
            return OperationalDigestState.Recovering;

        if (triage.Items.Count >= 3)
            return OperationalDigestState.AttentionRequired;

        return OperationalDigestState.Stable;
    }

    private static string ComposeDominantStory(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalPatternSummaryDto patternSummary,
        OperationalIntegrityReportDto integrityReport)
    {
        if (AreasMatch(dominantArea, AreaReplay))
        {
            return
                $"Replay instability remains dominant upstream driver with {recovery.OverallDirection.ToString().ToLowerInvariant()} recovery movement";
        }

        if (AreasMatch(dominantArea, AreaRuntime)
            || patternSummary.DominantArchetype.Contains("runtime", StringComparison.OrdinalIgnoreCase))
        {
            return
                "Runtime survivability pressure dominates the operational story with containment stabilization required";
        }

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
            or OperationalRecoveryDirection.Converging)
        {
            return
                $"Stabilization convergence improving across {dominantArea.ToLowerInvariant()} with collapsing escalation propagation";
        }

        if (integrityReport.ContradictionCount > 0)
        {
            return
                $"{dominantArea} operational pressure with fragmented cross-layer interpretation requiring integrity review";
        }

        return situationRoom.OperationalNarrative;
    }

    private static string ComposeExecutiveDigestText(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalFocusSummaryDto focus,
        IReadOnlyList<OperationalHighlightDto> highlights)
    {
        var topHighlight = highlights.FirstOrDefault()?.OperatorInterpretation
            ?? focus.StrongestRecoverySignal;

        return
            $"{dominantArea} dominates the operational story. " +
            $"Recovery direction: {recovery.OverallDirection.ToString().ToLowerInvariant()}. " +
            $"Escalation: {situationRoom.EscalationSeverity.ToString().ToLowerInvariant()}. " +
            $"Priority focus: {focus.HighestPriorityArea.ToLowerInvariant()}. " +
            topHighlight;
    }

    private static string ComposeOperatorDigestText(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalFocusSummaryDto focus,
        IReadOnlyList<OperationalHighlightDto> highlights,
        IReadOnlyList<OperationalNavigationHighlightDto> navigationHighlights)
    {
        var highlightSummary = highlights.Count > 0
            ? highlights[0].RecommendedAttention
            : "Review dashboard for operational overview";

        var navSummary = navigationHighlights.FirstOrDefault()?.RecommendedSurface
            ?? OperationalExperienceGraphAggregation.SurfaceDashboard;

        return
            $"Dominant area: {dominantArea.ToLowerInvariant()}. " +
            $"Recovery: {recovery.OverallDirection.ToString().ToLowerInvariant()}. " +
            $"Top action: {highlightSummary.ToLowerInvariant()}. " +
            $"Next surface: {navSummary.ToLowerInvariant()}. " +
            $"Sequence: {focus.RecommendedOperatorSequence.ToLowerInvariant()}.";
    }

    private static string ComposeExecutiveHeadline(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalIntegrityReportDto integrityReport)
    {
        if (integrityReport.OverallIntegrityState == OperationalIntegrityState.Contradictory)
            return "Operational interpretations fragmented — integrity review required";

        if (situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.High)
            return $"Escalation pressure elevated in {dominantArea.ToLowerInvariant()} operational context";

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
            or OperationalRecoveryDirection.Converging)
            return $"Stabilization convergence improving across {dominantArea.ToLowerInvariant()}";

        return $"Operational attention focused on {dominantArea.ToLowerInvariant()} stabilization";
    }

    private static string ComposeRecoveryOutlook(
        OperationalRecoveryPostureDto recovery,
        OperationalPatternSummaryDto patternSummary,
        OperationalSituationRoomDto situationRoom)
    {
        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
            or OperationalRecoveryDirection.Converging)
        {
            return
                $"Recovery outlook improving with {recovery.OverallConfidence.ToString().ToLowerInvariant()} confidence" +
                (patternSummary.RecoveryPatternStrength is OperationalPatternConfidence.High
                    or OperationalPatternConfidence.Elevated
                    ? " and strong pattern recovery alignment"
                    : string.Empty);
        }

        return
            $"Recovery outlook {recovery.OverallDirection.ToString().ToLowerInvariant()}; " +
            $"situation room stabilization {situationRoom.StabilizationDirection.ToString().ToLowerInvariant()}";
    }

    private static string ResolveLeadershipAttention(
        OperationalSituationRoomDto situationRoom,
        OperationalIntegrityReportDto integrityReport,
        OperationalIncidentCasesSummaryDto incidentSummary)
    {
        if (situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.High
            || integrityReport.ContradictionCount >= 2
            || incidentSummary.EscalatingIncidentCount > 0)
            return "Leadership attention recommended for escalation or integrity reconciliation";

        return "No immediate leadership escalation required";
    }

    private static string ResolveDominantRiskArea(
        string dominantArea,
        OperationalSituationRoomDto situationRoom,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalPatternSummaryDto patternSummary)
    {
        if (situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.High)
            return NormalizeArea(situationRoom.DominantOperationalRisk);

        if (incidentSummary.EscalatingIncidentCount > 0)
            return dominantArea;

        if (!string.IsNullOrWhiteSpace(patternSummary.HighestRiskPattern))
            return patternSummary.HighestRiskPattern;

        return dominantArea;
    }

    private static string ComposeEscalationPressure(
        OperationalSituationRoomDto situationRoom,
        OperationalPropagationAnalysisDto propagation,
        OperationalIncidentCasesSummaryDto incidentSummary)
    {
        var escalating = CountEscalatingPropagations(propagation);
        if (situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.High)
            return $"High escalation severity with {escalating} escalating propagation signal(s)";

        if (incidentSummary.EscalatingIncidentCount > 0)
            return $"{incidentSummary.EscalatingIncidentCount} escalating incident case(s) active";

        return "Escalation pressure within normal condensed digest bounds";
    }

    private static string ResolveHighestPriorityArea(
        string dominantArea,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalSituationRoomDto situationRoom)
    {
        if (AreasMatch(dominantArea, AreaReplay))
            return "Replay containment validation";

        var topPlaybook = playbooks.Playbooks
            .OrderByDescending(p => p.Severity)
            .FirstOrDefault();

        if (topPlaybook != null && !string.IsNullOrWhiteSpace(topPlaybook.StabilizationObjective))
            return topPlaybook.StabilizationObjective;

        if (!string.IsNullOrWhiteSpace(simulationSummary.HighestLeverageArea))
            return $"{simulationSummary.HighestLeverageArea} stabilization leverage";

        return situationRoom.HighestPriorityFocus;
    }

    private static string ResolveHighestRiskEscalation(
        string dominantArea,
        OperationalSituationRoomDto situationRoom,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalPatternSummaryDto patternSummary)
    {
        if (AreasMatch(dominantArea, AreaRuntime)
            || patternSummary.DominantArchetype.Contains("runtime", StringComparison.OrdinalIgnoreCase))
            return "Runtime survivability degradation remains highest operational risk";

        if (situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.High)
            return situationRoom.DominantOperationalRisk;

        if (CountEscalatingPropagations(propagation) >= 2)
            return "Escalating propagation pressure across operational areas";

        if (incidentSummary.EscalatingIncidentCount > 0)
            return "Active incident escalation pressure";

        return "No critical escalation risk identified in condensed digest";
    }

    private static string ResolveStrongestRecoverySignal(
        OperationalRecoveryPostureDto recovery,
        OperationalPatternSummaryDto patternSummary,
        OperationalSituationRoomDto situationRoom)
    {
        if (recovery.OverallDirection is OperationalRecoveryDirection.Converging)
            return "Recovery convergence signal strongest across replay and reconciliation movement";

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving)
            return "Recovery improving with active stabilization convergence";

        if (patternSummary.RecoveryPatternStrength is OperationalPatternConfidence.High
            or OperationalPatternConfidence.Elevated)
            return $"Strong recovery pattern alignment in {patternSummary.DominantArchetype.ToLowerInvariant()}";

        return $"Recovery {recovery.OverallDirection.ToString().ToLowerInvariant()} with {situationRoom.StabilizationDirection.ToString().ToLowerInvariant()} stabilization";
    }

    private static OperationalAttentionPriority MapNavigationPriority(
        OperationalTraversalPriority priority)
    {
        return priority switch
        {
            OperationalTraversalPriority.Immediate => OperationalAttentionPriority.Immediate,
            OperationalTraversalPriority.High => OperationalAttentionPriority.High,
            OperationalTraversalPriority.Normal => OperationalAttentionPriority.Normal,
            _ => OperationalAttentionPriority.Contextual
        };
    }

    private static OperationalAttentionPriority MapTraversalPriority(
        OperationalTraversalPriority priority)
    {
        return MapNavigationPriority(priority);
    }

    private static int CountEscalatingPropagations(OperationalPropagationAnalysisDto propagation)
    {
        return propagation.Propagations.Count(p => p.IsEscalating);
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
