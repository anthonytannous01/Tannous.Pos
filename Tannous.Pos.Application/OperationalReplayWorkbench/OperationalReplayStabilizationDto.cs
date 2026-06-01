namespace Tannous.Pos.Application.OperationalReplayWorkbench;

/// <summary>Operator-facing replay stabilization visibility (no automation).</summary>
public sealed class OperationalReplayStabilizationDto
{
    public bool StabilizationActive { get; init; }
    public bool ReplayRecoveryImproving { get; init; }
    public bool ReplayRecoveryStalled { get; init; }
    public bool ReplayPressureEscalating { get; init; }
    public bool ProtectiveContainmentActive { get; init; }
    public bool OperatorInterventionRecommended { get; init; }
    public string Summary { get; init; } = string.Empty;
}
