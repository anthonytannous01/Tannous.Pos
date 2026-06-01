using MediatR;
using Tannous.Pos.Application.DTOs.Customers;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Customers.Commands.CreateCustomer;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;

    public CreateCustomerCommandHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            FirstName  = command.FirstName,
            LastName   = command.LastName,
            Email      = command.Email,
            Phone      = command.Phone,
            Address    = command.Address,
            Notes      = command.Notes,
            Allergies  = command.Allergies,
            IsActive   = true
        };

        await _customerRepository.AddAsync(customer);
        await _customerRepository.CommitAsync(cancellationToken);  // AddAsync does NOT auto-commit

        return new CustomerDto
        {
            Id          = customer.Id,
            FirstName   = customer.FirstName,
            LastName    = customer.LastName,
            Email       = customer.Email,
            Phone       = customer.Phone,
            Address     = customer.Address,
            Notes       = customer.Notes,
            Allergies   = customer.Allergies,
            IsActive    = customer.IsActive,
            LastVisitDate = customer.LastVisitDate,
            TotalOrders = customer.TotalOrders,
            CreatedAt   = customer.CreatedAt
            // Version intentionally omitted (matches original CreateCustomer response)
        };
    }
}
