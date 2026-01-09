using System.Diagnostics;
using System.Text.Json;
using core.jarvis.data;
using Npgsql;
using Microsoft.Extensions.Logging;

namespace core.jarvis.Data;

/// <summary>
/// Wrapper for PgClient to replace SupabaseClientWrapper.
/// </summary>
public class PgClientWrapper : IPgClient, IDisposable
{
    private readonly PgClient _pgClient;
    private readonly NpgsqlConnection _connection;
    private readonly bool _ownsConnection;
    private readonly ILogger? _logger;
    private bool _disposed = false;
    private string? _currentJwt;
    
    // Activity source for distributed tracing and performance monitoring
    private static readonly ActivitySource ActivitySource = new ActivitySource("Jarvis.Database.PgClientWrapper");
    
    public PgClientWrapper(string connectionString, ILogger? logger = null)
    {
        _connection = new NpgsqlConnection(connectionString);
        _logger = logger;
        _pgClient = new PgClient(_connection, null, null); // Disable verbose data layer logging
        _ownsConnection = true;
    }
    
    public PgClientWrapper(NpgsqlConnection connection, bool ownsConnection = true, ILogger? logger = null)
    {
        _connection = connection;
        _logger = logger;
        _pgClient = new PgClient(connection, null, null); // Disable verbose data layer logging
        _ownsConnection = ownsConnection;
    }

    /// <inheritdoc/>
    public PgTable<T> From<T>() where T : class, new()
    {
        return _pgClient.From<T>();
    }
    
    /// <inheritdoc/>
    public void SetJwt(string jwt)
    {
        _currentJwt = jwt;
        _pgClient.JWT(jwt);
    }
    
    /// <inheritdoc/>
    public void JWT(string jwt)
    {
        _currentJwt = jwt;
        _pgClient.JWT(jwt);
    }
    
    /// <inheritdoc/>
    public bool HasValidJWT()
    {
        return !string.IsNullOrEmpty(_currentJwt);
    }
    
    /// <inheritdoc/>
    public string? GetJWT()
    {
        return _currentJwt;
    }
    
    /// <inheritdoc/>
    public Dictionary<string, string>? GetJWTClaims()
    {
        if (string.IsNullOrEmpty(_currentJwt))
            return null;
            
        try
        {
            // Simple JWT parsing - in production use proper JWT library
            var parts = _currentJwt.Split('.');
            if (parts.Length != 3)
                return null;

            var payload = parts[1];
            var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(padded);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            
            var claims = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            return claims?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "");
        }
        catch
        {
            return null;
        }
    }
    
    /// <inheritdoc/>
    public async Task<NpgsqlConnection> GetConnectionAsync()
    {
        using var activity = ActivitySource.StartActivity("PgClientWrapper.GetConnection");
        activity?.SetTag("connection_state_before", _connection.State.ToString());
        
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            using (var openActivity = ActivitySource.StartActivity("PgClientWrapper.OpenConnection"))
            {
                openActivity?.SetTag("connection_string", _connection.ConnectionString?.Split(';')[0]); // Log only host part for security
                await _connection.OpenAsync();
                openActivity?.SetTag("connection_state_after", _connection.State.ToString());
            }
        }
        
        activity?.SetTag("connection_state_final", _connection.State.ToString());
        return _connection;
    }
    
    /// <inheritdoc/>
    public async Task<T> ExecuteScalar<T>(string query, object? parameters = null)
    {
        var connection = await GetConnectionAsync();
        await using var command = new NpgsqlCommand(query, connection);
        
        if (parameters != null)
        {
            AddParameters(command, parameters);
        }
        
        var result = await command.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result, typeof(T));
    }
    
    /// <inheritdoc/>
    public async Task Execute(string command, object? parameters = null)
    {
        var connection = await GetConnectionAsync();
        await using var cmd = new NpgsqlCommand(command, connection);
        
        if (parameters != null)
        {
            AddParameters(cmd, parameters);
        }
        
        await cmd.ExecuteNonQueryAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<T>> Query<T>(string query, object? parameters = null)
    {
        var connection = await GetConnectionAsync();
        await using var command = new NpgsqlCommand(query, connection);
        
        if (parameters != null)
        {
            AddParameters(command, parameters);
        }
        
        var results = new List<T>();
        await using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var item = MapReaderToObject<T>(reader);
            results.Add(item);
        }
        
        return results;
    }
    
    private void AddParameters(NpgsqlCommand command, object parameters)
    {
        // Handle ExpandoObject and other IDictionary<string, object> types
        if (parameters is IDictionary<string, object> dict)
        {
            foreach (var kvp in dict)
            {
                command.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
            }
            return;
        }

        // Handle regular objects via reflection
        var properties = parameters.GetType().GetProperties();
        foreach (var prop in properties)
        {
            var value = prop.GetValue(parameters);
            command.Parameters.AddWithValue($"@{prop.Name}", value ?? DBNull.Value);
        }
    }
    
    private T MapReaderToObject<T>(NpgsqlDataReader reader)
    {
        var type = typeof(T);
        var instance = Activator.CreateInstance<T>();
        
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);
            var property = type.GetProperty(columnName, 
                System.Reflection.BindingFlags.IgnoreCase | 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Instance);
            
            if (property != null && property.CanWrite && !reader.IsDBNull(i))
            {
                var value = reader.GetValue(i);
                var convertedValue = Convert.ChangeType(value, property.PropertyType);
                property.SetValue(instance, convertedValue);
            }
        }
        
        return instance;
    }
    
    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            if (_ownsConnection && _connection != null)
            {
                try
                {
                    // Only close if the connection was actually opened
                    if (_connection.State != System.Data.ConnectionState.Closed)
                    {
                        _connection.Close();
                    }
                    _connection.Dispose();
                }
                catch (NullReferenceException)
                {
                    // Npgsql can throw NullReferenceException when disposing
                    // a connection that was never fully initialized
                }
            }
            _disposed = true;
        }
    }
}