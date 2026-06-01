namespace Tannous.Pos.Application.Audit;

/// <summary>Stale-read risk classification for cached diagnostics envelopes.</summary>
public enum OperationalDiagnosticsCacheStaleRisk
{
    Fresh,
    Aging,
    NearExpiry,
    Expired
}
