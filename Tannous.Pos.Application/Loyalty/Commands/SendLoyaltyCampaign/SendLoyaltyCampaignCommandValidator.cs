using FluentValidation;

namespace Tannous.Pos.Application.Loyalty.Commands.SendLoyaltyCampaign;

public class SendLoyaltyCampaignCommandValidator : AbstractValidator<SendLoyaltyCampaignCommand>
{
    public SendLoyaltyCampaignCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Campaign name is required.")
            .MaximumLength(100).WithMessage("Campaign name must not exceed 100 characters.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Campaign message is required.")
            .MaximumLength(500).WithMessage("Campaign message must not exceed 500 characters.");

        RuleFor(x => x.TargetSegment)
            .IsInEnum().WithMessage("Target segment is invalid.");
    }
}
