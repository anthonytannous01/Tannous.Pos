using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.DTOs.Scheduling;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Scheduling.Commands.CreateSchedule;

public class CreateScheduleCommandHandler : IRequestHandler<CreateScheduleCommand, EmployeeScheduleDto>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<CreateScheduleCommandHandler> _logger;

    public CreateScheduleCommandHandler(DbContext dbContext, ILogger<CreateScheduleCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<EmployeeScheduleDto> Handle(CreateScheduleCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"Active user {request.UserId} not found");

        var branchExists = await _dbContext.Set<Branch>()
            .AnyAsync(b => b.Id == request.BranchId, cancellationToken);
        if (!branchExists)
            throw new KeyNotFoundException($"Branch {request.BranchId} not found");

        var schedule = new EmployeeSchedule
        {
            UserId         = request.UserId,
            User           = user,
            BranchId       = request.BranchId,
            ScheduledStart = request.ScheduledStart,
            ScheduledEnd   = request.ScheduledEnd,
            Position       = request.Position,
            Notes          = request.Notes,
            Status         = ScheduleStatus.Draft
        };

        _dbContext.Set<EmployeeSchedule>().Add(schedule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Employee schedule created. ScheduleId={ScheduleId}, UserId={UserId}, Start={Start}, End={End}",
            schedule.Id, schedule.UserId, schedule.ScheduledStart, schedule.ScheduledEnd);

        return SchedulingMappings.ToDto(schedule);
    }
}
