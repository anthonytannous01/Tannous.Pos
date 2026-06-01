using Tannous.Pos.Application.OperationalCognition;
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
using Tannous.Pos.Application.OperationalTopology;

namespace Tannous.Pos.Application.OperationalConvergence;

/// <summary>Deterministic operational signal convergence and interpretation stability analysis.</summary>
public static class OperationalConvergenceAggregation
{
    public const int MaxReinforcements = 8;
    public const int MaxDivergences = 8;
    public const int MaxAmbiguities = 8;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;

    public const string AreaReplay = "Replay";
    public const string AreaRuntime = "Runtime";
    public const string AreaReconciliation = "Reconciliation";
    public const string AreaOperational = "Operational Stability";

    public static OperationalConvergenceReportDto ComposeConvergenceReport(
        OperationalRecoveryPostureDto recovery,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalPatternSummaryDto patternSummary,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalIntegrityReportDto integrityReport,
        OperationalExperienceGraphDto experienceGraph,
        OperationalDigestDto digest,
        OperationalTopologyDto topology,
        IReadOnlyList<OperationalConvergenceSnapshot> priorConvergenceSnapshots,
        DateTime generatedAtUtc)
    {
        var dominantArea = NormalizeArea(causalitySummary.DominantOperationalArea);
        var reinforcements = ComposeReinforcements(
            dominantArea,
            recovery,
            causalitySummary,
            simulationSummary,
            playbooks,
            patternSummary,
            evolutionTimeline,
            integrityReport,
            digest,
            topology);

        var ambiguities = ComposeAmbiguities(
            dominantArea,
            recovery,
            causalitySummary,
            situationRoom,
            simulationSummary,
            evolutionTimeline,
            integrityReport,
            topology);

        var divergences = ComposeDivergences(
            dominantArea,
            recovery,
            causalitySummary,
            situationRoom,
            simulationSummary,
            evolutionTimeline,
            integrityReport,
            digest,
            topology);

        var continuity = ComposeConvergenceContinuity(
            dominantArea,
            recovery,
            situationRoom,
            evolutionTimeline,
            integrityReport,
            reinforcements,
            divergences,
            priorConvergenceSnapshots);

        var convergenceStrength = ResolveConvergenceStrength(
            reinforcements,
            divergences,
            ambiguities,
            integrityReport);

        var dominantNarrative = !string.IsNullOrWhiteSpace(integrityReport.DominantOperationalNarrative)
            ? integrityReport.DominantOperationalNarrative
            : digest.DominantOperationalStory;

        var divergencePressure = DescribeDivergencePressure(divergences, integrityReport);
        var stabilizationConfidence = DescribeStabilizationConfidence(
            recovery,
            situationRoom,
            evolutionTimeline,
            integrityReport,
            divergences);
        var escalationConfidence = DescribeEscalationConfidence(
            causalitySummary,
            situationRoom,
            evolutionTimeline,
            topology,
            divergences);

        var highestAmbiguity = ambiguities
            .OrderByDescending(a => MapAmbiguityLevel(a.SignalAgreementLevel))
            .ThenBy(a => a.OperationalArea, StringComparer.Ordinal)
            .FirstOrDefault()?.OperationalArea
            ?? dominantArea;

        var operatorSummary = ComposeOperatorSummary(
            convergenceStrength,
            dominantNarrative,
            reinforcements.Count,
            divergences.Count,
            highestAmbiguity,
            priorConvergenceSnapshots);

        return new OperationalConvergenceReportDto
        {
            GeneratedAtUtc = generatedAtUtc,
            DominantOperationalNarrative = dominantNarrative,
            ConvergenceStrength = convergenceStrength,
            DivergencePressure = divergencePressure,
            StabilizationConfidence = stabilizationConfidence,
            EscalationConfidence = escalationConfidence,
            HighestAmbiguityArea = highestAmbiguity,
            Reinforcements = reinforcements,
            Ambiguities = ambiguities,
            ConvergenceContinuity = continuity,
            OperatorSummary = operatorSummary
        };
    }

