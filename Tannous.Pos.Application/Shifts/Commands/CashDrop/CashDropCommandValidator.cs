using FluentValidation;
using Tannous.Pos.Application.Shifts.Commands.CashDrop;

namespace Tannous.Pos.Application.Shifts.Commands.CashDrop;

public class CashDropCommandValidator : AbstractValidator<CashDropCommand>
{
    public CashDropCommandValidator()
    {
        RuleFor(x => x.ShiftId)
            .NotEmpty()
            .WithMessage("Shift ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Cash drop amount must be greater than zero");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage("Note must not exceed 500 characters");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required");
    }
}
