using System.Text.Json;

namespace LeadHub.Infra;

/// <summary>
/// Converts between the plain-CLR-object Payload dictionaries the core works with and the JSON
/// representation stored in Postgres (jsonb column).
/// </summary>
public static class JsonPayloadSerializer
{
    public static string Serialize(IReadOnlyDictionary<string, object?> payload) =>
        JsonSerializer.Serialize(payload);

    public static Dictionary<string, object?> Deserialize(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ToDictionary(document.RootElement);
    }

    private static Dictionary<string, object?> ToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object?>();
        foreach (var property in element.EnumerateObject())
            result[property.Name] = ToValue(property.Value);

        return result;
    }

    private static object? ToValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => ToDictionary(element),
        JsonValueKind.Array => element.EnumerateArray().Select(ToValue).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };
}
