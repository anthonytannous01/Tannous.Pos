using FluentValidation;

namespace Tannous.Pos.Application.Kds.Queries.GetKdsPerformance;

public class GetKdsPerformanceQueryValidator : AbstractValidator<GetKdsPerformanceQuery>
{
    public GetKdsPerformanceQueryValidator()
    {
        RuleFor(q => q.To)
            .GreaterThan(q => q.From)
            .WithMessage("To must be after From.");

        RuleFor(q => q)
            .Must(q => (q.To - q.From).TotalDays <= 90)
            .WithMessage("Date range must not exceed 90 days.");
    }
}
