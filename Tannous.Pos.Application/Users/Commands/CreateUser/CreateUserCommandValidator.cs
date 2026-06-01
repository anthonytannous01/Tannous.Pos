using FluentValidation;
using Tannous.Pos.Application.Users.Commands.CreateUser;
using Tannous.Pos.Domain.Enums;
using System.Text.RegularExpressions;

namespace Tannous.Pos.Application.Users.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9._-]+$", RegexOptions.Compiled);
    private static readonly Regex PasswordRegex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", RegexOptions.Compiled);

    public CreateUserCommandValidator()
    {
        RuleFor(x => x.User.Username)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters")
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters")
            .Matches(UsernameRegex).WithMessage("Username can only contain letters, numbers, dots, underscores, and hyphens");

        RuleFor(x => x.User.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .When(x => !string.IsNullOrWhiteSpace(x.User.Email));

        RuleFor(x => x.User.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(PasswordRegex).WithMessage("Password must contain at least one uppercase letter, one lowercase letter, and one number");

        RuleFor(x => x.User.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.User.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.User.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(BeValidRole).WithMessage("Invalid role. Must be one of: Owner, Manager, Cashier, Kitchen, Waiter");
    }

    private static bool BeValidRole(string role)
    {
        return Enum.TryParse<Role>(role, ignoreCase: true, out _);
    }
}


