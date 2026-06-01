namespace Tannous.Pos.Application.Sync;

public sealed class SyncConflictQueryFilter
{
    public string? ResolutionStatus { get; init; }
    public string? ConflictType { get; init; }
    public bool UnresolvedOnly { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}
