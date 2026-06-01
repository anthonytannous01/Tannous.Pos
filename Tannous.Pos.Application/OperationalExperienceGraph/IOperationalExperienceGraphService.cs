namespace Tannous.Pos.Application.OperationalExperienceGraph;

/// <summary>Operator operational experience graph and contextual navigation (advisory, process-local).</summary>
public interface IOperationalExperienceGraphService
{
    Task<OperationalExperienceGraphDto> GetExperienceGraphAsync(CancellationToken cancellationToken = default);

    Task<OperationalExperienceTraversalPathsDto> GetTraversalPathsAsync(CancellationToken cancellationToken = default);

    Task<OperationalContextualNavigationDto> GetContextualNavigationAsync(CancellationToken cancellationToken = default);
}
