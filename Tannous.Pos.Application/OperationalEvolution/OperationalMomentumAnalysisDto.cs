namespace Tannous.Pos.Application.OperationalEvolution;

/// <summary>Deterministic recovery, escalation, and stabilization momentum interpretation.</summary>
public sealed class OperationalMomentumAnalysisDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string RecoveryMomentum { get; init; } = string.Empty;
    public string EscalationMomentum { get; init; } = string.Empty;
    public string StabilizationMomentum { get; init; } = string.Empty;
    public OperationalMomentumState RecoveryMomentumState { get; init; }
    public OperationalMomentumState EscalationMomentumState { get; init; }
    public OperationalMomentumState StabilizationMomentumState { get; init; }
    public string DominantOperationalAcceleration { get; init; } = string.Empty;
    public string DominantOperationalDeceleration { get; init; } = string.Empty;
    public string OperationalConfidence { get; init; } = string.Empty;
    public string MomentumNote { get; init; } =
        "Advisory deterministic momentum interpretation from bounded snapshot continuity. Not forecasting or predictive analytics.";
}
