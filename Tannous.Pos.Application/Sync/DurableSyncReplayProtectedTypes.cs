namespace Tannous.Pos.Application.Sync;

/// <summary>Sync operation types that use durable <c>SyncOperationReceipt</c> replay protection (money, inventory, customer, and shift push paths).</summary>
public static class DurableSyncReplayProtectedTypes
{
    public static readonly HashSet<string> All = new(StringComparer.Ordinal)
    {
        "CreateOrder",
        "FinalizeOrder",
        "CashDrop",
        "AdjustInventory",
        "RecordWastage",
        "OpenShift",
        "CreateCustomer"
    };

    public static bool IsProtected(string operationType) =>
        !string.IsNullOrWhiteSpace(operationType) && All.Contains(operationType);

    /// <summary>Inventory-affecting protected types (sync push applies stock / movement rows).</summary>
    public static bool IsInventoryProtected(string operationType) =>
        string.Equals(operationType, "AdjustInventory", StringComparison.Ordinal) ||
        string.Equals(operationType, "RecordWastage", StringComparison.Ordinal);

    /// <summary>Customer/shift placeholder processors still wrapped by durable replay at push boundary.</summary>
    public static bool IsCustomerOrShiftProtected(string operationType) =>
        string.Equals(operationType, "CreateCustomer", StringComparison.Ordinal) ||
        string.Equals(operationType, "OpenShift", StringComparison.Ordinal);
}