    public static OperationalConvergenceSummaryDto ComposeConvergenceSummary(
        OperationalConvergenceReportDto report,
        OperationalSituationRoomDto situationRoom,
        IReadOnlyList<OperationalDivergenceDto> divergences,
        DateTime generatedAtUtc)
    {
        var dominantArea = report.Reinforcements
            .OrderByDescending(r => r.ReinforcementStrength)
            .ThenBy(r => r.OperationalArea, StringComparer.Ordinal)
            .FirstOrDefault()?.OperationalArea
            ?? report.HighestAmbiguityArea;

        var highestDivergence = divergences
            .OrderByDescending(d => d.DivergenceSeverity)
            .ThenBy(d => d.DivergenceId, StringComparer.Ordinal)
            .FirstOrDefault()?.OperationalRisk
            ?? report.DivergencePressure;

        var strongestReinforcement = report.Reinforcements
            .OrderByDescending(r => r.ReinforcementStrength)
            .FirstOrDefault()?.OperatorInterpretation
            ?? "No strong reinforcement detected in bounded continuity window";

        var highestAmbiguity = report.Ambiguities
            .OrderByDescending(a => MapAmbiguityLevel(a.SignalAgreementLevel))
            .FirstOrDefault()?.OperatorInterpretation
            ?? report.HighestAmbiguityArea;

        var stabilityState = ResolveConvergenceState(report.ConvergenceStrength, divergences, report);

        var summary =
            $"Convergence is {report.ConvergenceStrength.ToString().ToLowerInvariant()} with {report.Reinforcements.Count} reinforcement signal(s) " +
            $"and {divergences.Count} divergence signal(s). Dominant narrative: {report.DominantOperationalNarrative.ToLowerInvariant()}.";

        return new OperationalConvergenceSummaryDto
        {
            GeneratedAtUtc = generatedAtUtc,
            DominantConvergenceArea = dominantArea,
            HighestDivergencePressure = highestDivergence,
            StrongestReinforcement = strongestReinforcement,
            HighestAmbiguityConcentration = highestAmbiguity,
            OperationalStabilityState = stabilityState,
            OperatorAttentionLevel = situationRoom.AttentionLevel.ToString(),
            Summary = summary
        };
    }

    public static IReadOnlyList<OperationalDivergenceDto> ComposeDivergences(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalIntegrityReportDto integrityReport,
        OperationalDigestDto digest,
        OperationalTopologyDto topology)
    {
        var divergences = new List<OperationalDivergenceDto>();

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            && (situationRoom.StabilizationDirection is OperationalSituationDirection.Escalating
                || topology.TopologyState == OperationalTopologyState.EscalationDominant))
        {
            divergences.Add(new OperationalDivergenceDto
            {
                DivergenceId = "divergence-runtime-ambiguity",
                OperationalArea = dominantArea,
                ConflictingLayers = new[] { "Recovery", "Topology", "Situation Room" },
                DivergenceType = OperationalDivergenceType.TopologyRecoveryMismatch,
                DivergenceSeverity = OperationalAmbiguityLevel.Elevated,
                OperationalRisk = "Recovery improving while topology escalation strengthening",
                RecommendedOperatorFocus = "Validate recovery interpretation against topology escalation signals"
            });
        }

        if (simulationSummary.DegradationScenarioCount > simulationSummary.StabilizationScenarioCount
            && recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging)
        {
            divergences.Add(new OperationalDivergenceDto
            {
                DivergenceId = "divergence-simulation-recovery-tension",
                OperationalArea = NormalizeArea(simulationSummary.HighestLeverageArea),
                ConflictingLayers = new[] { "Simulation", "Recovery" },
                DivergenceType = OperationalDivergenceType.SimulationRecoveryTension,
                DivergenceSeverity = OperationalAmbiguityLevel.Elevated,
                OperationalRisk = "Simulation degradation expanding while recovery posture improving",
                RecommendedOperatorFocus = "Review simulation leverage against recovery convergence claims"
            });
        }

        foreach (var warning in integrityReport.IntegrityWarnings.Take(3))
        {
            divergences.Add(new OperationalDivergenceDto
            {
                DivergenceId = $"divergence-integrity-{warning.WarningType.Replace(" ", "-", StringComparison.Ordinal).ToLowerInvariant()}",
                OperationalArea = NormalizeArea(warning.RelatedArea),
                ConflictingLayers = new[] { "Integrity", warning.RelatedArea },
                DivergenceType = OperationalDivergenceType.IntegrityContradiction,
                DivergenceSeverity = MapIntegritySeverity(integrityReport.ContradictionCount),
                OperationalRisk = warning.OperationalImpact,
                RecommendedOperatorFocus = warning.SuggestedOperatorFocus
            });
        }

