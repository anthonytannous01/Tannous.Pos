using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;

using Tannous.Pos.Application.OperationalCognition;

namespace Tannous.Pos.Application.OperationalExperienceGraph;

/// <summary>Deterministic operational experience graph and contextual navigation synthesis.</summary>
public static class OperationalExperienceGraphAggregation
{
    public const int MaxRelationships = 8;
    public const int MaxTraversalPaths = 8;
    public const int MaxRelatedAreas = 8;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;

    public const string SurfaceDashboard = "Dashboard";
    public const string SurfaceTimeline = "Timeline";
    public const string SurfaceTrends = "Trends";
    public const string SurfaceTriage = "Triage";
    public const string SurfaceRecovery = "Recovery";
    public const string SurfaceIncidents = "Incident Cases";
    public const string SurfaceCausality = "Causality";
    public const string SurfaceSimulation = "Simulation";
    public const string SurfacePlaybooks = "Playbooks";
    public const string SurfacePatterns = "Patterns";
    public const string SurfaceIntegrity = "Integrity";
    public const string SurfaceSituationRoom = "Situation Room";
    public const string SurfaceReplayWorkbench = "Replay Workbench";
    public const string SurfaceInventoryWorkbench = "Inventory Workbench";
    public const string SurfaceReconciliationWorkbench = "Reconciliation Workbench";

    public const string AreaReplay = "Replay";
    public const string AreaRuntime = "Runtime";
    public const string AreaInventory = "Inventory";
    public const string AreaReconciliation = "Reconciliation";
    public const string AreaOperational = "Operational Stability";

    public const string PathReplayInvestigation = "path-replay-investigation";
    public const string PathRuntimeContainment = "path-runtime-containment";
    public const string PathRecoveryVerification = "path-recovery-verification";
    public const string PathStabilizationGuidance = "path-stabilization-guidance";
    public const string PathIncidentInvestigation = "path-incident-investigation";

    public static OperationalExperienceGraphDto ComposeExperienceGraph(
        OperationalTrendSummaryDto trend,
        OperationalTimelineDto timeline,
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
        IReadOnlyList<OperationalExperienceSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var dominantContext = ResolveDominantContext(
            causalitySummary,
            incidentSummary,
            recovery,
            situationRoom,
            patternSummary);

        var relationships = ComposeRelationships(
            dominantContext,
            causalitySummary,
            recovery,
            incidentSummary,
            propagation,
            situationRoom,
            simulationSummary,
            playbooks,
            patternSummary,
            integrityReport);

        var traversalPaths = ComposeTraversalPaths(
            dominantContext,
            causalitySummary,
            recovery,
            incidentSummary,
            situationRoom,
            simulationSummary,
            playbooks,
            patternSummary,
            integrityReport,
            triage);

        var investigationContinuity = ComposeInvestigationContinuity(
            dominantContext,
            causalitySummary,
            recovery,
            incidentSummary,
            situationRoom,
            playbooks,
            patternSummary,
            integrityReport,
            traversalPaths);

        var experienceSummary = ComposeExperienceSummary(
            dominantContext,
            relationships,
            traversalPaths,
            recovery,
            situationRoom,
            triage);

        var experienceState = ResolveExperienceState(relationships, integrityReport, triage);
        var entryPoint = ResolveRecommendedEntryPoint(dominantContext, triage, situationRoom);
        var traversalPathSummary = traversalPaths.FirstOrDefault()?.OperatorSummary
            ?? "Review dashboard for overall operational context";

        var operatorSummary = ComposeOperatorSummary(
            dominantContext,
            experienceState,
            relationships.Count,
            entryPoint,
            priorSnapshots);

        return new OperationalExperienceGraphDto
        {
            GeneratedAtUtc = generatedAtUtc,
            DominantOperationalContext = dominantContext,
            ExperienceState = experienceState,
            ActiveRelationshipCount = relationships.Count,
            RecommendedEntryPoint = entryPoint,
            RecommendedTraversalPath = traversalPathSummary,
            Relationships = relationships,
            InvestigationContinuity = investigationContinuity,
            ExperienceSummary = experienceSummary,
            OperatorSummary = operatorSummary
        };
    }

