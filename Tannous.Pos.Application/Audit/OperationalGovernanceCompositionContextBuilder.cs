namespace Tannous.Pos.Application.Audit;

/// <summary>Builds shared governance composition context from cache metadata and telemetry.</summary>
public static class OperationalGovernanceCompositionContextBuilder
{
    public static OperationalGovernanceCompositionContext Build(
        IReadOnlyList<OperationalDiagnosticsCacheEntryMetadataDto> entries,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry,
        IOperationalResiliencePressureState pressureState) =>
        Governance.OperationalGovernanceProjectionPipeline.Execute(
            entries,
            telemetry,
            pressureState,
            Governance.OperationalGovernanceProfileSettings.Default);
}
