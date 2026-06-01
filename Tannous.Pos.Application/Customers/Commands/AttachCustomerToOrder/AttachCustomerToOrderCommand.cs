using MediatR;

namespace Tannous.Pos.Application.Customers.Commands.AttachCustomerToOrder;

public class AttachCustomerToOrderCommand : IRequest<AttachCustomerToOrderResult>
{
    public Guid OrderId    { get; set; }
    public Guid CustomerId { get; set; }
}

public class AttachCustomerToOrderResult
{
    public bool OrderFound    { get; init; }
    public bool CustomerFound { get; init; }
}
