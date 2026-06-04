using MediatR;
using Tannous.Pos.Application.DTOs.Kds;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Kds.Commands.UpdateKdsStatus;

public class UpdateKdsStatusCommand : IRequest<KdsTicketDto>
{
    public Guid OrderLineId { get; set; }
    public KdsStatus NewStatus { get; set; }
}
