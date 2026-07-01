using Tannous.Pos.Domain.Common;

namespace Tannous.Pos.Domain.Entities;

/// <summary>
/// Long-lived read-only API key for third-party integrators.
/// Presented via the X-Api-Key header and validated by
/// Tannous.Pos.WebApi.Authentication.ApiKeyAuthenticationHandler, which maps a valid key to the
/// "ApiIntegrator" role — a role no write-oriented policy recognizes, so a key can never grant
/// write access by construction. Currently wired onto: ReportsController (all actions) and
/// CustomersController's two GET actions (GetCustomers, GetCustomer). The public digital menu
/// (MenuController) already requires no authentication at all, key or otherwise.
/// KNOWN LIMITATION: BranchId below is stored but not yet enforced — no query handler filters by
/// it, so a "branch-scoped" key currently still sees all-branch data on the endpoints above. See
/// AUTHORIZATION_POLICIES.md "Known limitations".
/// </summary>
public class ApiKey : BaseEntity, IAggregateRoot
{
    public string  Name       { get; set; } = string.Empty;
    public string  KeyHash    { get; set; } = string.Empty;
    public string  KeyPrefix  { get; set; } = string.Empty;
    public bool    IsActive   { get; set; } = true;
    public Guid?   BranchId   { get; set; }
    public DateTime? ExpiresAt  { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public virtual Branch? Branch { get; set; }
}
