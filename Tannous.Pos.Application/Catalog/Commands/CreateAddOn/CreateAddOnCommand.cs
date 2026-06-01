using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;

namespace Tannous.Pos.Application.Catalog.Commands.CreateAddOn;

public class CreateAddOnCommand : IRequest<AddOnDto>
{
    public CreateAddOnDto AddOn { get; set; } = new();
}
