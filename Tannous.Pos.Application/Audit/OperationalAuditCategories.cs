namespace Tannous.Pos.Application.Audit;

public static class OperationalAuditCategories
{
    public const string Order = "Order";
    public const string Inventory = "Inventory";
    public const string Settlement = "Settlement";
    public const string Replay = "Replay";
    public const string Reconciliation = "Reconciliation";
    public const string Refund = "Refund";
    public const string Concurrency = "Concurrency";
    public const string ReconciliationWorkflow = "ReconciliationWorkflow";
}
