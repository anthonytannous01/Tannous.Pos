using MediatR;
using Tannous.Pos.Application.DTOs.Customers;

namespace Tannous.Pos.Application.Customers.Queries.GetCustomer;

public class GetCustomerQuery : IRequest<CustomerDto?>
{
    public Guid Id { get; set; }
}
