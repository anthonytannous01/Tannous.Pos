using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Sync;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services;

public class SyncConflictReconciliationService : ISyncConflictReconciliationService
{
    private static readonly string[] BacklogSeveritiesRequiringBypass = { "Elevated", "High" };

    private static readonly string[] UnresolvedStatuses =
    {
        nameof(ReconciliationResolutionStatus.Unresolved),
        nameof(ReconciliationResolutionStatus.Acknowledged),
        nameof(ReconciliationResolutionStatus.Investigating)
    };

    private readonly PosDbContext _db;
    private readonly IOperationalAuditRecorder _auditRecorder;
    private readonly IOperationalResilienceDiagnosticsService _resilienceDiagnostics;
    private readonly IOperationalDiagnosticsCache _cache;
    private readonly IOperationalDiagnosticsCacheInvalidator _cacheInvalidator;
    private readonly IOperationalResiliencePressureState _pressureState;
    private readonly IOperationalDiagnosticsCacheTelemetry _cacheTelemetry;
    private readonly ILogger<SyncConflictReconciliationService> _logger;

    public SyncConflictReconciliationService(
        PosDbContext db,
        IOperationalAuditRecorder auditRecorder,
        IOperationalResilienceDiagnosticsService resilienceDiagnostics,
        IOperationalDiagnosticsCache cache,
        IOperationalDiagnosticsCacheInvalidator cacheInvalidator,
        IOperationalResiliencePressureState pressureState,
        IOperationalDiagnosticsCacheTelemetry cacheTelemetry,
        ILogger<SyncConflictReconciliationService> logger)
    {
        _db = db;
        _auditRecorder = auditRecorder;
        _resilienceDiagnostics = resilienceDiagnostics;
        _cache = cache;
        _cacheInvalidator = cacheInvalidator;
        _pressureState = pressureState;
        _cacheTelemetry = cacheTelemetry;
        _logger = logger;
    }

    public Task<SyncConflictPageDto> GetUnresolvedAsync(
        SyncConflictQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational reconciliation observability: unresolved conflict query executed. Page={Page}, PageSize={PageSize}",
            page,
            pageSize);

        var unresolvedFilter = new SyncConflictQueryFilter
        {
            ConflictType = filter.ConflictType,
            FromUtc = filter.FromUtc,
            ToUtc = filter.ToUtc,
            UnresolvedOnly = true
        };

