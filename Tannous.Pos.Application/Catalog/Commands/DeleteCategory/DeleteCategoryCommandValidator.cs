using FluentValidation;
using Tannous.Pos.Application.Catalog.Commands.DeleteCategory;

namespace Tannous.Pos.Application.Catalog.Commands.DeleteCategory;

public class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Category ID is required");
    }
}
