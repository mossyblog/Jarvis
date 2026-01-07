using Newtonsoft.Json;

namespace core.jarvis.api.Security;

/// <summary>
/// Provides secure JSON serialization settings to prevent deserialization attacks.
///
/// SECURITY: TypeNameHandling.None prevents deserialization of type metadata that could
/// be exploited for Remote Code Execution (RCE) attacks via gadget chains.
///
/// MaxDepth limits prevent stack overflow attacks via deeply nested JSON.
/// </summary>
public static class SafeJsonSettings
{
    /// <summary>
    /// Default safe settings for JSON deserialization.
    /// - TypeNameHandling.None: Prevents type-based RCE attacks
    /// - MaxDepth: 32: Prevents stack overflow via deep nesting
    /// </summary>
    public static readonly JsonSerializerSettings Default = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.None,
        MaxDepth = 32
    };

    /// <summary>
    /// Safely deserialize JSON to the specified type.
    /// </summary>
    /// <typeparam name="T">Target type for deserialization.</typeparam>
    /// <param name="json">JSON string to deserialize.</param>
    /// <returns>Deserialized object or null.</returns>
    public static T? Deserialize<T>(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return default;

        return JsonConvert.DeserializeObject<T>(json, Default);
    }

    /// <summary>
    /// Safely serialize an object to JSON.
    /// </summary>
    /// <param name="value">Object to serialize.</param>
    /// <returns>JSON string.</returns>
    public static string Serialize(object? value)
    {
        return JsonConvert.SerializeObject(value, Default);
    }
}
