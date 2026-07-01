namespace Tannous.Pos.Application.DTOs.Scheduling;

/// <summary>
/// Lightweight user record returned by GET /schedule/staff for the shift-picker.
/// Requires CanManageShifts (Owner + Manager) — not CanManageUsers (Owner only).
/// </summary>
public class StaffMemberDto
{
    public Guid   Id        { get; set; }
    public string Username  { get; set; } = string.Empty;
    public string Email     { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public string Role      { get; set; } = string.Empty;
}
