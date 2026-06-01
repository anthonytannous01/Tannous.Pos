using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Application.OperationalAttention;
using Tannous.Pos.Application.OperationalConvergence;
using Tannous.Pos.Application.OperationalDigest;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalResilience;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTopology;

namespace Tannous.Pos.Application.OperationalStrategy;

/// <summary>Deterministic operational strategic posture from bounded cognition continuity.</summary>
public static class OperationalStrategyAggregation
{
    public const int MaxPostures = 8;
    public const int MaxCoordination = 8;
    public const int MaxAlignments = 8;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;

    public const string AreaReplay = "Replay";
    public const string AreaRuntime = "Runtime";
    public const string AreaReconciliation = "Reconciliation";
    public const string AreaOperational = "Operational Stability";

    public static OperationalStrategyReportDto ComposeStrategyReport(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        OperationalAttentionReportDto attentionReport,
        OperationalDigestDto digest,
        OperationalPlaybooksDto playbooks,
        OperationalIntegrityReportDto integrityReport,
        IReadOnlyList<OperationalFragilityDto> fragilities,
        IReadOnlyList<OperationalStrategySnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var dominantArea = ResolveDominantArea(attentionReport, topology, resilienceReport);
        var dominantPosture = ResolveDominantPosture(
            recovery,
            situationRoom,
            evolutionTimeline,
            convergenceReport,
            resilienceReport,
            attentionReport,
            fragilities,
            integrityReport);

        var postures = ComposeStrategicPostures(
            dominantArea,
            recovery,
            situationRoom,
            evolutionTimeline,
            topology,
            convergenceReport,
            resilienceReport,
            attentionReport,
            fragilities);

        var coordination = ComposeOperationalCoordination(
            dominantArea,
            recovery,
            situationRoom,
            evolutionTimeline,
            topology,
            convergenceReport,
            resilienceReport,
            attentionReport,
            playbooks,
            integrityReport,
            fragilities);

        var alignments = ComposeStrategicAlignments(
            dominantArea,
            recovery,
            situationRoom,
            evolutionTimeline,
            convergenceReport,
            resilienceReport,
            attentionReport,
            digest,
            integrityReport,
            fragilities);

        var continuity = ComposeStrategyContinuity(
            dominantArea,
            dominantPosture,
            recovery,
            situationRoom,
            evolutionTimeline,
            convergenceReport,
            attentionReport,
            coordination,
            priorSnapshots);

        var alignmentStrength = ResolveAlignmentStrength(
            convergenceReport,
            resilienceReport,
            attentionReport,
            integrityReport,
            alignments);

        var stabilizationState = DescribeStabilizationState(
            recovery,
            situationRoom,
            evolutionTimeline,
            convergenceReport,
            resilienceReport,
            attentionReport);

        var escalationState = DescribeEscalationCoordinationState(
            situationRoom,
            evolutionTimeline,
            topology,
            convergenceReport,
            attentionReport,
            fragilities);

        var recoveryState = DescribeRecoveryCoordinationState(
            recovery,
            convergenceReport,
            evolutionTimeline,
            resilienceReport,
            attentionReport,
            fragilities);

        var strategicFocus = attentionReport.HighestUrgencyArea;
        if (string.IsNullOrWhiteSpace(strategicFocus))
            strategicFocus = dominantArea;

        var operatorSummary = ComposeOperatorSummary(
            dominantPosture,
            stabilizationState,
            escalationState,
            recoveryState,
            alignmentStrength,
            strategicFocus,
            priorSnapshots);

        return new OperationalStrategyReportDto
        {
            GeneratedAtUtc = generatedAtUtc,
            DominantOperationalPosture = dominantPosture,
            StrategicStabilizationState = stabilizationState,
            EscalationCoordinationState = escalationState,
            RecoveryCoordinationState = recoveryState,
            OperationalAlignmentStrength = alignmentStrength,
            DominantStrategicFocus = strategicFocus,
            StrategicPostures = postures,
            OperationalCoordination = coordination,
            StrategicAlignments = alignments,
            StrategyContinuity = continuity,
            OperatorSummary = operatorSummary
        };
    }

