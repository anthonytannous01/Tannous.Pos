using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Lightweight source checks for rate limit wiring on auth and high-risk mutations.
/// </summary>
public class RateLimitingGovernanceSourceTests
{
    [Fact]
    public void Program_configures_rate_limiter_policies()
    {
        var program = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Program.cs");
        var text = File.ReadAllText(program);
        Assert.Contains("AuthBurst", text, StringComparison.Ordinal);
        Assert.Contains("MutationsPerDevice", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthController_login_and_refresh_use_auth_burst_limiter()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "AuthController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("EnableRateLimiting(\"AuthBurst\")", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OrdersController_finalize_uses_mutation_rate_limiter()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "OrdersController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("MutationsPerDevice", text, StringComparison.Ordinal);
    }
}
