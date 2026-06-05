using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Interfaces;

public interface IReservationRepository
{
    Task<IEnumerable<Reservation>> GetAsync(
        Guid? branchId, DateTime? from, DateTime? to,
        ReservationStatus? status, CancellationToken ct = default);

    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns table IDs that have a conflicting Pending/Confirmed reservation
    /// within ±2 hours of the requested slot.
    /// </summary>
    Task<IEnumerable<Guid>> GetConflictingTableIdsAsync(
        DateTime slot, CancellationToken ct = default);

    Task AddAsync(Reservation reservation, CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
}
