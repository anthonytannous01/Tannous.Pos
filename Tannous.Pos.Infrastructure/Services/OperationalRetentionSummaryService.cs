using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services;

public class OperationalRetentionSummaryService : IOperationalRetentionSummaryService
{
    private static readonly string[] UnresolvedStatuses =
    {
        nameof(ReconciliationResolutionStatus.Unresolved),
        nameof(ReconciliationResolutionStatus.Acknowledged),
        nameof(ReconciliationResolutionStatus.Investigating)
    };

    private readonly PosDbContext _db;
    private readonly IOperationalResilienceDiagnosticsService _resilienceDiagnostics;
    private readonly ILogger<OperationalRetentionSummaryService> _logger;

    public OperationalRetentionSummaryService(
        PosDbContext db,
        IOperationalResilienceDiagnosticsService resilienceDiagnostics,
        ILogger<OperationalRetentionSummaryService> logger)
    {
        _db = db;
        _resilienceDiagnostics = resilienceDiagnostics;
        _logger = logger;
    }

    public async Task<OperationalRetentionSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var advisoryCutoff = utcNow.AddDays(-OperationalRetentionConstants.UnresolvedAdvisoryDays);
        var elevatedCutoff = utcNow.AddDays(-OperationalRetentionConstants.UnresolvedElevatedDays);

        var unresolvedQuery = _db.SyncConflictRecords.AsNoTracking()
            .Where(r => UnresolvedStatuses.Contains(r.ResolutionStatus));

        var unresolvedCount = await unresolvedQuery.CountAsync(cancellationToken);
        var over7 = await unresolvedQuery.CountAsync(r => r.CreatedAtUtc <= advisoryCutoff, cancellationToken);
        var over30 = await unresolvedQuery.CountAsync(r => r.CreatedAtUtc <= elevatedCutoff, cancellationToken);
        var oldestUnresolved = await unresolvedQuery
            .OrderBy(r => r.CreatedAtUtc)
            .Select(r => (DateTime?)r.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var replayMismatchUnresolved = await unresolvedQuery.CountAsync(
            r => r.ConflictType.Contains("ReplayMismatch"),
            cancellationToken);

        var inventoryDriftUnresolved = await unresolvedQuery.CountAsync(
            r => r.ConflictType.Contains("InventoryDrift"),
            cancellationToken);

        var auditCount = await _db.OperationalAuditRecords.AsNoTracking().CountAsync(cancellationToken);
        var conflictCount = await _db.SyncConflictRecords.AsNoTracking().CountAsync(cancellationToken);
        var receiptCount = await _db.SyncOperationReceipts.AsNoTracking().CountAsync(cancellationToken);

        var truncationIndicated = over7 > 0 || auditCount > OperationalRetentionConstants.MaxTimelineExpansionItems;

        _logger.LogInformation(
            "Operational retention observability: retention summary generated. Unresolved={Unresolved}, Over7Days={Over7}, Over30Days={Over30}, AuditCount={AuditCount}, TruncationIndicated={TruncationIndicated}",
            unresolvedCount,
            over7,
            over30,
            auditCount,
            truncationIndicated);

        var guidance = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["hotOperationalWindowDays"] = OperationalRetentionConstants.HotOperationalWindowDays.ToString(),
            ["warmReconciliationWindowDays"] = OperationalRetentionConstants.WarmReconciliationWindowDays.ToString(),
            ["longTermForensicWindowDays"] = OperationalRetentionConstants.LongTermForensicWindowDays.ToString(),
            ["maxQueryDateRangeDays"] = OperationalRetentionConstants.MaxQueryDateRangeDays.ToString(),
            ["maxForensicAuditItems"] = OperationalForensicSnapshotConstants.MaxAuditTimelineItems.ToString(),
            ["forensicExportModel"] = "on-demand GET export only; no persisted export volume counter",
            ["nonGoals"] = "no automatic pruning; no physical archive provider; no compliance vault"
        };

        if (truncationIndicated)
        {
            _logger.LogWarning(
                "Operational export survivability: truncation warnings indicated in retention summary. UnresolvedOver7Days={Over7}, AuditCount={AuditCount}",
                over7,
                auditCount);
        }

        var resilience = await _resilienceDiagnostics.GetSummaryAsync(cancellationToken);

        return new OperationalRetentionSummaryDto
        {
            GeneratedAtUtc = utcNow,
            UnresolvedConflictCount = unresolvedCount,
            UnresolvedOver7DaysCount = over7,
            UnresolvedOver30DaysCount = over30,
            AuditRecordCount = auditCount,
            SyncConflictRecordCount = conflictCount,
            ReplayReceiptCount = receiptCount,
            ReplayMismatchUnresolvedCount = replayMismatchUnresolved,
            InventoryDriftUnresolvedCount = inventoryDriftUnresolved,
            OldestUnresolvedConflictUtc = oldestUnresolved,
            TruncationWarningsIndicated = truncationIndicated,
            PrimaryDegradedMode = resilience.PrimaryDegradedMode,
            QueryPressureIndicated = resilience.QueryPressureIndicated,
            ReplayStormRiskIndicated = resilience.ReplayStormRiskIndicated,
            ExportTruncationPressureIndicated = resilience.ExportTruncationPressureIndicated,
            ReconciliationBacklogSeverity = resilience.ReconciliationBacklogSeverity,
            RetentionGuidance = guidance
        };
    }
}
