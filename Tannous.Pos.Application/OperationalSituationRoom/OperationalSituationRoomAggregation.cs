using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;

using Tannous.Pos.Application.OperationalCognition;

namespace Tannous.Pos.Application.OperationalSituationRoom;

/// <summary>Deterministic operator briefing synthesis from existing operational read models.</summary>
public static class OperationalSituationRoomAggregation
{
    public const int MaxNarratives = 8;
    public const int MaxRiskConcentrations = 8;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;

    public const string AreaReplay = "Replay";
    public const string AreaInventory = "Inventory";
    public const string AreaReconciliation = "Reconciliation";
    public const string AreaRuntime = "Runtime";
    public const string AreaOperational = "Operational Stability";

    public static OperationalSituationRoomDto ComposeSituationRoom(
        OperationalDashboardSummaryDto dashboard,
        OperationalTrendSummaryDto trend,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        OperationalRecoveryOutlookDto recoveryOutlook,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalCausalChainsDto chains,
        IReadOnlyList<OperationalSituationSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var platformCondition = ResolvePlatformCondition(recovery, dashboard, incidentSummary);
        var dominantRisk = ResolveDominantRisk(causalitySummary, propagation, recovery);
        var stabilizationDirection = ResolveStabilizationDirection(recovery, propagation, trend);
        var escalationSeverity = ResolveEscalationSeverity(
            recovery,
            propagation,
            incidentSummary,
            dashboard);
        var attentionLevel = ResolveAttentionLevel(triage, escalationSeverity, incidentSummary);
        var highestFocus = ResolveHighestPriorityFocus(triage, dominantRisk);
        var recommendedFocus = ResolveRecommendedFocus(dominantRisk, propagation, recovery);
        var outlookDetail = ComposeOutlook(
            recovery,
            recoveryOutlook,
            propagation,
            dominantRisk,
            recommendedFocus,
            stabilizationDirection);
        var narratives = ComposeNarratives(
            platformCondition,
            dominantRisk,
            stabilizationDirection,
            recovery,
            recoveryOutlook,
            incidentSummary,
            causalitySummary,
            propagation,
            triage,
            highestFocus);
        var riskConcentrations = ComposeRiskConcentrations(
            incidentSummary,
            propagation,
            recovery,
            chains);

        var operatorSummary = ComposeOperatorSummary(
            platformCondition,
            dominantRisk,
            stabilizationDirection,
            incidentSummary,
            causalitySummary);
        var executiveSummary = ComposeExecutiveSummary(
            platformCondition,
            dominantRisk,
            escalationSeverity,
            recovery,
            outlookDetail);
        var primaryNarrative = narratives.Count > 0
            ? narratives[0].OperatorInterpretation
            : "Platform operating within expected advisory bounds";

        var outlookBrief = outlookDetail.RecoveryTrajectory switch
        {
            OperationalSituationDirection.Improving => "Recovery trajectory improving with stabilization signals emerging",
            OperationalSituationDirection.Stabilizing => "Stabilization progressing; monitor residual pressure",
            OperationalSituationDirection.Escalating => "Escalation pressure active; prioritize dominant instability",
            OperationalSituationDirection.Degrading => "Operational outlook degrading; investigation focus required",
            _ => "Operational outlook stable; continue routine monitoring"
        };

        if (priorSnapshots.Count >= 2)
        {
            var previous = priorSnapshots[^1];
            if (previous.PlatformCondition != platformCondition
                && platformCondition == OperationalSituationState.Degrading)
            {
                outlookBrief = "Platform condition shifted toward degradation since prior briefing";
            }
            else if (previous.StabilizationDirection == OperationalSituationDirection.Degrading
                     && stabilizationDirection == OperationalSituationDirection.Improving)
            {
                outlookBrief = "Stabilization improving relative to prior briefing";
            }
        }

        return new OperationalSituationRoomDto
        {
            GeneratedAtUtc = generatedAtUtc,
            PlatformCondition = platformCondition,
            DominantOperationalRisk = dominantRisk,
            StabilizationDirection = stabilizationDirection,
            RecoveryConfidence = recovery.OverallConfidence,
            EscalationSeverity = escalationSeverity,
            ActiveIncidentCount = incidentSummary.ActiveIncidentCount,
            EscalatingPropagationCount = causalitySummary.EscalatingPropagationCount,
            HighestPriorityFocus = highestFocus,
            RecommendedOperationalFocus = recommendedFocus,
            OperatorSummary = operatorSummary,
            ExecutiveSummary = executiveSummary,
            OperationalNarrative = primaryNarrative,
            AttentionLevel = attentionLevel,
            Outlook = outlookBrief,
            OutlookDetail = outlookDetail,
            Narratives = narratives,
            RiskConcentrations = riskConcentrations
        };
    }

