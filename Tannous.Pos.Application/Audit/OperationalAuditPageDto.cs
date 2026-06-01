namespace Tannous.Pos.Application.Audit;

/// <summary>Paginated internal operational audit diagnostics result.</summary>
public sealed class OperationalAuditPageDto
{
    public IReadOnlyList<OperationalAuditTimelineItemDto> Items { get; init; } =
        Array.Empty<OperationalAuditTimelineItemDto>();

    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
}
