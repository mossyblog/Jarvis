using core.jarvis.data;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace core.jarvis.Data;

/// <summary>
/// Pooled implementation of IPgClient that acquires connections per-operation.
/// Enables concurrent database operations within a single scope.
/// </summary>
public class PgClientPooled : IPgClient
{
    private readonly INpgsqlConnectionFactory _connectionFactory;
    private readonly ILogger<PgClientPooled> _logger;
    private string? _jwt;
    private bool _disposed;
    
    /// <summary>
    /// Initializes a new instance of the PgClientPooled class.
    /// </summary>
    /// <param name="connectionFactory">The connection factory for pooled connections.</param>
    /// <param name="logger">The logger for operation tracking.</param>
    public PgClientPooled(
        INpgsqlConnectionFactory connectionFactory,
        ILogger<PgClientPooled> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <inheritdoc/>
    /// <remarks>
    /// This property is not supported in pooled mode as each operation uses a different connection.
    /// </remarks>
    public PgClient Client => throw new NotSupportedException(
        "Direct client access is not supported in pooled mode. Use operation methods instead.");
    
    /// <inheritdoc/>
    public PgTable<T> From<T>() where T : class, new()
    {
        // This is a complex case - we need to defer connection acquisition
        // For now, throw to indicate this pattern needs refactoring
        throw new NotSupportedException(
            "Direct table access is not yet supported in pooled mode. " +
            "This requires refactoring to support deferred connection acquisition.");
    }
    
    /// <inheritdoc/>
    public void SetJwt(string jwt)
    {
        _jwt = jwt;
        _logger.LogDebug("JWT set for pooled client");
    }
    
    /// <inheritdoc/>
    public void JWT(string jwt)
    {
        SetJwt(jwt);
    }
    
    /// <inheritdoc/>
    public bool HasValidJWT()
    {
        return !string.IsNullOrEmpty(_jwt);
    }
    
    /// <inheritdoc/>
    public string? GetJWT()
    {
        return _jwt;
    }
    
    /// <inheritdoc/>
    public Dictionary<string, string>? GetJWTClaims()
    {
        if (string.IsNullOrEmpty(_jwt))
            return null;
            
        try
        {
            // Simple JWT parsing - in production use proper JWT library
            var parts = _jwt.Split('.');
            if (parts.Length != 3)
                return null;

            var payload = parts[1];
            var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(padded);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            
            var claims = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            return claims?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JWT claims");
            return null;
        }
    }
    
    /// <inheritdoc/>
    public async Task<NpgsqlConnection> GetConnectionAsync()
    {
        ThrowIfDisposed();
        return await _connectionFactory.GetConnectionAsync();
    }
    
    /// <inheritdoc/>
    public async Task<T> ExecuteScalar<T>(string query, object? parameters = null)
    {
        return await ExecuteWithConnectionAsync(async pgClient =>
        {
            var connection = await GetConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);
            
            if (parameters != null)
            {
                AddParameters(command, parameters);
            }
            
            var result = await command.ExecuteScalarAsync();
            return (T)Convert.ChangeType(result, typeof(T));
        });
    }
    
    /// <inheritdoc/>
    public async Task Execute(string command, object? parameters = null)
    {
        await ExecuteWithConnectionAsync<bool>(async pgClient =>
        {
            var connection = await GetConnectionAsync();
            await using var cmd = new NpgsqlCommand(command, connection);
            
            if (parameters != null)
            {
                AddParameters(cmd, parameters);
            }
            
            await cmd.ExecuteNonQueryAsync();
            return true;
        });
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<T>> Query<T>(string query, object? parameters = null)
    {
        return await ExecuteWithConnectionAsync(async pgClient =>
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
            
            return (IEnumerable<T>)results;
        });
    }
    
    private void AddParameters(NpgsqlCommand command, object parameters)
    {
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
    
    /// <summary>
    /// Executes an operation with a pooled connection.
    /// The connection is automatically acquired and returned to the pool.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute with the connection.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<T> ExecuteWithConnectionAsync<T>(Func<PgClient, Task<T>> operation)
    {
        ThrowIfDisposed();
        
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));
        
        var connection = await _connectionFactory.GetConnectionAsync();
        try
        {
            var pgClient = new PgClient(connection, null, _logger);
            
            // Apply JWT if set
            if (!string.IsNullOrEmpty(_jwt))
            {
                pgClient.JWT(_jwt);
            }
            
            _logger.LogDebug("Executing pooled operation with connection");
            return await operation(pgClient);
        }
        finally
        {
            await _connectionFactory.ReturnConnectionAsync(connection);
        }
    }
    
    /// <summary>
    /// Executes an operation with a pooled connection without returning a result.
    /// The connection is automatically acquired and returned to the pool.
    /// </summary>
    /// <param name="operation">The operation to execute with the connection.</param>
    public async Task ExecuteWithConnectionAsync(Func<PgClient, Task> operation)
    {
        await ExecuteWithConnectionAsync(async pgClient =>
        {
            await operation(pgClient);
            return true; // Dummy return value
        });
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
            // Nothing to dispose directly - the factory manages connections
            _logger.LogDebug("PgClientPooled disposed");
            _disposed = true;
        }
    }
    
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PgClientPooled));
        }
    }
}