    public static OperationalExperienceTraversalPathsDto ComposeTraversalPathsResponse(
        OperationalTrendSummaryDto trend,
        OperationalTimelineDto timeline,
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
        DateTime generatedAtUtc)
    {
        var dominantContext = ResolveDominantContext(
            causalitySummary,
            incidentSummary,
            recovery,
            situationRoom,
            patternSummary);

        var paths = ComposeTraversalPaths(
            dominantContext,
            causalitySummary,
            recovery,
            incidentSummary,
            situationRoom,
            simulationSummary,
            playbooks,
            patternSummary,
            integrityReport,
            triage);

        return new OperationalExperienceTraversalPathsDto
        {
            GeneratedAtUtc = generatedAtUtc,
            PathCount = paths.Count,
            TraversalPaths = paths
        };
    }

    public static OperationalContextualNavigationDto ComposeContextualNavigation(
        OperationalTrendSummaryDto trend,
        OperationalTimelineDto timeline,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalPatternSummaryDto patternSummary,
        OperationalIntegrityReportDto integrityReport,
        DateTime generatedAtUtc)
    {
        var dominantContext = ResolveDominantContext(
            causalitySummary,
            incidentSummary,
            recovery,
            situationRoom,
            patternSummary);

        var dominantArea = NormalizeArea(causalitySummary.DominantOperationalArea);
        var currentFocus = ResolveCurrentFocus(dominantContext, dominantArea, situationRoom, triage);
        var nextSurface = ResolveRecommendedNextSurface(dominantContext, triage, situationRoom, integrityReport);
        var relatedAreas = ComposeRelatedAreas(
            causalitySummary,
            simulationSummary,
            playbooks,
            patternSummary,
            recovery);

        var priority = ResolveNavigationPriority(triage, situationRoom, integrityReport);
        var strength = ResolveNavigationStrength(integrityReport, triage);
        var reason = ComposeDominantReason(dominantContext, dominantArea, situationRoom);
        var interpretation = ComposeNavigationInterpretation(
            currentFocus,
            nextSurface,
            dominantContext,
            integrityReport);

        return new OperationalContextualNavigationDto
        {
            GeneratedAtUtc = generatedAtUtc,
            CurrentOperationalFocus = currentFocus,
            RecommendedNextSurface = nextSurface,
            RelatedOperationalAreas = relatedAreas,
            DominantReason = reason,
            InvestigationPriority = priority,
            NavigationStrength = strength,
            OperatorInterpretation = interpretation
        };
    }

    public static OperationalExperienceSnapshot CreateSnapshot(OperationalExperienceGraphDto graph)
    {
        return new OperationalExperienceSnapshot
        {
            GeneratedAtUtc = graph.GeneratedAtUtc,
            DominantOperationalContext = graph.DominantOperationalContext,
            ExperienceState = graph.ExperienceState,
            ActiveRelationshipCount = graph.ActiveRelationshipCount,
            RecommendedEntryPoint = graph.RecommendedEntryPoint,
            DominantOperationalFlow = graph.ExperienceSummary.DominantOperationalFlow
        };
    }

