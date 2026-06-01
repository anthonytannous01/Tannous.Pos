namespace Tannous.Pos.Application.OperationalIntegrity;

/// <summary>Overall cross-layer operational interpretation integrity state.</summary>
public enum OperationalIntegrityState
{
    Coherent = 0,
    MostlyCoherent = 1,
    Fragmented = 2,
    Contradictory = 3
}
