using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Domain.Interfaces;

/// <summary>Soft-delete purge operations for admin maintenance.</summary>
public interface IAdminPurgeRepository
{
    Task<IReadOnlyList<Customer>> GetSoftDeletedCustomersAsync(
        DateTime cutoff, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MenuItem>> GetSoftDeletedMenuItemsAsync(
        DateTime cutoff, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AddOn>> GetSoftDeletedAddOnsAsync(
        DateTime cutoff, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all three entity collections and persists atomically in one SaveChanges call.
    /// </summary>
    Task PurgeAsync(
        IReadOnlyList<Customer> customers,
        IReadOnlyList<MenuItem> menuItems,
        IReadOnlyList<AddOn> addOns,
        CancellationToken cancellationToken = default);
}
