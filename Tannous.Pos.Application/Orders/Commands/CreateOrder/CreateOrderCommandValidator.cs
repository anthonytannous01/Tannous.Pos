using FluentValidation;
using Tannous.Pos.Application.DTOs.Orders;

namespace Tannous.Pos.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Order.OrderType)
            .IsInEnum()
            .WithMessage("OrderType must be a valid value");

        RuleFor(x => x.Order.OrderLines)
            .NotEmpty()
            .WithMessage("Order must have at least one line item");

        RuleForEach(x => x.Order.OrderLines)
            .SetValidator(new OrderLineDtoValidator());

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");
    }
}

public class OrderLineDtoValidator : AbstractValidator<OrderLineDto>
{
    public OrderLineDtoValidator()
    {
        RuleFor(x => x.MenuItemId)
            .NotEmpty()
            .WithMessage("MenuItemId is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("UnitPrice must be greater than or equal to 0");
    }
}
