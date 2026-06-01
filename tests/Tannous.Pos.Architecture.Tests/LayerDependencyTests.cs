using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Enforces project reference / namespace dependency direction aligned with .cursorrules and ARCHITECTURE_SUMMARY.md.
/// </summary>
public class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(Tannous.Pos.Domain.Entities.Order).Assembly;
    private static readonly Assembly Application = typeof(Tannous.Pos.Application.Orders.Commands.CreateOrder.CreateOrderCommand).Assembly;
    private static readonly Assembly Infrastructure = typeof(Tannous.Pos.Infrastructure.Data.PosDbContext).Assembly;
    private static readonly Assembly WebApi = typeof(Tannous.Pos.WebApi.Controllers.AuthController).Assembly;

    [Fact]
    public void Domain_must_not_reference_WebApi_or_Infrastructure()
    {
        var result = Types.InAssembly(Domain)
            .Should()
            .NotHaveDependencyOn("Tannous.Pos.WebApi")
            .And()
            .NotHaveDependencyOn("Tannous.Pos.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatViolations("Domain → WebApi/Infrastructure", result));
    }

    [Fact]
    public void Application_must_not_reference_WebApi()
    {
        var result = Types.InAssembly(Application)
            .Should()
            .NotHaveDependencyOn("Tannous.Pos.WebApi")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatViolations("Application → WebApi", result));
    }

    [Fact]
    public void Infrastructure_must_not_reference_WebApi()
    {
        var result = Types.InAssembly(Infrastructure)
            .Should()
            .NotHaveDependencyOn("Tannous.Pos.WebApi")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatViolations("Infrastructure → WebApi", result));
    }

    private static string FormatViolations(string rule, TestResult result)
    {
        if (result.IsSuccessful)
            return string.Empty;

        var failing = result.FailingTypes.Select(t => t.FullName ?? t.Name);
        return $"{rule} violated. Failing types: {string.Join(", ", failing)}";
    }
}
