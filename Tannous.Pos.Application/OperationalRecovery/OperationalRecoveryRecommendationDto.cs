namespace Tannous.Pos.Application.OperationalRecovery;

/// <summary>Advisory recovery posture recommendation for operator routing.</summary>
public sealed class OperationalRecoveryRecommendationDto
{
    public string RecommendationId { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public OperationalRecoverySeverity Severity { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string RecommendedRoute { get; init; } = string.Empty;
}
