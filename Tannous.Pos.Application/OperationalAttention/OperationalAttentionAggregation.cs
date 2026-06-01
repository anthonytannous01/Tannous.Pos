using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalConvergence;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalResilience;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTopology;

namespace Tannous.Pos.Application.OperationalAttention;

/// <summary>Deterministic operational attention and priority coordination from bounded cognition continuity.</summary>
public static class OperationalAttentionAggregation
{
    public const int MaxPriorities = 8;
    public const int MaxCoordination = 8;
    public const int MaxEmphasis = 8;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;

    public const string AreaReplay = "Replay";
    public const string AreaRuntime = "Runtime";
    public const string AreaReconciliation = "Reconciliation";
    public const string AreaOperational = "Operational Stability";

    public static OperationalAttentionReportDto ComposeAttentionReport(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalDigestDto digest,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        IReadOnlyList<OperationalFragilityDto> fragilities,
        IReadOnlyList<OperationalAttentionSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var dominantArea = ResolveDominantArea(topology, convergenceReport, resilienceReport, fragilities);
        var priorities = ComposePriorities(
            dominantArea,
            recovery,
            situationRoom,
            causalitySummary,
            propagation,
            simulationSummary,
            playbooks,
            digest,
            evolutionTimeline,
            topology,
            convergenceReport,
            resilienceReport,
            fragilities);

        var emphasis = ComposeOperationalEmphasis(
            dominantArea,
            recovery,
            situationRoom,
            digest,
            evolutionTimeline,
            topology,
            convergenceReport,
            fragilities);

        var coordination = ComposeAttentionCoordination(
            dominantArea,
            recovery,
            situationRoom,
            playbooks,
            evolutionTimeline,
            topology,
            convergenceReport,
            resilienceReport,
            fragilities,
            priorities);

        var continuity = ComposeAttentionContinuity(
            dominantArea,
            recovery,
            situationRoom,
            evolutionTimeline,
            convergenceReport,
            priorities,
            priorSnapshots);

        var dominantPriority = priorities.FirstOrDefault()?.PriorityType ?? OperationalPriorityType.OperationalBalance;
        var highestUrgencyArea = priorities
            .OrderByDescending(p => p.OperationalUrgency)
            .ThenByDescending(p => p.PriorityStrength)
            .FirstOrDefault()?.OperationalArea
            ?? dominantArea;

        var stabilizationFocus = ResolveStabilizationFocusArea(
            recovery,
            situationRoom,
            evolutionTimeline,
            topology,
            resilienceReport,
            fragilities,
            dominantArea);

        var escalationFocus = ResolveEscalationFocusArea(
            situationRoom,
            propagation,
            evolutionTimeline,
            topology,
            convergenceReport,
            fragilities,
            dominantArea);

        var investigationFocus = ResolveInvestigationFocusArea(
            recovery,
            convergenceReport,
            resilienceReport,
            fragilities,
            dominantArea);

        var attentionPressure = ResolveAttentionPressureLevel(
            situationRoom,
            convergenceReport,
            resilienceReport,
            fragilities,
            priorities);

        var operatorSummary = ComposeOperatorSummary(
            dominantPriority,
            highestUrgencyArea,
            stabilizationFocus,
            escalationFocus,
            investigationFocus,
            attentionPressure,
            priorSnapshots);

        return new OperationalAttentionReportDto
        {
            GeneratedAtUtc = generatedAtUtc,
            DominantOperationalPriority = dominantPriority,
            HighestUrgencyArea = highestUrgencyArea,
            StabilizationFocusArea = stabilizationFocus,
            EscalationFocusArea = escalationFocus,
            InvestigationPriorityArea = investigationFocus,
            AttentionPressureLevel = attentionPressure,
            Priorities = priorities,
            AttentionCoordination = coordination,
            OperationalEmphasis = emphasis,
            AttentionContinuity = continuity,
            OperatorSummary = operatorSummary
        };
    }