    public static OperationalExecutiveBriefingDto ComposeExecutiveBriefing(
        OperationalSituationRoomDto situationRoom)
    {
        var headline = situationRoom.EscalationSeverity switch
        {
            OperationalExecutiveSeverity.Critical => "Critical operational instability requires immediate leadership attention",
            OperationalExecutiveSeverity.High => "Elevated operational pressure across platform domains",
            OperationalExecutiveSeverity.Elevated => "Operational conditions warrant focused monitoring",
            _ => "Platform operating within advisory stability bounds"
        };

        var escalationStatus = situationRoom.EscalationSeverity switch
        {
            OperationalExecutiveSeverity.Critical => "Critical escalation pressure active",
            OperationalExecutiveSeverity.High => "High escalation pressure detected",
            OperationalExecutiveSeverity.Elevated => "Elevated escalation signals present",
            _ => "Escalation pressure within normal bounds"
        };

        var recoveryOutlook = situationRoom.StabilizationDirection switch
        {
            OperationalSituationDirection.Improving => "Recovery outlook improving",
            OperationalSituationDirection.Stabilizing => "Stabilization in progress",
            OperationalSituationDirection.Escalating => "Recovery outlook constrained by active escalation",
            OperationalSituationDirection.Degrading => "Recovery outlook degrading",
            _ => "Recovery outlook stable"
        };

        var recommendedAction = situationRoom.RecommendedOperationalFocus;

        return new OperationalExecutiveBriefingDto
        {
            Headline = headline,
            Situation = DescribePlatformCondition(situationRoom.PlatformCondition),
            DominantRisk = situationRoom.DominantOperationalRisk,
            RecoveryOutlook = recoveryOutlook,
            EscalationStatus = escalationStatus,
            RecommendedAction = recommendedAction,
            Confidence = situationRoom.RecoveryConfidence,
            Summary = situationRoom.ExecutiveSummary
        };
    }

    public static OperationalSituationSummaryDto ComposeSituationSummary(
        OperationalSituationRoomDto situationRoom,
        OperationalCausalitySummaryDto causalitySummary)
    {
        var executiveAttention = situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.High
            || situationRoom.AttentionLevel >= OperationalAttentionLevel.High
            || situationRoom.ActiveIncidentCount >= 3;

        return new OperationalSituationSummaryDto
        {
            PlatformState = situationRoom.PlatformCondition,
            DominantArea = string.IsNullOrWhiteSpace(causalitySummary.DominantOperationalArea)
                ? AreaOperational
                : causalitySummary.DominantOperationalArea,
            OverallRecoveryDirection = situationRoom.StabilizationDirection,
            EscalationPressure = situationRoom.EscalationSeverity,
            RecoveryConfidence = situationRoom.RecoveryConfidence,
            OperatorAttentionLevel = situationRoom.AttentionLevel,
            ExecutiveAttentionRequired = executiveAttention
        };
    }

    public static OperationalSituationSnapshot CreateSnapshot(OperationalSituationRoomDto room)
    {
        return new OperationalSituationSnapshot
        {
            GeneratedAtUtc = room.GeneratedAtUtc,
            PlatformCondition = room.PlatformCondition,
            DominantOperationalRisk = room.DominantOperationalRisk,
            StabilizationDirection = room.StabilizationDirection,
            ActiveIncidentCount = room.ActiveIncidentCount,
            EscalatingPropagationCount = room.EscalatingPropagationCount,
            AttentionLevel = room.AttentionLevel,
            OperatorSummary = room.OperatorSummary
        };
    }

