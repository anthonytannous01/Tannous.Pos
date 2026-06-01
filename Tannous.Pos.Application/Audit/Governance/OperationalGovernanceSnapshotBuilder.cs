namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceSnapshotBuilder
{
    public static OperationalGovernanceSnapshotComposition BuildComposition(
        string snapshotKey,
        OperationalGovernanceProfile profile,
        OperationalGovernanceCompositionContext context,
        IOperationalDiagnosticsCacheTelemetry telemetry,
        int buildElapsedMilliseconds = 0)
    {
        var telemetrySnapshot = context.Telemetry;
        var runtimeProtection = OperationalGovernanceRuntimeProtectionBuilder.Build(
            context,
            profile,
            telemetry);
        var telemetrySaturation = OperationalGovernanceRuntimeProtectionBuilder.BuildTelemetrySaturation(
            context,
            telemetrySnapshot);
        var executionDiagnostics = OperationalGovernanceRuntimeProtectionBuilder.BuildExecutionDiagnostics(context);
        var governanceConsistency = OperationalCacheGovernanceConsistencyValidator.Validate(
            context.Overview,
            context.Survivability);
        var explainability = OperationalGovernanceSnapshotExplainabilityBuilder.Build(
            OperationalGovernanceSnapshotState.Fresh,
            OperationalGovernanceProjectionReuseLevel.None,
            OperationalGovernanceSnapshotConsistencyLevel.Strong,
            wasReused: false,
            wasRebuilt: true);

        var compositionWithoutFingerprint = new OperationalGovernanceSnapshotComposition
        {
            SnapshotKey = snapshotKey,
            Profile = profile,
            CreatedUtc = DateTime.UtcNow,
            Context = context,
            RuntimeProtection = runtimeProtection,
            TelemetrySaturation = telemetrySaturation,
            ExecutionDiagnostics = executionDiagnostics,
            GovernanceConsistency = governanceConsistency,
            ExplainabilityCodes = explainability,
            FingerprintHash = string.Empty,
            NormalizedSignature = string.Empty,
            SignatureSegments = Array.Empty<string>()
        };

        var (fingerprintHash, _, signature) =
            OperationalGovernanceFingerprintBuilder.BuildFingerprintParts(compositionWithoutFingerprint);

        return new OperationalGovernanceSnapshotComposition
        {
            SnapshotKey = snapshotKey,
            Profile = profile,
            CreatedUtc = DateTime.UtcNow,
            Context = context,
            RuntimeProtection = runtimeProtection,
            TelemetrySaturation = telemetrySaturation,
            ExecutionDiagnostics = executionDiagnostics,
            GovernanceConsistency = governanceConsistency,
            ExplainabilityCodes = explainability,
            FingerprintHash = fingerprintHash,
            NormalizedSignature = signature.NormalizedSignature,
            SignatureSegments = signature.SignatureSegments,
            BuildElapsedMilliseconds = buildElapsedMilliseconds
        };
    }

    public static OperationalGovernanceSnapshotDto BuildSnapshotDto(
        OperationalGovernanceSnapshotComposition composition,
        OperationalGovernanceSnapshotFreshnessDto freshness,
        OperationalGovernanceProjectionReuseLevel reuseLevel,
        OperationalGovernanceSnapshotConsistencyLevel consistencyLevel)
    {
        var context = composition.Context;
        var snapshotState = Enum.TryParse<OperationalGovernanceSnapshotState>(
            freshness.FreshnessState,
            out var parsedState)
            ? parsedState
            : OperationalGovernanceSnapshotState.Fresh;

        var explainability = OperationalGovernanceSnapshotExplainabilityBuilder.Build(
            snapshotState,
            reuseLevel,
            consistencyLevel,
            freshness.WasReused,
            wasRebuilt: snapshotState == OperationalGovernanceSnapshotState.Fresh && !freshness.WasReused);

        return new OperationalGovernanceSnapshotDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Metadata = BuildMetadata(composition, freshness, reuseLevel, explainability),
            Freshness = freshness,
            Overview = context.Overview,
            Stability = context.Stability,
            Survivability = context.Survivability,
            StaleRisk = context.StaleRisk,
            RuntimeProtection = composition.RuntimeProtection,
            TelemetrySaturation = composition.TelemetrySaturation,
            GovernanceConsistency = composition.GovernanceConsistency,
            InvalidationPressureSeverity = context.InvalidationPressureSeverity,
            ExplainabilityCodes = explainability,
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Governance snapshots are projection-only and process-local.",
                "Snapshots are not business caches and do not guarantee business freshness."
            }, 3)
        };
    }

    public static OperationalGovernanceProjectionReuseDto BuildReuseDto(
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        OperationalGovernanceSnapshotFreshnessDto freshness,
        OperationalGovernanceProjectionReuseLevel reuseLevel)
    {
        var hits = telemetry.ProjectionReuseHits;
        var misses = telemetry.ProjectionReuseMisses;
        var signals = new List<string> { $"Reuse{reuseLevel}" };
        if (freshness.WasReused)
            signals.Add("SnapshotReused");
        if (reuseLevel >= OperationalGovernanceProjectionReuseLevel.Dominant)
            signals.Add("ProjectionReuseDominant");

        return new OperationalGovernanceProjectionReuseDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            SnapshotKey = OperationalGovernanceSnapshotKeys.Standard,
            ReuseLevel = reuseLevel.ToString(),
            SnapshotState = freshness.FreshnessState,
            GovernanceSnapshotBuilds = telemetry.GovernanceSnapshotBuilds,
            GovernanceSnapshotReuses = telemetry.GovernanceSnapshotReuses,
            ProjectionReuseHits = hits,
            ProjectionReuseMisses = misses,
            ReuseHitRatio = OperationalGovernanceProjectionReuseClassifier.ComputeHitRatio(hits, misses),
            ReuseSignals = OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(signals, 6),
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Reuse is advisory orchestration optimization only.",
                "No business-path throttling is applied."
            }, 2)
        };
    }

    public static OperationalGovernanceProjectionConsistencyDto BuildConsistencyDto(
        OperationalGovernanceSnapshotComposition composition,
        OperationalGovernanceSnapshotFreshnessDto freshness,
        OperationalGovernanceProjectionReuseLevel reuseLevel,
        OperationalGovernanceSnapshotConsistencyLevel consistencyLevel,
        long snapshotConsistencyTransitions)
    {
        var snapshotState = Enum.TryParse<OperationalGovernanceSnapshotState>(
            freshness.FreshnessState,
            out var parsedState)
            ? parsedState
            : OperationalGovernanceSnapshotState.Fresh;

        return new OperationalGovernanceProjectionConsistencyDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ConsistencyLevel = consistencyLevel.ToString(),
            SnapshotState = freshness.FreshnessState,
            ReuseLevel = reuseLevel.ToString(),
            SnapshotAgeSeconds = freshness.SnapshotAgeSeconds,
            ProjectionCount = composition.ProjectionCount,
            ExplainabilitySignalCount = composition.ExplainabilityCodes.Count,
            SnapshotConsistencyTransitions = snapshotConsistencyTransitions,
            ConsistencySignals = OperationalGovernanceProjectionConsistencyClassifier.BuildSignals(
                consistencyLevel,
                snapshotState,
                reuseLevel),
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Consistency classification is advisory only.",
                "No automatic recovery or remediation is performed."
            }, 2)
        };
    }

    private static OperationalGovernanceSnapshotMetadataDto BuildMetadata(
        OperationalGovernanceSnapshotComposition composition,
        OperationalGovernanceSnapshotFreshnessDto freshness,
        OperationalGovernanceProjectionReuseLevel reuseLevel,
        IReadOnlyList<string> explainabilityCodes) =>
        new()
        {
            GeneratedAtUtc = DateTime.UtcNow,
            SnapshotKey = composition.SnapshotKey,
            Profile = composition.Profile.ToString(),
            SnapshotCreatedUtc = composition.CreatedUtc,
            SnapshotAgeSeconds = freshness.SnapshotAgeSeconds,
            SnapshotState = freshness.FreshnessState,
            ReuseLevel = reuseLevel.ToString(),
            ProjectionCount = composition.ProjectionCount,
            ExplainabilitySignalCount = explainabilityCodes.Count,
            TtlSeconds = OperationalGovernanceSnapshotReuseConstants.TtlSeconds,
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                $"Profile:{composition.Profile}",
                "Snapshot reuse is process-local."
            }, 2)
        };
}
