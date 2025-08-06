# Authentication Troubleshooting Guide

This guide helps diagnose and resolve common authentication issues in Jarvis applications.

## Table of Contents

1. [Common Issues](#common-issues)
2. [Registration Problems](#registration-problems)
3. [Authentication Failures](#authentication-failures)
4. [Token Issues](#token-issues)
5. [Session Problems](#session-problems)
6. [Performance Issues](#performance-issues)
7. [Security Concerns](#security-concerns)
8. [Debugging Tools](#debugging-tools)

## Common Issues

### Authentication Returns Empty Token

**Symptoms:**
- `AuthHandler.Authenticate()` returns `AuthToken` with empty `AccessToken`
- No error exceptions thrown

**Possible Causes:**
1. **Wrong Password**
   ```csharp
   // Check password verification
   var isValid = BCrypt.Net.BCrypt.Verify("user-input", account.PasswordHash);
   ```

2. **Account Not Active**
   ```csharp
   var account = await accountHandler.GetOrDefault();
   if (!account.IsActive) 
   {
       // Account needs activation
   }
   ```

3. **Account Doesn't Exist**
   ```csharp
   var query = dataContext.Query().With<Account>(a => a.Email == email);
   var accounts = await query.ToEntityComponents();
   if (!accounts.Any()) 
   {
       // Email not found
   }
   ```

**Solution Steps:**
1. Verify the account exists and is active
2. Test password hashing/verification separately
3. Check authentication audit logs
4. Enable debug logging for `AuthHandler`

### Handler Not Found Exception

**Symptoms:**
```
InvalidOperationException: Handler type 'AccountHandler' not registered
```

**Cause:**
Missing or incorrect dependency injection registration.

**Solution:**
Ensure both interface and concrete handler registrations:

```csharp
// In Program.cs or Startup.cs
services.AddScoped<IComponentHandler, AccountHandler>();
services.AddScoped<AccountHandler>(); // Required for DataContext.For<T>()

services.AddScoped<IComponentHandler, AuthHandler>();
services.AddScoped<AuthHandler>();
```

### Component Not Saving

**Symptoms:**
- `DataContext.Commit()` doesn't throw but component isn't persisted
- `GetOrDefault()` returns null after commit

**Possible Causes:**
1. **Invalid OwnerEntityId**
   ```csharp
   // Check entity ID
   if (component.OwnerEntityId == Guid.Empty)
   {
       // Must set valid entity ID
       component = component with { OwnerEntityId = entityId };
   }
   ```

2. **Transaction Issues**
   ```csharp
   // Ensure proper transaction handling
   try 
   {
       await dataContext.Commit(component);
       // Success
   }
   catch (Exception ex)
   {
       Logger.LogError(ex, "Component save failed");
   }
   ```

**Solution:**
1. Verify `OwnerEntityId` is set correctly
2. Check database logs for constraint violations
3. Ensure proper transaction scope

## Registration Problems

### Email Already Exists Error

**Symptoms:**
```csharp
BusinessRuleException: EMAIL_EXISTS - An account with this email already exists
```

**Diagnosis:**
```csharp
// Check existing accounts
var existingQuery = dataContext.Query()
    .With<Account>(a => a.Email == "user@example.com");
var existing = await existingQuery.ToEntityComponents();

Console.WriteLine($"Found {existing.Count} accounts with this email");
```

**Solutions:**
1. **Check for case sensitivity issues:**
   ```csharp
   // Normalize email before checking
   var normalizedEmail = email.ToLowerInvariant().Trim();
   ```

2. **Clean up orphaned accounts (if appropriate):**
   ```csharp
   // For development only - remove test accounts
   foreach (var (entityId, components) in existing)
   {
       var accountHandler = dataContext.For<AccountHandler>(entityId);
       await accountHandler.Remove();
   }
   ```

### Password Validation Failures

**Symptoms:**
```csharp
ValidationException: Password does not meet policy requirements
```

**Diagnosis:**
```csharp
var passwordPolicy = serviceProvider.GetService<IPasswordPolicyService>();
var result = await passwordPolicy.ValidatePassword("userPassword");

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Password error: {error}");
    }
}
```

**Common Password Issues:**
- Too short (minimum 8 characters)
- Missing uppercase letters
- Missing numbers
- Missing special characters
- Common password (e.g., "password123")

**Solution:**
```csharp
// Example strong password generator for testing
public static string GenerateStrongPassword()
{
    var random = new Random();
    var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
    return new string(Enumerable.Repeat(chars, 12)
        .Select(s => s[random.Next(s.Length)]).ToArray()) + "A1!";
}
```

### Account Created But Not Activating

**Symptoms:**
- Registration succeeds
- Account remains inactive after calling `Activate()`

**Diagnosis:**
```csharp
var accountHandler = dataContext.For<AccountHandler>(entityId);
var account = await accountHandler.GetOrDefault();

Console.WriteLine($"Account exists: {account != null}");
Console.WriteLine($"Account active: {account?.IsActive}");
Console.WriteLine($"Account ID: {account?.Id}");
Console.WriteLine($"Entity ID: {entityId}");
```

**Common Issues:**
1. **Wrong Entity ID:** Using account component ID instead of entity ID
2. **Database Transaction:** Changes not committed
3. **Component Update:** Using wrong component instance

**Solution:**
```csharp
// Correct activation pattern
var entityId = Guid.NewGuid(); // User's entity ID
var accountHandler = dataContext.For<AccountHandler>(entityId);

// Register
var account = await accountHandler.Register(new Account { ... });

// Activate (use same entity ID)
var activatedAccount = await accountHandler.Activate();

// Verify
activatedAccount.IsActive.ShouldBeTrue();
```

## Authentication Failures

### Authentication Takes Too Long

**Symptoms:**
- Authentication requests timeout
- Slow response times (>5 seconds)

**Diagnosis:**
```csharp
var stopwatch = Stopwatch.StartNew();
var authToken = await authHandler.Authenticate(credentials);
stopwatch.Stop();
Console.WriteLine($"Authentication took: {stopwatch.ElapsedMilliseconds}ms");
```

**Possible Causes:**
1. **High BCrypt Cost Factor:**
   ```json
   // In appsettings.json - reduce for development
   {
     "Security": {
       "BCryptWorkFactor": 4  // Lower for testing (production: 12+)
     }
   }
   ```

2. **Database Performance:**
   ```sql
   -- Check for missing indexes
   EXPLAIN ANALYZE SELECT * FROM account_component WHERE email = 'user@example.com';
   
   -- Add index if needed
   CREATE INDEX idx_account_email ON account_component(email);
   ```

3. **Timing Protection:**
   ```csharp
   // Timing protection may add delays - check configuration
   var isTestEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Test";
   var minimumTime = isTestEnvironment ? 10 : 500; // Reduced for tests
   ```

### Constant Authentication Failures

**Symptoms:**
- All authentication attempts return empty tokens
- Even known good credentials fail

**Diagnosis:**
```csharp
// Test password verification directly
var account = await accountHandler.GetOrDefault();
var isPasswordValid = BCrypt.Net.BCrypt.Verify("testPassword", account.PasswordHash);
Console.WriteLine($"Password verification: {isPasswordValid}");

// Check account status
Console.WriteLine($"Account active: {account.IsActive}");

// Test token service
var tokenService = serviceProvider.GetService<ITokenService>();
var testToken = tokenService.AccessToken(Guid.NewGuid(), "test@example.com");
Console.WriteLine($"Token service working: {!string.IsNullOrEmpty(testToken)}");
```

**Common Issues:**
1. **JWT Configuration:**
   ```json
   {
     "Jwt": {
       "SecretKey": "must-be-at-least-256-bits-long-for-HMAC-SHA256",
       "Issuer": "YourApp",
       "Audience": "YourAppUsers"
     }
   }
   ```

2. **Service Registration:**
   ```csharp
   // Ensure all services are registered
   services.AddScoped<ITokenService, TokenService>();
   services.AddScoped<IPasswordService, PasswordService>();
   services.AddScoped<ISecurityAuditService, SecurityAuditService>();
   ```

### Brute Force Lockout Issues

**Symptoms:**
- Authentication fails with "Account locked" messages
- Can't authenticate even with correct credentials

**Diagnosis:**
```csharp
var bruteForceService = serviceProvider.GetService<IBruteForceProtectionService>();
var isBlocked = await bruteForceService.IsBlocked("192.168.1.1");
Console.WriteLine($"IP blocked: {isBlocked}");

// Check lockout status
var securityHandler = dataContext.For<SecurityProfileHandler>(userEntityId);
var profile = await securityHandler.GetOrDefault();
Console.WriteLine($"Failed attempts: {profile?.FailedLoginAttempts}");
Console.WriteLine($"Locked until: {profile?.LockedUntil}");
```

**Solutions:**
1. **Clear IP Block (Development):**
   ```csharp
   await bruteForceService.ClearAttempts("192.168.1.1");
   ```

2. **Reset User Lockout:**
   ```csharp
   var securityHandler = dataContext.For<SecurityProfileHandler>(userEntityId);
   await securityHandler.ClearFailedAttempts();
   ```

3. **Adjust Lockout Settings:**
   ```json
   {
     "Security": {
       "MaxFailedAttempts": 10,     // Increase for development
       "LockoutDurationMinutes": 1  // Reduce for development
     }
   }
   ```

## Token Issues

### Access Token Expired

**Symptoms:**
- API calls return 401 Unauthorized
- Token validation fails

**Diagnosis:**
```csharp
var tokenService = serviceProvider.GetService<ITokenService>();
var principal = await tokenService.ValidateAccessToken(accessToken);

if (principal == null)
{
    Console.WriteLine("Token is invalid or expired");
    
    // Check token claims manually
    var handler = new JwtSecurityTokenHandler();
    if (handler.CanReadToken(accessToken))
    {
        var jwt = handler.ReadJwtToken(accessToken);
        Console.WriteLine($"Token expires: {jwt.ValidTo}");
        Console.WriteLine($"Current time: {DateTime.UtcNow}");
    }
}
```

**Solutions:**
1. **Implement Automatic Token Refresh:**
   ```javascript
   // Frontend token refresh
   async function refreshTokenIfNeeded() {
     const token = localStorage.getItem('accessToken');
     const tokenData = JSON.parse(atob(token.split('.')[1]));
     const expiryTime = tokenData.exp * 1000;
     
     if (Date.now() >= expiryTime - 60000) { // Refresh 1 minute before expiry
       await refreshTokens();
     }
   }
   ```

2. **Extend Token Lifetime (Development):**
   ```json
   {
     "Jwt": {
       "AccessTokenExpirationMinutes": 60  // Longer for development
     }
   }
   ```

### Refresh Token Not Working

**Symptoms:**
- Refresh endpoint returns 401 Unauthorized
- `RefreshToken()` returns empty token

**Diagnosis:**
```csharp
// Check if refresh token exists in database
var tokenQuery = dataContext.Query()
    .With<AuthToken>(t => t.RefreshTokenHash == hashedRefreshToken)
    .With<AuthToken>(t => !t.IsRevoked);
    
var tokens = await tokenQuery.ToEntityComponents();
Console.WriteLine($"Found {tokens.Count} matching refresh tokens");

if (tokens.Any())
{
    var token = tokens.First().Value.Get<AuthToken>();
    Console.WriteLine($"Token expires: {token.RefreshExpiresAt}");
    Console.WriteLine($"Is revoked: {token.IsRevoked}");
}
```

**Common Issues:**
1. **Token Already Used:** Refresh tokens are single-use
2. **Token Expired:** Check `RefreshExpiresAt`
3. **Token Revoked:** Check `IsRevoked` flag
4. **Hash Mismatch:** Verify refresh token hashing

**Solutions:**
```csharp
// Debug refresh token hashing
var tokenService = serviceProvider.GetService<ITokenService>();
var providedHash = tokenService.HashRefreshToken(refreshTokenString);
Console.WriteLine($"Provided hash: {providedHash}");

// Check stored hash
var storedToken = await tokenHandler.GetOrDefault();
Console.WriteLine($"Stored hash: {storedToken?.RefreshTokenHash}");
```

### JWT Validation Errors

**Symptoms:**
```
SecurityTokenException: IDX10223: Lifetime validation failed
SecurityTokenException: IDX10214: Audience validation failed
```

**Diagnosis:**
```csharp
// Check JWT configuration
var validationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = configuration["Jwt:Issuer"],
    ValidateAudience = true,
    ValidAudience = configuration["Jwt:Audience"],
    ValidateLifetime = true,
    ClockSkew = TimeSpan.FromMinutes(5),
    IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]))
};

// Manually validate token
try
{
    var handler = new JwtSecurityTokenHandler();
    var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
    Console.WriteLine("Token validation successful");
}
catch (SecurityTokenException ex)
{
    Console.WriteLine($"Token validation failed: {ex.Message}");
}
```

**Solutions:**
1. **Check Configuration:**
   ```json
   {
     "Jwt": {
       "Issuer": "YourApp",        // Must match token issuer
       "Audience": "YourAppUsers", // Must match token audience
       "SecretKey": "your-secret-key"
     }
   }
   ```

2. **Time Synchronization:**
   ```csharp
   // Allow for clock skew
   validationParameters.ClockSkew = TimeSpan.FromMinutes(5);
   ```

## Session Problems

### Session Limit Exceeded

**Symptoms:**
- Can't create new sessions
- "Maximum sessions exceeded" errors

**Diagnosis:**
```csharp
var userSessions = await dataContext.Query()
    .With<AuthToken>(t => t.OwnerEntityId == userId)
    .With<AuthToken>(t => !t.IsRevoked)
    .With<AuthToken>(t => t.RefreshExpiresAt > DateTime.UtcNow)
    .ToEntityComponents();

Console.WriteLine($"Active sessions: {userSessions.Count}");
```

**Solutions:**
1. **Clean Up Old Sessions:**
   ```csharp
   var tokenHandler = dataContext.For<AuthTokenHandler>(userId);
   await tokenHandler.CleanupExpiredTokens();
   ```

2. **Increase Session Limit:**
   ```json
   {
     "Security": {
       "MaxActiveSessions": 10  // Increase from default 5
     }
   }
   ```

3. **Force Logout Oldest Session:**
   ```csharp
   var oldestSession = userSessions.Values
       .Select(c => c.Get<AuthToken>())
       .OrderBy(t => t.IssuedAt)
       .First();
       
   var tokenHandler = dataContext.For<AuthTokenHandler>(
       userSessions.First(kvp => kvp.Value.Get<AuthToken>().Id == oldestSession.Id).Key);
   await tokenHandler.RevokeToken();
   ```

### Session Hijacking Detection

**Symptoms:**
- "Session terminated due to suspicious activity" errors
- Valid users can't access their sessions

**Diagnosis:**
```csharp
var sessionSecurity = serviceProvider.GetService<ISessionSecurityService>();
var httpContext = CreateTestHttpContext("192.168.1.100", "Chrome/91.0");

var isValid = await sessionSecurity.ValidateSession(authToken, httpContext);
Console.WriteLine($"Session validation result: {isValid}");

// Check session binding
Console.WriteLine($"Token IP: {authToken.IpAddress}");
Console.WriteLine($"Current IP: {httpContext.Connection.RemoteIpAddress}");
Console.WriteLine($"Token User-Agent: {authToken.UserAgent}");
Console.WriteLine($"Current User-Agent: {httpContext.Request.Headers["User-Agent"]}");
```

**Solutions:**
1. **Adjust IP Validation (Development):**
   ```csharp
   // In SessionSecurityService, allow local IPs
   private bool IsLocalNetwork(string ipAddress)
   {
       return ipAddress.StartsWith("192.168.") || 
              ipAddress.StartsWith("10.") || 
              ipAddress == "127.0.0.1";
   }
   ```

2. **Relax User-Agent Matching:**
   ```csharp
   // Allow more flexible user agent matching
   var similarity = CalculateUserAgentSimilarity(stored, current);
   if (similarity < 0.6) // Reduced from 0.8
   {
       // Still suspicious
   }
   ```

## Performance Issues

### Slow Authentication

**Symptoms:**
- Authentication takes >2 seconds
- Database queries are slow

**Diagnosis:**
1. **Profile Authentication Method:**
   ```csharp
   var stopwatch = Stopwatch.StartNew();
   
   stopwatch.Restart();
   var account = await FindAccountByEmail(email);
   Console.WriteLine($"Account lookup: {stopwatch.ElapsedMilliseconds}ms");
   
   stopwatch.Restart();
   var isValid = BCrypt.Net.BCrypt.Verify(password, account.PasswordHash);
   Console.WriteLine($"Password verification: {stopwatch.ElapsedMilliseconds}ms");
   
   stopwatch.Restart();
   var tokens = tokenService.GenerateTokenPair(account.Id, account.Email);
   Console.WriteLine($"Token generation: {stopwatch.ElapsedMilliseconds}ms");
   ```

2. **Check Database Performance:**
   ```sql
   -- PostgreSQL query analysis
   EXPLAIN ANALYZE 
   SELECT * FROM account_component 
   WHERE email = 'user@example.com';
   
   -- Add index if needed
   CREATE INDEX CONCURRENTLY idx_account_component_email 
   ON account_component(email);
   ```

**Solutions:**
1. **Optimize BCrypt Work Factor:**
   ```csharp
   // Reduce for development
   services.Configure<PasswordServiceOptions>(options =>
   {
       options.WorkFactor = Environment.IsDevelopment() ? 4 : 12;
   });
   ```

2. **Add Database Indexes:**
   ```sql
   CREATE INDEX idx_account_component_email ON account_component(email);
   CREATE INDEX idx_auth_token_refresh_hash ON auth_token_component(refresh_token_hash);
   CREATE INDEX idx_auth_token_owner ON auth_token_component(owner_entity_id);
   ```

3. **Implement Caching:**
   ```csharp
   public class CachedPasswordService : IPasswordService
   {
       private readonly IMemoryCache _cache;
       private readonly IPasswordService _innerService;
       
       public bool VerifyPassword(string password, string hash)
       {
           var cacheKey = $"pwd_verify_{hash}_{password.GetHashCode()}";
           return _cache.GetOrCreate(cacheKey, entry =>
           {
               entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
               return _innerService.VerifyPassword(password, hash);
           });
       }
   }
   ```

### Memory Leaks

**Symptoms:**
- Memory usage grows over time
- OutOfMemoryException after extended use

**Diagnosis:**
```csharp
// Monitor memory usage
public class MemoryMonitoringService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var totalMemory = GC.GetTotalMemory(false);
            var workingSet = Environment.WorkingSet;
            
            Console.WriteLine($"GC Memory: {totalMemory / 1024 / 1024} MB");
            Console.WriteLine($"Working Set: {workingSet / 1024 / 1024} MB");
            
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

**Solutions:**
1. **Implement Proper Disposal:**
   ```csharp
   public class AuthHandler : ComponentHandler<Account>, IDisposable
   {
       private readonly IServiceScope _scope;
       
       public void Dispose()
       {
           _scope?.Dispose();
       }
   }
   ```

2. **Use Memory-Efficient Collections:**
   ```csharp
   // Instead of keeping all tokens in memory
   private readonly ConcurrentDictionary<string, AuthToken> _tokens = new();
   
   // Use time-based eviction
   private readonly MemoryCache _tokenCache = new(new MemoryCacheOptions
   {
       SizeLimit = 1000,
       CompactionPercentage = 0.2
   });
   ```

## Security Concerns

### Suspicious Activity Alerts

**Symptoms:**
- High number of failed login attempts
- Multiple IPs for same user
- Unusual authentication patterns

**Investigation:**
```csharp
// Query security audit events
var recentEvents = await dataContext.Query()
    .With<SecurityAuditEvent>(e => e.Timestamp > DateTime.UtcNow.AddHours(-24))
    .With<SecurityAuditEvent>(e => e.EventType == "LOGIN_FAILED")
    .ToEntityComponents();

var failuresByIp = recentEvents.Values
    .Select(c => c.Get<SecurityAuditEvent>())
    .GroupBy(e => e.IpAddress)
    .OrderByDescending(g => g.Count())
    .Take(10);

foreach (var group in failuresByIp)
{
    Console.WriteLine($"IP {group.Key}: {group.Count()} failures");
}
```

**Response Actions:**
1. **Block Suspicious IPs:**
   ```csharp
   foreach (var suspiciousIp in highFailureIps)
   {
       await ipBlockingService.BlockIp(suspiciousIp, TimeSpan.FromHours(24));
   }
   ```

2. **Force Password Reset:**
   ```csharp
   var affectedUsers = GetUsersFromSuspiciousActivity();
   foreach (var userId in affectedUsers)
   {
       await ForcePasswordReset(userId);
       await RevokeAllSessions(userId);
   }
   ```

### Token Theft Detection

**Symptoms:**
- Tokens used from multiple IPs simultaneously
- Unusual usage patterns

**Investigation:**
```csharp
// Check token usage patterns
var tokenUsage = await dataContext.Query()
    .With<SecurityAuditEvent>(e => e.EventType == "TOKEN_USED")
    .With<SecurityAuditEvent>(e => e.Timestamp > DateTime.UtcNow.AddHours(-1))
    .ToEntityComponents();

var multiIpTokens = tokenUsage.Values
    .Select(c => c.Get<SecurityAuditEvent>())
    .GroupBy(e => e.Details["token_id"])
    .Where(g => g.Select(e => e.IpAddress).Distinct().Count() > 1);

foreach (var tokenGroup in multiIpTokens)
{
    Console.WriteLine($"Token {tokenGroup.Key} used from {tokenGroup.Select(e => e.IpAddress).Distinct().Count()} IPs");
}
```

**Response:**
```csharp
// Revoke suspicious tokens
foreach (var suspiciousTokenId in multiIpTokens.Select(g => g.Key))
{
    var tokenHandler = dataContext.For<AuthTokenHandler>(Guid.Parse(suspiciousTokenId));
    await tokenHandler.RevokeToken();
}
```

## Debugging Tools

### Authentication Debug Service

```csharp
public class AuthenticationDebugService
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<AuthenticationDebugService> _logger;

    public async Task<AuthenticationDiagnostics> DiagnoseAuthenticationIssue(
        string email, string password)
    {
        var diagnostics = new AuthenticationDiagnostics();

        try
        {
            // 1. Check if account exists
            var accountQuery = _dataContext.Query()
                .With<Account>(a => a.Email == email);
            var accounts = await accountQuery.ToEntityComponents();
            
            diagnostics.AccountExists = accounts.Any();
            if (!diagnostics.AccountExists)
            {
                diagnostics.Issues.Add("Account not found");
                return diagnostics;
            }

            var account = accounts.First().Value.Get<Account>();
            diagnostics.AccountActive = account.IsActive;
            diagnostics.AccountCreated = account.CreatedAt;

            if (!account.IsActive)
            {
                diagnostics.Issues.Add("Account is not active");
            }

            // 2. Check password
            var passwordService = _dataContext.GetService<IPasswordService>();
            diagnostics.PasswordValid = passwordService.VerifyPassword(password, account.PasswordHash);
            
            if (!diagnostics.PasswordValid)
            {
                diagnostics.Issues.Add("Password verification failed");
            }

            // 3. Check for lockouts
            var bruteForceService = _dataContext.GetService<IBruteForceProtectionService>();
            diagnostics.IsBlocked = await bruteForceService.IsBlocked(email);
            
            if (diagnostics.IsBlocked)
            {
                diagnostics.Issues.Add("Account is blocked due to too many failed attempts");
            }

            // 4. Check token service
            var tokenService = _dataContext.GetService<ITokenService>();
            var testToken = tokenService.AccessToken(Guid.NewGuid(), email);
            diagnostics.TokenServiceWorking = !string.IsNullOrEmpty(testToken);
            
            if (!diagnostics.TokenServiceWorking)
            {
                diagnostics.Issues.Add("Token service is not working");
            }

            // 5. Check recent audit events
            var recentEvents = await _dataContext.Query()
                .With<SecurityAuditEvent>(e => e.Email == email)
                .With<SecurityAuditEvent>(e => e.Timestamp > DateTime.UtcNow.AddHours(-1))
                .ToEntityComponents();

            diagnostics.RecentFailures = recentEvents.Values
                .Select(c => c.Get<SecurityAuditEvent>())
                .Count(e => e.EventType == "LOGIN_FAILED");

            diagnostics.Summary = diagnostics.Issues.Any() 
                ? $"Found {diagnostics.Issues.Count} issues" 
                : "No issues detected";

        }
        catch (Exception ex)
        {
            diagnostics.Issues.Add($"Diagnostic error: {ex.Message}");
        }

        return diagnostics;
    }
}

public class AuthenticationDiagnostics
{
    public bool AccountExists { get; set; }
    public bool AccountActive { get; set; }
    public DateTime AccountCreated { get; set; }
    public bool PasswordValid { get; set; }
    public bool IsBlocked { get; set; }
    public bool TokenServiceWorking { get; set; }
    public int RecentFailures { get; set; }
    public List<string> Issues { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}
```

### Debug API Endpoints

```csharp
// For development/testing only
[Route("api/debug/auth")]
[ApiController]
public class AuthDebugController : ControllerBase
{
    private readonly AuthenticationDebugService _debugService;

    [HttpPost("diagnose")]
    public async Task<IActionResult> DiagnoseAuthentication([FromBody] DiagnoseRequest request)
    {
        if (!Environment.IsDevelopment())
        {
            return NotFound();
        }

        var diagnostics = await _debugService.DiagnoseAuthenticationIssue(
            request.Email, request.Password);
        
        return Ok(diagnostics);
    }

    [HttpPost("clear-lockout")]
    public async Task<IActionResult> ClearLockout([FromBody] ClearLockoutRequest request)
    {
        if (!Environment.IsDevelopment())
        {
            return NotFound();
        }

        var bruteForceService = HttpContext.RequestServices.GetService<IBruteForceProtectionService>();
        await bruteForceService.ClearAttempts(request.Identifier);
        
        return Ok(new { message = "Lockout cleared" });
    }
}
```

### Logging Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "core.jarvis.api.Handlers.AuthHandler": "Debug",
      "core.jarvis.api.Handlers.AccountHandler": "Debug",
      "core.jarvis.api.Services.TokenService": "Debug",
      "core.jarvis.api.Services.SecurityAuditService": "Information"
    }
  }
}
```

### Monitoring Dashboard Queries

```sql
-- Recent authentication failures
SELECT 
    DATE_TRUNC('hour', timestamp) as hour,
    COUNT(*) as failure_count,
    COUNT(DISTINCT ip_address) as unique_ips
FROM security_audit_event 
WHERE event_type = 'LOGIN_FAILED' 
  AND timestamp > NOW() - INTERVAL '24 hours'
GROUP BY hour
ORDER BY hour;

-- Top failing IPs
SELECT 
    ip_address,
    COUNT(*) as failure_count,
    MIN(timestamp) as first_failure,
    MAX(timestamp) as last_failure
FROM security_audit_event 
WHERE event_type = 'LOGIN_FAILED' 
  AND timestamp > NOW() - INTERVAL '1 hour'
GROUP BY ip_address
ORDER BY failure_count DESC
LIMIT 10;

-- Active sessions by user
SELECT 
    owner_entity_id,
    COUNT(*) as active_sessions,
    MIN(issued_at) as oldest_session,
    MAX(issued_at) as newest_session
FROM auth_token_component 
WHERE is_revoked = false 
  AND refresh_expires_at > NOW()
GROUP BY owner_entity_id
HAVING COUNT(*) > 1
ORDER BY active_sessions DESC;
```

## Getting Help

### Support Channels

1. **Documentation:**
   - [Getting Started Guide](authentication-getting-started.md)
   - [API Reference](authentication-api-reference.md)
   - [Security Guide](authentication-security.md)

2. **Community:**
   - GitHub Issues for bug reports
   - GitHub Discussions for questions
   - Stack Overflow with `jarvis-framework` tag

3. **Enterprise Support:**
   - Contact enterprise support team
   - Dedicated Slack/Teams channels
   - Priority bug fixing

### Information to Include in Bug Reports

```
**Environment:**
- Jarvis Framework Version: 
- .NET Version: 
- Database: PostgreSQL version
- Operating System: 
- Environment: Development/Staging/Production

**Issue Description:**
[Detailed description of the problem]

**Steps to Reproduce:**
1. 
2. 
3. 

**Expected Behavior:**
[What should happen]

**Actual Behavior:**
[What actually happens]

**Logs:**
[Include relevant log entries]

**Code Samples:**
[Minimal reproducible code example]
```

---

**Previous**: [Testing Guide](authentication-testing.md) - Comprehensive testing strategies for authentication systems

**Index**: [Authentication Documentation Hub](README.md) - Complete authentication documentation overview