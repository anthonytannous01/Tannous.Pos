namespace Tannous.Pos.Application.OperationalWorkbench;

/// <summary>Operator-facing replay instability projection (no governance jargon).</summary>
public sealed class OperationalReconciliationReplayRiskDto
{
    public string InstabilityLevel { get; init; } = "Low";
    public bool ProtectiveModeActive { get; init; }
    public bool ReplayEscalationObserved { get; init; }
    public bool StabilizationRecovering { get; init; }
    public string Summary { get; init; } = string.Empty;
}
