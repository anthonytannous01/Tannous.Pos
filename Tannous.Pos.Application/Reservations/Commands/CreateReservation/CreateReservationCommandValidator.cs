using FluentValidation;

namespace Tannous.Pos.Application.Reservations.Commands.CreateReservation;

public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().MaximumLength(100);

        RuleFor(x => x.CustomerPhone)
            .MaximumLength(50).When(x => x.CustomerPhone != null);

        RuleFor(x => x.PartySize)
            .GreaterThan(0).WithMessage("Party size must be at least 1.");

        RuleFor(x => x.ReservationDateTime)
            .GreaterThan(DateTime.UtcNow).WithMessage("Reservation must be in the future.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null);
    }
}
