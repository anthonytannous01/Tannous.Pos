using FluentValidation;
using Tannous.Pos.Application.GoodsReceipts.Commands.CreateGoodsReceipt;
using Tannous.Pos.Application.DTOs.Suppliers;

namespace Tannous.Pos.Application.GoodsReceipts.Commands.CreateGoodsReceipt;

public class CreateGoodsReceiptCommandValidator : AbstractValidator<CreateGoodsReceiptCommand>
{
    public CreateGoodsReceiptCommandValidator()
    {
        RuleFor(x => x.GoodsReceipt.ReceiptDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.GoodsReceipt.ReceiptDate != default)
            .WithMessage("Receipt date cannot be in the future");

        RuleFor(x => x.GoodsReceipt.Lines)
            .NotEmpty()
            .WithMessage("Goods receipt must have at least one line");

        RuleForEach(x => x.GoodsReceipt.Lines)
            .SetValidator(new CreateGoodsReceiptLineDtoValidator());
    }
}

public class CreateGoodsReceiptLineDtoValidator : AbstractValidator<CreateGoodsReceiptLineDto>
{
    public CreateGoodsReceiptLineDtoValidator()
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
