using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Source anchors for runtime pipeline and financial governance (no behavior changes in app code beyond drift detection).
/// </summary>
public class RuntimeIntegrityAnchorsGovernanceTests
{
    [Fact]
    public void OrderFinancialGovernance_ComputeLegacyTaxOnSubtotal_uses_MidpointRounding_AwayFromZero()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.Application", "Orders", "OrderFinancialGovernance.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("MidpointRounding.AwayFromZero", text, StringComparison.Ordinal);
        Assert.Contains("ComputeLegacyTaxOnSubtotal", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalExceptionHandler_sets_ProblemDetails_Status_and_problem_json_content_type()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Middleware", "GlobalExceptionHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Status = statusCode", text, StringComparison.Ordinal);
        Assert.Contains("application/problem+json", text, StringComparison.Ordinal);
        Assert.Contains("DbUpdateConcurrencyException", text, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status409Conflict", text, StringComparison.Ordinal);
        Assert.Contains("Extensions[\"correlationId\"]", text, StringComparison.Ordinal);
        Assert.Contains("\"Concurrency conflict\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_uses_exception_handler_pipeline()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Program.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("UseExceptionHandler(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_uses_serilog_request_logging()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Program.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("UseSerilogRequestLogging(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_uses_correlation_id_middleware()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Program.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("UseMiddleware<CorrelationIdMiddleware>()", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_JwtBearer_sets_ClockSkew_explicitly()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Program.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("ClockSkew", text, StringComparison.Ordinal);
        Assert.Contains("TimeSpan.FromMinutes(1)", text, StringComparison.Ordinal);
    }
}
