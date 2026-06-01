namespace Tannous.Pos.Application.OperationalReplayWorkbench;

/// <summary>Operator-facing replay pressure summary (advisory only).</summary>
public sealed class OperationalReplayPressureSummaryDto
{
    public OperationalReplayPressureLevel InstabilityLevel { get; init; }
    public int ActiveReplayPressure { get; init; }
    public bool ReplayEscalationVisible { get; init; }
    public bool ProtectiveModeVisible { get; init; }
    public bool RecoveryProgressionIndicated { get; init; }
    public OperationalReplayStabilizationState StabilizationPressureState { get; init; }
    public string Summary { get; init; } = string.Empty;
}
