# Row Level Security (RLS) Configuration Guide

This guide explains how to configure and use Row Level Security in Jarvis ECS with PostgreSQL.

## Overview

Row Level Security (RLS) provides fine-grained access control at the database level, ensuring users can only access data they're authorized to see. Jarvis integrates RLS seamlessly with JWT authentication.

## How RLS Works in Jarvis

1. **JWT Claims**: User identity and permissions are extracted from JWT tokens
2. **Session Variables**: Claims are set as PostgreSQL session variables
3. **RLS Policies**: Database policies use these variables to filter data
4. **Automatic Application**: Policies apply transparently to all queries

## Setting Up RLS

### 1. Enable RLS on Tables

```sql
-- Enable RLS on a table
ALTER TABLE components ENABLE ROW LEVEL SECURITY;

-- Force RLS for table owners too (recommended)
ALTER TABLE components FORCE ROW LEVEL SECURITY;
```

### 2. Create RLS Policies

```sql
-- Basic policy: Users can only see their own data
CREATE POLICY user_own_data ON components
    FOR ALL
    TO public
    USING (owner_entity_id = current_setting('jwt.claims.sub')::uuid);

-- Read-only policy for specific role
CREATE POLICY readonly_policy ON components
    FOR SELECT
    TO public
    USING (
        current_setting('jwt.claims.role') = 'viewer' OR
        owner_entity_id = current_setting('jwt.claims.sub')::uuid
    );

-- Admin bypass policy
CREATE POLICY admin_all_access ON components
    FOR ALL
    TO public
    USING (current_setting('jwt.claims.role') = 'admin');
```

### 3. Using RLS in Code

```csharp
// Set JWT for RLS
pgClient.JWT(jwtToken);

// All subsequent queries automatically apply RLS
var myComponents = await pgClient
    .From<MyComponent>()
    .Select()
    .Execute();  // Only returns user's own data
```

## Common RLS Patterns

### Multi-Tenant Isolation

```sql
-- Tenant isolation policy
CREATE POLICY tenant_isolation ON components
    FOR ALL
    TO public
    USING (
        tenant_id = current_setting('jwt.claims.tid')::uuid
    );
```

### Hierarchical Access

```sql
-- Manager can see their team's data
CREATE POLICY manager_team_access ON components
    FOR SELECT
    TO public
    USING (
        owner_entity_id = current_setting('jwt.claims.sub')::uuid
        OR
        owner_entity_id IN (
            SELECT id FROM users 
            WHERE manager_id = current_setting('jwt.claims.sub')::uuid
        )
    );
```

### Time-Based Access

```sql
-- Access expires after certain time
CREATE POLICY time_limited_access ON components
    FOR SELECT
    TO public
    USING (
        created_at > CURRENT_TIMESTAMP - INTERVAL '30 days'
        AND owner_entity_id = current_setting('jwt.claims.sub')::uuid
    );
```

## Default RLS Policies in Jarvis

Jarvis provides default RLS policies through `DefaultRLSPolicies`:

```csharp
// Registered automatically in PgClient
DefaultRLSPolicies.RegisterDefaultPolicies(rlsPolicies);
```

Default policies include:
- User data isolation (owner-based)
- Audit trail protection
- System component access control

## JWT Claims Mapping

Common JWT claims are automatically mapped:

| JWT Claim | PostgreSQL Variable | Usage |
|-----------|-------------------|--------|
| sub | jwt.claims.sub | User ID |
| role | jwt.claims.role | User role |
| email | jwt.claims.email | User email |
| tid | jwt.claims.tid | Tenant ID |
| oid | jwt.claims.oid | Object ID |

## Testing RLS Policies

```csharp
// Test with different users
var user1Token = "jwt-token-for-user-1";
var user2Token = "jwt-token-for-user-2";

// User 1's data
pgClient.JWT(user1Token);
var user1Data = await pgClient.From<Component>().Select().Execute();

// User 2's data (different results)
pgClient.JWT(user2Token);
var user2Data = await pgClient.From<Component>().Select().Execute();
```

## Debugging RLS

### Check Current Session Variables

```sql
-- View current JWT claims
SELECT current_setting('jwt.claims.sub');
SELECT current_setting('jwt.claims.role');
```

### Test Policies Directly

```sql
-- Test what a specific user can see
SET LOCAL jwt.claims.sub = 'user-id-here';
SELECT * FROM components;  -- Shows filtered results
```

### Disable RLS Temporarily (Development Only)

```sql
-- Disable for debugging
ALTER TABLE components DISABLE ROW LEVEL SECURITY;

-- Re-enable when done
ALTER TABLE components ENABLE ROW LEVEL SECURITY;
```

## Best Practices

1. **Always Enable RLS**: On all tables containing user data
2. **Use FORCE**: Apply policies even to table owners
3. **Fail Secure**: No policy = no access (default behavior)
4. **Test Thoroughly**: Verify policies with different user roles
5. **Document Policies**: Keep policy documentation updated
6. **Monitor Performance**: Complex policies can impact query performance

## Security Considerations

1. **JWT Validation**: Always validate JWT signatures (Jarvis does this automatically)
2. **Claims Sanitization**: Jarvis sanitizes claim names for PostgreSQL compatibility
3. **Session Isolation**: Each request sets its own session variables
4. **Policy Bypass**: Only superusers can bypass RLS (not application users)

## Troubleshooting

### "Permission Denied" Errors
- Check if RLS is enabled on the table
- Verify JWT claims are being set correctly
- Ensure appropriate policies exist

### No Data Returned
- Check if policies are too restrictive
- Verify JWT contains expected claims
- Test with `USING (true)` policy to isolate issue

### Performance Issues
- Simplify complex policy conditions
- Add appropriate indexes for policy columns
- Consider materialized views for complex access patterns

## Related Documentation

- [PgClient API Reference](../api-reference/pgclient-api.md)
- [Authentication Guide](./authentication-guide.md)
- [Security Best Practices](./security-best-practices.md)