using FluentValidation;
using Tannous.Pos.Application.Shifts.Commands.CloseShift;

namespace Tannous.Pos.Application.Shifts.Commands.CloseShift;

public class CloseShiftCommandValidator : AbstractValidator<CloseShiftCommand>
{
    public CloseShiftCommandValidator()
    {
        RuleFor(x => x.ShiftId)
            .NotEmpty()
            .WithMessage("Shift ID is required");

        RuleFor(x => x.ClosingCount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Closing count must be zero or greater");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage("Note must not exceed 500 characters");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required");
    }
}
