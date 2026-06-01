using System.Reflection;
using FluentValidation;
using MediatR;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Naming conventions for CQRS types in Application (per .cursorrules / CURSOR_RULES.md).
/// </summary>
public class ApplicationNamingGovernanceTests
{
    private static readonly Assembly Application = typeof(Tannous.Pos.Application.Orders.Commands.CreateOrder.CreateOrderCommand).Assembly;

    private static IEnumerable<Type> PublicConcreteTypesInApplication() =>
        Application.GetTypes()
            .Where(t => t.IsPublic && t.IsClass && !t.IsAbstract && t.Namespace != null && t.Namespace.StartsWith("Tannous.Pos.Application", StringComparison.Ordinal));

    private static bool ImplementsGenericIRequest(Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

    private static bool IsExcludedFromRequestNaming(Type type) =>
        type.Namespace?.StartsWith("Tannous.Pos.Application.Behaviors", StringComparison.Ordinal) == true;

    [Fact]
    public void IRequest_implementers_must_be_named_Command_or_Query()
    {
        var violations = new List<string>();
        foreach (var type in PublicConcreteTypesInApplication())
        {
            if (IsExcludedFromRequestNaming(type))
                continue;

            if (!ImplementsGenericIRequest(type))
                continue;

            if (type.Name.EndsWith("Command", StringComparison.Ordinal) || type.Name.EndsWith("Query", StringComparison.Ordinal))
                continue;

            violations.Add($"{type.FullName} implements IRequest<> but name does not end with Command or Query.");
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void IRequestHandler_implementers_must_end_with_Handler()
    {
        var violations = new List<string>();
        foreach (var type in PublicConcreteTypesInApplication())
        {
            var iface = type.GetInterfaces().FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
            if (iface == null)
                continue;

            if (!type.Name.EndsWith("Handler", StringComparison.Ordinal))
                violations.Add($"{type.FullName} implements IRequestHandler<,> but name does not end with Handler.");
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void AbstractValidator_subclasses_must_end_with_Validator()
    {
        var violations = new List<string>();
        foreach (var type in PublicConcreteTypesInApplication())
        {
            if (!IsAbstractValidatorSubclass(type))
                continue;

            if (!type.Name.EndsWith("Validator", StringComparison.Ordinal))
                violations.Add($"{type.FullName} inherits AbstractValidator<> but name does not end with Validator.");
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    private static bool IsAbstractValidatorSubclass(Type type)
    {
        for (var b = type.BaseType; b != null; b = b.BaseType)
        {
            if (b.IsGenericType && b.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
                return true;
        }

        return false;
    }
}
