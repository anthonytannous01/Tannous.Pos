namespace Tannous.Pos.Application.OperationalTopology;

/// <summary>Deterministic operational influence profile for a single area.</summary>
public sealed class OperationalInfluenceDto
{
    public string Area { get; init; } = string.Empty;
    public OperationalInfluenceType InfluenceType { get; init; }
    public string UpstreamInfluenceStrength { get; init; } = string.Empty;
    public string DownstreamInfluenceStrength { get; init; } = string.Empty;
    public string RecoveryImpact { get; init; } = string.Empty;
    public string EscalationImpact { get; init; } = string.Empty;
    public OperationalCriticalityLevel OperationalImportance { get; init; }
}
