using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class FeedbackRepository : IFeedbackRepository
{
    private readonly PosDbContext _db;

    public FeedbackRepository(PosDbContext db) => _db = db;

    public async Task AddAsync(FeedbackSubmission feedback, CancellationToken cancellationToken = default)
        => await _db.FeedbackSubmissions.AddAsync(feedback, cancellationToken);

    public async Task<IEnumerable<FeedbackSubmission>> GetAsync(
        Guid? branchId, DateTime? from, DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = _db.FeedbackSubmissions.AsNoTracking();

        if (branchId.HasValue)
            query = query.Where(f => f.BranchId == branchId.Value);

        if (from.HasValue)
            query = query.Where(f => f.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(f => f.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
