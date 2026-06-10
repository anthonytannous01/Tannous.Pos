using FluentValidation;

namespace Tannous.Pos.Application.Scheduling.Commands.CreateSchedule;

public class CreateScheduleCommandValidator : AbstractValidator<CreateScheduleCommand>
{
    public CreateScheduleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("BranchId is required.");

        RuleFor(x => x.ScheduledEnd)
            .GreaterThan(x => x.ScheduledStart)
            .WithMessage("ScheduledEnd must be after ScheduledStart.");

        RuleFor(x => x)
            .Must(x => (x.ScheduledEnd - x.ScheduledStart).TotalMinutes >= 30)
            .WithMessage("Scheduled slot must be at least 30 minutes.")
            .Must(x => (x.ScheduledEnd - x.ScheduledStart).TotalHours <= 16)
            .WithMessage("Scheduled slot must not exceed 16 hours.");

        RuleFor(x => x.ScheduledStart)
            .GreaterThan(_ => DateTime.UtcNow)
            .WithMessage("ScheduledStart must be in the future.");

        RuleFor(x => x.Position)
            .MaximumLength(100).WithMessage("Position must not exceed 100 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.");
    }
}
