using Tannous.Pos.Application.DTOs.Admin;

namespace Tannous.Pos.Application.Interfaces;

/// <summary>Read-only aggregate statistics for the admin database view.</summary>
public interface IAdminDatabaseStatsRepository
{
    Task<AdminDatabaseStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}
