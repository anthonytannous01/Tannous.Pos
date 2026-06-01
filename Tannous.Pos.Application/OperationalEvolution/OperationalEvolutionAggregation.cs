using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalExperienceGraph;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTrends;

using Tannous.Pos.Application.OperationalCognition;

namespace Tannous.Pos.Application.OperationalEvolution;

/// <summary>Deterministic operational evolution and transition intelligence from bounded snapshot continuity.</summary>
public static class OperationalEvolutionAggregation
{
    public const int MaxTransitions = 8;
    public const int MaxPhases = 8;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;

    public const string AreaReplay = "Replay";
    public const string AreaRuntime = "Runtime";
    public const string AreaReconciliation = "Reconciliation";
    public const string AreaOperational = "Operational Stability";

    public static OperationalEvolutionTimelineDto ComposeEvolutionTimeline(
        OperationalTrendSummaryDto trend,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalSituationRoomDto situationRoom,
        OperationalDigestDto digest,
        OperationalIntegrityReportDto integrityReport,
        OperationalPatternSummaryDto patternSummary,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalExperienceGraphDto experienceGraph,
        IReadOnlyList<OperationalDigestSnapshot> digestSnapshots,
        IReadOnlyList<OperationalIntegritySnapshot> integritySnapshots,
        IReadOnlyList<OperationalSituationSnapshot> situationSnapshots,
        IReadOnlyList<OperationalPatternSnapshot> patternSnapshots,
        IReadOnlyList<OperationalExperienceSnapshot> experienceSnapshots,
        IReadOnlyList<OperationalSimulationSnapshot> simulationSnapshots,
        IReadOnlyList<OperationalEvolutionSnapshot> priorEvolutionSnapshots,
        DateTime generatedAtUtc)
    {
        var dominantArea = NormalizeArea(causalitySummary.DominantOperationalArea);
        var momentum = ComposeMomentumAnalysis(
            recovery,
            situationRoom,
            integrityReport,
            digest,
            integritySnapshots,
            situationSnapshots,
            generatedAtUtc);

        var transitions = ComposeTransitions(
            dominantArea,
            recovery,
            incidentSummary,
            situationRoom,
            digest,
            integrityReport,
            patternSummary,
            simulationSummary,
            experienceGraph,
            digestSnapshots,
            integritySnapshots,
            situationSnapshots,
            patternSnapshots,
            experienceSnapshots,
            simulationSnapshots);

        var phases = ComposePhases(
            dominantArea,
            recovery,
            situationRoom,
            digest,
            integrityReport,
            patternSummary,
            digestSnapshots,
            integritySnapshots,
            situationSnapshots);

        var continuity = ComposeEvolutionContinuity(
            dominantArea,
            recovery,
            situationRoom,
            digest,
            integrityReport,
            experienceGraph,
            digestSnapshots,
            integritySnapshots,
            patternSnapshots,
            transitions);

        var dominantDirection = ResolveDominantEvolutionDirection(
            recovery,
            situationRoom,
            integrityReport,
            momentum,
            transitions);

        var dominantShift = ResolveDominantOperationalShift(transitions, digestSnapshots, integritySnapshots);
        var operatorSummary = ComposeOperatorSummary(
            dominantDirection,
            transitions.Count,
            dominantShift,
            momentum,
            priorEvolutionSnapshots);

        return new OperationalEvolutionTimelineDto
        {
            GeneratedAtUtc = generatedAtUtc,
            DominantEvolutionDirection = dominantDirection,
            ActiveTransitionCount = transitions.Count,
            RecoveryMomentum = momentum.RecoveryMomentum,
            EscalationMomentum = momentum.EscalationMomentum,
            StabilizationMomentum = momentum.StabilizationMomentum,
            DominantOperationalShift = dominantShift,
            Transitions = transitions,
            Phases = phases,
            EvolutionContinuity = continuity,
            OperatorSummary = operatorSummary
        };
    }

