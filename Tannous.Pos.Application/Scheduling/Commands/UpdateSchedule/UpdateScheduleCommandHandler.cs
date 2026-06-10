using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.DTOs.Scheduling;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Scheduling.Commands.UpdateSchedule;

public class UpdateScheduleCommandHandler : IRequestHandler<UpdateScheduleCommand, EmployeeScheduleDto>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<UpdateScheduleCommandHandler> _logger;

    public UpdateScheduleCommandHandler(DbContext dbContext, ILogger<UpdateScheduleCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<EmployeeScheduleDto> Handle(UpdateScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _dbContext.Set<EmployeeSchedule>()
            .Include(es => es.User)
            .FirstOrDefaultAsync(es => es.Id == request.ScheduleId, cancellationToken);

        if (schedule == null)
            throw new KeyNotFoundException($"Schedule {request.ScheduleId} not found");

        if (schedule.Status != ScheduleStatus.Draft)
            throw new InvalidOperationException("Only Draft schedules can be updated.");

        schedule.ScheduledStart = request.ScheduledStart;
        schedule.ScheduledEnd   = request.ScheduledEnd;
        schedule.Position       = request.Position;
        schedule.Notes          = request.Notes;
        schedule.UpdatedAt      = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee schedule updated. ScheduleId={ScheduleId}", schedule.Id);

        return SchedulingMappings.ToDto(schedule);
    }
}
