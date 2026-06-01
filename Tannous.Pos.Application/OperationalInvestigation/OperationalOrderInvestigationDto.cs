using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalBriefing;
using Tannous.Pos.Application.OperationalEntityStatus;

namespace Tannous.Pos.Application.OperationalInvestigation;

/// <summary>
/// Correlated investigation view for a specific order.
/// Combines entity health assessment, top audit highlights (Warning/Critical only, most recent first),
/// and current system cognition context into a single operator-actionable surface.
/// AuditHighlights contains at most 5 records. All fields are advisory and read-only.
/// </summary>
public sealed class OperationalOrderInvestigationDto
{
    public Guid OrderId { get; init; }
    public DateTime InvestigationTimestampUtc { get; init; }

    // From entity status
    public EntityHealthClassification HealthClassification { get; init; }
    public string StatusNarrative { get; init; } = string.Empty;
    public int AuditRecordCount { get; init; }
    public string HighestSeverity { get; init; } = string.Empty;
    public int UnresolvedConflictCount { get; init; }
    public DateTime? LastActivityUtc { get; init; }

    // Top Warning/Critical audit records, descending — at most 5
    public IReadOnlyList<OperationalAuditTimelineItemDto> AuditHighlights { get; init; } =
        Array.Empty<OperationalAuditTimelineItemDto>();

    // From briefing
    public BriefingCognitionAge SystemCognitionAge { get; init; }
    public string SystemContextSummary { get; init; } = string.Empty;
}
