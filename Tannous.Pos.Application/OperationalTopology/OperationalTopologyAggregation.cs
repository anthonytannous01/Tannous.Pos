using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalExperienceGraph;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;

using Tannous.Pos.Application.OperationalCognition;

namespace Tannous.Pos.Application.OperationalTopology;

/// <summary>Deterministic operational topology and dependency intelligence from bounded continuity.</summary>
public static class OperationalTopologyAggregation
{
    public const int MaxDependencies = 8;
    public const int MaxDependencyChains = 8;
    public const int MaxInfluences = 8;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;

    public const string AreaReplay = "Replay";
    public const string AreaRuntime = "Runtime";
    public const string AreaReconciliation = "Reconciliation";
    public const string AreaOperational = "Operational Stability";

    public static OperationalTopologyDto ComposeOperationalTopology(
        OperationalRecoveryPostureDto recovery,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        IReadOnlyList<OperationalCausalChainDto> chains,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalPatternSummaryDto patternSummary,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalIntegrityReportDto integrityReport,
        OperationalExperienceGraphDto experienceGraph,
        OperationalDigestDto digest,
        IReadOnlyList<OperationalTopologySnapshot> priorTopologySnapshots,
        DateTime generatedAtUtc)
    {
        var dominantArea = NormalizeArea(causalitySummary.DominantOperationalArea);
        var dependencies = ComposeDependencies(
            dominantArea,
            recovery,
            causalitySummary,
            propagation,
            situationRoom,
            simulationSummary,
            playbooks,
            patternSummary,
            evolutionTimeline,
            experienceGraph,
            digest);

        var influences = ComposeInfluences(
            dominantArea,
            recovery,
            causalitySummary,
            propagation,
            situationRoom,
            simulationSummary,
            dependencies);

        var continuity = ComposeTopologyContinuity(
            dominantArea,
            recovery,
            situationRoom,
            evolutionTimeline,
            integrityReport,
            dependencies,
            priorTopologySnapshots);

        var topologyState = ResolveTopologyState(
            recovery,
            situationRoom,
            integrityReport,
            causalitySummary,
            evolutionTimeline);

        var dominantTopology = ResolveDominantOperationalTopology(
            dominantArea,
            recovery,
            situationRoom,
            patternSummary,
            evolutionTimeline);

        var highestInfluence = influences
            .OrderByDescending(i => i.OperationalImportance)
            .ThenBy(i => i.Area, StringComparer.Ordinal)
            .FirstOrDefault()?.Area
            ?? dominantArea;

        var highestRisk = dependencies
            .OrderByDescending(d => d.OperationalCriticality)
            .ThenBy(d => d.SourceArea, StringComparer.Ordinal)
            .FirstOrDefault()?.OperatorInterpretation
            ?? "No elevated dependency risk detected in bounded continuity window";

        var stabilizationStrength = DescribeStabilizationDependencyStrength(
            recovery,
            situationRoom,
            propagation,
            simulationSummary);

        var escalationStrength = DescribeEscalationPropagationStrength(
            causalitySummary,
            propagation,
            situationRoom,
            evolutionTimeline);

        var operatorSummary = ComposeOperatorSummary(
            dominantTopology,
            highestInfluence,
            topologyState,
            dependencies.Count,
            stabilizationStrength,
            escalationStrength,
            priorTopologySnapshots);

        return new OperationalTopologyDto
        {
            GeneratedAtUtc = generatedAtUtc,
            DominantOperationalTopology = dominantTopology,
            HighestInfluenceArea = highestInfluence,
            HighestDependencyRisk = highestRisk,
            StabilizationDependencyStrength = stabilizationStrength,
            EscalationPropagationStrength = escalationStrength,
            TopologyState = topologyState,
            Dependencies = dependencies,
            Influences = influences,
            TopologyContinuity = continuity,
            OperatorSummary = operatorSummary
        };
    }

