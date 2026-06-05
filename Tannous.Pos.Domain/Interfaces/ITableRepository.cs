using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Domain.Interfaces;

public interface ITableRepository
{
    /// <summary>Returns active tables with capacity >= minCapacity, including their FloorPlan.</summary>
    Task<IEnumerable<Table>> GetActiveAsync(int minCapacity = 1, CancellationToken ct = default);
}
