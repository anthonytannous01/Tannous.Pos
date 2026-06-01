using FluentValidation;

namespace Tannous.Pos.Application.Users.Commands.SetUserStatus;

public class SetUserStatusCommandValidator : AbstractValidator<SetUserStatusCommand>
{
    public SetUserStatusCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}