    public static OperationalTopologySummaryDto ComposeTopologySummary(
        OperationalTopologyDto topology,
        OperationalSituationRoomDto situationRoom,
        OperationalRecoveryPostureDto recovery,
        IReadOnlyList<OperationalDependencyChainDto> chains,
        DateTime generatedAtUtc)
    {
        var dominantFlow = chains.FirstOrDefault()?.DominantOperationalFlow
            ?? topology.DominantOperationalTopology;

        var highestRisk = topology.Dependencies
            .OrderByDescending(d => d.OperationalCriticality)
            .ThenBy(d => d.SourceArea, StringComparer.Ordinal)
            .FirstOrDefault()?.OperatorInterpretation
            ?? topology.HighestDependencyRisk;

        var strongestStabilization = topology.Dependencies
            .Where(d => d.DependencyType == OperationalDependencyType.StabilizationDependency)
            .OrderByDescending(d => d.OperationalCriticality)
            .FirstOrDefault()?.OperatorInterpretation
            ?? topology.StabilizationDependencyStrength;

        var strongestEscalation = topology.Dependencies
            .Where(d => d.DependencyType == OperationalDependencyType.EscalationDependency
                || d.DependencyType == OperationalDependencyType.PropagationDependency)
            .OrderByDescending(d => d.OperationalCriticality)
            .FirstOrDefault()?.OperatorInterpretation
            ?? topology.EscalationPropagationStrength;

        var summary =
            $"Topology is {topology.TopologyState.ToString().ToLowerInvariant()} with dominant flow {dominantFlow.ToLowerInvariant()}. " +
            $"Highest influence area: {topology.HighestInfluenceArea.ToLowerInvariant()}.";

        return new OperationalTopologySummaryDto
        {
            GeneratedAtUtc = generatedAtUtc,
            DominantDependencyFlow = dominantFlow,
            HighestRiskDependency = highestRisk,
            StrongestStabilizationInfluence = strongestStabilization,
            StrongestEscalationInfluence = strongestEscalation,
            OperationalCriticalityState = topology.TopologyState,
            OperatorAttentionLevel = situationRoom.AttentionLevel.ToString(),
            Summary = summary
        };
    }

