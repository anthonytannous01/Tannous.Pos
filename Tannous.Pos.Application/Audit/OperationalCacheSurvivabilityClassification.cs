namespace Tannous.Pos.Application.Audit;

/// <summary>Advisory cache survivability classification (non-authoritative).</summary>
public enum OperationalCacheSurvivabilityClassification
{
    Durable = 0,
    Stable = 1,
    Fragile = 2,
    Volatile = 3
}
