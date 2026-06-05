using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Domain.Interfaces;

public interface IBranchRepository
{
    Task<IEnumerable<Branch>> GetAllAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<Branch?> GetDefaultAsync(CancellationToken cancellationToken = default);
    Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Branch branch, CancellationToken cancellationToken = default);
    /// <summary>Clears IsDefault on all current default branches (to enforce single-default invariant).</summary>
    Task ClearDefaultAsync(CancellationToken cancellationToken = default);
}
