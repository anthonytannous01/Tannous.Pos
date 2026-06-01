using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTriage;

using Tannous.Pos.Application.OperationalCognition;

namespace Tannous.Pos.Application.OperationalPlaybooks;

/// <summary>Deterministic operational response playbook synthesis from existing read models.</summary>
public static class OperationalPlaybookAggregation
{
    public const int MaxPlaybooks = 8;
    public const int MaxResponseSteps = 8;
    public const int MaxEscalationGuidance = 8;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;
    public const int MaxSequenceSteps = 4;

    public const string AreaReplay = "Replay";
    public const string AreaInventory = "Inventory";
    public const string AreaReconciliation = "Reconciliation";
    public const string AreaRuntime = "Runtime";
    public const string AreaOperational = "Operational Stability";

    public const string PlaybookReplayStabilization = "playbook-replay-stabilization";
    public const string PlaybookRuntimeContainment = "playbook-runtime-containment";
    public const string PlaybookInventoryDrift = "playbook-inventory-drift-stabilization";
    public const string PlaybookReconciliationRecovery = "playbook-reconciliation-recovery";
    public const string PlaybookIncidentContinuity = "playbook-incident-continuity";
    public const string PlaybookRecoveryAcceleration = "playbook-recovery-acceleration";
    public const string PlaybookCrossDomainMonitoring = "playbook-cross-domain-monitoring";

    public static OperationalPlaybooksDto ComposePlaybooks(
        OperationalDashboardSummaryDto dashboard,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        OperationalRecoveryOutlookDto recoveryOutlook,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationScenariosDto simulation,
        IReadOnlyList<OperationalPlaybookSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var playbooks = ComposePlaybookItems(
            dashboard,
            recovery,
            recoveryOutlook,
            incidentSummary,
            causalitySummary,
            propagation,
            situationRoom,
            simulation);

        var responseSteps = ComposeResponseSteps(playbooks, propagation, recovery, triage);
        var escalationGuidance = ComposeEscalationGuidance(
            propagation,
            recovery,
            incidentSummary,
            situationRoom,
            simulation);
        var alignment = ComposeResponseAlignment(
            incidentSummary,
            recovery,
            causalitySummary,
            simulation,
            situationRoom);

        if (priorSnapshots.Count >= 2
            && priorSnapshots[^1].PlaybookCount != playbooks.Count)
        {
            playbooks = playbooks
                .OrderByDescending(p => p.Severity)
                .ThenBy(p => p.PlaybookId, StringComparer.Ordinal)
                .Take(MaxPlaybooks)
                .ToList();
        }

        return new OperationalPlaybooksDto
        {
            GeneratedAtUtc = generatedAtUtc,
            PlaybookCount = playbooks.Count,
            ResponseStepCount = responseSteps.Count,
            EscalationGuidanceCount = escalationGuidance.Count,
            Playbooks = playbooks,
            ResponseSteps = responseSteps,
            EscalationGuidance = escalationGuidance,
            ResponseAlignment = alignment
        };
    }

    public static OperationalPlaybookSummaryDto ComposeSummary(
        OperationalPlaybooksDto playbooks,
        OperationalStabilizationGuidanceDto stabilizationGuidance,
        OperationalSituationRoomDto situationRoom,
        OperationalRecoveryPostureDto recovery,
        DateTime generatedAtUtc)
    {
        var highestPriority = playbooks.Playbooks
            .OrderByDescending(p => p.Severity)
            .ThenBy(p => p.DominantArea, StringComparer.Ordinal)
            .FirstOrDefault()?.DominantArea
            ?? situationRoom.RecommendedOperationalFocus;

        var recoveryReadiness = recovery.OverallDirection switch
        {
            OperationalRecoveryDirection.Improving or OperationalRecoveryDirection.Converging =>
                "Recovery readiness improving",
            OperationalRecoveryDirection.Degrading or OperationalRecoveryDirection.Diverging =>
                "Recovery readiness constrained",
            _ => "Recovery readiness stable"
        };

        var summary =
            $"{playbooks.PlaybookCount} active playbook(s) with {playbooks.ResponseStepCount} sequenced response step(s). " +
            $"Highest priority area: {highestPriority.ToLowerInvariant()}. " +
            $"Dominant constraint: {stabilizationGuidance.DominantConstraint.ToLowerInvariant()}.";

        return new OperationalPlaybookSummaryDto
        {
            GeneratedAtUtc = generatedAtUtc,
            ActivePlaybookCount = playbooks.PlaybookCount,
            EscalationGuidanceCount = playbooks.EscalationGuidanceCount,
            StabilizationGuidanceCount = playbooks.PlaybookCount > 0 ? 1 : 0,
            HighestPriorityArea = highestPriority,
            DominantRecoveryConstraint = stabilizationGuidance.DominantConstraint,
            RecoveryReadiness = recoveryReadiness,
            OperatorAttentionLevel = situationRoom.AttentionLevel,
            Summary = summary
        };
    }

