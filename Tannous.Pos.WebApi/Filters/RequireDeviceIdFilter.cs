using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Tannous.Pos.WebApi.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireDeviceIdFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var httpMethod = context.HttpContext.Request.Method;
        
        // Only apply to mutation methods
        if (httpMethod == "GET" || httpMethod == "HEAD" || httpMethod == "OPTIONS")
            return;

        // Check if Device-Id header is present
        if (!context.HttpContext.Request.Headers.TryGetValue("Device-Id", out var deviceId) || 
            string.IsNullOrEmpty(deviceId))
        {
            context.Result = new BadRequestObjectResult(new
            {
                Error = "Device-Id header is required for this operation",
                Code = "DEVICE_ID_REQUIRED"
            });
            return;
        }

        base.OnActionExecuting(context);
    }
}
