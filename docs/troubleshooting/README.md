# Troubleshooting Guide

Quick solutions to common problems with the Jarvis ECS SDK.

## 🔥 Common Issues

### Handler Issues

#### "Handler not found" error
```csharp
// ❌ Wrong - handler not registered
var handler = dataContext.For<MyHandler>(entityId);

// ✅ Fix - register in DI
services.AddScoped<IComponentHandler, MyHandler>();
services.AddScoped<MyHandler>(); // Also register concrete type
```

#### "Entity not found" exception
```csharp
// ❌ Wrong - assuming entity exists
var order = await handler.Get();

// ✅ Fix - check for null
var order = await handler.GetOrDefault();
if (order == null) 
{
    // Handle missing entity
}
```

### Database Connection Issues

#### "Connection refused" error
1. Check PostgreSQL is running: `docker ps`
2. Verify connection string format
3. Check firewall/network settings
4. See [Database Connection Guide](supabase-connection.md)

#### "Permission denied" errors
- Ensure JWT token is set: `client.JWT(token)`
- Check RLS policies are configured
- Verify user has correct permissions

### Performance Problems

#### Slow queries (N+1 problem)
```csharp
// ❌ Wrong - multiple queries
foreach (var entityId in entityIds)
{
    var handler = dataContext.For<OrderHandler>(entityId);
    var order = await handler.Get();
}

// ✅ Fix - batch query
var orders = await dataContext.Query()
    .WithAll<OrderComponent>()
    .Where(o => entityIds.Contains(o.OwnerEntityId))
    .ToListAsync();
```

See [Performance Optimization](performance-issues.md) for more tips.

### Concurrency Issues

#### "Concurrency conflict" error
```csharp
// ❌ Wrong - ignoring concurrency
await dataContext.Commit(component);

// ✅ Fix - use TryCommit and handle conflicts
var success = await dataContext.TryCommit(component);
if (!success)
{
    // Reload and retry
    var current = await handler.Get();
    // Apply changes to current version
    current.Status = newStatus;
    success = await dataContext.TryCommit(current);
}
```

### Testing Issues

#### "Database not found" in tests
```csharp
// ❌ Wrong - hardcoded connection
var conn = "Host=localhost;Database=prod";

// ✅ Fix - use test configuration
var conn = Environment.GetEnvironmentVariable("TEST_DATABASE_URL") 
    ?? "Host=localhost;Database=jarvis_test";
```

#### "Concurrent test failures"
- Use `[Collection("Sequential")]` for tests that can't run in parallel
- Clean up test data in `Dispose()` method
- Use unique entity IDs per test

## 🛠️ Debugging Tips

### Enable Detailed Logging
```csharp
services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddConsole();
    builder.AddDebug();
});
```

### Check Component State
```csharp
// Debug helper
public async Task DebugEntity(Guid entityId)
{
    var components = await dataContext.Query()
        .WithEntity(entityId)
        .ToComponentsAsync();
        
    foreach (var component in components)
    {
        Console.WriteLine($"{component.GetType().Name}: {JsonSerializer.Serialize(component)}");
    }
}
```

### Monitor SQL Queries
Enable query logging in PostgreSQL:
```sql
ALTER SYSTEM SET log_statement = 'all';
SELECT pg_reload_conf();
```

## 📋 Checklist for Issues

When reporting an issue, include:

- [ ] Jarvis version numbers (all three SDKs)
- [ ] .NET version (`dotnet --version`)
- [ ] PostgreSQL version
- [ ] Minimal code to reproduce
- [ ] Full error message and stack trace
- [ ] Connection string format (without credentials)

## 🔗 Quick Links

- [Performance Issues](performance-issues.md) - Optimize slow queries
- [Database Connection](supabase-connection.md) - Connection troubleshooting
- [GitHub Issues](https://github.com/yourusername/jarvis/issues) - Report bugs
- [Stack Overflow](https://stackoverflow.com/questions/tagged/jarvis-ecs) - Community help

## 💬 Still Stuck?

1. Search existing [GitHub Issues](https://github.com/yourusername/jarvis/issues)
2. Check [Discussions](https://github.com/yourusername/jarvis/discussions)
3. Create a new issue with the checklist above
4. Join our Discord community (link in main README) 