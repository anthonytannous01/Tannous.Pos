using FluentValidation;

namespace Tannous.Pos.Application.Feedback.Commands.SubmitFeedback;

public class SubmitFeedbackCommandValidator : AbstractValidator<SubmitFeedbackCommand>
{
    public SubmitFeedbackCommandValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(1000).When(x => x.Comment != null);

        RuleFor(x => x.Category)
            .InclusiveBetween(0, 5).WithMessage("Invalid feedback category.");
    }
}
