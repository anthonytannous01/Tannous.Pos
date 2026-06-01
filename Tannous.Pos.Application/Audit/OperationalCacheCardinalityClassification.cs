namespace Tannous.Pos.Application.Audit;

/// <summary>Deterministic in-process cache cardinality classification (advisory only).</summary>
public enum OperationalCacheCardinalityClassification
{
    Normal = 0,
    Elevated = 1,
    High = 2,
    Saturated = 3
}
