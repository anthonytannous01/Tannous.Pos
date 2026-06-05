using FluentValidation;

namespace Tannous.Pos.Application.Branches.Commands.CreateBranch;

public class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Branch name is required.")
            .MaximumLength(100).WithMessage("Branch name must not exceed 100 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(256).When(x => x.Address != null);

        RuleFor(x => x.Phone)
            .MaximumLength(50).When(x => x.Phone != null);
    }
}
