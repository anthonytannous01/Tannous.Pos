using Tannous.Pos.Application.OperationalReconciliation;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalReconciliationSystemArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void ReconciliationSystem_dto_has_no_public_setters()
    {
        var file = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Application", "OperationalReconciliation",
            "OperationalReconciliationSystemDto.cs");
        Assert.True(File.Exists(file), $"DTO file not found: {file}");
        var text = File.ReadAllText(file);
        Assert.DoesNotContain("public set;", text);
        Assert.Contains("{ get; init; }", text);
    }

    [Fact]
    public void ReconciliationSystem_health_enum_has_expected_values()
    {
        var values = Enum.GetNames(typeof(ReconciliationSystemHealth));
        Assert.Contains("Stable", values);
        Assert.Contains("Pressured", values);
        Assert.Contains("Backlogged", values);
        Assert.Contains("Critical", values);
    }

    [Fact]
    public void ReconciliationSystem_aggregation_is_static_class()
    {
        var type = typeof(OperationalReconciliationSystemAggregation);
        Assert.True(type.IsAbstract && type.IsSealed,
            $"{type.Name} must be a static class (abstract + sealed).");
    }

    [Fact]
    public void ReconciliationSystem_service_does_not_reference_PosDbContext()
    {
        var file = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure", "Services",
            "OperationalReconciliationSystemService.cs");
        Assert.True(File.Exists(file), $"Service file not found: {file}");
        var text = File.ReadAllText(file);
        Assert.DoesNotContain("PosDbContext", text);
    }

    [Fact]
    public void ReconciliationSystem_controller_does_not_reference_PosDbContext()
    {
        var file = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi", "Controllers", "Internal",
            "OperationalAuditReconciliationSystemController.cs");
        Assert.True(File.Exists(file), $"Controller file not found: {file}");
        var text = File.ReadAllText(file);
        Assert.DoesNotContain("PosDbContext", text);
    }
}
