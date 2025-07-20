# PgClient API Reference

The `PgClient` class provides a secure, convention-based client for PostgreSQL access with JWT-based Row Level Security (RLS) support.

## Overview

`PgClient` is the low-level database access layer in Jarvis that:
- Integrates JWT authentication with PostgreSQL's Row Level Security
- Provides type-safe table access through `PgTable<T>`
- Handles PascalCase to snake_case convention mapping
- Manages database connections and session variables

## Class Definition

```csharp
namespace core.jarvis.data
{
    public class PgClient
    {
        public Guid CurrentUserId { get; private set; }
        
        public PgClient(NpgsqlConnection conn, RLSPolicyRegistry? rlsPolicies = null)
    }
}
```

## Constructor

### Parameters
- **conn** (`NpgsqlConnection`): An open PostgreSQL connection
- **rlsPolicies** (`RLSPolicyRegistry?`): Optional RLS policy registry. If null, default policies are registered.

### Example
```csharp
var connectionString = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=jarvis";
var dataSource = NpgsqlDataSource.Create(connectionString);
var connection = dataSource.CreateConnection();
var pgClient = new PgClient(connection);
```

## Methods

### JWT(string jwt)

Sets the JWT token for subsequent database operations. The token is validated and claims are extracted for RLS.

```csharp
public void JWT(string jwt)
```

**Parameters:**
- **jwt**: A valid JWT token string

**Usage:**
```csharp
// Set JWT for user authentication
pgClient.JWT(userJwtToken);

// Now all subsequent operations will respect the user's permissions
var userOrders = await pgClient.From<Order>().Get();
```

**Important Notes:**
- The JWT must contain standard claims (sub, exp, iat, etc.)
- Claims are automatically set as PostgreSQL session variables
- Long claim names are sanitized to fit PostgreSQL's 63-character limit

### From\<T\>()

Returns a strongly-typed table accessor for performing CRUD operations on a specific table.

```csharp
public PgTable<T> From<T>() where T : class, new()
```

**Type Parameters:**
- **T**: The model type representing the database table

**Returns:** `PgTable<T>` - A fluent interface for table operations

**Usage Examples:**

```csharp
// Insert a new record
var order = new Order { CustomerId = "123", Total = 99.99m };
await pgClient.From<Order>().Insert(order);

// Query with filters
var pendingOrders = await pgClient.From<Order>()
    .Filter("status", "eq", "pending")
    .Get();

// Update a record
order.Status = "completed";
await pgClient.From<Order>().Update(order);

// Delete records
await pgClient.From<Order>()
    .Filter("created_at", "lt", DateTime.UtcNow.AddDays(-90))
    .Delete();

// Get a single record
var singleOrder = await pgClient.From<Order>()
    .Filter("id", "eq", orderId)
    .Single();

// Upsert (insert or update)
await pgClient.From<Order>().Upsert(order);
```

### Rpc(string functionName, object args)

Calls a PostgreSQL function with named arguments.

```csharp
public async Task Rpc(string functionName, object args)
```

**Parameters:**
- **functionName**: The name of the PostgreSQL function to call
- **args**: An anonymous object containing the function arguments

**Usage:**
```csharp
// Call a PostgreSQL function
await pgClient.Rpc("calculate_order_total", new { 
    order_id = orderId, 
    include_tax = true,
    discount_percent = 10 
});

// Call a function that returns data
var result = await pgClient.Rpc("get_user_statistics", new { 
    user_id = userId,
    start_date = DateTime.UtcNow.AddMonths(-1),
    end_date = DateTime.UtcNow
});
```

### ExecuteAsync(string sql, object? parameters = null)

Executes arbitrary SQL commands with optional parameters.

```csharp
public async Task ExecuteAsync(string sql, object? parameters = null)
```

**Parameters:**
- **sql**: The SQL command to execute
- **parameters**: Optional parameters for the SQL command

**Usage:**
```csharp
// Execute a simple command
await pgClient.ExecuteAsync("VACUUM ANALYZE orders");

// Execute with parameters
await pgClient.ExecuteAsync(
    "UPDATE users SET last_login = @timestamp WHERE id = @userId", 
    new { 
        timestamp = DateTime.UtcNow, 
        userId = currentUserId 
    }
);

// Execute complex SQL
await pgClient.ExecuteAsync(@"
    WITH inactive_users AS (
        SELECT id FROM users 
        WHERE last_login < @cutoffDate
    )
    UPDATE user_settings 
    SET notifications_enabled = false 
    WHERE user_id IN (SELECT id FROM inactive_users)",
    new { cutoffDate = DateTime.UtcNow.AddMonths(-6) }
);
```

