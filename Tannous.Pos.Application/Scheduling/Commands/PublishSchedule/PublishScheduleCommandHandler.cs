using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Scheduling.Commands.PublishSchedule;

public class PublishScheduleCommandHandler : IRequestHandler<PublishScheduleCommand, int>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<PublishScheduleCommandHandler> _logger;

    public PublishScheduleCommandHandler(DbContext dbContext, ILogger<PublishScheduleCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<int> Handle(PublishScheduleCommand request, CancellationToken cancellationToken)
    {
        var drafts = await _dbContext.Set<EmployeeSchedule>()
            .Where(es => request.ScheduleIds.Contains(es.Id) && es.Status == ScheduleStatus.Draft)
            .ToListAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        foreach (var schedule in drafts)
        {
            schedule.Status    = ScheduleStatus.Published;
            schedule.UpdatedAt = utcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Employee schedules published. Requested={Requested}, Published={Published}",
            request.ScheduleIds.Count, drafts.Count);

        return drafts.Count;
    }
}