    private static IReadOnlyList<OperationalRelationshipDto> ComposeRelationships(
        OperationalContextType dominantContext,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalPatternSummaryDto patternSummary,
        OperationalIntegrityReportDto integrityReport)
    {
        var relationships = new List<OperationalRelationshipDto>();
        var dominantArea = NormalizeArea(causalitySummary.DominantOperationalArea);

        if (dominantContext is OperationalContextType.ReplayInstability
            or OperationalContextType.IncidentInvestigation)
        {
            AddRelationship(
                relationships,
                SurfaceTimeline,
                SurfaceCausality,
                OperationalRelationshipType.CausalExplanation,
                dominantContext,
                OperationalNavigationStrength.Strong,
                "Timeline events provide upstream context for causal interpretation",
                "Start from timeline chronology, then open causality to trace replay instability continuity");

            AddRelationship(
                relationships,
                SurfaceCausality,
                SurfaceSimulation,
                OperationalRelationshipType.InvestigationContinuity,
                dominantContext,
                OperationalNavigationStrength.Strong,
                "Causal dominant area aligns with simulation leverage interpretation",
                "Use causality to identify upstream source, then simulation for hypothetical stabilization leverage");

            AddRelationship(
                relationships,
                SurfaceSimulation,
                SurfacePlaybooks,
                OperationalRelationshipType.StabilizationFlow,
                dominantContext,
                OperationalNavigationStrength.Strong,
                "Simulation leverage points inform playbook stabilization sequencing",
                "Review simulation leverage before following playbook stabilization guidance");

            AddRelationship(
                relationships,
                SurfacePlaybooks,
                SurfaceIntegrity,
                OperationalRelationshipType.NarrativeContinuity,
                dominantContext,
                OperationalNavigationStrength.Moderate,
                "Playbook guidance should align with cross-layer integrity interpretation",
                "Validate playbook recommendations against integrity consistency before acting");
        }

        if (dominantContext == OperationalContextType.RuntimeContainment)
        {
            AddRelationship(
                relationships,
                SurfaceIncidents,
                SurfaceRecovery,
                OperationalRelationshipType.RecoveryAlignment,
                dominantContext,
                OperationalNavigationStrength.Strong,
                "Incident containment pressure connects to recovery posture movement",
                "Review active incident cases alongside recovery posture for containment context");

            AddRelationship(
                relationships,
                SurfaceRecovery,
                SurfaceSituationRoom,
                OperationalRelationshipType.EscalationContext,
                dominantContext,
                OperationalNavigationStrength.Strong,
                "Recovery movement informs situation room escalation interpretation",
                "Compare recovery posture with situation room executive briefing");

            AddRelationship(
                relationships,
                SurfaceSituationRoom,
                SurfacePlaybooks,
                OperationalRelationshipType.StabilizationFlow,
                dominantContext,
                OperationalNavigationStrength.Strong,
                "Situation room priority focus guides playbook stabilization sequencing",
                "Use situation room briefing to prioritize playbook stabilization steps");

            AddRelationship(
                relationships,
                SurfacePlaybooks,
                SurfacePatterns,
                OperationalRelationshipType.NarrativeContinuity,
                dominantContext,
                OperationalNavigationStrength.Moderate,
                "Stabilization guidance connects to recurring runtime pattern recognition",
                "Cross-check playbook guidance with pattern archetypes for runtime containment");
        }

        if (dominantContext == OperationalContextType.RecoveryVerification)
        {
            AddRelationship(
                relationships,
                SurfaceRecovery,
                SurfaceIntegrity,
                OperationalRelationshipType.RecoveryAlignment,
                dominantContext,
                OperationalNavigationStrength.Strong,
                "Recovery posture should remain consistent with cross-layer integrity",
                "Verify recovery movement against integrity consistency before trusting stabilization");

            AddRelationship(
                relationships,
                SurfaceIntegrity,
                SurfacePatterns,
                OperationalRelationshipType.NarrativeContinuity,
                dominantContext,
                OperationalNavigationStrength.Moderate,
                "Integrity narrative aligns with recurring operational pattern context",
                "Use integrity narrative consistency to validate pattern interpretation");

            AddRelationship(
                relationships,
                SurfacePatterns,
                SurfaceSimulation,
                OperationalRelationshipType.InvestigationContinuity,
                dominantContext,
                OperationalNavigationStrength.Moderate,
                "Pattern recognition informs hypothetical stabilization simulation",
                "Review patterns before simulation to understand recurring stabilization leverage");
        }

        AddRelationship(
            relationships,
            SurfaceTriage,
            SurfaceDashboard,
            OperationalRelationshipType.InvestigationContinuity,
            OperationalContextType.OperationalOverview,
            OperationalNavigationStrength.Contextual,
            "Triage priorities connect to dashboard health overview",
            "Use triage queue to prioritize which dashboard sections need immediate attention");

        AddRelationship(
            relationships,
            SurfaceTrends,
            SurfaceTimeline,
            OperationalRelationshipType.CausalExplanation,
            OperationalContextType.OperationalOverview,
            OperationalNavigationStrength.Contextual,
            "Trend movement provides short-window context for timeline events",
            "Compare trend deltas with timeline chronology for movement continuity");

        if (AreasMatch(dominantArea, AreaReplay))
        {
            AddRelationship(
                relationships,
                SurfaceReplayWorkbench,
                SurfaceCausality,
                OperationalRelationshipType.CausalExplanation,
                OperationalContextType.ReplayInstability,
                OperationalNavigationStrength.Strong,
                "Replay workbench pressure connects to causality upstream interpretation",
                "Open replay workbench pressure before causality when replay is the dominant area");
        }

        if (integrityReport.ContradictionCount > 0)
        {
            AddRelationship(
                relationships,
                SurfaceIntegrity,
                SurfaceSituationRoom,
                OperationalRelationshipType.NarrativeContinuity,
                OperationalContextType.StabilizationGuidance,
                OperationalNavigationStrength.Strong,
                "Integrity contradictions require situation room narrative reconciliation",
                "When contradictions exist, reconcile situation room narrative with integrity findings first");
        }

        if (incidentSummary.EscalatingIncidentCount > 0)
        {
            AddRelationship(
                relationships,
                SurfaceIncidents,
                SurfaceTriage,
                OperationalRelationshipType.InvestigationContinuity,
                OperationalContextType.IncidentInvestigation,
                OperationalNavigationStrength.Strong,
                "Escalating incidents connect to triage investigation priorities",
                "Follow triage queue after reviewing escalating incident cases");
        }

        return relationships
            .OrderByDescending(r => r.RelevanceStrength)
            .ThenBy(r => r.SourceSurface, StringComparer.Ordinal)
            .ThenBy(r => r.TargetSurface, StringComparer.Ordinal)
            .Take(MaxRelationships)
            .ToList();
    }

