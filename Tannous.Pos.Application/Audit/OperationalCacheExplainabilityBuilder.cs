namespace Tannous.Pos.Application.Audit;

/// <summary>Deterministic bounded explainability strings for governance classifications.</summary>
public static class OperationalCacheExplainabilityBuilder
{
    public static IReadOnlyList<string> Bound(IEnumerable<string> items) =>
        OperationalGovernanceExplainabilityComposer.Compose(
            items,
            OperationalGovernanceExplainabilityComposer.ExplainabilityProfile.CacheGovernance);

    public static string NormalizeCode(string code) =>
        OperationalGovernanceExplainabilityComposer.NormalizeCode(
            code,
            OperationalGovernanceExplainabilityComposer.ExplainabilityProfile.CacheGovernance);

    public static IReadOnlyList<string> BuildPressureReasonCodes(
        OperationalCachePressureSeverity severity,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalCacheCardinalityClassification cardinality)
    {
        var codes = new List<string> { $"Pressure{severity}" };

        if (cardinality is OperationalCacheCardinalityClassification.High
            or OperationalCacheCardinalityClassification.Saturated)
            codes.Add("ScopedKeySaturation");

        if (telemetry.TotalBypasses > 0)
            codes.Add("HighBypassRatio");

        if (telemetry.RepeatedColdMisses >= 1)
            codes.Add("FrequentColdMisses");

        if (telemetry.TotalInvalidations >= 3)
            codes.Add("InvalidationChurn");

        return Bound(codes);
    }

    public static IReadOnlyList<string> BuildStabilityTriggerSignals(OperationalCacheStabilityDto stability) =>
        Bound(new[]
        {
            $"HitRatio:{stability.HitRatio:F2}",
            $"StaleServeRatio:{stability.StaleServeRatio:F2}",
            $"BypassRatio:{stability.BypassRatio:F2}",
            stability.RepeatedColdMisses > 0 ? "RepeatedColdMisses" : string.Empty,
            stability.InvalidationChurn > 0 ? "InvalidationChurn" : string.Empty
        });

    public static IReadOnlyList<string> BuildReadinessNotes(
        OperationalCacheReadinessState readiness,
        OperationalCachePressureSeverity pressureSeverity) =>
        Bound(new[]
        {
            $"Readiness:{readiness}",
            pressureSeverity >= OperationalCachePressureSeverity.Elevated ? "PressureDegradedReadiness" : string.Empty,
            OperationalCacheGovernanceFinalizationGovernance.GetExplainabilityAssumption()
        });
}
