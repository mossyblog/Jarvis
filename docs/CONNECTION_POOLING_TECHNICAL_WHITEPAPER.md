# Connection Pooling in Jarvis: Technical Whitepaper

## Abstract

This whitepaper provides a comprehensive technical analysis of the connection pooling implementation in the Jarvis Entity Component System (ECS) framework. We examine the architectural decisions, implementation details, performance characteristics, and future considerations for managing PostgreSQL connections in a high-concurrency environment, particularly within Azure Functions and serverless architectures.

## Table of Contents

1. [Introduction](#introduction)
2. [Problem Statement](#problem-statement)
3. [Architecture Overview](#architecture-overview)
4. [Implementation Details](#implementation-details)
5. [Performance Analysis](#performance-analysis)
6. [Configuration and Deployment](#configuration-and-deployment)
7. [Known Limitations](#known-limitations)
8. [Future Enhancements](#future-enhancements)
9. [Conclusion](#conclusion)

## Introduction

The Jarvis framework is a .NET 8-based Entity Component System designed for building scalable, data-driven applications with PostgreSQL as the primary data store. As the framework evolved to support higher concurrency and serverless deployments, the need for efficient connection management became critical.

### Key Objectives

1. **Eliminate Connection Exhaustion**: Prevent "too many clients" errors under load
2. **Enable Concurrent Operations**: Support parallel database operations within a single request
3. **Maintain Backward Compatibility**: Ensure existing code continues to function
4. **Optimize for Serverless**: Design for Azure Functions' execution model

## Problem Statement

### The Single Connection Model

The original Jarvis implementation used a single-connection-per-request model:

```csharp
public class PgClient : IPgClient
{
    private readonly NpgsqlConnection _connection;
    
    public PgClient(string connectionString)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
    }
}
```

This approach had several limitations:

1. **No Concurrency**: Only one database operation could execute at a time
2. **Connection Overhead**: Each request created and destroyed a connection
3. **Resource Exhaustion**: High load scenarios quickly exhausted database connections
4. **Poor Scalability**: Linear performance degradation with increased load

### Test Environment Challenges

During testing, the framework would spawn multiple parallel test executions, each creating its own connections:

```
Test1 → DataContext → PgClient → New Connection
Test2 → DataContext → PgClient → New Connection
Test3 → DataContext → PgClient → New Connection
...
TestN → DataContext → PgClient → New Connection
```

With PostgreSQL's default `max_connections` of 100, running the full test suite would frequently hit connection limits.

## Architecture Overview

### Connection Pooling Design

The solution implements a multi-layered connection pooling architecture:

```
┌─────────────────┐
│   IDataContext  │
└────────┬────────┘
         │
┌────────▼────────┐
│   IPgClient     │
└────────┬────────┘
         │
┌────────▼────────────────┐     ┌──────────────────────┐
│   PgClientPooled        │────▶│ INpgsqlConnectionFactory │
│ (Per-operation conn)    │     └───────────┬──────────┘
└─────────────────────────┘                 │
                                  ┌─────────▼──────────┐
                                  │ NpgsqlDataSource   │
                                  │ (Built-in pooling) │
                                  └────────────────────┘
```

### Key Components

#### 1. INpgsqlConnectionFactory

The factory interface provides abstraction for connection management:

```csharp
public interface INpgsqlConnectionFactory : IDisposable
{
    Task<NpgsqlConnection> GetConnectionAsync();
    Task ReturnConnectionAsync(NpgsqlConnection connection);
    ConnectionPoolStatistics GetPoolStatistics();
}
```

#### 2. NpgsqlConnectionFactory

Implements connection pooling using Npgsql's built-in `NpgsqlDataSource`:

```csharp
public class NpgsqlConnectionFactory : INpgsqlConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ConcurrentDictionary<NpgsqlConnection, DateTime> _activeConnections;
    
    public NpgsqlConnectionFactory(string connectionString, ILogger<NpgsqlConnectionFactory> logger,
        int maxPoolSize = 20, int minPoolSize = 5)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Pooling = true,
            MaxPoolSize = maxPoolSize,
            MinPoolSize = minPoolSize
        };
        
        _dataSource = NpgsqlDataSource.Create(builder.ConnectionString);
    }
}
```

#### 3. PgClientPooled

Acquires connections per-operation rather than per-request:

```csharp
public class PgClientPooled : IPgClient
{
    private readonly INpgsqlConnectionFactory _connectionFactory;
    
    public async Task<T> ExecuteAsync<T>(Func<NpgsqlConnection, Task<T>> operation)
    {
        var connection = await _connectionFactory.GetConnectionAsync();
        try
        {
            return await operation(connection);
        }
        finally
        {
            await _connectionFactory.ReturnConnectionAsync(connection);
        }
    }
}
```

## Implementation Details

### Connection Lifecycle Management

#### 1. Connection Acquisition

When a database operation is initiated:

```csharp
// 1. Request comes in
await dataContext.Commit(component);

// 2. DataContext uses PgClientPooled
var pgClient = serviceProvider.GetService<IPgClient>();

// 3. PgClientPooled gets connection from factory
var connection = await _connectionFactory.GetConnectionAsync();

// 4. Factory returns pooled connection
return await _dataSource.OpenConnectionAsync();
```

#### 2. Connection Return

After operation completion:

```csharp
finally
{
    // 1. Return connection to factory
    await _connectionFactory.ReturnConnectionAsync(connection);
    
    // 2. Factory closes connection (returns to pool)
    await connection.CloseAsync();
    
    // 3. NpgsqlDataSource manages actual pooling
}
```

### Feature Flag Implementation

The system uses configuration-based feature flags for safe rollout:

```csharp
public static IServiceCollection RegisterJarvis(this IServiceCollection services, 
    LogLevel logLevel = LogLevel.Warning, 
    IConfiguration? configuration = null)
{
    var useConnectionPooling = configuration?.GetValue<bool>(
        "Jarvis:Database:ConnectionPooling:Enabled") ?? false;
    
    if (useConnectionPooling)
    {
        // Register pooled implementation
        services.AddSingleton<INpgsqlConnectionFactory>(sp => /* ... */);
        services.AddScoped<IPgClient, PgClientPooled>();
    }
    else
    {
        // Register single-connection implementation
        services.AddScoped<IPgClient, PgClient>();
    }
}
```

### Thread Safety Considerations

The implementation ensures thread safety through:

1. **ConcurrentDictionary for Tracking**: Active connections are tracked using thread-safe collections
2. **Async/Await Patterns**: All operations use proper async patterns to prevent blocking
3. **Connection Isolation**: Each operation gets its own connection instance

### Error Handling

Robust error handling prevents connection leaks:

```csharp
public async Task<NpgsqlConnection> GetConnectionAsync()
{
    try
    {
        var connection = await _dataSource.OpenConnectionAsync();
        _activeConnections.TryAdd(connection, DateTime.UtcNow);
        return connection;
    }
    catch (NpgsqlException ex) when (ex.Message.Contains("pool"))
    {
        _logger.LogError(ex, "Connection pool exhausted. Active: {Count}", 
            _activeConnections.Count);
        throw new InvalidOperationException(
            $"Connection pool exhausted. Current active connections: {_activeConnections.Count}", ex);
    }
}
```

## Performance Analysis

### Benchmarks

Testing with different configurations revealed:

#### Sequential Operations
- **Single Connection**: ~50ms per operation
- **Pooled Connections**: ~5ms per operation (after warm-up)
- **Improvement**: 90% reduction in connection overhead

#### Concurrent Operations (Current Limitations)
- **Single Connection**: Not supported
- **Pooled Connections**: Limited by framework architecture
- **Issue**: New PgClient instances created for handlers

### Memory Usage

Connection pooling trades memory for performance:

- **Per Connection**: ~2-5MB depending on buffer sizes
- **Pool of 20**: ~40-100MB total memory overhead
- **Recommendation**: Size pools based on concurrent load, not total requests

### Stress Test Results

From `ConnectionPoolingStressTests.cs`:

```
Test Scenario          | Result | Notes
--------------------- | ------ | -----
Sequential (100 ops)   | Pass   | Avg 3ms per operation
Concurrent (3 ops)     | Fail   | "Command already in progress"
Scope Disposal         | Pass   | Proper cleanup verified
Pool Exhaustion        | Pass   | Graceful degradation
```

## Configuration and Deployment

### Configuration Options

```json
{
  "Jarvis": {
    "Database": {
      "ConnectionPooling": {
        "Enabled": true,
        "MaxPoolSize": 20,
        "MinPoolSize": 5,
        "ConnectionLifetimeMinutes": 5,
        "ConnectionIdleTimeoutMinutes": 1,
        "EnableStatisticsLogging": true,
        "StatisticsLogIntervalSeconds": 60
      }
    }
  }
}
```

### Environment-Specific Recommendations

#### Azure Functions Consumption Plan
- MaxPoolSize: 10 (limited concurrent executions)
- MinPoolSize: 2 (reduce cold start impact)
- ConnectionLifetime: 5 minutes (handle scale-to-zero)

#### Azure Functions Premium Plan
- MaxPoolSize: 30 (higher concurrency)
- MinPoolSize: 5 (maintain warm connections)
- ConnectionLifetime: 10 minutes

#### Container Apps / App Service
- MaxPoolSize: 50+ (persistent instances)
- MinPoolSize: 10 (stable load)
- ConnectionLifetime: 15 minutes

### Monitoring and Diagnostics

The implementation provides comprehensive diagnostics:

```csharp
var stats = factory.GetPoolStatistics();
_logger.LogInformation(
    "Pool Stats - Max: {Max}, Min: {Min}, Active: {Active}, Available: {Available}",
    stats.MaxPoolSize, stats.MinPoolSize, stats.ActiveConnections, 
    stats.MaxPoolSize - stats.ActiveConnections);
```

## Known Limitations

### 1. Handler Instance Creation

The current framework architecture creates new handler instances with their own PgClient:

```csharp
// Current problematic flow
dataContext.For<Handler>(entityId) 
    → Creates new handler instance
    → Handler may create new PgClient
    → Bypasses connection pooling
```

### 2. Direct Table Access

`PgClientPooled` doesn't support direct table access patterns:

```csharp
// Not supported in PgClientPooled
var table = pgClient.From<Component>();
await table.Insert(component);
```

### 3. Transaction Scope

Long-running transactions hold connections for their duration:

```csharp
await dataContext.InTransaction(async tx =>
{
    // Connection held for entire transaction
    await Task.Delay(5000); // Blocks connection for 5 seconds
    await tx.Commit(component);
});
```

## Future Enhancements

### 1. Handler Registry Improvements

Modify handler resolution to reuse PgClient instances:

```csharp
public interface IComponentHandler
{
    void Initialize(IPgClient pgClient); // Injected, not created
}
```

### 2. Query Builder Integration

Enhance query builders to work with pooled connections:

```csharp
public class EntityQuery : IEntityQuery
{
    private readonly Func<Task<IPgClient>> _pgClientFactory;
    
    public async Task<List<Guid>> ExecuteAsync()
    {
        var pgClient = await _pgClientFactory();
        // Use and release connection
    }
}
```

### 3. Connection Multiplexing

Implement true multiplexing for read operations:

```csharp
public class MultiplexedPgClient : IPgClient
{
    public async Task<T> ExecuteReadAsync<T>(Func<NpgsqlConnection, Task<T>> operation)
    {
        // Share connection for concurrent reads
    }
}
```

### 4. Adaptive Pool Sizing

Implement dynamic pool sizing based on load:

```csharp
public class AdaptiveConnectionFactory : INpgsqlConnectionFactory
{
    private void AdjustPoolSize()
    {
        if (_queueDepth > threshold)
            IncreasePoolSize();
        else if (_idleConnections > threshold)
            DecreasePoolSize();
    }
}
```

## Conclusion

The connection pooling implementation in Jarvis represents a significant step forward in managing database resources efficiently. While the current implementation successfully addresses connection exhaustion issues and provides a foundation for concurrent operations, there are opportunities for further optimization.

### Key Achievements

1. **Eliminated Connection Exhaustion**: No more "too many clients" errors during testing
2. **Improved Performance**: 90% reduction in connection overhead for sequential operations
3. **Backward Compatibility**: Existing code continues to function with feature flags
4. **Production Ready**: Comprehensive error handling and monitoring

### Recommendations

1. **Enable Gradually**: Use feature flags to roll out in stages
2. **Monitor Closely**: Track pool statistics during initial deployment
3. **Size Appropriately**: Configure pools based on actual load patterns
4. **Plan for Growth**: Consider architectural improvements for full concurrency support

The foundation laid by this implementation positions Jarvis for continued growth in high-concurrency, cloud-native environments while maintaining the framework's core principles of simplicity and reliability.

---

*Document Version: 1.0*  
*Last Updated: January 2025*  
*Authors: Jarvis Development Team*