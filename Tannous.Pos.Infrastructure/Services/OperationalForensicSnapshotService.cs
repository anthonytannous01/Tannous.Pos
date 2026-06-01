using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Sync;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Live forensic exports (timeline/conflict/replay bodies loaded per request).
/// GOVERNANCE: full exports remain source-of-truth diagnostics; compact summaries are advisory only.
/// Cached upstream summaries may be stale within TTL; no caching of <see cref="OperationalForensicSnapshotDto"/>.
/// </summary>
public class OperationalForensicSnapshotService : IOperationalForensicSnapshotService
{
    private readonly PosDbContext _db;
    private readonly IOperationalResilienceDiagnosticsService _resilienceDiagnostics;
    private readonly ISyncConflictReconciliationService _reconciliation;
    private readonly IOperationalIncidentCorrelationService _incidentCorrelation;
    private readonly IOperationalAlertSignalService _alertSignals;
    private readonly ILogger<OperationalForensicSnapshotService> _logger;

    public OperationalForensicSnapshotService(
        PosDbContext db,
        IOperationalResilienceDiagnosticsService resilienceDiagnostics,
        ISyncConflictReconciliationService reconciliation,
        IOperationalIncidentCorrelationService incidentCorrelation,
        IOperationalAlertSignalService alertSignals,
        ILogger<OperationalForensicSnapshotService> logger)
    {
        _db = db;
        _resilienceDiagnostics = resilienceDiagnostics;
        _reconciliation = reconciliation;
        _incidentCorrelation = incidentCorrelation;
        _alertSignals = alertSignals;
        _logger = logger;
    }

    public async Task<OperationalForensicSnapshotDto?> ExportByConflictIdAsync(
        Guid conflictId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational forensic observability: forensic conflict export executed. ConflictId={ConflictId}",
            conflictId);

