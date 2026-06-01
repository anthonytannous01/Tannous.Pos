using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Application.OperationalAttention;
using Tannous.Pos.Application.OperationalConvergence;
using Tannous.Pos.Application.OperationalEvolution;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalResilience;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalStrategy;
using Tannous.Pos.Application.OperationalTopology;

namespace Tannous.Pos.Application.OperationalEquilibrium;

/// <summary>Deterministic operational equilibrium and systemic balance from bounded cognition continuity.</summary>
public static class OperationalEquilibriumAggregation
{
    public const int MaxSystemicBalances = 8;
    public const int MaxImbalances = 8;
    public const int MaxPressureDistributions = 8;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;

    public const string AreaReplay = "Replay";
    public const string AreaRuntime = "Runtime";
    public const string AreaReconciliation = "Reconciliation";
    public const string AreaOperational = "Operational Stability";

    public static OperationalEquilibriumReportDto ComposeEquilibriumReport(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        OperationalAttentionReportDto attentionReport,
        OperationalStrategyReportDto strategyReport,
        OperationalIntegrityReportDto integrityReport,
        IReadOnlyList<OperationalFragilityDto> fragilities,
        IReadOnlyList<OperationalEquilibriumSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var dominantArea = ResolveDominantArea(strategyReport, attentionReport, resilienceReport, topology);
        var imbalances = ComposeImbalances(
            dominantArea,
            recovery,
            situationRoom,
            evolutionTimeline,
            topology,
            convergenceReport,
            resilienceReport,
            attentionReport,
            strategyReport,
            integrityReport,
            fragilities);

        var systemicBalances = ComposeSystemicBalances(
            dominantArea,
            recovery,
            situationRoom,
            evolutionTimeline,
            topology,
            convergenceReport,
            resilienceReport,
            attentionReport,
            strategyReport,
            fragilities);

        var pressureDistributions = ComposePressureDistributions(
            dominantArea,
            recovery,
            situationRoom,
            evolutionTimeline,
            topology,
            convergenceReport,
            attentionReport,
            strategyReport,
            fragilities);

        var equilibriumState = ResolveEquilibriumState(
            recovery,
            situationRoom,
            evolutionTimeline,
            convergenceReport,
            resilienceReport,
            attentionReport,
            strategyReport,
            imbalances,
            integrityReport);

        var continuity = ComposeEquilibriumContinuity(
            dominantArea,
            equilibriumState,
            recovery,
            situationRoom,
            evolutionTimeline,
            strategyReport,
            imbalances,
            priorSnapshots);

        var systemicStrain = ResolveSystemicStrainLevel(
            situationRoom,
            attentionReport,
            strategyReport,
            fragilities,
            imbalances);

        var stabilizationBalance = DescribeStabilizationBalance(
            recovery,
            situationRoom,
            evolutionTimeline,
            convergenceReport,
            strategyReport);

        var escalationBalance = DescribeEscalationBalance(
            situationRoom,
            evolutionTimeline,
            topology,
            convergenceReport,
            attentionReport,
            fragilities);

        var recoveryEquilibrium = DescribeRecoveryEquilibriumStrength(
            recovery,
            convergenceReport,
            evolutionTimeline,
            resilienceReport,
            strategyReport,
            fragilities);

        var highestImbalance = imbalances
            .OrderByDescending(i => i.ImbalanceSeverity)
            .ThenBy(i => i.OperationalArea, StringComparer.Ordinal)
            .FirstOrDefault()?.OperationalArea
            ?? dominantArea;

        var operatorSummary = ComposeOperatorSummary(
            equilibriumState,
            stabilizationBalance,
            escalationBalance,
            recoveryEquilibrium,
            systemicStrain,
            highestImbalance,
            priorSnapshots);

        return new OperationalEquilibriumReportDto
        {
            GeneratedAtUtc = generatedAtUtc,
            EquilibriumState = equilibriumState,
            StabilizationBalance = stabilizationBalance,
            EscalationBalance = escalationBalance,
            RecoveryEquilibriumStrength = recoveryEquilibrium,
            SystemicStrainLevel = systemicStrain,
            HighestImbalanceArea = highestImbalance,
            SystemicBalances = systemicBalances,
            Imbalances = imbalances,
            PressureDistributions = pressureDistributions,
            EquilibriumContinuity = continuity,
            OperatorSummary = operatorSummary
        };
    }

