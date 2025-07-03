using System.Text.Json;
using core.jarvis.data;
using Npgsql;

namespace core.jarvis.Data;

/// <summary>
/// Wrapper for PgClient to replace SupabaseClientWrapper.
/// </summary>
public class PgClientWrapper : IPgClient, IDisposable
{
    private readonly PgClient _pgClient;
    private readonly NpgsqlConnection _connection;
    private readonly bool _ownsConnection;
    private bool _disposed = false;
    private string? _currentJwt;
    
    public PgClientWrapper(string connectionString)
    {
        _connection = new NpgsqlConnection(connectionString);
        _pgClient = new PgClient(_connection);
        _ownsConnection = true;
    }
    
    public PgClientWrapper(NpgsqlConnection connection)
    {
        _connection = connection;
        _pgClient = new PgClient(connection);
        _ownsConnection = false;
    }
    
    /// <inheritdoc/>
    public PgClient Client => _pgClient;
    
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
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();
        return _connection;
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
            if (_ownsConnection)
            {
                _connection?.Dispose();
            }
            _disposed = true;
        }
    }
}