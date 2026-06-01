namespace Tannous.Pos.Application.OperationalInventoryWorkbench;

/// <summary>
/// Operator inventory drift workbench read model (read-only; advisory workflow visibility).
/// NON-GOAL: not governance infrastructure; not authoritative business truth; no mutations.
/// </summary>
public sealed class OperationalInventoryWorkbenchDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalInventoryDriftSummaryDto DriftSummary { get; init; } = new();
    public IReadOnlyList<OperationalInventoryDriftHotspotDto> Hotspots { get; init; } = Array.Empty<OperationalInventoryDriftHotspotDto>();
    public OperationalInventoryResolutionReadinessDto ResolutionReadiness { get; init; } = new();
    public IReadOnlyList<OperationalInventoryMismatchCategoryDto> MismatchCategories { get; init; } = Array.Empty<OperationalInventoryMismatchCategoryDto>();
    public IReadOnlyList<OperationalInventoryAttentionItemDto> AttentionItems { get; init; } = Array.Empty<OperationalInventoryAttentionItemDto>();
    public string WorkbenchNote { get; init; } =
        "Advisory inventory drift workbench composed from existing diagnostics. Review items are guidance only — no automated resolution is performed.";
}
