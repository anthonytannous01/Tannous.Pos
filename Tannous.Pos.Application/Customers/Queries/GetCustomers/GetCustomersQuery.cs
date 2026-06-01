using MediatR;
using Tannous.Pos.Application.DTOs.Common;
using Tannous.Pos.Application.DTOs.Customers;

namespace Tannous.Pos.Application.Customers.Queries.GetCustomers;

public class GetCustomersQuery : IRequest<PaginatedResponseDto<CustomerDto>>
{
    public PaginatedRequestDto Request { get; set; } = new();
}
