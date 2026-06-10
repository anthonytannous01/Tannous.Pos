using MediatR;

namespace Tannous.Pos.Application.Scheduling.Commands.PublishSchedule;

/// <summary>
/// Publishes a batch of Draft schedule entries. Non-Draft ids are skipped.
/// Returns the number of entries actually published.
/// </summary>
public class PublishScheduleCommand : IRequest<int>
{
    public List<Guid> ScheduleIds { get; set; } = new();
}
