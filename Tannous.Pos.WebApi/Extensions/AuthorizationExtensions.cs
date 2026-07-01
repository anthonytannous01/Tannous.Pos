using Microsoft.AspNetCore.Authorization;
using Tannous.Pos.WebApi.Authentication;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Extensions;

/// <summary>
/// Extension methods for configuring authorization policies for Tannous POS.
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Adds all Tannous POS authorization policies to the AuthorizationOptions.
    /// </summary>
    /// <param name="options">The AuthorizationOptions to configure.</param>
    public static void AddPosAuthorizationPolicies(this AuthorizationOptions options)
    {
        // CanSell: Owner, Manager, Cashier can process sales
        options.AddPolicy(PolicyConstants.CanSell, policy =>
            policy.RequireRole(RoleConstants.Owner, RoleConstants.Manager, RoleConstants.Cashier));

        // CanManageShifts: Owner, Manager, Cashier can open/close shifts and manage cash
        options.AddPolicy(PolicyConstants.CanManageShifts, policy =>
            policy.RequireRole(RoleConstants.Owner, RoleConstants.Manager, RoleConstants.Cashier));

        // CanManageCatalog: Owner, Manager can create/update/delete catalog items
        options.AddPolicy(PolicyConstants.CanManageCatalog, policy =>
            policy.RequireRole(RoleConstants.Owner, RoleConstants.Manager));

        // CanManageCustomers: Owner, Manager, Cashier can create/update customer records
        options.AddPolicy(PolicyConstants.CanManageCustomers, policy =>
            policy.RequireRole(RoleConstants.Owner, RoleConstants.Manager, RoleConstants.Cashier));

        // CanViewReports: Owner, Manager can view business reports
        options.AddPolicy(PolicyConstants.CanViewReports, policy =>
            policy.RequireRole(RoleConstants.Owner, RoleConstants.Manager));

        // CanManageUsers: Owner only can create/update/delete users
        options.AddPolicy(PolicyConstants.CanManageUsers, policy =>
            policy.RequireRole(RoleConstants.Owner));

        // CanManageSettings: Owner only can modify business settings
        options.AddPolicy(PolicyConstants.CanManageSettings, policy =>
            policy.RequireRole(RoleConstants.Owner));

        // CanViewKds: Kitchen staff, Waiters, Managers, and Owners can view/update KDS
        options.AddPolicy(PolicyConstants.CanViewKds, policy =>
            policy.RequireRole(
                RoleConstants.Kitchen,
                RoleConstants.Waiter,
                RoleConstants.Manager,
                RoleConstants.Owner));

        // CanViewReportsOrApiKey: same staff roles as CanViewReports, OR a valid read-only
        // third-party API key (see ApiKeyAuthenticationHandler). Must list both authentication
        // schemes ("Bearer,ApiKey") on the [Authorize] attribute for the ApiKeyId claim check
        // to ever get a chance to run.
        options.AddPolicy(PolicyConstants.CanViewReportsOrApiKey, policy =>
            policy.RequireAssertion(ctx =>
                ctx.User.IsInRole(RoleConstants.Owner) ||
                ctx.User.IsInRole(RoleConstants.Manager) ||
                ctx.User.HasClaim(c => c.Type == ApiKeyAuthenticationHandler.ApiKeyIdClaimType)));

        // CanViewCustomersOrApiKey: same staff roles as CanManageCustomers, OR a valid read-only
        // API key. Applied only to the two customer GET actions — write actions keep using
        // CanManageCustomers alone, so an API key can never create/update a customer record.
        options.AddPolicy(PolicyConstants.CanViewCustomersOrApiKey, policy =>
            policy.RequireAssertion(ctx =>
                ctx.User.IsInRole(RoleConstants.Owner) ||
                ctx.User.IsInRole(RoleConstants.Manager) ||
                ctx.User.IsInRole(RoleConstants.Cashier) ||
                ctx.User.HasClaim(c => c.Type == ApiKeyAuthenticationHandler.ApiKeyIdClaimType)));
    }
}

