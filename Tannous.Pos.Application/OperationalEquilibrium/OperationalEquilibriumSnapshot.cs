namespace Tannous.Pos.Application.OperationalEquilibrium;

/// <summary>Lightweight equilibrium snapshot for bounded FIFO continuity.</summary>
public sealed class OperationalEquilibriumSnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalEquilibriumState EquilibriumState { get; init; }
    public OperationalStrainLevel SystemicStrainLevel { get; init; }
    public string HighestImbalanceArea { get; init; } = string.Empty;
    public int ImbalanceCount { get; init; }
}