        if (integrityReport.ContradictionCount > 0)
        {
            divergences.Add(new OperationalDivergenceDto
            {
                DivergenceId = "divergence-integrity-contradictions",
                OperationalArea = dominantArea,
                ConflictingLayers = new[] { "Integrity", "Causality", "Simulation" },
                DivergenceType = OperationalDivergenceType.IntegrityContradiction,
                DivergenceSeverity = MapIntegritySeverity(integrityReport.ContradictionCount),
                OperationalRisk = $"{integrityReport.ContradictionCount} cross-layer contradiction(s) detected",
                RecommendedOperatorFocus = "Resolve integrity contradictions before acting on condensed guidance"
            });
        }

        if (!string.Equals(digest.DominantOperationalStory, integrityReport.DominantOperationalNarrative, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(integrityReport.DominantOperationalNarrative))
        {
            divergences.Add(new OperationalDivergenceDto
            {
                DivergenceId = "divergence-narrative-shift",
                OperationalArea = dominantArea,
                ConflictingLayers = new[] { "Digest", "Integrity" },
                DivergenceType = OperationalDivergenceType.NarrativeShift,
                DivergenceSeverity = OperationalAmbiguityLevel.Moderate,
                OperationalRisk = "Condensed digest narrative diverges from integrity narrative",
                RecommendedOperatorFocus = "Reconcile digest and integrity narratives before executive condensation"
            });
        }

        if (recovery.OverallDirection is OperationalRecoveryDirection.Degrading
            or OperationalRecoveryDirection.Diverging
            && situationRoom.EscalatingPropagationCount > 0)
        {
            divergences.Add(new OperationalDivergenceDto
            {
                DivergenceId = "divergence-recovery-escalation-tension",
                OperationalArea = dominantArea,
                ConflictingLayers = new[] { "Recovery", "Situation Room", "Causality" },
                DivergenceType = OperationalDivergenceType.RecoveryEscalationTension,
                DivergenceSeverity = OperationalAmbiguityLevel.High,
                OperationalRisk = "Recovery degrading concurrent with active escalation propagation",
                RecommendedOperatorFocus = "Prioritize upstream stabilization before recovery convergence"
            });
        }

        if (evolutionTimeline.DominantEvolutionDirection == OperationalEvolutionDirection.Escalating
            && topology.TopologyState == OperationalTopologyState.RecoveryConverging)
        {
            divergences.Add(new OperationalDivergenceDto
            {
                DivergenceId = "divergence-evolution-topology-tension",
                OperationalArea = dominantArea,
                ConflictingLayers = new[] { "Evolution", "Topology" },
                DivergenceType = OperationalDivergenceType.EvolutionTopologyTension,
                DivergenceSeverity = OperationalAmbiguityLevel.Elevated,
                OperationalRisk = "Evolution escalation signal conflicts with recovery-converging topology",
                RecommendedOperatorFocus = "Validate evolution transitions against topology dependency structure"
            });
        }

        if (causalitySummary.EscalatingPropagationCount >= 2
            && recovery.OverallDirection is OperationalRecoveryDirection.Improving)
        {
            divergences.Add(new OperationalDivergenceDto
            {
                DivergenceId = "divergence-causality-recovery-tension",
                OperationalArea = dominantArea,
                ConflictingLayers = new[] { "Causality", "Recovery" },
                DivergenceType = OperationalDivergenceType.RecoveryEscalationTension,
                DivergenceSeverity = OperationalAmbiguityLevel.Moderate,
                OperationalRisk = "Escalating propagation concurrent with improving recovery posture",
                RecommendedOperatorFocus = "Confirm recovery improvement is not masking upstream escalation"
            });
        }

        return divergences
            .OrderByDescending(d => d.DivergenceSeverity)
            .ThenBy(d => d.DivergenceType)
            .ThenBy(d => d.DivergenceId, StringComparer.Ordinal)
            .Take(MaxDivergences)
            .ToList();
    }

