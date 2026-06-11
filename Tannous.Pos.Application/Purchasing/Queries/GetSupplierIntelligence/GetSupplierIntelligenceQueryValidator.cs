using FluentValidation;

namespace Tannous.Pos.Application.Purchasing.Queries.GetSupplierIntelligence;

public class GetSupplierIntelligenceQueryValidator : AbstractValidator<GetSupplierIntelligenceQuery>
{
    public GetSupplierIntelligenceQueryValidator()
    {
        RuleFor(q => q.ForecastDays)
            .InclusiveBetween(1, 30)
            .WithMessage("ForecastDays must be between 1 and 30.");
    }
}
