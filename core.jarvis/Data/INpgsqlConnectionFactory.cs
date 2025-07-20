using Npgsql;

namespace core.jarvis.Data;

/// <summary>
/// Factory interface for managing pooled Npgsql connections.
/// Provides connection pooling capabilities for concurrent database operations.
/// </summary>
public interface INpgsqlConnectionFactory : IDisposable
{
    /// <summary>
    /// Gets a connection from the pool for a single operation.
    /// The connection will be automatically opened if not already open.
    /// </summary>
    /// <returns>An open NpgsqlConnection from the pool.</returns>
    Task<NpgsqlConnection> GetConnectionAsync();
    
    /// <summary>
    /// Returns a connection to the pool.
    /// The connection will be reset and made available for reuse.
    /// </summary>
    /// <param name="connection">The connection to return to the pool.</param>
    Task ReturnConnectionAsync(NpgsqlConnection connection);
    
    /// <summary>
    /// Gets the current pool statistics for monitoring.
    /// </summary>
    ConnectionPoolStatistics GetPoolStatistics();
}

/// <summary>
/// Provides statistics about the connection pool for monitoring and debugging.
/// </summary>
public record ConnectionPoolStatistics
{
    /// <summary>
    /// Gets the maximum number of connections allowed in the pool.
    /// </summary>
    public int MaxPoolSize { get; init; }
    
    /// <summary>
    /// Gets the minimum number of connections maintained in the pool.
    /// </summary>
    public int MinPoolSize { get; init; }
    
    /// <summary>
    /// Gets the current number of connections in the pool.
    /// </summary>
    public int CurrentPoolSize { get; init; }
    
    /// <summary>
    /// Gets the number of active (in-use) connections.
    /// </summary>
    public int ActiveConnections { get; init; }
    
    /// <summary>
    /// Gets the number of idle connections available in the pool.
    /// </summary>
    public int IdleConnections => CurrentPoolSize - ActiveConnections;
}