    public static OperationalAttentionSummaryDto ComposeAttentionSummary(
        OperationalAttentionReportDto report,
        OperationalSituationRoomDto situationRoom,
        IReadOnlyList<OperationalPriorityDto> priorities,
        DateTime generatedAtUtc)
    {
        var highestConcern = priorities
            .OrderByDescending(p => p.OperationalUrgency)
            .ThenByDescending(p => p.PriorityStrength)
            .FirstOrDefault()?.RecommendedOperatorFocus
            ?? report.OperatorSummary;

        var dominantEscalation = report.EscalationFocusArea;
        var dominantStabilization = report.StabilizationFocusArea;
        var strongestEmphasis = report.OperationalEmphasis
            .OrderByDescending(e => e.EmphasisStrength)
            .FirstOrDefault()?.OperatorInterpretation
            ?? report.HighestUrgencyArea;

        var attentionState = ResolveAttentionState(report, priorities);

        var summary =
            $"Operational attention is {attentionState.ToString().ToLowerInvariant()} with dominant priority " +
            $"{report.DominantOperationalPriority.ToString().ToLowerInvariant()}. Highest urgency: {report.HighestUrgencyArea.ToLowerInvariant()}.";

        return new OperationalAttentionSummaryDto
        {
            GeneratedAtUtc = generatedAtUtc,
            HighestPriorityConcern = highestConcern,
            DominantEscalationFocus = dominantEscalation,
            DominantStabilizationFocus = dominantStabilization,
            StrongestOperationalEmphasis = strongestEmphasis,
            OperationalAttentionState = attentionState,
            OperatorAttentionLevel = situationRoom.AttentionLevel.ToString(),
            Summary = summary
        };
    }

    public static IReadOnlyList<OperationalPriorityDto> ComposePriorities(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalDigestDto digest,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        var priorities = new List<OperationalPriorityDto>();

        if (AreasMatch(dominantArea, AreaReplay)
            || digest.OperationalHighlights.Any(h =>
                h.OperatorInterpretation.Contains("replay", StringComparison.OrdinalIgnoreCase))
            || fragilities.Any(f => AreasMatch(f.OperationalArea, AreaReplay)))
        {
            var replayFragility = fragilities.Count(f => AreasMatch(f.OperationalArea, AreaReplay));
            var escalationRecurring = evolutionTimeline.EscalationMomentum.Contains("expanding", StringComparison.OrdinalIgnoreCase)
                || situationRoom.EscalatingPropagationCount > 0;

            if (replayFragility > 0 || escalationRecurring)
            {
                priorities.Add(new OperationalPriorityDto
                {
                    OperationalArea = AreaReplay,
                    PriorityType = OperationalPriorityType.EscalationDominant,
                    PriorityStrength = replayFragility >= 2
                        ? OperationalEmphasisStrength.Strong
                        : OperationalEmphasisStrength.Moderate,
                    OperationalUrgency = replayFragility >= 2
                        ? OperationalUrgencyLevel.Critical
                        : OperationalUrgencyLevel.High,
                    StabilizationImportance = "Replay stabilization governs downstream operational continuity",
                    EscalationImportance = "Replay escalation reinforcement exceeds recovery continuity",
                    RecommendedOperatorFocus = "Prioritize replay escalation containment before downstream recovery",
                    OperatorInterpretation =
                        "Replay receives highest operational priority from escalation reinforcement and fragility concentration"
                });
            }
        }

        if (resilienceReport.SurvivabilityState is OperationalSurvivabilityState.Strained
                or OperationalSurvivabilityState.Fragile
                or OperationalSurvivabilityState.Critical
            || (AreasMatch(dominantArea, AreaRuntime)
                && situationRoom.StabilizationDirection is OperationalSituationDirection.Escalating
                    or OperationalSituationDirection.Degrading))
        {
            priorities.Add(new OperationalPriorityDto
            {
                OperationalArea = AreaRuntime,
                PriorityType = OperationalPriorityType.ContainmentCritical,
                PriorityStrength = resilienceReport.SurvivabilityState == OperationalSurvivabilityState.Critical
                    ? OperationalEmphasisStrength.Strong
                    : OperationalEmphasisStrength.Moderate,
                OperationalUrgency = resilienceReport.SurvivabilityState == OperationalSurvivabilityState.Critical
                    ? OperationalUrgencyLevel.Critical
                    : OperationalUrgencyLevel.High,
                StabilizationImportance = "Runtime containment durability governs platform survivability",
                EscalationImportance = "Runtime escalation resistance collapsing under continued pressure",
                RecommendedOperatorFocus = "Route stabilization effort toward runtime containment before expansion",
                OperatorInterpretation =
                    "Runtime containment becomes dominant stabilization focus under survivability pressure"
            });
        }

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            && (convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Weak
                or OperationalConvergenceStrength.Fragmented
                || convergenceReport.Ambiguities.Count >= 2))
        {
            priorities.Add(new OperationalPriorityDto
            {
                OperationalArea = NormalizeArea(convergenceReport.HighestAmbiguityArea),
                PriorityType = OperationalPriorityType.RecoveryValidation,
                PriorityStrength = OperationalEmphasisStrength.Moderate,
                OperationalUrgency = OperationalUrgencyLevel.Elevated,
                StabilizationImportance = "Recovery validation required before assuming sustained stabilization",
                EscalationImportance = "Ambiguity elevated while convergence weak",
                RecommendedOperatorFocus = "Shift investigation priority toward recovery validation focus",
                OperatorInterpretation =
                    "Recovery improving but ambiguity elevated and convergence weak — validation focus required"
            });
        }

