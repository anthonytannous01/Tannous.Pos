namespace Tannous.Pos.Application.Audit;

/// <summary>In-process cache telemetry (singleton-safe; not persisted).</summary>
public interface IOperationalDiagnosticsCacheTelemetry
{
    void RecordHit(string category);
    void RecordMiss(string category);
    void RecordBypass(string category);
    void RecordStaleServe(string category, OperationalDiagnosticsCacheStaleRisk staleRisk);
    void RecordInvalidation(string category, int removedCount, bool scopedKey = false);
    void RecordCrossCategoryInvalidation(int categoriesAffected);
    void RecordScopedInvalidationRecovery();
    void RecordFreshnessRecovery();
    void RecordInvalidationDrift();
    void RecordInvalidationPressureEscalation();
    void RecordConsistencyRecoveryCycle();
    void RecordContainmentEscalation();
    void RecordPropagationDetection();
    void RecordRecoveryWindowExtension();
    void RecordConsistencyConfidenceDrop();
    void RecordPressureRecoveryCycle();
    void RecordPressureLifecycleTransition();
    void RecordPressureConvergenceRecovery();
    void RecordStickyPressureRecovery();
    void RecordStabilizationWindowReset();
    void RecordAdaptiveTtlRecovery();
    void RecordGovernanceFailsafeActivation();
    void RecordRuntimeBudgetConstrainedEvent();
    void RecordProjectionComplexityElevation();
    void RecordTelemetrySaturationEvent();
    void RecordExplainabilityTruncation();
    void RecordGovernanceSnapshotBuild();
    void RecordGovernanceSnapshotReuse();
    void RecordProjectionReuseHit();
    void RecordProjectionReuseMiss();
    void RecordSnapshotConsistencyTransition();
    void RecordGovernanceFingerprintTransition();
    void RecordGovernanceStableFingerprintHit();
    void RecordGovernanceDriftEscalation();
    void RecordReplayConsistencyCheck();
    void RecordProjectionFragmentationSignal();
    void RecordCompositionReuseHit();
    void RecordCompositionReuseMiss();
    void RecordCompositionNestedReadAvoidance();
    void RecordCompositionSnapshotBuild();
    /// <summary>Clears governance-only telemetry that can keep readiness degraded after pressure recovery.</summary>
    void ResetGovernanceStabilizationBaseline();
    void RecordAdaptiveTtlReduction(string category);
    void RecordRepeatedColdMiss(string category);
    void RecordWarmRecommendation(string category);
    OperationalDiagnosticsCacheTelemetrySnapshotDto GetSnapshot();
}
