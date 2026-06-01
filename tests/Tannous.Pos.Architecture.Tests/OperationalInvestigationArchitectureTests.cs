using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalInvestigationArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Investigation_dto_has_no_public_setters()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Application",
            "OperationalInvestigation",
            "OperationalOrderInvestigationDto.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("public set;", text, StringComparison.Ordinal);
        Assert.Contains("{ get; init; }", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Investigation_aggregation_is_static_class()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Application",
            "OperationalInvestigation",
            "OperationalInvestigationAggregation.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("public static class OperationalInvestigationAggregation", text, StringComparison.Ordinal);
        Assert.DoesNotContain("async", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Investigation_service_does_not_reference_PosDbContext()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalInvestigationService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalEntityStatusService", text, StringComparison.Ordinal);
        Assert.Contains("IOperationalAuditQueryService",   text, StringComparison.Ordinal);
        Assert.Contains("IOperationalBriefingService",     text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext",              text, StringComparison.Ordinal);
        Assert.DoesNotContain("BoundedFifoSnapshotStore",  text, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.WhenAll",              text, StringComparison.Ordinal);
    }

    [Fact]
    public void Investigation_service_uses_sequential_awaits_with_configure_await()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalInvestigationService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("GetOrderStatusAsync",          text, StringComparison.Ordinal);
        Assert.Contains("GetOrderAuditHighlightsAsync", text, StringComparison.Ordinal);
        Assert.Contains("GetBriefingSummaryAsync",    text, StringComparison.Ordinal);
        Assert.Contains("ConfigureAwait(false)",      text, StringComparison.Ordinal);
        Assert.Contains("GetOrderAuditHighlightsAsync(orderId, 5", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Investigation_controller_is_get_only_with_versioned_internal_route()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditInvestigationController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains(
            "api/v{version:apiVersion}/internal/operational-audit",
            text, StringComparison.Ordinal);
        Assert.Contains("investigation/order/{orderId:guid}", text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("HttpGet", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPost",   text, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPut",    text, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpDelete", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Device_investigation_dto_has_no_public_setters()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Application",
            "OperationalInvestigation",
            "OperationalDeviceInvestigationDto.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("public set;", text, StringComparison.Ordinal);
        Assert.Contains("{ get; init; }", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Investigation_controller_exposes_device_investigation_endpoint()
    {
        var file = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi", "Controllers", "Internal",
            "OperationalAuditInvestigationController.cs");
        Assert.True(File.Exists(file), $"Controller file not found: {file}");
        var text = File.ReadAllText(file);
        Assert.Contains("GetDeviceInvestigation", text);
        Assert.Contains("investigation/device/", text);
    }
}
