using System.Reflection;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Domain repository interfaces should not spread to new controllers without an explicit allowlist
/// (prefer MediatR + Application handlers).
/// </summary>
public class WebApiRepositoryInjectionGovernanceTests
{
    private static readonly HashSet<string> RepositoryControllerAllowlist = new(StringComparer.Ordinal)
    {
        // CatalogController migrated to MediatR (Step 63). All controllers now use CQRS for data access.
        // This allowlist is intentionally empty — any future repository injection in a controller fails CI.
    };

    private static IEnumerable<Type> ControllerTypes() =>
        typeof(Tannous.Pos.WebApi.Controllers.AuthController).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller", StringComparison.Ordinal));

    [Fact]
    public void New_controllers_must_not_inject_Domain_repositories_outside_allowlist()
    {
        var violations = new List<string>();
        foreach (var type in ControllerTypes())
        {
            foreach (var ctor in type.GetConstructors())
            {
                foreach (var p in ctor.GetParameters())
                {
                    var n = p.ParameterType.Name;
                    if (!n.EndsWith("Repository", StringComparison.Ordinal))
                        continue;

                    if (!RepositoryControllerAllowlist.Contains(type.Name))
                        violations.Add($"{type.FullName} injects {p.ParameterType.FullName} (repository in WebApi). Migrate to CQRS or extend allowlist with team review.");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }
}
