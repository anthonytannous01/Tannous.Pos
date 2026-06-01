using MediatR;
using Tannous.Pos.Application.DTOs.Customers;

namespace Tannous.Pos.Application.Customers.Commands.UpdateCustomer;

public class UpdateCustomerCommand : IRequest<UpdateCustomerResult>
{
    public Guid Id          { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public string? Email    { get; set; }
    public string? Phone    { get; set; }
    public string? Address  { get; set; }
    public string? Notes    { get; set; }
    public string? Allergies { get; set; }
    public byte[]? Version  { get; set; }
}

public class UpdateCustomerResult
{
    public bool IsConflict   { get; init; }
    /// <summary>Current server state when IsConflict is true.</summary>
    public CustomerDto? ServerState { get; init; }
    /// <summary>Applied update when IsConflict is false.</summary>
    public CustomerDto? Updated    { get; init; }
}
