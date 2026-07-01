namespace Tannous.Pos.WebApi.Constants;

/// <summary>
/// Centralized authorization policy name constants.
/// Use these constants instead of hardcoded strings to ensure consistency.
/// </summary>
public static class PolicyConstants
{
    public const string CanSell = "CanSell";
    public const string CanManageShifts = "CanManageShifts";
    public const string CanManageCatalog = "CanManageCatalog";
    public const string CanManageCustomers = "CanManageCustomers";
    public const string CanViewReports = "CanViewReports";
    public const string CanManageUsers = "CanManageUsers";
    public const string CanManageSettings = "CanManageSettings";

    /// <summary>Kitchen Display System — kitchen staff + managers + owners.</summary>
    public const string CanViewKds = "CanViewKds";

    /// <summary>Owner/Manager JWT, OR a valid read-only third-party API key (X-Api-Key). Reports only —
    /// there is no write equivalent.</summary>
    public const string CanViewReportsOrApiKey = "CanViewReportsOrApiKey";

    /// <summary>Owner/Manager/Cashier JWT, OR a valid read-only third-party API key (X-Api-Key).
    /// Applies only to customer READ endpoints — writes still require CanManageCustomers alone.</summary>
    public const string CanViewCustomersOrApiKey = "CanViewCustomersOrApiKey";
}

