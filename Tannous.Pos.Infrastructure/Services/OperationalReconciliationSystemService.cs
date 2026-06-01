using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalBriefing;
using Tannous.Pos.Application.OperationalReconciliation;

namespace Tannous.Pos.Infrastructure.Services;

public class OperationalReconciliationSystemService : IOperationalReconciliationSystemService
{
    private readonly IOperationalAuditQueryService _auditQueryService;
    private readonly IOperationalBriefingService   _briefingService;
    private readonly ILogger<OperationalReconciliationSystemService> _logger;

    public OperationalReconciliationSystemService(
        IOperationalAuditQueryService auditQueryService,
        IOperationalBriefingService briefingService,
        ILogger<OperationalReconciliationSystemService> logger)
    {
        _auditQueryService = auditQueryService;
        _briefingService   = briefingService;
        _logger            = logger;
    }

    public async Task<OperationalReconciliationSystemDto> GetReconciliationSystemAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational reconciliation system: system view assembled.");

        var auditSummary = await _auditQueryService
            .GetReconciliationSystemSummaryAsync(cancellationToken)
            .ConfigureAwait(false);

        var briefing = await _briefingService
            .GetBriefingSummaryAsync(cancellationToken)
            .ConfigureAwait(false);

        var assessedAt = DateTime.UtcNow;

        return OperationalReconciliationSystemAggregation.ComposeReconciliationSystem(
            auditSummary,
            briefing,
            assessedAt);
    }
}
