using MediatR;
using Tannous.Pos.Application.DTOs.Customers;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Customers.Queries.GetCustomer;

public class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, CustomerDto?>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerDto?> Handle(GetCustomerQuery query, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(query.Id);
        if (customer == null) return null;
        return MapToDto(customer);
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
        CreatedAt   = c.CreatedAt
        // Version intentionally omitted (matches original GetCustomer response)
    };
}
