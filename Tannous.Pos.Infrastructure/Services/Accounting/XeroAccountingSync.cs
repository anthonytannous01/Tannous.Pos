using Microsoft.Extensions.Logging;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Infrastructure.Services.Accounting;

/// <summary>
/// Xero integration stub — registration pattern in place; full OAuth + sync is a future step.
/// </summary>
public sealed class XeroAccountingSync : IAccountingSync
{
    private readonly ILogger<XeroAccountingSync> _logger;

    public XeroAccountingSync(ILogger<XeroAccountingSync> logger) => _logger = logger;

    public AccountingProvider Provider => AccountingProvider.Xero;

    public Task<bool> ExchangeCodeAsync(string code, string? branchId, CancellationToken ct = default)
    {
        _logger.LogWarning("Xero integration coming soon — ExchangeCodeAsync not implemented");
        return Task.FromResult(false);
    }

    public Task<bool> RefreshTokenAsync(AccountingConnection connection, CancellationToken ct = default)
    {
        _logger.LogWarning("Xero integration coming soon — RefreshTokenAsync not implemented");
        return Task.FromResult(false);
    }

    public Task<(bool Success, string? ExternalRef, string? Error)> SyncDayAsync(
        AccountingConnection connection, DateTime date, CancellationToken ct = default)
        => Task.FromResult<(bool, string?, string?)>((false, null, "Xero integration not yet implemented"));
}