    public static OperationalEvolutionSummaryDto ComposeEvolutionSummary(
        OperationalEvolutionTimelineDto timeline,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalMomentumAnalysisDto momentum,
        DateTime generatedAtUtc)
    {
        var dominantTransition = timeline.Transitions
            .OrderByDescending(t => t.Severity)
            .ThenBy(t => t.TransitionId, StringComparer.Ordinal)
            .FirstOrDefault()?.OperatorInterpretation
            ?? "No dominant transition detected in bounded continuity window";

        var recoveryDirection = momentum.RecoveryMomentumState switch
        {
            OperationalMomentumState.Accelerating => "Recovery accelerating across bounded continuity",
            OperationalMomentumState.Slowing => "Recovery slowing; upstream stabilization may be required",
            OperationalMomentumState.Plateauing => "Recovery plateauing within current operational phase",
            _ => $"Recovery {recovery.OverallDirection.ToString().ToLowerInvariant()} with stable momentum"
        };

        var escalationDirection = momentum.EscalationMomentumState switch
        {
            OperationalMomentumState.Expanding => "Escalation expanding across operational surfaces",
            OperationalMomentumState.Collapsing => "Escalation collapsing toward stabilization",
            OperationalMomentumState.Slowing => "Escalation slowing but remains active",
            _ => "Escalation momentum stable within continuity bounds"
        };

        var stabilizationDirection = momentum.StabilizationMomentumState switch
        {
            OperationalMomentumState.Accelerating => "Stabilization convergence strengthening",
            OperationalMomentumState.Plateauing => "Stabilization plateauing at current posture",
            OperationalMomentumState.Slowing => "Stabilization slowing; review dominant constraints",
            _ => "Stabilization momentum stable"
        };

        var overallMomentum = ResolveOverallMomentumState(momentum);
        var attention = situationRoom.AttentionLevel.ToString();

        var summary =
            $"Evolution direction is {timeline.DominantEvolutionDirection.ToString().ToLowerInvariant()} " +
            $"with {timeline.ActiveTransitionCount} active transition(s). " +
            $"Dominant shift: {timeline.DominantOperationalShift.ToLowerInvariant()}.";

        return new OperationalEvolutionSummaryDto
        {
            GeneratedAtUtc = generatedAtUtc,
            DominantTransition = dominantTransition,
            RecoveryDirection = recoveryDirection,
            EscalationDirection = escalationDirection,
            StabilizationDirection = stabilizationDirection,
            OperationalMomentumState = overallMomentum,
            OperatorAttentionLevel = attention,
            Summary = summary
        };
    }

    public static OperationalMomentumAnalysisDto ComposeMomentumAnalysis(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalIntegrityReportDto integrityReport,
        OperationalDigestDto digest,
        IReadOnlyList<OperationalIntegritySnapshot> integritySnapshots,
        IReadOnlyList<OperationalSituationSnapshot> situationSnapshots,
        DateTime generatedAtUtc)
    {
        var priorIntegrity = integritySnapshots.LastOrDefault();
        var priorSituation = situationSnapshots.LastOrDefault();

        var recoveryState = ResolveRecoveryMomentumState(recovery, priorIntegrity, integrityReport);
        var escalationState = ResolveEscalationMomentumState(situationRoom, priorSituation);
        var stabilizationState = ResolveStabilizationMomentumState(recovery, situationRoom, priorIntegrity, integrityReport);

        var recoveryMomentum = DescribeMomentum(recoveryState, "recovery");
        var escalationMomentum = DescribeMomentum(escalationState, "escalation");
        var stabilizationMomentum = DescribeMomentum(stabilizationState, "stabilization");

        var acceleration = recoveryState == OperationalMomentumState.Accelerating
            ? "Recovery acceleration dominant in bounded continuity window"
            : escalationState == OperationalMomentumState.Expanding
                ? "Escalation expansion dominant in bounded continuity window"
                : "No strong acceleration signal in bounded continuity window";

        var deceleration = recoveryState == OperationalMomentumState.Slowing
            ? "Recovery deceleration indicated by continuity comparison"
            : escalationState == OperationalMomentumState.Collapsing
                ? "Escalation deceleration indicated by continuity comparison"
                : "No strong deceleration signal in bounded continuity window";

        var confidence = integrityReport.OverallIntegrityState switch
        {
            OperationalIntegrityState.Coherent when recoveryState == OperationalMomentumState.Accelerating
                => "High operational evolution confidence",
            OperationalIntegrityState.MostlyCoherent => "Moderate operational evolution confidence",
            OperationalIntegrityState.Fragmented => "Reduced evolution confidence due to fragmented continuity",
            _ => "Low evolution confidence until integrity contradictions resolve"
        };

        return new OperationalMomentumAnalysisDto
        {
            GeneratedAtUtc = generatedAtUtc,
            RecoveryMomentum = recoveryMomentum,
            EscalationMomentum = escalationMomentum,
            StabilizationMomentum = stabilizationMomentum,
            RecoveryMomentumState = recoveryState,
            EscalationMomentumState = escalationState,
            StabilizationMomentumState = stabilizationState,
            DominantOperationalAcceleration = acceleration,
            DominantOperationalDeceleration = deceleration,
            OperationalConfidence = confidence
        };
    }

    public static OperationalEvolutionSnapshot CreateSnapshot(OperationalEvolutionTimelineDto timeline)
    {
        return new OperationalEvolutionSnapshot
        {
            GeneratedAtUtc = timeline.GeneratedAtUtc,
            DominantEvolutionDirection = timeline.DominantEvolutionDirection,
            RecoveryMomentum = timeline.RecoveryMomentum,
            EscalationMomentum = timeline.EscalationMomentum,
            StabilizationMomentum = timeline.StabilizationMomentum,
            ActiveTransitionCount = timeline.ActiveTransitionCount,
            DominantOperationalShift = timeline.DominantOperationalShift
        };
    }

