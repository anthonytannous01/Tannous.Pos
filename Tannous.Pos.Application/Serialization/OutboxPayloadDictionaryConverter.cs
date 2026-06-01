using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tannous.Pos.Application.Serialization;

/// <summary>
/// Accepts payload as either a JSON object or a JSON string containing an object (mobile outbox encodes payload as a string).
/// </summary>
public sealed class OutboxPayloadDictionaryConverter : JsonConverter<Dictionary<string, object?>>
{
    public override Dictionary<string, object?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return new Dictionary<string, object?>();

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str))
                return new Dictionary<string, object?>();
            using var doc = JsonDocument.Parse(str);
            return FlattenObject(doc.RootElement);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            return FlattenObject(doc.RootElement);
        }

        throw new JsonException("Payload must be a JSON object or a JSON string containing an object.");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, object?> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }

    private static Dictionary<string, object?> FlattenObject(JsonElement root)
    {
        var dict = new Dictionary<string, object?>();
        if (root.ValueKind != JsonValueKind.Object)
            return dict;

        foreach (var p in root.EnumerateObject())
            dict[p.Name] = ToScalar(p.Value);
        return dict;
    }

    private static object? ToScalar(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => e.GetString(),
        JsonValueKind.Number => e.TryGetDecimal(out var d) ? d : e.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => e.GetRawText()
    };
}
