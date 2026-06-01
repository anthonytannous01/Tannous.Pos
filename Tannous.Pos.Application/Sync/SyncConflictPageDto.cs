namespace Tannous.Pos.Application.Sync;

public sealed class SyncConflictPageDto
{
    public IReadOnlyList<SyncConflictItemDto> Items { get; init; } = Array.Empty<SyncConflictItemDto>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
}