    public static OperationalEquilibriumSummaryDto ComposeEquilibriumSummary(
        OperationalEquilibriumReportDto report,
        OperationalSituationRoomDto situationRoom,
        IReadOnlyList<OperationalImbalanceDto> imbalances,
        DateTime generatedAtUtc)
    {
        var highestPressure = imbalances
            .OrderByDescending(i => i.ImbalanceSeverity)
            .FirstOrDefault()?.StrainConcentration
            ?? report.EscalationBalance;

        var strongestStabilization = report.SystemicBalances
            .OrderByDescending(b => b.BalanceStrength)
            .FirstOrDefault()?.OperatorInterpretation
            ?? report.StabilizationBalance;

        var weakestRecovery = report.SystemicBalances
            .OrderBy(b => b.BalanceStrength)
            .FirstOrDefault()?.RecoveryPressure
            ?? report.RecoveryEquilibriumStrength;

        var direction = ResolveEquilibriumDirection(report, imbalances);

        var summary =
            $"Operational equilibrium is {report.EquilibriumState.ToString().ToLowerInvariant()} with systemic strain " +
            $"{report.SystemicStrainLevel.ToString().ToLowerInvariant()}. Highest imbalance: {report.HighestImbalanceArea.ToLowerInvariant()}.";

        return new OperationalEquilibriumSummaryDto
        {
            GeneratedAtUtc = generatedAtUtc,
            DominantEquilibriumState = report.EquilibriumState,
            HighestImbalancePressure = highestPressure,
            StrongestStabilizationBalance = strongestStabilization,
            WeakestRecoveryEquilibrium = weakestRecovery,
            OperationalEquilibriumDirection = direction,
            OperatorAttentionLevel = situationRoom.AttentionLevel.ToString(),
            Summary = summary
        };
    }

    public static IReadOnlyList<OperationalImbalanceDto> ComposeImbalances(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        OperationalAttentionReportDto attentionReport,
        OperationalStrategyReportDto strategyReport,
        OperationalIntegrityReportDto integrityReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        var imbalances = new List<OperationalImbalanceDto>();

        if (evolutionTimeline.EscalationMomentum.Contains("expanding", StringComparison.OrdinalIgnoreCase)
            && situationRoom.EscalatingPropagationCount >= 2)
        {
            imbalances.Add(new OperationalImbalanceDto
            {
                ImbalanceId = "imbalance-escalation-overload",
                OperationalArea = dominantArea,
                ImbalanceType = OperationalImbalanceType.EscalationOverload,
                ImbalanceSeverity = OperationalBalanceStrength.Weak,
                StrainConcentration = "Escalation continuity recurring across bounded window",
                StabilizationRisk = "Stabilization may not keep pace with escalation propagation",
                RecommendedOperatorFocus = "Rebalance attention toward escalation containment before expansion"
            });
        }

        if (strategyReport.OperationalAlignmentStrength == OperationalCoordinationStrength.Fragmented
            || integrityReport.ContradictionCount >= 2)
        {
            imbalances.Add(new OperationalImbalanceDto
            {
                ImbalanceId = "imbalance-stabilization-fragmentation",
                OperationalArea = dominantArea,
                ImbalanceType = OperationalImbalanceType.StabilizationFragmentation,
                ImbalanceSeverity = OperationalBalanceStrength.Brittle,
                StrainConcentration = "Strategic alignment fragmented across operational signals",
                StabilizationRisk = "Stabilization forces not coherently coordinated",
                RecommendedOperatorFocus = "Restore stabilization coherence before assuming equilibrium"
            });
        }

        if (resilienceReport.SurvivabilityState is OperationalSurvivabilityState.Strained
                or OperationalSurvivabilityState.Fragile
                or OperationalSurvivabilityState.Critical)
        {
            imbalances.Add(new OperationalImbalanceDto
            {
                ImbalanceId = "imbalance-survivability-asymmetry",
                OperationalArea = NormalizeArea(resilienceReport.HighestFragilityArea),
                ImbalanceType = OperationalImbalanceType.SurvivabilityAsymmetry,
                ImbalanceSeverity = resilienceReport.SurvivabilityState == OperationalSurvivabilityState.Critical
                    ? OperationalBalanceStrength.Brittle
                    : OperationalBalanceStrength.Weak,
                StrainConcentration = $"Survivability {resilienceReport.SurvivabilityState.ToString().ToLowerInvariant()} concentrates strain",
                StabilizationRisk = resilienceReport.StabilizationDurability,
                RecommendedOperatorFocus = "Rebalance survivability pressure before downstream recovery expansion"
            });
        }

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            && (convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Weak
                or OperationalConvergenceStrength.Fragmented
                || strategyReport.OperationalAlignmentStrength == OperationalCoordinationStrength.Fragmented))
        {
            imbalances.Add(new OperationalImbalanceDto
            {
                ImbalanceId = "imbalance-recovery-weak",
                OperationalArea = NormalizeArea(convergenceReport.HighestAmbiguityArea),
                ImbalanceType = OperationalImbalanceType.RecoveryImbalance,
                ImbalanceSeverity = OperationalBalanceStrength.Brittle,
                StrainConcentration = "Recovery improving while convergence or alignment fragmented",
                StabilizationRisk = "Recovery equilibrium weak and unstable",
                RecommendedOperatorFocus = "Validate recovery equilibrium before assuming sustained balance"
            });
        }