    private static IReadOnlyList<OperationalTransitionDto> ComposeTransitions(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalSituationRoomDto situationRoom,
        OperationalDigestDto digest,
        OperationalIntegrityReportDto integrityReport,
        OperationalPatternSummaryDto patternSummary,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalExperienceGraphDto experienceGraph,
        IReadOnlyList<OperationalDigestSnapshot> digestSnapshots,
        IReadOnlyList<OperationalIntegritySnapshot> integritySnapshots,
        IReadOnlyList<OperationalSituationSnapshot> situationSnapshots,
        IReadOnlyList<OperationalPatternSnapshot> patternSnapshots,
        IReadOnlyList<OperationalExperienceSnapshot> experienceSnapshots,
        IReadOnlyList<OperationalSimulationSnapshot> simulationSnapshots)
    {
        var transitions = new List<OperationalTransitionDto>();
        var priorDigest = digestSnapshots.LastOrDefault();
        var priorIntegrity = integritySnapshots.LastOrDefault();
        var priorSituation = situationSnapshots.LastOrDefault();
        var priorPattern = patternSnapshots.LastOrDefault();
        var priorExperience = experienceSnapshots.LastOrDefault();
        var priorSimulation = simulationSnapshots.LastOrDefault();

        if (priorDigest != null && priorDigest.DigestState != digest.DigestState)
        {
            transitions.Add(new OperationalTransitionDto
            {
                TransitionId = "transition-digest-state-shift",
                SourceState = priorDigest.DigestState.ToString(),
                TargetState = digest.DigestState.ToString(),
                TransitionType = OperationalTransitionType.PhaseTransition,
                DominantArea = dominantArea,
                Direction = MapDigestStateDirection(priorDigest.DigestState, digest.DigestState),
                Severity = OperationalEvolutionSeverity.Elevated,
                TransitionReason = "Condensed digest state shifted between continuity snapshots",
                OperationalImpact = "Operator focus and condensation narrative may require recalibration",
                OperatorInterpretation =
                    $"Operational digest evolved from {priorDigest.DigestState.ToString().ToLowerInvariant()} to {digest.DigestState.ToString().ToLowerInvariant()}"
            });
        }

        if (priorIntegrity != null)
        {
            if (priorIntegrity.IntegrityState != integrityReport.OverallIntegrityState)
            {
                transitions.Add(new OperationalTransitionDto
                {
                    TransitionId = "transition-integrity-state-shift",
                    SourceState = priorIntegrity.IntegrityState.ToString(),
                    TargetState = integrityReport.OverallIntegrityState.ToString(),
                    TransitionType = OperationalTransitionType.IntegrityShift,
                    DominantArea = dominantArea,
                    Direction = MapIntegrityDirection(priorIntegrity, integrityReport),
                    Severity = integrityReport.ContradictionCount > priorIntegrity.ContradictionCount
                        ? OperationalEvolutionSeverity.High
                        : OperationalEvolutionSeverity.Elevated,
                    TransitionReason = "Cross-layer integrity state changed between continuity snapshots",
                    OperationalImpact = "Operational coherence interpretation shifted across intelligence layers",
                    OperatorInterpretation =
                        $"Integrity evolved from {priorIntegrity.IntegrityState.ToString().ToLowerInvariant()} to {integrityReport.OverallIntegrityState.ToString().ToLowerInvariant()}"
                });
            }

            if (integrityReport.ContradictionCount < priorIntegrity.ContradictionCount)
            {
                transitions.Add(new OperationalTransitionDto
                {
                    TransitionId = "transition-integrity-convergence",
                    SourceState = $"{priorIntegrity.ContradictionCount} contradiction(s)",
                    TargetState = $"{integrityReport.ContradictionCount} contradiction(s)",
                    TransitionType = OperationalTransitionType.RecoveryProgression,
                    DominantArea = dominantArea,
                    Direction = OperationalEvolutionDirection.Converging,
                    Severity = OperationalEvolutionSeverity.Normal,
                    TransitionReason = "Integrity contradictions decreased across continuity snapshots",
                    OperationalImpact = "Operational coherence improving; navigation focus may narrow",
                    OperatorInterpretation = "Stabilization convergence strengthening with decreasing integrity contradictions"
                });
            }

            if (integrityReport.ConsistencyScore > priorIntegrity.ConsistencyScore)
            {
                transitions.Add(new OperationalTransitionDto
                {
                    TransitionId = "transition-consistency-improvement",
                    SourceState = $"Score {priorIntegrity.ConsistencyScore}",
                    TargetState = $"Score {integrityReport.ConsistencyScore}",
                    TransitionType = OperationalTransitionType.StabilizationShift,
                    DominantArea = dominantArea,
                    Direction = OperationalEvolutionDirection.Improving,
                    Severity = OperationalEvolutionSeverity.Normal,
                    TransitionReason = "Integrity consistency score improved between snapshots",
                    OperationalImpact = "Cross-layer operational trust alignment strengthening",
                    OperatorInterpretation = "Operational coherence improving across bounded continuity window"
                });
            }
        }

        if (priorSituation != null
            && priorSituation.StabilizationDirection != situationRoom.StabilizationDirection)
        {
            transitions.Add(new OperationalTransitionDto
            {
                TransitionId = "transition-stabilization-direction-shift",
                SourceState = priorSituation.StabilizationDirection.ToString(),
                TargetState = situationRoom.StabilizationDirection.ToString(),
                TransitionType = OperationalTransitionType.StabilizationShift,
                DominantArea = dominantArea,
                Direction = MapSituationDirection(priorSituation.StabilizationDirection, situationRoom.StabilizationDirection),
                Severity = situationRoom.StabilizationDirection == OperationalSituationDirection.Escalating
                    ? OperationalEvolutionSeverity.High
                    : OperationalEvolutionSeverity.Elevated,
                TransitionReason = "Situation room stabilization direction changed between snapshots",
                OperationalImpact = "Executive briefing stabilization interpretation shifted",
                OperatorInterpretation =
                    $"Stabilization direction moved from {priorSituation.StabilizationDirection.ToString().ToLowerInvariant()} to {situationRoom.StabilizationDirection.ToString().ToLowerInvariant()}"
            });
        }

        if (priorSituation != null
            && priorSituation.EscalatingPropagationCount < situationRoom.EscalatingPropagationCount)
        {
            transitions.Add(new OperationalTransitionDto
            {
                TransitionId = "transition-escalation-expansion",
                SourceState = $"{priorSituation.EscalatingPropagationCount} escalating propagation(s)",
                TargetState = $"{situationRoom.EscalatingPropagationCount} escalating propagation(s)",
                TransitionType = OperationalTransitionType.EscalationProgression,
                DominantArea = dominantArea,
                Direction = OperationalEvolutionDirection.Escalating,
                Severity = OperationalEvolutionSeverity.High,
                TransitionReason = "Escalating propagation count increased between situation snapshots",
                OperationalImpact = "Propagation expansion; recovery may decelerate",
                OperatorInterpretation = "Runtime or cross-domain escalation transition with expanding propagation pressure"
            });
        }

        if (AreasMatch(dominantArea, AreaReplay)
            && recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            && priorSituation?.StabilizationDirection is OperationalSituationDirection.Escalating
                or OperationalSituationDirection.Degrading)
        {
            transitions.Add(new OperationalTransitionDto
            {
                TransitionId = "transition-replay-recovery",
                SourceState = "Replay escalation",
                TargetState = "Replay stabilization",
                TransitionType = OperationalTransitionType.RecoveryProgression,
                DominantArea = AreaReplay,
                Direction = OperationalEvolutionDirection.Improving,
                Severity = OperationalEvolutionSeverity.Elevated,
                TransitionReason = "Replay area recovery improving after prior escalation pressure",
                OperationalImpact = "Reconciliation alignment may improve; escalation momentum may decrease",
                OperatorInterpretation =
                    "Replay recovery transition: escalation momentum decreasing with stabilization confidence increasing"
            });
        }

        if (AreasMatch(dominantArea, AreaRuntime)
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Degrading
                or OperationalSituationDirection.Escalating)
        {
            transitions.Add(new OperationalTransitionDto
            {
                TransitionId = "transition-runtime-escalation",
                SourceState = "Runtime survivability pressure",
                TargetState = "Runtime escalation increasing",
                TransitionType = OperationalTransitionType.EscalationProgression,
                DominantArea = AreaRuntime,
                Direction = OperationalEvolutionDirection.Escalating,
                Severity = OperationalEvolutionSeverity.Critical,
                TransitionReason = "Runtime survivability pressure increasing in current operational posture",
                OperationalImpact = "Propagation expansion likely; recovery deceleration indicated",
                OperatorInterpretation =
                    "Runtime escalation transition: survivability pressure increasing with recovery deceleration"
            });
        }

        if (priorPattern != null
            && !string.Equals(priorPattern.DominantArchetype, patternSummary.DominantArchetype, StringComparison.OrdinalIgnoreCase))
        {
            transitions.Add(new OperationalTransitionDto
            {
                TransitionId = "transition-pattern-archetype-shift",
                SourceState = priorPattern.DominantArchetype,
                TargetState = patternSummary.DominantArchetype,
                TransitionType = OperationalTransitionType.NarrativeShift,
                DominantArea = dominantArea,
                Direction = OperationalEvolutionDirection.Stable,
                Severity = OperationalEvolutionSeverity.Elevated,
                TransitionReason = "Dominant pattern archetype shifted between continuity snapshots",
                OperationalImpact = "Recurring operational narrative interpretation changed",
                OperatorInterpretation =
                    $"Pattern narrative shifted from {priorPattern.DominantArchetype.ToLowerInvariant()} to {patternSummary.DominantArchetype.ToLowerInvariant()}"
            });
        }

        if (priorExperience != null
            && priorExperience.DominantOperationalContext != experienceGraph.DominantOperationalContext)
        {
            transitions.Add(new OperationalTransitionDto
            {
                TransitionId = "transition-navigation-context-shift",
                SourceState = priorExperience.DominantOperationalContext.ToString(),
                TargetState = experienceGraph.DominantOperationalContext.ToString(),
                TransitionType = OperationalTransitionType.PhaseTransition,
                DominantArea = dominantArea,
                Direction = OperationalEvolutionDirection.Stable,
                Severity = OperationalEvolutionSeverity.Normal,
                TransitionReason = "Dominant operational navigation context shifted",
                OperationalImpact = "Recommended traversal paths and entry points may change",
                OperatorInterpretation =
                    "Navigation focus evolved; operator traversal continuity requires realignment"
            });
        }

        if (priorSimulation != null
            && !AreasMatch(priorSimulation.HighestLeverageArea, simulationSummary.HighestLeverageArea))
        {
            transitions.Add(new OperationalTransitionDto
            {
                TransitionId = "transition-simulation-leverage-shift",
                SourceState = priorSimulation.HighestLeverageArea,
                TargetState = simulationSummary.HighestLeverageArea,
                TransitionType = OperationalTransitionType.StabilizationShift,
                DominantArea = dominantArea,
                Direction = OperationalEvolutionDirection.Stable,
                Severity = OperationalEvolutionSeverity.Normal,
                TransitionReason = "Simulation leverage focus shifted between continuity snapshots",
                OperationalImpact = "Stabilization guidance emphasis may require operator review",
                OperatorInterpretation = "Simulation continuity indicates shifting stabilization leverage interpretation"
            });
        }

        if (priorDigest != null
            && !string.Equals(priorDigest.DominantRiskArea, digest.DominantRiskArea, StringComparison.OrdinalIgnoreCase))
        {
            transitions.Add(new OperationalTransitionDto
            {
                TransitionId = "transition-dominant-risk-shift",
                SourceState = priorDigest.DominantRiskArea,
                TargetState = digest.DominantRiskArea,
                TransitionType = OperationalTransitionType.NarrativeShift,
                DominantArea = dominantArea,
                Direction = OperationalEvolutionDirection.Stable,
                Severity = OperationalEvolutionSeverity.Elevated,
                TransitionReason = "Dominant operational risk area shifted in condensed digest continuity",
                OperationalImpact = "Operator and leadership focus area changed between snapshots",
                OperatorInterpretation =
                    $"Dominant risk evolved from {priorDigest.DominantRiskArea.ToLowerInvariant()} to {digest.DominantRiskArea.ToLowerInvariant()}"
            });
        }

        if (incidentSummary.EscalatingIncidentCount > 0
            && recovery.OverallDirection is OperationalRecoveryDirection.Degrading
                or OperationalRecoveryDirection.Diverging)
        {
            transitions.Add(new OperationalTransitionDto
            {
                TransitionId = "transition-incident-recovery-tension",
                SourceState = "Active incident escalation",
                TargetState = "Recovery degrading",
                TransitionType = OperationalTransitionType.EscalationProgression,
                DominantArea = dominantArea,
                Direction = OperationalEvolutionDirection.Degrading,
                Severity = OperationalEvolutionSeverity.High,
                TransitionReason = "Incident escalation concurrent with degrading recovery posture",
                OperationalImpact = "Operational evolution under dual escalation and recovery deceleration pressure",
                OperatorInterpretation = "Incident and recovery trajectories diverging in bounded continuity window"
            });
        }

        return transitions
            .OrderByDescending(t => t.Severity)
            .ThenBy(t => t.TransitionType)
            .ThenBy(t => t.TransitionId, StringComparer.Ordinal)
            .Take(MaxTransitions)
            .ToList();
    }

