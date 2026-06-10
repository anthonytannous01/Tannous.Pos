using MediatR;
using Tannous.Pos.Application.DTOs.Kds;

namespace Tannous.Pos.Application.Kds.Commands.CreateKdsStation;

public class CreateKdsStationCommand : IRequest<KdsStationDto>
{
    public string  Name         { get; set; } = string.Empty;
    public string? NameAr       { get; set; }
    public string? Color        { get; set; }
    public int     DisplayOrder { get; set; }
    public Guid?   BranchId     { get; set; }
}
