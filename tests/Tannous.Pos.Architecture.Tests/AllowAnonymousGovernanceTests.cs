using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// AllowAnonymous widens the attack surface; restrict to auth endpoints.
/// </summary>
public class AllowAnonymousGovernanceTests
{
    [Fact]
    public void AllowAnonymous_is_only_used_on_AuthController()
    {
        var violations = new List<string>();
        foreach (var type in typeof(Tannous.Pos.WebApi.Controllers.AuthController).Assembly
                     .GetTypes()
                     .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller", StringComparison.Ordinal)))
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (method.DeclaringType != type)
                    continue;

                if (method.GetCustomAttribute<NonActionAttribute>() != null)
                    continue;

                if (method.GetCustomAttribute<AllowAnonymousAttribute>() == null)
                    continue;

                if (type.Name != "AuthController")
                    violations.Add($"{type.FullName}.{method.Name} uses [AllowAnonymous] outside AuthController.");
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }
}
