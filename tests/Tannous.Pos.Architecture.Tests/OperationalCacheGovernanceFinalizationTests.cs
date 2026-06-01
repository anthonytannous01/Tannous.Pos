using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalCacheGovernanceFinalizationTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray()));

    private static string InfrastructureGlob() =>
        string.Join(
            "\n",
            Directory.EnumerateFiles(
                    Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure"),
                    "*.cs",
                    SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                            && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(File.ReadAllText));

    [Fact]
    public void Governance_finalization_endpoints_are_get_only_without_payloads()
    {
        var controller = Read(
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");

        Assert.Contains("[HttpGet(\"governance-audit\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"governance-consistency\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"survivability\")]", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", controller, StringComparison.Ordinal);
        Assert.DoesNotContain(".Value", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Governance_finalization_has_no_persistence_or_background_services()
    {
        var infra = InfrastructureGlob();
        var audit = Read("Tannous.Pos.Application", "Audit", "OperationalCacheGovernanceAuditBuilder.cs");

        Assert.DoesNotContain("IHostedService", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("BackgroundService", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("DbSet<", audit, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChangesAsync", audit, StringComparison.Ordinal);
        Assert.DoesNotContain("GC.GetTotalMemory(", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.Run(", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("PeriodicTimer", infra, StringComparison.Ordinal);
    }

    [Fact]
    public void Explainability_bounded_and_advisory_only()
    {
        var composer = Read("Tannous.Pos.Application", "Audit", "OperationalGovernanceExplainabilityComposer.cs");
        var governance = Read("Tannous.Pos.Application", "Audit", "OperationalCacheGovernanceFinalizationGovernance.cs");
        Assert.Contains("MaxExplainabilityItems", composer + governance, StringComparison.Ordinal);
        Assert.Contains("MaxReasonCodeLength", composer + governance, StringComparison.Ordinal);
        Assert.Contains(".Take(maxItems)", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void Consistency_validator_does_not_throw()
    {
        var text = Read("Tannous.Pos.Application", "Audit", "OperationalCacheGovernanceConsistencyValidator.cs");
        Assert.DoesNotContain("throw ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Governance_audit_builder_projects_without_envelope_values()
    {
        var text = Read("Tannous.Pos.Application", "Audit", "OperationalCacheGovernanceAuditBuilder.cs");
        Assert.DoesNotContain("OperationalDiagnosticsCacheEnvelope", text, StringComparison.Ordinal);
        Assert.Contains("OperationalCacheGovernanceAuditDto", text, StringComparison.Ordinal);
    }
}
