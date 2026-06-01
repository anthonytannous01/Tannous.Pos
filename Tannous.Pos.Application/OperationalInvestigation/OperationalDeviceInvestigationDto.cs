using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalBriefing;
using Tannous.Pos.Application.OperationalEntityStatus;

namespace Tannous.Pos.Application.OperationalInvestigation;

/// <summary>
/// Correlated investigation view for a specific device.
/// Combines device health assessment, top audit highlights (Warning/Critical only, most recent first),
/// receipt outcome summary, and current system cognition context.
/// AuditHighlights contains at most 5 records. All fields are advisory and read-only.
/// </summary>
public sealed class OperationalDeviceInvestigationDto
{
    public string DeviceId { get; init; } = string.Empty;
    public DateTime InvestigationTimestampUtc { get; init; }

    // From device entity status
    public EntityHealthClassification HealthClassification { get; init; }
    public string StatusNarrative { get; init; } = string.Empty;
    public int AuditRecordCount { get; init; }
    public string HighestSeverity { get; init; } = string.Empty;
    public int UnresolvedConflictCount { get; init; }
    public DateTime? LastActivityUtc { get; init; }
    public int ReceiptTotal { get; init; }
    public int ReceiptSuccessCount { get; init; }
    public int ReceiptConflictCount { get; init; }

    // Top Warning/Critical audit records, descending — at most 5
    public IReadOnlyList<OperationalAuditTimelineItemDto> AuditHighlights { get; init; } =
        Array.Empty<OperationalAuditTimelineItemDto>();

    // From briefing
    public BriefingCognitionAge SystemCognitionAge { get; init; }
    public string SystemContextSummary { get; init; } = string.Empty;
}
