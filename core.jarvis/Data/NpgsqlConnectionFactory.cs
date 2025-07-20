using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace core.jarvis.Data;

/// <summary>
/// Implements connection pooling for Npgsql using NpgsqlDataSource.
/// Provides high-performance, thread-safe connection management.
/// </summary>
public class NpgsqlConnectionFactory : INpgsqlConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<NpgsqlConnectionFactory> _logger;
    private readonly ConcurrentDictionary<NpgsqlConnection, DateTime> _activeConnections;
    private readonly int _maxPoolSize;
    private readonly int _minPoolSize;
    private bool _disposed;
    
    /// <summary>
    /// Initializes a new instance of the NpgsqlConnectionFactory.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="logger">The logger for connection pool events.</param>
    /// <param name="maxPoolSize">Maximum number of connections in the pool (default: 20).</param>
    /// <param name="minPoolSize">Minimum number of connections maintained in the pool (default: 5).</param>
    /// <param name="connectionLifetime">Maximum lifetime of a connection (default: 5 minutes).</param>
    public NpgsqlConnectionFactory(
        string connectionString,
        ILogger<NpgsqlConnectionFactory> logger,
        int maxPoolSize = 20,
        int minPoolSize = 5,
        TimeSpan? connectionLifetime = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activeConnections = new ConcurrentDictionary<NpgsqlConnection, DateTime>();
        _maxPoolSize = maxPoolSize;
        _minPoolSize = minPoolSize;
        
        // Build connection string with pooling parameters
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Pooling = true,
            MaxPoolSize = maxPoolSize,
            MinPoolSize = minPoolSize
        };
        
        if (connectionLifetime.HasValue)
        {
            builder.ConnectionLifetime = (int)connectionLifetime.Value.TotalSeconds;
        }
        
        // Create the data source with built-in pooling
        _dataSource = NpgsqlDataSource.Create(builder.ConnectionString);
        
        _logger.LogInformation(
            "Initialized connection pool with MaxPoolSize={MaxPoolSize}, MinPoolSize={MinPoolSize}", 
            maxPoolSize, minPoolSize);
    }
    
    /// <inheritdoc/>
    public async Task<NpgsqlConnection> GetConnectionAsync()
    {
        ThrowIfDisposed();
        
        try
        {
            var connection = await _dataSource.OpenConnectionAsync();
            _activeConnections.TryAdd(connection, DateTime.UtcNow);
            
            _logger.LogDebug(
                "Acquired connection from pool. Active connections: {ActiveCount}", 
                _activeConnections.Count);
            
            return connection;
        }
        catch (NpgsqlException ex) when (ex.Message.Contains("pool", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(ex, "Connection pool exhausted. Active connections: {ActiveCount}", 
                _activeConnections.Count);
            throw new InvalidOperationException(
                $"Connection pool exhausted. Current active connections: {_activeConnections.Count}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire connection from pool");
            throw;
        }
    }
    
    /// <inheritdoc/>
    public async Task ReturnConnectionAsync(NpgsqlConnection connection)
    {
        if (connection == null) return;
        
        try
        {
            _activeConnections.TryRemove(connection, out _);
            
            // Close the connection to return it to the pool
            await connection.CloseAsync();
            
            _logger.LogDebug(
                "Returned connection to pool. Active connections: {ActiveCount}", 
                _activeConnections.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error returning connection to pool");
            // Don't rethrow - we still want to try to clean up
            try
            {
                connection.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }
    
    /// <inheritdoc/>
    public ConnectionPoolStatistics GetPoolStatistics()
    {
        ThrowIfDisposed();
        
        // For now, return approximate statistics based on our tracking
        // NpgsqlDataSource.Statistics is internal in the current version
        return new ConnectionPoolStatistics
        {
            MaxPoolSize = _maxPoolSize,
            MinPoolSize = _minPoolSize,
            CurrentPoolSize = _activeConnections.Count, // Approximate
            ActiveConnections = _activeConnections.Count
        };
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
            try
            {
                // Clean up any tracked connections
                foreach (var connection in _activeConnections.Keys)
                {
                    try
                    {
                        connection.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }
                
                _activeConnections.Clear();
                
                // Dispose the data source
                _dataSource?.Dispose();
                
                _logger.LogInformation("Connection pool disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing connection pool");
            }
            
            _disposed = true;
        }
    }
    
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NpgsqlConnectionFactory));
        }
    }
}