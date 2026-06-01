namespace Tannous.Pos.Application.Audit;

/// <summary>Adaptive TTL classification (deterministic; no ML).</summary>
public enum OperationalCacheTtlMode
{
    Normal = 0,
    Reduced = 1,
    Minimal = 2,
    BypassPreferred = 3
}
