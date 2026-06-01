using MediatR;
using Tannous.Pos.Application.DTOs.Admin;

namespace Tannous.Pos.Application.Admin.Queries.GetAdminDatabaseStats;

public class GetAdminDatabaseStatsQuery : IRequest<AdminDatabaseStatsDto>
{
}
