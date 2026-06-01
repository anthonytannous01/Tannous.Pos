namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Approved telemetry access for governance projections (in-process snapshot only).</summary>
public static class OperationalGovernanceTelemetryAccess
{
    public static OperationalDiagnosticsCacheTelemetrySnapshotDto CaptureSnapshot(
        IOperationalDiagnosticsCacheTelemetry telemetry) =>
        telemetry.GetSnapshot();
}
