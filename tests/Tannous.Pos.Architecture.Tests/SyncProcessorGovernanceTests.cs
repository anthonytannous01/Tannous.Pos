using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Classifies sync push <c>Process*</c> methods. Grouped output: money → inventory → replay → placeholder → customer.
/// Fails only when a <b>money-affecting</b> placeholder lacks GOVERNANCE / RISK + Placeholder success + replay/idempotency wording,
/// or an <b>inventory-affecting</b> processor lacks replay/idempotency commentary.
/// </summary>
public class SyncProcessorGovernanceTests
{
    private static readonly HashSet<string> PlaceholderProcessors = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreateCustomer", "OpenShift"
    };

    private static readonly HashSet<string> InventoryProcessors = new(StringComparer.OrdinalIgnoreCase)
    {
        "RecordWastage", "AdjustInventory"
    };

    /// <summary>Money-affecting processors expected to run via MediatR command delegation.</summary>
    private static readonly HashSet<string> MoneyAffectingRuntimeProcessors = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreateOrder", "FinalizeOrder", "CashDrop"
    };

    private static readonly HashSet<string> CustomerAffectingProcessors = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreateCustomer"
    };

    private readonly ITestOutputHelper _output;

    public SyncProcessorGovernanceTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void SyncController_Process_methods_governance_classification()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Controllers", "SyncController.cs");
        var text = File.ReadAllText(path);

        var methodRx = new Regex(SyncControllerProcessBodyExtractor.ProcessMethodDeclarationPattern, RegexOptions.Compiled);
        var matches = methodRx.Matches(text).Cast<Match>().OrderBy(m => m.Index).ToList();
        Assert.NotEmpty(matches);

        var violations = new List<string>();
        var byBucket = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["inventory-affecting"] = new(),
            ["money-affecting"] = new(),
            ["customer-affecting"] = new(),
            ["placeholder/stub"] = new(),
            ["replay-sensitive"] = new()
        };

        for (var i = 0; i < matches.Count; i++)
        {
            var m = matches[i];
            var name = m.Groups[2].Value;
            var start = m.Index;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            var body = text.Substring(start, end - start);

            var isPlaceholder = PlaceholderProcessors.Contains(name);
            var isInventory = InventoryProcessors.Contains(name);
            var isMoneyRuntime = MoneyAffectingRuntimeProcessors.Contains(name);
            var isCustomer = CustomerAffectingProcessors.Contains(name);
            var hasGovernanceRisk = body.Contains("GOVERNANCE / RISK", StringComparison.Ordinal);
            var hasPlaceholderWording = body.Contains("Placeholder success", StringComparison.OrdinalIgnoreCase);
            var hasReplayWording = Regex.IsMatch(body, @"\b(replay|idempotency)\b", RegexOptions.IgnoreCase);

            var flags = new List<string>();
            if (isPlaceholder) flags.Add("placeholder/stub");
            if (isInventory) flags.Add("inventory-affecting");
            if (isMoneyRuntime) flags.Add("money-affecting");
            if (isCustomer) flags.Add("customer-affecting");
            if (isPlaceholder || isInventory || isMoneyRuntime) flags.Add("replay-sensitive");

            foreach (var f in flags.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (byBucket.TryGetValue(f, out var list))
                    list.Add($"Process{name}");
            }

            _output.WriteLine(
                $"Process{name}: {string.Join(", ", flags)} | GOVERNANCE/RISK={hasGovernanceRisk} | PlaceholderWording={hasPlaceholderWording} | replay/idempotency={hasReplayWording}");

            if (isInventory && !hasReplayWording)
                violations.Add($"Process{name}: inventory-affecting body must document replay/idempotency risk (comment).");

            if (isMoneyRuntime)
            {
                var hasMediatrDelegation = body.Contains("_mediator.Send", StringComparison.Ordinal);
                if (!hasGovernanceRisk || !hasMediatrDelegation || !hasReplayWording || hasPlaceholderWording)
                {
                    violations.Add(
                        $"Process{name}: money-affecting runtime path must include GOVERNANCE / RISK, MediatR delegation, replay/idempotency commentary, and no placeholder success wording (GOV={hasGovernanceRisk}, MED={hasMediatrDelegation}, RP={hasReplayWording}, PH={hasPlaceholderWording}).");
                }
            }
        }

        _output.WriteLine("--- Grouped processors (governance order) ---");
        var bucketOrder = new[]
        {
            "money-affecting",
            "inventory-affecting",
            "replay-sensitive",
            "placeholder/stub",
            "customer-affecting"
        };
        foreach (var key in bucketOrder)
        {
            if (!byBucket.TryGetValue(key, out var list))
                continue;
            _output.WriteLine($"{key}: {string.Join(", ", list.Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal))}");
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }
}
