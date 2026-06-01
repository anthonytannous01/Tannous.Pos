namespace Tannous.Pos.Application.OperationalNavigation;

public interface IOperationalNavigationService
{
    Task<OperationalNavigationIndexDto> GetNavigationIndexAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalNavigationRouteDto>> GetNavigationRoutesAsync(CancellationToken cancellationToken = default);
}
