using System.Text.Json;
using Newtonsoft.Json;

namespace core.jarvis.Data;

/// <summary>
/// Represents a snapshot record for a component, containing all historical versions.
/// Table: component_snapshots (automatic snake_case mapping from ComponentSnapshots)
/// </summary>
public class ComponentSnapshots
{
    /// <summary>
    /// Unique identifier for the snapshot record.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Entity ID this snapshot belongs to.
    /// Maps to entity_id in database.
    /// </summary>
    public Guid EntityId { get; set; }
    
    /// <summary>
    /// Type of component being snapshotted.
    /// Maps to component_type in database.
    /// </summary>
    public string ComponentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Component ID being snapshotted.
    /// Maps to component_id in database.
    /// </summary>
    public Guid ComponentId { get; set; }
    
    /// <summary>
    /// JSON string containing snapshot data.
    /// Maps to snapshots column in database.
    /// </summary>
    public string Snapshots { get; set; } = "[]";
    
    /// <summary>
    /// Created timestamp.
    /// Maps to created_at in database.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Updated timestamp.
    /// Maps to last_updated in database.
    /// </summary>
    public DateTime LastUpdated { get; set; }
    
    /// <summary>
    /// Gets the snapshot entries from the JSON document
    /// </summary>
    public List<Snapshot> GetSnapshots()
    {
        var snapshots = new List<Snapshot>();
        using var doc = JsonDocument.Parse(Snapshots);
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var snapshot = new Snapshot
            {
                Version = element.GetProperty("version").GetInt32(),
                Data = JsonDocument.Parse(element.GetProperty("data").GetRawText()),
                Operation = element.GetProperty("operation").GetString() ?? "UPDATE",
                Timestamp = element.GetProperty("timestamp").GetDateTime(),
                CreatedBy = element.GetProperty("created_by").GetString() ?? "system"
            };
            snapshots.Add(snapshot);
        }
        return snapshots;
    }
}

/// <summary>
/// Represents a single snapshot of a component at a point in time
/// </summary>
public class Snapshot
{
    public int Version { get; set; }
    
    // Store JSON data as string to avoid Dapper serialization issues
    private string _dataJson = "{}";
    
    [JsonIgnore]
    public JsonDocument Data 
    { 
        get => JsonDocument.Parse(_dataJson);
        set => _dataJson = value.RootElement.GetRawText();
    }
    
    // Property for serialization
    [JsonProperty("data")]
    public string DataJson 
    { 
        get => _dataJson;
        set => _dataJson = value;
    }
    
    public string Operation { get; set; } = "UPDATE";
    public DateTime Timestamp { get; set; }
    public string CreatedBy { get; set; } = "system";
    
    /// <summary>
    /// Deserializes the snapshot data to the specified component type
    /// </summary>
    public T Deserialize<T>() where T : class, IComponent
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(Data.RootElement.GetRawText()) 
            ?? throw new InvalidOperationException($"Failed to deserialize snapshot to {typeof(T).Name}");
    }
}