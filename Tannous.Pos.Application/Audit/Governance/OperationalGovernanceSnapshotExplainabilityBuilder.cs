namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceSnapshotExplainabilityBuilder
{
    public static IReadOnlyList<string> Build(
        OperationalGovernanceSnapshotState snapshotState,
        OperationalGovernanceProjectionReuseLevel reuseLevel,
        OperationalGovernanceSnapshotConsistencyLevel consistencyLevel,
        bool wasReused,
        bool wasRebuilt)
    {
        var raw = new[]
        {
            wasReused ? "SnapshotReused" : string.Empty,
            wasRebuilt ? "SnapshotRebuilt" : string.Empty,
            snapshotState == OperationalGovernanceSnapshotState.Aging ? "SnapshotAging" : string.Empty,
            snapshotState == OperationalGovernanceSnapshotState.Expired ? "SnapshotExpired" : string.Empty,
            reuseLevel >= OperationalGovernanceProjectionReuseLevel.Dominant ? "ProjectionReuseDominant" : string.Empty,
            reuseLevel >= OperationalGovernanceProjectionReuseLevel.Significant ? "ProjectionReuseSignificant" : string.Empty,
            consistencyLevel == OperationalGovernanceSnapshotConsistencyLevel.Fragmented ? "ConsistencyFragmented" : string.Empty,
            $"Snapshot{snapshotState}"
        };

        return OperationalGovernanceExplainabilityComposer.ComposeWithRuntimeCap(
            raw,
            OperationalGovernanceRuntimeBudget.MaxExplainabilitySignals,
            orderDeterministically: true);
    }
}
