using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Infrastructure.Data;

/// <summary>
/// Ensures bytea RowVersion tokens rotate on insert/update (PostgreSQL has no auto-updating rowversion column).
/// </summary>
public sealed class ByteaRowVersionSaveInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyRowVersions(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyRowVersions(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ApplyRowVersions(DbContext? context)
    {
        if (context == null)
            return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not (Order or InventoryItem or Shift))
                continue;

            var rowVersion = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "RowVersion");
            if (rowVersion == null)
                continue;

            if (entry.State == EntityState.Added)
            {
                rowVersion.CurrentValue = RandomNumberGenerator.GetBytes(8);
                continue;
            }

            if (entry.State != EntityState.Modified)
                continue;

            // Rotate only when the client has not diverged from the loaded token (preserves stale-row concurrency tests).
            var orig = rowVersion.OriginalValue as byte[];
            var cur = rowVersion.CurrentValue as byte[];
            if (orig is { Length: > 0 } && cur is { Length: > 0 } && orig.AsSpan().SequenceEqual(cur))
                rowVersion.CurrentValue = RandomNumberGenerator.GetBytes(8);
        }
    }
}
