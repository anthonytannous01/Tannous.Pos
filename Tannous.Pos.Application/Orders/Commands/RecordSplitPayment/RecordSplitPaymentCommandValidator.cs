using FluentValidation;

namespace Tannous.Pos.Application.Orders.Commands.RecordSplitPayment;

public class RecordSplitPaymentCommandValidator : AbstractValidator<RecordSplitPaymentCommand>
{
    public RecordSplitPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.TotalWays).InclusiveBetween(2, 20);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method).NotEmpty();
    }
}