    public static OperationalConvergenceSnapshot CreateSnapshot(
        OperationalConvergenceReportDto report,
        IReadOnlyList<OperationalDivergenceDto> divergences)
    {
        return new OperationalConvergenceSnapshot
        {
            GeneratedAtUtc = report.GeneratedAtUtc,
            ConvergenceStrength = report.ConvergenceStrength,
            ConvergenceState = ResolveConvergenceState(report.ConvergenceStrength, divergences, report),
            DominantOperationalNarrative = report.DominantOperationalNarrative,
            HighestAmbiguityArea = report.HighestAmbiguityArea,
            ReinforcementCount = report.Reinforcements.Count,
            DivergenceCount = divergences.Count
        };
    }

    private static IReadOnlyList<OperationalSignalReinforcementDto> ComposeReinforcements(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalPatternSummaryDto patternSummary,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalIntegrityReportDto integrityReport,
        OperationalDigestDto digest,
        OperationalTopologyDto topology)
    {
        var areaReinforcements = new Dictionary<string, List<(string Layer, OperationalReinforcementType Type)>>(
            StringComparer.OrdinalIgnoreCase);

        AddReinforcement(areaReinforcements, NormalizeArea(causalitySummary.DominantOperationalArea), "Causality", OperationalReinforcementType.CausalReinforcement);
        AddReinforcement(areaReinforcements, NormalizeArea(simulationSummary.HighestLeverageArea), "Simulation", OperationalReinforcementType.SimulationReinforcement);
        AddReinforcement(areaReinforcements, NormalizeArea(digest.DominantRiskArea), "Digest", OperationalReinforcementType.DigestReinforcement);
        AddReinforcement(areaReinforcements, NormalizeArea(topology.HighestInfluenceArea), "Topology", OperationalReinforcementType.TopologyReinforcement);
        AddReinforcement(areaReinforcements, NormalizeArea(patternSummary.HighestRiskPattern), "Patterns", OperationalReinforcementType.PatternReinforcement);

        var topPlaybook = playbooks.Playbooks
            .OrderByDescending(p => p.Severity)
            .FirstOrDefault();
        if (topPlaybook != null)
            AddReinforcement(areaReinforcements, NormalizeArea(topPlaybook.DominantArea), "Playbooks", OperationalReinforcementType.PlaybookReinforcement);

        if (integrityReport.AlignmentCount > integrityReport.ContradictionCount)
            AddReinforcement(areaReinforcements, dominantArea, "Integrity", OperationalReinforcementType.IntegrityReinforcement);

        if (evolutionTimeline.ActiveTransitionCount > 0)
        {
            var topTransition = evolutionTimeline.Transitions.FirstOrDefault();
            if (topTransition != null)
                AddReinforcement(areaReinforcements, NormalizeArea(topTransition.DominantArea), "Evolution", OperationalReinforcementType.EvolutionReinforcement);
        }

        if (AreasMatch(dominantArea, AreaReplay))
        {
            var replayLayers = new List<(string, OperationalReinforcementType)>();
            if (AreasMatch(causalitySummary.DominantOperationalArea, AreaReplay))
                replayLayers.Add(("Causality", OperationalReinforcementType.CausalReinforcement));
            if (AreasMatch(simulationSummary.HighestLeverageArea, AreaReplay))
                replayLayers.Add(("Simulation", OperationalReinforcementType.SimulationReinforcement));
            if (playbooks.Playbooks.Any(p => AreasMatch(p.DominantArea, AreaReplay)))
                replayLayers.Add(("Playbooks", OperationalReinforcementType.PlaybookReinforcement));
            if (AreasMatch(topology.HighestInfluenceArea, AreaReplay))
                replayLayers.Add(("Topology", OperationalReinforcementType.TopologyReinforcement));
            if (integrityReport.AlignmentCount > 0)
                replayLayers.Add(("Integrity", OperationalReinforcementType.IntegrityReinforcement));

            if (replayLayers.Count >= 3)
            {
                return new List<OperationalSignalReinforcementDto>
                {
                    new()
                    {
                        OperationalArea = AreaReplay,
                        ReinforcingLayers = replayLayers.Select(l => l.Item1).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                        ReinforcementStrength = OperationalConvergenceStrength.Strong,
                        SharedOperationalDirection = recovery.OverallDirection.ToString(),
                        SharedStabilizationInterpretation = digest.StabilizationPriority,
                        SharedEscalationInterpretation = causalitySummary.HighestRiskPropagation,
                        OperatorInterpretation =
                            "Strong replay reinforcement: causality, simulation, playbooks, topology, and integrity identify replay as dominant upstream instability"
                    }
                };
            }
        }

        var results = areaReinforcements
            .Select(kvp => new OperationalSignalReinforcementDto
            {
                OperationalArea = kvp.Key,
                ReinforcingLayers = kvp.Value.Select(v => v.Layer).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                ReinforcementStrength = ResolveReinforcementStrength(kvp.Value.Count),
                SharedOperationalDirection = recovery.OverallDirection.ToString(),
                SharedStabilizationInterpretation = situationStabilizationInterpretation(recovery, digest),
                SharedEscalationInterpretation = causalitySummary.EscalatingPropagationCount > 0
                    ? $"{causalitySummary.EscalatingPropagationCount} escalating propagation signal(s)"
                    : "Escalation within bounded continuity",
                OperatorInterpretation =
                    $"{kvp.Value.Count} layer(s) reinforce operational focus on {kvp.Key.ToLowerInvariant()}: " +
                    string.Join(", ", kvp.Value.Select(v => v.Layer.ToLowerInvariant()))
            })
            .OrderByDescending(r => r.ReinforcementStrength)
            .ThenByDescending(r => r.ReinforcingLayers.Count)
            .ThenBy(r => r.OperationalArea, StringComparer.Ordinal)
            .Take(MaxReinforcements)
            .ToList();

        return results;
    }

