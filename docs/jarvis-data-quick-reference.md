# Jarvis.Data Quick Reference Guide

## Setup & Configuration

### Basic Setup
```csharp
var conn = new NpgsqlConnection(connectionString);
var client = await PgClientFactory.Create(conn);
```

### With Custom RLS Policies
```csharp
var registry = new RLSPolicyRegistry();
// Add custom policies...
var client = await PgClientFactory.Create(conn, registry);
```

### Authentication
```csharp
var jwt = await client.Authenticate("user@example.com", "password");
client.JWT(jwt);
```

## CRUD Operations

### Create (Insert)
```csharp
var product = new Product { Name = "Laptop", Price = 999.99m };
await client.From<Product>().Insert(product);
```

### Read (Get)
```csharp
// Get all
var products = await client.From<Product>().Get();

// With filters
var electronics = await client.From<Product>()
    .Filter("category", "eq", "Electronics")
    .Filter("price", "gte", 100)
    .Get();

// Single record
var product = await client.From<Product>()
    .Filter("id", "eq", productId)
    .SingleOrDefault();
```

### Update
```csharp
product.Price = 899.99m;
await client.From<Product>().Update(product);
```

### Delete
```csharp
await client.From<Product>()
    .Filter("id", "eq", productId)
    .Delete();
```

### Upsert
```csharp
await client.From<Product>().Upsert(product);
```

## Filtering

### Basic Filters
```csharp
.Filter("column_name", "eq", value)     // Equal
.Filter("column_name", "neq", value)    // Not equal
.Filter("column_name", "lt", value)     // Less than
.Filter("column_name", "lte", value)    // Less than or equal
.Filter("column_name", "gt", value)     // Greater than
.Filter("column_name", "gte", value)    // Greater than or equal
```

### IN Clause
```csharp
var categories = new[] { "Electronics", "Books", "Clothing" };
await client.From<Product>().In("category", categories).Get();
```

### Chaining Filters
```csharp
var results = await client.From<Product>()
    .Filter("category", "eq", "Electronics")
    .Filter("price", "gte", 100)
    .Filter("is_active", "eq", true)
    .Get();
```

## Entity Mapping

### Property to Column Mapping
```csharp
public record MyEntity
{
    public Guid Id { get; set; }                    // → id
    public string ProductName { get; set; }         // → product_name
    public bool IsActive { get; set; }              // → is_active
    public DateTime CreatedAt { get; set; }         // → created_at
    public string XMLData { get; set; }             // → xml_data
    public string APIKey { get; set; }              // → api_key
    public Guid CustomerID { get; set; }            // → customer_id
}
```

### JSONB Support
```csharp
public record ComponentEntity
{
    public string Metadata { get; set; } = "{}";    // Auto-cast to JSONB
    public string Snapshots { get; set; } = "[]";   // Auto-cast to JSONB
    public string ChildTypes { get; set; } = "[]";  // Auto-cast to JSONB
}
```

## RLS Policies

### Register Multi-Tenant Policy
```csharp
registry.RegisterPolicy(new RLSPolicy
{
    TableName = "orders",
    Type = PolicyType.All,
    WhereClause = claims => claims.TryGetValue("tenant_id", out var id) 
        ? $"tenant_id = '{id}'::uuid" 
        : "1=0",
    CheckFunction = (claims, data) =>
    {
        return claims.TryGetValue("tenant_id", out var claimTenant) &&
               data.TryGetValue("tenant_id", out var dataTenant) &&
               claimTenant == dataTenant?.ToString();
    }
});
```

### Register Role-Based Policy
```csharp
registry.RegisterPolicy(new RLSPolicy
{
    TableName = "sensitive_data",
    Type = PolicyType.Select,
    WhereClause = claims =>
    {
        if (!claims.TryGetValue("role", out var role)) return "1=0";
        
        return role.ToLower() switch
        {
            "admin" => "1=1",
            "manager" => "classification IN ('public', 'internal', 'confidential')",
            "user" => "classification IN ('public', 'internal')",
            _ => "classification = 'public'"
        };
    }
});
```

## JWT Integration

### Generate JWT with Claims
```csharp
var jwt = GenerateJWT(
    userId: Guid.NewGuid(),
    tenantId: Guid.NewGuid(),
    role: "manager",
    additionalClaims: new Dictionary<string, string>
    {
        { "department", "engineering" },
        { "clearance_level", "secret" }
    }
);
```

### Standard Claims
- `sub`: User ID
- `tenant_id`: Tenant identifier
- `role`: User role
- Custom claims: Any additional claims

## Remote Procedures

### Call PostgreSQL Function
```csharp
await client.Rpc("calculate_commission", new
{
    sales_person_id = salesPersonId,
    period_start = DateTime.UtcNow.AddMonths(-1),
    period_end = DateTime.UtcNow,
    bonus_rate = 0.05m
});
```

## Transactions

