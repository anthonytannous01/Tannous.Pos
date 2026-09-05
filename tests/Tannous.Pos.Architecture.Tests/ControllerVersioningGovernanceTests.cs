using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Prefer versioned <c>api/v{version:apiVersion}/...</c> routes for new controllers.
/// Legacy controllers remain allowlisted until migrated.
/// </summary>
public class ControllerVersioningGovernanceTests
{
    private const string VersionToken = "v{version:apiVersion}";

    /// <summary>
    /// Controllers permitted to have no versioned route template. Empty as of 2026-09-05.
    ///
    /// It previously held four entries. Three of them (Inventory, Suppliers, Reports) had been
    /// versioned long before, so the allowlist was granting exemptions nobody needed and would
    /// have silently permitted a regression on any of them. DevicesController was versioned in
    /// Step 126. An entry here is an exemption from a rule, so it should be removed the moment
    /// it stops being true.
    /// </summary>
    private static readonly HashSet<string> UnversionedRouteAllowlist = new(StringComparer.Ordinal);

    private static IEnumerable<Type> ControllerTypes() =>
        typeof(Tannous.Pos.WebApi.Controllers.AuthController).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller", StringComparison.Ordinal));

    [Fact]
    public void New_controllers_should_include_versioned_api_route()
    {
        var violations = new List<string>();
        foreach (var type in ControllerTypes())
        {
            var routes = type.GetCustomAttributes<RouteAttribute>(inherit: true)
                .Select(a => a.Template ?? string.Empty)
                .ToList();

            if (routes.Count == 0)
                continue;

            var hasVersioned = routes.Any(r => r.Contains(VersionToken, StringComparison.Ordinal));
            if (hasVersioned)
                continue;

            if (UnversionedRouteAllowlist.Contains(type.Name))
                continue;

            violations.Add(
                $"{type.FullName} has no [Route] containing '{VersionToken}'. Add a versioned route or extend {nameof(UnversionedRouteAllowlist)} with team review.");
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }
}