    private static IReadOnlyList<OperationalAmbiguityAnalysisDto> ComposeAmbiguities(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalIntegrityReportDto integrityReport,
        OperationalTopologyDto topology)
    {
        var ambiguities = new List<OperationalAmbiguityAnalysisDto>();

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            && topology.TopologyState == OperationalTopologyState.EscalationDominant)
        {
            ambiguities.Add(new OperationalAmbiguityAnalysisDto
            {
                OperationalArea = dominantArea,
                AmbiguitySource = "Recovery-topology signal disagreement",
                SignalAgreementLevel = OperationalConvergenceStrength.Weak,
                StabilizationUncertainty = "Stabilization interpretation unstable across recovery and topology",
                EscalationUncertainty = "Escalation topology strengthening despite recovery improvement",
                RecoveryUncertainty = "Recovery improvement may not reflect upstream escalation pressure",
                OperatorInterpretation = "Runtime ambiguity: recovery improving while topology escalation strengthening"
            });
        }

        if (simulationSummary.DegradationScenarioCount > simulationSummary.StabilizationScenarioCount)
        {
            ambiguities.Add(new OperationalAmbiguityAnalysisDto
            {
                OperationalArea = NormalizeArea(simulationSummary.HighestLeverageArea),
                AmbiguitySource = "Simulation degradation expansion",
                SignalAgreementLevel = OperationalConvergenceStrength.Moderate,
                StabilizationUncertainty = "Stabilization paths weakly supported by simulation leverage",
                EscalationUncertainty = "Degradation scenarios outnumber stabilization scenarios",
                RecoveryUncertainty = recovery.OverallDirection.ToString(),
                OperatorInterpretation =
                    "Simulation ambiguity: degradation expanding relative to stabilization scenario balance"
            });
        }

        if (integrityReport.ContradictionCount >= 2)
        {
            ambiguities.Add(new OperationalAmbiguityAnalysisDto
            {
                OperationalArea = dominantArea,
                AmbiguitySource = "Integrity contradiction concentration",
                SignalAgreementLevel = OperationalConvergenceStrength.Fragmented,
                StabilizationUncertainty = "Cross-layer stabilization agreement reduced",
                EscalationUncertainty = integrityReport.AlignmentState,
                RecoveryUncertainty = "Recovery interpretation requires integrity reconciliation",
                OperatorInterpretation =
                    $"{integrityReport.ContradictionCount} integrity contradiction(s) concentrate operational ambiguity"
            });
        }

        if (evolutionTimeline.EscalationMomentum.Contains("expanding", StringComparison.OrdinalIgnoreCase)
            && evolutionTimeline.RecoveryMomentum.Contains("accelerating", StringComparison.OrdinalIgnoreCase))
        {
            ambiguities.Add(new OperationalAmbiguityAnalysisDto
            {
                OperationalArea = dominantArea,
                AmbiguitySource = "Evolution momentum tension",
                SignalAgreementLevel = OperationalConvergenceStrength.Weak,
                StabilizationUncertainty = "Stabilization and escalation momentum diverging",
                EscalationUncertainty = evolutionTimeline.EscalationMomentum,
                RecoveryUncertainty = evolutionTimeline.RecoveryMomentum,
                OperatorInterpretation = "Evolution ambiguity: recovery accelerating while escalation expanding"
            });
        }

