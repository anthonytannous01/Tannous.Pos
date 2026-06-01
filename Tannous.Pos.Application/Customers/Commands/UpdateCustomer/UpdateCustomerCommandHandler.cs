using MediatR;
using Tannous.Pos.Application.DTOs.Customers;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Customers.Commands.UpdateCustomer;

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, UpdateCustomerResult>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IAuditService       _auditService;

    public UpdateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IAuditService auditService)
    {
        _customerRepository = customerRepository;
        _auditService       = auditService;
    }

    public async Task<UpdateCustomerResult> Handle(
        UpdateCustomerCommand command, CancellationToken cancellationToken)
    {
        // GetByIdAsync uses FindAsync — entity is tracked; mutation before CommitAsync persists
        var customer = await _customerRepository.GetByIdAsync(command.Id);
        if (customer == null)
            return new UpdateCustomerResult { IsConflict = false, Updated = null };

        // Optimistic concurrency check — mirrors original controller byte[] comparison
        if (command.Version != null && !customer.Version.SequenceEqual(command.Version))
        {
            return new UpdateCustomerResult
            {
                IsConflict  = true,
                ServerState = MapToDto(customer)
            };
        }

        customer.FirstName  = command.FirstName;
        customer.LastName   = command.LastName;
        customer.Email      = command.Email;
        customer.Phone      = command.Phone;
        customer.Address    = command.Address;
        customer.Notes      = command.Notes;
        customer.Allergies  = command.Allergies;
        customer.UpdatedAt  = DateTime.UtcNow;

        await _customerRepository.CommitAsync(cancellationToken);

        await _auditService.LogEventAsync("UpdateCustomer", "Customer", command.Id, new
        {
            CustomerId    = command.Id,
            UpdatedFields = new[] { "FirstName", "LastName", "Email", "Phone", "Address", "Notes", "Allergies" }
        });

        return new UpdateCustomerResult { Updated = MapToDto(customer) };
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
        Version     = c.Version   // Version IS included in update response
    };
}
