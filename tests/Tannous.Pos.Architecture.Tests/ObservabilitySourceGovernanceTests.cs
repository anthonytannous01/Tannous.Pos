using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Source-level checks for observability contracts (no HTTP runtime required).
/// </summary>
public class ObservabilitySourceGovernanceTests
{
    public static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "Tannous.Pos.WebApi", "Tannous.Pos.WebApi.csproj");
            if (File.Exists(candidate))
                return dir;

            dir = Path.GetFullPath(Path.Combine(dir, ".."));
        }

        throw new InvalidOperationException("Could not locate repository root from test base directory.");
    }

    [Fact]
    public void GlobalExceptionHandler_sets_problem_details_correlation_extension()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Middleware", "GlobalExceptionHandler.cs");
        Assert.True(File.Exists(path), $"Missing {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("Extensions[\"correlationId\"]", text, StringComparison.Ordinal);
        Assert.Contains("application/problem+json", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalExceptionHandler_problem_details_includes_http_status()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Middleware", "GlobalExceptionHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("new ProblemDetails", text, StringComparison.Ordinal);
        Assert.Contains("Status = statusCode", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CorrelationIdMiddleware_propagates_header_and_items()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Middleware", "CorrelationIdMiddleware.cs");
        Assert.True(File.Exists(path), $"Missing {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("X-Correlation-ID", text, StringComparison.Ordinal);
        Assert.Contains("Items[\"CorrelationId\"]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Health_response_writer_emits_compact_json_shape()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "HealthChecks", "HealthCheckResponseWriter.cs");
        Assert.True(File.Exists(path), $"Missing {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("totalDurationMs", text, StringComparison.Ordinal);
        Assert.Contains("application/json", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalExceptionHandler_logs_include_correlationId_and_path_tokens()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Middleware", "GlobalExceptionHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("CorrelationId={CorrelationId}", text, StringComparison.Ordinal);
        Assert.Contains("Path={Path}", text, StringComparison.Ordinal);
    }

    [Fact]
    public void HealthCheckResponseWriter_payload_avoids_raw_exception_stack_in_shape()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "HealthChecks", "HealthCheckResponseWriter.cs");
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("StackTrace", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_wires_structured_request_logging()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs");
        Assert.True(File.Exists(path), $"Missing {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("UseSerilogRequestLogging", text, StringComparison.Ordinal);
        Assert.Contains("CorrelationIdMiddleware", text, StringComparison.Ordinal);
    }
}
