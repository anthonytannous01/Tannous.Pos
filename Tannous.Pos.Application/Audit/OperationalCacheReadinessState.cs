namespace Tannous.Pos.Application.Audit;

/// <summary>Advisory cache readiness (visibility-only; no background warming).</summary>
public enum OperationalCacheReadinessState
{
    Cold = 0,
    WarmingRecommended = 1,
    Stable = 2,
    PressureDegraded = 3
}
