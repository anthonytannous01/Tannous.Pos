namespace Tannous.Pos.Application.OperationalIncidents;

/// <summary>Lightweight incident snapshot for short-term recurrence recognition (not persisted).</summary>
public sealed class OperationalIncidentCaseSnapshot
{
    public string IncidentId { get; init; } = string.Empty;
    public string CategoryKey { get; init; } = string.Empty;
    public OperationalIncidentSeverity Severity { get; init; }
    public string RecommendedRoute { get; init; } = string.Empty;
    public string StabilityKey { get; init; } = string.Empty;
    public DateTime ObservedAtUtc { get; init; }
}
