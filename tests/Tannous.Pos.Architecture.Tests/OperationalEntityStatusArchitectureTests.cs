using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalEntityStatusArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Entity_status_service_injects_query_service_not_dbcontext()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalEntityStatusService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("IOperationalAuditQueryService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDbContext",            text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalEquilibriumService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperationalBriefingService",    text, StringComparison.Ordinal);
    }

    [Fact]
    public void Entity_status_response_dtos_contain_no_record_arrays()
    {
        var orderDto = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Application",
            "OperationalEntityStatus",
            "OperationalOrderStatusDto.cs");
        var deviceDto = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Application",
            "OperationalEntityStatus",
            "OperationalDeviceStatusDto.cs");

        foreach (var path in new[] { orderDto, deviceDto })
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("IReadOnlyList", text, StringComparison.Ordinal);
            Assert.DoesNotContain("List<",         text, StringComparison.Ordinal);
            Assert.DoesNotContain("[]",            text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Audit_query_service_uses_distinct_for_severity_not_multiple_any()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalAuditQueryService.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("GetOrderAuditSummaryAsync",  text, StringComparison.Ordinal);
        Assert.Contains("GetDeviceAuditSummaryAsync", text, StringComparison.Ordinal);
        Assert.Contains(".Distinct()",                text, StringComparison.Ordinal);
        Assert.Contains("SyncConflictRecords",        text, StringComparison.Ordinal);
        Assert.Contains("SyncOperationReceipts",      text, StringComparison.Ordinal);
    }

    [Fact]
    public void Entity_status_aggregation_is_deterministic()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Application",
            "OperationalEntityStatus",
            "OperationalEntityStatusAggregation.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("System.Reflection", text, StringComparison.Ordinal);
        Assert.DoesNotContain("dynamic",           text, StringComparison.Ordinal);
        Assert.DoesNotContain("async",             text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext",         text, StringComparison.Ordinal);
    }

    [Fact]
    public void Entity_status_controller_is_get_only_with_versioned_internal_route()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditEntityStatusController.cs");
        var text = File.ReadAllText(path);

        Assert.Contains(
            "api/v{version:apiVersion}/internal/operational-audit/entity-status",
            text, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.Contains("HttpGet",   text, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPost",   text, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPut",    text, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpDelete", text, StringComparison.Ordinal);
    }
}
