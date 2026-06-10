using MediatR;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Accounting.Commands.DisconnectAccounting;

public class DisconnectAccountingCommand : IRequest<bool>
{
    public AccountingProvider Provider { get; set; }
    public Guid? BranchId { get; set; }
}
