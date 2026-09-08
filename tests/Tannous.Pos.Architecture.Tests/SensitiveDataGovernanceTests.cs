using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Guards the decision that this system stores no health data about customers.
///
/// The customer record used to carry an <c>Allergies</c> free-text field. Nobody had used it, and
/// it was expensive: a GDPR special category, a "Health info" declaration on Play, and a paragraph
/// in the privacy policy, all for a field the restaurant does not need. It was removed rather than
/// hidden, because a field the API still accepts is still collected no matter what the UI shows.
///
/// This test exists because the field was easy to add and its cost was invisible at the point of
/// adding it. Reintroducing it should be a deliberate act that fails the build first, not a
/// convenient afternoon.
/// </summary>
public class SensitiveDataGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    /// <summary>
    /// Migrations are history and must not be edited: the original column and the migration that
    /// drops it both live there permanently.
    /// </summary>
    private static readonly string[] ScannedProjects =
    {
        "Tannous.Pos.Domain",
        "Tannous.Pos.Application",
        "Tannous.Pos.Infrastructure",
        "Tannous.Pos.WebApi"
    };

    private static IEnumerable<string> SourceFiles(string project)
    {
        var root = Path.Combine(RepoRoot(), project);
        if (!Directory.Exists(root)) yield break;

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var normalized = file.Replace('\\', '/');
            if (normalized.Contains("/bin/", StringComparison.Ordinal)) continue;
            if (normalized.Contains("/obj/", StringComparison.Ordinal)) continue;
            if (normalized.Contains("/Migrations/", StringComparison.Ordinal)) continue;
            yield return file;
        }
    }

    [Fact]
    public void No_customer_health_field_is_reintroduced()
    {
        var offenders = new List<string>();

        foreach (var project in ScannedProjects)
        foreach (var file in SourceFiles(project))
        {
            var text = File.ReadAllText(file);
            if (text.Contains("Allergies", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Allergen", StringComparison.OrdinalIgnoreCase))
            {
                offenders.Add(Path.GetRelativePath(RepoRoot(), file));
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Customer health data was removed deliberately (see SensitiveDataGovernanceTests). " +
            "Reintroducing it means a GDPR special category and a Play 'Health info' declaration. " +
            "Found in: " + string.Join(", ", offenders));
    }

    [Fact]
    public void Customer_entity_still_has_the_ordinary_contact_fields()
    {
        // Pins the scope of the removal: it took out the health field, not the record around it.
        // Without this, deleting Customer.cs entirely would make the test above pass.
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Domain", "Entities", "Customer.cs");
        Assert.True(File.Exists(path), $"Missing {path}");

        var text = File.ReadAllText(path);
        Assert.Contains("public string? Email", text, StringComparison.Ordinal);
        Assert.Contains("public string? Phone", text, StringComparison.Ordinal);
        Assert.Contains("public string? Notes", text, StringComparison.Ordinal);
    }
}
