# Security: Row-Level Security Architecture

Jarvis provides **automatic data isolation** through Row-Level Security (RLS). This document explains how identity flows from authentication through to database-level access control.

## Why This Matters

When you query data in Jarvis, you automatically get only the data you're authorized to see:

```csharp
// This query is AUTOMATICALLY filtered by RLS
var orders = await dataContext.Query()
    .WithAll<OrderComponent>()
    .ToEntityComponents();

// User only sees their tenant's orders - framework handles this
```

Developers don't write access control logic. The framework enforces it at the database level via JWT claims.

## The RLS Flow

```
+------------------+     +------------------+     +------------------+
|   User Login     | --> |   JWT Created    | --> |  JWT Contains    |
|   (credentials)  |     |   (AuthHandler)  |     |  sub, tenant_id, |
|                  |     |                  |     |  role, etc.      |
+------------------+     +------------------+     +------------------+
                                                          |
                                                          v
+------------------+     +------------------+     +------------------+
|   PostgreSQL     | <-- |   Session Vars   | <-- |   PgClient       |
|   RLS Policies   |     |   jwt.claims.*   |     |   .JWT(token)    |
+------------------+     +------------------+     +------------------+
```

**The key insight:** JWT claims become PostgreSQL session variables, which RLS policies use to filter data.

## Framework Identity Components

These components are **framework infrastructure**, not application code. They exist to feed the RLS system.

### Account

Stores authentication credentials:

```csharp
public record Account : IComponent
{
    public string Email { get; init; }
    public string PasswordHash { get; init; }  // BCrypt hash
    public string AuthMethod { get; init; }    // "password", "otp", etc.
    public bool IsActive { get; set; }
}
```

**Purpose:** Identity storage for authentication. NOT application-specific user data.

### AuthToken

JWT wrapper with session metadata:

```csharp
public record AuthToken : IComponent
{
    public string AccessToken { get; init; }   // The JWT
    public string RefreshToken { get; init; }
    public Guid SessionId { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsRevoked { get; set; }
}
```

**Purpose:** Carries the JWT that flows to the database for RLS.

### SecurityProfile

Roles and permissions:

```csharp
public record SecurityProfile : IComponent
{
    public string Name { get; init; }
    public string[] RoleIds { get; init; }
    public string[] PermissionIds { get; init; }
}
```

**Purpose:** Authorization model that maps to JWT claims.

## Two-Layer RLS Enforcement

Jarvis enforces RLS at two levels for defense in depth.

### Layer 1: SDK Level (PgTable)

Before any query hits the database, `PgTable` checks the RLS policy:

```csharp
// Inside PgTable.Get()
var rlsWhereClause = _rlsPolicies.BuildWhereClause(tableName, _jwtClaims);
// Appends: WHERE tenant_id = '{claims["tenant_id"]}'
```

For INSERT/UPDATE/DELETE:

```csharp
// Inside PgTable.Insert()
if (!_rlsPolicies.CheckOperation(tableName, PolicyType.Insert, _jwtClaims, data))
{
    return; // Silently denied - like PostgreSQL RLS behavior
}
```

### Layer 2: Database Level (PostgreSQL)

PostgreSQL policies use session variables set by PgClient:

```sql
-- PostgreSQL RLS policy
CREATE POLICY tenant_isolation ON orders
    FOR ALL
    USING (tenant_id = current_setting('jwt.claims.tenant_id')::uuid);
```

Both layers must pass. If either denies, data is not accessible.

## JWT Claims to Session Variables

When you call `pgClient.JWT(token)`, the following happens:

1. **JWT Validation:** Signature, expiration, issuer, audience
2. **Claims Extraction:** Parse claims into dictionary
3. **Session Setup:** Set PostgreSQL session variables

```csharp
// PgClient.JWTClaims() executes:
SET SESSION "jwt.claims.sub" = '11111111-1111-1111-1111-111111111111';
SET SESSION "jwt.claims.tenant_id" = 'f47ac10b-58cc-4372-a567-0e02b2c3d479';
SET SESSION "jwt.claims.role" = 'manager';
```

PostgreSQL functions then read these:

```sql
-- In your database
CREATE FUNCTION current_tenant_id() RETURNS uuid AS $$
    SELECT current_setting('jwt.claims.tenant_id')::uuid;
$$ LANGUAGE sql STABLE;
```

