namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceSnapshotFreshnessClassifier
{
    public static OperationalGovernanceSnapshotState Classify(
        double ageSeconds,
        bool wasReused,
        bool isExpired)
    {
        if (isExpired)
            return OperationalGovernanceSnapshotState.Expired;

        if (ageSeconds >= OperationalGovernanceSnapshotReuseConstants.AgingThresholdSeconds)
            return OperationalGovernanceSnapshotState.Aging;

        if (wasReused)
            return OperationalGovernanceSnapshotState.Reused;

        return OperationalGovernanceSnapshotState.Fresh;
    }

    public static OperationalGovernanceSnapshotFreshnessDto Build(
        double ageSeconds,
        bool wasReused,
        bool isExpired)
    {
        var state = Classify(ageSeconds, wasReused, isExpired);
        var signals = new List<string> { $"Snapshot{state}" };
        if (wasReused)
            signals.Add("SnapshotReused");

        return new OperationalGovernanceSnapshotFreshnessDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            FreshnessState = state.ToString(),
            SnapshotAgeSeconds = Math.Round(ageSeconds, 3),
            TtlSeconds = OperationalGovernanceSnapshotReuseConstants.TtlSeconds,
            WasReused = wasReused,
            FreshnessSignals = OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(signals, 6),
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Freshness refers to governance snapshot age only.",
                "Does not guarantee business data freshness."
            }, 2)
        };
    }
}
