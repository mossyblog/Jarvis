using core.jarvis.data;

namespace core.jarvis.Data;

/// <summary>
/// Interface for PostgreSQL client operations, replacing Supabase client.
/// </summary>
public interface IPgClient : IDisposable
{
    /// <summary>
    /// Gets the underlying PgClient instance.
    /// </summary>
    PgClient Client { get; }
    
    /// <summary>
    /// Gets a table accessor for the specified entity type.
    /// </summary>
    PgTable<T> From<T>() where T : class, new();
    
    /// <summary>
    /// Sets the JWT token for authentication.
    /// </summary>
    void SetJwt(string jwt);
    
    /// <summary>
    /// Sets the JWT token for authentication (alias for SetJwt).
    /// </summary>
    void JWT(string jwt);
    
    /// <summary>
    /// Checks if a valid JWT is currently set.
    /// </summary>
    bool HasValidJWT();
    
    /// <summary>
    /// Gets the current JWT token if set.
    /// </summary>
    string? GetJWT();
    
    /// <summary>
    /// Gets the parsed JWT claims.
    /// </summary>
    Dictionary<string, string>? GetJWTClaims();
    
    /// <summary>
    /// Gets the underlying database connection.
    /// </summary>
    Task<Npgsql.NpgsqlConnection> GetConnectionAsync();
}