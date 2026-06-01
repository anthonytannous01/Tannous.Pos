using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class ReconciliationSecurityGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Reconciliation_controller_requires_admin_authorization()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "Internal", "OperationalAuditReconciliationController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("[Authorize(Policy = \"Admin\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[AllowAnonymous]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolution_notes_length_is_capped()
    {
        var constants = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Sync", "ReconciliationWorkflowConstants.cs"));
        var service = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "SyncConflictReconciliationService.cs"));
        Assert.Contains("MaxResolutionNotesLength", constants, StringComparison.Ordinal);
        Assert.Contains("reconciliation notes truncated", service, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sync_conflict_dto_has_safe_projection_without_stack_or_payload()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Sync", "SyncConflictItemDto.cs");
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("StackTrace", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Payload", text, StringComparison.Ordinal);
        Assert.Contains("ResolutionStatus", text, StringComparison.Ordinal);
    }
}
