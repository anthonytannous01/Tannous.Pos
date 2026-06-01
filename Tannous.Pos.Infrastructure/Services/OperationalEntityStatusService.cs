using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalEntityStatus;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Pre-correlated entity health assessment via audit query summaries.
/// Injects IOperationalAuditQueryService only — no direct database access.
/// </summary>
public sealed class OperationalEntityStatusService : IOperationalEntityStatusService
{
    private readonly IOperationalAuditQueryService _auditQueryService;
    private readonly ILogger<OperationalEntityStatusService> _logger;

    public OperationalEntityStatusService(
        IOperationalAuditQueryService auditQueryService,
        ILogger<OperationalEntityStatusService> logger)
    {
        _auditQueryService = auditQueryService;
        _logger = logger;
    }

    public async Task<OperationalOrderStatusDto> GetOrderStatusAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var summary = await _auditQueryService
            .GetOrderAuditSummaryAsync(orderId, cancellationToken)
            .ConfigureAwait(false);

        var classification = OperationalEntityStatusAggregation.ClassifyHealth(
            summary.AuditRecordCount,
            summary.HighestSeverity,
            summary.UnresolvedConflictCount);

        var narrative = OperationalEntityStatusAggregation.ComposeOrderNarrative(
            summary.AuditRecordCount,
            summary.HighestSeverity,
            summary.UnresolvedConflictCount,
            classification);

        _logger.LogInformation(
            "Operational entity status observability: order assessed. OrderId={OrderId}, AuditRecordCount={AuditRecordCount}, HighestSeverity={HighestSeverity}, UnresolvedConflictCount={UnresolvedConflictCount}, Classification={Classification}",
            orderId,
            summary.AuditRecordCount,
            summary.HighestSeverity,
            summary.UnresolvedConflictCount,
            classification);

        return new OperationalOrderStatusDto
        {
            OrderId                 = orderId,
            AssessedAtUtc           = DateTime.UtcNow,
            AuditRecordCount        = summary.AuditRecordCount,
            HighestSeverity         = summary.HighestSeverity,
            UnresolvedConflictCount = summary.UnresolvedConflictCount,
            LastActivityUtc         = summary.LastActivityUtc,
            HealthClassification    = classification,
            StatusNarrative         = narrative
        };
    }

    public async Task<OperationalDeviceStatusDto> GetDeviceStatusAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        var summary = await _auditQueryService
            .GetDeviceAuditSummaryAsync(deviceId, cancellationToken)
            .ConfigureAwait(false);

        var classification = OperationalEntityStatusAggregation.ClassifyHealth(
            summary.AuditRecordCount,
            summary.HighestSeverity,
            summary.UnresolvedConflictCount);

        var narrative = OperationalEntityStatusAggregation.ComposeDeviceNarrative(
            summary.AuditRecordCount,
            summary.HighestSeverity,
            summary.UnresolvedConflictCount,
            summary.ReceiptTotal,
            summary.ReceiptConflictCount,
            classification);

        _logger.LogInformation(
            "Operational entity status observability: device assessed. DeviceId={DeviceId}, AuditRecordCount={AuditRecordCount}, HighestSeverity={HighestSeverity}, UnresolvedConflictCount={UnresolvedConflictCount}, ReceiptTotal={ReceiptTotal}, Classification={Classification}",
            deviceId,
            summary.AuditRecordCount,
            summary.HighestSeverity,
            summary.UnresolvedConflictCount,
            summary.ReceiptTotal,
            classification);

        return new OperationalDeviceStatusDto
        {
            DeviceId                = deviceId,
            AssessedAtUtc           = DateTime.UtcNow,
            AuditRecordCount        = summary.AuditRecordCount,
            HighestSeverity         = summary.HighestSeverity,
            UnresolvedConflictCount = summary.UnresolvedConflictCount,
            ReceiptTotal            = summary.ReceiptTotal,
            ReceiptSuccessCount     = summary.ReceiptSuccessCount,
            ReceiptConflictCount    = summary.ReceiptConflictCount,
            LastActivityUtc         = summary.LastActivityUtc,
            HealthClassification    = classification,
            StatusNarrative         = narrative
        };
    }
}
