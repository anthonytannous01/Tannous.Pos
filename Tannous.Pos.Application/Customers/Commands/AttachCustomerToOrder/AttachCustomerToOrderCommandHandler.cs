using MediatR;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Customers.Commands.AttachCustomerToOrder;

public class AttachCustomerToOrderCommandHandler
    : IRequestHandler<AttachCustomerToOrderCommand, AttachCustomerToOrderResult>
{
    private readonly IOrderRepository    _orderRepository;
    private readonly ICustomerRepository _customerRepository;

    public AttachCustomerToOrderCommandHandler(
        IOrderRepository    orderRepository,
        ICustomerRepository customerRepository)
    {
        _orderRepository    = orderRepository;
        _customerRepository = customerRepository;
    }

    public async Task<AttachCustomerToOrderResult> Handle(
        AttachCustomerToOrderCommand command, CancellationToken cancellationToken)
    {
        // GetByIdAsync uses FindAsync — entities are tracked; CommitAsync persists mutations
        var order = await _orderRepository.GetByIdAsync(command.OrderId);
        if (order == null)
            return new AttachCustomerToOrderResult { OrderFound = false, CustomerFound = false };

        var customer = await _customerRepository.GetByIdAsync(command.CustomerId);
        if (customer == null)
            return new AttachCustomerToOrderResult { OrderFound = true, CustomerFound = false };

        order.CustomerId = command.CustomerId;
        order.UpdatedAt  = DateTime.UtcNow;

        // CommitAsync flushes all pending changes on the shared scoped DbContext
        await _customerRepository.CommitAsync(cancellationToken);

        return new AttachCustomerToOrderResult { OrderFound = true, CustomerFound = true };
    }
}
