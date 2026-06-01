using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Domain.Interfaces;

/// <summary>
/// Repository for the single BusinessSettings record.
/// This entity follows a singleton-row pattern — at most one row exists.
/// </summary>
public interface IBusinessSettingsRepository
{
    /// <summary>Returns the BusinessSettings record, or null if it has never been created.</summary>
    Task<BusinessSettings?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new BusinessSettings entity and persists it. Call only when GetAsync returns null.</summary>
    Task CreateAsync(BusinessSettings settings, CancellationToken cancellationToken = default);

    /// <summary>Persists tracked changes to an already-loaded BusinessSettings entity.</summary>
    Task UpdateAsync(CancellationToken cancellationToken = default);
}
