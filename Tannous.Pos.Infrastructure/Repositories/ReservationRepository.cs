using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly PosDbContext _db;

    public ReservationRepository(PosDbContext db) => _db = db;

    public async Task<IEnumerable<Reservation>> GetAsync(
        Guid? branchId, DateTime? from, DateTime? to,
        ReservationStatus? status, CancellationToken ct = default)
    {
        var query = _db.Reservations
            .Include(r => r.Table)
            .AsNoTracking();

        if (branchId.HasValue)
            query = query.Where(r => r.BranchId == branchId.Value);

        if (from.HasValue)
            query = query.Where(r => r.ReservationDateTime >= from.Value);

        if (to.HasValue)
            query = query.Where(r => r.ReservationDateTime <= to.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query
            .OrderBy(r => r.ReservationDateTime)
            .ToListAsync(ct);
    }

    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Reservations
            .Include(r => r.Table)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IEnumerable<Guid>> GetConflictingTableIdsAsync(
        DateTime slot, CancellationToken ct = default)
    {
        var window = TimeSpan.FromHours(2);
        var from   = slot - window;
        var to     = slot + window;

        return await _db.Reservations
            .Where(r => r.TableId.HasValue
                && r.ReservationDateTime >= from
                && r.ReservationDateTime <= to
                && (r.Status == ReservationStatus.Pending ||
                    r.Status == ReservationStatus.Confirmed ||
                    r.Status == ReservationStatus.Seated))
            .Select(r => r.TableId!.Value)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task AddAsync(Reservation reservation, CancellationToken ct = default)
        => await _db.Reservations.AddAsync(reservation, ct);

    public Task CommitAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
