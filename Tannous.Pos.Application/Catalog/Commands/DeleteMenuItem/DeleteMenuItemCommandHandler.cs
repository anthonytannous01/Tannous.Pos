using MediatR;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Catalog.Commands.DeleteMenuItem;

public class DeleteMenuItemCommandHandler : IRequestHandler<DeleteMenuItemCommand, bool>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteMenuItemCommand request, CancellationToken cancellationToken)
    {
        var menuItem = await _menuItemRepository.GetByIdAsync(request.Id);
        if (menuItem == null)
            throw new ArgumentException($"Menu item with ID {request.Id} not found");

        // Check if menu item has been used in orders.
        // Must be a direct DB query: the old GetAllAsync() scan never loaded OrderLines
        // (no Include, no lazy loading), so the check silently always passed and the
        // hard delete hit the OrderLine->MenuItem FK (Restrict) with a 500.
        var hasOrders = await _orderRepository.AnyOrderLineForMenuItemAsync(request.Id);

        if (hasOrders && !request.Force)
        {
            throw new InvalidOperationException(
                $"Cannot delete menu item '{menuItem.Name}' because it has been used in orders. Use force=true to override.");
        }

        // If force=true, deactivate instead of delete
        if (hasOrders && request.Force)
        {
            menuItem.IsActive = false;
            menuItem.UpdatedAt = DateTime.UtcNow;
            await _menuItemRepository.UpdateAsync(menuItem);
        }
        else
        {
            await _menuItemRepository.DeleteAsync(request.Id);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