    private static IReadOnlyList<OperationalTraversalPathDto> ComposeTraversalPaths(
        OperationalContextType dominantContext,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalPatternSummaryDto patternSummary,
        OperationalIntegrityReportDto integrityReport,
        OperationalTriageQueueDto triage)
    {
        var paths = new List<OperationalTraversalPathDto>();
        var dominantArea = NormalizeArea(causalitySummary.DominantOperationalArea);

        paths.Add(new OperationalTraversalPathDto
        {
            PathId = PathReplayInvestigation,
            StartingSurface = SurfaceTimeline,
            RecommendedSequence = new[]
            {
                SurfaceTimeline,
                SurfaceCausality,
                SurfaceSimulation,
                SurfacePlaybooks,
                SurfaceIntegrity
            },
            DominantOperationalFocus = AreaReplay,
            ExpectedOperatorOutcome = "Trace replay instability from chronology through stabilization verification",
            TraversalPriority = AreasMatch(dominantArea, AreaReplay)
                ? OperationalTraversalPriority.Immediate
                : OperationalTraversalPriority.Contextual,
            OperatorSummary = "Replay investigation flow: timeline → causality → simulation → playbooks → integrity"
        });

        paths.Add(new OperationalTraversalPathDto
        {
            PathId = PathRuntimeContainment,
            StartingSurface = SurfaceIncidents,
            RecommendedSequence = new[]
            {
                SurfaceIncidents,
                SurfaceRecovery,
                SurfaceSituationRoom,
                SurfacePlaybooks,
                SurfacePatterns
            },
            DominantOperationalFocus = AreaRuntime,
            ExpectedOperatorOutcome = "Understand runtime containment pressure and stabilization guidance continuity",
            TraversalPriority = dominantContext == OperationalContextType.RuntimeContainment
                ? OperationalTraversalPriority.Immediate
                : OperationalTraversalPriority.High,
            OperatorSummary = "Runtime containment flow: incidents → recovery → situation room → playbooks → patterns"
        });

        paths.Add(new OperationalTraversalPathDto
        {
            PathId = PathRecoveryVerification,
            StartingSurface = SurfaceRecovery,
            RecommendedSequence = new[]
            {
                SurfaceRecovery,
                SurfaceIntegrity,
                SurfacePatterns,
                SurfaceSimulation
            },
            DominantOperationalFocus = dominantArea,
            ExpectedOperatorOutcome = "Verify recovery movement against cross-layer operational consistency",
            TraversalPriority = dominantContext == OperationalContextType.RecoveryVerification
                ? OperationalTraversalPriority.High
                : OperationalTraversalPriority.Normal,
            OperatorSummary = "Recovery verification flow: recovery → integrity → patterns → simulation"
        });

        paths.Add(new OperationalTraversalPathDto
        {
            PathId = PathStabilizationGuidance,
            StartingSurface = SurfaceTriage,
            RecommendedSequence = new[]
            {
                SurfaceTriage,
                SurfacePlaybooks,
                SurfaceSimulation,
                SurfaceIntegrity
            },
            DominantOperationalFocus = situationRoom.HighestPriorityFocus,
            ExpectedOperatorOutcome = "Follow prioritized stabilization guidance with consistency verification",
            TraversalPriority = triage.Items.Count > 0
                ? OperationalTraversalPriority.High
                : OperationalTraversalPriority.Normal,
            OperatorSummary = "Stabilization guidance flow: triage → playbooks → simulation → integrity"
        });

        paths.Add(new OperationalTraversalPathDto
        {
            PathId = PathIncidentInvestigation,
            StartingSurface = SurfaceDashboard,
            RecommendedSequence = new[]
            {
                SurfaceDashboard,
                SurfaceTriage,
                SurfaceIncidents,
                SurfaceCausality,
                SurfaceSituationRoom
            },
            DominantOperationalFocus = dominantArea,
            ExpectedOperatorOutcome = "Investigate active operational pressure from overview to executive context",
            TraversalPriority = incidentSummary.ActiveIncidentCount > 0
                ? OperationalTraversalPriority.High
                : OperationalTraversalPriority.Contextual,
            OperatorSummary = "Incident investigation flow: dashboard → triage → incidents → causality → situation room"
        });

        if (AreasMatch(dominantArea, AreaReplay))
        {
            paths.Add(new OperationalTraversalPathDto
            {
                PathId = "path-replay-workbench-pressure",
                StartingSurface = SurfaceReplayWorkbench,
                RecommendedSequence = new[]
                {
                    SurfaceReplayWorkbench,
                    SurfaceTimeline,
                    SurfaceCausality,
                    SurfacePlaybooks
                },
                DominantOperationalFocus = AreaReplay,
                ExpectedOperatorOutcome = "Connect replay workbench pressure to causal and stabilization guidance",
                TraversalPriority = OperationalTraversalPriority.Immediate,
                OperatorSummary = "Replay workbench flow: replay workbench → timeline → causality → playbooks"
            });
        }

        if (integrityReport.ContradictionCount >= 2)
        {
            paths.Add(new OperationalTraversalPathDto
            {
                PathId = "path-integrity-reconciliation",
                StartingSurface = SurfaceIntegrity,
                RecommendedSequence = new[]
                {
                    SurfaceIntegrity,
                    SurfaceSituationRoom,
                    SurfaceRecovery,
                    SurfaceSimulation
                },
                DominantOperationalFocus = dominantArea,
                ExpectedOperatorOutcome = "Reconcile contradictory operational interpretations before stabilization",
                TraversalPriority = OperationalTraversalPriority.Immediate,
                OperatorSummary = "Integrity reconciliation flow: integrity → situation room → recovery → simulation"
            });
        }

        return paths
            .OrderBy(p => p.TraversalPriority)
            .ThenBy(p => p.PathId, StringComparer.Ordinal)
            .Take(MaxTraversalPaths)
            .ToList();
    }

