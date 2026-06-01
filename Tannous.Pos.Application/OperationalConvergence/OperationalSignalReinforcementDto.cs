namespace Tannous.Pos.Application.OperationalConvergence;

/// <summary>Deterministic signal reinforcement across operational intelligence layers.</summary>
public sealed class OperationalSignalReinforcementDto
{
    public string OperationalArea { get; init; } = string.Empty;
    public IReadOnlyList<string> ReinforcingLayers { get; init; } = Array.Empty<string>();
    public OperationalConvergenceStrength ReinforcementStrength { get; init; }
    public string SharedOperationalDirection { get; init; } = string.Empty;
    public string SharedStabilizationInterpretation { get; init; } = string.Empty;
    public string SharedEscalationInterpretation { get; init; } = string.Empty;
    public string OperatorInterpretation { get; init; } = string.Empty;
}
