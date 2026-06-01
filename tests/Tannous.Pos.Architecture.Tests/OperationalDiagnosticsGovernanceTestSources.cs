namespace Tannous.Pos.Architecture.Tests;

internal static class OperationalDiagnosticsGovernanceTestSources
{
    public static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    public static string DiagnosticsAndProjectionsSource()
    {
        var root = RepoRoot();
        var servicePath = Path.Combine(
            root,
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsCacheDiagnosticsService.cs");
        var projectionsDir = Path.Combine(
            root,
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsProjections");

        var parts = new List<string> { File.ReadAllText(servicePath) };
        if (Directory.Exists(projectionsDir))
        {
            parts.AddRange(
                Directory.EnumerateFiles(projectionsDir, "*.cs", SearchOption.TopDirectoryOnly)
                    .OrderBy(p => p, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
        }

        return string.Join('\n', parts);
    }

    public static string ExplainabilityComposerAndGovernanceSource() =>
        File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Application",
            "Audit",
            "OperationalGovernanceExplainabilityComposer.cs"))
        + File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Tannous.Pos.Application",
            "Audit",
            "OperationalCacheGovernanceFinalizationGovernance.cs"));
}
