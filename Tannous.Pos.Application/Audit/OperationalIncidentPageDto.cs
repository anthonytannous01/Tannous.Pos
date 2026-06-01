namespace Tannous.Pos.Application.Audit;

public sealed class OperationalIncidentPageDto
{
    public IReadOnlyList<CorrelatedIncidentItemDto> Items { get; init; } = Array.Empty<CorrelatedIncidentItemDto>();
    public int Total { get; init; }
}
