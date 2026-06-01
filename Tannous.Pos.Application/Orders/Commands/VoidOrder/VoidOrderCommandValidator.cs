using FluentValidation;
using Tannous.Pos.Application.Orders.Commands.VoidOrder;

namespace Tannous.Pos.Application.Orders.Commands.VoidOrder;

public class VoidOrderCommandValidator : AbstractValidator<VoidOrderCommand>
{
    public VoidOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Void reason is required and must not exceed 500 characters");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required");
    }
}
