using Microsoft.AspNetCore.Authorization;
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
    }
}

