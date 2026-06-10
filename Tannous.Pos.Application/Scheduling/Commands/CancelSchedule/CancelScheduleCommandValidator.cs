using FluentValidation;

namespace Tannous.Pos.Application.Scheduling.Commands.CancelSchedule;

public class CancelScheduleCommandValidator : AbstractValidator<CancelScheduleCommand>
{
    public CancelScheduleCommandValidator()
    {
        RuleFor(x => x.ScheduleId)
            .NotEmpty().WithMessage("ScheduleId is required.");
    }
}