        if (attentionReport.AttentionPressureLevel >= OperationalUrgencyLevel.High
            && fragilities.Any(f => f.FragilityType == OperationalFragilityType.ContainmentInstability))
        {
            imbalances.Add(new OperationalImbalanceDto
            {
                ImbalanceId = "imbalance-containment-concentration",
                OperationalArea = AreaRuntime,
                ImbalanceType = OperationalImbalanceType.ContainmentConcentration,
                ImbalanceSeverity = OperationalBalanceStrength.Weak,
                StrainConcentration = "Containment strain concentrated under elevated attention pressure",
                StabilizationRisk = "Containment durability may not sustain continued pressure",
                RecommendedOperatorFocus = "Rebalance containment load before systemic overload"
            });
        }

        if (topology.TopologyState == OperationalTopologyState.EscalationDominant
            || topology.TopologyState == OperationalTopologyState.Concentrated)
        {
            imbalances.Add(new OperationalImbalanceDto
            {
                ImbalanceId = "imbalance-topology-concentration",
                OperationalArea = NormalizeArea(topology.HighestInfluenceArea),
                ImbalanceType = OperationalImbalanceType.EscalationOverload,
                ImbalanceSeverity = OperationalBalanceStrength.Moderate,
                StrainConcentration = "Operational pressure concentrated in dependency topology",
                StabilizationRisk = topology.StabilizationDependencyStrength,
                RecommendedOperatorFocus = "Review topology concentration before assuming systemic balance"
            });
        }

        if (IsAttentionOverloaded(attentionReport)
            && fragilities.Count >= 3)
        {
            imbalances.Add(new OperationalImbalanceDto
            {
                ImbalanceId = "imbalance-attention-overload",
                OperationalArea = attentionReport.HighestUrgencyArea,
                ImbalanceType = OperationalImbalanceType.EscalationOverload,
                ImbalanceSeverity = OperationalBalanceStrength.Brittle,
                StrainConcentration = "Attention overloaded with concentrated fragility signals",
                StabilizationRisk = "Systemic imbalance elevated under attention overload",
                RecommendedOperatorFocus = "Reduce attention concentration before equilibrium degrades"
            });
        }

