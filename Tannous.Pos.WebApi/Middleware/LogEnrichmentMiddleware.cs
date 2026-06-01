using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Tannous.Pos.WebApi.Middleware;

public class LogEnrichmentMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LogEnrichmentMiddleware> _logger;

    public LogEnrichmentMiddleware(RequestDelegate next, ILogger<LogEnrichmentMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["DeviceId"] = GetDeviceId(context),
            ["UserId"] = GetUserId(context),
            ["OrderId"] = GetOrderId(context),
            ["Path"] = context.Request.Path,
            ["Method"] = context.Request.Method
        });

        await _next(context);
    }

    private static string GetDeviceId(HttpContext context)
    {
        return context.Request.Headers.TryGetValue("Device-Id", out var deviceId) 
            ? deviceId.ToString() 
            : "unknown";
    }

    private static string GetUserId(HttpContext context)
    {
        var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim ?? "anonymous";
    }

    private static string GetOrderId(HttpContext context)
    {
        // Versioned routes use /api/v{version}/orders/{id}; avoid brittle StartsWithSegments("/api/orders")
        if (!context.Request.RouteValues.TryGetValue("id", out var id))
            return "none";

        var path = context.Request.Path.Value ?? string.Empty;
        return path.Contains("/orders", StringComparison.OrdinalIgnoreCase)
            ? id?.ToString() ?? "unknown"
            : "none";
    }
}
