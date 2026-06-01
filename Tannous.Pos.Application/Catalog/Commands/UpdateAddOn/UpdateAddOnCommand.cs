using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;

namespace Tannous.Pos.Application.Catalog.Commands.UpdateAddOn;

public class UpdateAddOnCommand : IRequest<AddOnDto>
{
    public Guid Id { get; set; }
    public UpdateAddOnDto AddOn { get; set; } = new();
}
