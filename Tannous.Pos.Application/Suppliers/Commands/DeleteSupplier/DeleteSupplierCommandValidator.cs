using FluentValidation;
using Tannous.Pos.Application.Suppliers.Commands.DeleteSupplier;

namespace Tannous.Pos.Application.Suppliers.Commands.DeleteSupplier;

public class DeleteSupplierCommandValidator : AbstractValidator<DeleteSupplierCommand>
{
    public DeleteSupplierCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Supplier ID is required");
    }
}
