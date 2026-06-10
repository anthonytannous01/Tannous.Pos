using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.DTOs.Scheduling;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Scheduling.Commands.ClockOut;

public class ClockOutCommandHandler : IRequestHandler<ClockOutCommand, TimeEntryDto>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<ClockOutCommandHandler> _logger;

    public ClockOutCommandHandler(DbContext dbContext, ILogger<ClockOutCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TimeEntryDto> Handle(ClockOutCommand request, CancellationToken cancellationToken)
    {
        var entry = await _dbContext.Set<TimeEntry>()
            .Include(te => te.User)
            .FirstOrDefaultAsync(
                te => te.UserId == request.UserId && te.Status == TimeEntryStatus.Active,
                cancellationToken);

        if (entry == null)
            throw new KeyNotFoundException("No active time entry found");

        var utcNow = DateTime.UtcNow;
        entry.ClockOut     = utcNow;
        entry.BreakMinutes = request.BreakMinutes;
        if (!string.IsNullOrWhiteSpace(request.Notes))
            entry.Notes = request.Notes;
        entry.Status    = TimeEntryStatus.Completed;
        entry.UpdatedAt = utcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Employee clocked out. TimeEntryId={TimeEntryId}, UserId={UserId}, WorkedMinutes={WorkedMinutes}",
            entry.Id, entry.UserId, SchedulingMappings.ComputeWorkedMinutes(entry, utcNow));

        return SchedulingMappings.ToDto(entry, utcNow);
    }
}
