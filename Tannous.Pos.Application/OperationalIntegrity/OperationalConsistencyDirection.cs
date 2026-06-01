namespace Tannous.Pos.Application.OperationalIntegrity;

/// <summary>Cross-layer stabilization interpretation direction.</summary>
public enum OperationalConsistencyDirection
{
    Aligning = 0,
    Stable = 1,
    Diverging = 2,
    Contradicting = 3
}
