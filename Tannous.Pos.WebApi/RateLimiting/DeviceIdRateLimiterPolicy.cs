using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Tannous.Pos.WebApi.RateLimiting;

public class DeviceIdRateLimiterPolicy : IRateLimiterPolicy<string>
{
    private readonly Func<OnRejectedContext, CancellationToken, ValueTask> _onRejected;

    public DeviceIdRateLimiterPolicy(ILogger<DeviceIdRateLimiterPolicy> logger)
    {
        _onRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            context.HttpContext.Response.Headers["Retry-After"] = "60";
            await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
        };
    }

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue("Device-Id", out var deviceId))
        {
            deviceId = "unknown";
        }

        return RateLimitPartition.GetFixedWindowLimiter(deviceId.ToString(), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask> OnRejected => _onRejected;
}
