using System.Text.Json;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Validates <c>governance/concurrency-upgrade-plan.json</c> for future concurrency rollout: entities, hot paths, duplicates, and alignment with hotspot or migration-readiness inventories.
/// </summary>
public class ConcurrencyUpgradeGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void Concurrency_upgrade_plan_aligns_with_repo_hotspots_and_readiness_baseline()
    {
        var root = RepoRoot();
        var planPath = Path.Combine(root, "governance", "concurrency-upgrade-plan.json");
        var hotspotPath = Path.Combine(root, "governance", "concurrency-hotspots.json");
        var readinessPath = Path.Combine(root, "governance", "concurrency-migration-readiness-baseline.json");

        Assert.True(File.Exists(planPath), $"Missing {planPath}");
        Assert.True(File.Exists(hotspotPath), $"Missing {hotspotPath}");
        Assert.True(File.Exists(readinessPath), $"Missing {readinessPath}");

        using var plan = JsonDocument.Parse(File.ReadAllText(planPath));
        using var hs = JsonDocument.Parse(File.ReadAllText(hotspotPath));
        using var rs = JsonDocument.Parse(File.ReadAllText(readinessPath));

        var hotspotNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in hs.RootElement.GetProperty("entities").EnumerateArray())
            hotspotNames.Add(e.GetProperty("name").GetString()!);

        var readinessShortNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in rs.RootElement.GetProperty("entities").EnumerateArray())
        {
            var tn = e.GetProperty("typeName").GetString()!;
            var shortName = tn.Contains('.', StringComparison.Ordinal) ? tn[(tn.LastIndexOf('.') + 1)..] : tn;
            readinessShortNames.Add(shortName);
        }

        var seenPlanNames = new HashSet<string>(StringComparer.Ordinal);
        var violations = new List<string>();

        foreach (var ent in plan.RootElement.GetProperty("entities").EnumerateArray())
        {
            var name = ent.GetProperty("name").GetString()!;
            if (!seenPlanNames.Add(name))
                violations.Add($"Duplicate entity name `{name}` in concurrency-upgrade-plan.json — each entity must appear once.");

            var entityFile = Path.Combine(root, "Tannous.Pos.Domain", "Entities", $"{name}.cs");
            if (!File.Exists(entityFile))
                violations.Add($"Upgrade-plan entity `{name}` has no Domain file at `{entityFile}`.");

            if (!hotspotNames.Contains(name) && !readinessShortNames.Contains(name))
                violations.Add(
                    $"Upgrade-plan entity `{name}` must also appear in concurrency-hotspots.json OR concurrency-migration-readiness-baseline.json (inventory alignment).");

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
                        $"Upgrade-plan `{name}` references hotPath `{token}` but no matching .cs file was found (excluding bin/obj).");
            }

            foreach (var prop in new[] { "recommendedToken", "risk", "migrationComplexity", "notes" })
            {
                if (!ent.TryGetProperty(prop, out var p) || string.IsNullOrWhiteSpace(p.GetString()))
                    violations.Add($"Upgrade-plan entity `{name}` is missing or empty required property `{prop}`.");
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }
}