        if (imbalances.Count == 0)
        {
            imbalances.Add(new OperationalImbalanceDto
            {
                ImbalanceId = "imbalance-balanced",
                OperationalArea = dominantArea,
                ImbalanceType = OperationalImbalanceType.StabilizationFragmentation,
                ImbalanceSeverity = OperationalBalanceStrength.Strong,
                StrainConcentration = "No significant imbalance in bounded continuity window",
                StabilizationRisk = "Stabilization within normal equilibrium bounds",
                RecommendedOperatorFocus = "Maintain routine operational balance monitoring"
            });
        }

        return imbalances
            .OrderByDescending(i => i.ImbalanceSeverity)
            .ThenBy(i => i.ImbalanceId, StringComparer.Ordinal)
            .Take(MaxImbalances)
            .ToList();
    }

    public static OperationalEquilibriumSnapshot CreateSnapshot(
        OperationalEquilibriumReportDto report,
        IReadOnlyList<OperationalImbalanceDto> imbalances)
    {
        return new OperationalEquilibriumSnapshot
        {
            GeneratedAtUtc = report.GeneratedAtUtc,
            EquilibriumState = report.EquilibriumState,
            SystemicStrainLevel = report.SystemicStrainLevel,
            HighestImbalanceArea = report.HighestImbalanceArea,
            ImbalanceCount = imbalances.Count
        };
    }

    private static IReadOnlyList<OperationalSystemicBalanceDto> ComposeSystemicBalances(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        OperationalAttentionReportDto attentionReport,
        OperationalStrategyReportDto strategyReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        var balances = new List<OperationalSystemicBalanceDto>
        {
            BuildSystemicBalance(
                dominantArea,
                recovery,
                situationRoom,
                evolutionTimeline,
                convergenceReport,
                resilienceReport,
                attentionReport,
                strategyReport,
                fragilities.Count(f => AreasMatch(f.OperationalArea, dominantArea)))
        };

        if (!AreasMatch(dominantArea, AreaRuntime)
            && resilienceReport.SurvivabilityState is OperationalSurvivabilityState.Strained
                or OperationalSurvivabilityState.Fragile
                or OperationalSurvivabilityState.Critical)
        {
            balances.Add(BuildSystemicBalance(
                AreaRuntime,
                recovery,
                situationRoom,
                evolutionTimeline,
                convergenceReport,
                resilienceReport,
                attentionReport,
                strategyReport,
                fragilities.Count(f => AreasMatch(f.OperationalArea, AreaRuntime))));
        }

        if (topology.TopologyState == OperationalTopologyState.EscalationDominant)
        {
            balances.Add(BuildSystemicBalance(
                NormalizeArea(topology.HighestInfluenceArea),
                recovery,
                situationRoom,
                evolutionTimeline,
                convergenceReport,
                resilienceReport,
                attentionReport,
                strategyReport,
                fragilities.Count(f => AreasMatch(f.OperationalArea, topology.HighestInfluenceArea))));
        }

        return balances
            .OrderByDescending(b => b.BalanceStrength)
            .ThenBy(b => b.OperationalArea, StringComparer.Ordinal)
            .Take(MaxSystemicBalances)
            .ToList();
    }

    private static OperationalSystemicBalanceDto BuildSystemicBalance(
        string area,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        OperationalAttentionReportDto attentionReport,
        OperationalStrategyReportDto strategyReport,
        int localFragilityCount)
    {
        var strength = localFragilityCount switch
        {
            0 when convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Strong
                && strategyReport.OperationalAlignmentStrength == OperationalCoordinationStrength.Strong
                => OperationalBalanceStrength.Strong,
            0 => OperationalBalanceStrength.Moderate,
            1 => OperationalBalanceStrength.Weak,
            _ => OperationalBalanceStrength.Brittle
        };

        var stabilizationPressure = situationRoom.StabilizationDirection is OperationalSituationDirection.Escalating
                or OperationalSituationDirection.Degrading
            ? "Stabilization pressure elevated"
            : evolutionTimeline.StabilizationMomentum;

        var escalationPressure = evolutionTimeline.EscalationMomentum.Contains("expanding", StringComparison.OrdinalIgnoreCase)
            ? "Escalation pressure expanding"
            : evolutionTimeline.EscalationMomentum.Contains("collapsing", StringComparison.OrdinalIgnoreCase)
                ? "Escalation pressure collapsing"
                : "Escalation pressure within bounds";

        var recoveryPressure = recovery.OverallDirection is OperationalRecoveryDirection.Degrading
                or OperationalRecoveryDirection.Diverging
            ? "Recovery pressure elevated"
            : recovery.OverallDirection.ToString();

        return new OperationalSystemicBalanceDto
        {
            OperationalArea = area,
            BalanceStrength = strength,
            StabilizationPressure = stabilizationPressure,
            EscalationPressure = escalationPressure,
            RecoveryPressure = recoveryPressure,
            CoordinationBalance = strategyReport.OperationalAlignmentStrength.ToString(),
            OperatorInterpretation =
                $"Systemic balance in {area.ToLowerInvariant()} is {strength.ToString().ToLowerInvariant()} " +
                $"with attention {attentionReport.AttentionPressureLevel.ToString().ToLowerInvariant()}"
        };
    }

    private static IReadOnlyList<OperationalPressureDistributionDto> ComposePressureDistributions(
        string dominantArea,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        OperationalAttentionReportDto attentionReport,
        OperationalStrategyReportDto strategyReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        var distributions = new List<OperationalPressureDistributionDto>();

        var escalationWeight = attentionReport.EscalationFocusArea.Contains("replay", StringComparison.OrdinalIgnoreCase)
            ? "Escalation weight elevated in replay domain"
            : situationRoom.EscalatingPropagationCount >= 2
                ? "Escalation weight elevated from propagation continuity"
                : "Escalation weight within normal distribution";

        distributions.Add(new OperationalPressureDistributionDto
        {
            OperationalArea = dominantArea,
            PressureWeight = attentionReport.AttentionPressureLevel.ToString(),
            EscalationDistribution = escalationWeight,
            StabilizationDistribution = attentionReport.StabilizationFocusArea,
            RecoveryDistribution = attentionReport.InvestigationPriorityArea,
            OperatorInterpretation =
                $"Pressure distribution in {dominantArea.ToLowerInvariant()} governed by {attentionReport.DominantOperationalPriority.ToString().ToLowerInvariant()} priority"
        });

        if (!AreasMatch(dominantArea, AreaRuntime))
        {
            distributions.Add(new OperationalPressureDistributionDto
            {
                OperationalArea = AreaRuntime,
                PressureWeight = fragilities.Any(f => AreasMatch(f.OperationalArea, AreaRuntime))
                    ? "Elevated"
                    : "Normal",
                EscalationDistribution = evolutionTimeline.EscalationMomentum,
                StabilizationDistribution = "Runtime containment governs stabilization distribution",
                RecoveryDistribution = recovery.OverallDirection.ToString(),
                OperatorInterpretation = "Runtime pressure distribution influences platform equilibrium"
            });
        }

        if (topology.TopologyState == OperationalTopologyState.Concentrated
            || topology.TopologyState == OperationalTopologyState.EscalationDominant)
        {
            distributions.Add(new OperationalPressureDistributionDto
            {
                OperationalArea = NormalizeArea(topology.HighestInfluenceArea),
                PressureWeight = "Elevated",
                EscalationDistribution = topology.EscalationPropagationStrength,
                StabilizationDistribution = topology.StabilizationDependencyStrength,
                RecoveryDistribution = convergenceReport.ConvergenceStrength.ToString(),
                OperatorInterpretation =
                    $"Topology concentration in {topology.HighestInfluenceArea.ToLowerInvariant()} skews pressure distribution"
            });
        }

        if (strategyReport.OperationalAlignmentStrength == OperationalCoordinationStrength.Fragmented)
        {
            distributions.Add(new OperationalPressureDistributionDto
            {
                OperationalArea = AreaOperational,
                PressureWeight = "High",
                EscalationDistribution = strategyReport.EscalationCoordinationState,
                StabilizationDistribution = strategyReport.StrategicStabilizationState,
                RecoveryDistribution = strategyReport.RecoveryCoordinationState,
                OperatorInterpretation = "Strategic fragmentation skews platform-wide pressure distribution"
            });
        }

        return distributions
            .OrderByDescending(d => d.PressureWeight, StringComparer.Ordinal)
            .ThenBy(d => d.OperationalArea, StringComparer.Ordinal)
            .Take(MaxPressureDistributions)
            .ToList();
    }

    private static OperationalEquilibriumContinuityDto ComposeEquilibriumContinuity(
        string dominantArea,
        OperationalEquilibriumState equilibriumState,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalStrategyReportDto strategyReport,
        IReadOnlyList<OperationalImbalanceDto> imbalances,
        IReadOnlyList<OperationalEquilibriumSnapshot> priorSnapshots)
    {
        var prior = priorSnapshots.LastOrDefault();

        var equilibriumShift = prior != null && prior.EquilibriumState != equilibriumState
            ? OperationalContinuityPhrasing.StateShift(
                "Equilibrium",
                prior.EquilibriumState.ToString().ToLowerInvariant(),
                equilibriumState.ToString().ToLowerInvariant())
            : OperationalContinuityPhrasing.ConsistentAcrossBoundedWindow("Equilibrium");

        var stabilizationConsistency = evolutionTimeline.StabilizationMomentum.Contains("accelerating", StringComparison.OrdinalIgnoreCase)
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Stabilizing
                or OperationalSituationDirection.Improving
            ? "Stabilization balance consistency strengthening"
            : $"Stabilization balance {OperationalInterpretationVocabulary.WithinModerateContinuity}";

        var escalationConsistency = OperationalContinuityPhrasing.EscalationMomentumAlignment(
            evolutionTimeline.EscalationMomentum,
            "Escalation balance consistency improving",
            "Escalation balance consistency weakening",
            "Escalation balance consistency stable");

        var recoveryAlignment = OperationalContinuityPhrasing.RecoveryAlignment(
            recovery,
            "Recovery equilibrium alignment improving",
            "Recovery equilibrium requires upstream balance");

        var coordinationAlignment = strategyReport.OperationalAlignmentStrength switch
        {
            OperationalCoordinationStrength.Strong => "Systemic coordination alignment strong",
            OperationalCoordinationStrength.Fragmented => "Systemic coordination alignment fragmented",
            _ => "Systemic coordination alignment within moderate bounds"
        };

        return new OperationalEquilibriumContinuityDto
        {
            DominantEquilibriumShift = equilibriumShift,
            StabilizationBalanceConsistency = stabilizationConsistency,
            EscalationBalanceConsistency = escalationConsistency,
            RecoveryEquilibriumAlignment = recoveryAlignment,
            SystemicCoordinationAlignment = coordinationAlignment,
            OperatorInterpretation =
                $"Equilibrium continuity in {dominantArea.ToLowerInvariant()} with {imbalances.Count} imbalance signal(s) " +
                $"and strategy {strategyReport.DominantOperationalPosture.ToString().ToLowerInvariant()}"
        };
    }

    private static OperationalEquilibriumState ResolveEquilibriumState(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalConvergenceReportDto convergenceReport,
        OperationalResilienceReportDto resilienceReport,
        OperationalAttentionReportDto attentionReport,
        OperationalStrategyReportDto strategyReport,
        IReadOnlyList<OperationalImbalanceDto> imbalances,
        OperationalIntegrityReportDto integrityReport)
    {
        if (IsAttentionOverloaded(attentionReport)
            || imbalances.Count(i => i.ImbalanceSeverity == OperationalBalanceStrength.Brittle) >= 2)
            return OperationalEquilibriumState.Overloaded;

        if (strategyReport.OperationalAlignmentStrength == OperationalCoordinationStrength.Fragmented
            || integrityReport.ContradictionCount >= 2
            || convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Fragmented)
            return OperationalEquilibriumState.Fragmented;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            && (convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Weak
                or OperationalConvergenceStrength.Fragmented))
            return OperationalEquilibriumState.RecoveryImbalanced;

        if (imbalances.Any(i => i.ImbalanceType == OperationalImbalanceType.EscalationOverload)
            || situationRoom.EscalatingPropagationCount >= 2
            || evolutionTimeline.EscalationMomentum.Contains("expanding", StringComparison.OrdinalIgnoreCase))
            return OperationalEquilibriumState.EscalationStrained;

        if (evolutionTimeline.EscalationMomentum.Contains("collapsing", StringComparison.OrdinalIgnoreCase)
            && convergenceReport.ConvergenceStrength is OperationalConvergenceStrength.Strong
                or OperationalConvergenceStrength.Moderate
            && resilienceReport.SurvivabilityState is OperationalSurvivabilityState.Strong
                or OperationalSurvivabilityState.Stable
            && strategyReport.OperationalAlignmentStrength is OperationalCoordinationStrength.Strong
                or OperationalCoordinationStrength.Moderate)
            return OperationalEquilibriumState.StabilizationDominant;

        if (imbalances.Count <= 1
            && attentionReport.AttentionPressureLevel <= OperationalUrgencyLevel.Elevated)
            return OperationalEquilibriumState.Balanced;

        return OperationalEquilibriumState.EscalationStrained;
    }

    private static OperationalStrainLevel ResolveSystemicStrainLevel(
        OperationalSituationRoomDto situationRoom,
        OperationalAttentionReportDto attentionReport,
        OperationalStrategyReportDto strategyReport,
        IReadOnlyList<OperationalFragilityDto> fragilities,
        IReadOnlyList<OperationalImbalanceDto> imbalances)
    {
        if (IsAttentionOverloaded(attentionReport)
            || imbalances.Count(i => i.ImbalanceSeverity == OperationalBalanceStrength.Brittle) >= 2)
            return OperationalStrainLevel.Critical;

        if (fragilities.Count >= 3
            || attentionReport.AttentionPressureLevel >= OperationalUrgencyLevel.High
            || strategyReport.OperationalAlignmentStrength == OperationalCoordinationStrength.Fragmented)
            return OperationalStrainLevel.High;

        if (situationRoom.AttentionLevel is OperationalAttentionLevel.Elevated
                or OperationalAttentionLevel.High
            || attentionReport.AttentionPressureLevel == OperationalUrgencyLevel.Elevated
            || imbalances.Count >= 2)
            return OperationalStrainLevel.Elevated;

        return OperationalStrainLevel.Normal;
    }

    private static OperationalEquilibriumDirection ResolveEquilibriumDirection(
        OperationalEquilibriumReportDto report,
        IReadOnlyList<OperationalImbalanceDto> imbalances)
    {
        if (report.EquilibriumState == OperationalEquilibriumState.StabilizationDominant)
            return OperationalEquilibriumDirection.TowardStabilization;

        if (report.EquilibriumState is OperationalEquilibriumState.Fragmented
                or OperationalEquilibriumState.Overloaded)
            return OperationalEquilibriumDirection.TowardFragmentation;

        if (report.SystemicStrainLevel >= OperationalStrainLevel.High)
            return OperationalEquilibriumDirection.Strained;

        if (imbalances.Count <= 1 && report.EquilibriumState == OperationalEquilibriumState.Balanced)
            return OperationalEquilibriumDirection.TowardBalance;

        return OperationalEquilibriumDirection.Stable;
    }

    private static string DescribeStabilizationBalance(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalConvergenceReportDto convergenceReport,
        OperationalStrategyReportDto strategyReport)
    {
        if (evolutionTimeline.EscalationMomentum.Contains("collapsing", StringComparison.OrdinalIgnoreCase)
            && convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Strong
            && strategyReport.OperationalAlignmentStrength == OperationalCoordinationStrength.Strong)
            return "Stabilization strongly outweighs escalation — equilibrium stabilization-oriented";

        if (situationRoom.StabilizationDirection is OperationalSituationDirection.Stabilizing
                or OperationalSituationDirection.Improving)
            return "Stabilization balance improving within bounded continuity";

        return strategyReport.StrategicStabilizationState;
    }

    private static string DescribeEscalationBalance(
        OperationalSituationRoomDto situationRoom,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalTopologyDto topology,
        OperationalConvergenceReportDto convergenceReport,
        OperationalAttentionReportDto attentionReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        if (fragilities.Count >= 3 && attentionReport.AttentionPressureLevel >= OperationalUrgencyLevel.High)
            return "Escalation balance strained — systemic imbalance elevated";

        if (topology.TopologyState == OperationalTopologyState.EscalationDominant)
            return "Escalation balance dominated by topology propagation pressure";

        if (evolutionTimeline.EscalationMomentum.Contains("collapsing", StringComparison.OrdinalIgnoreCase))
            return "Escalation balance improving as propagation collapses";

        return convergenceReport.EscalationConfidence;
    }

    private static string DescribeRecoveryEquilibriumStrength(
        OperationalRecoveryPostureDto recovery,
        OperationalConvergenceReportDto convergenceReport,
        OperationalEvolutionTimelineDto evolutionTimeline,
        OperationalResilienceReportDto resilienceReport,
        OperationalStrategyReportDto strategyReport,
        IReadOnlyList<OperationalFragilityDto> fragilities)
    {
        if (fragilities.Any(f => f.FragilityType == OperationalFragilityType.RecoveryBrittleness)
            || strategyReport.DominantOperationalPosture == OperationalStrategicPostureType.ReactiveRecovery)
            return "Recovery equilibrium weak and unstable";

        if (recovery.OverallDirection is OperationalRecoveryDirection.Converging
            && convergenceReport.ConvergenceStrength == OperationalConvergenceStrength.Strong
            && evolutionTimeline.RecoveryMomentum.Contains("accelerating", StringComparison.OrdinalIgnoreCase))
            return "Strong recovery equilibrium with reinforcing convergence";

        return resilienceReport.RecoverySustainability;
    }

    private static string ComposeOperatorSummary(
        OperationalEquilibriumState state,
        string stabilizationBalance,
        string escalationBalance,
        string recoveryEquilibrium,
        OperationalStrainLevel strainLevel,
        string highestImbalance,
        IReadOnlyList<OperationalEquilibriumSnapshot> priorSnapshots)
    {
        var continuity = string.Empty;
        if (priorSnapshots.Count > 0)
        {
            var prior = priorSnapshots[^1];
            if (prior.EquilibriumState != state)
            {
                continuity =
                    $" Equilibrium moved from {prior.EquilibriumState.ToString().ToLowerInvariant()} " +
                    $"to {state.ToString().ToLowerInvariant()}.";
            }
        }

        return
            $"Operational equilibrium is {state.ToString().ToLowerInvariant()}. " +
            $"Stabilization balance: {stabilizationBalance.ToLowerInvariant()}. " +
            $"Escalation balance: {escalationBalance.ToLowerInvariant()}. " +
            $"Recovery equilibrium: {recoveryEquilibrium.ToLowerInvariant()}. " +
            $"Systemic strain: {strainLevel.ToString().ToLowerInvariant()}. " +
            $"Highest imbalance: {highestImbalance.ToLowerInvariant()}.{continuity}";
    }

    private static string ResolveDominantArea(
        OperationalStrategyReportDto strategyReport,
        OperationalAttentionReportDto attentionReport,
        OperationalResilienceReportDto resilienceReport,
        OperationalTopologyDto topology)
    {
        if (!string.IsNullOrWhiteSpace(strategyReport.DominantStrategicFocus))
            return NormalizeArea(strategyReport.DominantStrategicFocus);

        if (!string.IsNullOrWhiteSpace(attentionReport.HighestUrgencyArea))
            return NormalizeArea(attentionReport.HighestUrgencyArea);

        if (!string.IsNullOrWhiteSpace(resilienceReport.HighestFragilityArea))
            return NormalizeArea(resilienceReport.HighestFragilityArea);

        return NormalizeArea(topology.HighestInfluenceArea);
    }

    private static bool IsAttentionOverloaded(OperationalAttentionReportDto attentionReport)
    {
        return attentionReport.AttentionPressureLevel == OperationalUrgencyLevel.Critical
            || (attentionReport.AttentionPressureLevel >= OperationalUrgencyLevel.High
                && attentionReport.Priorities.Count >= 4);
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