    public static OperationalStabilizationGuidanceDto ComposeStabilizationGuidance(
        OperationalPlaybooksDto playbooks,
        OperationalSimulationScenariosDto simulation,
        OperationalPropagationAnalysisDto propagation,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom)
    {
        var dominantConstraint = propagation.StabilizationBlockers
            .OrderByDescending(b => b.PreventingRecovery)
            .ThenBy(b => b.Area, StringComparer.Ordinal)
            .FirstOrDefault()?.Description
            ?? situationRoom.OutlookDetail.DominantConstraint;

        if (string.IsNullOrWhiteSpace(dominantConstraint))
            dominantConstraint = "No dominant constraint identified";

        var recoveryOrder = playbooks.ResponseSteps
            .OrderBy(s => s.SequenceOrder)
            .Select(s => $"{s.SequenceOrder}. {s.Area}: {s.RecommendedFocus}")
            .Take(MaxSequenceSteps)
            .ToList();

        if (recoveryOrder.Count == 0)
        {
            recoveryOrder = simulation.StabilizationPaths
                .OrderByDescending(p => p.RecoveryAccelerationPotential)
                .SelectMany(p => p.ExpectedImprovementSequence)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxSequenceSteps)
                .Select((area, index) => $"{index + 1}. {area}: stabilization review")
                .ToList();
        }

        if (recoveryOrder.Count == 0)
            recoveryOrder.Add("1. Operational stability: routine monitoring");

        var acceleration = playbooks.Playbooks
            .OrderByDescending(p => p.OperationalConfidence)
            .FirstOrDefault()?.ScenarioType == OperationalPlaybookScenarioType.RecoveryAcceleration
            ? OperationalStabilizationPriority.High
            : simulation.StabilizationPaths
                .OrderByDescending(p => p.RecoveryAccelerationPotential)
                .FirstOrDefault()?.RecoveryAccelerationPotential switch
                {
                    OperationalLeverageStrength.Critical => OperationalStabilizationPriority.Immediate,
                    OperationalLeverageStrength.Strong => OperationalStabilizationPriority.High,
                    OperationalLeverageStrength.Moderate => OperationalStabilizationPriority.Elevated,
                    _ => OperationalStabilizationPriority.Monitoring
                };

        var likelihood = situationRoom.StabilizationDirection switch
        {
            OperationalSituationDirection.Improving or OperationalSituationDirection.Stabilizing =>
                OperationalGuidanceSeverity.High,
            OperationalSituationDirection.Escalating or OperationalSituationDirection.Degrading =>
                OperationalGuidanceSeverity.Elevated,
            _ => OperationalGuidanceSeverity.Normal
        };

