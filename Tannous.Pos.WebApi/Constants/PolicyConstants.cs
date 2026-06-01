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
}

