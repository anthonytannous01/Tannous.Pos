using MediatR;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Catalog.Commands.DeleteAddOn;

public class DeleteAddOnCommandHandler : IRequestHandler<DeleteAddOnCommand, bool>
{
    private readonly IAddOnRepository _addOnRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAddOnCommandHandler(
        IAddOnRepository addOnRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _addOnRepository = addOnRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteAddOnCommand request, CancellationToken cancellationToken)
    {
        var addOn = await _addOnRepository.GetByIdAsync(request.Id);
        if (addOn == null)
            throw new ArgumentException($"Add-on with ID {request.Id} not found");

        // Check if add-on has been used in orders
        var orders = await _orderRepository.GetAllAsync();
        var hasOrders = orders.Any(o => o.OrderLines.Any(ol => ol.OrderLineAddOns.Any(ola => ola.AddOnId == request.Id)));

        if (hasOrders && !request.Force)
        {
            throw new InvalidOperationException(
                $"Cannot delete add-on '{addOn.Name}' because it has been used in orders. Use force=true to override.");
        }

        // If force=true, deactivate instead of delete
        if (hasOrders && request.Force)
        {
            addOn.IsActive = false;
            addOn.UpdatedAt = DateTime.UtcNow;
            await _addOnRepository.UpdateAsync(addOn);
        }
        else
        {
            await _addOnRepository.DeleteAsync(request.Id);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
