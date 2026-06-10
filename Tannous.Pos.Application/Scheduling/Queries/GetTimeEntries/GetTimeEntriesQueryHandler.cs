using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Scheduling;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Scheduling.Queries.GetTimeEntries;

public class GetTimeEntriesQueryHandler : IRequestHandler<GetTimeEntriesQuery, List<TimeEntryDto>>
{
    private readonly DbContext _dbContext;

    public GetTimeEntriesQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TimeEntryDto>> Handle(GetTimeEntriesQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<TimeEntry>()
            .Include(te => te.User)
            .Where(te => te.ClockIn >= request.From && te.ClockIn < request.To);

        if (request.UserId.HasValue)
            query = query.Where(te => te.UserId == request.UserId.Value);

        if (request.BranchId.HasValue)
            query = query.Where(te => te.BranchId == request.BranchId.Value);

        var entries = await query
            .OrderByDescending(te => te.ClockIn)
            .ToListAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        return entries.Select(te => SchedulingMappings.ToDto(te, utcNow)).ToList();
    }
}