## Properties

### CurrentUserId

Gets the current user ID extracted from the JWT token.

```csharp
public Guid CurrentUserId { get; private set; }
```

**Usage:**
```csharp
pgClient.JWT(userToken);
var userId = pgClient.CurrentUserId; // Extracted from JWT 'sub' claim
```

## RLS (Row Level Security) Integration

PgClient automatically integrates with PostgreSQL's Row Level Security by:

1. **Setting Session Variables**: JWT claims are set as session variables before each operation
2. **User Context**: The `sub` claim from JWT becomes `app.user_id` in PostgreSQL
3. **Automatic Filtering**: RLS policies automatically filter data based on user permissions

### Example RLS Policy
```sql
-- Example RLS policy that uses JWT claims
CREATE POLICY user_orders ON orders
    FOR ALL
    TO authenticated
    USING (user_id = current_setting('app.user_id')::uuid);
```

### How It Works
```csharp
// When you set a JWT
pgClient.JWT(userToken); // Contains claim: sub = "123e4567-e89b-12d3-a456-426614174000"

// This query automatically respects RLS
var orders = await pgClient.From<Order>().Get();
// SQL executed includes: SET SESSION "app.user_id" = '123e4567-e89b-12d3-a456-426614174000'
```

## Connection Management

PgClient does not manage the connection lifecycle. The calling code is responsible for:

```csharp
// Using connection with proper disposal
using var dataSource = NpgsqlDataSource.Create(connectionString);
using var connection = dataSource.CreateConnection();
await connection.OpenAsync();

var pgClient = new PgClient(connection);
// Use pgClient...
// Connection is disposed when leaving using block
```

## Error Handling

PgClient operations can throw the following exceptions:

- **NpgsqlException**: Database connectivity or query errors
- **InvalidOperationException**: Invalid JWT or configuration issues
- **ArgumentException**: Invalid parameters or malformed queries

Example error handling:
```csharp
try
{
    var result = await pgClient.From<Order>()
        .Filter("id", "eq", orderId)
        .Single();
}
catch (InvalidOperationException ex) when (ex.Message.Contains("Sequence contains no elements"))
{
    // Record not found
    return null;
}
catch (NpgsqlException ex)
{
    // Database error
    logger.LogError(ex, "Database operation failed");
    throw;
}
```

## Best Practices

1. **Always Set JWT**: For RLS to work properly, always set the JWT before operations
   ```csharp
   pgClient.JWT(userToken);
   ```

2. **Use Type-Safe Operations**: Prefer `From<T>()` over `ExecuteAsync()` when possible
   ```csharp
   // Good
   await pgClient.From<Order>().Insert(order);
   
   // Avoid when possible
   await pgClient.ExecuteAsync("INSERT INTO orders ...");
   ```

3. **Handle Concurrency**: Use proper error handling for concurrent operations
   ```csharp
   try
   {
       await pgClient.From<Order>().Update(order);
   }
   catch (NpgsqlException ex) when (ex.SqlState == "40001")
   {
       // Handle serialization failure
   }
   ```

4. **Connection Pooling**: Use NpgsqlDataSource for connection pooling
   ```csharp
   // Create once and reuse
   private static readonly NpgsqlDataSource DataSource = 
       NpgsqlDataSource.Create(connectionString);
   ```

5. **Dispose Properly**: Always dispose connections when done
   ```csharp
   using var connection = DataSource.CreateConnection();
   var pgClient = new PgClient(connection);
   ```

## Configuration

PgClient reads JWT configuration from environment variables:

- **Jwt__SecretKey**: Secret key for JWT validation
- **Jwt__Issuer**: Expected JWT issuer
- **Jwt__Audience**: Expected JWT audience

Example configuration:
```bash
export Jwt__SecretKey="your-secret-key-at-least-32-characters-long"
export Jwt__Issuer="jarvis-auth"
export Jwt__Audience="jarvis-api"
```

## See Also

- [PgTable API Reference](pgtable-api.md) - Detailed table operations
- [DataContext API Reference](datacontext-api.md) - Higher-level data access
- [RLS Configuration Guide](../guides/rls-configuration.md) - Setting up Row Level Security