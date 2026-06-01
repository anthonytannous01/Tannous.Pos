namespace Tannous.Pos.Application.OperationalInventoryWorkbench;

/// <summary>Grouped operator-facing inventory mismatch category (no raw entity payloads).</summary>
public sealed class OperationalInventoryMismatchCategoryDto
{
    public string Category { get; init; } = string.Empty;
    public int ConflictCount { get; init; }
    public OperationalInventoryDriftSeverity Severity { get; init; }
    public string Summary { get; init; } = string.Empty;
}
