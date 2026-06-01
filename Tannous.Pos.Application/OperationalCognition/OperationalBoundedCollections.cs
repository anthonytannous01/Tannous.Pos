namespace Tannous.Pos.Application.OperationalCognition;

/// <summary>Deterministic bounded collection policies for operational cognition outputs.</summary>
public static class OperationalBoundedCollections
{
    public static IReadOnlyList<T> TakeBounded<T>(IEnumerable<T> source, int maxCount)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (maxCount <= 0)
            return Array.Empty<T>();

        return source.Take(maxCount).ToList();
    }

    public static IReadOnlyList<T> TakeOrderedBounded<T, TKey>(
        IEnumerable<T> source,
        Func<T, TKey> orderKey,
        int maxCount)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(orderKey);
        if (maxCount <= 0)
            return Array.Empty<T>();

        return source
            .OrderBy(orderKey)
            .Take(maxCount)
            .ToList();
    }
}