    private static OperationalSituationState ResolvePlatformCondition(
        OperationalRecoveryPostureDto recovery,
        OperationalDashboardSummaryDto dashboard,
        OperationalIncidentCasesSummaryDto incidentSummary)
    {
        if (recovery.OverallState == OperationalRecoveryState.Saturated
            || recovery.OverallSeverity >= OperationalRecoverySeverity.Critical
            || incidentSummary.HighestSeverity >= OperationalIncidentSeverity.Critical)
            return OperationalSituationState.Critical;

        if (recovery.OverallState is OperationalRecoveryState.Degrading or OperationalRecoveryState.Volatile
            || dashboard.Pressure.ProtectiveModeActive
            || incidentSummary.EscalatingIncidentCount >= 2)
            return OperationalSituationState.Degrading;

        if (recovery.OverallState is OperationalRecoveryState.Recovering or OperationalRecoveryState.Stabilizing
            || recovery.OverallDirection is OperationalRecoveryDirection.Improving or OperationalRecoveryDirection.Converging)
            return OperationalSituationState.Recovering;

        if (recovery.OverallState == OperationalRecoveryState.Stable
            && incidentSummary.ActiveIncidentCount == 0
            && !dashboard.Pressure.RuntimeSaturationIndicated)
            return OperationalSituationState.Stable;

        return OperationalSituationState.Stressed;
    }

    private static string ResolveDominantRisk(
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalRecoveryPostureDto recovery)
    {
        var area = causalitySummary.DominantOperationalArea;
        if (string.IsNullOrWhiteSpace(area))
            area = AreaOperational;

        var escalatingReplay = propagation.Propagations.Any(p =>
            p.IsEscalating
            && string.Equals(p.SourceArea, AreaReplay, StringComparison.OrdinalIgnoreCase));

        if (escalatingReplay || string.Equals(area, AreaReplay, StringComparison.OrdinalIgnoreCase))
            return "Replay instability";

        if (string.Equals(area, AreaRuntime, StringComparison.OrdinalIgnoreCase)
            || recovery.OverallSeverity >= OperationalRecoverySeverity.Critical)
            return "Runtime survivability degradation";

        if (string.Equals(area, AreaInventory, StringComparison.OrdinalIgnoreCase))
            return "Inventory drift pressure";

        if (string.Equals(area, AreaReconciliation, StringComparison.OrdinalIgnoreCase))
            return "Reconciliation instability";

        return "Operational volatility";
    }

