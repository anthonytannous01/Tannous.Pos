namespace Tannous.Pos.Application.Audit;

/// <summary>Advisory governance drift detection (no automatic correction).</summary>
public static class OperationalCacheGovernanceDriftDetector
{
    public static OperationalCacheGovernanceDriftDto Detect(
        OperationalCacheGovernanceOverviewDto overview,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalCacheStabilityDto stability)
    {
        var signals = new List<string>();
        var reasons = new List<string>();

        if (stability.StabilityClassification == "Stable" && overview.PressureSeverity == OperationalCachePressureSeverity.Critical)
        {
            signals.Add("StableHitRatioCriticalPressure");
            reasons.Add("HitRatioPressureMismatch");
        }

        if (overview.ReadinessState == OperationalCacheReadinessState.PressureDegraded
            && overview.DegradationState == OperationalCacheDegradationState.Healthy
            && telemetry.TotalInvalidations < 3)
        {
            signals.Add("DegradedReadinessWithoutChurn");
            reasons.Add("ReadinessDegradationMismatch");
        }

        if (overview.CardinalityClassification is OperationalCacheCardinalityClassification.High
                or OperationalCacheCardinalityClassification.Saturated
            && telemetry.TotalInvalidations == 0
            && overview.ActiveEntryCount > 0)
        {
            signals.Add("SaturationWithoutInvalidations");
            reasons.Add("CardinalityInvalidationMismatch");
        }

        var total = telemetry.TotalHits + telemetry.TotalMisses;
        if (telemetry.TotalStaleServes >= 3 && telemetry.TotalMisses <= 1 && total > 0)
        {
            signals.Add("HighStaleServesLowMisses");
            reasons.Add("StaleRiskDistributionMismatch");
        }

        if (overview.ReadinessState == OperationalCacheReadinessState.Cold
            && overview.WarmCandidateCount > 0
            && !overview.WarmRecommendationsSuppressed)
        {
            signals.Add("ColdStateWithWarmCandidates");
            reasons.Add("ReadinessWarmingMismatch");
        }

        var severity = ClassifySeverity(signals.Count);
        var detected = severity != OperationalCacheGovernanceDriftSeverity.None;

        return new OperationalCacheGovernanceDriftDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            DriftDetected = detected,
            DriftSeverity = severity,
            DriftSignals = OperationalCacheExplainabilityBuilder.Bound(signals),
            ReasonCodes = OperationalCacheExplainabilityBuilder.Bound(reasons),
            GovernanceNotes = OperationalCacheExplainabilityBuilder.Bound(new[]
            {
                detected ? "Governance drift is advisory only; no auto-remediation." : "No material drift detected.",
                OperationalCacheGovernanceFinalizationGovernance.GetAssumption()
            })
        };
    }

    private static OperationalCacheGovernanceDriftSeverity ClassifySeverity(int signalCount) =>
        signalCount switch
        {
            0 => OperationalCacheGovernanceDriftSeverity.None,
            1 => OperationalCacheGovernanceDriftSeverity.Low,
            2 => OperationalCacheGovernanceDriftSeverity.Moderate,
            _ => OperationalCacheGovernanceDriftSeverity.High
        };
}
