using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.DTOs.Scheduling;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Scheduling.Commands.ClockIn;

public class ClockInCommandHandler : IRequestHandler<ClockInCommand, TimeEntryDto>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<ClockInCommandHandler> _logger;

    public ClockInCommandHandler(DbContext dbContext, ILogger<ClockInCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TimeEntryDto> Handle(ClockInCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"Active user {request.UserId} not found");

        var alreadyActive = await _dbContext.Set<TimeEntry>()
            .AnyAsync(te => te.UserId == request.UserId && te.Status == TimeEntryStatus.Active, cancellationToken);
        if (alreadyActive)
            throw new InvalidOperationException("Already clocked in");

        var utcNow = DateTime.UtcNow;
        var entry = new TimeEntry
        {
            UserId   = request.UserId,
            User     = user,
            BranchId = request.BranchId,
            ClockIn  = utcNow,
            Status   = TimeEntryStatus.Active
        };

        _dbContext.Set<TimeEntry>().Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Employee clocked in. TimeEntryId={TimeEntryId}, UserId={UserId}, BranchId={BranchId}",
            entry.Id, entry.UserId, entry.BranchId);

        return SchedulingMappings.ToDto(entry, utcNow);
    }
}
