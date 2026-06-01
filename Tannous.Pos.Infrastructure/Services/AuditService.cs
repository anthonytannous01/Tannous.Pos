using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly PosDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(PosDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogEventAsync(string action, string entity, Guid? entityId, object? payload = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = GetUserIdFromContext(httpContext);
        var deviceId = GetDeviceIdFromContext(httpContext);
        var correlationId = GetCorrelationIdFromContext(httpContext);

        await LogEventAsync(action, entity, entityId, userId, deviceId, correlationId, payload);
    }

    public async Task LogEventAsync(string action, string entity, Guid? entityId, Guid? userId, string? deviceId, string? correlationId, object? payload = null)
    {
        var auditEvent = new AuditEvent
        {
            Utc = DateTime.UtcNow,
            UserId = userId,
            DeviceId = deviceId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            CorrelationId = correlationId,
            PayloadJson = payload != null ? JsonSerializer.Serialize(payload) : null
        };

        _context.AuditEvents.Add(auditEvent);
        await _context.SaveChangesAsync();
    }

    private static Guid? GetUserIdFromContext(HttpContext? httpContext)
    {
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }

    private static string? GetDeviceIdFromContext(HttpContext? httpContext)
    {
        return httpContext?.Request.Headers.TryGetValue("Device-Id", out var deviceId) == true 
            ? deviceId.ToString() 
            : null;
    }

    private static string? GetCorrelationIdFromContext(HttpContext? httpContext)
    {
        return httpContext?.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) == true 
            ? correlationId.ToString() 
            : null;
    }
}
