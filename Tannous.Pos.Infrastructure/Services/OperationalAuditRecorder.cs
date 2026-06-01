using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services;

public class OperationalAuditRecorder : IOperationalAuditRecorder
{
    private static readonly JsonSerializerOptions MetadataJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOperationalAuditPersistenceTelemetry _persistenceTelemetry;
    private readonly ILogger<OperationalAuditRecorder> _logger;

    public OperationalAuditRecorder(
        IServiceScopeFactory scopeFactory,
        IOperationalAuditPersistenceTelemetry persistenceTelemetry,
        ILogger<OperationalAuditRecorder> logger)
    {
        _scopeFactory = scopeFactory;
        _persistenceTelemetry = persistenceTelemetry;
        _logger = logger;
    }

    public async Task RecordAsync(OperationalAuditRecordRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();

            if (request.DedupeByDeviceOperationAndAction &&
                !string.IsNullOrWhiteSpace(request.DeviceId) &&
                !string.IsNullOrWhiteSpace(request.OperationId) &&
                !string.IsNullOrWhiteSpace(request.Action))
            {
                var exists = await db.OperationalAuditRecords.AsNoTracking().AnyAsync(
                    r => r.DeviceId == request.DeviceId
                         && r.OperationId == request.OperationId
                         && r.Action == request.Action,
                    cancellationToken);
                if (exists)
                    return;
            }

            var record = new OperationalAuditRecord
            {
                Category = request.Category,
                Action = request.Action,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                OrderId = request.OrderId,
                DeviceId = request.DeviceId,
                OperationId = request.OperationId,
                CorrelationId = request.CorrelationId,
                Severity = request.Severity,
                Summary = request.Summary,
                MetadataJson = SerializeMetadata(request.Metadata),
                CreatedAtUtc = DateTime.UtcNow
            };

            db.OperationalAuditRecords.Add(record);
            await db.SaveChangesAsync(cancellationToken);
            _persistenceTelemetry.RecordSuccess();

            _logger.LogInformation(
                "Operational audit observability: audit persisted. Category={Category}, Action={Action}, EntityType={EntityType}, EntityId={EntityId}, OrderId={OrderId}, DeviceId={DeviceId}, OperationId={OperationId}, CorrelationId={CorrelationId}, Severity={Severity}",
                request.Category,
                request.Action,
                request.EntityType,
                request.EntityId,
                request.OrderId,
                request.DeviceId,
                request.OperationId,
                request.CorrelationId,
                request.Severity);
        }
        catch (Exception ex)
        {
            var failureClassification = ex.GetType().Name;
            _persistenceTelemetry.RecordFailure(failureClassification);

            _logger.LogWarning(
                ex,
                "Operational audit observability: persistence failure (best-effort; business path continues). Category={Category}, Action={Action}, DeviceId={DeviceId}, OperationId={OperationId}",
                request.Category,
                request.Action,
                request.DeviceId,
                request.OperationId);

            _logger.LogWarning(
                "Operational resilience observability: audit persistence failure classified. Classification={Classification}, FailureCount={FailureCount}",
                failureClassification,
                _persistenceTelemetry.GetRecentFailureCount());

            _logger.LogWarning(
                "Operational degraded mode: audit persistence pressure indicated. Classification={Classification}",
                failureClassification);
        }
    }

    private static string? SerializeMetadata(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata == null || metadata.Count == 0)
            return null;

        return JsonSerializer.Serialize(metadata, MetadataJsonOptions);
    }
}
