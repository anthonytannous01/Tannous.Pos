namespace Tannous.Pos.Domain.Interfaces;

/// <summary>Read-only reporting interface for cash drawer event aggregations.</summary>
public interface ICashDrawerEventRepository
{
    /// <summary>
    /// Returns the sum of cash drop amounts (EventType == "Drop") in the given UTC range [from, to).
    /// </summary>
    Task<decimal> GetDropTotalAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
