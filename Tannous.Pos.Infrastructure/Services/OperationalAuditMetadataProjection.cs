using System.Text.Json;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>Safe metadata projection for internal audit diagnostics (no payload blobs).</summary>
internal static class OperationalAuditMetadataProjection
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> BlockedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "payload",
        "stackTrace",
        "stack",
        "exception",
        "body",
        "requestBody",
        "responseBody",
        "raw",
        "innerException"
    };

    private const int MaxKeys = 20;
    private const int MaxValueLength = 256;

    public static IReadOnlyDictionary<string, string> Project(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(metadataJson, JsonOptions);
            if (parsed == null || parsed.Count == 0)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in parsed)
            {
                if (result.Count >= MaxKeys)
                    break;

                if (BlockedKeys.Contains(key))
                    continue;

                var text = value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Number => value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => null,
                    _ => null
                };

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (text.Length > MaxValueLength)
                    text = text[..MaxValueLength];

                result[key] = text;
            }

            return result;
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
