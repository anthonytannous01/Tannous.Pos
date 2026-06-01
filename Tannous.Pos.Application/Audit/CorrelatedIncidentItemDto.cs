namespace Tannous.Pos.Application.Audit;

public sealed class CorrelatedIncidentItemDto
{
    public Guid IncidentGroupId { get; init; }
    public string PrimaryIncidentType { get; init; } = string.Empty;
    public string Severity { get; init; } = OperationalIncidentSeverity.Low;
    public string CorrelatedRisk { get; init; } = OperationalIncidentSeverity.Low;
    public IReadOnlyList<string> CorrelatedSubsystems { get; init; } = Array.Empty<string>();
    public string CausalityHint { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }
    public string? DeviceId { get; init; }
    public string? OperationId { get; init; }
    public Guid? EntityId { get; init; }
    public int SignalCount { get; init; }
    public DateTime FirstSeenUtc { get; init; }
    public DateTime LastSeenUtc { get; init; }
}
