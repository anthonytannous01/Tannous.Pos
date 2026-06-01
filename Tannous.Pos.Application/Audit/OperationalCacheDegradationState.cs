namespace Tannous.Pos.Application.Audit;

/// <summary>Advisory cache degradation visibility (non-authoritative).</summary>
public enum OperationalCacheDegradationState
{
    Healthy = 0,
    Recovering = 1,
    Degraded = 2,
    SeverelyDegraded = 3
}
