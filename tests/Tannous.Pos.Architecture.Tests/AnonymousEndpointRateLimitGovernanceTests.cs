using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Every unauthenticated endpoint must be rate limited.
///
/// Anonymous callers cannot be partitioned by device or user, so the MutationsPerDevice policy
/// lumps them all into a single "unknown" bucket that one client can exhaust for everyone.
/// Before this guard existed, none of the anonymous controllers had any limit at all: kiosk order
/// placement and customer feedback created rows on every request from any caller that could reach
/// the API, and kiosk orders surface directly on the kitchen display.
/// </summary>
public class AnonymousEndpointRateLimitGovernanceTests
{
    private static IEnumerable<Type> ControllerTypes() =>
        typeof(Tannous.Pos.WebApi.Controllers.OrdersController).Assembly
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

    [Fact]
    public void Every_anonymous_endpoint_is_rate_limited()
    {
        var violations = new List<string>();

        foreach (var type in ControllerTypes())
        {
            var controllerAnonymous = type.GetCustomAttribute<AllowAnonymousAttribute>() != null;
            var controllerLimited = type.GetCustomAttribute<EnableRateLimitingAttribute>() != null;

            var actions = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && m.GetCustomAttribute<NonActionAttribute>() == null);

            foreach (var action in actions)
            {
                var isAnonymous = controllerAnonymous
                                  || action.GetCustomAttribute<AllowAnonymousAttribute>() != null;
                if (!isAnonymous)
                    continue;

                var isLimited = controllerLimited
                                || action.GetCustomAttribute<EnableRateLimitingAttribute>() != null;

                // A [DisableRateLimiting] on an anonymous endpoint is always a mistake.
                if (action.GetCustomAttribute<DisableRateLimitingAttribute>() != null)
                    isLimited = false;

                if (!isLimited)
                    violations.Add($"{type.Name}.{action.Name}");
            }
        }

        Assert.True(
            violations.Count == 0,
            "Anonymous endpoints without a rate limit policy: " + string.Join(", ", violations));
    }
}