## Default RLS Policies

Jarvis includes default policies for common patterns:

### Multi-Tenant Isolation

All rows filtered by `tenant_id`:

```csharp
registry.RegisterPolicy(new RLSPolicy
{
    TableName = "tenant_data",
    Type = PolicyType.All,
    WhereClause = claims => $"tenant_id = '{claims["tenant_id"]}'::uuid"
});
```

### User-Level Access

Users see only their own data:

```csharp
registry.RegisterPolicy(new RLSPolicy
{
    TableName = "user_data",
    Type = PolicyType.Select,
    WhereClause = claims =>
        $"tenant_id = '{claims["tenant_id"]}'::uuid " +
        $"AND (user_id = '{claims["sub"]}'::uuid OR is_public = TRUE)"
});
```

### Role-Based Access

Visibility varies by role:

```csharp
registry.RegisterPolicy(new RLSPolicy
{
    TableName = "sensitive_data",
    Type = PolicyType.Select,
    WhereClause = claims => claims["role"] switch
    {
        "admin" => "1=1",  // See all
        "manager" => "classification IN ('public', 'internal', 'confidential')",
        "user" => "classification IN ('public', 'internal')",
        _ => "classification = 'public'"
    }
});
```

## Custom RLS Policies

Extend the framework with your own policies:

```csharp
// In your application startup
public void ConfigureRLS(RLSPolicyRegistry registry)
{
    // Hierarchy-based access: users see their org and children
    registry.RegisterPolicy(new RLSPolicy
    {
        TableName = "organization_data",
        Type = PolicyType.Select,
        WhereClause = claims =>
            $"org_path LIKE '{claims["org_path"]}%'"  // Org path prefix matching
    });

    // Time-based access: only see recent records
    registry.RegisterPolicy(new RLSPolicy
    {
        TableName = "audit_logs",
        Type = PolicyType.Select,
        WhereClause = claims =>
            claims["role"] == "admin"
                ? "1=1"
                : "created_at > NOW() - INTERVAL '30 days'"
    });
}
```

## Security Guarantees

### What the Framework Enforces

1. **JWT Validation:** Invalid tokens rejected before data access
2. **Claim Propagation:** Claims always flow to database
3. **SDK RLS Checks:** Pre-query filtering at SDK level
4. **SQL Injection Prevention:** Claims sanitized, queries parameterized
5. **Session Isolation:** Each request gets its own session variables

### What You Must Handle

1. **JWT Secret Management:** Keep signing keys secure
2. **Claim Assignment:** Put correct tenant_id, role in tokens
3. **Custom Policies:** Register policies for your domain tables
4. **Audit Logging:** Track who accessed what (use SecurityAuditService)

## Complete Authentication Flow

```
1. User submits credentials
   POST /auth { email, password }

2. AuthHandler validates
   - Query Account by email
   - Verify password hash (BCrypt)
   - Check IsActive status

3. JWT generated with claims
   {
     "sub": "user-id",
     "tenant_id": "tenant-id",
     "role": "manager",
     "exp": 1234567890
   }

4. Client stores JWT, sends in Authorization header
   Authorization: Bearer eyJhbG...

5. Request reaches application
   - Middleware validates JWT
   - PgClient receives JWT
   - Claims set as session variables

6. Handler queries data
   var orders = await dataContext.Query()
       .WithAll<OrderComponent>()
       .ToEntityComponents();

7. RLS filters results
   SDK: Adds WHERE tenant_id = '{tenant_id}'
   DB: PostgreSQL policy also filters

8. User receives only their data
```

## Summary

| Component | Purpose | Location |
|-----------|---------|----------|
| Account | Identity storage | Framework |
| AuthToken | JWT carrier | Framework |
| SecurityProfile | Roles/permissions | Framework |
| PgClient.JWT() | Claims to session vars | Framework |
| RLSPolicyRegistry | Policy enforcement | Framework |
| Custom Policies | Domain-specific rules | Application |

**Key Takeaway:** Account and Auth components are framework infrastructure for RLS - not application code. Every Jarvis application uses them to get automatic data isolation.

**Next:** [06-database.md](06-database.md) - Database patterns and DataContext API
