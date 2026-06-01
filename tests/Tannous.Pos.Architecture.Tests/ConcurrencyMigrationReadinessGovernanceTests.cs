using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Optimistic concurrency migration readiness: surfaces timestamp / rowversion / token / updated-style fields per entity.
/// Fails only when baseline <c>expectRetentionSubstrings</c> lists a substring that disappears from inspected source.
/// </summary>
public class ConcurrencyMigrationReadinessGovernanceTests
{
    private readonly ITestOutputHelper _output;

    public ConcurrencyMigrationReadinessGovernanceTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Key_entities_concurrency_migration_readiness_visibility()
    {
        var repoRoot = ObservabilitySourceGovernanceTests.RepoRoot();
        var baselinePath = Path.Combine(repoRoot, "governance", "concurrency-migration-readiness-baseline.json");
        Assert.True(File.Exists(baselinePath), $"Missing {baselinePath}");

        using var doc = JsonDocument.Parse(File.ReadAllText(baselinePath));
        var entities = doc.RootElement.GetProperty("entities");
        var violations = new List<string>();
        var baseEntityPath = Path.Combine(repoRoot, "Tannous.Pos.Domain", "Common", "BaseEntity.cs");
        var baseText = File.Exists(baseEntityPath) ? File.ReadAllText(baseEntityPath) : string.Empty;

        foreach (var e in entities.EnumerateArray())
        {
            var typeName = e.GetProperty("typeName").GetString()!;
            var rel = e.GetProperty("sourceFile").GetString()!.Replace('/', Path.DirectorySeparatorChar);
            var entityPath = Path.Combine(repoRoot, rel);
            Assert.True(File.Exists(entityPath), $"Missing entity source {entityPath}");
            var entityText = File.ReadAllText(entityPath);

            var includeBase = e.TryGetProperty("includeBaseEntity", out var ib) && ib.GetBoolean();
            var combined = includeBase ? entityText + Environment.NewLine + baseText : entityText;

            var hasTimestamp = Regex.IsMatch(entityText, @"\[\s*Timestamp\s*\]", RegexOptions.IgnoreCase);
            var hasRowVersion = Regex.IsMatch(entityText, @"\bRowVersion\b", RegexOptions.IgnoreCase);
            var hasConcurrencyFluent = entityText.Contains("IsConcurrencyToken", StringComparison.Ordinal);
            var hasUpdatedStyle = Regex.IsMatch(combined, @"\b(UpdatedAt|LastUpdated|ModifiedAt)\b", RegexOptions.IgnoreCase);

            _output.WriteLine(
                $"{typeName}: timestamp={hasTimestamp}, rowVersion={hasRowVersion}, concurrencyFluent={hasConcurrencyFluent}, updatedAtStyle={hasUpdatedStyle} (entity+base when applicable)");

            if (e.TryGetProperty("expectRetentionSubstrings", out var expect) && expect.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in expect.EnumerateArray())
                {
                    var sub = s.GetString()!;
                    if (!combined.Contains(sub, StringComparison.Ordinal))
                        violations.Add($"{typeName}: baseline retention substring missing: `{sub}`");
                }
            }
        }

        Assert.Empty(violations);
    }
}
