using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Tannous.Pos.WebApi.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        context.Items["CorrelationId"] = correlationId;

        // Add correlation ID to response headers
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        
        // MEL scope + Serilog LogContext so all sinks and request logs get CorrelationId
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // Check if correlation ID is already in request headers
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var existingCorrelationId))
        {
            return existingCorrelationId.ToString();
        }

        // Check if correlation ID is in query string
        if (context.Request.Query.TryGetValue("correlationId", out var queryCorrelationId))
        {
            return queryCorrelationId.ToString();
        }

        // Generate new correlation ID
        return Guid.NewGuid().ToString();
    }
}
