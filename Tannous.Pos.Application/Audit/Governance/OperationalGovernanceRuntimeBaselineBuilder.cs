namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceRuntimeBaselineBuilder
{
    public static OperationalGovernanceRuntimeBaselineDto Build(
        OperationalGovernanceSnapshotComposition composition,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        int projectionCollaboratorCount,
        bool snapshotWasReused,
        IEnumerable<string>? additionalSignals = null)
    {
        var timingBand = OperationalGovernanceExecutionBudgetClassifier.ClassifyTimingBand(
            composition.BuildElapsedMilliseconds);
        var budgetState = OperationalGovernanceExecutionBudgetClassifier.Classify(
            composition.Context,
            timingBand,
            telemetry);
        var reuseRatio = OperationalGovernanceProjectionReuseClassifier.ComputeHitRatio(
            telemetry.ProjectionReuseHits,
            telemetry.ProjectionReuseMisses);

        var signals = new List<string>
        {
            $"ExecutionBudget:{budgetState}",
            $"TimingBand:{timingBand}",
            snapshotWasReused ? "SnapshotReused" : "SnapshotFresh"
        };

        if (telemetry.ExplainabilityTruncations > 0)
            signals.Add("ExplainabilityTruncated");
        if (telemetry.GovernanceFailsafeActivations > 0)
            signals.Add("RuntimeFailsafeObserved");

        if (additionalSignals != null)
            signals.AddRange(additionalSignals.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new OperationalGovernanceRuntimeBaselineDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ExecutionBudgetState = budgetState.ToString(),
            ProjectionTiming = new OperationalGovernanceProjectionTimingDto
            {
                GeneratedAtUtc = DateTime.UtcNow,
                BuildElapsedMilliseconds = composition.BuildElapsedMilliseconds,
                TimingBand = timingBand.ToString(),
                GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
                {
                    "Timing is coarse build duration only.",
                    "No per-stage profiling is performed."
                }, 2)
            },
            SnapshotReuseRatio = reuseRatio,
            ProjectionCollaboratorCount = projectionCollaboratorCount,
            PipelineStageCount = OperationalGovernanceProjectionPipeline.StageOrder.Count,
            ExplainabilityTruncations = telemetry.ExplainabilityTruncations,
            RuntimeFailsafeActivations = telemetry.GovernanceFailsafeActivations,
            RuntimeBudgetConstrainedEvents = telemetry.RuntimeBudgetConstrainedEvents,
            BaselineSignals = OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(signals, 6),
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Runtime baselines are advisory and process-local.",
                "No historical persistence or sampling."
            }, 2)
        };
    }
}
