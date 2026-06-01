using MediatR;
using Tannous.Pos.Application.DTOs.Admin;

namespace Tannous.Pos.Application.Admin.Commands.ReconcileReceipts;

public class ReconcileReceiptsCommand : IRequest<ReconcileReceiptsResultDto>
{
}
