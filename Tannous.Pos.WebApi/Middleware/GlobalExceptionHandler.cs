using System.Linq;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Sync;

namespace Tannous.Pos.WebApi.Middleware;

/// <summary>
/// Central handler for unhandled exceptions: RFC 7807 ProblemDetails + correlation id.
/// Does not replace controller-handled outcomes or FluentValidation automatic API behavior for model binding.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IServiceScopeFactory _scopeFactory;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _environment = environment;
        _scopeFactory = scopeFactory;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items["CorrelationId"]?.ToString()
            ?? httpContext.Response.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? "unknown";

        if (exception is DbUpdateConcurrencyException concurrencyEx)
        {
            // GOVERNANCE / RISK: global DbUpdateConcurrencyException — 409 ProblemDetails; no automatic retry or merge.
            var affectedTypes = FormatConcurrencyEntityTypeNames(concurrencyEx);
            _logger.LogWarning(
                concurrencyEx,
                "Optimistic concurrency conflict (DbUpdateConcurrencyException). CorrelationId={CorrelationId} Path={Path} AffectedEntityTypes={AffectedEntityTypes}",
                correlationId,
                httpContext.Request.Path.Value,
                affectedTypes);

            await RecordConcurrencyConflictBestEffortAsync(
                httpContext,
                correlationId,
                affectedTypes,
                cancellationToken);
        }

        var (statusCode, title, detail, extensions) = MapException(exception);

        if (statusCode >= 500)
        {
            _logger.LogError(exception,
                "Unhandled exception. CorrelationId={CorrelationId} Path={Path}",
                correlationId,
                httpContext.Request.Path.Value);
        }
        else if (exception is ValidationException)
        {
            _logger.LogWarning(
                "Validation failed. CorrelationId={CorrelationId} Path={Path}",
                correlationId,
                httpContext.Request.Path.Value);
        }
        else if (!(exception is DbUpdateConcurrencyException))
        {
            _logger.LogInformation(
                "Handled exception mapped to {StatusCode}. CorrelationId={CorrelationId} Type={ExceptionType} Path={Path}",
                statusCode,
                correlationId,
                exception.GetType().Name,
                httpContext.Request.Path.Value);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json; charset=utf-8";

        var problem = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

        problem.Extensions["correlationId"] = correlationId;

        foreach (var kv in extensions)
            problem.Extensions[kv.Key] = kv.Value!;

        if (_environment.IsDevelopment())
        {
            problem.Extensions["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name;
            problem.Extensions["exceptionMessage"] = exception.Message;
            problem.Extensions["stackTrace"] = exception.StackTrace ?? string.Empty;
        }

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>
    /// Best-effort aggregate names for operator logs only (not sent on ProblemDetails).
    /// </summary>
    private async Task RecordConcurrencyConflictBestEffortAsync(
        HttpContext httpContext,
        string correlationId,
        string affectedTypes,
        CancellationToken cancellationToken)
    {
        try
        {
            var deviceId = httpContext.Request.Headers["Device-Id"].FirstOrDefault();
            await using var scope = _scopeFactory.CreateAsyncScope();
            var recorder = scope.ServiceProvider.GetRequiredService<ISyncConflictRecorder>();
            await recorder.RecordAsync(
                new SyncConflictRecordRequest
                {
                    DeviceId = deviceId,
                    EntityType = string.IsNullOrWhiteSpace(affectedTypes) ? "Unknown" : affectedTypes,
                    ConflictType = SyncConflictTypes.ConcurrencyConflict,
                    Reason = "DbUpdateConcurrencyException (global handler)",
                    CorrelationId = correlationId
                },
                cancellationToken);

            var auditRecorder = scope.ServiceProvider.GetRequiredService<IOperationalAuditRecorder>();
            await auditRecorder.RecordAsync(
                new OperationalAuditRecordRequest
                {
                    Category = OperationalAuditCategories.Concurrency,
                    Action = OperationalAuditActions.ConcurrencyConflict,
                    EntityType = string.IsNullOrWhiteSpace(affectedTypes) ? "Unknown" : affectedTypes,
                    DeviceId = deviceId,
                    CorrelationId = correlationId,
                    Severity = OperationalAuditSeverity.Critical,
                    Summary = "Concurrency conflict (global handler)",
                    Metadata = new Dictionary<string, object?> { ["path"] = httpContext.Request.Path.Value }
                },
                cancellationToken);
        }
        catch (Exception recordEx)
        {
            _logger.LogWarning(
                recordEx,
                "Sync reconciliation observability: failed to record concurrency conflict (best-effort). CorrelationId={CorrelationId}",
                correlationId);
        }
    }

    private static string FormatConcurrencyEntityTypeNames(DbUpdateConcurrencyException ex)
    {
        try
        {
            return string.Join(
                ", ",
                ex.Entries
                    .Select(e => e.Metadata?.ClrType?.Name ?? e.Entity.GetType().Name)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal));
        }
        catch
        {
            return "unknown";
        }
    }

    private static (int StatusCode, string Title, string Detail, Dictionary<string, object?> Extensions) MapException(
        Exception exception)
    {
        switch (exception)
        {
            case ValidationException vex:
                var errors = vex.Errors
                    .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "_" : e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => (object)g.Select(e => e.ErrorMessage).ToArray());
                return (
                    StatusCodes.Status400BadRequest,
                    "Validation failed",
                    "One or more validation errors occurred.",
                    new Dictionary<string, object?> { ["errors"] = errors });

            case UnauthorizedAccessException unauthorized:
                return (
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized",
                    string.IsNullOrWhiteSpace(unauthorized.Message) ? "Access denied." : unauthorized.Message,
                    new Dictionary<string, object?>());

            case KeyNotFoundException knf:
                return (
                    StatusCodes.Status404NotFound,
                    "Not found",
                    string.IsNullOrWhiteSpace(knf.Message) ? "The requested resource was not found." : knf.Message,
                    new Dictionary<string, object?>());

            case ArgumentException arg:
                return (
                    StatusCodes.Status400BadRequest,
                    "Bad request",
                    string.IsNullOrWhiteSpace(arg.Message) ? "The request was invalid." : arg.Message,
                    new Dictionary<string, object?>());

            case InvalidOperationException io:
                return (
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    string.IsNullOrWhiteSpace(io.Message) ? "The operation could not be completed." : io.Message,
                    new Dictionary<string, object?>());

            case DbUpdateConcurrencyException:
                return (
                    StatusCodes.Status409Conflict,
                    "Concurrency conflict",
                    "The record was modified by another request. Refresh the entity and retry.",
                    new Dictionary<string, object?>());

            default:
                return (
                    StatusCodes.Status500InternalServerError,
                    "Server error",
                    "An error occurred while processing your request.",
                    new Dictionary<string, object?>());
        }
    }
}