### Basic Transaction
```csharp
using var transaction = await conn.BeginTransactionAsync();
try
{
    await client.From<Order>().Insert(order);
    await client.From<OrderItem>().Insert(orderItem);
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## Common Patterns

### Multi-Tenant Data Access
```csharp
// Tenant isolation is automatic with JWT
client.JWT(tenantUserJwt);
var tenantData = await client.From<CustomerData>().Get();
// Only returns data for the user's tenant
```

### User-Specific Data
```csharp
// Users see their own data + public data from their tenant
var userData = await client.From<UserData>().Get();
// Auto-applies: WHERE tenant_id = '{jwt.tenant_id}' 
//               AND (user_id = '{jwt.sub}' OR is_public = TRUE)
```

### Role-Based Access
```csharp
// Different data based on user role
var sensitiveData = await client.From<SensitiveData>().Get();
// Automatically filters based on role in JWT
```

## Error Handling

### SQL Injection Protection
```csharp
// These will throw ArgumentException
.Filter("invalid_column", "eq", "value")    // Column not in entity
.Filter("name", "invalid_op", "value")      // Invalid operator

// These are safe
.Filter("name", "eq", "O'Malley")          // Values are parameterized
.Filter("price", "gte", 100)               // Type-safe operations
```

### RLS Failures
```csharp
// RLS violations fail silently (like PostgreSQL)
await client.From<TenantData>().Insert(new TenantData
{
    TenantId = differentTenantId // Will silently fail if not allowed
});

// Check if operation succeeded by querying
var inserted = await client.From<TenantData>()
    .Filter("id", "eq", entity.Id)
    .SingleOrDefault();
```

## Performance Tips

### Use Indexes for RLS
```sql
-- Recommended indexes
CREATE INDEX idx_tenant_data_tenant_id ON tenant_data(tenant_id);
CREATE INDEX idx_user_data_tenant_user ON user_data(tenant_id, user_id);
CREATE INDEX idx_orders_tenant_status ON orders(tenant_id, status);
```

### Batch Operations
```csharp
// Use transactions for bulk operations
using var transaction = await conn.BeginTransactionAsync();
foreach (var item in items)
{
    await client.From<Item>().Insert(item);
}
await transaction.CommitAsync();
```

### Filter Early
```csharp
// Apply most selective filters first
await client.From<Product>()
    .Filter("category", "eq", "Electronics")    // Most selective
    .Filter("price", "gte", 100)
    .Filter("in_stock", "eq", true)
    .Get();
```

## Testing Patterns

### Test Structure
```csharp
/// <summary>
/// INTENT: Verify tenant isolation prevents data leakage
/// PURPOSE: Ensure security boundaries are enforced
/// BUSINESS CONTEXT: Multi-tenant SaaS data protection
/// WHY IMPORTANT: GDPR compliance and customer trust
/// ARCHITECTURAL SIGNIFICANCE: Validates RLS engine
/// FUTURE RESILIENCE: Prevents security regressions
/// </summary>
[Fact]
public async Task TenantIsolation_PreventsDataLeakage()
{
    // Arrange
    var tenant1 = Guid.NewGuid();
    var tenant2 = Guid.NewGuid();
    
    await SeedTenantData(tenant1, "Tenant 1 Data");
    await SeedTenantData(tenant2, "Tenant 2 Data");
    
    var jwt = GenerateJWT(tenantId: tenant1);
    _client.JWT(jwt);
    
    // Act
    var results = await _client.From<TenantData>().Get();
    
    // Assert
    results.Count.ShouldBe(1);
    results[0].TenantId.ShouldBe(tenant1);
}
```

### Common Test Scenarios
```csharp
// Test tenant isolation
[Fact] public async Task Get_WithTenantJWT_ReturnsOnlyTenantData()

// Test role-based access
[Fact] public async Task Get_WithUserRole_ReturnsLimitedData()

// Test SQL injection protection
[Fact] public void Filter_WithMaliciousInput_ThrowsException()

// Test authentication
[Fact] public async Task Authenticate_WithValidCredentials_ReturnsJWT()

// Test RLS enforcement
[Fact] public async Task Insert_WithInvalidTenant_SilentlyFails()
```

## Common Issues & Solutions

### "Column 'xyz' is not allowed"
```csharp
// Problem: Column name doesn't match entity property
.Filter("productName", "eq", "value")  // ❌ Wrong

// Solution: Use snake_case column name
.Filter("product_name", "eq", "value") // ✅ Correct

// Or add property to entity
public string ProductName { get; set; }
```

### "Required table 'users' does not exist"
```sql
-- Create required users table
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL
);

-- Enable RLS
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
CREATE POLICY user_access ON users FOR ALL USING (true);
```

### Empty Results When Expected Data
```csharp
// Check JWT is set correctly
client.JWT(validJwtToken);

// Verify JWT contains required claims
// Check RLS policies allow access
// Ensure entity properties match database columns
```

### Upsert Not Working
```csharp
// For components, ensure OwnerEntityId is set
var component = new BlogPostComponent
{
    Id = Guid.NewGuid(),           // Set for updates
    OwnerEntityId = blogPostId,    // Required for components
    // ... other properties
};
```

## Security Checklist

- ✅ Always validate JWTs before calling `client.JWT()`
- ✅ Use environment variables for connection strings
- ✅ Test all security boundaries
- ✅ Never store sensitive data in JWT claims
- ✅ Use transactions for related operations
- ✅ Handle null results from `SingleOrDefault()`
- ✅ Apply appropriate database indexes
- ✅ Follow test documentation template
- ✅ Use parameterized queries (automatic)
- ✅ Validate RLS policies work as expected

---

**Quick Reference Version**: 2.0.0  
**For Full Documentation**: See [jarvis-data-documentation.md](jarvis-data-documentation.md)