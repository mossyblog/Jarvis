# Authentication Troubleshooting Guide

This guide helps you diagnose and resolve authentication issues in Jarvis applications.

## Quick Diagnosis

### 🔴 Authentication Returns Empty Token

**Symptoms:**
- `AuthHandler.Authenticate()` returns `AuthToken` with empty `AccessToken`
- No exceptions thrown

**Quick Check:**
```csharp
// 1. Check if account exists and is active
var query = dataContext.Query()
    .With<Account>(a => a.Email == "user@example.com");
var accounts = await query.ToEntityComponents();

if (!accounts.Any()) 
{
    Console.WriteLine("Account not found");
}
else 
{
    var account = accounts.First().Value.Get<Account>();
    Console.WriteLine($"Account active: {account.IsActive}");
}

// 2. Test password directly
var isValid = BCrypt.Net.BCrypt.Verify("password", account.PasswordHash);
Console.WriteLine($"Password valid: {isValid}");
```

**Solutions:**
1. Verify account exists and is active
2. Check password is correct
3. Ensure account isn't locked
4. Enable debug logging for AuthHandler

[Full Empty Token Troubleshooting →](authentication-common-issues.md#empty-token)

### 🔴 Handler Not Found Exception

**Error:**
```
InvalidOperationException: Handler type 'AccountHandler' not registered
```

**Quick Fix:**
```csharp
// In Program.cs - register BOTH ways
services.AddScoped<IComponentHandler, AccountHandler>();
services.AddScoped<AccountHandler>(); // Required for DataContext.For<T>()
```

[Full Handler Registration Guide →](authentication-common-issues.md#handler-registration)

### 🔴 Authentication Takes Too Long

**Symptoms:**
- Login takes >5 seconds
- Timeouts on authentication

**Quick Check:**
```csharp
var stopwatch = Stopwatch.StartNew();
var result = await authHandler.Authenticate(credentials);
Console.WriteLine($"Auth took: {stopwatch.ElapsedMilliseconds}ms");
```

**Solutions:**
1. Reduce BCrypt work factor in development
2. Check database indexes
3. Review timing protection settings

[Full Performance Guide →](authentication-performance.md)

## Common Issues by Category

### Registration Issues
- [Email Already Exists](authentication-common-issues.md#email-exists)
- [Password Validation Failures](authentication-common-issues.md#password-validation)
- [Account Not Activating](authentication-common-issues.md#activation-issues)

### Login Issues
- [Wrong Password But Sure It's Right](authentication-common-issues.md#password-mismatch)
- [Account Locked Out](authentication-common-issues.md#account-lockout)
- [Constant Authentication Failures](authentication-common-issues.md#auth-failures)

### Token Issues
- [Access Token Expired](authentication-token-issues.md#token-expired)
- [Refresh Token Not Working](authentication-token-issues.md#refresh-failure)
- [JWT Validation Errors](authentication-token-issues.md#jwt-validation)

### Session Issues
- [Session Limit Exceeded](authentication-session-issues.md#session-limit)
- [Session Hijacking Detection](authentication-session-issues.md#session-hijacking)
- [Sessions Not Persisting](authentication-session-issues.md#session-persistence)

### Performance Issues
- [Slow Authentication](authentication-performance.md#slow-auth)
- [Database Bottlenecks](authentication-performance.md#database-issues)
- [Memory Leaks](authentication-performance.md#memory-issues)

## Diagnostic Tools

### Authentication Debug Service

Use this service to diagnose authentication issues:

```csharp
public class AuthDebugService
{
    public async Task<AuthDiagnostics> DiagnoseAuthentication(
        string email, string password)
    {
        var diag = new AuthDiagnostics();
        
        // Check account exists
        var account = await FindAccount(email);
        diag.AccountExists = account != null;
        
        if (account != null)
        {
            diag.AccountActive = account.IsActive;
            diag.PasswordValid = BCrypt.Verify(password, account.PasswordHash);
            diag.IsLocked = await CheckLockout(email);
        }
        
        return diag;
    }
}
```

### Enable Debug Logging

```json
{
  "Logging": {
    "LogLevel": {
      "core.jarvis.api.Handlers.AuthHandler": "Debug",
      "core.jarvis.api.Handlers.AccountHandler": "Debug"
    }
  }
}
```

### Database Queries

Check authentication data:

```sql
-- Find account by email
SELECT * FROM account_component 
WHERE email = 'user@example.com';

-- Check active sessions
SELECT * FROM auth_token_component 
WHERE owner_entity_id = 'user-entity-id'
  AND is_revoked = false
  AND refresh_expires_at > NOW();

-- Recent auth failures
SELECT * FROM security_audit_event
WHERE event_type = 'LOGIN_FAILED'
  AND timestamp > NOW() - INTERVAL '1 hour'
ORDER BY timestamp DESC;
```

## Quick Solutions

### Reset User Password

```csharp
var accountHandler = dataContext.For<AccountHandler>(userEntityId);
var account = await accountHandler.GetOrDefault();

var newPasswordHash = BCrypt.Net.BCrypt.HashPassword("NewPassword123!");
var updated = account with { PasswordHash = newPasswordHash };
await dataContext.Commit(updated);
```

### Unlock Account

```csharp
var securityHandler = dataContext.For<SecurityProfileHandler>(userEntityId);
await securityHandler.ClearFailedAttempts();
await securityHandler.Unlock();
```

### Clear All Sessions

```csharp
var sessions = await dataContext.Query()
    .With<AuthToken>(t => t.OwnerEntityId == userEntityId)
    .ToEntityIds();

foreach (var sessionId in sessions)
{
    var handler = dataContext.For<AuthTokenHandler>(sessionId);
    await handler.Remove();
}
```

## Getting Help

### 1. Check Error Logs

Look for detailed error messages:
```bash
# Azure Functions logs
func host start --verbose

# Application Insights
Search for: traces | where message contains "auth"
```

### 2. Enable Verbose Logging

```csharp
services.RegisterJarvis(LogLevel.Debug, Configuration);
```

### 3. Collect Diagnostics

When reporting issues, include:
- Jarvis version
- Authentication configuration (without secrets)
- Error messages and stack traces
- Steps to reproduce

### 4. Community Support

- [GitHub Issues](https://github.com/jarvis/issues) - Bug reports
- [Discussions](https://github.com/jarvis/discussions) - Questions
- [Stack Overflow](https://stackoverflow.com/questions/tagged/jarvis-framework) - Community Q&A

## Related Guides

- [Common Issues Reference](authentication-common-issues.md) - Detailed issue resolution
- [Performance Troubleshooting](authentication-performance.md) - Performance optimization
- [Security Troubleshooting](authentication-security-issues.md) - Security concerns
- [Testing Guide](/docs/guides/authentication/testing.md) - Testing strategies

---

**Emergency?** For production issues, check the [Emergency Response Guide](authentication-emergency.md)