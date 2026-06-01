using FluentValidation;
using Tannous.Pos.Application.Orders.Commands.FinalizeOrder;

namespace Tannous.Pos.Application.Orders.Commands.FinalizeOrder;

public class FinalizeOrderCommandValidator : AbstractValidator<FinalizeOrderCommand>
{
    public FinalizeOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required");

        RuleFor(x => x.Payments)
            .NotEmpty()
            .WithMessage("At least one payment is required");

        RuleForEach(x => x.Payments)
            .SetValidator(new PaymentDtoValidator());

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required");
    }
}

public class PaymentDtoValidator : AbstractValidator<PaymentDto>
{
    public PaymentDtoValidator()
    {
        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .WithMessage("Payment method is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Payment amount must be greater than zero");
    }
}