        var conflict = await _db.SyncConflictRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == conflictId, cancellationToken);

        if (conflict == null)
            return null;

        var auditQuery = BuildConflictRelatedAuditQuery(conflict);
        var conflictQuery = _db.SyncConflictRecords.AsNoTracking()
            .Where(r =>
                (r.DeviceId == conflict.DeviceId && r.OperationId == conflict.OperationId)
                || r.Id == conflictId);

        var (audits, auditTruncated) = await LoadAuditTimelineAsync(auditQuery, cancellationToken);
        var (conflicts, conflictTruncated) = await LoadConflictsAsync(conflictQuery, cancellationToken);
        var (replayReceipts, replayTruncated) = await LoadReplayReceiptsAsync(conflict.DeviceId, conflict.OperationId, cancellationToken);

        return await BuildSnapshotAsync(
            OperationalForensicSnapshotTypes.Conflict,
            $"export/conflict/{conflictId}",
            $"Forensic snapshot for conflict {conflictId}: {audits.Count} audit entries, {conflicts.Count} conflict records, {replayReceipts.Count} replay receipts.",
            PickCorrelationId(conflict.CorrelationId, audits),
            audits,
            conflicts,
            replayReceipts,
            new ForensicTruncationFlags
            {
                AuditTimelineTruncated = auditTruncated,
                ConflictRecordsTruncated = conflictTruncated,
                ReplayReceiptsTruncated = replayTruncated
            },
            conflict.CreatedAtUtc,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["scope"] = "conflict",
                ["conflictId"] = conflictId.ToString()
            },
            cancellationToken);
    }

    public async Task<OperationalForensicSnapshotDto> ExportByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational forensic observability: forensic timeline aggregation executed. Scope={Scope}, OrderId={OrderId}",
            "Order",
            orderId);

        var auditQuery = _db.OperationalAuditRecords.AsNoTracking().Where(r => r.OrderId == orderId);
        var conflictQuery = _db.SyncConflictRecords.AsNoTracking()
            .Where(r => r.EntityId == orderId || (r.EntityType == "Order" && r.EntityId == orderId));

        var (audits, auditTruncated) = await LoadAuditTimelineAsync(auditQuery, cancellationToken);
        var (conflicts, conflictTruncated) = await LoadConflictsAsync(conflictQuery, cancellationToken);
        var operationIds = audits
            .Select(a => a.OperationId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.Ordinal)
            .Take(OperationalForensicSnapshotConstants.MaxReplayReceipts)
            .ToList();

        var (replayReceipts, replayTruncated) = await LoadReplayReceiptsForOperationsAsync(
            audits.Select(a => a.DeviceId).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d)),
            operationIds,
            cancellationToken);

        var anchorUtc = audits.FirstOrDefault()?.TimestampUtc ?? conflicts.FirstOrDefault()?.CreatedAtUtc ?? DateTime.UtcNow;

        return await BuildSnapshotAsync(
            OperationalForensicSnapshotTypes.Order,
            $"export/order/{orderId}",
            $"Forensic snapshot for order {orderId}: {audits.Count} audit entries, {conflicts.Count} conflict records, {replayReceipts.Count} replay receipts.",
            PickCorrelationId(null, audits),
            audits,
            conflicts,
            replayReceipts,
            new ForensicTruncationFlags
            {
                AuditTimelineTruncated = auditTruncated,
                ConflictRecordsTruncated = conflictTruncated,
                ReplayReceiptsTruncated = replayTruncated
            },
            anchorUtc,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["scope"] = "order",
                ["orderId"] = orderId.ToString()
            },
            cancellationToken);
    }

    public async Task<OperationalForensicSnapshotDto> ExportByOperationIdAsync(
        string operationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational forensic observability: forensic timeline aggregation executed. Scope={Scope}, OperationId={OperationId}",
            "Operation",
            operationId);

        var auditQuery = _db.OperationalAuditRecords.AsNoTracking().Where(r => r.OperationId == operationId);
        var conflictQuery = _db.SyncConflictRecords.AsNoTracking().Where(r => r.OperationId == operationId);

        var (audits, auditTruncated) = await LoadAuditTimelineAsync(auditQuery, cancellationToken);
        var (conflicts, conflictTruncated) = await LoadConflictsAsync(conflictQuery, cancellationToken);
        var deviceId = audits.Select(a => a.DeviceId).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d))
            ?? conflicts.Select(c => c.DeviceId).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d));

        var (replayReceipts, replayTruncated) = await LoadReplayReceiptsAsync(deviceId, operationId, cancellationToken);
        var anchorUtc = audits.FirstOrDefault()?.TimestampUtc ?? conflicts.FirstOrDefault()?.CreatedAtUtc ?? DateTime.UtcNow;

        return await BuildSnapshotAsync(
            OperationalForensicSnapshotTypes.Operation,
            $"export/operation/{operationId}",
            $"Forensic snapshot for operation {operationId}: {audits.Count} audit entries, {conflicts.Count} conflict records, {replayReceipts.Count} replay receipts.",
            PickCorrelationId(null, audits),
            audits,
            conflicts,
            replayReceipts,
            new ForensicTruncationFlags
            {
                AuditTimelineTruncated = auditTruncated,
                ConflictRecordsTruncated = conflictTruncated,
                ReplayReceiptsTruncated = replayTruncated
            },
            anchorUtc,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["scope"] = "operation",
                ["operationId"] = operationId
            },
            cancellationToken);
    }

    public async Task<OperationalForensicSnapshotDto> ExportByDeviceIdAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Operational forensic observability: forensic timeline aggregation executed. Scope={Scope}, DeviceId={DeviceId}",
            "Device",
            deviceId);

        var auditQuery = _db.OperationalAuditRecords.AsNoTracking().Where(r => r.DeviceId == deviceId);
        var conflictQuery = _db.SyncConflictRecords.AsNoTracking().Where(r => r.DeviceId == deviceId);

        var (audits, auditTruncated) = await LoadAuditTimelineAsync(auditQuery, cancellationToken);
        var (conflicts, conflictTruncated) = await LoadConflictsAsync(conflictQuery, cancellationToken);
        var (replayReceipts, replayTruncated) = await LoadReplayReceiptsAsync(deviceId, operationId: null, cancellationToken);
        var anchorUtc = audits.FirstOrDefault()?.TimestampUtc ?? conflicts.FirstOrDefault()?.CreatedAtUtc ?? DateTime.UtcNow;

        return await BuildSnapshotAsync(
            OperationalForensicSnapshotTypes.Device,
            $"export/device/{deviceId}",
            $"Forensic snapshot for device {deviceId}: {audits.Count} audit entries, {conflicts.Count} conflict records, {replayReceipts.Count} replay receipts.",
            PickCorrelationId(null, audits),
            audits,
            conflicts,
            replayReceipts,
            new ForensicTruncationFlags
            {
                AuditTimelineTruncated = auditTruncated,
                ConflictRecordsTruncated = conflictTruncated,
                ReplayReceiptsTruncated = replayTruncated
            },
            anchorUtc,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["scope"] = "device",
                ["deviceId"] = deviceId
            },
            cancellationToken);
    }

    private IQueryable<OperationalAuditRecord> BuildConflictRelatedAuditQuery(SyncConflictRecord conflict)
    {
        var query = _db.OperationalAuditRecords.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(conflict.CorrelationId))
        {
            var correlationId = conflict.CorrelationId;
            return query.Where(r =>
                r.CorrelationId == correlationId
                || r.OperationId == conflict.OperationId
                || (r.DeviceId == conflict.DeviceId && r.OperationId == conflict.OperationId)
                || (conflict.EntityId.HasValue && r.EntityId == conflict.EntityId));
        }

        return query.Where(r =>
            r.OperationId == conflict.OperationId
            || (r.DeviceId == conflict.DeviceId && r.OperationId == conflict.OperationId)
            || (conflict.EntityId.HasValue && r.EntityId == conflict.EntityId));
    }

    private async Task<(List<AuditTimelineSnapshotItemDto> Items, bool Truncated)> LoadAuditTimelineAsync(
        IQueryable<OperationalAuditRecord> query,
        CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(r => r.CreatedAtUtc)
            .ThenBy(r => r.Id)
            .Take(OperationalForensicSnapshotConstants.MaxAuditTimelineItems)
            .ToListAsync(cancellationToken);

        if (total > OperationalRetentionConstants.MaxTimelineExpansionItems)
        {
            _logger.LogWarning(
                "Operational query protection: timeline reconstruction exceeds safe limit. Total={Total}, SafeLimit={SafeLimit}",
                total,
                OperationalRetentionConstants.MaxTimelineExpansionItems);
        }

        return (rows.Select(ProjectAudit).ToList(), total > OperationalForensicSnapshotConstants.MaxAuditTimelineItems);
    }

    private async Task<(List<ConflictSnapshotItemDto> Items, bool Truncated)> LoadConflictsAsync(
        IQueryable<SyncConflictRecord> query,
        CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(r => r.CreatedAtUtc)
            .ThenBy(r => r.Id)
            .Take(OperationalForensicSnapshotConstants.MaxConflictRecords)
            .ToListAsync(cancellationToken);

        if (total > OperationalRetentionConstants.MaxConflictAggregationItems)
        {
            _logger.LogWarning(
                "Operational query protection: conflict aggregation exceeds safe limit. Total={Total}, SafeLimit={SafeLimit}",
                total,
                OperationalRetentionConstants.MaxConflictAggregationItems);
        }

        return (rows.Select(ProjectConflict).ToList(), total > OperationalForensicSnapshotConstants.MaxConflictRecords);
    }

    private async Task<(List<SyncOperationReceipt> Items, bool Truncated)> LoadReplayReceiptsAsync(
        string? deviceId,
        string? operationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return (new List<SyncOperationReceipt>(), false);

        var query = _db.SyncOperationReceipts.AsNoTracking().Where(r => r.DeviceId == deviceId);

        if (!string.IsNullOrWhiteSpace(operationId))
            query = query.Where(r => r.OperationId == operationId);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(r => r.ProcessedAtUtc)
            .ThenBy(r => r.Id)
            .Take(OperationalForensicSnapshotConstants.MaxReplayReceipts)
            .ToListAsync(cancellationToken);

        return (rows, total > OperationalForensicSnapshotConstants.MaxReplayReceipts);
    }

    private async Task<(List<SyncOperationReceipt> Items, bool Truncated)> LoadReplayReceiptsForOperationsAsync(
        string? deviceId,
        IReadOnlyList<string> operationIds,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || operationIds.Count == 0)
            return (new List<SyncOperationReceipt>(), false);

        var query = _db.SyncOperationReceipts.AsNoTracking()
            .Where(r => r.DeviceId == deviceId && operationIds.Contains(r.OperationId));

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(r => r.ProcessedAtUtc)
            .ThenBy(r => r.Id)
            .Take(OperationalForensicSnapshotConstants.MaxReplayReceipts)
            .ToListAsync(cancellationToken);

        return (rows, total > OperationalForensicSnapshotConstants.MaxReplayReceipts);
    }

    private async Task<OperationalForensicSnapshotDto> BuildSnapshotAsync(
        string snapshotType,
        string exportSource,
        string summary,
        string? correlationId,
        IReadOnlyList<AuditTimelineSnapshotItemDto> audits,
        IReadOnlyList<ConflictSnapshotItemDto> conflicts,
        IReadOnlyList<SyncOperationReceipt> replayReceipts,
        ForensicTruncationFlags truncationFlags,
        DateTime retentionAnchorUtc,
        IReadOnlyDictionary<string, string> scopeMetadata,
        CancellationToken cancellationToken)
    {
        var metadata = MergeMetadata(scopeMetadata, audits, conflicts, replayReceipts);
        var metadataTruncated = metadata.Count >= OperationalForensicSnapshotConstants.MaxSnapshotMetadataKeys;
        if (metadataTruncated)
        {
            truncationFlags = new ForensicTruncationFlags
            {
                AuditTimelineTruncated = truncationFlags.AuditTimelineTruncated,
                ConflictRecordsTruncated = truncationFlags.ConflictRecordsTruncated,
                ReplayReceiptsTruncated = truncationFlags.ReplayReceiptsTruncated,
                MetadataTruncated = true
            };
        }

        if (truncationFlags.AnyTruncated)
        {
            _resilienceDiagnostics.NoteForensicExportTruncation(true);
            _logger.LogWarning(
                "Operational export survivability: forensic snapshot truncated. ExportSource={ExportSource}, AuditTruncated={AuditTruncated}, ConflictTruncated={ConflictTruncated}, ReplayTruncated={ReplayTruncated}, MetadataTruncated={MetadataTruncated}",
                exportSource,
                truncationFlags.AuditTimelineTruncated,
                truncationFlags.ConflictRecordsTruncated,
                truncationFlags.ReplayReceiptsTruncated,
                truncationFlags.MetadataTruncated);

            _logger.LogWarning(
                "Operational backpressure visibility: export pressure classification applied. ExportSource={ExportSource}",
                exportSource);
        }

        var generatedUtc = DateTime.UtcNow;
        var retentionClassification = OperationalRetentionGovernance.ClassifyRetention(retentionAnchorUtc, generatedUtc);
        var truncationSeverity = OperationalDegradedModeClassifier.ClassifyExportTruncationSeverity(truncationFlags);
        var exportPressure = OperationalDegradedModeClassifier.ClassifyExportPressure(
            new OperationalResilienceMetricsSnapshot
            {
                GeneratedAtUtc = generatedUtc,
                AuditRecordCount = audits.Count,
                ForensicExportTruncated = truncationFlags.AnyTruncated
            },
            truncationFlags);

        string? survivabilityWarning = truncationFlags.AnyTruncated
            ? "Snapshot truncated for safe export; not a complete immutable archive."
            : null;

        var incidentCorrelation = _incidentCorrelation.BuildForensicCorrelation(conflicts, audits, truncationFlags);

        // Sequential cached upstream composition (no forensic DTO cache; live scope bodies already loaded).
        var resilienceSummary = await _resilienceDiagnostics.GetSummaryAsync(cancellationToken).ConfigureAwait(false);
        var reconciliationSummary = await _reconciliation.GetSummaryAsync(cancellationToken).ConfigureAwait(false);
        var incidentSummary = await _incidentCorrelation.GetSummaryAsync(cancellationToken).ConfigureAwait(false);

        var alertSignals = FilterSignalsForSnapshot(
            await _alertSignals.GetCurrentSignalsAsync(cancellationToken).ConfigureAwait(false),
            conflicts,
            audits,
            correlationId);
        var alertSummary = BuildAlertSummary(alertSignals, generatedUtc);
        var escalationRisk = DeriveEscalationRisk(alertSignals, incidentCorrelation.CorrelatedIncidentRisk);
        var pressureSummary =
            $"exportPressure={exportPressure}; truncationSeverity={truncationSeverity}; alertSignals={alertSignals.Count}; primaryMode={resilienceSummary.PrimaryDegradedMode}; backlogSeverity={resilienceSummary.ReconciliationBacklogSeverity}";

        var compactSummary = BuildCompactForensicSummary(
            generatedUtc,
            audits.Count,
            conflicts.Count,
            replayReceipts.Count,
            truncationFlags,
            incidentCorrelation,
            resilienceSummary,
            reconciliationSummary,
            incidentSummary,
            escalationRisk,
            pressureSummary);

        _logger.LogInformation(
            "Operational forensic observability: forensic snapshot generated. SnapshotType={SnapshotType}, ExportSource={ExportSource}, AuditCount={AuditCount}, ConflictCount={ConflictCount}, ReplayCount={ReplayCount}, CorrelationId={CorrelationId}, AlertSignalCount={AlertSignalCount}, CompactSummaryPresent={CompactSummaryPresent}",
            snapshotType,
            exportSource,
            audits.Count,
            conflicts.Count,
            replayReceipts.Count,
            correlationId ?? "none",
            alertSignals.Count,
            compactSummary != null);

        return new OperationalForensicSnapshotDto
        {
            GeneratedAtUtc = generatedUtc,
            SnapshotGeneratedUtc = generatedUtc,
            SnapshotSchemaVersion = OperationalForensicSnapshotConstants.SnapshotSchemaVersion,
            ExportSource = exportSource,
            RetentionClassification = retentionClassification,
            TruncationFlags = truncationFlags,
            ExportPressureClassification = exportPressure,
            TruncationSeverity = truncationSeverity,
            ExportSurvivabilityWarning = survivabilityWarning,
            CorrelatedIncidentRisk = incidentCorrelation.CorrelatedIncidentRisk,
            CorrelatedSubsystems = incidentCorrelation.CorrelatedSubsystems,
            IncidentCorrelationSummary = incidentCorrelation.IncidentCorrelationSummary,
            CorrelationId = correlationId,
            SnapshotType = snapshotType,
            Summary = summary,
            ConflictRecords = conflicts,
            AuditTimeline = audits,
            Metadata = metadata,
            AlertSignals = alertSignals,
            AlertSummary = alertSummary,
            EscalationRisk = escalationRisk,
            OperationalPressureSummary = pressureSummary,
            CompactSummary = compactSummary
        };
    }

    private static OperationalForensicSnapshotSummaryDto BuildCompactForensicSummary(
        DateTime generatedUtc,
        int auditCount,
        int conflictCount,
        int replayReceiptCount,
        ForensicTruncationFlags truncationFlags,
        ForensicIncidentCorrelationDto incidentCorrelation,
        OperationalResilienceSummaryDto resilienceSummary,
        ReconciliationSummaryDto reconciliationSummary,
        OperationalIncidentSummaryDto incidentSummary,
        string escalationRisk,
        string operationalPressureSummary) =>
        new()
        {
            GeneratedAtUtc = generatedUtc,
            AuditRecordCount = auditCount,
            ConflictCount = conflictCount,
            ReplayReceiptCount = replayReceiptCount,
            CorrelatedIncidentRisk = incidentCorrelation.CorrelatedIncidentRisk,
            EscalationRisk = escalationRisk,
            ContainsTruncatedData = truncationFlags.AnyTruncated,
            PrimarySubsystem = incidentCorrelation.CorrelatedSubsystems.FirstOrDefault()
                ?? (reconciliationSummary.UnresolvedCount > 0 ? "Reconciliation" : "Operations"),
            OperationalPressureSummary =
                $"{operationalPressureSummary}; unresolved={reconciliationSummary.UnresolvedCount}; incidentGroups={incidentSummary.TotalIncidentGroups}; degradedMode={resilienceSummary.PrimaryDegradedMode}"
        };

    private static IReadOnlyList<OperationalAlertSignalDto> FilterSignalsForSnapshot(
        IReadOnlyList<OperationalAlertSignalDto> allSignals,
        IReadOnlyList<ConflictSnapshotItemDto> conflicts,
        IReadOnlyList<AuditTimelineSnapshotItemDto> audits,
        string? correlationId)
    {
        if (allSignals.Count == 0)
            return allSignals;

        var deviceIds = conflicts.Select(c => c.DeviceId).Where(d => !string.IsNullOrWhiteSpace(d)).ToHashSet(StringComparer.Ordinal);
        var operationIds = conflicts.Select(c => c.OperationId)
            .Concat(audits.Select(a => a.OperationId))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        var scoped = allSignals
            .Where(s =>
                conflicts.Count > 0
                || audits.Count > 0
                || s.Severity == OperationalAlertSeverity.Critical)
            .Where(s =>
                s.Severity == OperationalAlertSeverity.Critical
                || deviceIds.Count == 0
                || operationIds.Count == 0
                || s.AlertType is OperationalAlertTypes.ExportTruncationPressure
                    or OperationalAlertTypes.ReconciliationBacklog
                    or OperationalAlertTypes.CascadingOperationalPressure)
            .Take(20)
            .ToList();

        if (scoped.Count == 0 && !string.IsNullOrWhiteSpace(correlationId))
            return allSignals.Take(10).ToList();

        return scoped.Count > 0 ? scoped : allSignals.Take(10).ToList();
    }

    private static OperationalAlertSummaryDto BuildAlertSummary(
        IReadOnlyList<OperationalAlertSignalDto> signals,
        DateTime generatedUtc) =>
        new()
        {
            GeneratedAtUtc = generatedUtc,
            TotalSignals = signals.Count,
            CriticalSignals = signals.Count(s => s.Severity == OperationalAlertSeverity.Critical),
            WarningSignals = signals.Count(s => s.Severity == OperationalAlertSeverity.Warning),
            ReplayRelatedSignals = signals.Count(s => s.AlertType == OperationalAlertTypes.ReplayStormRisk),
            InventoryRelatedSignals = signals.Count(s => s.AlertType == OperationalAlertTypes.InventoryDriftEscalation)
        };

    private static string DeriveEscalationRisk(
        IReadOnlyList<OperationalAlertSignalDto> signals,
        string incidentRisk)
    {
        if (signals.Any(s => s.Severity == OperationalAlertSeverity.Critical))
            return OperationalAlertSeverity.Critical;

        if (signals.Any(s => s.Severity == OperationalAlertSeverity.Warning))
            return OperationalAlertSeverity.Warning;

        return incidentRisk switch
        {
            OperationalIncidentSeverity.Critical or OperationalIncidentSeverity.High => OperationalAlertSeverity.Warning,
            _ => OperationalAlertSeverity.Info
        };
    }

    private Dictionary<string, string> MergeMetadata(
        IReadOnlyDictionary<string, string> scopeMetadata,
        IReadOnlyList<AuditTimelineSnapshotItemDto> audits,
        IReadOnlyList<ConflictSnapshotItemDto> conflicts,
        IReadOnlyList<SyncOperationReceipt> replayReceipts)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in scopeMetadata)
            merged[key] = value;

        merged["auditEntryCount"] = audits.Count.ToString();
        merged["conflictRecordCount"] = conflicts.Count.ToString();
        merged["replayReceiptCount"] = replayReceipts.Count.ToString();

        if (conflicts.Count > 0)
        {
            var statuses = conflicts
                .Select(c => c.ResolutionStatus)
                .Distinct(StringComparer.Ordinal)
                .Take(10);
            merged["reconciliationStatuses"] = string.Join(",", statuses);
        }

        var metadataTruncated = false;
        for (var i = 0; i < replayReceipts.Count && merged.Count < OperationalForensicSnapshotConstants.MaxSnapshotMetadataKeys; i++)
        {
            var receipt = replayReceipts[i];
            merged[$"replay[{i}].operationType"] = Truncate(receipt.OperationType, 64);
            merged[$"replay[{i}].success"] = receipt.Success.ToString();
            merged[$"replay[{i}].conflict"] = receipt.Conflict.ToString();
            merged[$"replay[{i}].processedAtUtc"] = receipt.ProcessedAtUtc.ToString("O");
            if (!string.IsNullOrWhiteSpace(receipt.ResultMessage))
                merged[$"replay[{i}].resultMessage"] = Truncate(receipt.ResultMessage, 128);
        }

        if (replayReceipts.Count > 0 && merged.Count >= OperationalForensicSnapshotConstants.MaxSnapshotMetadataKeys)
            metadataTruncated = true;

        _logger.LogDebug(
            "Operational forensic observability: forensic metadata sanitized. KeyCount={KeyCount}, MetadataTruncated={MetadataTruncated}",
            merged.Count,
            metadataTruncated);

        if (metadataTruncated)
        {
            merged["metadataTruncated"] = "true";
        }

        return merged;
    }

    private static string? PickCorrelationId(
        string? primary,
        IReadOnlyList<AuditTimelineSnapshotItemDto> audits) =>
        !string.IsNullOrWhiteSpace(primary)
            ? primary
            : audits.Select(a => a.CorrelationId).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));

    private static AuditTimelineSnapshotItemDto ProjectAudit(OperationalAuditRecord record)
    {
        var metadata = OperationalAuditMetadataProjection.Project(record.MetadataJson);
        return new AuditTimelineSnapshotItemDto
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
            Metadata = metadata
        };
    }

    private static ConflictSnapshotItemDto ProjectConflict(SyncConflictRecord record) =>
        new()
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
            ResolvedAtUtc = record.ResolvedAtUtc
        };

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
