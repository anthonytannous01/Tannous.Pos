using FluentValidation;
using Tannous.Pos.Application.PurchaseOrders.Commands.SubmitPurchaseOrder;

namespace Tannous.Pos.Application.PurchaseOrders.Commands.SubmitPurchaseOrder;

public class SubmitPurchaseOrderCommandValidator : AbstractValidator<SubmitPurchaseOrderCommand>
{
    public SubmitPurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Purchase order ID is required");
    }
}
