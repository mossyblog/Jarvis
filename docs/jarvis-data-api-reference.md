# Jarvis.Data API Reference & Implementation Guide

## Table of Contents

1. [Class Reference](#class-reference)
2. [Security Implementation](#security-implementation)
3. [String Mapping Algorithm](#string-mapping-algorithm)
4. [RLS Policy Engine](#rls-policy-engine)
5. [Testing Patterns](#testing-patterns)
6. [Implementation Details](#implementation-details)

## Class Reference

### PgClient

The main client class that provides secure, convention-based PostgreSQL access.

#### Constructor

```csharp
public PgClient(NpgsqlConnection conn, RLSPolicyRegistry? rlsPolicies = null)
```

**Parameters:**
- `conn`: Active Npgsql database connection
- `rlsPolicies`: Optional RLS policy registry (defaults to built-in policies)

**Initialization Behavior:**
- Verifies connection state
- Validates required `users` table exists
- Ensures `password_hash` column is present
- Confirms at least one RLS policy exists on users table
- Registers default RLS policies if no custom registry provided

#### Methods

##### Authenticate

```csharp
public async Task<string?> Authenticate(string email, string password)
```

Validates user credentials and returns a JWT token.

**Implementation:**
- Queries users table by email
- Uses BCrypt to verify password hash
- Returns placeholder JWT (replace with actual JWT generation)
- Returns `null` if authentication fails

**Security Notes:**
- Uses parameterized query to prevent SQL injection
- Handles null/empty password hashes safely
- BCrypt provides timing attack protection

##### JWT

```csharp
public void JWT(string jwt)
```

Sets the JWT token for all subsequent operations.

**Implementation:**
- Parses JWT claims without signature validation
- Stores claims in internal dictionary
- Claims become available to RLS policies

⚠️ **Security Warning**: This method does NOT validate JWT signatures. Always validate JWTs before calling this method.

##### From<T>

```csharp
public PgTable<T> From<T>() where T : class, new()
```

Returns a strongly-typed table accessor.

**Implementation:**
- Creates new `PgTable<T>` instance
- Passes connection, client reference, RLS policies, and JWT claims
- Table name derived from type name using snake_case conversion

##### Rpc

```csharp
public async Task Rpc(string functionName, object args)
```

Calls PostgreSQL functions with named parameters.

**Implementation:**
- Reflects object properties to create parameter list
- Sets JWT claims as session variables before execution
- Uses Dapper's parameter binding for safety

**Example:**
```csharp
await client.Rpc("calculate_total", new { 
    order_id = orderId, 
    tax_rate = 0.08m 
});
```

### PgTable<T>

Strongly-typed table interface with security enforcement.

#### Security Features

- **Column Whitelisting**: Only allows columns that exist as entity properties
- **Operator Whitelisting**: Restricts to safe operators (`eq`, `neq`, `lt`, `lte`, `gt`, `gte`)
- **Parameterized Queries**: All values properly parameterized
- **RLS Integration**: Automatic policy enforcement

#### Methods

##### Filter

```csharp
public PgTable<T> Filter(string column, string op, object value)
```

Adds type-safe filter conditions.

**Validation:**
- Column must exist in `AllowedColumns` set (derived from entity properties)
- Operator must exist in `AllowedOperators` set
- Throws `ArgumentException` for invalid inputs

**SQL Generation:**
```csharp
// Input: .Filter("price", "gte", 100)
// Output: WHERE price >= @param_0
```

##### In

```csharp
public PgTable<T> In(string column, IEnumerable<object> values)
```

Adds IN clause with multiple values.

**Implementation:**
- Validates column against whitelist
- Creates individual parameters for each value
- Generates: `WHERE column IN (@param_0, @param_1, ...)`

##### Insert

```csharp
public async Task Insert(T entity)
```

Inserts entity with RLS policy enforcement.

**Security Flow:**
1. Extract entity data into dictionary
2. Check RLS policies for insert permission
3. Silently fail if policies deny access (PostgreSQL RLS behavior)
4. Build INSERT statement with snake_case column mapping
5. Handle special JSONB columns (`Metadata`, `Snapshots`, `ChildTypes`)
6. Set JWT claims as session variables
7. Execute parameterized query

**ID Handling:**
- Excludes `Id` property if it's default value (0 for int, empty GUID)
- Allows explicit ID values for data migration scenarios

##### Update

```csharp
public async Task Update(T entity)
```

Updates entity by ID with RLS enforcement.

**Requirements:**
- Entity must have `Id` property
- ID value cannot be null
- RLS policies must allow update operation

**Implementation:**
- Excludes `Id` from SET clause
- Uses `WHERE id = @Id` for record identification
- Applies same JSONB handling as Insert

##### Upsert

```csharp
public async Task Upsert(T entity)
```

Performs insert-or-update logic.

**Decision Logic:**
- If ID is null/empty GUID → Insert
- For components with `OwnerEntityId` → Check existence by `owner_entity_id`
- For other entities → Check existence by `id`
- If exists → Update, else → Insert

**Component Handling:**
Tables ending with `_component` (except `blog_post_component`) use `owner_entity_id` for uniqueness checks, supporting single-instance components.

##### Get

```csharp
public async Task<List<T>> Get()
```

Executes SELECT with RLS policy enforcement.

**Implementation:**
1. Build column list with snake_case to PascalCase mapping
2. Apply user filters from `Filter()` calls
3. Apply RLS policy WHERE clauses
4. Combine all conditions with AND
5. Set JWT session variables
6. Execute query with proper parameter binding

**Column Mapping:**
```sql
-- For property: CustomerName
-- Generates: customer_name AS CustomerName
```

##### Single / SingleOrDefault

```csharp
public async Task<T> Single()
public async Task<T?> SingleOrDefault()
```

Returns single records with validation.

**Single():**
- Throws if zero results found
- Throws if multiple results found

**SingleOrDefault():**
- Returns null if zero results
- Throws if multiple results found

##### Delete

```csharp
public async Task<int> Delete()
```

Deletes records matching current filters.

**Security:**
- Applies all user filters
- Applies RLS policy restrictions
- Checks RLS policies for delete permission
- Returns count of deleted records

### RLSPolicyRegistry

Manages Row Level Security policies for SDK-level enforcement.

#### Methods

##### RegisterPolicy

```csharp
public void RegisterPolicy(RLSPolicy policy)
```

Registers a security policy for a table.

**Policy Types:**
- `Select`: Controls read access
- `Insert`: Controls record creation
- `Update`: Controls record modification  
- `Delete`: Controls record deletion
- `All`: Applies to all operations

##### BuildWhereClause

```csharp
public string BuildWhereClause(string tableName, Dictionary<string, string> claims)
```

Generates WHERE clause additions for SELECT operations.

**Implementation:**
- Gets all SELECT and ALL policies for table
- Executes each policy's `WhereClause` function
- Combines results with AND logic
- Returns empty string if no policies found

##### CheckOperation

```csharp
public bool CheckOperation(string tableName, PolicyType type, Dictionary<string, string> claims, Dictionary<string, object> data)
```

Validates if an operation is permitted.

**Logic:**
- If no policies exist for table → Allow (no RLS configured)
- If table has policies but none for operation type → Deny
- All applicable policies must pass → Allow

### RLSPolicy

Defines a single security policy.

#### Properties

```csharp
public string TableName { get; set; }
public PolicyType Type { get; set; }
public Func<Dictionary<string, string>, string>? WhereClause { get; set; }
public Func<Dictionary<string, string>, Dictionary<string, object>, bool>? CheckFunction { get; set; }
```

**WhereClause Function:**
- Input: JWT claims dictionary
- Output: SQL WHERE condition string
- Used for SELECT operations

**CheckFunction:**
- Input: JWT claims and entity data
- Output: Boolean permission result
- Used for INSERT/UPDATE/DELETE operations

## Security Implementation

### SQL Injection Prevention

The library implements a **Defense in Depth** strategy:

#### Layer 1: Input Validation

```csharp
// Column whitelist - only entity properties allowed
private static readonly HashSet<string> AllowedColumns = typeof(T).GetProperties()
    .Select(p => p.Name.ToSnakeCase())
    .ToHashSet();

// Operator whitelist - only safe operators allowed
private static readonly HashSet<string> AllowedOperators = 
    new() { "eq", "neq", "lt", "lte", "gt", "gte" };
```

#### Layer 2: Parameterization

```csharp
// All user values are parameterized
string paramName = $"@param_{_parameters.ParameterNames.Count()}";
_whereClauses.Add($"{column} {TranslateOperator(op)} {paramName}");
_parameters.Add(paramName, value);
```

#### Layer 3: Type Safety

```csharp
// Table names from type system, not user input
_tableName = typeof(T).Name.ToSnakeCase();

// Column names from reflection, not user input
var columns = string.Join(", ", props.Select(p => p.Name.ToSnakeCase()));
```

#### Layer 4: Claim Escaping

```csharp
// JWT claim values are escaped when building SQL
var escapedValue = claim.Value.Replace("'", "''");
var sql = $"SET SESSION \"{variableName}\" = '{escapedValue}';";
```

### RLS Policy Security

#### Default Deny Principle

```csharp
// If table has policies but none for operation type → Deny
if (!policies.Any())
    return false;
```

#### Claim Validation

```csharp
// Policies validate required claims exist
if (!claims.TryGetValue("tenant_id", out var tenantId))
    return "1=0"; // No access without tenant claim
```

#### Silent Failure

```csharp
// Operations silently fail like PostgreSQL RLS
if (!_rlsPolicies.CheckOperation(_tableName, PolicyType.Insert, _jwtClaims, entityData))
{
    return; // Silent failure - no exception thrown
}
```

## String Mapping Algorithm

### ToSnakeCase Implementation

The library includes a sophisticated algorithm for converting PascalCase to snake_case:

```csharp
public static string ToSnakeCase(this string input)
{
    if (string.IsNullOrEmpty(input))
        return input;

    var result = new StringBuilder();
    
    for (int i = 0; i < input.Length; i++)
    {
        char current = input[i];
        
        if (char.IsUpper(current))
        {
            // Add underscore before uppercase (except first character)
            if (i > 0)
            {
                char previous = input[i - 1];
                
                // Don't add underscore if previous was also uppercase
                // Unless this is the start of a word (next char is lowercase)
                if (!char.IsUpper(previous) || 
                    (i + 1 < input.Length && char.IsLower(input[i + 1])))
                {
                    result.Append('_');
                }
            }
        }
        
        result.Append(char.ToLower(current));
    }
    
    return result.ToString();
}
```

### Mapping Examples

| C# Property | PostgreSQL Column | Pattern |
|-------------|-------------------|---------|
| `Id` | `id` | Simple |
| `CustomerName` | `customer_name` | PascalCase |
| `IsActive` | `is_active` | Boolean prefix |
| `XMLData` | `xml_data` | Acronym |
| `CustomerID` | `customer_id` | ID suffix |
| `IOOperation` | `io_operation` | Consecutive caps |
| `APIKey` | `api_key` | Acronym + word |

## RLS Policy Engine

### Default Policies

#### Multi-Tenant Isolation

```csharp
registry.RegisterPolicy(new RLSPolicy
{
    TableName = "tenant_data",
    Type = PolicyType.All,
    WhereClause = claims =>
    {
        if (claims.TryGetValue("tenant_id", out var tenantId))
            return $"tenant_id = '{tenantId}'::uuid";
        return "1=0"; // No access without tenant_id
    },
    CheckFunction = (claims, data) =>
    {
        if (!claims.TryGetValue("tenant_id", out var claimTenantId))
            return false;
        
        if (data.TryGetValue("tenant_id", out var dataTenantId))
        {
            return claimTenantId == dataTenantId?.ToString();
        }
        return false;
    }
});
```

#### User-Level Security

```csharp
registry.RegisterPolicy(new RLSPolicy
{
    TableName = "user_data",
    Type = PolicyType.Select,
    WhereClause = claims =>
    {
        if (!claims.TryGetValue("tenant_id", out var tenantId))
            return "1=0";
        if (!claims.TryGetValue("sub", out var userId))
            return "1=0";
        
        return $"tenant_id = '{tenantId}'::uuid AND (user_id = '{userId}'::uuid OR is_public = TRUE)";
    }
});
```

#### Role-Based Access

```csharp
registry.RegisterPolicy(new RLSPolicy
{
    TableName = "sensitive_data",
    Type = PolicyType.Select,
    WhereClause = claims =>
    {
        if (!claims.TryGetValue("tenant_id", out var tenantId))
            return "1=0";
        if (!claims.TryGetValue("role", out var role))
            return "1=0";

        var conditions = new List<string> { $"tenant_id = '{tenantId}'::uuid" };
        
        switch (role.ToLower())
        {
            case "admin":
                // Admin sees all classifications
                break;
            case "manager":
                conditions.Add("classification IN ('public', 'internal', 'confidential')");
                break;
            case "user":
                conditions.Add("classification IN ('public', 'internal')");
                break;
            default:
                conditions.Add("classification = 'public'");
                break;
        }

        return string.Join(" AND ", conditions);
    }
});
```

### Custom Policy Patterns

#### Time-Based Access

```csharp
registry.RegisterPolicy(new RLSPolicy
{
    TableName = "time_sensitive_data",
    Type = PolicyType.Select,
    WhereClause = claims =>
    {
        if (claims.TryGetValue("access_expires", out var expires))
        {
            return $"created_at <= '{expires}'::timestamp";
        }
        return "created_at <= NOW()"; // Default to current time
    }
});
```

#### Geographic Restrictions

```csharp
registry.RegisterPolicy(new RLSPolicy
{
    TableName = "regional_data",
    Type = PolicyType.All,
    WhereClause = claims =>
    {
        if (claims.TryGetValue("allowed_regions", out var regions))
        {
            var regionList = regions.Split(',')
                .Select(r => $"'{r.Trim()}'");
            return $"region IN ({string.Join(", ", regionList)})";
        }
        return "1=0"; // No access without region claims
    }
});
```

## Testing Patterns

### Test Documentation Template

```csharp
/// <summary>
/// INTENT: [What is being tested]
/// PURPOSE: [Why this test exists]
/// BUSINESS CONTEXT: [Business scenario this supports]
/// WHY IMPORTANT: [Why this is critical for correctness]
/// ARCHITECTURAL SIGNIFICANCE: [What contract this enforces]
/// FUTURE RESILIENCE: [How this protects against regressions]
/// </summary>
```

### Security Test Examples

#### Tenant Isolation Test

```csharp
[Fact]
public async Task MultiTenantIsolation_PreventsDataLeakage()
{
    // Arrange
    var tenant1 = Guid.NewGuid();
    var tenant2 = Guid.NewGuid();
    
    await SeedData(tenant1, "Tenant 1 Secret");
    await SeedData(tenant2, "Tenant 2 Secret");
    
    var jwt1 = CreateJWT(tenantId: tenant1);
    _client.JWT(jwt1);
    
    // Act
    var results = await _client.From<TenantData>().Get();
    
    // Assert
    results.ShouldHaveSingleItem();
    results[0].TenantId.ShouldBe(tenant1);
    results.ShouldNotContain(r => r.TenantId == tenant2);
}
```

#### Injection Protection Test

```csharp
[Fact]
public void Filter_WithSQLInjectionAttempt_ThrowsException()
{
    // Arrange & Act & Assert
    Should.Throw<ArgumentException>(() =>
        _client.From<TestEntity>().Filter("'; DROP TABLE users; --", "eq", "value")
    );
}
```

#### Role Escalation Test

```csharp
[Fact]
public async Task RoleBasedAccess_PreventsPrivilegeEscalation()
{
    // Arrange
    await SeedSensitiveData("secret", "top_secret_data");
    
    var userJwt = CreateJWT(role: "user");
    _client.JWT(userJwt);
    
    // Act
    var results = await _client.From<SensitiveData>().Get();
    
    // Assert
    results.ShouldBeEmpty(); // Users can't see secret data
}
```

## Implementation Details

### Connection Management

The library expects the caller to manage the database connection:

```csharp
// Caller responsibility
var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

// Library uses provided connection
var client = await PgClientFactory.Create(conn);
```

### Transaction Handling

For transactional operations, wrap in explicit transactions:

```csharp
using var transaction = await conn.BeginTransactionAsync();
try
{
    await client.From<Order>().Insert(order);
    await client.From<OrderItem>().Insert(items);
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### Performance Considerations

#### Query Optimization

```sql
-- Recommended indexes for RLS performance
CREATE INDEX idx_tenant_data_tenant_id ON tenant_data(tenant_id);
CREATE INDEX idx_user_data_tenant_user ON user_data(tenant_id, user_id);
CREATE INDEX idx_sensitive_data_tenant_class ON sensitive_data(tenant_id, classification);
```

#### Bulk Operations

```csharp
// Efficient bulk insert pattern
var entities = GenerateLargeDataSet();
using var transaction = await conn.BeginTransactionAsync();

foreach (var batch in entities.Chunk(1000))
{
    foreach (var entity in batch)
    {
        await client.From<Entity>().Insert(entity);
    }
}

await transaction.CommitAsync();
```

### Error Handling

The library follows PostgreSQL RLS conventions:

- **Silent Failures**: Security violations result in no data/operations, not exceptions
- **Validation Errors**: Input validation throws `ArgumentException`
- **Connection Errors**: Database errors propagate as-is
- **Authentication Errors**: Return `null` from `Authenticate()`

### Memory Usage

- **Connection Reuse**: Single connection per client instance
- **Policy Caching**: RLS policies cached in registry
- **Parameter Pooling**: Dapper handles parameter object pooling
- **Type Reflection**: Column mappings cached statically per type

---

**API Version**: 2.0.0  
**Last Updated**: [Current Date]  
**Security Reviewed**: [Review Date]