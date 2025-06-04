# core.jarvis.data

A secure, convention-based PostgreSQL data access library for .NET with built-in Row Level Security (RLS) enforcement, JWT authentication, and automatic snake_case mapping.

## Features

- **SDK-Level Row Level Security (RLS)**: Enforce data access policies within the SDK, independent of database permissions
- **JWT-Based Authentication**: Built-in JWT parsing and claim extraction for secure, stateless authentication
- **Automatic PascalCase to snake_case Mapping**: Write idiomatic C# code without database naming concerns
- **Multi-Tenant Data Isolation**: Complete data separation between tenants
- **Role-Based Access Control**: Fine-grained permissions based on user roles
- **Type-Safe Table Access**: Strongly-typed interface with compile-time safety
- **SQL Injection Prevention**: Parameterized queries and column/operator whitelisting

## Installation

```xml
<PackageReference Include="core.jarvis.data" Version="2.0.0" />
```

## Quick Start

```csharp
// Create a client
var connectionString = "Host=localhost;Database=mydb;Username=myuser;Password=mypass";
var conn = new NpgsqlConnection(connectionString);
var client = await PgClientFactory.Create(conn);

// Authenticate and get JWT
var jwt = await client.Authenticate("user@example.com", "password");
client.JWT(jwt);

// Access data with automatic RLS enforcement
var myData = await client.From<CustomerData>().Get();
// Only returns data the authenticated user is allowed to see
```

## Row Level Security (RLS)

### SDK-Level Enforcement

Unlike traditional database-level RLS, this library enforces security policies within the SDK itself. This means:
- Works with any database user (no special permissions required)
- Policies are checked before queries reach the database
- Complete control over data access regardless of database configuration

### Default Policies

The library includes default RLS policies for common scenarios:

#### Multi-Tenant Isolation
```csharp
// Users can only access data from their own tenant
var tenantData = await client.From<TenantData>().Get();
// Automatically filters: WHERE tenant_id = '{jwt.tenant_id}'
```

#### User-Level Security
```csharp
// Users see their own private data + public data from their tenant
var userData = await client.From<UserData>().Get();
// Automatically filters: WHERE tenant_id = '{jwt.tenant_id}' 
//                        AND (user_id = '{jwt.sub}' OR is_public = TRUE)
```

#### Role-Based Access
```csharp
// Access to sensitive data based on user role
var sensitiveData = await client.From<SensitiveData>().Get();
// Filters based on role:
// - 'user': sees public and internal
// - 'manager': sees public, internal, and confidential
// - 'admin': sees everything
```

### Custom Policies

Create custom RLS policies for your tables:

```csharp
var registry = new RLSPolicyRegistry();

// Add a custom policy
registry.RegisterPolicy(new RLSPolicy
{
    TableName = "orders",
    Type = PolicyType.Select,
    WhereClause = claims =>
    {
        if (claims.TryGetValue("department", out var dept) && dept == "sales")
            return "1=1"; // Sales sees all orders
        
        if (claims.TryGetValue("sub", out var userId))
            return $"created_by = '{userId}'::uuid"; // Others see only their orders
            
        return "1=0"; // No access
    }
});

// Create client with custom policies
var client = await PgClientFactory.Create(conn, registry);
```

## PascalCase to snake_case Mapping

Write clean C# code without worrying about PostgreSQL naming conventions:

```csharp
public record CustomerOrder
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? XMLData { get; set; }
    public string? APIKey { get; set; }
}

// Automatically maps to PostgreSQL table: customer_order
// With columns: id, customer_name, is_active, created_at, xml_data, api_key
```

Handles complex naming patterns:
- `IsActive` → `is_active`
- `XMLContent` → `xml_content`
- `CustomerID` → `customer_id`
- `IOOperation` → `io_operation`

## JWT Integration

The library automatically extracts and uses JWT claims for RLS:

```csharp
// Generate JWT with claims
var jwt = GenerateJWT(
    userId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
    tenantId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
    role: "manager",
    additionalClaims: new Dictionary<string, string>
    {
        { "department", "engineering" },
        { "clearance_level", "secret" }
    }
);

client.JWT(jwt);

// Claims are automatically available to RLS policies
```

Common JWT claims used by default policies:
- `sub`: User ID
- `tenant_id`: Tenant ID for multi-tenant isolation
- `role`: User role for RBAC

## Secure Operations

### Query Filtering
```csharp
// Type-safe filtering with SQL injection prevention
var results = await client.From<Product>()
    .Filter("price", "gte", 100)
    .Filter("category", "eq", "electronics")
    .Get();
```

### Insert with RLS Check
```csharp
var newRecord = new UserData
{
    UserId = currentUserId,
    TenantId = currentTenantId,
    Title = "My Data",
    IsPublic = false
};

// Insert only succeeds if JWT claims match the record's user_id and tenant_id
await client.From<UserData>().Insert(newRecord);
```

## Configuration

### Connection String
```csharp
// Via environment variable
Environment.SetEnvironmentVariable("DATABASE_URL", "your-connection-string");

// Or direct
var conn = new NpgsqlConnection("Host=localhost;Database=mydb;Username=myuser;Password=mypass");
```

### Custom RLS Policies
```csharp
// Option 1: Use default policies
var client = await PgClientFactory.Create(conn);

// Option 2: Custom policy registry
var registry = new RLSPolicyRegistry();
// ... register custom policies ...
var client = await PgClientFactory.Create(conn, registry);
```

## Testing

The library includes comprehensive tests demonstrating:
- Multi-tenant data isolation
- User-level security with public/private data
- Role-based access control
- JWT claim propagation
- PascalCase to snake_case mapping
- SQL injection prevention

Run tests with:
```bash
dotnet test
```

## Security Considerations

1. **JWT Validation**: The library reads JWT claims but does NOT validate signatures. Validate JWTs before passing to the library.
2. **SQL Injection**: Protected via parameterized queries and whitelisted columns/operators
3. **Default Deny**: Tables with policies deny access by default unless explicitly allowed
4. **Claim Escaping**: JWT claim values are escaped when building SQL queries

## License

[Your License Here]

## Contributing

[Your Contributing Guidelines Here]