        return new OperationalStabilizationGuidanceDto
        {
            DominantConstraint = dominantConstraint,
            RecommendedRecoveryOrder = recoveryOrder,
            RecoveryAccelerationPotential = acceleration,
            OperationalRiskReduction = playbooks.Playbooks.Count > 0
                ? "Upstream stabilization before downstream validation reduces cross-domain escalation risk"
                : "Operational risk contained under current advisory bounds",
            StabilizationLikelihood = likelihood,
            OperatorPriority = situationRoom.RecommendedOperationalFocus
        };
    }

    public static OperationalPlaybookSnapshot CreateSnapshot(
        OperationalPlaybooksDto playbooks,
        OperationalPlaybookSummaryDto summary)
    {
        return new OperationalPlaybookSnapshot
        {
            GeneratedAtUtc = summary.GeneratedAtUtc,
            PlaybookCount = playbooks.PlaybookCount,
            EscalationGuidanceCount = playbooks.EscalationGuidanceCount,
            HighestPriorityArea = summary.HighestPriorityArea,
            DominantConstraint = summary.DominantRecoveryConstraint,
            OperatorSummary = summary.Summary
        };
    }

    private static List<OperationalPlaybookDto> ComposePlaybookItems(
        OperationalDashboardSummaryDto dashboard,
        OperationalRecoveryPostureDto recovery,
        OperationalRecoveryOutlookDto recoveryOutlook,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationScenariosDto simulation)
    {
        var items = new List<(int Priority, OperationalPlaybookDto Playbook)>();

        var replayVisible = propagation.Propagations.Any(p =>
            string.Equals(p.SourceArea, AreaReplay, StringComparison.OrdinalIgnoreCase));
        var runtimeVisible = dashboard.Pressure.ProtectiveModeActive
            || dashboard.Pressure.RuntimeSaturationIndicated
            || propagation.Propagations.Any(p =>
                string.Equals(p.SourceArea, AreaRuntime, StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.TargetArea, AreaRuntime, StringComparison.OrdinalIgnoreCase));
        var inventoryVisible = propagation.Propagations.Any(p =>
            string.Equals(p.SourceArea, AreaInventory, StringComparison.OrdinalIgnoreCase));
        var reconciliationVisible = propagation.Propagations.Any(p =>
            string.Equals(p.SourceArea, AreaReconciliation, StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.TargetArea, AreaReconciliation, StringComparison.OrdinalIgnoreCase));

        if (replayVisible)
        {
            items.Add((1, CreateReplayStabilizationPlaybook(recovery, propagation, situationRoom)));
        }

        if (runtimeVisible)
        {
            items.Add((2, CreateRuntimeContainmentPlaybook(recovery, propagation, situationRoom)));
        }

        if (inventoryVisible)
        {
            items.Add((3, CreateInventoryDriftPlaybook(recovery, situationRoom)));
        }

        if (reconciliationVisible)
        {
            items.Add((4, CreateReconciliationRecoveryPlaybook(recovery, propagation, situationRoom)));
        }

        if (incidentSummary.ActiveIncidentCount > 0)
        {
            items.Add((5, new OperationalPlaybookDto
            {
                PlaybookId = PlaybookIncidentContinuity,
                Title = "Incident continuity response",
                ScenarioType = OperationalPlaybookScenarioType.IncidentContinuity,
                DominantArea = causalitySummary.DominantOperationalArea,
                Severity = MapIncidentSeverity(incidentSummary.HighestSeverity),
                StabilizationObjective = "Maintain incident continuity while stabilizing dominant instability",
                RecommendedSequence = new[]
                {
                    "Review active incident case continuity",
                    "Align investigation with triage priorities",
                    "Validate recovery posture alignment",
                    "Reassess escalation pressure"
                },
                EstimatedOperationalImpact = "Incident escalation containment and recovery alignment",
                RecoveryAlignment = recovery.Summary,
                OperationalConfidence = MapRecoveryConfidence(recovery.OverallConfidence),
                OperatorSummary = "Follow incident continuity sequencing before broad cross-domain changes"
            }));
        }

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving or OperationalRecoveryDirection.Converging)
        {
            items.Add((6, new OperationalPlaybookDto
            {
                PlaybookId = PlaybookRecoveryAcceleration,
                Title = "Recovery acceleration guidance",
                ScenarioType = OperationalPlaybookScenarioType.RecoveryAcceleration,
                DominantArea = causalitySummary.DominantOperationalArea,
                Severity = OperationalGuidanceSeverity.Elevated,
                StabilizationObjective = "Accelerate recovery by addressing dominant constraint first",
                RecommendedSequence = new[]
                {
                    $"Address dominant constraint: {situationRoom.OutlookDetail.DominantConstraint}",
                    "Validate propagation collapse signals",
                    "Confirm recovery convergence across domains",
                    "Monitor residual operational pressure"
                },
                EstimatedOperationalImpact = recoveryOutlook.Summary,
                RecoveryAlignment = recovery.Summary,
                OperationalConfidence = OperationalResponseConfidence.High,
                OperatorSummary = "Recovery acceleration path viable when dominant constraint eases"
            }));
        }

        if (items.Count == 0)
        {
            items.Add((99, new OperationalPlaybookDto
            {
                PlaybookId = PlaybookCrossDomainMonitoring,
                Title = "Cross-domain monitoring",
                ScenarioType = OperationalPlaybookScenarioType.CrossDomainMonitoring,
                DominantArea = AreaOperational,
                Severity = OperationalGuidanceSeverity.Normal,
                StabilizationObjective = "Maintain routine operational monitoring",
                RecommendedSequence = new[]
                {
                    "Review operational dashboard posture",
                    "Confirm triage queue is within expected bounds",
                    "Validate recovery posture stability",
                    "Continue routine monitoring"
                },
                EstimatedOperationalImpact = "No active stabilization sequence required",
                RecoveryAlignment = recovery.Summary,
                OperationalConfidence = OperationalResponseConfidence.Moderate,
                OperatorSummary = "Platform within advisory stability bounds; continue routine monitoring"
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Playbook.PlaybookId, StringComparer.Ordinal)
            .Take(MaxPlaybooks)
            .Select(i => i.Playbook)
            .ToList();
    }

    private static OperationalPlaybookDto CreateReplayStabilizationPlaybook(
        OperationalRecoveryPostureDto recovery,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom)
    {
        var escalating = propagation.Propagations.Any(p =>
            p.IsEscalating && string.Equals(p.SourceArea, AreaReplay, StringComparison.OrdinalIgnoreCase));

        return new OperationalPlaybookDto
        {
            PlaybookId = PlaybookReplayStabilization,
            Title = "Replay stabilization playbook",
            ScenarioType = OperationalPlaybookScenarioType.ReplayStabilization,
            DominantArea = AreaReplay,
            Severity = escalating ? OperationalGuidanceSeverity.High : OperationalGuidanceSeverity.Elevated,
            StabilizationObjective = "Collapse replay propagation and restore reconciliation alignment",
            RecommendedSequence = new[]
            {
                "Evaluate replay escalation hotspots",
                "Review reconciliation pressure alignment",
                "Monitor runtime survivability",
                "Validate stabilization convergence"
            },
            EstimatedOperationalImpact = "Propagation collapse and recovery confidence improvement",
            RecoveryAlignment = recovery.Summary,
            OperationalConfidence = MapRecoveryConfidence(recovery.OverallConfidence),
            OperatorSummary = "Stabilize replay upstream before downstream reconciliation validation"
        };
    }

    private static OperationalPlaybookDto CreateRuntimeContainmentPlaybook(
        OperationalRecoveryPostureDto recovery,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom)
    {
        var critical = recovery.OverallSeverity >= OperationalRecoverySeverity.Critical
            || situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.Critical;

        return new OperationalPlaybookDto
        {
            PlaybookId = PlaybookRuntimeContainment,
            Title = "Runtime pressure containment",
            ScenarioType = OperationalPlaybookScenarioType.RuntimeContainment,
            DominantArea = AreaRuntime,
            Severity = critical ? OperationalGuidanceSeverity.Critical : OperationalGuidanceSeverity.High,
            StabilizationObjective = "Contain runtime survivability degradation before downstream reassessment",
            RecommendedSequence = new[]
            {
                "Review runtime protection transitions",
                "Evaluate survivability degradation",
                "Monitor downstream replay propagation",
                "Validate stabilization direction"
            },
            EstimatedOperationalImpact = "Escalation containment and pressure stabilization",
            RecoveryAlignment = recovery.Summary,
            OperationalConfidence = MapRecoveryConfidence(recovery.OverallConfidence),
            OperatorSummary = "Runtime containment before replay reassessment reduces downstream spread"
        };
    }

    private static OperationalPlaybookDto CreateInventoryDriftPlaybook(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom)
    {
        return new OperationalPlaybookDto
        {
            PlaybookId = PlaybookInventoryDrift,
            Title = "Inventory drift stabilization",
            ScenarioType = OperationalPlaybookScenarioType.InventoryDriftStabilization,
            DominantArea = AreaInventory,
            Severity = OperationalGuidanceSeverity.Elevated,
            StabilizationObjective = "Collapse drift hotspots and align reconciliation recovery",
            RecommendedSequence = new[]
            {
                "Review drift concentration",
                "Validate reconciliation recovery alignment",
                "Monitor volatility collapse",
                "Reassess recovery posture"
            },
            EstimatedOperationalImpact = "Reconciliation stabilization and operational confidence improvement",
            RecoveryAlignment = recovery.Summary,
            OperationalConfidence = MapRecoveryConfidence(recovery.OverallConfidence),
            OperatorSummary = "Inventory drift stabilization before broad reconciliation changes"
        };
    }

    private static OperationalPlaybookDto CreateReconciliationRecoveryPlaybook(
        OperationalRecoveryPostureDto recovery,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom)
    {
        return new OperationalPlaybookDto
        {
            PlaybookId = PlaybookReconciliationRecovery,
            Title = "Reconciliation recovery playbook",
            ScenarioType = OperationalPlaybookScenarioType.ReconciliationRecovery,
            DominantArea = AreaReconciliation,
            Severity = OperationalGuidanceSeverity.Elevated,
            StabilizationObjective = "Reduce reconciliation queue pressure and validate replay alignment",
            RecommendedSequence = new[]
            {
                "Review reconciliation queue escalation",
                "Validate replay pressure alignment",
                "Monitor cross-domain propagation",
                "Confirm recovery convergence"
            },
            EstimatedOperationalImpact = "Queue pressure reduction and replay recovery alignment",
            RecoveryAlignment = recovery.Summary,
            OperationalConfidence = MapRecoveryConfidence(recovery.OverallConfidence),
            OperatorSummary = "Reconciliation validation after upstream replay or inventory stabilization"
        };
    }

    private static List<OperationalResponseStepDto> ComposeResponseSteps(
        IReadOnlyList<OperationalPlaybookDto> playbooks,
        OperationalPropagationAnalysisDto propagation,
        OperationalRecoveryPostureDto recovery,
        OperationalTriageQueueDto triage)
    {
        var items = new List<(int Priority, OperationalResponseStepDto Step)>();
        var order = 1;

        foreach (var playbook in playbooks.OrderBy(p => p.Severity, Comparer<OperationalGuidanceSeverity>.Create((a, b) => b.CompareTo(a))))
        {
            foreach (var sequenceItem in playbook.RecommendedSequence.Take(MaxSequenceSteps))
            {
                if (order > MaxResponseSteps)
                    break;

                var area = ResolveAreaFromSequence(sequenceItem, playbook.DominantArea);
                var escalating = propagation.Propagations.Any(p =>
                    p.IsEscalating
                    && (string.Equals(p.SourceArea, area, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(p.TargetArea, area, StringComparison.OrdinalIgnoreCase)));

                items.Add((order, new OperationalResponseStepDto
                {
                    SequenceOrder = order,
                    Area = area,
                    Objective = playbook.StabilizationObjective,
                    RecommendedFocus = sequenceItem,
                    ExpectedOutcome = playbook.EstimatedOperationalImpact,
                    EscalationRisk = escalating ? OperationalGuidanceSeverity.High : OperationalGuidanceSeverity.Elevated,
                    StabilizationContribution = $"Supports {playbook.Title.ToLowerInvariant()}",
                    OperatorInstruction = $"Step {order}: {sequenceItem}"
                }));

                order++;
            }
        }

        if (items.Count == 0 && triage.Items.Count > 0)
        {
            var top = triage.Items.OrderBy(i => i.Priority).First();
            items.Add((1, new OperationalResponseStepDto
            {
                SequenceOrder = 1,
                Area = AreaOperational,
                Objective = "Address highest triage priority",
                RecommendedFocus = top.Summary,
                ExpectedOutcome = top.SuggestedOperatorAction,
                EscalationRisk = OperationalGuidanceSeverity.Elevated,
                StabilizationContribution = "Aligns response with triage queue",
                OperatorInstruction = top.SuggestedOperatorAction
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Step.SequenceOrder)
            .Take(MaxResponseSteps)
            .Select(i => i.Step)
            .ToList();
    }

    private static string ResolveAreaFromSequence(string sequenceItem, string defaultArea)
    {
        foreach (var area in new[] { AreaReplay, AreaRuntime, AreaInventory, AreaReconciliation, AreaOperational })
        {
            if (sequenceItem.Contains(area, StringComparison.OrdinalIgnoreCase))
                return area;
        }

        return defaultArea;
    }

    private static List<OperationalEscalationGuidanceDto> ComposeEscalationGuidance(
        OperationalPropagationAnalysisDto propagation,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationScenariosDto simulation)
    {
        var items = new List<(int Priority, OperationalEscalationGuidanceDto Guidance)>();

        if (propagation.Propagations.Any(p => p.IsEscalating))
        {
            var top = propagation.Propagations
                .Where(p => p.IsEscalating)
                .OrderBy(p => p.SourceArea, StringComparer.Ordinal)
                .First();

            items.Add((1, new OperationalEscalationGuidanceDto
            {
                EscalationType = OperationalEscalationType.PropagationEscalation,
                Severity = OperationalGuidanceSeverity.High,
                TriggerCondition = top.OperatorInterpretation,
                RecommendedOperatorFocus = $"Contain escalation from {top.SourceArea.ToLowerInvariant()} before downstream validation",
                ContainmentPriority = OperationalStabilizationPriority.Immediate,
                RecoveryPriority = OperationalStabilizationPriority.High,
                OperatorInterpretation = "Upstream stabilization before downstream validation"
            }));
        }

        if (situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.High)
        {
            items.Add((2, new OperationalEscalationGuidanceDto
            {
                EscalationType = OperationalEscalationType.RuntimeSurvivability,
                Severity = OperationalGuidanceSeverity.Critical,
                TriggerCondition = "Runtime survivability degradation or protective mode active",
                RecommendedOperatorFocus = "Runtime protection review and survivability monitoring",
                ContainmentPriority = OperationalStabilizationPriority.Immediate,
                RecoveryPriority = OperationalStabilizationPriority.High,
                OperatorInterpretation = "Runtime containment before replay reassessment"
            }));
        }

        if (incidentSummary.EscalatingIncidentCount >= 1)
        {
            items.Add((3, new OperationalEscalationGuidanceDto
            {
                EscalationType = OperationalEscalationType.IncidentEscalation,
                Severity = MapIncidentSeverity(incidentSummary.HighestSeverity),
                TriggerCondition = $"{incidentSummary.EscalatingIncidentCount} escalating incident case(s) active",
                RecommendedOperatorFocus = "Incident continuity review aligned with triage priorities",
                ContainmentPriority = OperationalStabilizationPriority.High,
                RecoveryPriority = OperationalStabilizationPriority.Elevated,
                OperatorInterpretation = "Incident escalation may amplify cross-domain pressure"
            }));
        }

        if (recovery.OverallDirection is OperationalRecoveryDirection.Degrading or OperationalRecoveryDirection.Diverging)
        {
            items.Add((4, new OperationalEscalationGuidanceDto
            {
                EscalationType = OperationalEscalationType.RecoveryDivergence,
                Severity = OperationalGuidanceSeverity.High,
                TriggerCondition = "Recovery posture diverging across operational domains",
                RecommendedOperatorFocus = situationRoom.RecommendedOperationalFocus,
                ContainmentPriority = OperationalStabilizationPriority.High,
                RecoveryPriority = OperationalStabilizationPriority.Immediate,
                OperatorInterpretation = "Address dominant constraint before broad downstream changes"
            }));
        }

        if (simulation.DegradationPaths.Any(p => p.OperationalSeverity >= OperationalSimulationSeverity.High))
        {
            items.Add((5, new OperationalEscalationGuidanceDto
            {
                EscalationType = OperationalEscalationType.OperationalVolatility,
                Severity = OperationalGuidanceSeverity.Elevated,
                TriggerCondition = "Hypothetical degradation path indicates elevated spread risk",
                RecommendedOperatorFocus = simulation.DegradationPaths.First().OperatorSummary,
                ContainmentPriority = OperationalStabilizationPriority.Elevated,
                RecoveryPriority = OperationalStabilizationPriority.Elevated,
                OperatorInterpretation = "Monitor degradation path while executing stabilization sequence"
            }));
        }

        if (items.Count == 0)
        {
            items.Add((99, new OperationalEscalationGuidanceDto
            {
                EscalationType = OperationalEscalationType.OperationalVolatility,
                Severity = OperationalGuidanceSeverity.Normal,
                TriggerCondition = "No active escalation guidance required",
                RecommendedOperatorFocus = "Continue routine operational monitoring",
                ContainmentPriority = OperationalStabilizationPriority.Monitoring,
                RecoveryPriority = OperationalStabilizationPriority.Monitoring,
                OperatorInterpretation = "Escalation pressure within normal advisory bounds"
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Guidance.EscalationType)
            .Take(MaxEscalationGuidance)
            .Select(i => i.Guidance)
            .ToList();
    }

    private static OperationalResponseAlignmentDto ComposeResponseAlignment(
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalRecoveryPostureDto recovery,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalSimulationScenariosDto simulation,
        OperationalSituationRoomDto situationRoom)
    {
        var simulationArea = simulation.LeveragePoints
            .OrderByDescending(l => l.LeverageStrength)
            .FirstOrDefault()?.Area
            ?? AreaOperational;

        return new OperationalResponseAlignmentDto
        {
            IncidentAlignment = incidentSummary.ActiveIncidentCount > 0
                ? $"Aligned with {incidentSummary.ActiveIncidentCount} active incident case(s)"
                : "No active incident alignment required",
            RecoveryAlignment = recovery.Summary,
            CausalityAlignment = $"Dominant area {causalitySummary.DominantOperationalArea} informs response sequencing",
            SimulationAlignment = $"Hypothetical leverage focus: {simulationArea.ToLowerInvariant()}",
            SituationRoomAlignment = situationRoom.RecommendedOperationalFocus,
            OperationalConsistency = situationRoom.OperatorSummary
        };
    }

    private static OperationalResponseConfidence MapRecoveryConfidence(OperationalRecoveryConfidence confidence) =>
        confidence switch
        {
            OperationalRecoveryConfidence.High => OperationalResponseConfidence.High,
            OperationalRecoveryConfidence.Elevated => OperationalResponseConfidence.Elevated,
            OperationalRecoveryConfidence.Moderate => OperationalResponseConfidence.Moderate,
            _ => OperationalResponseConfidence.Low
        };

    private static OperationalGuidanceSeverity MapIncidentSeverity(OperationalIncidentSeverity severity) =>
        severity switch
        {
            OperationalIncidentSeverity.Critical => OperationalGuidanceSeverity.Critical,
            OperationalIncidentSeverity.High => OperationalGuidanceSeverity.High,
            OperationalIncidentSeverity.Elevated => OperationalGuidanceSeverity.Elevated,
            _ => OperationalGuidanceSeverity.Normal
        };
}
