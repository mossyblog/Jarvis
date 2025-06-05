# Jarvis.Data Library Documentation

## Overview

Jarvis.data is a secure, convention-based PostgreSQL data access library for .NET that implements **SDK-level Row Level Security (RLS)**, JWT-based authentication, and automatic PascalCase to snake_case mapping. Unlike traditional database-level RLS, this library enforces security policies within the SDK itself, providing complete control over data access regardless of database configuration.

## Table of Contents

1. [Architecture](#architecture)
2. [Installation & Setup](#installation--setup)
3. [Core Concepts](#core-concepts)
4. [Security Features](#security-features)
5. [Usage Patterns](#usage-patterns)
6. [Advanced Features](#advanced-features)
7. [Testing & Best Practices](#testing--best-practices)
8. [API Reference](#api-reference)
9. [Migration Guide](#migration-guide)
10. [Troubleshooting](#troubleshooting)

## Architecture

### Core Components

The library consists of four main components working together:

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   PgClient      │    │   PgTable<T>    │    │ RLSPolicyRegistry│
│                 │    │                 │    │                 │
│ - JWT Handling  │◄──►│ - CRUD Ops      │◄──►│ - Policy Rules  │
│ - Authentication│    │ - Type Safety   │    │ - Access Control│
│ - Connection    │    │ - SQL Building  │    │ - Multi-tenant  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
           │                       │                       │
           └───────────────────────┼───────────────────────┘
                                   │
                          ┌─────────────────┐
                          │ StringExtensions│
                          │                 │
                          │ - snake_case    │
                          │   Mapping       │
                          └─────────────────┘
```

### Design Principles

1. **Security First**: All operations are secured by default with multiple protection layers
2. **Convention over Configuration**: Automatic mapping reduces boilerplate
3. **Type Safety**: Leverage C# type system for compile-time validation
4. **SDK-Level RLS**: Security policies independent of database permissions
5. **Zero Trust**: Every operation validated against JWT claims and RLS policies

## Installation & Setup

### Package Installation

```xml
<PackageReference Include="core.jarvis.data" Version="2.0.0" />
```

### Dependencies

The library requires these NuGet packages (automatically included):
- `Npgsql` - PostgreSQL driver
- `Dapper` - Micro-ORM for SQL operations
- `BCrypt.Net-Next` - Password hashing
- `System.IdentityModel.Tokens.Jwt` - JWT handling

### Database Requirements

Your PostgreSQL database must have:

1. **Users table** with authentication support:
```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);
```

2. **At least one RLS policy** on the users table:
```sql
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
CREATE POLICY user_isolation ON users FOR ALL USING (true);
```

### Basic Setup

```csharp
// Create connection
var connectionString = "Host=localhost;Database=mydb;Username=myuser;Password=mypass";
var conn = new NpgsqlConnection(connectionString);

// Create client with default policies
var client = await PgClientFactory.Create(conn);

// Or with custom policies
var registry = new RLSPolicyRegistry();
// ... configure custom policies ...
var client = await PgClientFactory.Create(conn, registry);
```

## Core Concepts

### 1. SDK-Level Row Level Security

Unlike traditional database RLS, Jarvis.data enforces security at the **SDK level**:

**Traditional Database RLS:**
```sql
-- Database enforces this
CREATE POLICY tenant_isolation ON orders 
FOR ALL USING (tenant_id = current_setting('app.current_tenant')::uuid);
```

**Jarvis.data SDK RLS:**
```csharp
// SDK enforces this before queries reach database
registry.RegisterPolicy(new RLSPolicy
{
    TableName = "orders",
    Type = PolicyType.All,
    WhereClause = claims => claims.TryGetValue("tenant_id", out var id) 
        ? $"tenant_id = '{id}'::uuid" 
        : "1=0"
});
```

**Advantages:**
- Works with any database user (no special permissions needed)
- Policies checked before queries reach database
- Complete control over access logic
- Easy testing and debugging

### 2. Automatic PascalCase to snake_case Mapping

The library automatically converts C# naming conventions to PostgreSQL conventions:

```csharp
public record CustomerOrder
{
    public Guid Id { get; set; }                    // → id
    public string CustomerName { get; set; }        // → customer_name  
    public bool IsActive { get; set; }              // → is_active
    public DateTime CreatedAt { get; set; }         // → created_at
    public string? XMLData { get; set; }            // → xml_data
    public string? APIKey { get; set; }             // → api_key
    public Guid CustomerID { get; set; }            // → customer_id
}
```

**Complex Cases Handled:**
- Acronyms: `XMLContent` → `xml_content`
- ID suffixes: `CustomerID` → `customer_id` 
- Boolean prefixes: `IsActive` → `is_active`
- Consecutive capitals: `IOOperation` → `io_operation`

### 3. JWT-Based Authentication

The library integrates seamlessly with JWT tokens:

```csharp
// Authenticate user
var jwt = await client.Authenticate("user@example.com", "password");

// Set JWT for subsequent operations
client.JWT(jwt);

// Claims are automatically available to RLS policies
var data = await client.From<UserData>().Get();
// Automatically applies: WHERE tenant_id = '{jwt.tenant_id}' AND user_id = '{jwt.sub}'
```

**Standard Claims Used:**
- `sub`: User ID
- `tenant_id`: Tenant identifier for multi-tenant isolation
- `role`: User role for RBAC
- Custom claims: Any additional claims in the JWT

## Security Features

### 1. Multi-Layer SQL Injection Prevention

The library provides comprehensive protection against SQL injection:

**Layer 1: Column Whitelisting**
```csharp
// Only allows columns that exist as entity properties
private static readonly HashSet<string> AllowedColumns = typeof(T).GetProperties()
    .Select(p => p.Name.ToSnakeCase())
    .ToHashSet();
```

**Layer 2: Operator Whitelisting**  
```csharp
// Strictly limits allowed operators
private static readonly HashSet<string> AllowedOperators = 
    new() { "eq", "neq", "lt", "lte", "gt", "gte" };
```

**Layer 3: Parameterized Queries**
```csharp
// All values are parameterized, never concatenated
var sql = "SELECT * FROM users WHERE email = @Email";
await _conn.QueryAsync<User>(sql, new { Email = userEmail });
```

**Layer 4: Type Safety**
```csharp
// Table names derived from type system, not user input
_tableName = typeof(T).Name.ToSnakeCase();
```

### 2. Default RLS Policies

The library includes battle-tested policies for common scenarios:

**Multi-Tenant Isolation:**
```csharp
// Users only see data from their tenant
await client.From<TenantData>().Get();
// Auto-applies: WHERE tenant_id = '{jwt.tenant_id}'
```

**User-Level Security:**
```csharp
// Users see their own data + public data from their tenant  
await client.From<UserData>().Get();
// Auto-applies: WHERE tenant_id = '{jwt.tenant_id}' 
//               AND (user_id = '{jwt.sub}' OR is_public = TRUE)
```

**Role-Based Access:**
```csharp
// Access based on user role
await client.From<SensitiveData>().Get();
// Filters by role:
// - 'user': classification IN ('public', 'internal')
// - 'manager': classification IN ('public', 'internal', 'confidential')  
// - 'admin': all classifications
```

### 3. Custom RLS Policies

Create domain-specific security policies:

```csharp
var registry = new RLSPolicyRegistry();

// Department-based access policy
registry.RegisterPolicy(new RLSPolicy
{
    TableName = "financial_reports",
    Type = PolicyType.Select,
    WhereClause = claims =>
    {
        if (!claims.TryGetValue("department", out var dept))
            return "1=0"; // No access without department
            
        return dept switch
        {
            "finance" => "1=1", // Finance sees all reports
            "executive" => "classification != 'confidential'", // Executives see most
            _ => "is_public = TRUE" // Others see only public
        };
    }
});

// Operation validation for inserts/updates
registry.RegisterPolicy(new RLSPolicy
{
    TableName = "financial_reports",
    Type = PolicyType.Insert,
    CheckFunction = (claims, data) =>
    {
        // Only finance department can create reports
        return claims.TryGetValue("department", out var dept) && 
               dept == "finance";
    }
});
```

## Usage Patterns

### 1. Basic CRUD Operations

```csharp
// Setup
var client = await PgClientFactory.Create(conn);
var jwt = await client.Authenticate("user@example.com", "password");
client.JWT(jwt);

// Create
var newProduct = new Product
{
    Name = "Laptop",
    Price = 999.99m,
    Category = "Electronics"
};
await client.From<Product>().Insert(newProduct);

// Read with filtering
var expensiveElectronics = await client.From<Product>()
    .Filter("category", "eq", "Electronics")
    .Filter("price", "gte", 500)
    .Get();

// Read single record
var product = await client.From<Product>()
    .Filter("id", "eq", productId)
    .SingleOrDefault();

// Update
product.Price = 899.99m;
await client.From<Product>().Update(product);

// Delete
await client.From<Product>()
    .Filter("id", "eq", productId)
    .Delete();
```

### 2. Advanced Querying

```csharp
// Multiple filters with IN clause
var categories = new[] { "Electronics", "Books", "Clothing" };
var products = await client.From<Product>()
    .In("category", categories)
    .Filter("is_active", "eq", true)
    .Get();

// Complex filtering
var premiumProducts = await client.From<Product>()
    .Filter("price", "gte", 100)
    .Filter("rating", "gt", 4.0)
    .Filter("in_stock", "eq", true)
    .Get();

// Upsert operations
var component = new BlogPostComponent
{
    OwnerEntityId = blogPostId,
    Title = "Updated Title",
    Content = "Updated content..."
};
await client.From<BlogPostComponent>().Upsert(component);
```

### 3. Multi-Tenant Patterns

```csharp
// Tenant isolation is automatic with JWT
client.JWT(tenantUserJwt); // Contains tenant_id claim

// All operations automatically scoped to tenant
var tenantData = await client.From<CustomerData>().Get();
// Auto-applies: WHERE tenant_id = '{jwt.tenant_id}'

// Cross-tenant access is impossible (returns empty)
var otherTenantData = await client.From<CustomerData>()
    .Filter("tenant_id", "eq", "other-tenant-id") // This will return empty
    .Get();
```

### 4. Role-Based Operations

```csharp
// Admin JWT with role claim
client.JWT(adminJwt); // Contains role: "admin"

// Admin sees all sensitive data
var allSensitive = await client.From<SensitiveData>().Get();

// Manager JWT  
client.JWT(managerJwt); // Contains role: "manager"

// Manager sees limited sensitive data
var managerSensitive = await client.From<SensitiveData>().Get();
// Auto-applies: WHERE classification IN ('public', 'internal', 'confidential')

// Regular user JWT
client.JWT(userJwt); // Contains role: "user"

// User sees even more limited data
var userSensitive = await client.From<SensitiveData>().Get();
// Auto-applies: WHERE classification IN ('public', 'internal')
```

## Advanced Features

### 1. Custom JWT Claims

Extend RLS policies with custom claims:

```csharp
// Generate JWT with custom claims
var jwt = GenerateJWT(
    userId: userId,
    tenantId: tenantId,
    role: "analyst",
    additionalClaims: new Dictionary<string, string>
    {
        { "department", "engineering" },
        { "clearance_level", "secret" },
        { "region", "us-west" },
        { "project_access", "project-alpha,project-beta" }
    }
);

// Create policies using custom claims
registry.RegisterPolicy(new RLSPolicy
{
    TableName = "classified_documents",
    Type = PolicyType.Select,
    WhereClause = claims =>
    {
        var conditions = new List<string>();
        
        // Region-based access
        if (claims.TryGetValue("region", out var region))
            conditions.Add($"region = '{region}'");
            
        // Clearance level check
        if (claims.TryGetValue("clearance_level", out var clearance))
        {
            var allowedLevels = clearance switch
            {
                "top_secret" => "('public', 'internal', 'secret', 'top_secret')",
                "secret" => "('public', 'internal', 'secret')",
                "internal" => "('public', 'internal')",
                _ => "('public')"
            };
            conditions.Add($"classification IN {allowedLevels}");
        }
        
        // Project access
        if (claims.TryGetValue("project_access", out var projects))
        {
            var projectList = projects.Split(',')
                .Select(p => $"'{p.Trim()}'");
            conditions.Add($"project_id IN ({string.Join(", ", projectList)})");
        }
        
        return conditions.Any() ? string.Join(" AND ", conditions) : "1=0";
    }
});
```

### 2. Remote Procedure Calls (RPC)

Execute PostgreSQL functions with automatic JWT propagation:

```csharp
// Call stored procedure with named parameters
await client.Rpc("calculate_commission", new
{
    sales_person_id = salesPersonId,
    period_start = DateTime.UtcNow.AddMonths(-1),
    period_end = DateTime.UtcNow,
    bonus_rate = 0.05m
});

// JWT claims are automatically set as session variables
// Available in the stored procedure as current_setting('jwt.claims.tenant_id'), etc.
```

### 3. JSONB Support

Special handling for PostgreSQL JSONB columns:

```csharp
public record ComponentEntity
{
    public Guid Id { get; set; }
    public string Metadata { get; set; } = "{}"; // Maps to JSONB
    public string Snapshots { get; set; } = "[]"; // Maps to JSONB  
    public string ChildTypes { get; set; } = "[]"; // Maps to JSONB
}

// Insert with JSONB casting
await client.From<ComponentEntity>().Insert(new ComponentEntity
{
    Metadata = """{"version": "1.0", "features": ["ai", "analytics"]}""",
    Snapshots = """[{"timestamp": "2024-01-01", "state": "active"}]""",
    ChildTypes = """["blog_post", "product_page"]"""
});
```

### 4. Batch Operations

Efficient bulk operations:

```csharp
// Bulk insert (call Insert multiple times in transaction)
using var transaction = await conn.BeginTransactionAsync();
try
{
    foreach (var product in productList)
    {
        await client.From<Product>().Insert(product);
    }
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}

// Bulk filtering with IN clause
var productIds = orders.Select(o => o.ProductId).ToList();
var products = await client.From<Product>()
    .In("id", productIds.Cast<object>())
    .Get();
```

## Testing & Best Practices

### 1. Test Structure

Follow the established test patterns:

```csharp
/// <summary>
/// INTENT: Verify that users can only access data from their own tenant
/// PURPOSE: Ensure multi-tenant data isolation works correctly
/// BUSINESS CONTEXT: Prevents data leakage between different customer organizations
/// WHY IMPORTANT: Data isolation is critical for GDPR compliance and customer trust
/// ARCHITECTURAL SIGNIFICANCE: Validates the core RLS policy engine
/// FUTURE RESILIENCE: Protects against regressions in tenant isolation logic
/// </summary>
[Fact]
public async Task Get_WithTenantId_ReturnsOnlyTenantData()
{
    // Arrange
    var tenant1Id = Guid.NewGuid();
    var tenant2Id = Guid.NewGuid();
    
    await SeedTenantData(tenant1Id, "Tenant 1 Data");
    await SeedTenantData(tenant2Id, "Tenant 2 Data");
    
    var jwt = GenerateJWT(userId: Guid.NewGuid(), tenantId: tenant1Id);
    _client.JWT(jwt);
    
    // Act
    var results = await _client.From<TenantData>().Get();
    
    // Assert
    results.Count.ShouldBe(1);
    results[0].TenantId.ShouldBe(tenant1Id);
    results[0].Title.ShouldBe("Tenant 1 Data");
}
```

### 2. Security Testing

Test all security boundaries:

```csharp
[Fact]
public async Task Insert_WithMismatchedTenantId_SilentlyFails()
{
    // Arrange - JWT claims tenant A, but trying to insert for tenant B
    var userTenantId = Guid.NewGuid();
    var targetTenantId = Guid.NewGuid();
    
    var jwt = GenerateJWT(userId: Guid.NewGuid(), tenantId: userTenantId);
    _client.JWT(jwt);
    
    // Act - Try to insert data for different tenant
    var badData = new TenantData
    {
        TenantId = targetTenantId, // Different from JWT claim
        Title = "Unauthorized Data"
    };
    
    await _client.From<TenantData>().Insert(badData);
    
    // Assert - Data should not be inserted
    var allData = await _client.From<TenantData>().Get();
    allData.Count.ShouldBe(0);
}
```

### 3. Best Practices

**Do:**
- Always set JWT before operations
- Use strongly-typed entities
- Test all security boundaries
- Use transactions for related operations
- Handle null results from `SingleOrDefault()`

**Don't:**
- Concatenate SQL strings manually
- Bypass RLS policies
- Store sensitive data in JWT claims
- Use raw SQL queries
- Assume operations will succeed

## API Reference

### PgClient

```csharp
public class PgClient
{
    // Authentication
    Task<string?> Authenticate(string email, string password)
    void JWT(string jwt)
    
    // Table Access
    PgTable<T> From<T>() where T : class, new()
    
    // Remote Procedures
    Task Rpc(string functionName, object args)
}
```

### PgTable<T>

```csharp
public class PgTable<T>
{
    // Filtering
    PgTable<T> Filter(string column, string op, object value)
    PgTable<T> In(string column, IEnumerable<object> values)
    
    // CRUD Operations
    Task Insert(T entity)
    Task Update(T entity)
    Task Upsert(T entity)
    Task<int> Delete()
    
    // Querying
    Task<List<T>> Get()
    Task<T> Single()
    Task<T?> SingleOrDefault()
}
```

### RLS Policy System

```csharp
public class RLSPolicy
{
    string TableName { get; set; }
    PolicyType Type { get; set; }
    Func<Dictionary<string, string>, string>? WhereClause { get; set; }
    Func<Dictionary<string, string>, Dictionary<string, object>, bool>? CheckFunction { get; set; }
}

public class RLSPolicyRegistry
{
    void RegisterPolicy(RLSPolicy policy)
    string BuildWhereClause(string tableName, Dictionary<string, string> claims)
    bool CheckOperation(string tableName, PolicyType type, Dictionary<string, string> claims, Dictionary<string, object> data)
}
```

## Migration Guide

### From Version 1.x to 2.x

**Breaking Changes:**
- Method names simplified (removed `Async` suffix)
- RLS policies now required for secured tables
- Enhanced snake_case mapping

**Migration Steps:**

1. **Update method calls:**
```csharp
// v1.x
var client = await PgClientFactory.CreateAsync(conn);

// v2.x  
var client = await PgClientFactory.Create(conn);
```

2. **Add RLS policies for existing tables:**
```csharp
// v2.x requires explicit policies
var registry = new RLSPolicyRegistry();
DefaultRLSPolicies.RegisterDefaultPolicies(registry);
var client = await PgClientFactory.Create(conn, registry);
```

3. **Review entity property names:**
```csharp
// Some edge cases now map differently
public string XMLData { get; set; } // v1.x: xmldata, v2.x: xml_data
public string APIKey { get; set; }  // v1.x: apikey, v2.x: api_key
```

## Troubleshooting

### Common Issues

**1. "Required table 'users' does not exist"**
```sql
-- Create the required users table
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL
);
```

**2. "No RLS policy found on 'users' table"**  
```sql
-- Enable RLS and create a basic policy
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
CREATE POLICY user_access ON users FOR ALL USING (true);
```

**3. "Column 'xyz' is not allowed"**
```csharp
// Ensure property exists on entity and matches exactly
public record MyEntity
{
    public string ColumnName { get; set; } // Must match filter column
}

// Use exact property name in snake_case
.Filter("column_name", "eq", value) // ✓ Correct
.Filter("columnName", "eq", value)  // ✗ Wrong
```

**4. Operations returning empty results unexpectedly**
```csharp
// Check JWT is set
client.JWT(validJwtToken);

// Verify JWT contains required claims
var jwt = "your-jwt-here";
var handler = new JwtSecurityTokenHandler();
var token = handler.ReadJwtToken(jwt);
// Ensure 'sub', 'tenant_id', 'role' claims are present
```

**5. Upsert not working as expected**
```csharp
// For components, ensure OwnerEntityId is set
var component = new BlogPostComponent
{
    Id = Guid.NewGuid(), // Set ID for updates
    OwnerEntityId = blogPostId, // Required for components
    // ... other properties
};
```

### Performance Optimization

1. **Use appropriate indexes:**
```sql
-- Index commonly filtered columns
CREATE INDEX idx_products_category ON products(category);
CREATE INDEX idx_orders_tenant_id ON orders(tenant_id);
CREATE INDEX idx_user_data_tenant_user ON user_data(tenant_id, user_id);
```

2. **Batch operations when possible:**
```csharp
// Use transactions for multiple related operations
using var transaction = await conn.BeginTransactionAsync();
// ... multiple operations ...
await transaction.CommitAsync();
```

3. **Filter early:**
```csharp
// Apply most selective filters first
var results = await client.From<Product>()
    .Filter("category", "eq", "Electronics") // Most selective first
    .Filter("price", "gte", 100)
    .Filter("in_stock", "eq", true)
    .Get();
```

## Security Considerations

### JWT Validation

⚠️ **Important**: The library reads JWT claims but does NOT validate signatures. Always validate JWTs before passing to the library:

```csharp
// Example JWT validation (implement according to your security requirements)
public bool ValidateJWT(string jwt)
{
    try
    {
        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "your-issuer",
            ValidateAudience = true,
            ValidAudience = "your-audience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret")),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
        
        handler.ValidateToken(jwt, validationParameters, out var validatedToken);
        return true;
    }
    catch
    {
        return false;
    }
}

// Use only validated JWTs
if (ValidateJWT(jwtString))
{
    client.JWT(jwtString);
}
```

### Environment Configuration

Store sensitive configuration securely:

```csharp
// Use environment variables or secure configuration
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? throw new InvalidOperationException("DATABASE_URL not configured");

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET not configured");
```

## Contributing

### Development Setup

1. Clone the repository
2. Set up PostgreSQL database
3. Configure connection string in `.env.local`
4. Run tests: `dotnet test`

### Submitting Changes

1. Follow existing code style and patterns
2. Add comprehensive tests for new features
3. Update documentation
4. Ensure all tests pass
5. Submit pull request with clear description

## License

[Your License Here]

---

**Version**: 2.0.0  
**Last Updated**: [Current Date]  
**Maintained By**: [Your Team]