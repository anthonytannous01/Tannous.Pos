namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Runtime consistency checks for snapshot reuse and memoizer coherence.</summary>
public static class OperationalGovernanceRuntimeConsistencyGuard
{
    public static OperationalGovernanceRuntimeConsistencyResult Validate(
        OperationalGovernanceSnapshotAccess access,
        OperationalGovernanceSnapshotComposition? priorComposition,
        bool governanceResetOccurred)
    {
        var issues = new List<string>();

        if (governanceResetOccurred && access.WasReused)
            issues.Add("StaleSnapshotReuseAfterReset");

        if (priorComposition != null
            && access.WasReused
            && !string.Equals(
                priorComposition.FingerprintHash,
                access.Composition.FingerprintHash,
                StringComparison.Ordinal))
            issues.Add("FingerprintChangedWhileSnapshotReused");

        if (access.WasReused && access.AgeSeconds >= OperationalGovernanceSnapshotReuseConstants.TtlSeconds)
            issues.Add("ReusedSnapshotBeyondTtl");

        var determinism = OperationalGovernanceDeterminismAudit.AuditComposition(
            access.Composition,
            access.WasReused);
        issues.AddRange(determinism.Issues);

        return new OperationalGovernanceRuntimeConsistencyResult(
            issues.Count == 0,
            OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(issues, 6));
    }

    public sealed record OperationalGovernanceRuntimeConsistencyResult(
        bool IsConsistent,
        IReadOnlyList<string> Issues);
}