    public static IReadOnlyList<OperationalDependencyChainDto> ComposeDependencyChains(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        IReadOnlyList<OperationalCausalChainDto> chains,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalPatternSummaryDto patternSummary)
    {
        var result = new List<OperationalDependencyChainDto>();

        foreach (var chain in chains.Take(MaxDependencyChains))
        {
            var sequence = BuildChainSequence(chain, propagation);
            result.Add(new OperationalDependencyChainDto
            {
                ChainId = $"chain-{chain.ChainId}",
                DominantOperationalFlow = chain.Title,
                UpstreamArea = NormalizeArea(chain.DominantArea),
                DownstreamArea = ResolveDownstreamArea(chain, propagation),
                DependencySequence = sequence,
                EscalationRisk = chain.PropagationDirection == OperationalCausalityDirection.Expanding
                    ? "Escalation concentration indicated in causal chain continuity"
                    : "Escalation risk within normal bounds for this chain",
                StabilizationPotential = string.IsNullOrWhiteSpace(chain.StabilizationBlocker)
                    ? "Stabilization potential aligned with upstream recovery focus"
                    : $"Stabilization blocked by {chain.StabilizationBlocker.ToLowerInvariant()}",
                OperatorSummary = chain.OperatorSummary
            });
        }

        if (AreasMatch(dominantArea, AreaReplay)
            && recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging)
        {
            result.Add(new OperationalDependencyChainDto
            {
                ChainId = "chain-replay-centric",
                DominantOperationalFlow = "Replay-centric stabilization topology",
                UpstreamArea = AreaReplay,
                DownstreamArea = AreaReconciliation,
                DependencySequence = new[] { AreaReplay, AreaReconciliation, AreaRuntime },
                EscalationRisk = "Replay escalation upstream may propagate to reconciliation and runtime",
                StabilizationPotential = "Replay stabilization strongly improves downstream recovery alignment",
                OperatorSummary =
                    "Replay-centric topology: replay instability upstream with reconciliation and runtime downstream dependency"
            });
        }

        if (AreasMatch(dominantArea, AreaRuntime)
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Escalating
                or OperationalSituationDirection.Degrading)
        {
            result.Add(new OperationalDependencyChainDto
            {
                ChainId = "chain-runtime-critical",
                DominantOperationalFlow = "Runtime-critical escalation topology",
                UpstreamArea = AreaRuntime,
                DownstreamArea = AreaReplay,
                DependencySequence = new[] { AreaRuntime, AreaReplay, AreaReconciliation },
                EscalationRisk = "Runtime survivability influences most escalation flows with downstream replay propagation",
                StabilizationPotential = "Stabilization blocked until runtime containment stabilizes",
                OperatorSummary =
                    "Runtime-critical topology: runtime survivability upstream with recurring downstream replay propagation"
            });
        }

        if (recovery.OverallDirection is OperationalRecoveryDirection.Converging
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Stabilizing
                or OperationalSituationDirection.Improving)
        {
            result.Add(new OperationalDependencyChainDto
            {
                ChainId = "chain-recovery-convergence",
                DominantOperationalFlow = "Recovery convergence topology",
                UpstreamArea = dominantArea,
                DownstreamArea = AreaOperational,
                DependencySequence = new[] { dominantArea, AreaReconciliation, AreaOperational },
                EscalationRisk = "Escalation propagation collapsing across dependency chains",
                StabilizationPotential = "Dependency chains shortening with operational coherence strengthening",
                OperatorSummary =
                    "Recovery convergence topology: stabilization propagation collapsing with dependency chains shortening"
            });
        }

        var topPlaybook = playbooks.Playbooks
            .OrderByDescending(p => p.Severity)
            .ThenBy(p => p.PlaybookId, StringComparer.Ordinal)
            .FirstOrDefault();

        if (topPlaybook != null && topPlaybook.RecommendedSequence.Count >= 2)
        {
            var playbookSequence = topPlaybook.RecommendedSequence.Take(MaxDependencyChains).ToList();
            result.Add(new OperationalDependencyChainDto
            {
                ChainId = $"chain-playbook-{topPlaybook.PlaybookId}",
                DominantOperationalFlow = topPlaybook.Title,
                UpstreamArea = NormalizeArea(topPlaybook.DominantArea),
                DownstreamArea = playbookSequence.Last(),
                DependencySequence = playbookSequence,
                EscalationRisk = $"Playbook severity {topPlaybook.Severity.ToString().ToLowerInvariant()} indicates sequencing dependency",
                StabilizationPotential = topPlaybook.StabilizationObjective,
                OperatorSummary = topPlaybook.OperatorSummary
            });
        }

        if (!AreasMatch(simulationSummary.HighestLeverageArea, dominantArea)
            && !string.IsNullOrWhiteSpace(simulationSummary.HighestLeverageArea))
        {
            result.Add(new OperationalDependencyChainDto
            {
                ChainId = "chain-simulation-leverage",
                DominantOperationalFlow = "Simulation leverage dependency chain",
                UpstreamArea = NormalizeArea(simulationSummary.HighestLeverageArea),
                DownstreamArea = dominantArea,
                DependencySequence = new[]
                {
                    NormalizeArea(simulationSummary.HighestLeverageArea),
                    dominantArea,
                    AreaOperational
                },
                EscalationRisk = "Leverage shift may reorder stabilization dependency priority",
                StabilizationPotential = simulationSummary.Summary,
                OperatorSummary =
                    $"Simulation leverage chain from {simulationSummary.HighestLeverageArea.ToLowerInvariant()} toward {dominantArea.ToLowerInvariant()}"
            });
        }

        if (evolutionTimeline.ActiveTransitionCount > 0)
        {
            var topTransition = evolutionTimeline.Transitions.FirstOrDefault();
            if (topTransition != null)
            {
                result.Add(new OperationalDependencyChainDto
                {
                    ChainId = $"chain-evolution-{topTransition.TransitionId}",
                    DominantOperationalFlow = topTransition.OperatorInterpretation,
                    UpstreamArea = NormalizeArea(topTransition.DominantArea),
                    DownstreamArea = NormalizeArea(topTransition.TargetState),
                    DependencySequence = new[] { topTransition.SourceState, topTransition.TargetState },
                    EscalationRisk = topTransition.Direction == OperationalEvolutionDirection.Escalating
                        ? "Evolution transition indicates expanding escalation dependency"
                        : "Evolution transition within bounded escalation bounds",
                    StabilizationPotential = topTransition.OperationalImpact,
                    OperatorSummary = topTransition.OperatorInterpretation
                });
            }
        }

        if (patternSummary.RecurringPatternCount > 0)
        {
            result.Add(new OperationalDependencyChainDto
            {
                ChainId = "chain-pattern-recurrence",
                DominantOperationalFlow = patternSummary.DominantArchetype,
                UpstreamArea = NormalizeArea(patternSummary.HighestRiskPattern),
                DownstreamArea = dominantArea,
                DependencySequence = new[] { patternSummary.HighestRiskPattern, patternSummary.DominantArchetype, dominantArea },
                EscalationRisk = $"Pattern escalation strength {patternSummary.EscalationPatternStrength.ToString().ToLowerInvariant()}",
                StabilizationPotential = patternSummary.Summary,
                OperatorSummary = patternSummary.Summary
            });
        }

        return result
            .OrderBy(c => c.ChainId, StringComparer.Ordinal)
            .Take(MaxDependencyChains)
            .ToList();
    }

    public static OperationalTopologySnapshot CreateSnapshot(OperationalTopologyDto topology)
    {
        return new OperationalTopologySnapshot
        {
            GeneratedAtUtc = topology.GeneratedAtUtc,
            DominantOperationalTopology = topology.DominantOperationalTopology,
            HighestInfluenceArea = topology.HighestInfluenceArea,
            TopologyState = topology.TopologyState,
            StabilizationDependencyStrength = topology.StabilizationDependencyStrength,
            EscalationPropagationStrength = topology.EscalationPropagationStrength,
            ActiveDependencyCount = topology.Dependencies.Count
        };
    }

