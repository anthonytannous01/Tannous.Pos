using FluentValidation;

namespace Tannous.Pos.Application.Reports.Queries.GetSectionSales;

public class GetSectionSalesQueryValidator : AbstractValidator<GetSectionSalesQuery>
{
    public GetSectionSalesQueryValidator()
    {
        RuleFor(q => q.To)
            .GreaterThan(q => q.From)
            .WithMessage("To must be after From.");

        RuleFor(q => q)
            .Must(q => (q.To - q.From).TotalDays <= 366)
            .WithMessage("Date range must not exceed 366 days.");
    }
}
