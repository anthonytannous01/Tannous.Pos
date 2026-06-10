using FluentValidation;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Delivery.Commands.IngestChannelOrder;

public class IngestChannelOrderCommandValidator : AbstractValidator<IngestChannelOrderCommand>
{
    public IngestChannelOrderCommandValidator()
    {
        RuleFor(x => x.Channel)
            .Must(c => c == DeliveryChannel.Toters
                    || c == DeliveryChannel.Talabat
                    || c == DeliveryChannel.Wolt)
            .WithMessage("Channel must be an external delivery platform (Toters, Talabat, or Wolt).");

        RuleFor(x => x.Payload)
            .NotNull().WithMessage("Payload is required.");

        RuleFor(x => x.Payload.ExternalOrderId)
            .NotEmpty().WithMessage("ExternalOrderId is required.")
            .When(x => x.Payload != null);

        RuleFor(x => x.Payload.CustomerName)
            .NotEmpty().WithMessage("Customer name is required.")
            .When(x => x.Payload != null);

        RuleFor(x => x.Payload.DeliveryAddress)
            .NotEmpty().WithMessage("Delivery address is required.")
            .When(x => x.Payload != null);

        RuleFor(x => x.Payload.Lines)
            .NotEmpty().WithMessage("At least one order line is required.")
            .When(x => x.Payload != null);
    }
}
