using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.OperationalReconciliation;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services;

public class OperationalAuditQueryService : IOperationalAuditQueryService
{
    private readonly PosDbContext _db;
    private readonly IOperationalResilienceDiagnosticsService _resilienceDiagnostics;
    private readonly ILogger<OperationalAuditQueryService> _logger;

    public OperationalAuditQueryService(
        PosDbContext db,
        IOperationalResilienceDiagnosticsService resilienceDiagnostics,
        ILogger<OperationalAuditQueryService> logger)
    {
        _db = db;
        _resilienceDiagnostics = resilienceDiagnostics;
        _logger = logger;
    }

    public Task<OperationalAuditPageDto> GetOrderTimelineAsync(
        Guid orderId,
        OperationalAuditQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational audit diagnostics: timeline query executed. Scope={Scope}, OrderId={OrderId}, Page={Page}, PageSize={PageSize}",
            "Order",
            orderId,
            page,
            pageSize);

        return QueryPageAsync(
            _db.OperationalAuditRecords.Where(r => r.OrderId == orderId),
            filter,
            page,
            pageSize,
            cancellationToken: cancellationToken);
    }

    public Task<OperationalAuditPageDto> GetDeviceTimelineAsync(
        string deviceId,
        OperationalAuditQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational audit diagnostics: timeline query executed. Scope={Scope}, DeviceId={DeviceId}, Page={Page}, PageSize={PageSize}",
            "Device",
            deviceId,
            page,
            pageSize);

        return QueryPageAsync(
            _db.OperationalAuditRecords.Where(r => r.DeviceId == deviceId),
            filter,
            page,
            pageSize,
            cancellationToken: cancellationToken);
    }

    public Task<OperationalAuditPageDto> GetOperationTimelineAsync(
        string operationId,
        OperationalAuditQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational audit diagnostics: timeline query executed. Scope={Scope}, OperationId={OperationId}, Page={Page}, PageSize={PageSize}",
            "Operation",
            operationId,
            page,
            pageSize);

        return QueryPageAsync(
            _db.OperationalAuditRecords.Where(r => r.OperationId == operationId),
            filter,
            page,
            pageSize,
            cancellationToken: cancellationToken);
    }

    public Task<OperationalAuditPageDto> GetEntityTimelineAsync(
        string entityType,
        Guid entityId,
        OperationalAuditQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational audit diagnostics: timeline query executed. Scope={Scope}, EntityType={EntityType}, EntityId={EntityId}, Page={Page}, PageSize={PageSize}",
            "Entity",
            entityType,
            entityId,
            page,
            pageSize);

        return QueryPageAsync(
            _db.OperationalAuditRecords.Where(r => r.EntityType == entityType && r.EntityId == entityId),
            filter,
            page,
            pageSize,
            cancellationToken: cancellationToken);
    }

    public Task<OperationalAuditPageDto> GetRecentConflictsAsync(
        OperationalAuditQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational audit diagnostics: conflict query executed. Page={Page}, PageSize={PageSize}",
            page,
            pageSize);

        var conflictFilter = new OperationalAuditQueryFilter
        {
            Category = filter.Category,
            Action = filter.Action,
            Severity = filter.Severity,
            FromUtc = filter.FromUtc,
            ToUtc = filter.ToUtc,
            ConflictsOnly = true,
            ConflictType = filter.ConflictType,
            ReconciliationStatus = filter.ReconciliationStatus
        };
        return QueryPageAsync(
            _db.OperationalAuditRecords.AsQueryable(),
            conflictFilter,
            page,
            pageSize,
            descending: true,
            cancellationToken: cancellationToken);
    }

    public Task<OperationalAuditPageDto> GetReconciliationWorkflowAuditAsync(
        OperationalAuditQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational audit diagnostics: conflict query executed. Scope={Scope}, Page={Page}, PageSize={PageSize}",
            "ReconciliationWorkflow",
            page,
            pageSize);

        var workflowFilter = new OperationalAuditQueryFilter
        {
            Category = OperationalAuditCategories.ReconciliationWorkflow,
            Action = filter.Action,
            Severity = filter.Severity,
            FromUtc = filter.FromUtc,
            ToUtc = filter.ToUtc,
            ReconciliationStatus = filter.ReconciliationStatus,
            ConflictType = filter.ConflictType
        };

        return QueryPageAsync(
            _db.OperationalAuditRecords.AsQueryable(),
            workflowFilter,
            page,
            pageSize,
            descending: true,
            cancellationToken: cancellationToken);
    }

    public async Task<OperationalOrderAuditSummaryDto> GetOrderAuditSummaryAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational audit diagnostics: entity status query executed. Scope={Scope}, OrderId={OrderId}",
            "OrderSummary",
            orderId);

        var auditCount = await _db.OperationalAuditRecords
            .Where(r => r.OrderId == orderId)
            .CountAsync(cancellationToken);

        var severities = await _db.OperationalAuditRecords
            .Where(r => r.OrderId == orderId)
            .Select(r => r.Severity)
            .Distinct()
            .ToListAsync(cancellationToken);

        var highestSeverity =
            severities.Contains(OperationalAuditSeverity.Critical) ? OperationalAuditSeverity.Critical
            : severities.Contains(OperationalAuditSeverity.Warning) ? OperationalAuditSeverity.Warning
            : OperationalAuditSeverity.Information;

        var conflictCount = await _db.SyncConflictRecords
            .Where(r => r.EntityType == "Order" && r.EntityId == orderId && !r.Resolved)
            .CountAsync(cancellationToken);

        var lastActivity = await _db.OperationalAuditRecords
            .Where(r => r.OrderId == orderId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => (DateTime?)r.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new OperationalOrderAuditSummaryDto
        {
            OrderId              = orderId,
            AuditRecordCount     = auditCount,
            HighestSeverity      = highestSeverity,
            UnresolvedConflictCount = conflictCount,
            LastActivityUtc      = lastActivity
        };
    }

    public async Task<OperationalDeviceAuditSummaryDto> GetDeviceAuditSummaryAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational audit diagnostics: entity status query executed. Scope={Scope}, DeviceId={DeviceId}",
            "DeviceSummary",
            deviceId);

        var auditCount = await _db.OperationalAuditRecords
            .Where(r => r.DeviceId == deviceId)
            .CountAsync(cancellationToken);

        var severities = await _db.OperationalAuditRecords
            .Where(r => r.DeviceId == deviceId)
            .Select(r => r.Severity)
            .Distinct()
            .ToListAsync(cancellationToken);

        var highestSeverity =
            severities.Contains(OperationalAuditSeverity.Critical) ? OperationalAuditSeverity.Critical
            : severities.Contains(OperationalAuditSeverity.Warning) ? OperationalAuditSeverity.Warning
            : OperationalAuditSeverity.Information;

        var conflictCount = await _db.SyncConflictRecords
            .Where(r => r.DeviceId == deviceId && !r.Resolved)
            .CountAsync(cancellationToken);

        var receiptTotal = await _db.SyncOperationReceipts
            .CountAsync(r => r.DeviceId == deviceId, cancellationToken);

        var receiptSuccessCount = await _db.SyncOperationReceipts
            .CountAsync(r => r.DeviceId == deviceId && r.Success, cancellationToken);

        var receiptConflictCount = await _db.SyncOperationReceipts
            .CountAsync(r => r.DeviceId == deviceId && r.Conflict, cancellationToken);

        var lastActivity = await _db.OperationalAuditRecords
            .Where(r => r.DeviceId == deviceId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => (DateTime?)r.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new OperationalDeviceAuditSummaryDto
        {
            DeviceId                = deviceId,
            AuditRecordCount        = auditCount,
            HighestSeverity         = highestSeverity,
            UnresolvedConflictCount = conflictCount,
            ReceiptTotal            = receiptTotal,
            ReceiptSuccessCount     = receiptSuccessCount,
            ReceiptConflictCount    = receiptConflictCount,
            LastActivityUtc         = lastActivity
        };
    }

    public async Task<IReadOnlyList<OperationalAuditTimelineItemDto>> GetOrderAuditHighlightsAsync(
        Guid orderId,
        int topN,
        CancellationToken cancellationToken = default)
    {
        var normalizedTopN = Math.Clamp(topN, 1, 10);

        _logger.LogInformation(
            "Operational audit diagnostics: highlight query executed. Scope={Scope}, OrderId={OrderId}, TopN={TopN}",
            "OrderHighlights",
            orderId,
            normalizedTopN);

        var rows = await _db.OperationalAuditRecords
            .Where(r => r.OrderId == orderId
                     && (r.Severity == OperationalAuditSeverity.Critical
                      || r.Severity == OperationalAuditSeverity.Warning))
            .OrderByDescending(r => r.CreatedAtUtc)
            .ThenByDescending(r => r.Id)
            .AsNoTracking()
            .Take(normalizedTopN)
            .ToListAsync(cancellationToken);

        return rows.Select(Project).ToList();
    }

    public async Task<IReadOnlyList<OperationalAuditTimelineItemDto>> GetDeviceAuditHighlightsAsync(
        string deviceId,
        int topN,
        CancellationToken cancellationToken = default)
    {
        var normalizedTopN = Math.Clamp(topN, 1, 10);

        _logger.LogInformation(
            "Operational audit diagnostics: highlight query executed. Scope={Scope}, DeviceId={DeviceId}, TopN={TopN}",
            "DeviceHighlights",
            deviceId,
            normalizedTopN);

        var rows = await _db.OperationalAuditRecords
            .Where(r => r.DeviceId == deviceId
                     && (r.Severity == OperationalAuditSeverity.Critical
                      || r.Severity == OperationalAuditSeverity.Warning))
            .OrderByDescending(r => r.CreatedAtUtc)
            .ThenByDescending(r => r.Id)
            .AsNoTracking()
            .Take(normalizedTopN)
            .ToListAsync(cancellationToken);

        return rows.Select(Project).ToList();
    }

    public async Task<OperationalReconciliationAuditSummaryDto> GetReconciliationSystemSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational audit diagnostics: reconciliation system summary query executed.");

        var totalUnresolved = await _db.SyncConflictRecords
            .CountAsync(r => !r.Resolved, cancellationToken);

        var oldestUnresolved = await _db.SyncConflictRecords
            .Where(r => !r.Resolved)
            .OrderBy(r => r.CreatedAtUtc)
            .Select(r => (DateTime?)r.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var orderScoped = await _db.SyncConflictRecords
            .CountAsync(r => !r.Resolved && r.EntityType == "Order", cancellationToken);

        return new OperationalReconciliationAuditSummaryDto
        {
            TotalUnresolvedConflicts       = totalUnresolved,
            OldestUnresolvedConflictUtc    = oldestUnresolved,
            OrderScopedUnresolvedConflicts = orderScoped
        };
    }

    private async Task<OperationalAuditPageDto> QueryPageAsync(
        IQueryable<Domain.Entities.OperationalAuditRecord> baseQuery,
        OperationalAuditQueryFilter filter,
        int page,
        int pageSize,
        bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = OperationalQueryProtection.NormalizePage(page);
        if (normalizedPage != page)
            LogQueryClamp("page", page, normalizedPage);

        var normalizedPageSize = OperationalQueryProtection.NormalizePageSize(pageSize);
        if (normalizedPageSize != pageSize)
            LogQueryClamp("pageSize", pageSize, normalizedPageSize);

        var dateRange = OperationalQueryProtection.NormalizeDateRange(filter.FromUtc, filter.ToUtc, DateTime.UtcNow);
        if (dateRange.DateRangeClamped)
        {
            _logger.LogWarning(
                "Operational query protection: date range clamped. RequestedDays={RequestedDays}, AppliedDays={AppliedDays}, EffectiveFrom={EffectiveFrom}, EffectiveTo={EffectiveTo}",
                dateRange.RequestedRangeDays,
                dateRange.AppliedRangeDays,
                dateRange.EffectiveFromUtc,
                dateRange.EffectiveToUtc);

            _resilienceDiagnostics.NoteQueryPressure(dateRangeClamped: true, pageSizeClamped: false);
            _logger.LogWarning(
                "Operational resilience observability: large-range diagnostics query pressure detected. AppliedDays={AppliedDays}",
                dateRange.AppliedRangeDays);
        }

        if (normalizedPageSize != pageSize)
            _resilienceDiagnostics.NoteQueryPressure(dateRangeClamped: false, pageSizeClamped: true);

        var query = ApplyFilters(baseQuery, filter, dateRange);

        var total = await query.CountAsync(cancellationToken);

        var ordered = descending
            ? query.OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id)
            : query.OrderBy(r => r.CreatedAtUtc).ThenBy(r => r.Id);

        var rows = await ordered
            .AsNoTracking()
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return new OperationalAuditPageDto
        {
            Items = rows.Select(Project).ToList(),
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            Total = total
        };
    }

    private void LogQueryClamp(string field, int requested, int applied)
    {
        _logger.LogWarning(
            "Operational query protection: pagination clamped. Field={Field}, Requested={Requested}, Applied={Applied}",
            field,
            requested,
            applied);
    }

    private static IQueryable<Domain.Entities.OperationalAuditRecord> ApplyFilters(
        IQueryable<Domain.Entities.OperationalAuditRecord> query,
        OperationalAuditQueryFilter filter,
        OperationalQueryRangeResult dateRange)
    {
        if (filter.ConflictsOnly)
        {
            query = query.Where(r =>
                r.Action == OperationalAuditActions.ReplayMismatch
                || r.Action == OperationalAuditActions.ConcurrencyConflict
                || r.Action == OperationalAuditActions.NegativeStockDetected
                || r.Action == OperationalAuditActions.LifecycleStateConflict
                || r.Action == OperationalAuditActions.PartialBatchReconciliation
                || r.Action == OperationalAuditActions.StaleOfflineMutation
                || r.Action == OperationalAuditActions.MixedBatchOutcomes);
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
            query = query.Where(r => r.Category == filter.Category);

        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(r => r.Action == filter.Action);

        if (!string.IsNullOrWhiteSpace(filter.Severity))
            query = query.Where(r => r.Severity == filter.Severity);

        if (dateRange.EffectiveFromUtc.HasValue)
            query = query.Where(r => r.CreatedAtUtc >= dateRange.EffectiveFromUtc.Value);

        if (dateRange.EffectiveToUtc.HasValue)
            query = query.Where(r => r.CreatedAtUtc <= dateRange.EffectiveToUtc.Value);

        if (!string.IsNullOrWhiteSpace(filter.ConflictType))
            query = query.Where(r => r.Summary.Contains(filter.ConflictType));

        if (!string.IsNullOrWhiteSpace(filter.ReconciliationStatus))
            query = query.Where(r => r.Summary.Contains(filter.ReconciliationStatus));

        return query;
    }

    private static OperationalAuditTimelineItemDto Project(Domain.Entities.OperationalAuditRecord record) =>
        new()
        {
            Id = record.Id,
            TimestampUtc = record.CreatedAtUtc,
            Category = record.Category,
            Action = record.Action,
            Severity = record.Severity,
            CorrelationId = record.CorrelationId,
            DeviceId = record.DeviceId,
            OperationId = record.OperationId,
            EntityType = record.EntityType,
            EntityId = record.EntityId,
            OrderId = record.OrderId,
            Message = record.Summary,
            Metadata = OperationalAuditMetadataProjection.Project(record.MetadataJson)
        };
}
