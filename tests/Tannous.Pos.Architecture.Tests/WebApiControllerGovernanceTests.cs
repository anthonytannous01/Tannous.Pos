using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Infrastructure.Data;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Guardrails for WebApi controller construction and authorization surface.
/// Existing violations are allowlisted; NEW violations fail CI.
/// </summary>
public class WebApiControllerGovernanceTests
{
    private static readonly Type PosDbContextType = typeof(PosDbContext);

    /// <summary>
    /// Controllers permitted to inject PosDbContext until migrated to Application layer.
    /// Empty: all controllers have been migrated off direct PosDbContext injection.
    /// Any new PosDbContext injection into any controller now fails CI with no exemptions.
    /// </summary>
    private static readonly HashSet<string> PosDbContextControllerAllowlist = new(StringComparer.Ordinal)
    {
    };

    private static IEnumerable<Type> ControllerTypes() =>
        typeof(Tannous.Pos.WebApi.Controllers.AuthController).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller", StringComparison.Ordinal));

    [Fact]
    public void New_controllers_must_not_inject_PosDbContext_outside_allowlist()
    {
        var violations = new List<string>();
        foreach (var type in ControllerTypes())
        {
            foreach (var ctor in type.GetConstructors())
            {
                var hasDb = ctor.GetParameters().Any(p => p.ParameterType == PosDbContextType);
                if (!hasDb)
                    continue;

                if (!PosDbContextControllerAllowlist.Contains(type.Name))
                    violations.Add($"{type.FullName} constructor injects PosDbContext (not allowlisted). Remove or migrate to MediatR + Application.");
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Controllers_except_Auth_must_have_class_level_Authorize()
    {
        var violations = new List<string>();
        foreach (var type in ControllerTypes())
        {
            if (type.Name == "AuthController")
                continue;

            var hasApiController = type.GetCustomAttribute<ApiControllerAttribute>() != null;
            if (!hasApiController)
                continue;

            // Controllers that are intentionally [AllowAnonymous] cannot also require [Authorize];
            // their public surface is governed by AllowAnonymousGovernanceTests' allowlist instead.
            if (type.GetCustomAttribute<AllowAnonymousAttribute>() != null)
                continue;

            var hasAuthorize = type.GetCustomAttribute<AuthorizeAttribute>() != null;
            if (!hasAuthorize)
                violations.Add($"{type.FullName} missing [Authorize] on controller class (AuthController exempt).");
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }
}
