using MediatR;
using Tannous.Pos.Application.DTOs.Customers;

namespace Tannous.Pos.Application.Customers.Commands.CreateCustomer;

public class CreateCustomerCommand : IRequest<CustomerDto>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public string? Email    { get; set; }
    public string? Phone    { get; set; }
    public string? Address  { get; set; }
    public string? Notes    { get; set; }
    public string? Allergies { get; set; }
}
