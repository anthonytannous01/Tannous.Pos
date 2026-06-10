using FluentValidation;

namespace Tannous.Pos.Application.Scheduling.Commands.ClockIn;

public class ClockInCommandValidator : AbstractValidator<ClockInCommand>
{
    public ClockInCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("BranchId is required.");
    }
}