    private static IReadOnlyList<OperationalDependencyDto> ComposeDependencies(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalPatternSummaryDto patternSummary,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalExperienceGraphDto experienceGraph,
        OperationalDigestDto digest)
    {
        var dependencies = new List<OperationalDependencyDto>();

        foreach (var propagationEdge in propagation.Propagations)
        {
            dependencies.Add(new OperationalDependencyDto
            {
                SourceArea = NormalizeArea(propagationEdge.SourceArea),
                TargetArea = NormalizeArea(propagationEdge.TargetArea),
                DependencyType = propagationEdge.IsEscalating
                    ? OperationalDependencyType.EscalationDependency
                    : OperationalDependencyType.PropagationDependency,
                InfluenceStrength = propagationEdge.IsEscalating ? "High" : "Moderate",
                StabilizationInfluence = propagationEdge.IsCollapsing
                    ? "Stabilization propagation collapsing"
                    : "Stabilization influence via propagation continuity",
                EscalationInfluence = propagationEdge.IsEscalating
                    ? "Escalation propagation expanding"
                    : "Escalation within bounded propagation",
                OperationalCriticality = propagationEdge.IsEscalating
                    ? OperationalCriticalityLevel.High
                    : OperationalCriticalityLevel.Elevated,
                OperatorInterpretation = propagationEdge.OperatorInterpretation
            });
        }

        foreach (var relationship in experienceGraph.Relationships)
        {
            dependencies.Add(new OperationalDependencyDto
            {
                SourceArea = NormalizeArea(relationship.SourceSurface),
                TargetArea = NormalizeArea(relationship.TargetSurface),
                DependencyType = OperationalDependencyType.NavigationDependency,
                InfluenceStrength = relationship.RelevanceStrength.ToString(),
                StabilizationInfluence = "Navigation continuity supports stabilization ordering",
                EscalationInfluence = "Traversal dependency for escalation investigation",
                OperationalCriticality = MapNavigationCriticality(relationship.RelevanceStrength),
                OperatorInterpretation = relationship.OperatorInterpretation
            });
        }

        if (AreasMatch(dominantArea, AreaReplay))
        {
            dependencies.Add(new OperationalDependencyDto
            {
                SourceArea = AreaReplay,
                TargetArea = AreaReconciliation,
                DependencyType = OperationalDependencyType.StabilizationDependency,
                InfluenceStrength = "High",
                StabilizationInfluence = "Replay stabilization strongly improves downstream reconciliation alignment",
                EscalationInfluence = "Replay escalation propagates to reconciliation dependency",
                OperationalCriticality = OperationalCriticalityLevel.High,
                OperatorInterpretation =
                    "Replay-centric dependency: replay instability upstream with reconciliation downstream dependency"
            });

            dependencies.Add(new OperationalDependencyDto
            {
                SourceArea = AreaReplay,
                TargetArea = AreaRuntime,
                DependencyType = OperationalDependencyType.PropagationDependency,
                InfluenceStrength = "Elevated",
                StabilizationInfluence = "Runtime survivability downstream of replay stabilization",
                EscalationInfluence = "Replay pressure may propagate runtime survivability risk",
                OperationalCriticality = OperationalCriticalityLevel.Elevated,
                OperatorInterpretation = "Replay upstream influence on runtime survivability downstream propagation"
            });
        }

        if (AreasMatch(dominantArea, AreaRuntime)
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Escalating
                or OperationalSituationDirection.Degrading)
        {
            dependencies.Add(new OperationalDependencyDto
            {
                SourceArea = AreaRuntime,
                TargetArea = AreaReplay,
                DependencyType = OperationalDependencyType.EscalationDependency,
                InfluenceStrength = "Critical",
                StabilizationInfluence = "Stabilization blocked by runtime containment instability",
                EscalationInfluence = "Runtime survivability influences most escalation flows",
                OperationalCriticality = OperationalCriticalityLevel.Critical,
                OperatorInterpretation =
                    "Runtime-critical dependency: runtime survivability upstream with downstream replay propagation recurring"
            });
        }

        if (!string.IsNullOrWhiteSpace(simulationSummary.HighestLeverageArea))
        {
            dependencies.Add(new OperationalDependencyDto
            {
                SourceArea = NormalizeArea(simulationSummary.HighestLeverageArea),
                TargetArea = dominantArea,
                DependencyType = OperationalDependencyType.StabilizationDependency,
                InfluenceStrength = simulationSummary.RecoveryAccelerationPotential.ToString(),
                StabilizationInfluence = "Simulation leverage point for stabilization ordering",
                EscalationInfluence = "Leverage shift may reorder escalation concentration",
                OperationalCriticality = OperationalCriticalityLevel.Elevated,
                OperatorInterpretation =
                    $"Simulation leverage dependency from {simulationSummary.HighestLeverageArea.ToLowerInvariant()} toward {dominantArea.ToLowerInvariant()}"
            });
        }

        var topPlaybook = playbooks.Playbooks
            .OrderByDescending(p => p.Severity)
            .ThenBy(p => p.PlaybookId, StringComparer.Ordinal)
            .FirstOrDefault();

        if (topPlaybook != null)
        {
            dependencies.Add(new OperationalDependencyDto
            {
                SourceArea = NormalizeArea(topPlaybook.DominantArea),
                TargetArea = dominantArea,
                DependencyType = OperationalDependencyType.SequencingDependency,
                InfluenceStrength = topPlaybook.OperationalConfidence.ToString(),
                StabilizationInfluence = topPlaybook.StabilizationObjective,
                EscalationInfluence = $"Playbook severity {topPlaybook.Severity.ToString().ToLowerInvariant()}",
                OperationalCriticality = MapPlaybookCriticality(topPlaybook.Severity),
                OperatorInterpretation = topPlaybook.OperatorSummary
            });
        }

        foreach (var transition in evolutionTimeline.Transitions.Take(3))
        {
            dependencies.Add(new OperationalDependencyDto
            {
                SourceArea = NormalizeArea(transition.DominantArea),
                TargetArea = NormalizeArea(transition.TargetState),
                DependencyType = transition.TransitionType == OperationalTransitionType.EscalationProgression
                    ? OperationalDependencyType.EscalationDependency
                    : OperationalDependencyType.RecoveryDependency,
                InfluenceStrength = transition.Severity.ToString(),
                StabilizationInfluence = transition.OperationalImpact,
                EscalationInfluence = transition.Direction.ToString(),
                OperationalCriticality = MapEvolutionSeverity(transition.Severity),
                OperatorInterpretation = transition.OperatorInterpretation
            });
        }

        if (causalitySummary.StabilizationBlockerCount > 0)
        {
            dependencies.Add(new OperationalDependencyDto
            {
                SourceArea = dominantArea,
                TargetArea = AreaOperational,
                DependencyType = OperationalDependencyType.StabilizationDependency,
                InfluenceStrength = "Elevated",
                StabilizationInfluence = $"{causalitySummary.StabilizationBlockerCount} stabilization blocker(s) constrain dependency flow",
                EscalationInfluence = causalitySummary.HighestRiskPropagation,
                OperationalCriticality = OperationalCriticalityLevel.High,
                OperatorInterpretation = causalitySummary.Summary
            });
        }

        if (patternSummary.RecurringPatternCount > 0)
        {
            dependencies.Add(new OperationalDependencyDto
            {
                SourceArea = NormalizeArea(patternSummary.HighestRiskPattern),
                TargetArea = NormalizeArea(patternSummary.DominantArchetype),
                DependencyType = OperationalDependencyType.RecoveryDependency,
                InfluenceStrength = patternSummary.RecoveryPatternStrength.ToString(),
                StabilizationInfluence = "Recurring pattern continuity defines stabilization ordering",
                EscalationInfluence = patternSummary.EscalationPatternStrength.ToString(),
                OperationalCriticality = OperationalCriticalityLevel.Elevated,
                OperatorInterpretation = patternSummary.Summary
            });
        }

        if (!string.IsNullOrWhiteSpace(digest.DominantRiskArea))
        {
            dependencies.Add(new OperationalDependencyDto
            {
                SourceArea = NormalizeArea(digest.DominantRiskArea),
                TargetArea = dominantArea,
                DependencyType = OperationalDependencyType.StabilizationDependency,
                InfluenceStrength = digest.DigestState.ToString(),
                StabilizationInfluence = digest.StabilizationPriority,
                EscalationInfluence = digest.FocusSummary.DominantConstraint,
                OperationalCriticality = digest.DigestState == OperationalDigestState.Escalating
                    ? OperationalCriticalityLevel.High
                    : OperationalCriticalityLevel.Normal,
                OperatorInterpretation = digest.OperatorDigest
            });
        }

        return dependencies
            .OrderByDescending(d => d.OperationalCriticality)
            .ThenBy(d => d.SourceArea, StringComparer.Ordinal)
            .ThenBy(d => d.TargetArea, StringComparer.Ordinal)
            .Take(MaxDependencies)
            .ToList();
    }

