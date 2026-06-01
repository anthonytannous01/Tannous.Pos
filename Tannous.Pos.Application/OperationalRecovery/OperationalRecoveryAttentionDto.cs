namespace Tannous.Pos.Application.OperationalRecovery;

/// <summary>Recovery posture attention item for operator follow-up.</summary>
public sealed class OperationalRecoveryAttentionDto
{
    public string AttentionId { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public OperationalRecoverySeverity Severity { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string RecommendedRoute { get; init; } = string.Empty;
}