    public static OperationalStrategySummaryDto ComposeStrategySummary(
        OperationalStrategyReportDto report,
        OperationalSituationRoomDto situationRoom,
        IReadOnlyList<OperationalCoordinationDto> coordination,
        IReadOnlyList<OperationalStrategicAlignmentDto> alignments,
        DateTime generatedAtUtc)
    {
        var strongestAlignment = alignments
            .Where(a => a.AlignmentStrength == OperationalAlignmentState.Aligned)
            .Select(a => a.OperatorInterpretation)
            .FirstOrDefault()
            ?? report.StrategicStabilizationState;

        var weakestCoordination = coordination
            .OrderBy(c => c.CoordinationStrength)
            .FirstOrDefault()?.OperatorSummary
            ?? report.EscalationCoordinationState;

        var dominantPressure = report.EscalationCoordinationState;
        if (report.RecoveryCoordinationState.Contains("reactive", StringComparison.OrdinalIgnoreCase))
            dominantPressure = report.RecoveryCoordinationState;

        var strategyState = ResolveStrategyState(report, coordination, alignments);

        var summary =
            $"Operational strategy is {strategyState.ToString().ToLowerInvariant()} with dominant posture " +
            $"{report.DominantOperationalPosture.ToString().ToLowerInvariant()}. Strategic focus: {report.DominantStrategicFocus.ToLowerInvariant()}.";

        return new OperationalStrategySummaryDto
        {
            GeneratedAtUtc = generatedAtUtc,
            DominantStrategicPosture = report.DominantOperationalPosture,
            StrongestOperationalAlignment = strongestAlignment,
            WeakestCoordinationArea = weakestCoordination,
            DominantStrategicPressure = dominantPressure,
            OperationalStrategyState = strategyState,
            OperatorAttentionLevel = situationRoom.AttentionLevel.ToString(),
            Summary = summary
        };
    }

    public static IReadOnlyList<OperationalCoordinationDto> ComposeOperationalCoordination(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        OperationalAttentionReportDto attentionReport,
        OperationalPlaybooksDto playbooks,
        OperationalIntegrityReportDto integrityReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        var coordination = new List<OperationalCoordinationDto>();

        coordination.Add(new OperationalCoordinationDto
        {
            CoordinationId = "coordination-strategic-posture",
            DominantOperationalStrategy = attentionReport.DominantOperationalPriority.ToString(),
            CoordinationStrength = MapAttentionPressureToCoordination(attentionReport.AttentionPressureLevel),
            StabilizationCoordination = attentionReport.StabilizationFocusArea,
            EscalationCoordination = attentionReport.EscalationFocusArea,
            RecoveryCoordination = attentionReport.InvestigationPriorityArea,
            OperatorSummary =
                $"Strategic coordination anchored on {attentionReport.DominantOperationalPriority.ToString().ToLowerInvariant()} priority"
        });

        if (convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Strong
                or OperationalConvergenceStrength.Moderate
            && evolutionTimeline.EscalationMomentum.Contains("collapsing", StringComparison.OrdinalIgnoreCase))
        {
            coordination.Add(new OperationalCoordinationDto
            {
                CoordinationId = "coordination-stabilization-aligned",
                DominantOperationalStrategy = OperationalStrategicPostureType.StabilizationOriented.ToString(),
                CoordinationStrength = OperationalCoordinationStrength.Strong,
                StabilizationCoordination = "Stabilization coordination aligned across convergence and escalation collapse",
                EscalationCoordination = "Escalation coordination weakening as propagation collapses",
                RecoveryCoordination = recovery.OverallDirection.ToString(),
                OperatorSummary = "Stabilization-oriented strategic coordination with strong signal alignment"
            });
        }

        if (resilienceReport.SurvivabilityState is OperationalSurvivabilityState.Strained
                or OperationalSurvivabilityState.Fragile
                or OperationalSurvivabilityState.Critical
            && attentionReport.StabilizationFocusArea.Contains("runtime", StringComparison.OrdinalIgnoreCase))
        {
            coordination.Add(new OperationalCoordinationDto
            {
                CoordinationId = "coordination-containment-focused",
                DominantOperationalStrategy = OperationalStrategicPostureType.ContainmentOriented.ToString(),
                CoordinationStrength = OperationalCoordinationStrength.Moderate,
                StabilizationCoordination = "Containment coordination prioritized over recovery expansion",
                EscalationCoordination = resilienceReport.EscalationFragility,
                RecoveryCoordination = resilienceReport.RecoverySustainability,
                OperatorSummary = "Containment-oriented strategic coordination under survivability pressure"
            });
        }

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            && convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Weak
                or OperationalConvergenceStrength.Fragmented)
        {
            coordination.Add(new OperationalCoordinationDto
            {
                CoordinationId = "coordination-reactive-recovery",
                DominantOperationalStrategy = OperationalStrategicPostureType.ReactiveRecovery.ToString(),
                CoordinationStrength = OperationalCoordinationStrength.Weak,
                StabilizationCoordination = "Stabilization coordination secondary to recovery validation",
                EscalationCoordination = convergenceReport.EscalationConfidence,
                RecoveryCoordination = "Recovery coordination reactive — convergence does not reinforce recovery",
                OperatorSummary = "Reactive recovery posture — coordination inconsistent across bounded signals"
            });
        }

