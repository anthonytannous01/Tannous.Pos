using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Suppliers.Commands.UpdateSupplier;

public class UpdateSupplierCommandHandler : IRequestHandler<UpdateSupplierCommand, SupplierDto>
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSupplierCommandHandler(
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork)
    {
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SupplierDto> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(request.Id);
        if (supplier == null)
            throw new ArgumentException($"Supplier with ID {request.Id} not found");

        // Check if name is being changed and if it conflicts with another supplier
        if (supplier.Name != request.Supplier.Name)
        {
            var existingSupplier = await _supplierRepository.GetByNameAsync(request.Supplier.Name);
            if (existingSupplier != null && existingSupplier.Id != request.Id)
                throw new ArgumentException($"Supplier with name '{request.Supplier.Name}' already exists");
        }

        // Update supplier properties
        supplier.Name = request.Supplier.Name;
        supplier.ContactPerson = request.Supplier.ContactPerson;
        supplier.Email = request.Supplier.Email;
        supplier.Phone = request.Supplier.Phone;
        supplier.Address = request.Supplier.Address;
        supplier.IsActive = request.Supplier.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _supplierRepository.UpdateAsync(supplier);
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
