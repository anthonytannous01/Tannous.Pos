namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Lightweight causality snapshot for short-term propagation continuity (not persisted).</summary>
public sealed class OperationalCausalitySnapshot
{
    public string DominantArea { get; init; } = string.Empty;
    public OperationalCausalityDirection PropagationDirection { get; init; }
    public int ActiveChainCount { get; init; }
    public DateTime ObservedAtUtc { get; init; }
}
