namespace Tannous.Pos.Application.OperationalStrategy;

/// <summary>Overall operational strategy coherence state.</summary>
public enum OperationalStrategyState
{
    Coherent,
    Coordinated,
    Strained,
    Fragmented,
    Overextended
}
