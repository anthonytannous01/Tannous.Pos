using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Scheduling;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Scheduling.Queries.GetWeeklySchedule;

public class GetWeeklyScheduleQueryHandler : IRequestHandler<GetWeeklyScheduleQuery, WeeklyScheduleDto>
{
    private readonly DbContext _dbContext;

    public GetWeeklyScheduleQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WeeklyScheduleDto> Handle(GetWeeklyScheduleQuery request, CancellationToken cancellationToken)
    {
        var weekStart = FloorToMonday(request.WeekStart.Date);
        var weekEndExclusive = weekStart.AddDays(7);

        var query = _dbContext.Set<EmployeeSchedule>()
            .Include(es => es.User)
            .Where(es => es.Status != ScheduleStatus.Cancelled
                && es.ScheduledStart >= weekStart
                && es.ScheduledStart < weekEndExclusive);

        if (request.BranchId.HasValue)
            query = query.Where(es => es.BranchId == request.BranchId.Value);

        var schedules = await query
            .OrderBy(es => es.ScheduledStart)
            .ToListAsync(cancellationToken);

        return new WeeklyScheduleDto
        {
            WeekStart = weekStart,
            WeekEnd   = weekEndExclusive.AddMinutes(-1),   // Sunday 23:59 UTC
            Schedules = schedules.Select(SchedulingMappings.ToDto).ToList()
        };
    }

    private static DateTime FloorToMonday(DateTime date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return DateTime.SpecifyKind(date.AddDays(-diff), DateTimeKind.Utc);
    }
}
