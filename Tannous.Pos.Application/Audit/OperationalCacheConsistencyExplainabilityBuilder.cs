namespace Tannous.Pos.Application.Audit;

public static class OperationalCacheConsistencyExplainabilityBuilder
{
    public static IReadOnlyList<string> Bound(IEnumerable<string> items) =>
        OperationalGovernanceExplainabilityComposer.Compose(
            items,
            OperationalGovernanceExplainabilityComposer.ExplainabilityProfile.Consistency);

    public static string NormalizeCode(string code) =>
        OperationalGovernanceExplainabilityComposer.NormalizeCode(
            code,
            OperationalGovernanceExplainabilityComposer.ExplainabilityProfile.Consistency);

    public static IReadOnlyList<string> BuildConsistencyReasonCodes(
        OperationalCacheConsistencyConfidence confidence,
        OperationalCacheRecoveryContainmentState containment,
        OperationalCachePropagationSeverity propagation,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalCacheRecoveryWindowDto window)
    {
        var codes = new List<string>
        {
            $"Confidence{confidence}",
            $"Containment{containment}"
        };

        if (propagation >= OperationalCachePropagationSeverity.Moderate)
            codes.Add("PropagationEscalated");

        if (telemetry.ConsistencyRecoveryCycles > 0)
            codes.Add("ContainmentStabilized");

        if (telemetry.RecoveryWindowExtensions > 0)
            codes.Add("RecoveryWindowExtended");

        if (telemetry.ConsistencyConfidenceDrops > 0)
            codes.Add("ConfidenceDropDetected");

        if (telemetry.CrossCategoryInvalidations > 0 && telemetry.TotalInvalidations >= 3)
            codes.Add("CrossCategoryRecoveryChurn");

        if (window.ChurnReboundDetected)
            codes.Add("ChurnReboundDetected");

        if (telemetry.TotalBypasses > 0 && telemetry.TotalHits + telemetry.TotalMisses > 0)
        {
            var bypassRatio = OperationalGovernanceThresholdEvaluator.ComputeBypassRatio(
                telemetry.TotalHits,
                telemetry.TotalMisses,
                telemetry.TotalBypasses);
            if (OperationalGovernanceThresholdEvaluator.IsAtOrAbove(
                    bypassRatio,
                    OperationalCacheConsistencyGovernance.ElevatedBypassRatioThreshold))
                codes.Add("ElevatedBypassDuringRecovery");
        }

        return Bound(codes);
    }
}
