using FluentValidation;
using Tannous.Pos.Application.Suppliers.Commands.CreateSupplier;

namespace Tannous.Pos.Application.Suppliers.Commands.CreateSupplier;

public class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.Supplier.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Supplier name is required and must not exceed 100 characters");

        RuleFor(x => x.Supplier.ContactPerson)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Supplier.ContactPerson))
            .WithMessage("Contact person name must not exceed 100 characters");

        RuleFor(x => x.Supplier.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Supplier.Email))
            .WithMessage("Email must be a valid email address");

        RuleFor(x => x.Supplier.Phone)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Supplier.Phone))
            .WithMessage("Phone number must not exceed 20 characters");

        RuleFor(x => x.Supplier.Address)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Supplier.Address))
            .WithMessage("Address must not exceed 500 characters");
    }
}
