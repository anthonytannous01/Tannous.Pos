using System.Text.RegularExpressions;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Source anchors for targeted sync runtime execution hardening.
/// Ensures CreateOrder/FinalizeOrder/CashDrop use MediatR and remain wrapped by durable replay coordinator.
/// </summary>
public class SyncRuntimeExecutionGovernanceTests
{
    [Fact]
    public void Push_switch_wraps_targeted_operations_with_durable_replay_coordinator()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);

        foreach (var opType in new[] { "CreateOrder", "FinalizeOrder", "CashDrop" })
        {
            Assert.Contains($"case \"{opType}\":", text, StringComparison.Ordinal);
        }

        Assert.Contains("_replayCoordinator.ExecuteAsync(", text, StringComparison.Ordinal);
        Assert.Contains("request.DeviceId", text, StringComparison.Ordinal);
        Assert.Contains("operation.OpId", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Push_switch_wraps_inventory_mutation_operations_with_durable_replay_coordinator()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);

        foreach (var opType in new[] { "AdjustInventory", "RecordWastage" })
        {
            Assert.Contains($"case \"{opType}\":", text, StringComparison.Ordinal);
        }

        Assert.Contains("() => ProcessAdjustInventory", text, StringComparison.Ordinal);
        Assert.Contains("() => ProcessRecordWastage", text, StringComparison.Ordinal);
        Assert.True(
            Regex.IsMatch(text, @"(?s)case\s+""AdjustInventory""\s*:[\s\S]{0,600}?_replayCoordinator\.ExecuteAsync"),
            "AdjustInventory must be wrapped by durable replay coordinator.");
        Assert.True(
            Regex.IsMatch(text, @"(?s)case\s+""RecordWastage""\s*:[\s\S]{0,600}?_replayCoordinator\.ExecuteAsync"),
            "RecordWastage must be wrapped by durable replay coordinator.");
    }

    [Fact]
    public void Targeted_processors_delegate_to_mediatr_instead_of_synthetic_server_ids()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);

        foreach (var proc in new[] { "ProcessCreateOrder", "ProcessFinalizeOrder", "ProcessCashDrop" })
        {
            Assert.Contains(proc, text, StringComparison.Ordinal);
        }

        Assert.Contains("_mediator.Send", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Guid.NewGuid().ToString()", text, StringComparison.Ordinal);
    }
}
