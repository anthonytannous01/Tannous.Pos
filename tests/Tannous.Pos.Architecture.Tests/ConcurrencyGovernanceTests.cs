using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Visibility for optimistic concurrency preparation. Fails only when baseline lists expected CLR members that disappear.
/// </summary>
public class ConcurrencyGovernanceTests
{
    private readonly ITestOutputHelper _output;

    public ConcurrencyGovernanceTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Key_aggregates_concurrency_visibility_and_baseline_enforcement()
    {
        var repoRoot = ObservabilitySourceGovernanceTests.RepoRoot();
        var baselinePath = Path.Combine(repoRoot, "governance", "concurrency-entity-baseline.json");
        Assert.True(File.Exists(baselinePath), $"Missing {baselinePath}");

        using var doc = JsonDocument.Parse(File.ReadAllText(baselinePath));
        var entities = doc.RootElement.GetProperty("entities");
        var violations = new List<string>();

        foreach (var e in entities.EnumerateArray())
        {
            var typeName = e.GetProperty("typeName").GetString()!;
            var rel = e.GetProperty("sourceFile").GetString()!.Replace('/', Path.DirectorySeparatorChar);
            var path = Path.Combine(repoRoot, rel);
            Assert.True(File.Exists(path), $"Missing entity source {path}");
            var text = File.ReadAllText(path);

            var hasTimestamp = Regex.IsMatch(text, @"\[\s*Timestamp\s*\]", RegexOptions.IgnoreCase);
            var hasRowVersion = Regex.IsMatch(text, @"\bRowVersion\b", RegexOptions.IgnoreCase);
            var hasConcurrencyFluent = text.Contains("IsConcurrencyToken", StringComparison.Ordinal);
            var tokenSummary = hasTimestamp || hasRowVersion || hasConcurrencyFluent
                ? $"token-like markers (Timestamp={hasTimestamp}, RowVersion={hasRowVersion}, IsConcurrencyToken={hasConcurrencyFluent})"
                : "no concurrency token / rowversion / IsConcurrencyToken markers";

            _output.WriteLine($"{typeName}: {tokenSummary}");

            if (e.TryGetProperty("expectConcurrencyPropertyNames", out var expect) && expect.ValueKind == JsonValueKind.Array)
            {
                foreach (var nameEl in expect.EnumerateArray())
                {
                    var member = nameEl.GetString()!;
                    if (!Regex.IsMatch(text, $@"\b{Regex.Escape(member)}\b"))
                        violations.Add($"{typeName}: baseline expected concurrency member `{member}` missing from {rel}.");
                }
            }
        }

        if (doc.RootElement.TryGetProperty("notInDomain", out var nd))
        {
            foreach (var x in nd.EnumerateArray())
                _output.WriteLine($"Note: aggregate `{x.GetString()}` not modeled as entity in Domain (visibility only).");
        }

        Assert.Empty(violations);
    }
}
