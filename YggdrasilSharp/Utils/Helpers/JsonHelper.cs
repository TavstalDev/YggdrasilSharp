using System.Text.Json;

namespace Tavstal.YggdrasilSharp.Utils.Helpers;

/// <summary>
/// Provides JSON serialization helpers using camelCase property names and indented output.
/// </summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Serializes an object to JSON.
    /// </summary>
    /// <typeparam name="T">The type of the object being serialized.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>The JSON representation of <paramref name="obj"/>.</returns>
    public static string SerializeJson<T>(T obj) =>
        JsonSerializer.Serialize(obj, _jsonOptions);

    /// <summary>
    /// Deserializes a JSON string into an object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize into.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object, or the default value of <typeparamref name="T"/> when the JSON is malformed.</returns>
    public static T? DeserializeJson<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            // Handle the exception as needed, e.g., log it or return a default value
            return default;
        }
    }
}