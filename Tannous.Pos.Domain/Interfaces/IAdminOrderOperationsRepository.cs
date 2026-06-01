using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Domain.Interfaces;

/// <summary>Admin operations on orders — receipt reconciliation support.</summary>
public interface IAdminOrderOperationsRepository
{
    /// <summary>
    /// Returns all Paid orders that have no receipt number assigned, ordered by CreatedAt ascending.
    /// MUST be tracked (no AsNoTracking) — caller modifies ReceiptNumber and persists via CommitAsync.
    /// </summary>
    Task<IReadOnlyList<Order>> GetPaidOrdersWithoutReceiptsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the ReceiptNumber string of the most recently assigned receipt (by descending string sort),
    /// or null if no orders have receipt numbers.
    /// </summary>
    Task<string?> GetLastAssignedReceiptNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists tracked changes — call after mutating entity fields on tracked entities.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);
}
