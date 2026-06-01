namespace Tannous.Pos.Application.Audit;

/// <summary>Optional filters for internal operational audit timeline queries.</summary>
public sealed class OperationalAuditQueryFilter
{
    public string? Category { get; init; }
    public string? Action { get; init; }
    public string? Severity { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public bool ConflictsOnly { get; init; }
    public string? ReconciliationStatus { get; init; }
    public string? ConflictType { get; init; }
    public bool UnresolvedOnly { get; init; }
}