    private static OperationalInvestigationContinuityDto ComposeInvestigationContinuity(
        OperationalContextType dominantContext,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalSituationRoomDto situationRoom,
        OperationalPlaybooksDto playbooks,
        OperationalPatternSummaryDto patternSummary,
        OperationalIntegrityReportDto integrityReport,
        IReadOnlyList<OperationalTraversalPathDto> traversalPaths)
    {
        var dominantArea = NormalizeArea(causalitySummary.DominantOperationalArea);
        var theme = dominantContext switch
        {
            OperationalContextType.ReplayInstability => "Replay instability investigation continuity",
            OperationalContextType.RuntimeContainment => "Runtime containment investigation continuity",
            OperationalContextType.RecoveryVerification => "Recovery verification continuity",
            OperationalContextType.IncidentInvestigation => "Active incident investigation continuity",
            OperationalContextType.StabilizationGuidance => "Stabilization guidance continuity",
            _ => "Operational overview continuity"
        };

        var primaryPath = traversalPaths.FirstOrDefault();
        var relatedSurfaces = primaryPath?.RecommendedSequence.ToList() ?? new List<string>();
        if (relatedSurfaces.Count == 0)
            relatedSurfaces.Add(SurfaceDashboard);

        var escalationAlignment = situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.Elevated
            ? "Escalation context active across situation room and triage surfaces"
            : "Escalation context remains within normal navigation bounds";

        var recoveryAlignment = recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            ? "Recovery navigation aligns with improving or converging posture"
            : "Recovery navigation should prioritize upstream instability surfaces first";

        var consistency = integrityReport.OverallIntegrityState switch
        {
            OperationalIntegrityState.Coherent => "Cross-layer operational interpretations remain coherent for traversal",
            OperationalIntegrityState.MostlyCoherent => "Most operational surfaces align; minor reconciliation may be needed",
            OperationalIntegrityState.Fragmented => "Navigation may require manual reconciliation across fragmented interpretations",
            _ => "Contradictory interpretations require integrity review before trusting traversal paths"
        };

        return new OperationalInvestigationContinuityDto
        {
            InvestigationTheme = theme,
            RelatedSurfaces = relatedSurfaces.Take(MaxRelatedAreas).ToList(),
            DominantArea = dominantArea,
            EscalationAlignment = escalationAlignment,
            RecoveryAlignment = recoveryAlignment,
            RecommendedOperatorFlow = primaryPath?.OperatorSummary ?? "Start from dashboard for operational overview",
            OperationalConsistency = consistency
        };
    }

