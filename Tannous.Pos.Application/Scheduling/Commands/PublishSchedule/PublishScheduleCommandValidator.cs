using FluentValidation;

namespace Tannous.Pos.Application.Scheduling.Commands.PublishSchedule;

public class PublishScheduleCommandValidator : AbstractValidator<PublishScheduleCommand>
{
    public PublishScheduleCommandValidator()
    {
        RuleFor(x => x.ScheduleIds)
            .NotEmpty().WithMessage("At least one schedule id is required.");

        RuleForEach(x => x.ScheduleIds)
            .NotEmpty().WithMessage("Schedule ids must be non-empty.");
    }
}
