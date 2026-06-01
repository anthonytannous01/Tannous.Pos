using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Source anchors for finalize transaction boundaries (rollback on failure).
/// </summary>
public class FinalizeOrderTransactionGovernanceSourceTests
{
    [Fact]
    public void FinalizeOrderCommandHandler_uses_explicit_transaction_commit_and_rollback()
    {
        var repoRoot = ObservabilitySourceGovernanceTests.RepoRoot();
        var path = Path.Combine(repoRoot, "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        Assert.True(File.Exists(path), $"Missing {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("BeginTransactionAsync", text, StringComparison.Ordinal);
        Assert.Contains("joining existing database transaction", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CommitAsync", text, StringComparison.Ordinal);
        Assert.Contains("RollbackAsync", text, StringComparison.Ordinal);
        Assert.Contains("AddMovementAsync", text, StringComparison.Ordinal);
        Assert.Contains("Inventory consistency observability:", text, StringComparison.Ordinal);
    }
}