    private static IReadOnlyList<OperationalInfluenceDto> ComposeInfluences(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        IReadOnlyList<OperationalDependencyDto> dependencies)
    {
        var areaScores = new Dictionary<string, (int Upstream, int Downstream, int Escalation, int Stabilization)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var dependency in dependencies)
        {
            UpdateAreaScore(areaScores, dependency.SourceArea, upstream: 1, downstream: 0, escalation: 0, stabilization: 1);
            UpdateAreaScore(areaScores, dependency.TargetArea, upstream: 0, downstream: 1, escalation: 1, stabilization: 0);
        }

        foreach (var propagationEdge in propagation.Propagations)
        {
            if (propagationEdge.IsEscalating)
                UpdateAreaScore(areaScores, propagationEdge.SourceArea, upstream: 0, downstream: 0, escalation: 2, stabilization: 0);
        }

        var influences = new List<OperationalInfluenceDto>();

        foreach (var (area, score) in areaScores.OrderByDescending(kvp => kvp.Value.Upstream + kvp.Value.Downstream))
        {
            var influenceType = ResolveInfluenceType(area, dominantArea, score, simulationSummary);
            influences.Add(new OperationalInfluenceDto
            {
                Area = NormalizeArea(area),
                InfluenceType = influenceType,
                UpstreamInfluenceStrength = DescribeStrength(score.Upstream),
                DownstreamInfluenceStrength = DescribeStrength(score.Downstream),
                RecoveryImpact = AreasMatch(area, dominantArea)
                    ? recovery.OverallDirection.ToString()
                    : "Indirect recovery influence via dependency continuity",
                EscalationImpact = score.Escalation > 0
                    ? "Escalation concentration indicated"
                    : situationRoom.EscalationSeverity.ToString(),
                OperationalImportance = MapInfluenceCriticality(influenceType, score)
            });
        }

