using MediatR;
using Tannous.Pos.Application.DTOs.Accounting;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Accounting.Commands.TriggerAccountingSync;

public class TriggerAccountingSyncCommandHandler
    : IRequestHandler<TriggerAccountingSyncCommand, SyncTriggerResultDto>
{
    private readonly IAccountingSyncCoordinator _coordinator;

    public TriggerAccountingSyncCommandHandler(IAccountingSyncCoordinator coordinator)
        => _coordinator = coordinator;

    public async Task<SyncTriggerResultDto> Handle(
        TriggerAccountingSyncCommand request, CancellationToken cancellationToken)
    {
        var date = (request.Date ?? DateTime.UtcNow.Date.AddDays(-1)).Date;
        var (synced, errors) = await _coordinator.RunSyncAsync(date, request.BranchId, cancellationToken);

        return new SyncTriggerResultDto
        {
            Synced = synced,
            Errors = errors
        };
    }
}
