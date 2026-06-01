using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Governance anchors for integration Docker/Testcontainers diagnostics (local infrastructure only).
/// </summary>
public class IntegrationEnvironmentGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void DockerEnvironmentDiagnostics_contains_integration_observability_anchors()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Integration", "Infrastructure", "DockerEnvironmentDiagnostics.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Integration environment observability: docker available", text, StringComparison.Ordinal);
        Assert.Contains("Integration environment observability: docker unavailable", text, StringComparison.Ordinal);
        Assert.Contains("Integration environment observability: testcontainer startup", text, StringComparison.Ordinal);
        Assert.Contains("Integration environment observability: postgres ready", text, StringComparison.Ordinal);
        Assert.Contains("EnsureDockerAvailableAsync", text, StringComparison.Ordinal);
    }

    [Fact]
    public void IntegrationPostgresFixture_uses_collection_fixture_and_shared_container()
    {
        var collection = Path.Combine(RepoRoot(), "Tannous.Pos.Integration", "Infrastructure", "IntegrationCollection.cs");
        var fixture = Path.Combine(RepoRoot(), "Tannous.Pos.Integration", "Infrastructure", "IntegrationPostgresFixture.cs");
        var basePath = Path.Combine(RepoRoot(), "Tannous.Pos.Integration", "IntegrationTestBase.cs");

        var collectionText = File.ReadAllText(collection);
        Assert.Contains("ICollectionFixture<IntegrationPostgresFixture>", collectionText, StringComparison.Ordinal);

        var fixtureText = File.ReadAllText(fixture);
        Assert.Contains("PostgreSqlBuilder", fixtureText, StringComparison.Ordinal);
        Assert.Contains("AllocateDatabaseAsync", fixtureText, StringComparison.Ordinal);

        var baseText = File.ReadAllText(basePath);
        Assert.Contains("IntegrationPostgresFixture", baseText, StringComparison.Ordinal);
        Assert.Contains("Skip.If", baseText, StringComparison.Ordinal);
    }

    [Fact]
    public void IntegrationDockerUnavailableException_includes_remediation_guidance()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Integration", "Infrastructure", "IntegrationDockerUnavailableException.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Docker Desktop", text, StringComparison.Ordinal);
        Assert.Contains("dockerDesktopLinuxEngine", text, StringComparison.Ordinal);
        Assert.Contains("TANNOUS_INTEGRATION_SKIP_WITHOUT_DOCKER", text, StringComparison.Ordinal);
    }
}
