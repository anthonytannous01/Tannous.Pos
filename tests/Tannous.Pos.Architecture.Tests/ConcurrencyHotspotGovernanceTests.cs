using System.Text.Json;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Validates <c>governance/concurrency-hotspots.json</c> against the repo: entity files, hot-path sources, and cross-link with migration-readiness baseline.
/// </summary>
public class ConcurrencyHotspotGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Concurrency_hotspot_inventory_aligns_with_repo_and_readiness_baseline()
    {
        var root = RepoRoot();
        var hotspotPath = Path.Combine(root, "governance", "concurrency-hotspots.json");
        var readinessPath = Path.Combine(root, "governance", "concurrency-migration-readiness-baseline.json");
        Assert.True(File.Exists(hotspotPath), $"Missing {hotspotPath}");
        Assert.True(File.Exists(readinessPath), $"Missing {readinessPath}");

        using var hs = JsonDocument.Parse(File.ReadAllText(hotspotPath));
        using var rs = JsonDocument.Parse(File.ReadAllText(readinessPath));

        var readinessShortNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in rs.RootElement.GetProperty("entities").EnumerateArray())
        {
            var tn = e.GetProperty("typeName").GetString()!;
            var shortName = tn.Contains('.', StringComparison.Ordinal) ? tn[(tn.LastIndexOf('.') + 1)..] : tn;
            readinessShortNames.Add(shortName);
        }

        var hotspotNames = new HashSet<string>(StringComparer.Ordinal);
        var violations = new List<string>();

        foreach (var ent in hs.RootElement.GetProperty("entities").EnumerateArray())
        {
            var name = ent.GetProperty("name").GetString()!;
            hotspotNames.Add(name);

            var entityFile = Path.Combine(root, "Tannous.Pos.Domain", "Entities", $"{name}.cs");
            if (!File.Exists(entityFile))
                violations.Add($"Hotspot entity `{name}` has no Domain file at `{entityFile}`.");

            if (!readinessShortNames.Contains(name))
                violations.Add(
                    $"Hotspot entity `{name}` is not listed in concurrency-migration-readiness-baseline.json (typeName should end with `.{name}`).");

            foreach (var hp in ent.GetProperty("hotPaths").EnumerateArray())
            {
                var token = hp.GetString()!;
                var matches = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                    .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    .Where(p => Path.GetFileNameWithoutExtension(p).Equals(token, StringComparison.OrdinalIgnoreCase)
                        || Path.GetFileNameWithoutExtension(p).Contains(token, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matches.Count == 0)
                    violations.Add(
                        $"Hotspot `{name}` references hotPath `{token}` but no matching .cs file was found under the repo (excluding bin/obj).");
            }
        }

        foreach (var rn in readinessShortNames)
        {
            if (!hotspotNames.Contains(rn))
                violations.Add(
                    $"Entity `{rn}` appears in concurrency-migration-readiness-baseline.json but is missing from concurrency-hotspots.json — extend the hotspot inventory or remove the readiness entry.");
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }
}
