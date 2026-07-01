using Microsoft.AspNetCore.Mvc.Filters;

namespace Tannous.Pos.WebApi.Filters;

/// <summary>
/// Global action filter that normalises all DateTime / DateTime? action parameters to
/// DateTimeKind.Utc before they reach the handler.
///
/// ASP.NET Core model binding parses ISO 8601 strings without a timezone suffix (e.g.
/// "2026-05-27T00:00:00") as Kind=Unspecified. Npgsql 8 rejects Unspecified values
/// written to timestamptz columns with ArgumentException, which maps to HTTP 400 for
/// every endpoint that accepts a date range. This filter fixes the issue globally.
///
/// Note: nullable DateTime? values are boxed as DateTime (the underlying type) when
/// stored in ActionArguments, so the single "is DateTime" check handles both.
/// </summary>
public sealed class UtcDateTimeActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var key in context.ActionArguments.Keys.ToList())
        {
            if (context.ActionArguments[key] is DateTime dt && dt.Kind != DateTimeKind.Utc)
            {
                context.ActionArguments[key] = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