    private static OperationalExperienceSummaryDto ComposeExperienceSummary(
        OperationalContextType dominantContext,
        IReadOnlyList<OperationalRelationshipDto> relationships,
        IReadOnlyList<OperationalTraversalPathDto> traversalPaths,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalTriageQueueDto triage)
    {
        var dominantFlow = dominantContext switch
        {
            OperationalContextType.ReplayInstability => "Replay investigation traversal",
            OperationalContextType.RuntimeContainment => "Runtime containment traversal",
            OperationalContextType.RecoveryVerification => "Recovery verification traversal",
            OperationalContextType.IncidentInvestigation => "Incident investigation traversal",
            OperationalContextType.StabilizationGuidance => "Stabilization guidance traversal",
            _ => "Operational overview traversal"
        };

        var mostConnected = relationships
            .SelectMany(r => new[] { r.SourceSurface, r.TargetSurface })
            .GroupBy(s => s, StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .FirstOrDefault()?.Key
            ?? SurfaceDashboard;

        var highestPriority = traversalPaths
            .OrderBy(p => p.TraversalPriority)
            .FirstOrDefault()?.PathId
            ?? PathIncidentInvestigation;

        var recoveryNav = recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            ? "Recovery surfaces should follow verification paths after stabilization"
            : "Recovery surfaces should precede simulation and playbook traversal";

        var escalationNav = situationRoom.StabilizationDirection == OperationalSituationDirection.Escalating
            ? "Prioritize triage and situation room before downstream stabilization surfaces"
            : "Escalation navigation remains secondary to dominant operational focus";

        var attention = triage.Items.Count >= 4
            ? "Elevated"
            : situationRoom.AttentionLevel.ToString();

        return new OperationalExperienceSummaryDto
        {
            DominantOperationalFlow = dominantFlow,
            MostConnectedSurface = mostConnected,
            HighestPriorityTraversal = highestPriority,
            RecoveryNavigationAlignment = recoveryNav,
            EscalationNavigationAlignment = escalationNav,
            OperatorAttentionLevel = attention
        };
    }

    private static OperationalContextType ResolveDominantContext(
        OperationalCausalitySummaryDto causalitySummary,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalPatternSummaryDto patternSummary)
    {
        var dominantArea = NormalizeArea(causalitySummary.DominantOperationalArea);

        if (AreasMatch(dominantArea, AreaReplay))
            return OperationalContextType.ReplayInstability;

        if (AreasMatch(dominantArea, AreaRuntime)
            || patternSummary.DominantArchetype.Contains("runtime", StringComparison.OrdinalIgnoreCase))
            return OperationalContextType.RuntimeContainment;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            && incidentSummary.EscalatingIncidentCount == 0)
            return OperationalContextType.RecoveryVerification;

        if (incidentSummary.ActiveIncidentCount > 0 || incidentSummary.EscalatingIncidentCount > 0)
            return OperationalContextType.IncidentInvestigation;

        if (situationRoom.StabilizationDirection is OperationalSituationDirection.Degrading
                or OperationalSituationDirection.Escalating)
            return OperationalContextType.StabilizationGuidance;

        return OperationalContextType.OperationalOverview;
    }

