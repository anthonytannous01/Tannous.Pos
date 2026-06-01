namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Platform-wide causality summary for operator attention.</summary>
public sealed class OperationalCausalitySummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int ActiveCausalChains { get; init; }
    public int EscalatingPropagationCount { get; init; }
    public int CollapsingPropagationCount { get; init; }
    public string DominantOperationalArea { get; init; } = string.Empty;
    public string HighestRiskPropagation { get; init; } = string.Empty;
    public int StabilizationBlockerCount { get; init; }
    public string PlatformRecoveryOutlook { get; init; } = string.Empty;
    public string OperatorAttentionLevel { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string CausalityNote { get; init; } =
        "Advisory deterministic causal interpretation composed from existing diagnostics. Heuristic only — not probabilistic diagnosis or distributed tracing.";
}
