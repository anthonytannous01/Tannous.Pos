using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.DTOs.Scheduling;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Scheduling.Commands.CancelSchedule;

public class CancelScheduleCommandHandler : IRequestHandler<CancelScheduleCommand, EmployeeScheduleDto>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<CancelScheduleCommandHandler> _logger;

    public CancelScheduleCommandHandler(DbContext dbContext, ILogger<CancelScheduleCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<EmployeeScheduleDto> Handle(CancelScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _dbContext.Set<EmployeeSchedule>()
            .Include(es => es.User)
            .FirstOrDefaultAsync(es => es.Id == request.ScheduleId, cancellationToken);

        if (schedule == null)
            throw new KeyNotFoundException($"Schedule {request.ScheduleId} not found");

        if (schedule.Status == ScheduleStatus.Cancelled)
            throw new InvalidOperationException("Schedule is already cancelled.");

        schedule.Status    = ScheduleStatus.Cancelled;
        schedule.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee schedule cancelled. ScheduleId={ScheduleId}", schedule.Id);

        return SchedulingMappings.ToDto(schedule);
    }
}