        if (causalitySummary.StabilizationBlockerCount > 0
            && recovery.OverallDirection is OperationalRecoveryDirection.Converging)
        {
            ambiguities.Add(new OperationalAmbiguityAnalysisDto
            {
                OperationalArea = dominantArea,
                AmbiguitySource = "Stabilization blocker continuity",
                SignalAgreementLevel = OperationalConvergenceStrength.Moderate,
                StabilizationUncertainty = $"{causalitySummary.StabilizationBlockerCount} stabilization blocker(s) weaken certainty",
                EscalationUncertainty = causalitySummary.HighestRiskPropagation,
                RecoveryUncertainty = "Recovery convergence may be blocked upstream",
                OperatorInterpretation = "Stabilization ambiguity: blockers present despite recovery convergence signal"
            });
        }

        if (topology.TopologyState == OperationalTopologyState.Fragmented)
        {
            ambiguities.Add(new OperationalAmbiguityAnalysisDto
            {
                OperationalArea = topology.HighestInfluenceArea,
                AmbiguitySource = "Fragmented topology structure",
                SignalAgreementLevel = OperationalConvergenceStrength.Weak,
                StabilizationUncertainty = topology.StabilizationDependencyStrength,
                EscalationUncertainty = topology.EscalationPropagationStrength,
                RecoveryUncertainty = "Dependency structure fragmented across operational areas",
                OperatorInterpretation = "Topology ambiguity: fragmented dependency structure reduces convergence certainty"
            });
        }

        if (situationRoom.StabilizationDirection is OperationalSituationDirection.Stable
            && evolutionTimeline.DominantEvolutionDirection == OperationalEvolutionDirection.Escalating)
        {
            ambiguities.Add(new OperationalAmbiguityAnalysisDto
            {
                OperationalArea = dominantArea,
                AmbiguitySource = "Situation-evolution stabilization disagreement",
                SignalAgreementLevel = OperationalConvergenceStrength.Moderate,
                StabilizationUncertainty = situationRoom.StabilizationDirection.ToString(),
                EscalationUncertainty = evolutionTimeline.DominantEvolutionDirection.ToString(),
                RecoveryUncertainty = recovery.OverallDirection.ToString(),
                OperatorInterpretation = "Stabilization ambiguity: situation room stable while evolution escalating"
            });
        }

