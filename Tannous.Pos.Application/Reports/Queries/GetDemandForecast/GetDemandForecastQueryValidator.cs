using FluentValidation;

namespace Tannous.Pos.Application.Reports.Queries.GetDemandForecast;

/// <summary>
/// Past target dates are deliberately allowed (historical forecasts for review are valid);
/// the shared ValidationBehavior throws on any failure, so a non-rejecting "warning" is not
/// expressible here. Only degenerate values are rejected.
/// </summary>
public class GetDemandForecastQueryValidator : AbstractValidator<GetDemandForecastQuery>
{
    public GetDemandForecastQueryValidator()
    {
        RuleFor(q => q.TargetDate)
            .Must(d => d!.Value > new DateTime(2000, 1, 1) && d.Value < DateTime.UtcNow.AddYears(1))
            .When(q => q.TargetDate.HasValue)
            .WithMessage("TargetDate must be a realistic date (after 2000 and within one year ahead).");
    }
}
