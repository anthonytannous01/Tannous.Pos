namespace Tannous.Pos.Domain.Interfaces;

public interface IAuditService
{
    Task LogEventAsync(string action, string entity, Guid? entityId, object? payload = null);
    Task LogEventAsync(string action, string entity, Guid? entityId, Guid? userId, string? deviceId, string? correlationId, object? payload = null);
}