        if (topology.TopologyState == OperationalTopologyState.EscalationDominant
            || topology.TopologyState == OperationalTopologyState.Concentrated)
        {
            priorities.Add(new OperationalPriorityDto
            {
                OperationalArea = topology.HighestInfluenceArea,
                PriorityType = OperationalPriorityType.InvestigationFocus,
                PriorityStrength = OperationalEmphasisStrength.Moderate,
                OperationalUrgency = OperationalUrgencyLevel.Elevated,
                StabilizationImportance = "Upstream dependency criticality influences stabilization routing",
                EscalationImportance = "Topology concentration amplifies escalation propagation",
                RecommendedOperatorFocus = "Concentrate investigation on upstream critical dependency topology",
                OperatorInterpretation =
                    $"Topology criticality in {topology.HighestInfluenceArea.ToLowerInvariant()} warrants investigation priority"
            });
        }

        var escalatingPropagationCount = propagation.Propagations.Count(p => p.IsEscalating);
        if (situationRoom.EscalatingPropagationCount >= 2
            || escalatingPropagationCount >= 2)
        {
            priorities.Add(new OperationalPriorityDto
            {
                OperationalArea = NormalizeArea(causalitySummary.DominantOperationalArea),
                PriorityType = OperationalPriorityType.EscalationDominant,
                PriorityStrength = OperationalEmphasisStrength.Moderate,
                OperationalUrgency = OperationalUrgencyLevel.High,
                StabilizationImportance = "Stabilization must precede escalation propagation expansion",
                EscalationImportance = "Causality propagation continuity recurring across bounded window",
                RecommendedOperatorFocus = "Address escalation propagation before downstream stabilization expansion",
                OperatorInterpretation =
                    "Escalation propagation continuity elevates operational urgency in causality-dominant area"
            });
        }

        if (playbooks.Playbooks.Count > 0
            && playbooks.Playbooks.Any(p =>
                p.StabilizationObjective.Contains("immediate", StringComparison.OrdinalIgnoreCase)
                || p.RecommendedSequence.Any(s => s.Contains("immediate", StringComparison.OrdinalIgnoreCase))))
        {
            var playbookArea = NormalizeArea(playbooks.Playbooks.First().DominantArea);
            priorities.Add(new OperationalPriorityDto
            {
                OperationalArea = playbookArea,
                PriorityType = OperationalPriorityType.StabilizationFirst,
                PriorityStrength = OperationalEmphasisStrength.Moderate,
                OperationalUrgency = OperationalUrgencyLevel.Elevated,
                StabilizationImportance = "Playbook sequencing indicates immediate stabilization priority",
                EscalationImportance = "Escalation pressure secondary to stabilization sequencing",
                RecommendedOperatorFocus = "Follow playbook stabilization sequencing as dominant focus",
                OperatorInterpretation =
                    $"Playbook sequencing routes stabilization-first focus to {playbookArea.ToLowerInvariant()}"
            });
        }

        if (simulationSummary.DegradationScenarioCount > simulationSummary.StabilizationScenarioCount)
        {
            priorities.Add(new OperationalPriorityDto
            {
                OperationalArea = NormalizeArea(simulationSummary.HighestLeverageArea),
                PriorityType = OperationalPriorityType.InvestigationFocus,
                PriorityStrength = OperationalEmphasisStrength.Weak,
                OperationalUrgency = OperationalUrgencyLevel.Elevated,
                StabilizationImportance = "Simulation leverage suggests stabilization path review",
                EscalationImportance = "Degradation scenarios outnumber stabilization paths",
                RecommendedOperatorFocus = "Review simulation leverage before assuming operational balance",
                OperatorInterpretation =
                    "Simulation degradation paths elevate investigation priority for leverage area"
            });
        }

