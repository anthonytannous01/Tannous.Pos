using Microsoft.EntityFrameworkCore;

namespace Tannous.Pos.Application.Orders;

/// <summary>
/// Shared formatting for optimistic concurrency operator logs (not exposed on API contracts).
/// </summary>
internal static class ConcurrencyConflictObservability
{
    public static string FormatAffectedClrTypeNames(DbUpdateConcurrencyException ex)
    {
        try
        {
            return string.Join(
                ", ",
                ex.Entries
                    .Select(e => e.Metadata?.ClrType?.Name ?? e.Entity.GetType().Name)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal));
        }
        catch
        {
            return "unknown";
        }
    }
}