    private static OperationalExperienceState ResolveExperienceState(
        IReadOnlyList<OperationalRelationshipDto> relationships,
        OperationalIntegrityReportDto integrityReport,
        OperationalTriageQueueDto triage)
    {
        if (integrityReport.OverallIntegrityState == OperationalIntegrityState.Contradictory)
            return OperationalExperienceState.Fragmented;

        if (triage.Items.Count >= 3)
            return OperationalExperienceState.InvestigationFocused;

        if (relationships.Any(r => r.RelationshipType == OperationalRelationshipType.StabilizationFlow))
            return OperationalExperienceState.StabilizationFocused;

        return OperationalExperienceState.Coherent;
    }

    private static string ResolveRecommendedEntryPoint(
        OperationalContextType dominantContext,
        OperationalTriageQueueDto triage,
        OperationalSituationRoomDto situationRoom)
    {
        if (triage.Items.Count > 0)
            return SurfaceTriage;

        return dominantContext switch
        {
            OperationalContextType.ReplayInstability => SurfaceTimeline,
            OperationalContextType.RuntimeContainment => SurfaceIncidents,
            OperationalContextType.RecoveryVerification => SurfaceRecovery,
            OperationalContextType.IncidentInvestigation => SurfaceDashboard,
            OperationalContextType.StabilizationGuidance => SurfaceSituationRoom,
            _ => situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.Elevated
                ? SurfaceSituationRoom
                : SurfaceDashboard
        };
    }

    private static string ResolveCurrentFocus(
        OperationalContextType dominantContext,
        string dominantArea,
        OperationalSituationRoomDto situationRoom,
        OperationalTriageQueueDto triage)
    {
        if (triage.Items.Count > 0)
            return $"{dominantArea} with active triage investigation priorities";

        return $"{dominantArea} — {dominantContext.ToString().ToLowerInvariant()} context";
    }

    private static string ResolveRecommendedNextSurface(
        OperationalContextType dominantContext,
        OperationalTriageQueueDto triage,
        OperationalSituationRoomDto situationRoom,
        OperationalIntegrityReportDto integrityReport)
    {
        if (integrityReport.ContradictionCount >= 2)
            return SurfaceIntegrity;

        if (triage.Items.Count > 0)
            return triage.Items[0].RecommendedRoute.Contains("incident", StringComparison.OrdinalIgnoreCase)
                ? SurfaceIncidents
                : SurfaceCausality;

        return dominantContext switch
        {
            OperationalContextType.ReplayInstability => SurfaceCausality,
            OperationalContextType.RuntimeContainment => SurfaceRecovery,
            OperationalContextType.RecoveryVerification => SurfaceIntegrity,
            OperationalContextType.IncidentInvestigation => SurfaceIncidents,
            OperationalContextType.StabilizationGuidance => SurfacePlaybooks,
            _ => situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.Elevated
                ? SurfaceSituationRoom
                : SurfaceDashboard
        };
    }

    private static IReadOnlyList<string> ComposeRelatedAreas(
        OperationalCausalitySummaryDto causalitySummary,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalPatternSummaryDto patternSummary,
        OperationalRecoveryPostureDto recovery)
    {
        var areas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            NormalizeArea(causalitySummary.DominantOperationalArea),
            NormalizeArea(simulationSummary.HighestLeverageArea)
        };

        foreach (var playbook in playbooks.Playbooks.Take(3))
            areas.Add(NormalizeArea(playbook.DominantArea));

        if (!string.IsNullOrWhiteSpace(patternSummary.DominantArchetype))
            areas.Add(patternSummary.DominantArchetype);

        if (recovery.OverallDirection is OperationalRecoveryDirection.Degrading
            or OperationalRecoveryDirection.Diverging)
            areas.Add(AreaOperational);

