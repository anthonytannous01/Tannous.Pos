using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Scheduling;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Scheduling.Queries.GetMyClockStatus;

public class GetMyClockStatusQueryHandler : IRequestHandler<GetMyClockStatusQuery, TimeEntryDto?>
{
    private readonly DbContext _dbContext;

    public GetMyClockStatusQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TimeEntryDto?> Handle(GetMyClockStatusQuery request, CancellationToken cancellationToken)
    {
        var entry = await _dbContext.Set<TimeEntry>()
            .Include(te => te.User)
            .Where(te => te.UserId == request.UserId && te.Status == TimeEntryStatus.Active)
            .OrderByDescending(te => te.ClockIn)
            .FirstOrDefaultAsync(cancellationToken);

        return entry == null ? null : SchedulingMappings.ToDto(entry, DateTime.UtcNow);
    }
}
