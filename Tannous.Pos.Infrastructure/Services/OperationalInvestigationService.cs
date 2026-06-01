using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalBriefing;
using Tannous.Pos.Application.OperationalEntityStatus;
using Tannous.Pos.Application.OperationalInvestigation;

namespace Tannous.Pos.Infrastructure.Services;

public class OperationalInvestigationService : IOperationalInvestigationService
{
    private readonly IOperationalEntityStatusService _entityStatusService;
    private readonly IOperationalAuditQueryService   _auditQueryService;
    private readonly IOperationalBriefingService     _briefingService;
    private readonly ILogger<OperationalInvestigationService> _logger;

    public OperationalInvestigationService(
        IOperationalEntityStatusService entityStatusService,
        IOperationalAuditQueryService auditQueryService,
        IOperationalBriefingService briefingService,
        ILogger<OperationalInvestigationService> logger)
    {
        _entityStatusService = entityStatusService;
        _auditQueryService   = auditQueryService;
        _briefingService     = briefingService;
        _logger              = logger;
    }

    public async Task<OperationalOrderInvestigationDto> GetOrderInvestigationAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational investigation: order investigation assembled. OrderId={OrderId}",
            orderId);

        var orderStatus = await _entityStatusService
            .GetOrderStatusAsync(orderId, cancellationToken)
            .ConfigureAwait(false);

        var auditHighlights = await _auditQueryService
            .GetOrderAuditHighlightsAsync(orderId, 5, cancellationToken)
            .ConfigureAwait(false);

        var briefing = await _briefingService
            .GetBriefingSummaryAsync(cancellationToken)
            .ConfigureAwait(false);

        return OperationalInvestigationAggregation.ComposeOrderInvestigation(
            orderStatus,
            auditHighlights,
            briefing,
            DateTime.UtcNow);
    }

    public async Task<OperationalDeviceInvestigationDto> GetDeviceInvestigationAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational investigation: device investigation assembled. DeviceId={DeviceId}",
            deviceId);

        var deviceStatus = await _entityStatusService
            .GetDeviceStatusAsync(deviceId, cancellationToken)
            .ConfigureAwait(false);

        var auditHighlights = await _auditQueryService
            .GetDeviceAuditHighlightsAsync(deviceId, 5, cancellationToken)
            .ConfigureAwait(false);

        var briefing = await _briefingService
            .GetBriefingSummaryAsync(cancellationToken)
            .ConfigureAwait(false);

        return OperationalInvestigationAggregation.ComposeDeviceInvestigation(
            deviceStatus,
            auditHighlights,
            briefing,
            DateTime.UtcNow);
    }
}
