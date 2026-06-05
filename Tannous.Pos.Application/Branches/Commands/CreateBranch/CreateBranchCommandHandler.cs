using MediatR;
using Tannous.Pos.Application.DTOs.Branches;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Branches.Commands.CreateBranch;

public class CreateBranchCommandHandler : IRequestHandler<CreateBranchCommand, BranchDto>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork       _unitOfWork;

    public CreateBranchCommandHandler(IBranchRepository branchRepository, IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _unitOfWork       = unitOfWork;
    }

    public async Task<BranchDto> Handle(
        CreateBranchCommand request, CancellationToken cancellationToken)
    {
        if (request.IsDefault)
            await _branchRepository.ClearDefaultAsync(cancellationToken);

        var branch = new Branch
        {
            Name         = request.Name.Trim(),
            Address      = request.Address?.Trim(),
            Phone        = request.Phone?.Trim(),
            IsActive     = true,
            IsDefault    = request.IsDefault,
            DisplayOrder = request.DisplayOrder
        };

        await _branchRepository.AddAsync(branch, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return new BranchDto
        {
            Id           = branch.Id,
            Name         = branch.Name,
            Address      = branch.Address,
            Phone        = branch.Phone,
            IsActive     = branch.IsActive,
            IsDefault    = branch.IsDefault,
            DisplayOrder = branch.DisplayOrder,
            CreatedAt    = branch.CreatedAt
        };
    }
}
