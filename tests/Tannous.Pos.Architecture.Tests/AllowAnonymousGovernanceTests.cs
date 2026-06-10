using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// AllowAnonymous widens the attack surface; restrict to explicitly approved controllers.
/// Allowlist: AuthController (login/refresh) + MenuController (public QR digital menu).
/// </summary>
public class AllowAnonymousGovernanceTests
{
    /// <summary>
    /// Controllers approved for AllowAnonymous. Add here only for genuinely public surfaces
    /// with documented justification.
    /// </summary>
    private static readonly HashSet<string> AllowAnonymousAllowlist = new()
    {
        "AuthController",      // login + token refresh
        "MenuController",      // QR digital menu — public customer-facing read-only
        "FeedbackController",  // POST /feedback — public submission (no PII required)
        "KioskController",     // self-ordering kiosk — customer-facing, unauthenticated
        "DeliveryWebhookController" // inbound delivery platform webhooks — authenticated by HMAC signature
    };

    [Fact]
    public void AllowAnonymous_is_only_used_on_approved_controllers()
    {
        var violations = new List<string>();
        foreach (var type in typeof(Tannous.Pos.WebApi.Controllers.AuthController).Assembly
                     .GetTypes()
                     .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller", StringComparison.Ordinal)))
        {
            // Check class-level [AllowAnonymous]
            if (type.GetCustomAttribute<AllowAnonymousAttribute>() != null
                && !AllowAnonymousAllowlist.Contains(type.Name))
            {
                violations.Add($"{type.FullName} has class-level [AllowAnonymous] — not in allowlist.");
            }

            // Check method-level [AllowAnonymous]
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (method.DeclaringType != type) continue;
                if (method.GetCustomAttribute<NonActionAttribute>() != null) continue;
                if (method.GetCustomAttribute<AllowAnonymousAttribute>() == null) continue;

                if (!AllowAnonymousAllowlist.Contains(type.Name))
                    violations.Add($"{type.FullName}.{method.Name} uses [AllowAnonymous] — not in allowlist.");
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }
}