    private static IReadOnlyList<OperationalEvolutionPhaseDto> ComposePhases(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalDigestDto digest,
        OperationalIntegrityReportDto integrityReport,
        OperationalPatternSummaryDto patternSummary,
        IReadOnlyList<OperationalDigestSnapshot> digestSnapshots,
        IReadOnlyList<OperationalIntegritySnapshot> integritySnapshots,
        IReadOnlyList<OperationalSituationSnapshot> situationSnapshots)
    {
        var phases = new List<OperationalEvolutionPhaseDto>();

        phases.Add(new OperationalEvolutionPhaseDto
        {
            PhaseId = "phase-current",
            PhaseName = "Current operational phase",
            PhaseType = ResolveCurrentPhaseType(recovery, situationRoom, integrityReport),
            DominantOperationalCondition = digest.DominantOperationalStory,
            RecoveryAlignment = recovery.OverallDirection.ToString(),
            EscalationAlignment = situationRoom.EscalationSeverity.ToString(),
            StabilizationAlignment = situationRoom.StabilizationDirection.ToString(),
            DominantConstraint = digest.FocusSummary.DominantConstraint,
            OperatorSummary = digest.OperatorDigest
        });

        if (digestSnapshots.Count >= 2)
        {
            var earlier = digestSnapshots[^2];
            var latest = digestSnapshots[^1];
            phases.Add(new OperationalEvolutionPhaseDto
            {
                PhaseId = "phase-prior-digest",
                PhaseName = "Prior condensed digest phase",
                PhaseType = MapDigestStateToPhase(earlier.DigestState),
                DominantOperationalCondition = earlier.DominantOperationalStory,
                RecoveryAlignment = "Prior continuity snapshot",
                EscalationAlignment = "Prior continuity snapshot",
                StabilizationAlignment = "Prior continuity snapshot",
                DominantConstraint = earlier.RecommendedOperatorFocus,
                OperatorSummary = $"Prior digest phase: {earlier.DigestState.ToString().ToLowerInvariant()} with focus on {earlier.DominantRiskArea.ToLowerInvariant()}"
            });

            if (earlier.DigestState != latest.DigestState)
            {
                phases.Add(new OperationalEvolutionPhaseDto
                {
                    PhaseId = "phase-digest-transition",
                    PhaseName = "Digest phase transition",
                    PhaseType = OperationalPhaseType.Convergence,
                    DominantOperationalCondition = $"Transition from {earlier.DigestState} to {latest.DigestState}",
                    RecoveryAlignment = "Digest continuity comparison",
                    EscalationAlignment = "Digest continuity comparison",
                    StabilizationAlignment = "Digest continuity comparison",
                    DominantConstraint = latest.RecommendedOperatorFocus,
                    OperatorSummary = "Operational condensation phase transition detected in bounded window"
                });
            }
        }

        if (integritySnapshots.Count >= 2)
        {
            var earlier = integritySnapshots[^2];
            var latest = integritySnapshots[^1];
            if (latest.ContradictionCount < earlier.ContradictionCount)
            {
                phases.Add(new OperationalEvolutionPhaseDto
                {
                    PhaseId = "phase-integrity-convergence",
                    PhaseName = "Integrity convergence phase",
                    PhaseType = OperationalPhaseType.Convergence,
                    DominantOperationalCondition = "Cross-layer coherence improving",
                    RecoveryAlignment = "Integrity contradictions decreasing",
                    EscalationAlignment = "Escalation coherence stabilizing",
                    StabilizationAlignment = latest.AlignmentState,
                    DominantConstraint = dominantArea,
                    OperatorSummary = "Recovery convergence transition with strengthening operational coherence"
                });
            }
        }

        if (AreasMatch(dominantArea, AreaReplay))
        {
            phases.Add(new OperationalEvolutionPhaseDto
            {
                PhaseId = "phase-replay-dominant",
                PhaseName = "Replay-dominant evolution phase",
                PhaseType = recovery.OverallDirection is OperationalRecoveryDirection.Improving
                    or OperationalRecoveryDirection.Converging
                    ? OperationalPhaseType.Recovery
                    : OperationalPhaseType.Escalation,
                DominantOperationalCondition = "Replay instability continuity",
                RecoveryAlignment = recovery.OverallDirection.ToString(),
                EscalationAlignment = situationRoom.EscalationSeverity.ToString(),
                StabilizationAlignment = digest.StabilizationPriority,
                DominantConstraint = AreaReplay,
                OperatorSummary = "Replay-dominant operational evolution phase in bounded continuity window"
            });
        }

        if (patternSummary.RecurringPatternCount > 0)
        {
            phases.Add(new OperationalEvolutionPhaseDto
            {
                PhaseId = "phase-pattern-recurrence",
                PhaseName = "Recurring pattern phase",
                PhaseType = OperationalPhaseType.Stabilization,
                DominantOperationalCondition = patternSummary.DominantArchetype,
                RecoveryAlignment = patternSummary.RecoveryPatternStrength.ToString(),
                EscalationAlignment = patternSummary.EscalationPatternStrength.ToString(),
                StabilizationAlignment = "Pattern recurrence continuity",
                DominantConstraint = patternSummary.HighestRiskPattern,
                OperatorSummary = patternSummary.Summary
            });
        }

        return phases
            .OrderBy(p => p.PhaseId, StringComparer.Ordinal)
            .Take(MaxPhases)
            .ToList();
    }

