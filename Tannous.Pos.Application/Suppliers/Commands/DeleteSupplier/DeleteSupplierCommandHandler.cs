using MediatR;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Suppliers.Commands.DeleteSupplier;

public class DeleteSupplierCommandHandler : IRequestHandler<DeleteSupplierCommand, bool>
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSupplierCommandHandler(
        ISupplierRepository supplierRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IUnitOfWork unitOfWork)
    {
        _supplierRepository = supplierRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(request.Id);
        if (supplier == null)
            throw new ArgumentException($"Supplier with ID {request.Id} not found");

        // Check if supplier has active purchase orders
        var activePurchaseOrders = await _purchaseOrderRepository.GetBySupplierAsync(request.Id);
        var hasActiveOrders = activePurchaseOrders.Any(po => po.Status == "Draft" || po.Status == "Submitted");

        if (hasActiveOrders && !request.Force)
        {
            throw new InvalidOperationException($"Cannot delete supplier '{supplier.Name}' because it has active purchase orders. Use force=true to override.");
        }

        // If force=true and there are active orders, deactivate the supplier instead
        if (hasActiveOrders && request.Force)
        {
            supplier.IsActive = false;
            supplier.UpdatedAt = DateTime.UtcNow;
            await _supplierRepository.UpdateAsync(supplier);
        }
        else
        {
            await _supplierRepository.DeleteAsync(request.Id);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
