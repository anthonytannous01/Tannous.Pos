using MediatR;
using Tannous.Pos.Application.DTOs.Branches;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Branches.Queries.GetBranches;

public class GetBranchesQueryHandler : IRequestHandler<GetBranchesQuery, IEnumerable<BranchDto>>
{
    private readonly IBranchRepository _branchRepository;

    public GetBranchesQueryHandler(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository;
    }

    public async Task<IEnumerable<BranchDto>> Handle(
        GetBranchesQuery request, CancellationToken cancellationToken)
    {
        var branches = await _branchRepository.GetAllAsync(request.ActiveOnly, cancellationToken);
        return branches.Select(b => new BranchDto
        {
            Id           = b.Id,
            Name         = b.Name,
            Address      = b.Address,
            Phone        = b.Phone,
            IsActive     = b.IsActive,
            IsDefault    = b.IsDefault,
            DisplayOrder = b.DisplayOrder,
            CreatedAt    = b.CreatedAt
        });
    }
}
