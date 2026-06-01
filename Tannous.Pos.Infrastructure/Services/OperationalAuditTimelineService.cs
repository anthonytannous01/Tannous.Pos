using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services;

public class OperationalAuditTimelineService : IOperationalAuditTimelineService
{
    private readonly PosDbContext _db;

    public OperationalAuditTimelineService(PosDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<OperationalAuditTimelineEntryDto>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        QueryTimelineAsync(
            _db.OperationalAuditRecords.Where(r => r.OrderId == orderId),
            cancellationToken);

    public Task<IReadOnlyList<OperationalAuditTimelineEntryDto>> GetByDeviceIdAsync(
        string deviceId,
        CancellationToken cancellationToken = default) =>
        QueryTimelineAsync(
            _db.OperationalAuditRecords.Where(r => r.DeviceId == deviceId),
            cancellationToken);

    public Task<IReadOnlyList<OperationalAuditTimelineEntryDto>> GetByOperationIdAsync(
        string operationId,
        CancellationToken cancellationToken = default) =>
        QueryTimelineAsync(
            _db.OperationalAuditRecords.Where(r => r.OperationId == operationId),
            cancellationToken);

    public Task<IReadOnlyList<OperationalAuditTimelineEntryDto>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default) =>
        QueryTimelineAsync(
            _db.OperationalAuditRecords.Where(r => r.EntityType == entityType && r.EntityId == entityId),
            cancellationToken);

    private static async Task<IReadOnlyList<OperationalAuditTimelineEntryDto>> QueryTimelineAsync(
        IQueryable<Domain.Entities.OperationalAuditRecord> query,
        CancellationToken cancellationToken)
    {
        var rows = await query
            .AsNoTracking()
            .OrderBy(r => r.CreatedAtUtc)
            .ThenBy(r => r.Id)
            .Select(r => new OperationalAuditTimelineEntryDto
            {
                Id = r.Id,
                Category = r.Category,
                Action = r.Action,
                EntityType = r.EntityType,
                EntityId = r.EntityId,
                OrderId = r.OrderId,
                DeviceId = r.DeviceId,
                OperationId = r.OperationId,
                CorrelationId = r.CorrelationId,
                Severity = r.Severity,
                Summary = r.Summary,
                MetadataJson = r.MetadataJson,
                CreatedAtUtc = r.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return rows;
    }
}
