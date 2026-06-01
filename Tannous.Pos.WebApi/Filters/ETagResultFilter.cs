using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.WebApi.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class ETagResultFilter : Attribute, IResultFilter
{
    private readonly string _entityType;
    private readonly IETagService _etagService;

    public ETagResultFilter(string entityType, IETagService etagService)
    {
        _entityType = entityType;
        _etagService = etagService;
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        // This will be set by the controller
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
        if (context.Result is ObjectResult objectResult)
        {
            var etag = objectResult.Value?.GetType().GetProperty("ETag")?.GetValue(objectResult.Value) as string;
            if (!string.IsNullOrEmpty(etag))
            {
                context.HttpContext.Response.Headers["ETag"] = etag;
                
                // Check If-None-Match header
                var ifNoneMatch = context.HttpContext.Request.Headers["If-None-Match"].FirstOrDefault();
                if (!string.IsNullOrEmpty(ifNoneMatch) && _etagService.IsETagValid(etag, ifNoneMatch))
                {
                    // Set status code directly since Result is read-only in OnResultExecuted
                    context.HttpContext.Response.StatusCode = 304; // Not Modified
                }
            }
        }
    }
}
