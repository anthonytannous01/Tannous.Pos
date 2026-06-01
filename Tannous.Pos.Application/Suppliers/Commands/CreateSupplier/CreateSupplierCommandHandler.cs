using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Suppliers.Commands.CreateSupplier;

public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, SupplierDto>
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSupplierCommandHandler(
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork)
    {
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SupplierDto> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        // Check if supplier with same name already exists
        var existingSupplier = await _supplierRepository.GetByNameAsync(request.Supplier.Name);
        if (existingSupplier != null)
            throw new ArgumentException($"Supplier with name '{request.Supplier.Name}' already exists");

        var supplier = new Supplier
        {
            Name = request.Supplier.Name,
            ContactPerson = request.Supplier.ContactPerson,
            Email = request.Supplier.Email,
            Phone = request.Supplier.Phone,
            Address = request.Supplier.Address,
            IsActive = request.Supplier.IsActive
        };

        await _supplierRepository.AddAsync(supplier);
        await _unitOfWork.SaveChangesAsync();

        return new SupplierDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            IsActive = supplier.IsActive,
            CreatedAt = supplier.CreatedAt
        };
    }
}
