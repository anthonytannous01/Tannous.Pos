namespace Tannous.Pos.Application.Audit;

/// <summary>Indicates which forensic snapshot sections were capped for safe export.</summary>
public sealed class ForensicTruncationFlags
{
    public bool AuditTimelineTruncated { get; init; }
    public bool ConflictRecordsTruncated { get; init; }
    public bool ReplayReceiptsTruncated { get; init; }
    public bool MetadataTruncated { get; init; }

    public bool AnyTruncated =>
        AuditTimelineTruncated || ConflictRecordsTruncated || ReplayReceiptsTruncated || MetadataTruncated;
}
