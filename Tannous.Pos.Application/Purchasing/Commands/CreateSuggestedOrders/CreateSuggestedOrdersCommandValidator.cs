using FluentValidation;

namespace Tannous.Pos.Application.Purchasing.Commands.CreateSuggestedOrders;

public class CreateSuggestedOrdersCommandValidator : AbstractValidator<CreateSuggestedOrdersCommand>
{
    public CreateSuggestedOrdersCommandValidator()
    {
        RuleFor(c => c.ForecastDays)
            .InclusiveBetween(1, 30)
            .WithMessage("ForecastDays must be between 1 and 30.");
    }
}
