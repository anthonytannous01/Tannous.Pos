using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Tannous.Pos.WebApi.Filters;
using Xunit;
using Xunit.Abstractions;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Reflection + MVC descriptor visibility: mutating actions should carry <see cref="RequireDeviceIdFilter"/>
/// metadata (including global registration). Does not change runtime behavior.
/// </summary>
public class MutationDeviceIdFilterGovernanceTests : IClassFixture<GovernanceApiFactory>
{
    private readonly GovernanceApiFactory _factory;
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Controller.Action keys accepted without RequireDeviceIdFilter on the descriptor (expand only with governance review).
    /// Auth login/refresh are ignored for enforcement per policy; listed for documentation if filters ever diverge.
    /// </summary>
    private static readonly HashSet<string> AcceptedMutationKeysWithoutDeviceIdFilter = new(StringComparer.OrdinalIgnoreCase)
    {
        // Intentionally empty: global RequireDeviceIdFilter should cover all mutations. Add keys only if a deliberate exception is approved.
    };

    public MutationDeviceIdFilterGovernanceTests(GovernanceApiFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public void Mutating_actions_expose_RequireDeviceIdFilter_or_are_allowlisted()
    {
        _factory.CreateClient();
        var provider = _factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>();
        var violations = new List<string>();
        var inspected = new List<string>();

        foreach (var descriptor in provider.ActionDescriptors.Items.OfType<ControllerActionDescriptor>())
        {
            if (!IsMutationAction(descriptor))
                continue;

            if (ShouldIgnoreForDeviceIdGovernance(descriptor))
                continue;

            var key = $"{descriptor.ControllerName}.{descriptor.ActionName}";
            inspected.Add(key);

            if (ActionDescriptorHasRequireDeviceIdFilter(descriptor))
                continue;

            if (AcceptedMutationKeysWithoutDeviceIdFilter.Contains(key))
                continue;

            violations.Add(key);
        }

        var sb = new StringBuilder();
        sb.AppendLine("Mutation actions (POST/PUT/PATCH/DELETE) inspected for RequireDeviceIdFilter metadata:");
        foreach (var line in inspected.OrderBy(s => s, StringComparer.Ordinal))
            sb.AppendLine($"  {line}");
        _output.WriteLine(sb.ToString());

        if (violations.Count > 0)
        {
            _output.WriteLine("VIOLATIONS (missing RequireDeviceIdFilter on descriptor, not allowlisted):");
            foreach (var v in violations.OrderBy(s => s, StringComparer.Ordinal))
                _output.WriteLine($"  {v}");
        }

        Assert.Empty(violations);
    }

    private static bool ShouldIgnoreForDeviceIdGovernance(ControllerActionDescriptor d)
    {
        if (string.Equals(d.ControllerName, "Auth", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(d.ActionName, nameof(Tannous.Pos.WebApi.Controllers.AuthController.Login), StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(d.ActionName, nameof(Tannous.Pos.WebApi.Controllers.AuthController.RefreshToken), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (d.ControllerName.Contains("Health", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool IsMutationAction(ControllerActionDescriptor descriptor)
    {
        var method = descriptor.MethodInfo;
        foreach (var attr in method.GetCustomAttributes(inherit: true))
        {
            switch (attr)
            {
                case HttpPostAttribute:
                case HttpPutAttribute:
                case HttpPatchAttribute:
                case HttpDeleteAttribute:
                    return true;
                case AcceptVerbsAttribute verbs:
                    foreach (var v in verbs.HttpMethods)
                    {
                        if (IsMutationVerb(v))
                            return true;
                    }

                    break;
            }
        }

        return false;
    }

    private static bool IsMutationVerb(string verb) =>
        verb.Equals("POST", StringComparison.OrdinalIgnoreCase)
        || verb.Equals("PUT", StringComparison.OrdinalIgnoreCase)
        || verb.Equals("PATCH", StringComparison.OrdinalIgnoreCase)
        || verb.Equals("DELETE", StringComparison.OrdinalIgnoreCase);

    private static bool ActionDescriptorHasRequireDeviceIdFilter(ControllerActionDescriptor action)
    {
        foreach (var fd in action.FilterDescriptors)
        {
            if (fd.Filter is RequireDeviceIdFilter)
                return true;

            var impl = fd.Filter.GetType().GetProperty("ImplementationType", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(fd.Filter) as Type;
            if (impl == typeof(RequireDeviceIdFilter))
                return true;
        }

        return false;
    }
}
