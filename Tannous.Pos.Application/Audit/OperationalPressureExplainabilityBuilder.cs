namespace Tannous.Pos.Application.Audit;

public static class OperationalPressureExplainabilityBuilder
{
    public static IReadOnlyList<string> Bound(IEnumerable<string> items) =>
        OperationalGovernanceExplainabilityComposer.Compose(
            items,
            OperationalGovernanceExplainabilityComposer.ExplainabilityProfile.Pressure);

    public static string NormalizeCode(string code) =>
        OperationalGovernanceExplainabilityComposer.NormalizeCode(
            code,
            OperationalGovernanceExplainabilityComposer.ExplainabilityProfile.Pressure);

    public static IReadOnlyList<string> BuildPressureReasonCodes(
        OperationalPressureLifecycleState lifecycle,
        OperationalPressureRecoveryClassification recovery,
        bool stickyPressure,
        bool pressureFlagsCleared,
        int convergenceScore) =>
        Bound(new[]
        {
            $"Lifecycle{lifecycle}",
            $"Recovery{recovery}",
            stickyPressure ? "StickyPressureDetected" : string.Empty,
            pressureFlagsCleared ? "PressureRecovered" : string.Empty,
            convergenceScore >= OperationalPressureGovernance.ConvergenceStableScoreThreshold
                ? "ConvergenceStable"
                : "ConvergenceUncertain",
            recovery == OperationalPressureRecoveryClassification.Stabilized ? "RecoveryStabilized" : string.Empty
        });
}