    private static OperationalEvolutionContinuityDto ComposeEvolutionContinuity(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalDigestDto digest,
        OperationalIntegrityReportDto integrityReport,
        OperationalExperienceGraphDto experienceGraph,
        IReadOnlyList<OperationalDigestSnapshot> digestSnapshots,
        IReadOnlyList<OperationalIntegritySnapshot> integritySnapshots,
        IReadOnlyList<OperationalPatternSnapshot> patternSnapshots,
        IReadOnlyList<OperationalTransitionDto> transitions)
    {
        var priorDigest = digestSnapshots.LastOrDefault();
        var priorIntegrity = integritySnapshots.LastOrDefault();

        var narrativeTransition = priorDigest != null
            && !string.Equals(priorDigest.DominantOperationalStory, digest.DominantOperationalStory, StringComparison.OrdinalIgnoreCase)
            ? $"Narrative shifted from prior condensed story toward {digest.DominantOperationalStory.ToLowerInvariant()}"
            : OperationalContinuityPhrasing.RemainsConsistentAcrossBoundedWindow("Dominant narrative");

        var repeatingFlow = experienceGraph.ExperienceSummary.DominantOperationalFlow;
        if (string.IsNullOrWhiteSpace(repeatingFlow))
            repeatingFlow = experienceGraph.RecommendedTraversalPath;

        var stabilizationConsistency = transitions.Any(t => t.TransitionType == OperationalTransitionType.StabilizationShift)
            ? "Stabilization interpretation changing between continuity snapshots"
            : "Stabilization continuity consistent across bounded window";

        var escalationConsistency = situationRoom.EscalatingPropagationCount > 0
            ? $"{situationRoom.EscalatingPropagationCount} escalating propagation signal(s) sustained"
            : "Escalation continuity within normal bounds";

        var recoveryConsistency = recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            ? "Recovery continuity improving across snapshots"
            : "Recovery continuity requires upstream stabilization focus";

        if (priorIntegrity != null && integrityReport.ContradictionCount < priorIntegrity.ContradictionCount)
            recoveryConsistency += "; integrity contradictions decreasing";

        var interpretation =
            $"Operational evolution in {dominantArea.ToLowerInvariant()} context. " +
            $"{transitions.Count} transition(s) detected. " +
            $"Dominant flow: {repeatingFlow.ToLowerInvariant()}.";

        return new OperationalEvolutionContinuityDto
        {
            DominantNarrativeTransition = narrativeTransition,
            RepeatingOperationalFlow = repeatingFlow,
            StabilizationConsistency = stabilizationConsistency,
            EscalationConsistency = escalationConsistency,
            RecoveryConsistency = recoveryConsistency,
            OperatorInterpretation = interpretation
        };
    }

