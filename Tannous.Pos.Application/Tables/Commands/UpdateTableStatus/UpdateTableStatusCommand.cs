using MediatR;
using Tannous.Pos.Application.DTOs.Tables;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Tables.Commands.UpdateTableStatus;

public class UpdateTableStatusCommand : IRequest<TableDto>
{
    public Guid TableId { get; set; }
    public TableStatus NewStatus { get; set; }
}