    private static OperationalSituationDirection ResolveStabilizationDirection(
        OperationalRecoveryPostureDto recovery,
        OperationalPropagationAnalysisDto propagation,
        OperationalTrendSummaryDto trend)
    {
        var escalating = propagation.Propagations.Count(p => p.IsEscalating);
        var collapsing = propagation.Propagations.Count(p => p.IsCollapsing);

        if (escalating >= 2 && collapsing == 0)
            return OperationalSituationDirection.Escalating;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving or OperationalRecoveryDirection.Converging
            || trend.OverallDirection == OperationalTrendDirection.Improving)
            return OperationalSituationDirection.Improving;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Degrading or OperationalRecoveryDirection.Diverging
            || trend.OverallDirection == OperationalTrendDirection.Degrading)
            return OperationalSituationDirection.Degrading;

        if (recovery.OverallState == OperationalRecoveryState.Stabilizing || collapsing >= escalating)
            return OperationalSituationDirection.Stabilizing;

        return OperationalSituationDirection.Stable;
    }

    private static OperationalExecutiveSeverity ResolveEscalationSeverity(
        OperationalRecoveryPostureDto recovery,
        OperationalPropagationAnalysisDto propagation,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalDashboardSummaryDto dashboard)
    {
        if (recovery.OverallSeverity >= OperationalRecoverySeverity.Critical
            || dashboard.Pressure.ProtectiveModeActive
            || incidentSummary.HighestSeverity >= OperationalIncidentSeverity.Critical)
            return OperationalExecutiveSeverity.Critical;

        var escalating = propagation.Propagations.Count(p => p.IsEscalating);
        if (escalating >= 2 || incidentSummary.EscalatingIncidentCount >= 2)
            return OperationalExecutiveSeverity.High;

        if (escalating >= 1 || incidentSummary.ActiveIncidentCount >= 1)
            return OperationalExecutiveSeverity.Elevated;

        return OperationalExecutiveSeverity.Normal;
    }

    private static OperationalAttentionLevel ResolveAttentionLevel(
        OperationalTriageQueueDto triage,
        OperationalExecutiveSeverity escalationSeverity,
        OperationalIncidentCasesSummaryDto incidentSummary)
    {
        if (escalationSeverity >= OperationalExecutiveSeverity.Critical
            || triage.OverallPriority >= OperationalTriagePriority.Critical)
            return OperationalAttentionLevel.Critical;

        if (escalationSeverity >= OperationalExecutiveSeverity.High
            || triage.OverallPriority >= OperationalTriagePriority.High
            || incidentSummary.EscalatingIncidentCount >= 2)
            return OperationalAttentionLevel.High;

        if (escalationSeverity >= OperationalExecutiveSeverity.Elevated
            || triage.ItemCount > 0
            || incidentSummary.ActiveIncidentCount > 0)
            return OperationalAttentionLevel.Elevated;

        return OperationalAttentionLevel.Normal;
    }

    private static string ResolveHighestPriorityFocus(
        OperationalTriageQueueDto triage,
        string dominantRisk)
    {
        var top = triage.Items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Summary, StringComparer.Ordinal)
            .FirstOrDefault();

        if (top != null && !string.IsNullOrWhiteSpace(top.Summary))
            return top.Summary;

        return dominantRisk switch
        {
            "Replay instability" => "Replay workbench pressure review",
            "Runtime survivability degradation" => "Runtime protection stability review",
            "Inventory drift pressure" => "Inventory drift workbench review",
            "Reconciliation instability" => "Reconciliation queue escalation review",
            _ => "Operational stability monitoring"
        };
    }

    private static string ResolveRecommendedFocus(
        string dominantRisk,
        OperationalPropagationAnalysisDto propagation,
        OperationalRecoveryPostureDto recovery)
    {
        var escalatingReplay = propagation.Propagations.Any(p =>
            p.IsEscalating
            && string.Equals(p.SourceArea, AreaReplay, StringComparison.OrdinalIgnoreCase));

        if (escalatingReplay)
            return "Replay workbench stabilization and reconciliation follow-through";

        if (string.Equals(dominantRisk, "Runtime survivability degradation", StringComparison.Ordinal))
            return "Runtime protection review and survivability monitoring";

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving or OperationalRecoveryDirection.Converging)
            return "Stabilization monitoring and residual pressure watch";

        return dominantRisk switch
        {
            "Inventory drift pressure" => "Inventory drift resolution and hotspot review",
            "Reconciliation instability" => "Reconciliation queue triage and conflict review",
            _ => "Cross-domain operational monitoring"
        };
    }

    private static OperationalSituationOutlookDto ComposeOutlook(
        OperationalRecoveryPostureDto recovery,
        OperationalRecoveryOutlookDto recoveryOutlook,
        OperationalPropagationAnalysisDto propagation,
        string dominantRisk,
        string recommendedFocus,
        OperationalSituationDirection stabilizationDirection)
    {
        var escalating = propagation.Propagations.Count(p => p.IsEscalating);
        var collapsing = propagation.Propagations.Count(p => p.IsCollapsing);

        var escalationLikelihood = escalating switch
        {
            >= 2 => OperationalExecutiveSeverity.High,
            1 => OperationalExecutiveSeverity.Elevated,
            _ => OperationalExecutiveSeverity.Normal
        };

        var stabilizationLikelihood = collapsing >= escalating && collapsing > 0
            ? OperationalExecutiveSeverity.High
            : stabilizationDirection is OperationalSituationDirection.Improving or OperationalSituationDirection.Stabilizing
                ? OperationalExecutiveSeverity.Elevated
                : OperationalExecutiveSeverity.Normal;

        var dominantConstraint = propagation.StabilizationBlockers
            .OrderByDescending(b => b.PreventingRecovery)
            .ThenBy(b => b.Area, StringComparer.Ordinal)
            .FirstOrDefault()?.Description
            ?? recoveryOutlook.Summary
            ?? dominantRisk;

        if (string.IsNullOrWhiteSpace(dominantConstraint))
            dominantConstraint = "No dominant constraint identified";

        return new OperationalSituationOutlookDto
        {
            RecoveryTrajectory = stabilizationDirection,
            EscalationLikelihood = escalationLikelihood,
            StabilizationLikelihood = stabilizationLikelihood,
            OperationalConfidence = recovery.OverallConfidence,
            DominantConstraint = dominantConstraint,
            RecommendedOperatorPriority = recommendedFocus
        };
    }

    private static IReadOnlyList<OperationalNarrativeDto> ComposeNarratives(
        OperationalSituationState platformCondition,
        string dominantRisk,
        OperationalSituationDirection stabilizationDirection,
        OperationalRecoveryPostureDto recovery,
        OperationalRecoveryOutlookDto recoveryOutlook,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalTriageQueueDto triage,
        string highestFocus)
    {
        var items = new List<(int Priority, OperationalNarrativeDto Narrative)>();

        items.Add((1, new OperationalNarrativeDto
        {
            NarrativeType = OperationalNarrativeType.PlatformPosture,
            Title = "Platform condition",
            Description = DescribePlatformCondition(platformCondition),
            Severity = MapSeverity(platformCondition),
            Direction = stabilizationDirection,
            RelatedArea = AreaOperational,
            OperatorInterpretation = $"Platform condition is {DescribePlatformCondition(platformCondition).ToLowerInvariant()}"
        }));

        items.Add((2, new OperationalNarrativeDto
        {
            NarrativeType = OperationalNarrativeType.DominantRisk,
            Title = "Dominant operational risk",
            Description = dominantRisk,
            Severity = MapRiskSeverity(dominantRisk),
            Direction = stabilizationDirection,
            RelatedArea = causalitySummary.DominantOperationalArea,
            OperatorInterpretation = $"{dominantRisk} is the dominant operational instability"
        }));

        if (propagation.Propagations.Any(p => p.IsEscalating))
        {
            var topPropagation = propagation.Propagations
                .Where(p => p.IsEscalating)
                .OrderByDescending(p => p.IsEscalating)
                .ThenBy(p => p.SourceArea, StringComparer.Ordinal)
                .First();

            items.Add((3, new OperationalNarrativeDto
            {
                NarrativeType = OperationalNarrativeType.PropagationAlert,
                Title = "Escalating propagation",
                Description = topPropagation.OperatorInterpretation,
                Severity = OperationalExecutiveSeverity.High,
                Direction = OperationalSituationDirection.Escalating,
                RelatedArea = topPropagation.SourceArea,
                OperatorInterpretation = topPropagation.OperatorInterpretation
            }));
        }

        if (stabilizationDirection is OperationalSituationDirection.Improving or OperationalSituationDirection.Stabilizing)
        {
            items.Add((4, new OperationalNarrativeDto
            {
                NarrativeType = OperationalNarrativeType.RecoveryOutlook,
                Title = "Recovery improvement",
                Description = recovery.Summary,
                Severity = OperationalExecutiveSeverity.Elevated,
                Direction = OperationalSituationDirection.Improving,
                RelatedArea = AreaOperational,
                OperatorInterpretation = "Recovery outlook improving with propagation pressure collapsing"
            }));
        }

        if (incidentSummary.ActiveIncidentCount > 0)
        {
            items.Add((5, new OperationalNarrativeDto
            {
                NarrativeType = OperationalNarrativeType.IncidentContinuity,
                Title = "Active incident continuity",
                Description = incidentSummary.Summary,
                Severity = MapIncidentSeverity(incidentSummary.HighestSeverity),
                Direction = stabilizationDirection,
                RelatedArea = causalitySummary.DominantOperationalArea,
                OperatorInterpretation =
                    $"{incidentSummary.ActiveIncidentCount} active incident case(s) require continuity review"
            }));
        }

        if (triage.ItemCount > 0)
        {
            items.Add((6, new OperationalNarrativeDto
            {
                NarrativeType = OperationalNarrativeType.InvestigationPriority,
                Title = "Investigation priority",
                Description = highestFocus,
                Severity = MapTriageSeverity(triage.OverallPriority),
                Direction = stabilizationDirection,
                RelatedArea = causalitySummary.DominantOperationalArea,
                OperatorInterpretation = $"Highest investigation priority: {highestFocus}"
            }));
        }

        if (string.Equals(dominantRisk, "Runtime survivability degradation", StringComparison.Ordinal))
        {
            items.Add((2, new OperationalNarrativeDto
            {
                NarrativeType = OperationalNarrativeType.EscalationPressure,
                Title = "Runtime protection instability",
                Description = "Survivability degradation impacting platform recovery",
                Severity = OperationalExecutiveSeverity.Critical,
                Direction = OperationalSituationDirection.Degrading,
                RelatedArea = AreaRuntime,
                OperatorInterpretation = "Runtime survivability degradation impacting platform recovery"
            }));
        }

        if (dominantRisk.Contains("Replay", StringComparison.Ordinal)
            && propagation.Propagations.Any(p =>
                p.IsEscalating && string.Equals(p.SourceArea, AreaReplay, StringComparison.OrdinalIgnoreCase)))
        {
            items.Add((2, new OperationalNarrativeDto
            {
                NarrativeType = OperationalNarrativeType.EscalationPressure,
                Title = "Escalating replay pressure",
                Description = "Replay pressure propagating into reconciliation instability",
                Severity = OperationalExecutiveSeverity.High,
                Direction = OperationalSituationDirection.Escalating,
                RelatedArea = AreaReplay,
                OperatorInterpretation = "Replay pressure propagating into reconciliation instability"
            }));
        }

        if (!string.IsNullOrWhiteSpace(recoveryOutlook.Summary))
        {
            items.Add((7, new OperationalNarrativeDto
            {
                NarrativeType = OperationalNarrativeType.StabilizationProgress,
                Title = "Stabilization outlook",
                Description = recoveryOutlook.Summary,
                Severity = OperationalExecutiveSeverity.Elevated,
                Direction = stabilizationDirection,
                RelatedArea = AreaOperational,
                OperatorInterpretation = recoveryOutlook.Summary
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Narrative.Title, StringComparer.Ordinal)
            .Take(MaxNarratives)
            .Select(i => i.Narrative)
            .ToList();
    }

    private static IReadOnlyList<OperationalRiskConcentrationDto> ComposeRiskConcentrations(
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalRecoveryPostureDto recovery,
        OperationalCausalChainsDto chains)
    {
        var areas = new[] { AreaReplay, AreaInventory, AreaReconciliation, AreaRuntime, AreaOperational };
        var items = new List<OperationalRiskConcentrationDto>();

        foreach (var area in areas)
        {
            var incidentContribution = chains.Chains.Count(c =>
                string.Equals(c.DominantArea, area, StringComparison.OrdinalIgnoreCase));
            var propagationContribution = propagation.Propagations.Count(p =>
                (string.Equals(p.SourceArea, area, StringComparison.OrdinalIgnoreCase)
                 || string.Equals(p.TargetArea, area, StringComparison.OrdinalIgnoreCase))
                && p.IsEscalating);

            if (incidentContribution == 0 && propagationContribution == 0 && area != AreaOperational)
                continue;

            var severity = propagationContribution switch
            {
                >= 2 => OperationalExecutiveSeverity.High,
                1 => OperationalExecutiveSeverity.Elevated,
                _ => incidentContribution >= 2
                    ? OperationalExecutiveSeverity.Elevated
                    : OperationalExecutiveSeverity.Normal
            };

            if (string.Equals(area, AreaRuntime, StringComparison.Ordinal)
                && recovery.OverallSeverity >= OperationalRecoverySeverity.Critical)
                severity = OperationalExecutiveSeverity.Critical;

            var recoveryImpact = recovery.Convergence
                .FirstOrDefault(c => string.Equals(c.Domain, area, StringComparison.OrdinalIgnoreCase))
                ?.Summary
                ?? "No direct recovery convergence signal";

            items.Add(new OperationalRiskConcentrationDto
            {
                Area = area,
                Severity = severity,
                IncidentContribution = incidentContribution,
                PropagationContribution = propagationContribution,
                RecoveryImpact = recoveryImpact,
                StabilizationRisk = propagationContribution >= 1
                    ? "Active propagation may delay stabilization"
                    : "Stabilization risk within normal bounds",
                OperatorAttentionRequired = severity >= OperationalExecutiveSeverity.Elevated
                    || (area == AreaOperational && incidentSummary.ActiveIncidentCount > 0)
            });
        }

        return items
            .OrderByDescending(r => r.Severity)
            .ThenByDescending(r => r.PropagationContribution)
            .ThenBy(r => r.Area, StringComparer.Ordinal)
            .Take(MaxRiskConcentrations)
            .ToList();
    }

    private static string ComposeOperatorSummary(
        OperationalSituationState platformCondition,
        string dominantRisk,
        OperationalSituationDirection stabilizationDirection,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary)
    {
        return
            $"Platform is {DescribePlatformCondition(platformCondition).ToLowerInvariant()}. " +
            $"Dominant risk: {dominantRisk.ToLowerInvariant()}. " +
            $"Stabilization is {DescribeDirection(stabilizationDirection).ToLowerInvariant()}. " +
            $"{incidentSummary.ActiveIncidentCount} active incident(s); " +
            $"{causalitySummary.EscalatingPropagationCount} escalating propagation signal(s).";
    }

    private static string ComposeExecutiveSummary(
        OperationalSituationState platformCondition,
        string dominantRisk,
        OperationalExecutiveSeverity escalationSeverity,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationOutlookDto outlook)
    {
        return
            $"{DescribePlatformCondition(platformCondition)} with {dominantRisk.ToLowerInvariant()} as the primary concern. " +
            $"Escalation severity is {escalationSeverity.ToString().ToLowerInvariant()}. " +
            $"Recovery confidence is {recovery.OverallConfidence.ToString().ToLowerInvariant()}. " +
            $"Dominant constraint: {outlook.DominantConstraint.ToLowerInvariant()}.";
    }

    private static string DescribePlatformCondition(OperationalSituationState state) =>
        state switch
        {
            OperationalSituationState.Critical => "Critical operational instability",
            OperationalSituationState.Degrading => "Degrading operational conditions",
            OperationalSituationState.Recovering => "Recovering operational conditions",
            OperationalSituationState.Stressed => "Stressed but manageable conditions",
            _ => "Stable operational conditions"
        };

    private static string DescribeDirection(OperationalSituationDirection direction) =>
        direction switch
        {
            OperationalSituationDirection.Improving => "Improving",
            OperationalSituationDirection.Stabilizing => "Stabilizing",
            OperationalSituationDirection.Escalating => "Escalating",
            OperationalSituationDirection.Degrading => "Degrading",
            _ => "Stable"
        };

    private static OperationalExecutiveSeverity MapSeverity(OperationalSituationState state) =>
        state switch
        {
            OperationalSituationState.Critical => OperationalExecutiveSeverity.Critical,
            OperationalSituationState.Degrading => OperationalExecutiveSeverity.High,
            OperationalSituationState.Stressed => OperationalExecutiveSeverity.Elevated,
            _ => OperationalExecutiveSeverity.Normal
        };

    private static OperationalExecutiveSeverity MapRiskSeverity(string dominantRisk) =>
        dominantRisk switch
        {
            "Runtime survivability degradation" => OperationalExecutiveSeverity.Critical,
            "Replay instability" or "Reconciliation instability" => OperationalExecutiveSeverity.High,
            _ => OperationalExecutiveSeverity.Elevated
        };

    private static OperationalExecutiveSeverity MapIncidentSeverity(OperationalIncidentSeverity severity) =>
        severity switch
        {
            OperationalIncidentSeverity.Critical => OperationalExecutiveSeverity.Critical,
            OperationalIncidentSeverity.High => OperationalExecutiveSeverity.High,
            OperationalIncidentSeverity.Elevated => OperationalExecutiveSeverity.Elevated,
            _ => OperationalExecutiveSeverity.Normal
        };

    private static OperationalExecutiveSeverity MapTriageSeverity(OperationalTriagePriority priority) =>
        priority switch
        {
            OperationalTriagePriority.Critical => OperationalExecutiveSeverity.Critical,
            OperationalTriagePriority.High => OperationalExecutiveSeverity.High,
            OperationalTriagePriority.Elevated => OperationalExecutiveSeverity.Elevated,
            _ => OperationalExecutiveSeverity.Normal
        };
}