    private static OperationalEvolutionDirection ResolveDominantEvolutionDirection(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalIntegrityReportDto integrityReport,
        OperationalMomentumAnalysisDto momentum,
        IReadOnlyList<OperationalTransitionDto> transitions)
    {
        if (transitions.Any(t => t.Direction == OperationalEvolutionDirection.Escalating))
            return OperationalEvolutionDirection.Escalating;

        if (momentum.RecoveryMomentumState == OperationalMomentumState.Accelerating
            || recovery.OverallDirection is OperationalRecoveryDirection.Converging)
            return OperationalEvolutionDirection.Converging;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving)
            return OperationalEvolutionDirection.Improving;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Degrading
            || situationRoom.StabilizationDirection == OperationalSituationDirection.Degrading)
            return OperationalEvolutionDirection.Degrading;

        if (integrityReport.OverallIntegrityState == OperationalIntegrityState.Fragmented)
            return OperationalEvolutionDirection.Degrading;

        return OperationalEvolutionDirection.Stable;
    }

    private static string ResolveDominantOperationalShift(
        IReadOnlyList<OperationalTransitionDto> transitions,
        IReadOnlyList<OperationalDigestSnapshot> digestSnapshots,
        IReadOnlyList<OperationalIntegritySnapshot> integritySnapshots)
    {
        var top = transitions.FirstOrDefault();
        if (top != null)
            return top.OperatorInterpretation;

        if (digestSnapshots.Count >= 2)
        {
            var prior = digestSnapshots[^2];
            var latest = digestSnapshots[^1];
            if (prior.DigestState != latest.DigestState)
                return $"Digest phase shift from {prior.DigestState.ToString().ToLowerInvariant()} to {latest.DigestState.ToString().ToLowerInvariant()}";
        }

        if (integritySnapshots.Count >= 2)
        {
            var prior = integritySnapshots[^2];
            var latest = integritySnapshots[^1];
            if (prior.ConsistencyScore != latest.ConsistencyScore)
                return $"Integrity consistency moved from {prior.ConsistencyScore} to {latest.ConsistencyScore}";
        }

        return "Operational posture stable within bounded continuity window";
    }

    private static OperationalMomentumState ResolveRecoveryMomentumState(
        OperationalRecoveryPostureDto recovery,
        OperationalIntegritySnapshot? priorIntegrity,
        OperationalIntegrityReportDto integrityReport)
    {
        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
            or OperationalRecoveryDirection.Converging)
        {
            if (priorIntegrity != null && integrityReport.ConsistencyScore > priorIntegrity.ConsistencyScore)
                return OperationalMomentumState.Accelerating;

            return OperationalMomentumState.Stable;
        }

        if (recovery.OverallDirection is OperationalRecoveryDirection.Degrading
            or OperationalRecoveryDirection.Diverging)
            return OperationalMomentumState.Slowing;

        return OperationalMomentumState.Plateauing;
    }

    private static OperationalMomentumState ResolveEscalationMomentumState(
        OperationalSituationRoomDto situationRoom,
        OperationalSituationSnapshot? priorSituation)
    {
        if (priorSituation != null)
        {
            if (situationRoom.EscalatingPropagationCount > priorSituation.EscalatingPropagationCount)
                return OperationalMomentumState.Expanding;

            if (situationRoom.EscalatingPropagationCount < priorSituation.EscalatingPropagationCount)
                return OperationalMomentumState.Collapsing;

            if (situationRoom.StabilizationDirection == OperationalSituationDirection.Escalating
                && priorSituation.StabilizationDirection != OperationalSituationDirection.Escalating)
                return OperationalMomentumState.Expanding;
        }

        if (situationRoom.StabilizationDirection == OperationalSituationDirection.Escalating)
            return OperationalMomentumState.Expanding;

        if (situationRoom.StabilizationDirection is OperationalSituationDirection.Improving
            or OperationalSituationDirection.Stabilizing)
            return OperationalMomentumState.Collapsing;

        return OperationalMomentumState.Stable;
    }

    private static OperationalMomentumState ResolveStabilizationMomentumState(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalIntegritySnapshot? priorIntegrity,
        OperationalIntegrityReportDto integrityReport)
    {
        if (recovery.OverallDirection is OperationalRecoveryDirection.Converging
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Stabilizing
                or OperationalSituationDirection.Improving)
        {
            if (priorIntegrity != null && integrityReport.ContradictionCount < priorIntegrity.ContradictionCount)
                return OperationalMomentumState.Accelerating;

            return OperationalMomentumState.Stable;
        }

        if (situationRoom.StabilizationDirection == OperationalSituationDirection.Stable)
            return OperationalMomentumState.Plateauing;

        if (situationRoom.StabilizationDirection is OperationalSituationDirection.Degrading
            or OperationalSituationDirection.Escalating)
            return OperationalMomentumState.Slowing;

        return OperationalMomentumState.Stable;
    }

    private static OperationalMomentumState ResolveOverallMomentumState(OperationalMomentumAnalysisDto momentum)
    {
        if (momentum.RecoveryMomentumState == OperationalMomentumState.Accelerating)
            return OperationalMomentumState.Accelerating;

        if (momentum.EscalationMomentumState == OperationalMomentumState.Expanding)
            return OperationalMomentumState.Expanding;

        if (momentum.EscalationMomentumState == OperationalMomentumState.Collapsing)
            return OperationalMomentumState.Collapsing;

        return OperationalMomentumState.Stable;
    }

    private static OperationalPhaseType ResolveCurrentPhaseType(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalIntegrityReportDto integrityReport)
    {
        if (integrityReport.OverallIntegrityState == OperationalIntegrityState.Contradictory)
            return OperationalPhaseType.Fragmentation;

        if (situationRoom.StabilizationDirection == OperationalSituationDirection.Escalating)
            return OperationalPhaseType.Escalation;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Converging)
            return OperationalPhaseType.Convergence;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving)
            return OperationalPhaseType.Recovery;

        if (situationRoom.StabilizationDirection is OperationalSituationDirection.Stabilizing)
            return OperationalPhaseType.Stabilization;

        return OperationalPhaseType.Containment;
    }

    private static OperationalPhaseType MapDigestStateToPhase(OperationalDigestState state)
    {
        return state switch
        {
            OperationalDigestState.Recovering => OperationalPhaseType.Recovery,
            OperationalDigestState.Escalating => OperationalPhaseType.Escalation,
            OperationalDigestState.Fragmented => OperationalPhaseType.Fragmentation,
            OperationalDigestState.AttentionRequired => OperationalPhaseType.Containment,
            _ => OperationalPhaseType.Stabilization
        };
    }

    private static OperationalEvolutionDirection MapDigestStateDirection(
        OperationalDigestState from,
        OperationalDigestState to)
    {
        if (to == OperationalDigestState.Recovering)
            return OperationalEvolutionDirection.Improving;

        if (to == OperationalDigestState.Escalating)
            return OperationalEvolutionDirection.Escalating;

        if (to == OperationalDigestState.Fragmented)
            return OperationalEvolutionDirection.Degrading;

        return OperationalEvolutionDirection.Stable;
    }

    private static OperationalEvolutionDirection MapIntegrityDirection(
        OperationalIntegritySnapshot prior,
        OperationalIntegrityReportDto current)
    {
        if (current.ContradictionCount < prior.ContradictionCount)
            return OperationalEvolutionDirection.Converging;

        if (current.ContradictionCount > prior.ContradictionCount)
            return OperationalEvolutionDirection.Degrading;

        return OperationalEvolutionDirection.Stable;
    }

    private static OperationalEvolutionDirection MapSituationDirection(
        OperationalSituationDirection from,
        OperationalSituationDirection to)
    {
        if (to is OperationalSituationDirection.Improving or OperationalSituationDirection.Stabilizing)
            return OperationalEvolutionDirection.Improving;

        if (to is OperationalSituationDirection.Escalating or OperationalSituationDirection.Degrading)
            return OperationalEvolutionDirection.Escalating;

        return OperationalEvolutionDirection.Stable;
    }

    private static string DescribeMomentum(OperationalMomentumState state, string domain)
    {
        return state switch
        {
            OperationalMomentumState.Accelerating => $"{domain} accelerating",
            OperationalMomentumState.Slowing => $"{domain} slowing",
            OperationalMomentumState.Plateauing => $"{domain} plateauing",
            OperationalMomentumState.Expanding => $"{domain} expanding",
            OperationalMomentumState.Collapsing => $"{domain} collapsing",
            _ => $"{domain} stable"
        };
    }

    private static string ComposeOperatorSummary(
        OperationalEvolutionDirection direction,
        int transitionCount,
        string dominantShift,
        OperationalMomentumAnalysisDto momentum,
        IReadOnlyList<OperationalEvolutionSnapshot> priorEvolutionSnapshots)
    {
        var continuity = string.Empty;
        if (priorEvolutionSnapshots.Count > 0)
        {
            var prior = priorEvolutionSnapshots[^1];
            if (prior.DominantEvolutionDirection != direction)
            {
                continuity =
                    $" Evolution direction moved from {prior.DominantEvolutionDirection.ToString().ToLowerInvariant()} " +
                    $"to {direction.ToString().ToLowerInvariant()}.";
            }
        }

        return
            $"Operational evolution is {direction.ToString().ToLowerInvariant()} with {transitionCount} active transition(s). " +
            $"Recovery: {momentum.RecoveryMomentum.ToLowerInvariant()}; " +
            $"Escalation: {momentum.EscalationMomentum.ToLowerInvariant()}; " +
            $"Stabilization: {momentum.StabilizationMomentum.ToLowerInvariant()}. " +
            $"Dominant shift: {dominantShift.ToLowerInvariant()}.{continuity}";
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
