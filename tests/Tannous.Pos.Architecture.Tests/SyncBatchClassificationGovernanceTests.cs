using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Governance anchors for internal sync batch classification and operational observability (not on mobile wire).
/// </summary>
public class SyncBatchClassificationGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void SyncOperationOutcomeClassification_enum_values_are_stable()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Sync", "SyncOperationOutcomeClassification.cs");
        var text = File.ReadAllText(path);
        foreach (var name in new[]
        {
            "Success",
            "ReplayShortCircuited",
            "ValidationFailure",
            "Conflict",
            "PartialBatchRisk",
            "PlaceholderOperation",
            "RetryableFailure",
            "NonRetryableFailure"
        })
        {
            Assert.Contains(name, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SyncController_push_has_batch_observability_and_classification_anchors()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("SyncPushBatchTelemetry", text, StringComparison.Ordinal);
        Assert.Contains("SyncOperationOutcomeClassifier.Classify", text, StringComparison.Ordinal);
        Assert.Contains("Sync batch observability: operation classified", text, StringComparison.Ordinal);
        Assert.Contains("Sync batch observability: push batch summary", text, StringComparison.Ordinal);
        Assert.Contains("Sync batch observability: partial batch classification", text, StringComparison.Ordinal);
        Assert.Contains("Sync replay visibility: partial application / mixed batch outcomes", text, StringComparison.Ordinal);
        Assert.Contains("batchTelemetry.Record(classification, result, operation.Type)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncOperationOutcomeClassifier_classifies_placeholder_operations()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Sync", "SyncOperationOutcomeClassifier.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("CreateCustomer", text, StringComparison.Ordinal);
        Assert.Contains("OpenShift", text, StringComparison.Ordinal);
        Assert.Contains("PlaceholderOperation", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DurableSyncReplayCoordinator_marks_replay_short_circuit_scope()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "DurableSyncReplayCoordinator.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("ISyncPushOperationExecutionScope", text, StringComparison.Ordinal);
        Assert.Contains("MarkReplayShortCircuited", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_registers_sync_push_execution_scope()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Program.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("ISyncPushOperationExecutionScope", text, StringComparison.Ordinal);
        Assert.Contains("SyncPushOperationExecutionScope", text, StringComparison.Ordinal);
    }
}