        if (!areaScores.ContainsKey(dominantArea))
        {
            influences.Add(new OperationalInfluenceDto
            {
                Area = dominantArea,
                InfluenceType = OperationalInfluenceType.StructuralHub,
                UpstreamInfluenceStrength = "High",
                DownstreamInfluenceStrength = "High",
                RecoveryImpact = recovery.OverallDirection.ToString(),
                EscalationImpact = causalitySummary.EscalatingPropagationCount > 0
                    ? $"{causalitySummary.EscalatingPropagationCount} escalating propagation signal(s)"
                    : "Escalation within bounded continuity",
                OperationalImportance = OperationalCriticalityLevel.High
            });
        }

        return influences
            .OrderByDescending(i => i.OperationalImportance)
            .ThenBy(i => i.Area, StringComparer.Ordinal)
            .Take(MaxInfluences)
            .ToList();
    }

    private static OperationalTopologyContinuityDto ComposeTopologyContinuity(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalIntegrityReportDto integrityReport,
        IReadOnlyList<OperationalDependencyDto> dependencies,
        IReadOnlyList<OperationalTopologySnapshot> priorTopologySnapshots)
    {
        var prior = priorTopologySnapshots.LastOrDefault();
        var topologyShift = prior != null
            && !string.Equals(prior.DominantOperationalTopology, ResolveDominantOperationalTopology(
                dominantArea, recovery, situationRoom, new OperationalPatternSummaryDto(), evolutionTimeline),
                StringComparison.OrdinalIgnoreCase)
            ? $"Topology shifted from {prior.DominantOperationalTopology.ToLowerInvariant()} toward current dominant structure"
            : OperationalContinuityPhrasing.RemainsConsistentAcrossBoundedWindow("Dominant operational topology");

        var dependencyStability = dependencies.Count > 0
            ? $"{dependencies.Count} active dependency relationship(s) in bounded window"
            : "Dependency relationships within normal continuity bounds";

        var escalationConsistency = OperationalContinuityPhrasing.EscalationMomentumAlignment(
            evolutionTimeline.EscalationMomentum,
            "Escalation topology collapsing toward stabilization",
            "Escalation topology expanding across dependency structure",
            "Escalation topology consistent within bounded window");

        var stabilizationConsistency = integrityReport.OverallIntegrityState is OperationalIntegrityState.Coherent
                or OperationalIntegrityState.MostlyCoherent
            ? "Stabilization topology aligned with integrity coherence"
            : "Stabilization topology fragmented by integrity contradictions";

        var recoveryAlignment = recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            ? "Recovery topology alignment improving"
            : "Recovery topology requires upstream stabilization focus";

        var interpretation =
            $"Operational topology in {dominantArea.ToLowerInvariant()} context with {dependencies.Count} dependency relationship(s). " +
            $"Evolution direction {evolutionTimeline.DominantEvolutionDirection.ToString().ToLowerInvariant()}.";

        return new OperationalTopologyContinuityDto
        {
            DominantTopologyShift = topologyShift,
            DependencyStability = dependencyStability,
            EscalationTopologyConsistency = escalationConsistency,
            StabilizationTopologyConsistency = stabilizationConsistency,
            RecoveryTopologyAlignment = recoveryAlignment,
            OperatorInterpretation = interpretation
        };
    }

    private static OperationalTopologyState ResolveTopologyState(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalIntegrityReportDto integrityReport,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalEvolutionTimelineDto evolutionTimeline)
    {
        if (integrityReport.OverallIntegrityState == OperationalIntegrityState.Fragmented
            || integrityReport.OverallIntegrityState == OperationalIntegrityState.Contradictory)
            return OperationalTopologyState.Fragmented;

        if (evolutionTimeline.DominantEvolutionDirection == OperationalEvolutionDirection.Escalating
            || situationRoom.StabilizationDirection == OperationalSituationDirection.Escalating)
            return OperationalTopologyState.EscalationDominant;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Converging
            && evolutionTimeline.DominantEvolutionDirection == OperationalEvolutionDirection.Converging)
            return OperationalTopologyState.RecoveryConverging;

        if (causalitySummary.EscalatingPropagationCount >= 2)
            return OperationalTopologyState.Concentrated;

        return OperationalTopologyState.Stable;
    }

    private static string ResolveDominantOperationalTopology(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalPatternSummaryDto patternSummary,
        OperationalEvolutionTimelineDto evolutionTimeline)
    {
        if (AreasMatch(dominantArea, AreaReplay))
            return "Replay-centric operational topology";

        if (AreasMatch(dominantArea, AreaRuntime)
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Escalating
                or OperationalSituationDirection.Degrading)
            return "Runtime-critical escalation topology";

        if (recovery.OverallDirection is OperationalRecoveryDirection.Converging
            && evolutionTimeline.DominantEvolutionDirection == OperationalEvolutionDirection.Converging)
            return "Recovery convergence topology";

        if (patternSummary.RecurringPatternCount > 0)
            return $"{patternSummary.DominantArchetype.ToLowerInvariant()}-dominated operational topology";

        return $"{dominantArea.ToLowerInvariant()}-centered operational topology";
    }

    private static string DescribeStabilizationDependencyStrength(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalPropagationAnalysisDto propagation,
        OperationalSimulationSummaryDto simulationSummary)
    {
        if (recovery.OverallDirection is OperationalRecoveryDirection.Converging
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Stabilizing
                or OperationalSituationDirection.Improving)
            return "Stabilization dependency chains strengthening";

        if (propagation.StabilizationBlockerCount > 0)
            return $"{propagation.StabilizationBlockerCount} stabilization blocker(s) weakening dependency flow";

        if (!string.IsNullOrWhiteSpace(simulationSummary.HighestLeverageArea))
            return $"Stabilization leverage concentrated at {simulationSummary.HighestLeverageArea.ToLowerInvariant()}";

        return "Stabilization dependency strength within normal bounds";
    }

    private static string DescribeEscalationPropagationStrength(
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline)
    {
        if (causalitySummary.EscalatingPropagationCount >= 2
            || situationRoom.EscalatingPropagationCount >= 2)
            return "Escalation propagation structurally concentrated";

        if (evolutionTimeline.EscalationMomentum.Contains("expanding", StringComparison.OrdinalIgnoreCase))
            return "Escalation propagation expanding across topology";

        if (propagation.Propagations.Any(p => p.IsCollapsing))
            return "Escalation propagation collapsing toward stabilization";

        return "Escalation propagation within bounded continuity";
    }

    private static string ComposeOperatorSummary(
        string dominantTopology,
        string highestInfluence,
        OperationalTopologyState topologyState,
        int dependencyCount,
        string stabilizationStrength,
        string escalationStrength,
        IReadOnlyList<OperationalTopologySnapshot> priorTopologySnapshots)
    {
        var continuity = string.Empty;
        if (priorTopologySnapshots.Count > 0)
        {
            var prior = priorTopologySnapshots[^1];
            if (prior.TopologyState != topologyState)
            {
                continuity =
                    $" Topology state moved from {prior.TopologyState.ToString().ToLowerInvariant()} " +
                    $"to {topologyState.ToString().ToLowerInvariant()}.";
            }
        }

        return
            $"Operational topology is {topologyState.ToString().ToLowerInvariant()} with {dependencyCount} active dependency relationship(s). " +
            $"Dominant structure: {dominantTopology.ToLowerInvariant()}. " +
            $"Highest influence: {highestInfluence.ToLowerInvariant()}. " +
            $"Stabilization: {stabilizationStrength.ToLowerInvariant()}; " +
            $"Escalation: {escalationStrength.ToLowerInvariant()}.{continuity}";
    }

    private static IReadOnlyList<string> BuildChainSequence(
        OperationalCausalChainDto chain,
        OperationalPropagationAnalysisDto propagation)
    {
        var sequence = new List<string> { NormalizeArea(chain.DominantArea) };

        var relatedPropagation = propagation.Propagations
            .FirstOrDefault(p => AreasMatch(p.SourceArea, chain.DominantArea));

        if (relatedPropagation != null)
            sequence.Add(NormalizeArea(relatedPropagation.TargetArea));

        if (!string.IsNullOrWhiteSpace(chain.RootCauseCandidate))
            sequence.Insert(0, NormalizeArea(chain.RootCauseCandidate));

        return sequence
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxDependencyChains)
            .ToList();
    }

    private static string ResolveDownstreamArea(
        OperationalCausalChainDto chain,
        OperationalPropagationAnalysisDto propagation)
    {
        var related = propagation.Propagations
            .FirstOrDefault(p => AreasMatch(p.SourceArea, chain.DominantArea));

        return related != null
            ? NormalizeArea(related.TargetArea)
            : NormalizeArea(chain.RecoveryImpact);
    }

    private static OperationalInfluenceType ResolveInfluenceType(
        string area,
        string dominantArea,
        (int Upstream, int Downstream, int Escalation, int Stabilization) score,
        OperationalSimulationSummaryDto simulationSummary)
    {
        if (AreasMatch(area, simulationSummary.HighestLeverageArea))
            return OperationalInfluenceType.StabilizationLeverage;

        if (AreasMatch(area, dominantArea))
            return OperationalInfluenceType.StructuralHub;

        if (score.Escalation >= 2)
            return OperationalInfluenceType.EscalationConcentration;

        if (score.Upstream > score.Downstream)
            return OperationalInfluenceType.UpstreamCritical;

        if (score.Downstream > score.Upstream)
            return OperationalInfluenceType.DownstreamPropagation;

        return OperationalInfluenceType.RecoveryAnchor;
    }

    private static void UpdateAreaScore(
        Dictionary<string, (int Upstream, int Downstream, int Escalation, int Stabilization)> scores,
        string area,
        int upstream,
        int downstream,
        int escalation,
        int stabilization)
    {
        var normalized = NormalizeArea(area);
        if (!scores.TryGetValue(normalized, out var current))
            current = (0, 0, 0, 0);

        scores[normalized] = (
            current.Upstream + upstream,
            current.Downstream + downstream,
            current.Escalation + escalation,
            current.Stabilization + stabilization);
    }

    private static string DescribeStrength(int score)
    {
        return score switch
        {
            >= 3 => "High",
            2 => "Elevated",
            1 => "Moderate",
            _ => "Low"
        };
    }

    private static OperationalCriticalityLevel MapNavigationCriticality(OperationalNavigationStrength strength)
    {
        return strength switch
        {
            OperationalNavigationStrength.Strong => OperationalCriticalityLevel.High,
            OperationalNavigationStrength.Moderate => OperationalCriticalityLevel.Elevated,
            _ => OperationalCriticalityLevel.Normal
        };
    }

    private static OperationalCriticalityLevel MapPlaybookCriticality(OperationalGuidanceSeverity severity)
    {
        return severity switch
        {
            OperationalGuidanceSeverity.Critical => OperationalCriticalityLevel.Critical,
            OperationalGuidanceSeverity.High => OperationalCriticalityLevel.High,
            OperationalGuidanceSeverity.Elevated => OperationalCriticalityLevel.Elevated,
            _ => OperationalCriticalityLevel.Normal
        };
    }

    private static OperationalCriticalityLevel MapEvolutionSeverity(OperationalEvolutionSeverity severity)
    {
        return severity switch
        {
            OperationalEvolutionSeverity.Critical => OperationalCriticalityLevel.Critical,
            OperationalEvolutionSeverity.High => OperationalCriticalityLevel.High,
            OperationalEvolutionSeverity.Elevated => OperationalCriticalityLevel.Elevated,
            _ => OperationalCriticalityLevel.Normal
        };
    }

    private static OperationalCriticalityLevel MapInfluenceCriticality(
        OperationalInfluenceType influenceType,
        (int Upstream, int Downstream, int Escalation, int Stabilization) score)
    {
        if (influenceType is OperationalInfluenceType.StructuralHub
            or OperationalInfluenceType.EscalationConcentration)
            return OperationalCriticalityLevel.High;

        if (score.Upstream + score.Downstream >= 3)
            return OperationalCriticalityLevel.Elevated;

        return OperationalCriticalityLevel.Normal;
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