        return QueryPageAsync(
            _db.SyncConflictRecords.AsQueryable(),
            unresolvedFilter,
            page,
            pageSize,
            descending: true,
            cancellationToken);
    }

    public Task<SyncConflictPageDto> GetByStatusAsync(
        string resolutionStatus,
        SyncConflictQueryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var statusFilter = new SyncConflictQueryFilter
        {
            ResolutionStatus = resolutionStatus,
            ConflictType = filter.ConflictType,
            FromUtc = filter.FromUtc,
            ToUtc = filter.ToUtc
        };

        return QueryPageAsync(
            _db.SyncConflictRecords.AsQueryable(),
            statusFilter,
            page,
            pageSize,
            descending: true,
            cancellationToken);
    }

    public async Task<ReconciliationSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Operational reconciliation observability: reconciliation summary query executed");
        return await GetSummaryCachedAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<ReconciliationSummaryDto> GetSummaryCachedAsync(CancellationToken cancellationToken)
    {
        var bypass = EvaluateReconciliationCacheBypass(out var bypassReason, out var degradedMode);

        if (bypass)
        {
            LogCachePressureEscalation(degradedMode, bypassReason);
        }

        var category = OperationalDiagnosticsCacheCategories.ReconciliationSummary;
        var pressureSignals = OperationalCacheAdaptivePressureSignalBuilder.ForReconciliation(
            _pressureState,
            _cache,
            IsReplayStormRisk,
            ClassifyReconciliationBacklogSeverity);
        var effectiveTtl = OperationalCacheAdaptiveTtlHelper.ResolveEffectiveTtl(
            category,
            pressureSignals,
            _cache,
            _cacheTelemetry,
            _logger,
            out _);

        var envelope = await _cache.GetOrCreateAsync(
            OperationalDiagnosticsCacheConstants.ReconciliationSummaryCacheKey,
            category,
            effectiveTtl,
            BuildSummaryAsync,
            bypass,
            cancellationToken).ConfigureAwait(false);

        return envelope.Value;
    }

    private bool EvaluateReconciliationCacheBypass(out string bypassReason, out string degradedMode)
    {
        if (_pressureState.QueryDateRangeClamped || _pressureState.QueryPageSizeClamped)
        {
            bypassReason = "elevated query pressure";
            degradedMode = OperationalDegradedModeTypes.ElevatedQueryPressure;
            return true;
        }

        if (_pressureState.ForensicExportTruncated)
        {
            bypassReason = "export pressure";
            degradedMode = OperationalDegradedModeTypes.ExportPressure;
            return true;
        }

        if (_cache.TryGetEnvelope<OperationalResilienceMetricsSnapshot>(
                OperationalDiagnosticsCacheConstants.ResilienceMetricsCacheKey,
                OperationalDiagnosticsCacheCategories.ResilienceMetrics,
                out var resilienceCached)
            && resilienceCached != null
            && IsReplayStormRisk(resilienceCached.Value))
        {
            bypassReason = "replay storm risk";
            degradedMode = OperationalDegradedModeTypes.ReplayStormRisk;
            return true;
        }

        if (_cache.TryGetEnvelope<ReconciliationSummaryDto>(
                OperationalDiagnosticsCacheConstants.ReconciliationSummaryCacheKey,
                OperationalDiagnosticsCacheCategories.ReconciliationSummary,
                out var summaryCached)
            && summaryCached != null)
        {
            var backlogSeverity = ClassifyReconciliationBacklogSeverity(summaryCached.Value);
            if (BacklogSeveritiesRequiringBypass.Contains(backlogSeverity, StringComparer.OrdinalIgnoreCase))
            {
                bypassReason = "reconciliation backlog elevated";
                degradedMode = OperationalDegradedModeTypes.ReconciliationPressure;
                return true;
            }
        }

        bypassReason = string.Empty;
        degradedMode = OperationalDegradedModeTypes.Normal;
        return false;
    }

    private void LogCachePressureEscalation(string degradedMode, string bypassReason)
    {
        _logger.LogWarning(
            "Operational cache pressure escalation: reconciliation summary cache bypassed. Category={Category}, DegradedMode={DegradedMode}, BypassReason={BypassReason}",
            OperationalDiagnosticsCacheCategories.ReconciliationSummary,
            degradedMode,
            bypassReason);
    }

    private static bool IsReplayStormRisk(OperationalResilienceMetricsSnapshot metrics) =>
        metrics.MaxReplayReceiptsOnSingleDevice >= OperationalResilienceConstants.ReplayStormDeviceReceiptThreshold
        || metrics.ReplayReceiptCount >= OperationalResilienceConstants.ReplayStormReceiptCountThreshold;

    private static string ClassifyReconciliationBacklogSeverity(ReconciliationSummaryDto summary)
    {
        if (summary.UnresolvedCount >= OperationalResilienceConstants.HighUnresolvedConflictThreshold)
            return "High";

        if (summary.UnresolvedCount >= OperationalResilienceConstants.ReconciliationBacklogElevatedThreshold)
            return "Elevated";

        return "Normal";
    }

    private async Task<ReconciliationSummaryDto> BuildSummaryAsync(CancellationToken cancellationToken)
    {
        var query = _db.SyncConflictRecords.AsNoTracking();

        var unresolvedCount = await query.CountAsync(
            r => UnresolvedStatuses.Contains(r.ResolutionStatus),
            cancellationToken).ConfigureAwait(false);

        var investigatingCount = await query.CountAsync(
            r => r.ResolutionStatus == nameof(ReconciliationResolutionStatus.Investigating),
            cancellationToken).ConfigureAwait(false);

        var resolvedCount = await query.CountAsync(
            r => r.ResolutionStatus == nameof(ReconciliationResolutionStatus.Resolved)
                 || r.ResolutionStatus == nameof(ReconciliationResolutionStatus.Ignored),
            cancellationToken).ConfigureAwait(false);

        var replayMismatchCount = await query.CountAsync(
            r => r.ConflictType == SyncConflictTypes.ReplayMismatch,
            cancellationToken).ConfigureAwait(false);

        var concurrencyConflictCount = await query.CountAsync(
            r => r.ConflictType == SyncConflictTypes.ConcurrencyConflict,
            cancellationToken).ConfigureAwait(false);

        var lifecycleConflictCount = await query.CountAsync(
            r => r.ConflictType == SyncConflictTypes.LifecycleStateConflict,
            cancellationToken).ConfigureAwait(false);

        var inventoryDriftRiskCount = await query.CountAsync(
            r => r.ConflictType == SyncConflictTypes.InventoryDriftRisk,
            cancellationToken).ConfigureAwait(false);

        return new ReconciliationSummaryDto
        {
            UnresolvedCount = unresolvedCount,
            InvestigatingCount = investigatingCount,
            ResolvedCount = resolvedCount,
            ReplayMismatchCount = replayMismatchCount,
            ConcurrencyConflictCount = concurrencyConflictCount,
            LifecycleConflictCount = lifecycleConflictCount,
            InventoryDriftRiskCount = inventoryDriftRiskCount
        };
    }

    public Task<SyncConflictItemDto> AcknowledgeAsync(
        Guid id,
        ReconciliationStatusChangeRequest request,
        string actor,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            id,
            request,
            actor,
            nameof(ReconciliationResolutionStatus.Acknowledged),
            OperationalAuditReconciliationActions.ConflictAcknowledged,
            cancellationToken);

    public Task<SyncConflictItemDto> InvestigateAsync(
        Guid id,
        ReconciliationStatusChangeRequest request,
        string actor,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            id,
            request,
            actor,
            nameof(ReconciliationResolutionStatus.Investigating),
            OperationalAuditReconciliationActions.InvestigationStarted,
            cancellationToken);

    public Task<SyncConflictItemDto> ResolveAsync(
        Guid id,
        ReconciliationStatusChangeRequest request,
        string actor,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            id,
            request,
            actor,
            nameof(ReconciliationResolutionStatus.Resolved),
            OperationalAuditReconciliationActions.ConflictResolved,
            cancellationToken);

    public Task<SyncConflictItemDto> IgnoreAsync(
        Guid id,
        ReconciliationStatusChangeRequest request,
        string actor,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            id,
            request,
            actor,
            nameof(ReconciliationResolutionStatus.Ignored),
            OperationalAuditReconciliationActions.ConflictIgnored,
            cancellationToken);

    private async Task<SyncConflictItemDto> TransitionAsync(
        Guid id,
        ReconciliationStatusChangeRequest request,
        string actor,
        string newStatus,
        string auditAction,
        CancellationToken cancellationToken)
    {
        var record = await _db.SyncConflictRecords.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (record == null)
            throw new KeyNotFoundException($"Sync conflict record {id} was not found.");

        var previousStatus = record.ResolutionStatus;
        var notes = NormalizeNotes(request.Notes);

        record.ResolutionStatus = newStatus;
        record.ResolutionNotes = notes;
        record.ResolvedBy = actor;

        var isTerminal = newStatus is nameof(ReconciliationResolutionStatus.Resolved)
            or nameof(ReconciliationResolutionStatus.Ignored);
        record.Resolved = isTerminal;
        record.ResolvedAtUtc = isTerminal ? DateTime.UtcNow : null;

        await _db.SaveChangesAsync(cancellationToken);

        await RecordWorkflowAuditAsync(record, previousStatus, newStatus, auditAction, actor, cancellationToken);

        _logger.LogInformation(
            "Operational reconciliation observability: reconciliation status changed. ConflictId={ConflictId}, PreviousStatus={PreviousStatus}, NewStatus={NewStatus}, ConflictType={ConflictType}, Actor={Actor}",
            record.Id,
            previousStatus,
            newStatus,
            record.ConflictType,
            actor);

        _cacheInvalidator.InvalidateAfterReconciliationWorkflow();

        return Project(record, DateTime.UtcNow);
    }

    private async Task RecordWorkflowAuditAsync(
        SyncConflictRecord record,
        string previousStatus,
        string newStatus,
        string auditAction,
        string actor,
        CancellationToken cancellationToken)
    {
        try
        {
            await _auditRecorder.RecordAsync(
                new OperationalAuditRecordRequest
                {
                    Category = OperationalAuditCategories.ReconciliationWorkflow,
                    Action = auditAction,
                    EntityType = nameof(SyncConflictRecord),
                    EntityId = record.Id,
                    OrderId = record.EntityType == nameof(Order) ? record.EntityId : null,
                    DeviceId = record.DeviceId,
                    OperationId = record.OperationId,
                    CorrelationId = record.CorrelationId,
                    Severity = OperationalAuditSeverity.Information,
                    Summary = $"Reconciliation workflow: {previousStatus} -> {newStatus} ({record.ConflictType})",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["previousStatus"] = previousStatus,
                        ["newStatus"] = newStatus,
                        ["conflictType"] = record.ConflictType,
                        ["actor"] = actor
                    }
                },
                cancellationToken);

            _logger.LogInformation(
                "Operational reconciliation observability: reconciliation audit persisted. ConflictId={ConflictId}, Action={Action}, Actor={Actor}",
                record.Id,
                auditAction,
                actor);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Operational reconciliation observability: reconciliation audit persistence failed (best-effort). ConflictId={ConflictId}, Action={Action}",
                record.Id,
                auditAction);
        }
    }

    private string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return notes;

        if (notes.Length <= ReconciliationWorkflowConstants.MaxResolutionNotesLength)
            return notes;

        _logger.LogWarning(
            "Operational reconciliation observability: reconciliation notes truncated. OriginalLength={OriginalLength}, MaxLength={MaxLength}",
            notes.Length,
            ReconciliationWorkflowConstants.MaxResolutionNotesLength);

        return notes[..ReconciliationWorkflowConstants.MaxResolutionNotesLength];
    }

    private async Task<SyncConflictPageDto> QueryPageAsync(
        IQueryable<SyncConflictRecord> baseQuery,
        SyncConflictQueryFilter filter,
        int page,
        int pageSize,
        bool descending,
        CancellationToken cancellationToken)
    {
        var normalizedPage = OperationalQueryProtection.NormalizePage(page);
        var normalizedPageSize = OperationalQueryProtection.NormalizePageSize(pageSize);

        if (normalizedPage != page || normalizedPageSize != pageSize)
        {
            _logger.LogWarning(
                "Operational query protection: pagination clamped. PageRequested={PageRequested}, PageApplied={PageApplied}, PageSizeRequested={PageSizeRequested}, PageSizeApplied={PageSizeApplied}",
                page,
                normalizedPage,
                pageSize,
                normalizedPageSize);
        }

        var dateRange = OperationalQueryProtection.NormalizeDateRange(filter.FromUtc, filter.ToUtc, DateTime.UtcNow);
        if (dateRange.DateRangeClamped)
        {
            _logger.LogWarning(
                "Operational query protection: date range clamped. RequestedDays={RequestedDays}, AppliedDays={AppliedDays}",
                dateRange.RequestedRangeDays,
                dateRange.AppliedRangeDays);

            _resilienceDiagnostics.NoteQueryPressure(dateRangeClamped: true, pageSizeClamped: false);
        }

        if (normalizedPageSize != pageSize)
            _resilienceDiagnostics.NoteQueryPressure(dateRangeClamped: false, pageSizeClamped: true);

        var query = ApplyFilters(baseQuery, filter, dateRange);
        var total = await query.CountAsync(cancellationToken);

        if (total > OperationalRetentionConstants.MaxConflictAggregationItems)
        {
            _logger.LogWarning(
                "Operational query protection: conflict aggregation exceeds safe limit. Total={Total}, SafeLimit={SafeLimit}",
                total,
                OperationalRetentionConstants.MaxConflictAggregationItems);
        }

        var ordered = descending
            ? query.OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id)
            : query.OrderBy(r => r.CreatedAtUtc).ThenBy(r => r.Id);

        var rows = await ordered
            .AsNoTracking()
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        return new SyncConflictPageDto
        {
            Items = rows.Select(r => Project(r, utcNow)).ToList(),
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            Total = total
        };
    }

    private static IQueryable<SyncConflictRecord> ApplyFilters(
        IQueryable<SyncConflictRecord> query,
        SyncConflictQueryFilter filter,
        OperationalQueryRangeResult dateRange)
    {
        if (filter.UnresolvedOnly)
            query = query.Where(r => UnresolvedStatuses.Contains(r.ResolutionStatus));

        if (!string.IsNullOrWhiteSpace(filter.ResolutionStatus))
            query = query.Where(r => r.ResolutionStatus == filter.ResolutionStatus);

        if (!string.IsNullOrWhiteSpace(filter.ConflictType))
            query = query.Where(r => r.ConflictType == filter.ConflictType);

        if (dateRange.EffectiveFromUtc.HasValue)
            query = query.Where(r => r.CreatedAtUtc >= dateRange.EffectiveFromUtc.Value);

        if (dateRange.EffectiveToUtc.HasValue)
            query = query.Where(r => r.CreatedAtUtc <= dateRange.EffectiveToUtc.Value);

        return query;
    }

    private static SyncConflictItemDto Project(SyncConflictRecord record, DateTime utcNow)
    {
        var aging = OperationalConflictLifecycleClassifier.ClassifyAgingSeverity(
            record.CreatedAtUtc,
            record.ResolutionStatus,
            utcNow);

        return new SyncConflictItemDto
        {
            Id = record.Id,
            DeviceId = record.DeviceId,
            OperationId = record.OperationId,
            OperationType = record.OperationType,
            EntityType = record.EntityType,
            EntityId = record.EntityId,
            ConflictType = record.ConflictType,
            Reason = record.Reason,
            CorrelationId = record.CorrelationId,
            CreatedAtUtc = record.CreatedAtUtc,
            ResolutionStatus = record.ResolutionStatus,
            ResolutionNotes = record.ResolutionNotes,
            ResolvedBy = record.ResolvedBy,
            ResolvedAtUtc = record.ResolvedAtUtc,
            AgingSeverity = aging,
            EscalationRecommendation = OperationalConflictLifecycleClassifier.GetEscalationRecommendation(
                record.ConflictType,
                aging)
        };
    }
}
