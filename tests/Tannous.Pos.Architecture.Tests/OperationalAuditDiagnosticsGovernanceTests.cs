using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalAuditDiagnosticsGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    private static string DiagnosticsControllerSource() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "Internal", "OperationalAuditDiagnosticsController.cs"));

    [Fact]
    public void Operational_audit_diagnostics_controller_uses_internal_route_prefix()
    {
        var text = DiagnosticsControllerSource();
        Assert.Contains("[Route(\"api/v{version:apiVersion}/internal/operational-audit\")]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Operational_audit_diagnostics_endpoints_are_get_only()
    {
        var text = DiagnosticsControllerSource();
        Assert.Contains("[HttpGet(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPatch(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Operational_audit_diagnostics_requires_admin_authorization()
    {
        var text = DiagnosticsControllerSource();
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[AllowAnonymous]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Operational_audit_diagnostics_declares_governance_internal_comments()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "Internal", "OperationalAuditDiagnosticsController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("GOVERNANCE / INTERNAL", text, StringComparison.Ordinal);
        Assert.Contains("no payload disclosure", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Not for customer/mobile", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Operational_audit_pagination_limits_are_defined()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalAuditQueryConstants.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("MaxPageSize = 200", text, StringComparison.Ordinal);
        Assert.Contains("DefaultPageSize", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Operational_audit_timeline_item_dto_has_no_payload_or_stack_fields()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Audit", "OperationalAuditTimelineItemDto.cs");
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("StackTrace", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MetadataJson", text, StringComparison.Ordinal);
        Assert.DoesNotContain("string Payload", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", text, StringComparison.Ordinal);
        Assert.Contains("TimestampUtc", text, StringComparison.Ordinal);
        Assert.Contains("Message", text, StringComparison.Ordinal);
    }
}
