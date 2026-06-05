using MediatR;
using Tannous.Pos.Application.DTOs.Branches;

namespace Tannous.Pos.Application.Branches.Commands.CreateBranch;

public class CreateBranchCommand : IRequest<BranchDto>
{
    public string  Name         { get; set; } = string.Empty;
    public string? Address      { get; set; }
    public string? Phone        { get; set; }
    public bool    IsDefault    { get; set; } = false;
    public int     DisplayOrder { get; set; } = 0;
}