        if (integrityReport.ContradictionCount >= 2)
        {
            coordination.Add(new OperationalCoordinationDto
            {
                CoordinationId = "coordination-integrity-fragmentation",
                DominantOperationalStrategy = "Strategic coherence strained",
                CoordinationStrength = OperationalCoordinationStrength.Fragmented,
                StabilizationCoordination = "Stabilization coordination fragmented by integrity contradictions",
                EscalationCoordination = integrityReport.AlignmentState,
                RecoveryCoordination = recovery.OverallDirection.ToString(),
                OperatorSummary =
                    $"{integrityReport.ContradictionCount} integrity contradiction(s) fragment strategic coordination"
            });
        }

        if (topology.TopologyState == OperationalTopologyState.EscalationDominant)
        {
            coordination.Add(new OperationalCoordinationDto
            {
                CoordinationId = "coordination-topology-pressure",
                DominantOperationalStrategy = "Escalation coordination dominant",
                CoordinationStrength = OperationalCoordinationStrength.Weak,
                StabilizationCoordination = topology.StabilizationDependencyStrength,
                EscalationCoordination = topology.EscalationPropagationStrength,
                RecoveryCoordination = recovery.OverallDirection.ToString(),
                OperatorSummary =
                    $"Topology escalation dominance in {topology.HighestInfluenceArea.ToLowerInvariant()} strains strategic coordination"
            });
        }

        if (playbooks.Playbooks.Count > 0)
        {
            coordination.Add(new OperationalCoordinationDto
            {
                CoordinationId = "coordination-playbook-sequencing",
                DominantOperationalStrategy = "Playbook-guided coordination",
                CoordinationStrength = OperationalCoordinationStrength.Moderate,
                StabilizationCoordination = "Playbook sequencing governs stabilization coordination",
                EscalationCoordination = "Escalation coordination modulated by playbook guidance",
                RecoveryCoordination = recovery.OverallDirection.ToString(),
                OperatorSummary = $"{playbooks.Playbooks.Count} playbook(s) anchor strategic coordination sequencing"
            });
        }

        if (fragilities.Count >= 3)
        {
            coordination.Add(new OperationalCoordinationDto
            {
                CoordinationId = "coordination-fragility-concentration",
                DominantOperationalStrategy = OperationalStrategicPostureType.Deteriorating.ToString(),
                CoordinationStrength = OperationalCoordinationStrength.Fragmented,
                StabilizationCoordination = "Stabilization coordination overwhelmed by fragility concentration",
                EscalationCoordination = $"{fragilities.Count} fragility signal(s) strain escalation coordination",
                RecoveryCoordination = resilienceReport.RecoverySustainability,
                OperatorSummary = "Fragility concentration degrades strategic coordination coherence"
            });
        }

        if (coordination.Count == 1)
        {
            coordination.Add(new OperationalCoordinationDto
            {
                CoordinationId = "coordination-balanced",
                DominantOperationalStrategy = OperationalStrategicPostureType.Balanced.ToString(),
                CoordinationStrength = OperationalCoordinationStrength.Moderate,
                StabilizationCoordination = situationRoom.StabilizationDirection.ToString(),
                EscalationCoordination = situationRoom.EscalationSeverity.ToString(),
                RecoveryCoordination = recovery.OverallDirection.ToString(),
                OperatorSummary = "Strategic coordination balanced across bounded operational continuity"
            });
        }

