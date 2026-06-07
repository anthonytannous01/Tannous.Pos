using FluentValidation;

namespace Tannous.Pos.Application.Kiosk.Commands.CreateKioskOrder;

public class CreateKioskOrderCommandValidator : AbstractValidator<CreateKioskOrderCommand>
{
    public CreateKioskOrderCommandValidator()
    {
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Order must contain at least one item.");
        RuleFor(x => x.CustomerName).MaximumLength(100).When(x => x.CustomerName != null);
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes != null);
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
