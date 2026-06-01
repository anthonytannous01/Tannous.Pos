namespace Tannous.Pos.Application.Audit;

/// <summary>Normalizes repeated severity/classification labels across governance projections.</summary>
public static class OperationalGovernanceClassificationNormalizer
{
    public static OperationalCachePressureSeverity NormalizePressureSeverity(
        OperationalCachePressureSeverity severity) =>
        severity;

    public static string NormalizeStabilityClassification(string classification) =>
        classification switch
        {
            "Stable" or "Recovering" or "Degraded" or "Unstable" => classification,
            _ => "Unstable"
        };

    public static string NormalizeConvergenceClassification(string classification) =>
        classification switch
        {
            "Stable" or "Moderate" or "Uncertain" or "Unstable" => classification,
            _ => "Unstable"
        };

    public static OperationalCacheConsistencyConfidence NormalizeConfidence(
        OperationalCacheConsistencyConfidence confidence) =>
        confidence;

    public static OperationalCacheInvalidationSeverity NormalizeInvalidationSeverity(
        OperationalCacheInvalidationSeverity severity) =>
        severity;
}
