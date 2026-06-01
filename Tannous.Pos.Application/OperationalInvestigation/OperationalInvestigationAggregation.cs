using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalBriefing;
using Tannous.Pos.Application.OperationalEntityStatus;

namespace Tannous.Pos.Application.OperationalInvestigation;

/// <summary>
/// Static, deterministic aggregation for order investigation views.
/// Synchronous, no side effects, no snapshot stores. Pure projection.
/// </summary>
public static class OperationalInvestigationAggregation
{
    public static OperationalOrderInvestigationDto ComposeOrderInvestigation(
        OperationalOrderStatusDto orderStatus,
        IReadOnlyList<OperationalAuditTimelineItemDto> auditHighlights,
        OperationalBriefingSummaryDto briefing,
        DateTime investigationTimestampUtc)
    {
        return new OperationalOrderInvestigationDto
        {
            OrderId                  = orderStatus.OrderId,
            InvestigationTimestampUtc = investigationTimestampUtc,
            HealthClassification     = orderStatus.HealthClassification,
            StatusNarrative          = orderStatus.StatusNarrative,
            AuditRecordCount         = orderStatus.AuditRecordCount,
            HighestSeverity          = orderStatus.HighestSeverity,
            UnresolvedConflictCount  = orderStatus.UnresolvedConflictCount,
            LastActivityUtc          = orderStatus.LastActivityUtc,
            AuditHighlights          = auditHighlights,
            SystemCognitionAge       = briefing.CognitionAge,
            SystemContextSummary     = briefing.BriefingSummary
        };
    }

    public static OperationalDeviceInvestigationDto ComposeDeviceInvestigation(
        OperationalDeviceStatusDto deviceStatus,
        IReadOnlyList<OperationalAuditTimelineItemDto> auditHighlights,
        OperationalBriefingSummaryDto briefing,
        DateTime investigationTimestampUtc)
    {
        return new OperationalDeviceInvestigationDto
        {
            DeviceId                  = deviceStatus.DeviceId,
            InvestigationTimestampUtc = investigationTimestampUtc,
            HealthClassification      = deviceStatus.HealthClassification,
            StatusNarrative           = deviceStatus.StatusNarrative,
            AuditRecordCount          = deviceStatus.AuditRecordCount,
            HighestSeverity           = deviceStatus.HighestSeverity,
            UnresolvedConflictCount   = deviceStatus.UnresolvedConflictCount,
            LastActivityUtc           = deviceStatus.LastActivityUtc,
            ReceiptTotal              = deviceStatus.ReceiptTotal,
            ReceiptSuccessCount       = deviceStatus.ReceiptSuccessCount,
            ReceiptConflictCount      = deviceStatus.ReceiptConflictCount,
            AuditHighlights           = auditHighlights,
            SystemCognitionAge        = briefing.CognitionAge,
            SystemContextSummary      = briefing.BriefingSummary
        };
    }
}
