namespace Tannous.Pos.Application.OperationalReplayWorkbench;

/// <summary>
/// Operator replay pressure &amp; stabilization workbench (read-only; advisory workflow visibility).
/// NON-GOAL: not governance infrastructure; not authoritative business truth; no mutations.
/// </summary>
public sealed class OperationalReplayWorkbenchDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalReplayPressureSummaryDto PressureSummary { get; init; } = new();
    public OperationalReplayStabilizationDto Stabilization { get; init; } = new();
    public IReadOnlyList<OperationalReplayHotspotDto> Hotspots { get; init; } = Array.Empty<OperationalReplayHotspotDto>();
    public OperationalReplayRecoveryConfidenceDto RecoveryConfidence { get; init; } = new();
    public IReadOnlyList<OperationalReplayAttentionItemDto> AttentionItems { get; init; } = Array.Empty<OperationalReplayAttentionItemDto>();
    public string WorkbenchNote { get; init; } =
        "Advisory replay pressure workbench composed from existing diagnostics. Review items are guidance only — no automated stabilization is performed.";
}
