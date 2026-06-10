using MediatR;
using Tannous.Pos.Application.DTOs.Kds;

namespace Tannous.Pos.Application.Kds.Commands.UpdateKdsStation;

public class UpdateKdsStationCommand : IRequest<KdsStationDto>
{
    public Guid    Id           { get; set; }
    public string  Name         { get; set; } = string.Empty;
    public string? NameAr       { get; set; }
    public string? Color        { get; set; }
    public int     DisplayOrder { get; set; }
    public bool    IsActive     { get; set; } = true;
}
