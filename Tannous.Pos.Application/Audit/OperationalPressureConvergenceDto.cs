namespace Tannous.Pos.Application.Audit;

public sealed class OperationalPressureConvergenceDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string ConvergenceClassification { get; init; } = string.Empty;
    public int ConvergenceScore { get; init; }
    public long PressureConvergenceRecoveries { get; init; }
    public long PressureLifecycleTransitions { get; init; }
    public bool StickyPressureDetected { get; init; }
    public string ReadinessState { get; init; } = string.Empty;
    public string PressureSeverity { get; init; } = string.Empty;
    public OperationalPressureStabilizationWindowDto StabilizationWindow { get; init; } = new();
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TriggerSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
