using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Domain.Interfaces;

/// <summary>
/// Pushes a single day's summarised sales as a journal entry to an accounting platform.
/// Implementations must be non-throwing — return false and populate errorMessage on failure.
/// </summary>
public interface IAccountingSync
{
    AccountingProvider Provider { get; }

    /// <summary>Exchange an OAuth2 auth code for tokens and save to AccountingConnection.</summary>
    Task<bool> ExchangeCodeAsync(string code, string? branchId, CancellationToken ct = default);

    /// <summary>Refresh the access token if it has expired.</summary>
    Task<bool> RefreshTokenAsync(AccountingConnection connection, CancellationToken ct = default);

    /// <summary>
    /// Push one day's sales summary (net sales, tax, payment methods) as a journal entry.
    /// Returns (success, externalRef, errorMessage).
    /// </summary>
    Task<(bool Success, string? ExternalRef, string? Error)> SyncDayAsync(
        AccountingConnection connection,
        DateTime             date,
        CancellationToken    ct = default);
}
