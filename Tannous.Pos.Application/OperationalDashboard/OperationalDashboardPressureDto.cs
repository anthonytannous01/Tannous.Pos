namespace Tannous.Pos.Application.OperationalDashboard;

/// <summary>Operator-facing pressure indicators (no internal diagnostics metadata).</summary>
public sealed class OperationalDashboardPressureDto
{
    public string Summary { get; init; } = string.Empty;
    public bool QueryPressureIndicated { get; init; }
    public bool ReplayStormRiskIndicated { get; init; }
    public bool ExportPressureIndicated { get; init; }
    public bool AuditPersistencePressureIndicated { get; init; }
    public bool RuntimeSaturationIndicated { get; init; }
    public bool ProtectiveModeActive { get; init; }
    public IReadOnlyList<string> PressureSignals { get; init; } = Array.Empty<string>();
}
