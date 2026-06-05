using MediatR;
using Tannous.Pos.Application.DTOs.Branches;

namespace Tannous.Pos.Application.Branches.Queries.GetBranches;

public class GetBranchesQuery : IRequest<IEnumerable<BranchDto>>
{
    /// <summary>When true, only active branches are returned. Defaults to true.</summary>
    public bool ActiveOnly { get; set; } = true;
}
