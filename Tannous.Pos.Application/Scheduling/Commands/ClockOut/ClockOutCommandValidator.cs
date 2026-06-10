using FluentValidation;

namespace Tannous.Pos.Application.Scheduling.Commands.ClockOut;

public class ClockOutCommandValidator : AbstractValidator<ClockOutCommand>
{
    public ClockOutCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("BranchId is required.");

        RuleFor(x => x.BreakMinutes)
            .InclusiveBetween(0, 960)
            .When(x => x.BreakMinutes.HasValue)
            .WithMessage("BreakMinutes must be between 0 and 960.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.");
    }
}