        return coordination
            .OrderByDescending(c => c.CoordinationStrength)
            .ThenBy(c => c.CoordinationId, StringComparer.Ordinal)
            .Take(MaxCoordination)
            .ToList();
    }

    public static OperationalStrategySnapshot CreateSnapshot(
        OperationalStrategyReportDto report,
        IReadOnlyList<OperationalCoordinationDto> coordination)
    {
        return new OperationalStrategySnapshot
        {
            GeneratedAtUtc = report.GeneratedAtUtc,
            DominantOperationalPosture = report.DominantOperationalPosture,
            OperationalAlignmentStrength = report.OperationalAlignmentStrength,
            DominantStrategicFocus = report.DominantStrategicFocus,
            CoordinationCount = coordination.Count
        };
    }

    private static IReadOnlyList<OperationalStrategicPostureDto> ComposeStrategicPostures(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        OperationalAttentionReportDto attentionReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        var postures = new List<OperationalStrategicPostureDto>
        {
            BuildStrategicPosture(
                dominantArea,
                recovery,
                situationRoom,
                evolutionTimeline,
                convergenceReport,
                resilienceReport,
                attentionReport,
                fragilities.Count(f => AreasMatch(f.OperationalArea, dominantArea)))
        };

        if (!AreasMatch(dominantArea, AreaRuntime)
            && (resilienceReport.SurvivabilityState is OperationalSurvivabilityState.Strained
                or OperationalSurvivabilityState.Fragile
                or OperationalSurvivabilityState.Critical))
        {
            postures.Add(BuildStrategicPosture(
                AreaRuntime,
                recovery,
                situationRoom,
                evolutionTimeline,
                convergenceReport,
                resilienceReport,
                attentionReport,
                fragilities.Count(f => AreasMatch(f.OperationalArea, AreaRuntime)),
                OperationalStrategicDirection.Containing));
        }

        if (!AreasMatch(dominantArea, AreaReplay)
            && attentionReport.EscalationFocusArea.Contains("replay", StringComparison.OrdinalIgnoreCase))
        {
            postures.Add(BuildStrategicPosture(
                AreaReplay,
                recovery,
                situationRoom,
                evolutionTimeline,
                convergenceReport,
                resilienceReport,
                attentionReport,
                fragilities.Count(f => AreasMatch(f.OperationalArea, AreaReplay)),
                OperationalStrategicDirection.Deteriorating));
        }

        if (topology.TopologyState == OperationalTopologyState.EscalationDominant)
        {
            postures.Add(BuildStrategicPosture(
                NormalizeArea(topology.HighestInfluenceArea),
                recovery,
                situationRoom,
                evolutionTimeline,
                convergenceReport,
                resilienceReport,
                attentionReport,
                fragilities.Count(f => AreasMatch(f.OperationalArea, topology.HighestInfluenceArea)),
                OperationalStrategicDirection.Containing));
        }

        return postures
            .OrderByDescending(p => p.StrategicInfluenceStrength)
            .ThenBy(p => p.OperationalArea, StringComparer.Ordinal)
            .Take(MaxPostures)
            .ToList();
    }

    private static OperationalStrategicPostureDto BuildStrategicPosture(
        string area,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        OperationalAttentionReportDto attentionReport,
        int localFragilityCount,
        OperationalStrategicDirection? forcedDirection = null)
    {
        var direction = forcedDirection ?? ResolveStrategicDirection(
            recovery,
            situationRoom,
            evolutionTimeline,
            convergenceReport,
            resilienceReport);

        var influence = localFragilityCount switch
        {
            0 when convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Strong
                => OperationalCoordinationStrength.Strong,
            0 => OperationalCoordinationStrength.Moderate,
            1 => OperationalCoordinationStrength.Weak,
            _ => OperationalCoordinationStrength.Fragmented
        };

        return new OperationalStrategicPostureDto
        {
            OperationalArea = area,
            StrategicOrientation = direction,
            StabilizationAlignment = situationRoom.StabilizationDirection.ToString(),
            EscalationAlignment = evolutionTimeline.EscalationMomentum,
            RecoveryAlignment = recovery.OverallDirection.ToString(),
            StrategicInfluenceStrength = influence,
            OperatorInterpretation =
                $"Strategic posture in {area.ToLowerInvariant()} is {direction.ToString().ToLowerInvariant()} " +
                $"with {localFragilityCount} local fragility signal(s) and attention on {attentionReport.DominantOperationalPriority.ToString().ToLowerInvariant()}"
        };
    }

    private static IReadOnlyList<OperationalStrategicAlignmentDto> ComposeStrategicAlignments(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        OperationalAttentionReportDto attentionReport,
        OperationalDigestDto digest,
        OperationalIntegrityReportDto integrityReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        var alignments = new List<OperationalStrategicAlignmentDto>();

        var alignmentState = ResolveAreaAlignment(
            recovery,
            convergenceReport,
            resilienceReport,
            integrityReport,
            fragilities.Count);

        alignments.Add(new OperationalStrategicAlignmentDto
        {
            OperationalArea = dominantArea,
            AlignmentStrength = alignmentState,
            ReinforcingOperationalSignals = digest.OperationalHighlights.Count > 0
                ? $"{digest.OperationalHighlights.Count} digest highlight(s) reinforce strategic alignment"
                : "No digest highlights in bounded window",
            ContradictingOperationalSignals = integrityReport.ContradictionCount > 0
                ? $"{integrityReport.ContradictionCount} integrity contradiction(s)"
                : "No integrity contradictions in bounded window",
            StrategicConsistency = convergenceReport.ConvergenceStrength.ToString(),
            OperatorInterpretation =
                $"Strategic alignment in {dominantArea.ToLowerInvariant()} is {alignmentState.ToString().ToLowerInvariant()}"
        });

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            && convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Weak
                or OperationalConvergenceStrength.Fragmented)
        {
            alignments.Add(new OperationalStrategicAlignmentDto
            {
                OperationalArea = NormalizeArea(convergenceReport.HighestAmbiguityArea),
                AlignmentStrength = OperationalAlignmentState.Misaligned,
                ReinforcingOperationalSignals = "Recovery momentum improving",
                ContradictingOperationalSignals = "Convergence weak or fragmented",
                StrategicConsistency = "Recovery and convergence strategically misaligned",
                OperatorInterpretation = "Reactive recovery alignment — recovery improving without convergence reinforcement"
            });
        }

        if (attentionReport.AttentionPressureLevel >= OperationalUrgencyLevel.High
            && resilienceReport.SurvivabilityState is OperationalSurvivabilityState.Stable
                or OperationalSurvivabilityState.Strong)
        {
            alignments.Add(new OperationalStrategicAlignmentDto
            {
                OperationalArea = attentionReport.HighestUrgencyArea,
                AlignmentStrength = OperationalAlignmentState.PartiallyAligned,
                ReinforcingOperationalSignals = "Survivability stable or strong",
                ContradictingOperationalSignals = "Attention pressure elevated",
                StrategicConsistency = "Attention and survivability partially aligned",
                OperatorInterpretation = "Attention urgency exceeds survivability posture — partial strategic alignment"
            });
        }

        if (evolutionTimeline.EscalationMomentum.Contains("collapsing", StringComparison.OrdinalIgnoreCase)
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Stabilizing
                or OperationalSituationDirection.Improving)
        {
            alignments.Add(new OperationalStrategicAlignmentDto
            {
                OperationalArea = dominantArea,
                AlignmentStrength = OperationalAlignmentState.Aligned,
                ReinforcingOperationalSignals = "Escalation collapsing with stabilizing posture",
                ContradictingOperationalSignals = "None in bounded window",
                StrategicConsistency = "Stabilization-oriented strategic alignment",
                OperatorInterpretation = "Strong stabilization alignment across escalation collapse and posture improvement"
            });
        }

        return alignments
            .OrderByDescending(a => a.AlignmentStrength)
            .ThenBy(a => a.OperationalArea, StringComparer.Ordinal)
            .Take(MaxAlignments)
            .ToList();
    }

    private static OperationalStrategyContinuityDto ComposeStrategyContinuity(
        string dominantArea,
        OperationalStrategicPostureType dominantPosture,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalConvergenceReportDto convergenceReport,
        OperationalAttentionReportDto attentionReport,
        IReadOnlyList<OperationalCoordinationDto> coordination,
        IReadOnlyList<OperationalStrategySnapshot> priorSnapshots)
    {
        var prior = priorSnapshots.LastOrDefault();
        var strategicShift = prior != null && prior.DominantOperationalPosture != dominantPosture
            ? OperationalContinuityPhrasing.StateShift(
                "Strategic posture",
                prior.DominantOperationalPosture.ToString().ToLowerInvariant(),
                dominantPosture.ToString().ToLowerInvariant())
            : OperationalContinuityPhrasing.ConsistentAcrossBoundedWindow("Strategic posture");

        var coordinationConsistency = coordination.All(c => c.CoordinationStrength >= OperationalCoordinationStrength.Moderate)
            ? "Coordination consistency strong across bounded window"
            : coordination.Any(c => c.CoordinationStrength == OperationalCoordinationStrength.Fragmented)
                ? "Coordination consistency fragmented in bounded window"
                : "Coordination consistency within moderate bounds";

        var recoveryAlignment = OperationalContinuityPhrasing.RecoveryAlignment(
            recovery,
            "Recovery strategy alignment improving",
            "Recovery strategy alignment requires upstream stabilization");

        var escalationAlignment = OperationalContinuityPhrasing.EscalationMomentumAlignment(
            evolutionTimeline.EscalationMomentum,
            "Escalation strategy alignment strengthening",
            "Escalation strategy alignment weakening",
            "Escalation strategy alignment stable");

        var stabilizationAlignment = OperationalContinuityPhrasing.StabilizationSituationAlignment(
            situationRoom,
            "Stabilization strategy alignment strengthening",
            "Stabilization strategy alignment requires reinforcement",
            "Stabilization strategy alignment within moderate bounds");

        var (oscillationDetected, postureOscillation) = DetectPostureOscillation(priorSnapshots, dominantPosture);

        return new OperationalStrategyContinuityDto
        {
            DominantStrategicShift = strategicShift,
            CoordinationConsistency = coordinationConsistency,
            RecoveryStrategyAlignment = recoveryAlignment,
            EscalationStrategyAlignment = escalationAlignment,
            StabilizationStrategyAlignment = stabilizationAlignment,
            PostureOscillation = postureOscillation,
            OscillationDetected = oscillationDetected,
            OperatorInterpretation =
                $"Strategy continuity in {dominantArea.ToLowerInvariant()} with {coordination.Count} coordination signal(s), " +
                $"attention {attentionReport.DominantOperationalPriority.ToString().ToLowerInvariant()}, " +
                $"convergence {convergenceReport.ConvergenceStrength.ToString().ToLowerInvariant()}"
        };
    }

    private static (bool Detected, string Phrasing) DetectPostureOscillation(
        IReadOnlyList<OperationalStrategySnapshot> priorSnapshots,
        OperationalStrategicPostureType currentPosture)
    {
        // Build full ordered sequence (oldest to newest): prior snapshots + current posture.
        var fullSequence = priorSnapshots
            .Select(s => s.DominantOperationalPosture)
            .Append(currentPosture)
            .ToList();

        // Need at least 4 observations to detect meaningful oscillation.
        if (fullSequence.Count < 4)
            return (false, "Insufficient bounded window for oscillation assessment");

        var distinctCount = fullSequence.Distinct().Count();

        // Count ABA reversals: position[i] == position[i-2] AND position[i] != position[i-1].
        var reversalCount = 0;
        for (var i = 2; i < fullSequence.Count; i++)
        {
            if (fullSequence[i] == fullSequence[i - 2] && fullSequence[i] != fullSequence[i - 1])
                reversalCount++;
        }

        // Oscillation: 2-3 distinct postures bouncing back and forth (>= 2 reversals).
        var detected = distinctCount is >= 2 and <= 3 && reversalCount >= 2;

        if (!detected)
            return (false, "No posture oscillation detected in bounded window");

        var distinctLabels = fullSequence
            .Distinct()
            .Select(p => p.ToString().ToLowerInvariant())
            .OrderBy(s => s, StringComparer.Ordinal);

        return (true,
            $"Strategic posture oscillating between {string.Join(" and ", distinctLabels)} " +
            $"across {fullSequence.Count} bounded observations without resolution");
    }

    private static OperationalStrategicPostureType ResolveDominantPosture(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        OperationalAttentionReportDto attentionReport,
        IReadOnlyList<OperationalFragilityDto> fragilities,
        OperationalIntegrityReportDto integrityReport)
    {
        if (fragilities.Count >= 3
            || situationRoom.StabilizationDirection is OperationalSituationDirection.Degrading
            || recovery.OverallDirection is OperationalRecoveryDirection.Degrading
                or OperationalRecoveryDirection.Diverging)
            return OperationalStrategicPostureType.Deteriorating;

        if (resilienceReport.SurvivabilityState is OperationalSurvivabilityState.Strained
                or OperationalSurvivabilityState.Fragile
                or OperationalSurvivabilityState.Critical
            || attentionReport.DominantOperationalPriority == OperationalPriorityType.ContainmentCritical)
            return OperationalStrategicPostureType.ContainmentOriented;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            && convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Weak
                or OperationalConvergenceStrength.Fragmented)
            return OperationalStrategicPostureType.ReactiveRecovery;

        if (evolutionTimeline.EscalationMomentum.Contains("collapsing", StringComparison.OrdinalIgnoreCase)
            && convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Strong
                or OperationalConvergenceStrength.Moderate
            && resilienceReport.SurvivabilityState is OperationalSurvivabilityState.Strong
                or OperationalSurvivabilityState.Stable)
            return OperationalStrategicPostureType.StabilizationOriented;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Converging
            && convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Strong
            && integrityReport.ContradictionCount == 0)
            return OperationalStrategicPostureType.RecoveryOriented;

        if (attentionReport.DominantOperationalPriority == OperationalPriorityType.EscalationDominant)
            return OperationalStrategicPostureType.ContainmentOriented;

        return OperationalStrategicPostureType.Balanced;
    }

    private static OperationalStrategicDirection ResolveStrategicDirection(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport)
    {
        if (evolutionTimeline.EscalationMomentum.Contains("expanding", StringComparison.OrdinalIgnoreCase)
            || situationRoom.StabilizationDirection is OperationalSituationDirection.Escalating
                or OperationalSituationDirection.Degrading)
            return OperationalStrategicDirection.Deteriorating;

        if (resilienceReport.SurvivabilityState is OperationalSurvivabilityState.Strained
                or OperationalSurvivabilityState.Fragile
                or OperationalSurvivabilityState.Critical)
            return OperationalStrategicDirection.Containing;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            && convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Strong)
            return OperationalStrategicDirection.Recovering;

        if (situationRoom.StabilizationDirection is OperationalSituationDirection.Stabilizing
                or OperationalSituationDirection.Improving
            || evolutionTimeline.StabilizationMomentum.Contains("accelerating", StringComparison.OrdinalIgnoreCase))
            return OperationalStrategicDirection.Stabilizing;

        return OperationalStrategicDirection.Balanced;
    }

    private static OperationalAlignmentState ResolveAreaAlignment(
        OperationalRecoveryPostureDto recovery,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        OperationalIntegrityReportDto integrityReport,
        int fragilityCount)
    {
        if (integrityReport.ContradictionCount >= 2 || fragilityCount >= 3)
            return OperationalAlignmentState.Contradictory;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            && convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Weak
                or OperationalConvergenceStrength.Fragmented)
            return OperationalAlignmentState.Misaligned;

        if (convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Strong
            && resilienceReport.SurvivabilityState is OperationalSurvivabilityState.Strong
                or OperationalSurvivabilityState.Stable)
            return OperationalAlignmentState.Aligned;

        return OperationalAlignmentState.PartiallyAligned;
    }

    private static OperationalCoordinationStrength ResolveAlignmentStrength(
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        OperationalAttentionReportDto attentionReport,
        OperationalIntegrityReportDto integrityReport,
        IReadOnlyList<OperationalStrategicAlignmentDto> alignments)
    {
        if (integrityReport.ContradictionCount >= 2
            || alignments.Any(a => a.AlignmentStrength == OperationalAlignmentState.Contradictory))
            return OperationalCoordinationStrength.Fragmented;

        if (convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Strong
            && resilienceReport.SurvivabilityState is OperationalSurvivabilityState.Strong
                or OperationalSurvivabilityState.Stable
            && attentionReport.AttentionPressureLevel <= OperationalUrgencyLevel.Elevated)
            return OperationalCoordinationStrength.Strong;

        if (convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Weak
                or OperationalConvergenceStrength.Fragmented
            || attentionReport.AttentionPressureLevel >= OperationalUrgencyLevel.High)
            return OperationalCoordinationStrength.Weak;

        return OperationalCoordinationStrength.Moderate;
    }

    private static OperationalStrategyState ResolveStrategyState(
        OperationalStrategyReportDto report,
        IReadOnlyList<OperationalCoordinationDto> coordination,
        IReadOnlyList<OperationalStrategicAlignmentDto> alignments)
    {
        if (report.OperationalAlignmentStrength == OperationalCoordinationStrength.Fragmented
            || coordination.Count(c => c.CoordinationStrength == OperationalCoordinationStrength.Fragmented) >= 2)
            return OperationalStrategyState.Fragmented;

        if (report.OperationalAlignmentStrength == OperationalCoordinationStrength.Strong
            && alignments.Any(a => a.AlignmentStrength == OperationalAlignmentState.Aligned))
            return OperationalStrategyState.Coherent;

        if (coordination.Count >= 4)
            return OperationalStrategyState.Overextended;

        if (report.OperationalAlignmentStrength == OperationalCoordinationStrength.Weak)
            return OperationalStrategyState.Strained;

        return OperationalStrategyState.Coordinated;
    }

    private static string DescribeStabilizationState(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        OperationalAttentionReportDto attentionReport)
    {
        if (evolutionTimeline.EscalationMomentum.Contains("collapsing", StringComparison.OrdinalIgnoreCase)
            && convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Strong
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Stabilizing
                or OperationalSituationDirection.Improving)
            return "Proactive stabilization-oriented posture with converging signals";

        if (attentionReport.DominantOperationalPriority == OperationalPriorityType.StabilizationFirst)
            return "Stabilization-first strategic orientation from attention routing";

        if (resilienceReport.StabilizationDurability.Contains("strong", StringComparison.OrdinalIgnoreCase))
            return resilienceReport.StabilizationDurability;

        return "Stabilization posture within moderate bounded continuity";
    }

    private static string DescribeEscalationCoordinationState(
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        OperationalAttentionReportDto attentionReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        if (fragilities.Any(f => f.FragilityType == OperationalFragilityType.EscalationRecurrence))
            return "Escalation coordination strained by recurring escalation signals";

        if (topology.TopologyState == OperationalTopologyState.EscalationDominant)
            return "Escalation coordination dominated by topology propagation pressure";

        if (evolutionTimeline.EscalationMomentum.Contains("collapsing", StringComparison.OrdinalIgnoreCase))
            return "Escalation coordination improving as propagation collapses";

        if (attentionReport.DominantOperationalPriority == OperationalPriorityType.EscalationDominant)
            return "Escalation coordination elevated — escalation-dominant attention routing";

        return convergenceReport.EscalationConfidence;
    }

    private static string DescribeRecoveryCoordinationState(
        OperationalRecoveryPostureDto recovery,
        OperationalConvergenceReportDto convergenceReport,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalResilienceReportDto resilienceReport,
        OperationalAttentionReportDto attentionReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        if (fragilities.Any(f => f.FragilityType == OperationalFragilityType.RecoveryBrittleness))
            return "Reactive recovery posture — recovery improving without convergence reinforcement";

        if (recovery.OverallDirection is OperationalRecoveryDirection.Converging
            && convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Strong
            && evolutionTimeline.RecoveryMomentum.Contains("accelerating", StringComparison.OrdinalIgnoreCase))
            return "Strong recovery coordination with reinforcing convergence";

        if (recovery.OverallDirection is OperationalRecoveryDirection.Degrading
            or OperationalRecoveryDirection.Diverging)
            return "Recovery coordination degrading";

        if (attentionReport.DominantOperationalPriority == OperationalPriorityType.RecoveryValidation)
            return "Recovery coordination reactive — validation focus dominates attention";

        return resilienceReport.RecoverySustainability;
    }

    private static OperationalCoordinationStrength MapAttentionPressureToCoordination(OperationalUrgencyLevel level)
    {
        return level switch
        {
            OperationalUrgencyLevel.Critical => OperationalCoordinationStrength.Fragmented,
            OperationalUrgencyLevel.High => OperationalCoordinationStrength.Weak,
            OperationalUrgencyLevel.Elevated => OperationalCoordinationStrength.Moderate,
            _ => OperationalCoordinationStrength.Strong
        };
    }

    private static string ResolveDominantArea(
        OperationalAttentionReportDto attentionReport,
        OperationalTopologyDto topology,
        OperationalResilienceReportDto resilienceReport)
    {
        if (!string.IsNullOrWhiteSpace(attentionReport.HighestUrgencyArea))
            return NormalizeArea(attentionReport.HighestUrgencyArea);

        if (!string.IsNullOrWhiteSpace(resilienceReport.HighestFragilityArea))
            return NormalizeArea(resilienceReport.HighestFragilityArea);

        return NormalizeArea(topology.HighestInfluenceArea);
    }

    private static string ComposeOperatorSummary(
        OperationalStrategicPostureType dominantPosture,
        string stabilizationState,
        string escalationState,
        string recoveryState,
        OperationalCoordinationStrength alignmentStrength,
        string strategicFocus,
        IReadOnlyList<OperationalStrategySnapshot> priorSnapshots)
    {
        var continuity = string.Empty;
        if (priorSnapshots.Count > 0)
        {
            var prior = priorSnapshots[^1];
            if (prior.DominantOperationalPosture != dominantPosture)
            {
                continuity =
                    $" Posture shifted from {prior.DominantOperationalPosture.ToString().ToLowerInvariant()} " +
                    $"to {dominantPosture.ToString().ToLowerInvariant()}.";
            }
        }

        return
            $"Operational strategic posture is {dominantPosture.ToString().ToLowerInvariant()}. " +
            $"Stabilization: {stabilizationState.ToLowerInvariant()}. " +
            $"Escalation coordination: {escalationState.ToLowerInvariant()}. " +
            $"Recovery coordination: {recoveryState.ToLowerInvariant()}. " +
            $"Alignment strength: {alignmentStrength.ToString().ToLowerInvariant()}. " +
            $"Strategic focus: {strategicFocus.ToLowerInvariant()}.{continuity}";
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
