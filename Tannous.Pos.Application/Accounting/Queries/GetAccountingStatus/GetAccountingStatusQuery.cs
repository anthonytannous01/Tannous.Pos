using MediatR;
using Tannous.Pos.Application.DTOs.Accounting;

namespace Tannous.Pos.Application.Accounting.Queries.GetAccountingStatus;

public class GetAccountingStatusQuery : IRequest<List<AccountingConnectionStatusDto>>
{
    public Guid? BranchId { get; set; }
}