        return ambiguities
            .OrderByDescending(a => MapAmbiguityLevel(a.SignalAgreementLevel))
            .ThenBy(a => a.OperationalArea, StringComparer.Ordinal)
            .Take(MaxAmbiguities)
            .ToList();
    }

    private static OperationalConvergenceContinuityDto ComposeConvergenceContinuity(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalIntegrityReportDto integrityReport,
        IReadOnlyList<OperationalSignalReinforcementDto> reinforcements,
        IReadOnlyList<OperationalDivergenceDto> divergences,
        IReadOnlyList<OperationalConvergenceSnapshot> priorSnapshots)
    {
        var prior = priorSnapshots.LastOrDefault();
        var convergenceShift = prior != null && prior.ConvergenceStrength != ResolveConvergenceStrength(reinforcements, divergences, Array.Empty<OperationalAmbiguityAnalysisDto>(), integrityReport)
            ? $"Convergence strength shifted from {prior.ConvergenceStrength.ToString().ToLowerInvariant()} toward current bounded interpretation"
            : OperationalContinuityPhrasing.ConsistentAcrossBoundedWindow("Convergence strength");

        var reinforcementStability = reinforcements.Any(r => r.ReinforcementStrength == OperationalConvergenceStrength.Strong)
            ? "Strong reinforcement signals sustained across layers"
            : "Reinforcement signals moderate or fragmented in bounded window";

        var divergenceConsistency = divergences.Count > 0
            ? $"{divergences.Count} divergence signal(s) sustained across continuity"
            : "Divergence within normal bounded continuity";

        var recoveryAlignment = OperationalContinuityPhrasing.RecoveryAlignment(
            recovery,
            "Recovery convergence alignment improving",
            "Recovery convergence requires upstream stabilization focus");

        if (integrityReport.ContradictionCount < (prior?.DivergenceCount ?? int.MaxValue))
            recoveryAlignment += "; integrity contradictions decreasing";

        var escalationAlignment = OperationalContinuityPhrasing.EscalationMomentumAlignment(
            evolutionTimeline.EscalationMomentum,
            "Escalation convergence collapsing toward stabilization",
            "Escalation convergence expanding across signals",
            "Escalation convergence stable within bounded window");

        var interpretation =
            $"Operational convergence in {dominantArea.ToLowerInvariant()} context. " +
            $"{reinforcements.Count} reinforcement(s), {divergences.Count} divergence(s). " +
            $"Integrity state {integrityReport.OverallIntegrityState.ToString().ToLowerInvariant()}.";

        return new OperationalConvergenceContinuityDto
        {
            DominantConvergenceShift = convergenceShift,
            ReinforcementStability = reinforcementStability,
            DivergenceConsistency = divergenceConsistency,
            RecoveryConvergenceAlignment = recoveryAlignment,
            EscalationConvergenceAlignment = escalationAlignment,
            OperatorInterpretation = interpretation
        };
    }

    private static OperationalConvergenceStrength ResolveConvergenceStrength(
        IReadOnlyList<OperationalSignalReinforcementDto> reinforcements,
        IReadOnlyList<OperationalDivergenceDto> divergences,
        IReadOnlyList<OperationalAmbiguityAnalysisDto> ambiguities,
        OperationalIntegrityReportDto integrityReport)
    {
        if (integrityReport.OverallIntegrityState == OperationalIntegrityState.Fragmented
            || integrityReport.OverallIntegrityState == OperationalIntegrityState.Contradictory)
            return OperationalConvergenceStrength.Fragmented;

        var strongReinforcements = reinforcements.Count(r => r.ReinforcementStrength == OperationalConvergenceStrength.Strong);
        if (strongReinforcements >= 1 && divergences.Count == 0)
            return OperationalConvergenceStrength.Strong;

        if (divergences.Any(d => d.DivergenceSeverity == OperationalAmbiguityLevel.High))
            return OperationalConvergenceStrength.Weak;

        if (reinforcements.Count >= 2 && divergences.Count <= 1)
            return OperationalConvergenceStrength.Moderate;

        if (ambiguities.Any(a => a.SignalAgreementLevel == OperationalConvergenceStrength.Fragmented))
            return OperationalConvergenceStrength.Fragmented;

        return OperationalConvergenceStrength.Moderate;
    }

    private static OperationalConvergenceState ResolveConvergenceState(
        OperationalConvergenceStrength strength,
        IReadOnlyList<OperationalDivergenceDto> divergences,
        OperationalConvergenceReportDto report)
    {
        if (strength == OperationalConvergenceStrength.Strong)
            return OperationalConvergenceState.Converged;

        if (strength == OperationalConvergenceStrength.Fragmented)
            return OperationalConvergenceState.Fragmented;

        if (divergences.Count >= 3)
            return OperationalConvergenceState.Diverging;

        if (report.Ambiguities.Count >= 2)
            return OperationalConvergenceState.Ambiguous;

        return OperationalConvergenceState.MostlyConverged;
    }

    private static string DescribeDivergencePressure(
        IReadOnlyList<OperationalDivergenceDto> divergences,
        OperationalIntegrityReportDto integrityReport)
    {
        if (divergences.Any(d => d.DivergenceSeverity == OperationalAmbiguityLevel.High))
            return "High divergence pressure across operational signals";

        if (integrityReport.ContradictionCount >= 2)
            return $"{integrityReport.ContradictionCount} integrity contradiction(s) elevate divergence pressure";

        if (divergences.Count > 0)
            return $"{divergences.Count} divergence signal(s) in bounded continuity window";

        return "Divergence pressure within normal bounded continuity";
    }

    private static string DescribeStabilizationConfidence(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalIntegrityReportDto integrityReport,
        IReadOnlyList<OperationalDivergenceDto> divergences)
    {
        if (recovery.OverallDirection is OperationalRecoveryDirection.Converging
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Stabilizing
                or OperationalSituationDirection.Improving
            && evolutionTimeline.StabilizationMomentum.Contains("accelerating", StringComparison.OrdinalIgnoreCase)
            && integrityReport.ContradictionCount == 0)
            return "Strong stabilization convergence across recovery, situation, and evolution signals";

        if (divergences.Any(d => d.DivergenceType == OperationalDivergenceType.TopologyRecoveryMismatch))
            return "Stabilization interpretation weakened by recovery-topology disagreement";

        if (integrityReport.ContradictionCount > 0)
            return "Stabilization certainty reduced by integrity contradictions";

        return "Stabilization certainty within moderate bounded continuity";
    }

    private static string DescribeEscalationConfidence(
        OperationalCausalitySummaryDto causalitySummary,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        IReadOnlyList<OperationalDivergenceDto> divergences)
    {
        if (causalitySummary.EscalatingPropagationCount == 0
            && evolutionTimeline.EscalationMomentum.Contains("collapsing", StringComparison.OrdinalIgnoreCase))
            return "Strong escalation convergence: propagation collapsing across signals";

        if (topology.TopologyState == OperationalTopologyState.EscalationDominant)
            return "Escalation interpretation strongly reinforced by topology structure";

        if (divergences.Any(d => d.DivergenceType == OperationalDivergenceType.RecoveryEscalationTension))
            return "Escalation certainty weakened by recovery-escalation tension";

        if (situationRoom.EscalatingPropagationCount > 0)
            return $"{situationRoom.EscalatingPropagationCount} escalating propagation signal(s) sustain escalation interpretation";

        return "Escalation certainty within bounded continuity";
    }

    private static string ComposeOperatorSummary(
        OperationalConvergenceStrength strength,
        string dominantNarrative,
        int reinforcementCount,
        int divergenceCount,
        string highestAmbiguity,
        IReadOnlyList<OperationalConvergenceSnapshot> priorSnapshots)
    {
        var continuity = string.Empty;
        if (priorSnapshots.Count > 0)
        {
            var prior = priorSnapshots[^1];
            if (prior.ConvergenceStrength != strength)
            {
                continuity =
                    $" Convergence moved from {prior.ConvergenceStrength.ToString().ToLowerInvariant()} " +
                    $"to {strength.ToString().ToLowerInvariant()}.";
            }
        }

        return
            $"Operational convergence is {strength.ToString().ToLowerInvariant()} with {reinforcementCount} reinforcement signal(s) " +
            $"and {divergenceCount} divergence signal(s). " +
            $"Dominant narrative: {dominantNarrative.ToLowerInvariant()}. " +
            $"Highest ambiguity: {highestAmbiguity.ToLowerInvariant()}.{continuity}";
    }

    private static void AddReinforcement(
        Dictionary<string, List<(string Layer, OperationalReinforcementType Type)>> map,
        string area,
        string layer,
        OperationalReinforcementType type)
    {
        if (string.IsNullOrWhiteSpace(area))
            return;

        if (!map.TryGetValue(area, out var list))
        {
            list = new List<(string, OperationalReinforcementType)>();
            map[area] = list;
        }

        list.Add((layer, type));
    }

    private static OperationalConvergenceStrength ResolveReinforcementStrength(int layerCount)
    {
        return layerCount switch
        {
            >= 4 => OperationalConvergenceStrength.Strong,
            3 => OperationalConvergenceStrength.Moderate,
            2 => OperationalConvergenceStrength.Moderate,
            _ => OperationalConvergenceStrength.Weak
        };
    }

    private static string situationStabilizationInterpretation(
        OperationalRecoveryPostureDto recovery,
        OperationalDigestDto digest)
    {
        return recovery.OverallDirection is OperationalRecoveryDirection.Converging
            ? digest.StabilizationPriority
            : "Stabilization requires upstream focus";
    }

    private static OperationalAmbiguityLevel MapIntegritySeverity(int contradictionCount)
    {
        return contradictionCount switch
        {
            >= 3 => OperationalAmbiguityLevel.High,
            2 => OperationalAmbiguityLevel.Elevated,
            1 => OperationalAmbiguityLevel.Moderate,
            _ => OperationalAmbiguityLevel.Low
        };
    }

    private static int MapAmbiguityLevel(OperationalConvergenceStrength strength)
    {
        return strength switch
        {
            OperationalConvergenceStrength.Fragmented => 4,
            OperationalConvergenceStrength.Weak => 3,
            OperationalConvergenceStrength.Moderate => 2,
            _ => 1
        };
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
