namespace Tannous.Pos.Application.OperationalDashboard;

/// <summary>
/// Unified operator dashboard read model composed from existing diagnostics (read-only; advisory).
/// GOVERNANCE / NON-GOAL: not deployment gating; not authoritative business truth; no payload exposure.
/// </summary>
public sealed class OperationalDashboardSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalDashboardHealthDto Health { get; init; } = new();
    public OperationalDashboardRiskDto Risk { get; init; } = new();
    public OperationalDashboardPressureDto Pressure { get; init; } = new();
    public OperationalDashboardActivityDto Activity { get; init; } = new();
    public IReadOnlyList<string> ActiveConcerns { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
    public string ReadinessSummary { get; init; } = string.Empty;
    public string FingerprintStabilitySummary { get; init; } = string.Empty;
    public string OperationalNote { get; init; } =
        "Advisory operational dashboard composed from existing diagnostics. Not a substitute for forensic export or reconciliation workflow.";
}
