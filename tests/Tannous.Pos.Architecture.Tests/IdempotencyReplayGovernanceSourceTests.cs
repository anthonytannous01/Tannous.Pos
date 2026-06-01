using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Source anchors for idempotency store scoping and finalize replay visibility.
/// </summary>
public class IdempotencyReplayGovernanceSourceTests
{
    [Fact]
    public void IdempotencyStore_scopes_lookup_by_key_and_endpoint_before_insert()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "IdempotencyStore.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("r.Key == key", text, StringComparison.Ordinal);
        Assert.Contains("r.Endpoint == endpoint", text, StringComparison.Ordinal);
        Assert.Contains("FirstOrDefaultAsync", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FinalizeOrderCommandHandler_short_circuits_paid_and_logs_replay_governance()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("OrderStatus.Paid", text, StringComparison.Ordinal);
        Assert.Contains("Order already finalized", text, StringComparison.Ordinal);
        Assert.Contains("IdempotencyKey", text, StringComparison.Ordinal);
        Assert.Contains("Finalize governance", text, StringComparison.Ordinal);
        Assert.Contains("Finalize idempotency observability: duplicate finalize or replay attempt", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncController_retains_replay_governance_strings_and_mediatr_dispatch_on_targeted_processors()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("GOVERNANCE / RISK", text, StringComparison.Ordinal);
        Assert.Contains("duplicate operationId within same batch", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sync replay visibility:", text, StringComparison.Ordinal);
        Assert.Contains("ProcessOpenShift", text, StringComparison.Ordinal);
        Assert.Contains("Placeholder success", text, StringComparison.Ordinal);
        Assert.Contains("ProcessCreateOrder", text, StringComparison.Ordinal);
        Assert.Contains("ProcessFinalizeOrder", text, StringComparison.Ordinal);
        Assert.Contains("ProcessCashDrop", text, StringComparison.Ordinal);
        Assert.Contains("_mediator.Send", text, StringComparison.Ordinal);
        Assert.Contains("replay", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("idempotency", text, StringComparison.OrdinalIgnoreCase);
    }
}
