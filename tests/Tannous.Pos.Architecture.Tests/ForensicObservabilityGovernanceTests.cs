using Xunit;

namespace Tannous.Pos.Architecture.Tests;

public class ForensicObservabilityGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Forensic_observability_anchors_exist_in_service_and_controller()
    {
        var service = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "OperationalForensicSnapshotService.cs"));
        var controller = File.ReadAllText(Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "Internal", "OperationalAuditForensicExportController.cs"));

        Assert.Contains("Operational forensic observability: forensic snapshot generated", service, StringComparison.Ordinal);
        Assert.Contains("Operational forensic observability: forensic conflict export executed", service, StringComparison.Ordinal);
        Assert.Contains("Operational forensic observability: forensic timeline aggregation executed", service, StringComparison.Ordinal);
        Assert.Contains("Operational forensic observability: forensic metadata sanitized", service, StringComparison.Ordinal);
        Assert.Contains("Operational forensic observability: forensic authorization path", controller, StringComparison.Ordinal);
    }
}
