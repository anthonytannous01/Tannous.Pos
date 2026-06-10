using System.Globalization;
using System.Text.Json;

namespace Tannous.Pos.Infrastructure.Delivery.Adapters;

/// <summary>
/// Defensive JSON accessors for delivery webhook parsing — tolerant of missing properties
/// and of numbers delivered as JSON strings (common across external platforms).
/// </summary>
internal static class JsonElementExtensions
{
    public static string GetStringOrEmpty(this JsonElement element, string property)
        => element.GetStringOrNull(property) ?? string.Empty;

    public static string? GetStringOrNull(this JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            _ => null
        };
    }

    public static decimal GetDecimalOrZero(this JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value)) return 0m;
        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetDecimal(out var d) ? d : 0m,
            JsonValueKind.String => decimal.TryParse(
                value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s) ? s : 0m,
            _ => 0m
        };
    }

    public static int? GetIntOrNull(this JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetInt32(out var i) ? i : (int?)null,
            JsonValueKind.String => int.TryParse(
                value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s) ? s : (int?)null,
            _ => null
        };
    }
}
