using MediatR;
using Tannous.Pos.Application.DTOs.Common;
using Tannous.Pos.Application.DTOs.Customers;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Customers.Queries.GetCustomers;

public class GetCustomersQueryHandler
    : IRequestHandler<GetCustomersQuery, PaginatedResponseDto<CustomerDto>>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomersQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<PaginatedResponseDto<CustomerDto>> Handle(
        GetCustomersQuery query, CancellationToken cancellationToken)
    {
        var req = query.Request;
        var (items, total) = await _customerRepository.SearchPagedAsync(
            req.SearchQuery, req.Sort, req.Dir, req.Skip, req.Take, cancellationToken);

        return new PaginatedResponseDto<CustomerDto>
        {
            Items = items.Select(MapToDto),
            Total = total,
            Page = req.Page,
            PageSize = req.Take
        };
    }

    private static CustomerDto MapToDto(Customer c) => new()
    {
        Id          = c.Id,
        FirstName   = c.FirstName,
        LastName    = c.LastName,
        Email       = c.Email,
        Phone       = c.Phone,
        Address     = c.Address,
        Notes       = c.Notes,
        Allergies   = c.Allergies,
        IsActive    = c.IsActive,
        LastVisitDate = c.LastVisitDate,
        TotalOrders = c.TotalOrders,
        CreatedAt   = c.CreatedAt,
        Version     = c.Version   // Preserves original list wire contract (controller mapped Version here)
    };
}
