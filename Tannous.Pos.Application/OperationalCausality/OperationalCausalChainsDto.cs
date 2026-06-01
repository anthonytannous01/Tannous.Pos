namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Bounded list of operator causal chains with nodes.</summary>
public sealed class OperationalCausalChainsDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int ChainCount { get; init; }
    public int MaxChains { get; init; } = OperationalCausalityAggregation.MaxCausalChains;
    public IReadOnlyList<OperationalCausalChainDto> Chains { get; init; } = Array.Empty<OperationalCausalChainDto>();
    public IReadOnlyList<OperationalCausalNodeDto> Nodes { get; init; } = Array.Empty<OperationalCausalNodeDto>();
}
