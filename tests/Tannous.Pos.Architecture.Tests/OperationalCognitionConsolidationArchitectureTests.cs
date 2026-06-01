using Tannous.Pos.Application.OperationalCognition;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class OperationalCognitionConsolidationArchitectureTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Cognition_primitives_avoid_reflection_and_generic_engines()
    {
        var dir = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "OperationalCognition");
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("System.Reflection", text, StringComparison.Ordinal);
            Assert.DoesNotContain("dynamic", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MachineLearning", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Bounded_fifo_snapshot_store_is_thread_safe_and_not_persisted()
    {
        var path = Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalCognition",
            "BoundedFifoSnapshotStore.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("BoundedFifoSnapshotStore", text, StringComparison.Ordinal);
        Assert.Contains("lock (_gate)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Redis", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Cognition_snapshot_stores_delegate_to_bounded_fifo_primitive()
    {
        var storePaths = Directory
            .GetFiles(
                Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services"),
                "*SnapshotStore.cs",
                SearchOption.AllDirectories)
            .Where(p => !p.Contains("OperationalGovernanceSnapshotStore", StringComparison.Ordinal)
                     && !p.Contains("BoundedFifoSnapshotStore", StringComparison.Ordinal))
            .ToList();

        Assert.True(storePaths.Count >= 15);
        foreach (var path in storePaths)
        {
            var text = File.ReadAllText(path);
            Assert.Contains("BoundedFifoSnapshotStore", text, StringComparison.Ordinal);
            Assert.Contains("OperationalCognitionSnapshotLimits", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Queue<", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Continuity_phrasing_is_deterministic_and_shared()
    {
        Assert.Equal(
            "Equilibrium consistent across bounded continuity window",
            OperationalContinuityPhrasing.ConsistentAcrossBoundedWindow("Equilibrium"));

        Assert.Equal(
            "Escalation balance consistency improving",
            OperationalContinuityPhrasing.EscalationMomentumAlignment(
                "collapsing escalation momentum",
                "Escalation balance consistency improving",
                "Escalation balance consistency weakening",
                "Escalation balance consistency stable"));

        Assert.Equal(8, OperationalCognitionSnapshotLimits.MaxStoredSnapshots);
    }

    [Fact]
    public void Bounded_collections_enforce_deterministic_truncation()
    {
        var values = OperationalBoundedCollections.TakeBounded(Enumerable.Range(1, 20), 8);
        Assert.Equal(8, values.Count);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }, values);
    }
}
