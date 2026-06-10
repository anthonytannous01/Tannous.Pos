using MediatR;
using Tannous.Pos.Application.DTOs.Accounting;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Accounting.Commands.TriggerAccountingSync;

public class TriggerAccountingSyncCommand : IRequest<SyncTriggerResultDto>
{
    public DateTime? Date { get; set; }
    public Guid? BranchId { get; set; }
}
