namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceProjectionConsistencyClassifier
{
    public static OperationalGovernanceSnapshotConsistencyLevel Classify(
        OperationalGovernanceSnapshotState snapshotState,
        OperationalGovernanceCompositionContext context,
        OperationalGovernanceProjectionReuseLevel reuseLevel,
        OperationalCacheGovernanceConsistencyDto governanceConsistency)
    {
        if (snapshotState is OperationalGovernanceSnapshotState.Expired
            or OperationalGovernanceSnapshotState.Aging)
            return OperationalGovernanceSnapshotConsistencyLevel.Transitional;

        if (context.DriftSummary.DriftDetected || !governanceConsistency.IsConsistent)
            return OperationalGovernanceSnapshotConsistencyLevel.Fragmented;

        if (context.ExecutionState is OperationalGovernanceExecutionState.Saturated
            or OperationalGovernanceExecutionState.Failsafe
            || reuseLevel == OperationalGovernanceProjectionReuseLevel.None)
            return OperationalGovernanceSnapshotConsistencyLevel.Transitional;

        if (context.Stability.StabilityScore >= 70
            && context.ExecutionState == OperationalGovernanceExecutionState.Healthy)
            return OperationalGovernanceSnapshotConsistencyLevel.Strong;

        return OperationalGovernanceSnapshotConsistencyLevel.Stable;
    }

    public static IReadOnlyList<string> BuildSignals(
        OperationalGovernanceSnapshotConsistencyLevel level,
        OperationalGovernanceSnapshotState snapshotState,
        OperationalGovernanceProjectionReuseLevel reuseLevel)
    {
        var signals = new List<string>
        {
            $"Consistency{level}",
            $"Snapshot{snapshotState}",
            $"Reuse{reuseLevel}"
        };

        if (level == OperationalGovernanceSnapshotConsistencyLevel.Fragmented)
            signals.Add("ConsistencyFragmented");

        return OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(signals, 6);
    }
}
