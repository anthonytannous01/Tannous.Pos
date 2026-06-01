namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheInvalidationRecommendationDto
{
    public string Code { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
}