        return areas
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .OrderBy(a => a, StringComparer.Ordinal)
            .Take(MaxRelatedAreas)
            .ToList();
    }

    private static OperationalTraversalPriority ResolveNavigationPriority(
        OperationalTriageQueueDto triage,
        OperationalSituationRoomDto situationRoom,
        OperationalIntegrityReportDto integrityReport)
    {
        if (integrityReport.ContradictionCount >= 2
            || situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.High)
            return OperationalTraversalPriority.Immediate;

        if (triage.Items.Count > 0)
            return OperationalTraversalPriority.High;

        return OperationalTraversalPriority.Normal;
    }

    private static OperationalNavigationStrength ResolveNavigationStrength(
        OperationalIntegrityReportDto integrityReport,
        OperationalTriageQueueDto triage)
    {
        if (integrityReport.OverallIntegrityState == OperationalIntegrityState.Coherent && triage.Items.Count > 0)
            return OperationalNavigationStrength.Strong;

        if (integrityReport.OverallIntegrityState == OperationalIntegrityState.MostlyCoherent)
            return OperationalNavigationStrength.Moderate;

        if (integrityReport.ContradictionCount > 0)
            return OperationalNavigationStrength.Weak;

        return OperationalNavigationStrength.Contextual;
    }

    private static string ComposeDominantReason(
        OperationalContextType dominantContext,
        string dominantArea,
        OperationalSituationRoomDto situationRoom)
    {
        return dominantContext switch
        {
            OperationalContextType.ReplayInstability =>
                $"Replay instability in {dominantArea.ToLowerInvariant()} requires upstream causal traversal",
            OperationalContextType.RuntimeContainment =>
                "Runtime containment pressure connects incident, recovery, and stabilization surfaces",
            OperationalContextType.RecoveryVerification =>
                "Recovery movement should be verified across integrity and pattern surfaces",
            OperationalContextType.IncidentInvestigation =>
                "Active incident pressure prioritizes triage-led investigation traversal",
            OperationalContextType.StabilizationGuidance =>
                $"Situation room focus: {situationRoom.HighestPriorityFocus.ToLowerInvariant()}",
            _ => "Operational overview provides the safest entry for contextual navigation"
        };
    }

    private static string ComposeNavigationInterpretation(
        string currentFocus,
        string nextSurface,
        OperationalContextType dominantContext,
        OperationalIntegrityReportDto integrityReport)
    {
        var integrityNote = integrityReport.ContradictionCount > 0
            ? " Integrity contradictions exist — reconcile before following stabilization paths."
            : string.Empty;

        return
            $"Current focus is {currentFocus.ToLowerInvariant()}. " +
            $"Recommended next surface: {nextSurface}. " +
            $"Dominant context: {dominantContext.ToString().ToLowerInvariant()}.{integrityNote}";
    }

    private static string ComposeOperatorSummary(
        OperationalContextType dominantContext,
        OperationalExperienceState experienceState,
        int relationshipCount,
        string entryPoint,
        IReadOnlyList<OperationalExperienceSnapshot> priorSnapshots)
    {
        var continuity = string.Empty;
        if (priorSnapshots.Count > 0)
        {
            var prior = priorSnapshots[^1];
            if (prior.DominantOperationalContext != dominantContext)
            {
                continuity =
                    $" Context shifted from {prior.DominantOperationalContext.ToString().ToLowerInvariant()} " +
                    $"to {dominantContext.ToString().ToLowerInvariant()}.";
            }
        }

        return
            $"Operational experience graph is {experienceState.ToString().ToLowerInvariant()} " +
            $"with {relationshipCount} active relationship(s). " +
            $"Recommended entry point: {entryPoint}.{continuity}";
    }

    private static void AddRelationship(
        List<OperationalRelationshipDto> relationships,
        string source,
        string target,
        OperationalRelationshipType relationshipType,
        OperationalContextType context,
        OperationalNavigationStrength strength,
        string reason,
        string interpretation)
    {
        relationships.Add(new OperationalRelationshipDto
        {
            SourceSurface = source,
            TargetSurface = target,
            RelationshipType = relationshipType,
            OperationalContext = context,
            RelevanceStrength = strength,
            TraversalReason = reason,
            OperatorInterpretation = interpretation
        });
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
