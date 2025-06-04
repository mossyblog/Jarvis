# Logging with Serilog

## Overview

The Jarvis framework uses [Serilog](https://serilog.net/) as its structured logging provider. Serilog is a diagnostic logging library for .NET applications that provides a simple, flexible API for capturing and routing log data.

## What is Serilog?

Serilog is a structured logging library that treats log events as structured data rather than text strings. This means that instead of writing:

```
"User john.doe logged in at 2024-01-15 10:30:00"
```

Serilog captures:
```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "level": "Information",
  "messageTemplate": "User {Username} logged in at {LoginTime}",
  "properties": {
    "Username": "john.doe",
    "LoginTime": "2024-01-15T10:30:00Z"
  }
}
```

## Why Serilog in Jarvis?

### 1. **Structured Data for Better Analysis**
- Log properties can be queried and filtered
- Integration with log analysis tools (Seq, Elasticsearch, etc.)
- Machine-readable format for automated monitoring

### 2. **Contextual Enrichment**
- Automatically adds context like machine name, environment, thread ID
- Custom enrichers for business-specific context
- Correlation IDs for tracing operations across services

### 3. **Flexible Output Sinks**
- Console for development
- File for local debugging
- Cloud services for production monitoring
- Multiple sinks simultaneously

### 4. **Performance**
- Asynchronous logging to avoid blocking operations
- Efficient serialization
- Conditional logging based on levels

## How Serilog Benefits Developers

### 1. **Debugging Production Issues**
```csharp
// Rich context helps identify issues
_logger.LogError(ex, "Failed to process order {OrderId} for customer {CustomerId}", 
    orderId, customerId);
// In production, you can search for all errors related to a specific customer
```

### 2. **Performance Monitoring**
```csharp
using (_logger.BeginTimedOperation("Database query for {QueryType}", queryType))
{
    // Operation being timed
    var results = await ExecuteQuery();
}
// Automatically logs duration, helping identify slow operations
```

### 3. **Audit Trails**
```csharp
_logger.LogInformation("User {UserId} modified entity {EntityId} setting {Property} from {OldValue} to {NewValue}",
    userId, entityId, propertyName, oldValue, newValue);
// Creates searchable audit logs with all relevant context
```

## Configuration in Jarvis

### Basic Setup

The framework provides an extension method for easy Serilog configuration:

```csharp
services.AddJarvisSerilog(config =>
{
    config
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj} " +
                          "{Properties:j}{NewLine}{Exception}");
});
```

### Test Configuration

For tests, use minimal output to reduce noise:

```csharp
services.AddJarvisSerilog(config =>
{
    config.MinimumLevel.Is(LogEventLevel.Warning)
        .MinimumLevel.Override("core.jarvis", LogEventLevel.Debug)
        .WriteTo.Console(
            restrictedToMinimumLevel: LogEventLevel.Information,
            outputTemplate: "[{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}");
});
```

## Usage Examples in Jarvis

### 1. **Handler Logging**

```csharp
public class InvoiceHandler : ComponentHandler<Invoice>
{
    private readonly ILogger<InvoiceHandler> _logger;

    public InvoiceHandler(IDataContext dataContext, ILogger<InvoiceHandler> logger) 
        : base(dataContext)
    {
        _logger = logger;
    }

    public async Task<Invoice> CreateInvoice(Guid entityId, decimal amount, string description)
    {
        _logger.LogInformation("Creating invoice for entity {EntityId} with amount {Amount:C}", 
            entityId, amount);

        try
        {
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                OwnerEntityId = entityId,
                Amount = amount,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            await Commit(invoice);
            
            _logger.LogInformation("Successfully created invoice {InvoiceId} for entity {EntityId}", 
                invoice.Id, entityId);
            
            return invoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create invoice for entity {EntityId}", entityId);
            throw;
        }
    }
}
```

### 2. **DataContext Operations**

```csharp
public async Task<TComponent?> TryCommit<TComponent>(TComponent component) 
    where TComponent : class, IComponent
{
    using (_logger.BeginScope("Component commit for {ComponentType} {ComponentId}", 
        typeof(TComponent).Name, component.Id))
    {
        try
        {
            // Version checking with detailed logging
            if (component is IVersionedComponent versioned)
            {
                var existing = await _pgClient.From<TComponent>()
                    .Filter("id", "eq", component.Id)
                    .SingleOrDefault();

                if (existing != null && existing is IVersionedComponent existingVersioned)
                {
                    if (existingVersioned.Version != versioned.Version)
                    {
                        _logger.LogWarning(
                            "Version mismatch for {ComponentType} ID {ComponentId}. " +
                            "Expected version: {ExpectedVersion}, Actual version: {ActualVersion}",
                            typeof(TComponent).Name,
                            component.Id,
                            versioned.Version,
                            existingVersioned.Version);
                        
                        return null;
                    }
                }
            }

            await _pgClient.From<TComponent>().Upsert(component);
            return component;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit component {ComponentType} {ComponentId}",
                typeof(TComponent).Name, component.Id);
            throw;
        }
    }
}
```

### 3. **Audit Service Integration**

```csharp
public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;

    public async Task LogEvent(string eventType, Guid entityId, object? metadata = null)
    {
        // Log the audit attempt
        _logger.LogDebug("Creating audit event {EventType} for entity {EntityId}", 
            eventType, entityId);

        try
        {
            var auditEvent = new AuditEvent
            {
                OwnerEntityId = entityId,
                EventType = eventType,
                Metadata = SerializeMetadata(metadata)
            };

            await _pgClient.From<AuditEvent>().Upsert(auditEvent);
            
            _logger.LogDebug("Successfully logged audit event {EventType} for entity {EntityId}", 
                eventType, entityId);
        }
        catch (Exception ex)
        {
            // Audit failures shouldn't break business operations
            _logger.LogWarning(ex, 
                "Failed to log audit event {EventType} for entity {EntityId}. " +
                "This error was swallowed to prevent disruption of business operations.",
                eventType, entityId);
        }
    }
}
```

### 4. **Structured Query Logging**

```csharp
public class EntityQuery : IEntityQuery
{
    private readonly ILogger<EntityQuery> _logger;

    public async Task<List<Guid>> ExecuteAsync()
    {
        using (_logger.BeginScope("EntityQuery with {ConditionCount} conditions", _conditions.Count))
        {
            _logger.LogDebug("Executing query with conditions: {@Conditions}", 
                _conditions.Select(c => new { c.Type, ComponentTypes = c.ComponentTypes }));

            var stopwatch = Stopwatch.StartNew();
            var results = await ExecuteQueryLogic();
            stopwatch.Stop();

            _logger.LogInformation(
                "Query completed in {Duration}ms returning {ResultCount} entities",
                stopwatch.ElapsedMilliseconds,
                results.Count);

            return results;
        }
    }
}
```

## Best Practices

### 1. **Use Structured Logging**
```csharp
// ✅ Good - structured properties
_logger.LogInformation("Processing order {OrderId} for customer {CustomerId}", 
    orderId, customerId);

// ❌ Bad - string concatenation
_logger.LogInformation($"Processing order {orderId} for customer {customerId}");
```

### 2. **Include Relevant Context**
```csharp
using (_logger.BeginScope("Processing batch {BatchId}", batchId))
{
    foreach (var item in batch)
    {
        // All logs within this scope will include BatchId
        _logger.LogDebug("Processing item {ItemId}", item.Id);
    }
}
```

### 3. **Use Appropriate Log Levels**
- **Verbose**: Detailed tracing information
- **Debug**: Internal system events useful for debugging
- **Information**: General informational messages
- **Warning**: Warnings about potentially harmful situations
- **Error**: Error events that might still allow the app to continue
- **Fatal**: Critical errors causing application termination

### 4. **Don't Log Sensitive Data**
```csharp
// ✅ Good - log user ID, not personal data
_logger.LogInformation("User {UserId} logged in", user.Id);

// ❌ Bad - logging sensitive information
_logger.LogInformation("User {Email} logged in with password {Password}", 
    user.Email, user.Password);
```

### 5. **Use Timing for Performance Monitoring**
```csharp
using (var operation = _logger.BeginTimedOperation("Complex calculation"))
{
    // Perform operation
    var result = await PerformCalculation();
    
    operation.Complete("Calculation", result.ItemsProcessed);
}
// Automatically logs: "Complex calculation completed in 1234ms (Calculation: 42)"
```

## Sink Configuration Examples

### File Sink for Local Development
```csharp
.WriteTo.File(
    path: "logs/jarvis-.txt",
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 7,
    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
```

### Seq for Centralized Logging
```csharp
.WriteTo.Seq(
    serverUrl: "http://localhost:5341",
    apiKey: Configuration["Seq:ApiKey"])
```

### Conditional Sinks
```csharp
.WriteTo.Conditional(
    condition: _ => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production",
    configureSink: wt => wt.ApplicationInsights(
        Configuration["ApplicationInsights:InstrumentationKey"],
        TelemetryConverter.Traces))
```

## Troubleshooting

### 1. **Too Much Logging**
Adjust minimum levels for noisy sources:
```csharp
.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
```

### 2. **Missing Context**
Ensure you're using scopes for operations:
```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["UserId"] = currentUser.Id,
    ["TenantId"] = currentTenant.Id
}))
{
    // All operations here include user and tenant context
}
```

### 3. **Performance Impact**
Use async sinks and buffering:
```csharp
.WriteTo.Async(a => a.File("logs/app.txt"), bufferSize: 10000)
```

## Integration with Jarvis Features

### Audit Integration
The framework automatically logs audit events through Serilog, providing a unified view of system activity:

```csharp
// In DataContext
await _auditService.LogEvent(
    AuditEventTypes.ForComponent(typeof(TComponent).Name, "CREATED"),
    component.OwnerEntityId,
    new { ComponentId = component.Id, ComponentType = typeof(TComponent).Name });

// This creates both an audit record AND a Serilog entry
```

### Error Handling Policy
The framework's error handling integrates with Serilog:

```csharp
ErrorHandlingPolicy.LogAndRethrow(
    ex,
    "Failed to save component {ComponentType} for entity {EntityId}",
    new { ComponentType = typeof(TComponent).Name, EntityId = component.OwnerEntityId });
```

This ensures consistent error logging across the framework with proper context preservation.