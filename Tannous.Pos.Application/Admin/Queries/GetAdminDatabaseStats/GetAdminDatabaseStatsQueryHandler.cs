using MediatR;
using Tannous.Pos.Application.DTOs.Admin;
using Tannous.Pos.Application.Interfaces;

namespace Tannous.Pos.Application.Admin.Queries.GetAdminDatabaseStats;

public class GetAdminDatabaseStatsQueryHandler : IRequestHandler<GetAdminDatabaseStatsQuery, AdminDatabaseStatsDto>
{
    private readonly IAdminDatabaseStatsRepository _statsRepository;

    public GetAdminDatabaseStatsQueryHandler(IAdminDatabaseStatsRepository statsRepository)
    {
        _statsRepository = statsRepository;
    }

    public Task<AdminDatabaseStatsDto> Handle(GetAdminDatabaseStatsQuery request, CancellationToken cancellationToken) =>
        _statsRepository.GetStatsAsync(cancellationToken);
}
