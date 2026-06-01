using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Interfaces;

public interface IShiftRepository : IRepository<Shift>
{
    Task<Shift?> GetByIdWithDetailsAsync(Guid id);
    Task<Shift?> GetOpenShiftByUserAsync(Guid userId);
    Task<IEnumerable<Shift>> GetByStatusAsync(ShiftStatus status);
    Task<IEnumerable<Shift>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<Shift?> GetByShiftNumberAsync(string shiftNumber);

    Task CommitAsync(CancellationToken cancellationToken = default);
}
