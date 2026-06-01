namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Combines measured surface, freeze guard, and dead-surface findings.</summary>
public static class OperationalGovernanceSurfaceAudit
{
    public static OperationalGovernanceSurfaceUsageReport Audit(string repositoryRoot)
    {
        var snapshot = OperationalGovernanceSurfaceMeasurementHelper.MeasureFromRepository(repositoryRoot);
        var validation = OperationalGovernanceExpansionGuard.Validate(snapshot);
        var deadSurface = OperationalGovernanceDeadSurfaceDetector.Detect(repositoryRoot);

        return new OperationalGovernanceSurfaceUsageReport
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Snapshot = snapshot,
            IsWithinBudget = snapshot.IsWithinBudget(),
            IsFreezeCompliant = validation.IsFrozenCompliant,
            Violations = validation.Violations,
            DeadSurfaceFindings = deadSurface.Findings,
            FreezeRationale = OperationalGovernanceFreezePolicy.FreezeRationale,
            ApprovedExtensionPolicy = OperationalGovernanceFreezePolicy.ApprovedExtensionPolicy,
            OwnershipBoundaries = OperationalGovernanceOwnershipBoundaries.All,
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Surface audit is process-local and advisory.",
                "No automatic deletion or remediation is performed."
            }, 2)
        };
    }
}
