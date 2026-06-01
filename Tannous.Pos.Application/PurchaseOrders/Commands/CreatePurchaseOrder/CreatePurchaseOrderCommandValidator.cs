using FluentValidation;
using Tannous.Pos.Application.PurchaseOrders.Commands.CreatePurchaseOrder;
using Tannous.Pos.Application.DTOs.Suppliers;

namespace Tannous.Pos.Application.PurchaseOrders.Commands.CreatePurchaseOrder;

public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.PurchaseOrder.SupplierId)
            .NotEmpty()
            .WithMessage("Supplier ID is required");

        RuleFor(x => x.PurchaseOrder.ExpectedDeliveryDate)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.PurchaseOrder.ExpectedDeliveryDate.HasValue)
            .WithMessage("Expected delivery date must be in the future");

        RuleFor(x => x.PurchaseOrder.Lines)
            .NotEmpty()
            .WithMessage("Purchase order must have at least one line");

        RuleForEach(x => x.PurchaseOrder.Lines)
            .SetValidator(new CreatePurchaseOrderLineDtoValidator());
    }
}

public class CreatePurchaseOrderLineDtoValidator : AbstractValidator<CreatePurchaseOrderLineDto>
{
    public CreatePurchaseOrderLineDtoValidator()
    {
        RuleFor(x => x.IngredientId)
            .NotEmpty()
            .WithMessage("Ingredient ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Unit cost must be greater than or equal to zero");
    }
}