        if (priorities.Count == 0)
        {
            priorities.Add(new OperationalPriorityDto
            {
                OperationalArea = dominantArea,
                PriorityType = OperationalPriorityType.OperationalBalance,
                PriorityStrength = OperationalEmphasisStrength.Minimal,
                OperationalUrgency = OperationalUrgencyLevel.Normal,
                StabilizationImportance = "Stabilization within normal bounded continuity",
                EscalationImportance = "Escalation pressure within normal bounds",
                RecommendedOperatorFocus = "Maintain routine operational monitoring",
                OperatorInterpretation =
                    $"Operational attention balanced across {dominantArea.ToLowerInvariant()} with no dominant urgency signal"
            });
        }

        return priorities
            .OrderByDescending(p => p.OperationalUrgency)
            .ThenByDescending(p => p.PriorityStrength)
            .ThenBy(p => p.OperationalArea, StringComparer.Ordinal)
            .Take(MaxPriorities)
            .ToList();
    }

    public static OperationalAttentionSnapshot CreateSnapshot(
        OperationalAttentionReportDto report,
        IReadOnlyList<OperationalPriorityDto> priorities)
    {
        return new OperationalAttentionSnapshot
        {
            GeneratedAtUtc = report.GeneratedAtUtc,
            DominantOperationalPriority = report.DominantOperationalPriority,
            AttentionPressureLevel = report.AttentionPressureLevel,
            HighestUrgencyArea = report.HighestUrgencyArea,
            PriorityCount = priorities.Count
        };
    }

    private static IReadOnlyList<OperationalEmphasisDto> ComposeOperationalEmphasis(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalDigestDto digest,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        var emphasis = new List<OperationalEmphasisDto>();

        var escalationPressure = evolutionTimeline.EscalationMomentum.Contains("expanding", StringComparison.OrdinalIgnoreCase)
            ? "Escalation pressure expanding"
            : evolutionTimeline.EscalationMomentum.Contains("collapsing", StringComparison.OrdinalIgnoreCase)
                ? "Escalation pressure collapsing"
                : "Escalation pressure within bounded continuity";

        var recoveryPressure = recovery.OverallDirection is OperationalRecoveryDirection.Degrading
                or OperationalRecoveryDirection.Diverging
            ? "Recovery pressure elevated"
            : recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
                ? "Recovery pressure easing"
                : "Recovery pressure stable";

        var stabilizationPressure = situationRoom.StabilizationDirection is OperationalSituationDirection.Escalating
                or OperationalSituationDirection.Degrading
            ? "Stabilization pressure elevated"
            : "Stabilization pressure within normal bounds";

        var localFragility = fragilities.Count(f => AreasMatch(f.OperationalArea, dominantArea));
        var strength = localFragility switch
        {
            0 when convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Strong
                => OperationalEmphasisStrength.Strong,
            0 => OperationalEmphasisStrength.Moderate,
            1 => OperationalEmphasisStrength.Weak,
            _ => OperationalEmphasisStrength.Strong
        };

        var reinforcing = digest.OperationalHighlights.Count > 0
            ? $"{digest.OperationalHighlights.Count} digest highlight(s) reinforce operational emphasis"
            : "No digest highlights in bounded window";

        emphasis.Add(new OperationalEmphasisDto
        {
            OperationalArea = dominantArea,
            EmphasisStrength = strength,
            ReinforcingSignals = reinforcing,
            EscalationPressure = escalationPressure,
            RecoveryPressure = recoveryPressure,
            StabilizationPressure = stabilizationPressure,
            OperatorInterpretation =
                $"Operational emphasis in {dominantArea.ToLowerInvariant()} is {strength.ToString().ToLowerInvariant()} " +
                $"with {localFragility} local fragility signal(s)"
        });

        if (!AreasMatch(dominantArea, AreaReplay))
        {
            emphasis.Add(new OperationalEmphasisDto
            {
                OperationalArea = AreaReplay,
                EmphasisStrength = fragilities.Any(f => AreasMatch(f.OperationalArea, AreaReplay))
                    ? OperationalEmphasisStrength.Moderate
                    : OperationalEmphasisStrength.Minimal,
                ReinforcingSignals = "Replay escalation continuity signals",
                EscalationPressure = escalationPressure,
                RecoveryPressure = recoveryPressure,
                StabilizationPressure = stabilizationPressure,
                OperatorInterpretation = "Replay emphasis governed by escalation reinforcement continuity"
            });
        }

        if (topology.TopologyState == OperationalTopologyState.EscalationDominant)
        {
            emphasis.Add(new OperationalEmphasisDto
            {
                OperationalArea = topology.HighestInfluenceArea,
                EmphasisStrength = OperationalEmphasisStrength.Moderate,
                ReinforcingSignals = "Topology criticality reinforces operational emphasis",
                EscalationPressure = topology.EscalationPropagationStrength,
                RecoveryPressure = recoveryPressure,
                StabilizationPressure = topology.StabilizationDependencyStrength,
                OperatorInterpretation =
                    $"Topology influence in {topology.HighestInfluenceArea.ToLowerInvariant()} reinforces operational emphasis"
            });
        }

        return emphasis
            .OrderByDescending(e => e.EmphasisStrength)
            .ThenBy(e => e.OperationalArea, StringComparer.Ordinal)
            .Take(MaxEmphasis)
            .ToList();
    }

    private static IReadOnlyList<OperationalAttentionCoordinationDto> ComposeAttentionCoordination(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalPlaybooksDto playbooks,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        IReadOnlyList<OperationalFragilityDto> fragilities,
        IReadOnlyList<OperationalPriorityDto> priorities)
    {
        var coordination = new List<OperationalAttentionCoordinationDto>();

        var topPriority = priorities.FirstOrDefault();
        if (topPriority != null)
        {
            coordination.Add(new OperationalAttentionCoordinationDto
            {
                CoordinationId = "coordination-dominant-priority",
                DominantOperationalConcern = topPriority.RecommendedOperatorFocus,
                AttentionRouting = $"Route attention to {topPriority.OperationalArea.ToLowerInvariant()} as {topPriority.PriorityType.ToString().ToLowerInvariant()} priority",
                EscalationWeight = topPriority.EscalationImportance,
                StabilizationWeight = topPriority.StabilizationImportance,
                InvestigationWeight = topPriority.PriorityType == OperationalPriorityType.InvestigationFocus
                    || topPriority.PriorityType == OperationalPriorityType.RecoveryValidation
                    ? "Investigation weight elevated"
                    : "Investigation weight secondary",
                OperatorSummary =
                    $"Dominant concern: {topPriority.RecommendedOperatorFocus.ToLowerInvariant()}"
            });
        }

        if (fragilities.Count >= 2)
        {
            coordination.Add(new OperationalAttentionCoordinationDto
            {
                CoordinationId = "coordination-fragility-concentration",
                DominantOperationalConcern = "Operational fragility concentrated across bounded areas",
                AttentionRouting = "Distribute attention across fragility signals before expansion",
                EscalationWeight = $"{fragilities.Count} fragility signal(s) elevate escalation weight",
                StabilizationWeight = "Stabilization weight must precede recovery expansion",
                InvestigationWeight = "Investigation weight aligned with fragility concentration",
                OperatorSummary =
                    $"{fragilities.Count} fragility signal(s) coordinate attention across operational areas"
            });
        }

        if (resilienceReport.SurvivabilityState is OperationalSurvivabilityState.Fragile
                or OperationalSurvivabilityState.Critical)
        {
            coordination.Add(new OperationalAttentionCoordinationDto
            {
                CoordinationId = "coordination-survivability-pressure",
                DominantOperationalConcern = $"Survivability {resilienceReport.SurvivabilityState.ToString().ToLowerInvariant()}",
                AttentionRouting = "Route attention toward survivability reinforcement before downstream focus",
                EscalationWeight = resilienceReport.EscalationFragility,
                StabilizationWeight = resilienceReport.StabilizationDurability,
                InvestigationWeight = "Investigation secondary to survivability reinforcement",
                OperatorSummary =
                    $"Survivability pressure coordinates attention toward {dominantArea.ToLowerInvariant()}"
            });
        }

        if (playbooks.Playbooks.Count > 0)
        {
            coordination.Add(new OperationalAttentionCoordinationDto
            {
                CoordinationId = "coordination-playbook-sequencing",
                DominantOperationalConcern = "Playbook sequencing governs attention routing",
                AttentionRouting = "Follow playbook stabilization sequence as attention anchor",
                EscalationWeight = "Escalation weight modulated by playbook sequencing",
                StabilizationWeight = "Stabilization weight primary in playbook sequence",
                InvestigationWeight = "Investigation weight follows playbook guidance",
                OperatorSummary = $"{playbooks.Playbooks.Count} playbook(s) coordinate operational attention routing"
            });
        }

        if (convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Weak
                or OperationalConvergenceStrength.Fragmented)
        {
            coordination.Add(new OperationalAttentionCoordinationDto
            {
                CoordinationId = "coordination-convergence-fragmentation",
                DominantOperationalConcern = "Signal convergence fragmented — attention requires validation",
                AttentionRouting = $"Shift investigation toward {NormalizeArea(convergenceReport.HighestAmbiguityArea).ToLowerInvariant()}",
                EscalationWeight = convergenceReport.EscalationConfidence,
                StabilizationWeight = "Stabilization weight reduced until convergence strengthens",
                InvestigationWeight = "Investigation weight elevated for ambiguity resolution",
                OperatorSummary =
                    "Convergence fragmentation coordinates investigation-focused attention routing"
            });
        }

        if (evolutionTimeline.EscalationMomentum.Contains("expanding", StringComparison.OrdinalIgnoreCase)
            && evolutionTimeline.RecoveryMomentum.Contains("slowing", StringComparison.OrdinalIgnoreCase))
        {
            coordination.Add(new OperationalAttentionCoordinationDto
            {
                CoordinationId = "coordination-momentum-divergence",
                DominantOperationalConcern = "Escalation expanding while recovery slowing",
                AttentionRouting = "Prioritize stabilization focus over recovery expansion",
                EscalationWeight = evolutionTimeline.EscalationMomentum,
                StabilizationWeight = evolutionTimeline.StabilizationMomentum,
                InvestigationWeight = "Investigation aligned with momentum divergence",
                OperatorSummary =
                    "Momentum divergence coordinates stabilization-first attention routing"
            });
        }

        if (topology.TopologyState == OperationalTopologyState.EscalationDominant)
        {
            coordination.Add(new OperationalAttentionCoordinationDto
            {
                CoordinationId = "coordination-topology-criticality",
                DominantOperationalConcern = "Topology escalation-dominant — upstream criticality governs routing",
                AttentionRouting = $"Route attention to {topology.HighestInfluenceArea.ToLowerInvariant()} upstream influence",
                EscalationWeight = topology.EscalationPropagationStrength,
                StabilizationWeight = topology.StabilizationDependencyStrength,
                InvestigationWeight = "Investigation weight on dependency chain criticality",
                OperatorSummary =
                    "Topology criticality coordinates escalation-weighted attention routing"
            });
        }

        if (coordination.Count == 0)
        {
            coordination.Add(new OperationalAttentionCoordinationDto
            {
                CoordinationId = "coordination-balanced",
                DominantOperationalConcern = "Operational attention balanced across bounded continuity",
                AttentionRouting = "Maintain routine monitoring across operational areas",
                EscalationWeight = situationRoom.EscalationSeverity.ToString(),
                StabilizationWeight = situationRoom.StabilizationDirection.ToString(),
                InvestigationWeight = "Investigation weight within normal bounds",
                OperatorSummary = "Attention coordination balanced with no dominant routing signal"
            });
        }

        return coordination
            .OrderBy(c => c.CoordinationId, StringComparer.Ordinal)
            .Take(MaxCoordination)
            .ToList();
    }

    private static OperationalAttentionContinuityDto ComposeAttentionContinuity(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalConvergenceReportDto convergenceReport,
        IReadOnlyList<OperationalPriorityDto> priorities,
        IReadOnlyList<OperationalAttentionSnapshot> priorSnapshots)
    {
        var prior = priorSnapshots.LastOrDefault();
        var currentPriority = priorities.FirstOrDefault()?.PriorityType ?? OperationalPriorityType.OperationalBalance;

        var attentionShift = prior != null && prior.DominantOperationalPriority != currentPriority
            ? OperationalContinuityPhrasing.StateShift(
                "Attention",
                prior.DominantOperationalPriority.ToString().ToLowerInvariant(),
                currentPriority.ToString().ToLowerInvariant())
            : OperationalContinuityPhrasing.ConsistentAcrossBoundedWindow("Operational attention");

        var priorityConsistency = OperationalContinuityPhrasing.PriorityAreaConsistency(
            prior != null && prior.HighestUrgencyArea == dominantArea);

        var escalationAlignment = OperationalContinuityPhrasing.EscalationMomentumAlignment(
            evolutionTimeline.EscalationMomentum,
            "Escalation attention alignment improving",
            "Escalation attention alignment strained",
            "Escalation attention alignment stable");

        var stabilizationAlignment = OperationalContinuityPhrasing.StabilizationSituationAlignment(
            situationRoom,
            "Stabilization attention alignment strengthening",
            "Stabilization attention alignment requires reinforcement",
            "Stabilization attention alignment within bounds");

        var investigationAlignment = convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Weak
                or OperationalConvergenceStrength.Fragmented
            ? "Investigation focus alignment elevated for ambiguity resolution"
            : "Investigation focus alignment within normal bounds";

        return new OperationalAttentionContinuityDto
        {
            DominantAttentionShift = attentionShift,
            PriorityConsistency = priorityConsistency,
            EscalationAttentionAlignment = escalationAlignment,
            StabilizationAttentionAlignment = stabilizationAlignment,
            InvestigationFocusAlignment = investigationAlignment,
            OperatorInterpretation =
                $"Attention continuity in {dominantArea.ToLowerInvariant()} with {priorities.Count} priority signal(s) " +
                $"and convergence {convergenceReport.ConvergenceStrength.ToString().ToLowerInvariant()}"
        };
    }

    private static string ResolveDominantArea(
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        if (!string.IsNullOrWhiteSpace(resilienceReport.HighestFragilityArea))
            return NormalizeArea(resilienceReport.HighestFragilityArea);

        if (!string.IsNullOrWhiteSpace(topology.HighestInfluenceArea))
            return NormalizeArea(topology.HighestInfluenceArea);

        return NormalizeArea(convergenceReport.HighestAmbiguityArea);
    }

    private static string ResolveStabilizationFocusArea(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalResilienceReportDto resilienceReport,
        IReadOnlyList<OperationalFragilityDto> fragilities,
        string dominantArea)
    {
        if (resilienceReport.SurvivabilityState is OperationalSurvivabilityState.Strained
                or OperationalSurvivabilityState.Fragile
                or OperationalSurvivabilityState.Critical
            || fragilities.Any(f => f.FragilityType == OperationalFragilityType.ContainmentInstability))
            return AreaRuntime;

        if (situationRoom.StabilizationDirection is OperationalSituationDirection.Stabilizing
                or OperationalSituationDirection.Improving
            && evolutionTimeline.StabilizationMomentum.Contains("accelerating", StringComparison.OrdinalIgnoreCase))
            return dominantArea;

        if (topology.TopologyState == OperationalTopologyState.RecoveryConverging)
            return NormalizeArea(topology.HighestInfluenceArea);

        return recovery.OverallDirection is OperationalRecoveryDirection.Degrading
            ? AreaOperational
            : dominantArea;
    }

    private static string ResolveEscalationFocusArea(
        OperationalSituationRoomDto situationRoom,
        OperationalPropagationAnalysisDto propagation,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        IReadOnlyList<OperationalFragilityDto> fragilities,
        string dominantArea)
    {
        if (fragilities.Any(f => f.FragilityType == OperationalFragilityType.EscalationRecurrence))
            return fragilities.First(f => f.FragilityType == OperationalFragilityType.EscalationRecurrence).OperationalArea;

        if (situationRoom.EscalatingPropagationCount >= 2)
            return dominantArea;

        if (topology.TopologyState == OperationalTopologyState.EscalationDominant)
            return NormalizeArea(topology.HighestInfluenceArea);

        if (evolutionTimeline.EscalationMomentum.Contains("expanding", StringComparison.OrdinalIgnoreCase))
            return NormalizeArea(convergenceReport.HighestAmbiguityArea);

        return propagation.Propagations.Any(p => p.IsEscalating)
            ? dominantArea
            : AreaOperational;
    }

    private static string ResolveInvestigationFocusArea(
        OperationalRecoveryPostureDto recovery,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        IReadOnlyList<OperationalFragilityDto> fragilities,
        string dominantArea)
    {
        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            && convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Weak
                or OperationalConvergenceStrength.Fragmented)
            return NormalizeArea(convergenceReport.HighestAmbiguityArea);

        if (fragilities.Any(f => f.FragilityType == OperationalFragilityType.RecoveryBrittleness))
            return fragilities.First(f => f.FragilityType == OperationalFragilityType.RecoveryBrittleness).OperationalArea;

        if (resilienceReport.SurvivabilityState is OperationalSurvivabilityState.Fragile
                or OperationalSurvivabilityState.Critical)
            return dominantArea;

        return NormalizeArea(convergenceReport.HighestAmbiguityArea);
    }

    private static OperationalUrgencyLevel ResolveAttentionPressureLevel(
        OperationalSituationRoomDto situationRoom,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        IReadOnlyList<OperationalFragilityDto> fragilities,
        IReadOnlyList<OperationalPriorityDto> priorities)
    {
        if (resilienceReport.SurvivabilityState == OperationalSurvivabilityState.Critical
            || priorities.Any(p => p.OperationalUrgency == OperationalUrgencyLevel.Critical))
            return OperationalUrgencyLevel.Critical;

        if (resilienceReport.SurvivabilityState == OperationalSurvivabilityState.Fragile
            || fragilities.Count >= 3
            || priorities.Any(p => p.OperationalUrgency == OperationalUrgencyLevel.High))
            return OperationalUrgencyLevel.High;

        if (situationRoom.AttentionLevel is OperationalAttentionLevel.Elevated
                or OperationalAttentionLevel.High
                or OperationalAttentionLevel.Critical
            || convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Weak
                or OperationalConvergenceStrength.Fragmented
            || priorities.Any(p => p.OperationalUrgency == OperationalUrgencyLevel.Elevated))
            return OperationalUrgencyLevel.Elevated;

        return OperationalUrgencyLevel.Normal;
    }

    private static OperationalAttentionState ResolveAttentionState(
        OperationalAttentionReportDto report,
        IReadOnlyList<OperationalPriorityDto> priorities)
    {
        if (report.AttentionPressureLevel == OperationalUrgencyLevel.Critical
            || priorities.Count(p => p.OperationalUrgency >= OperationalUrgencyLevel.High) >= 3)
            return OperationalAttentionState.Overloaded;

        if (report.AttentionPressureLevel == OperationalUrgencyLevel.High
            || priorities.Count >= 4)
            return OperationalAttentionState.Strained;

        if (priorities.Count >= 2)
            return OperationalAttentionState.Distributed;

        if (priorities.Count == 1 && priorities[0].PriorityType != OperationalPriorityType.OperationalBalance)
            return OperationalAttentionState.Focused;

        return OperationalAttentionState.Coordinated;
    }

    private static string ComposeOperatorSummary(
        OperationalPriorityType dominantPriority,
        string highestUrgencyArea,
        string stabilizationFocus,
        string escalationFocus,
        string investigationFocus,
        OperationalUrgencyLevel attentionPressure,
        IReadOnlyList<OperationalAttentionSnapshot> priorSnapshots)
    {
        var continuity = string.Empty;
        if (priorSnapshots.Count > 0)
        {
            var prior = priorSnapshots[^1];
            if (prior.DominantOperationalPriority != dominantPriority)
            {
                continuity =
                    $" Priority shifted from {prior.DominantOperationalPriority.ToString().ToLowerInvariant()} " +
                    $"to {dominantPriority.ToString().ToLowerInvariant()}.";
            }
        }

        return
            $"Operational attention pressure is {attentionPressure.ToString().ToLowerInvariant()}. " +
            $"Dominant priority: {dominantPriority.ToString().ToLowerInvariant()}. " +
            $"Highest urgency: {highestUrgencyArea.ToLowerInvariant()}. " +
            $"Stabilization focus: {stabilizationFocus.ToLowerInvariant()}. " +
            $"Escalation focus: {escalationFocus.ToLowerInvariant()}. " +
            $"Investigation priority: {investigationFocus.ToLowerInvariant()}.{continuity}";
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
