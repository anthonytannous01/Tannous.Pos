namespace Tannous.Pos.Application.Audit;

/// <summary>Retention classification labels for operational artifacts (guidance only).</summary>
public static class OperationalRetentionCategories
{
    public const string HotOperational = "HotOperational";
    public const string WarmReconciliation = "WarmReconciliation";
    public const string LongTermForensic = "LongTermForensic";
}
