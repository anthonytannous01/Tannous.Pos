namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Bounded pressure propagation interpretation between operational areas.</summary>
public sealed class OperationalPressurePropagationDto
{
    public string SourceArea { get; init; } = string.Empty;
    public string TargetArea { get; init; } = string.Empty;
    public OperationalPropagationType PropagationType { get; init; }
    public OperationalCausalityDirection Direction { get; init; }
    public bool IsEscalating { get; init; }
    public bool IsCollapsing { get; init; }
    public string OperatorInterpretation { get; init; } = string.Empty;
}
