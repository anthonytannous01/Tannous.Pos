namespace Tannous.Pos.Application.DTOs.Customers;

public class UpdateCustomerDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string? Allergies { get; set; }
    public bool IsActive { get; set; } = true;
}
