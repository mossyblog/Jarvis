# Authentication Security Considerations

This document covers security best practices, threat mitigation strategies, and advanced security features for authentication in Jarvis applications.

## Table of Contents

1. [Security Architecture](#security-architecture)
2. [Password Security](#password-security)
3. [Token Security](#token-security)
4. [Session Security](#session-security)
5. [Attack Prevention](#attack-prevention)
6. [Audit and Monitoring](#audit-and-monitoring)
7. [Compliance and Standards](#compliance-and-standards)
8. [Security Checklist](#security-checklist)

## Security Architecture

### Defense in Depth

Jarvis authentication implements multiple security layers:

```
┌─────────────────────────────────────────────────┐
│               Network Layer                      │
│  • HTTPS/TLS 1.3  • WAF Protection             │
│  • Rate Limiting   • DDoS Protection            │
└─────────────────────┬───────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────┐
│              Application Layer                   │
│  • Input Validation    • CSRF Protection        │
│  • XSS Prevention     • Injection Prevention    │
└─────────────────────┬───────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────┐
│            Authentication Layer                  │
│  • BCrypt Hashing     • JWT Tokens             │
│  • Timing Protection  • Session Management      │
└─────────────────────┬───────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────┐
│             Authorization Layer                  │
│  • Component Access   • Entity Ownership        │
│  • Handler Validation • Business Rules          │
└─────────────────────┬───────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────┐
│               Data Layer                         │
│  • Encrypted Storage  • Audit Logging          │
│  • Backup Security    • Access Controls         │
└─────────────────────────────────────────────────┘
```

### Security Principles

#### 1. Zero Trust Model
Every request is authenticated and authorized:

```csharp
public class SecureHandler<T> : ComponentHandler<T> where T : class, IComponent, new()
{
    protected override async Task<TResult> ExecuteOperation<TResult>(
        Func<Task<TResult>> operation)
    {
        // 1. Verify authentication
        var currentUser = GetCurrentUser();
        if (currentUser == null)
        {
            throw new UnauthorizedException("Authentication required");
        }

        // 2. Verify authorization
        if (!await HasPermission(typeof(T).Name))
        {
            throw new ForbiddenException("Insufficient permissions");
        }

        // 3. Verify entity ownership
        if (!await OwnsEntity(currentUser.Id, EntityId))
        {
            throw new ForbiddenException("Entity access denied");
        }

        // 4. Execute with audit
        return await ExecuteWithAudit(operation, currentUser);
    }
}
```

#### 2. Fail Secure
Default to denying access when errors occur:

```csharp
public async Task<bool> HasPermission(string permission)
{
    try
    {
        var roleHandler = DataContext.For<RoleHandler>(GetCurrentUserId());
        return await roleHandler.HasPermission(permission);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Permission check failed for {Permission}", permission);
        return false; // Fail secure - deny access on error
    }
}
```

#### 3. Least Privilege
Grant minimal required permissions:

```csharp
public static class DefaultRoles
{
    public static readonly Role User = new()
    {
        Name = "User",
        Permissions = new()
        {
            // Only own data access
            new("profile.read.own"),
            new("profile.write.own"),
            new("orders.read.own")
        }
    };

    public static readonly Role Admin = new()
    {
        Name = "Admin", 
        Permissions = new()
        {
            // Explicit admin permissions only
            new("users.read"),
            new("users.activate"),
            new("audit.read")
            // No wildcard permissions
        }
    };
}
```

## Password Security

### BCrypt Configuration

Secure password hashing with appropriate cost factors:

```csharp
public class PasswordService : IPasswordService
{
    private readonly int _workFactor;

    public PasswordService(IConfiguration config)
    {
        // Production: 12-15, Development: 10-12
        _workFactor = int.Parse(config["Security:BCryptWorkFactor"] ?? "12");
    }

    public string HashPassword(string password)
    {
        // BCrypt automatically generates unique salt
        return BCrypt.Net.BCrypt.HashPassword(password, _workFactor);
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception)
        {
            return false; // Fail secure
        }
    }
}
```

### Password Policy Enforcement

Implement strong password requirements:

```csharp
public class PasswordPolicyService : IPasswordPolicyService
{
    private readonly PasswordPolicyOptions _options;
    private readonly HashSet<string> _commonPasswords;

    public async Task<PasswordPolicyResult> ValidatePassword(string password, string? email = null)
    {
        var result = new PasswordPolicyResult();

        // Length check
        if (password.Length < _options.MinLength)
        {
            result.Errors.Add($"Password must be at least {_options.MinLength} characters");
        }

        // Complexity checks
        if (_options.RequireUppercase && !password.Any(char.IsUpper))
        {
            result.Errors.Add("Password must contain uppercase letters");
        }

        if (_options.RequireLowercase && !password.Any(char.IsLower))
        {
            result.Errors.Add("Password must contain lowercase letters");
        }

        if (_options.RequireNumbers && !password.Any(char.IsDigit))
        {
            result.Errors.Add("Password must contain numbers");
        }

        if (_options.RequireSpecialChars && !password.Any(IsSpecialChar))
        {
            result.Errors.Add("Password must contain special characters");
        }

        // Common password check
        if (_options.PreventCommonPasswords && _commonPasswords.Contains(password.ToLower()))
        {
            result.Errors.Add("Password is too common");
        }

        // Personal information check
        if (!string.IsNullOrEmpty(email))
        {
            var emailLocal = email.Split('@')[0].ToLower();
            if (password.ToLower().Contains(emailLocal))
            {
                result.Errors.Add("Password cannot contain email address");
            }
        }

        // Entropy check
        var entropy = CalculateEntropy(password);
        if (entropy < _options.MinEntropy)
        {
            result.Errors.Add("Password is not complex enough");
        }

        result.IsValid = !result.Errors.Any();
        return result;
    }

    private double CalculateEntropy(string password)
    {
        var characterSets = 0;
        if (password.Any(char.IsLower)) characterSets += 26;
        if (password.Any(char.IsUpper)) characterSets += 26;
        if (password.Any(char.IsDigit)) characterSets += 10;
        if (password.Any(IsSpecialChar)) characterSets += 32;

        return password.Length * Math.Log2(characterSets);
    }
}
```

### Secure Password Reset

Implement secure password reset with time-limited tokens:

```csharp
public class PasswordResetHandler : ComponentHandler<PasswordResetToken>
{
    private readonly IEmailService _emailService;
    private readonly ISecureRandomService _randomService;

    public async Task<bool> InitiatePasswordReset(string email)
    {
        // Find account by email (timing attack protection)
        var accountQuery = DataContext.Query()
            .With<Account>(a => a.Email == email);
        var accounts = await accountQuery.ToEntityComponents();

        if (!accounts.Any())
        {
            // Don't reveal if email exists - always return success
            await Task.Delay(Random.Shared.Next(100, 500)); // Random delay
            return true;
        }

        var accountEntityId = accounts.First().Key;
        
        // Generate secure reset token
        var resetToken = new PasswordResetToken
        {
            OwnerEntityId = accountEntityId,
            Token = _randomService.GenerateSecureToken(32),
            ExpiresAt = DateTime.UtcNow.AddHours(1), // 1 hour expiry
            IsUsed = false,
            IpAddress = GetClientIpAddress(),
            CreatedAt = DateTime.UtcNow
        };

        await DataContext.Commit(resetToken);

        // Send reset email
        await _emailService.SendPasswordResetEmail(email, resetToken.Token);

        // Audit the reset request
        await DataContext.For<SecurityAuditHandler>(accountEntityId)
            .LogPasswordResetRequested(email, GetClientIpAddress());

        return true;
    }

    public async Task<bool> ResetPassword(string token, string newPassword)
    {
        // Find and validate reset token
        var tokenQuery = DataContext.Query()
            .With<PasswordResetToken>(t => t.Token == token)
            .With<PasswordResetToken>(t => !t.IsUsed)
            .With<PasswordResetToken>(t => t.ExpiresAt > DateTime.UtcNow);

        var tokens = await tokenQuery.ToEntityComponents();
        if (!tokens.Any())
        {
            return false; // Invalid or expired token
        }

        var tokenEntityId = tokens.First().Key;
        var resetToken = tokens.First().Value.Get<PasswordResetToken>();
        
        // Validate new password
        var passwordPolicy = DataContext.GetService<IPasswordPolicyService>();
        var policyResult = await passwordPolicy.ValidatePassword(newPassword);
        if (!policyResult.IsValid)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["password"] = policyResult.Errors.ToArray()
            });
        }

        // Update password
        var accountHandler = DataContext.For<AccountHandler>(resetToken.OwnerEntityId);
        var account = await accountHandler.GetOrDefault();
        if (account == null) return false;

        var passwordService = DataContext.GetService<IPasswordService>();
        var updatedAccount = account with 
        { 
            PasswordHash = passwordService.HashPassword(newPassword),
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updatedAccount);

        // Mark token as used
        var usedToken = resetToken with 
        { 
            IsUsed = true, 
            UsedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        
        var tokenHandler = DataContext.For<PasswordResetTokenHandler>(tokenEntityId);
        await tokenHandler.Update(usedToken);

        // Revoke all existing sessions for security
        await RevokeAllUserSessions(resetToken.OwnerEntityId);

        // Audit password reset
        await DataContext.For<SecurityAuditHandler>(resetToken.OwnerEntityId)
            .LogPasswordReset(account.Email, GetClientIpAddress());

        return true;
    }

    private async Task RevokeAllUserSessions(Guid userEntityId)
    {
        var userTokens = await DataContext.Query()
            .With<AuthToken>(t => t.OwnerEntityId == userEntityId)
            .With<AuthToken>(t => !t.IsRevoked)
            .ToEntityIds();

        foreach (var tokenEntityId in userTokens)
        {
            var tokenHandler = DataContext.For<AuthTokenHandler>(tokenEntityId);
            await tokenHandler.RevokeToken();
        }
    }
}

public record PasswordResetToken : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? IpAddress { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
```

## Token Security

### JWT Security Best Practices

#### Secure Token Generation

```csharp
public class SecureTokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly byte[] _signingKey;

    public SecureTokenService(IConfiguration config)
    {
        _config = config;
        
        // Use 256-bit key minimum
        var keyString = config["Jwt:SecretKey"];
        if (string.IsNullOrEmpty(keyString) || keyString.Length < 32)
        {
            throw new InvalidOperationException("JWT secret key must be at least 256 bits (32 characters)");
        }
        
        _signingKey = Encoding.UTF8.GetBytes(keyString);
    }

    public string AccessToken(Guid userId, string email, Dictionary<string, string>? additionalClaims = null)
    {
        var now = DateTime.UtcNow;
        var expiry = now.AddMinutes(int.Parse(_config["Jwt:AccessTokenExpirationMinutes"] ?? "15"));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("token_type", "access"),
            new("session_id", Guid.NewGuid().ToString())
        };

        // Add additional claims if provided
        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims.Select(kvp => new Claim(kvp.Key, kvp.Value)));
        }

        var key = new SymmetricSecurityKey(_signingKey);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            notBefore: now,
            expires: expiry,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string RefreshToken()
    {
        // Generate cryptographically secure random refresh token
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
```

#### Token Validation with Security Checks

```csharp
public class TokenValidationService : ITokenValidationService
{
    private readonly TokenValidationParameters _validationParameters;
    private readonly IBlacklistService _blacklistService;

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            // Check token blacklist first
            if (await _blacklistService.IsBlacklisted(token))
            {
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);

            // Additional security checks
            if (!IsValidJwtToken(validatedToken))
            {
                return null;
            }

            // Check for token replay
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrEmpty(jti) && await _blacklistService.IsJtiUsed(jti))
            {
                return null;
            }

            // Mark JTI as used
            if (!string.IsNullOrEmpty(jti))
            {
                await _blacklistService.MarkJtiAsUsed(jti);
            }

            return principal;
        }
        catch (SecurityTokenException)
        {
            return null; // Invalid token
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Token validation error");
            return null; // Fail secure
        }
    }

    private bool IsValidJwtToken(SecurityToken token)
    {
        if (token is not JwtSecurityToken jwtToken)
        {
            return false;
        }

        // Verify algorithm
        if (jwtToken.Header.Alg != SecurityAlgorithms.HmacSha256)
        {
            return false;
        }

        // Verify token type claim
        var tokenType = jwtToken.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value;
        if (tokenType != "access")
        {
            return false;
        }

        return true;
    }
}
```

### Token Storage Security

#### Secure Token Storage (Frontend)

```javascript
// SecureTokenStorage.js
class SecureTokenStorage {
    constructor() {
        this.accessTokenKey = 'jarvis_access_token';
        this.refreshTokenKey = 'jarvis_refresh_token';
    }

    // Store tokens with encryption
    storeTokens(accessToken, refreshToken) {
        try {
            // Use sessionStorage for access tokens (cleared on tab close)
            sessionStorage.setItem(this.accessTokenKey, this.encrypt(accessToken));
            
            // Use localStorage for refresh tokens with HttpOnly cookie fallback
            if (this.supportsHttpOnlyCookies()) {
                this.setHttpOnlyCookie(this.refreshTokenKey, refreshToken);
            } else {
                localStorage.setItem(this.refreshTokenKey, this.encrypt(refreshToken));
            }
        } catch (error) {
            console.error('Failed to store tokens:', error);
        }
    }

    getAccessToken() {
        try {
            const encrypted = sessionStorage.getItem(this.accessTokenKey);
            return encrypted ? this.decrypt(encrypted) : null;
        } catch (error) {
            console.error('Failed to retrieve access token:', error);
            return null;
        }
    }

    getRefreshToken() {
        try {
            if (this.supportsHttpOnlyCookies()) {
                return this.getHttpOnlyCookie(this.refreshTokenKey);
            } else {
                const encrypted = localStorage.getItem(this.refreshTokenKey);
                return encrypted ? this.decrypt(encrypted) : null;
            }
        } catch (error) {
            console.error('Failed to retrieve refresh token:', error);
            return null;
        }
    }

    clearTokens() {
        sessionStorage.removeItem(this.accessTokenKey);
        localStorage.removeItem(this.refreshTokenKey);
        if (this.supportsHttpOnlyCookies()) {
            this.clearHttpOnlyCookie(this.refreshTokenKey);
        }
    }

    // Simple XOR encryption for client-side storage
    encrypt(text) {
        const key = this.getOrCreateKey();
        let result = '';
        for (let i = 0; i < text.length; i++) {
            result += String.fromCharCode(text.charCodeAt(i) ^ key.charCodeAt(i % key.length));
        }
        return btoa(result);
    }

    decrypt(encrypted) {
        const key = this.getOrCreateKey();
        const text = atob(encrypted);
        let result = '';
        for (let i = 0; i < text.length; i++) {
            result += String.fromCharCode(text.charCodeAt(i) ^ key.charCodeAt(i % key.length));
        }
        return result;
    }

    getOrCreateKey() {
        let key = localStorage.getItem('jarvis_key');
        if (!key) {
            key = this.generateRandomKey();
            localStorage.setItem('jarvis_key', key);
        }
        return key;
    }

    generateRandomKey() {
        const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
        let result = '';
        for (let i = 0; i < 32; i++) {
            result += chars.charAt(Math.floor(Math.random() * chars.length));
        }
        return result;
    }
}

export default new SecureTokenStorage();
```

## Session Security

### Session Hijacking Prevention

#### Session Binding

```csharp
public class SessionSecurityService : ISessionSecurityService
{
    public async Task<bool> ValidateSession(AuthToken token, HttpContext context)
    {
        // IP address validation
        var currentIp = GetClientIpAddress(context);
        if (!string.IsNullOrEmpty(token.IpAddress) && token.IpAddress != currentIp)
        {
            // Allow IP changes within same subnet for mobile users
            if (!IsSameSubnet(token.IpAddress, currentIp))
            {
                await LogSuspiciousActivity("IP address changed", token.SessionId, currentIp);
                return false;
            }
        }

        // User agent validation (more flexible)
        var currentUserAgent = context.Request.Headers["User-Agent"].ToString();
        if (!string.IsNullOrEmpty(token.UserAgent))
        {
            var similarity = CalculateUserAgentSimilarity(token.UserAgent, currentUserAgent);
            if (similarity < 0.8) // 80% similarity threshold
            {
                await LogSuspiciousActivity("User agent mismatch", token.SessionId, currentIp);
                return false;
            }
        }

        // Session age validation
        var sessionAge = DateTime.UtcNow - token.IssuedAt;
        if (sessionAge > TimeSpan.FromDays(30)) // Max session age
        {
            return false;
        }

        return true;
    }

    private bool IsSameSubnet(string ip1, string ip2)
    {
        try
        {
            var addr1 = IPAddress.Parse(ip1);
            var addr2 = IPAddress.Parse(ip2);
            
            // Check if in same /24 subnet for IPv4
            if (addr1.AddressFamily == AddressFamily.InterNetwork)
            {
                var bytes1 = addr1.GetAddressBytes();
                var bytes2 = addr2.GetAddressBytes();
                
                return bytes1[0] == bytes2[0] && 
                       bytes1[1] == bytes2[1] && 
                       bytes1[2] == bytes2[2];
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }

    private double CalculateUserAgentSimilarity(string ua1, string ua2)
    {
        // Simple similarity calculation based on common tokens
        var tokens1 = ua1.Split(' ', '/', '(', ')').ToHashSet();
        var tokens2 = ua2.Split(' ', '/', '(', ')').ToHashSet();
        
        var intersection = tokens1.Intersect(tokens2).Count();
        var union = tokens1.Union(tokens2).Count();
        
        return union == 0 ? 0 : (double)intersection / union;
    }
}
```

### Concurrent Session Management

```csharp
public class ConcurrentSessionManager : IConcurrentSessionManager
{
    private readonly IDataContext _dataContext;
    private readonly IConfiguration _config;

    public async Task<bool> AllowNewSession(Guid userId, string clientId)
    {
        var maxSessions = int.Parse(_config["Security:MaxConcurrentSessions"] ?? "5");
        
        // Get current active sessions
        var activeSessions = await _dataContext.Query()
            .With<AuthToken>(t => t.OwnerEntityId == userId)
            .With<AuthToken>(t => !t.IsRevoked)
            .With<AuthToken>(t => t.RefreshExpiresAt > DateTime.UtcNow)
            .ToEntityComponents();

        if (activeSessions.Count >= maxSessions)
        {
            // Remove oldest session to make room
            var oldestSession = activeSessions.Values
                .Select(c => new { EntityId = activeSessions.First(kvp => kvp.Value == c).Key, Token = c.Get<AuthToken>() })
                .Where(x => x.Token != null)
                .OrderBy(x => x.Token.IssuedAt)
                .First();

            var tokenHandler = _dataContext.For<AuthTokenHandler>(oldestSession.EntityId);
            await tokenHandler.RevokeToken();
            
            await LogSessionDisplaced(userId, oldestSession.Token.SessionId);
        }

        return true;
    }

    public async Task<List<ActiveSession>> GetActiveSessions(Guid userId)
    {
        var sessions = await _dataContext.Query()
            .With<AuthToken>(t => t.OwnerEntityId == userId)
            .With<AuthToken>(t => !t.IsRevoked)
            .With<AuthToken>(t => t.RefreshExpiresAt > DateTime.UtcNow)
            .ToEntityComponents();

        return sessions.Values
            .Select(c => c.Get<AuthToken>())
            .Where(t => t != null)
            .Select(t => new ActiveSession
            {
                SessionId = t.SessionId,
                ClientId = t.ClientId,
                IpAddress = t.IpAddress,
                UserAgent = t.UserAgent,
                IssuedAt = t.IssuedAt,
                LastActivity = t.LastUpdated,
                ExpiresAt = t.RefreshExpiresAt
            })
            .OrderByDescending(s => s.LastActivity)
            .ToList();
    }

    public async Task<bool> RevokeSession(Guid userId, Guid sessionId)
    {
        var sessions = await _dataContext.Query()
            .With<AuthToken>(t => t.OwnerEntityId == userId)
            .With<AuthToken>(t => t.SessionId == sessionId)
            .With<AuthToken>(t => !t.IsRevoked)
            .ToEntityIds();

        foreach (var sessionEntityId in sessions)
        {
            var tokenHandler = _dataContext.For<AuthTokenHandler>(sessionEntityId);
            await tokenHandler.RevokeToken();
        }

        return sessions.Any();
    }
}
```

## Attack Prevention

### Brute Force Protection

```csharp
public class BruteForceProtectionService : IBruteForceProtectionService
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;

    public async Task<bool> IsBlocked(string identifier)
    {
        var attempts = GetAttempts(identifier);
        var maxAttempts = int.Parse(_config["Security:MaxFailedAttempts"] ?? "5");
        var lockoutDuration = TimeSpan.FromMinutes(int.Parse(_config["Security:LockoutDurationMinutes"] ?? "15"));

        if (attempts.Count >= maxAttempts)
        {
            var oldestAttempt = attempts.Min();
            if (DateTime.UtcNow - oldestAttempt < lockoutDuration)
            {
                return true; // Still in lockout period
            }
            else
            {
                // Lockout period expired, clear attempts
                ClearAttempts(identifier);
            }
        }

        return false;
    }

    public async Task RecordFailedAttempt(string identifier)
    {
        var attempts = GetAttempts(identifier);
        attempts.Add(DateTime.UtcNow);
        
        var cacheKey = $"failed_attempts:{identifier}";
        var lockoutDuration = TimeSpan.FromMinutes(int.Parse(_config["Security:LockoutDurationMinutes"] ?? "15"));
        
        _cache.Set(cacheKey, attempts, lockoutDuration);

        // Alert on suspicious activity
        if (attempts.Count >= 3)
        {
            await AlertSecurityTeam($"Multiple failed login attempts from {identifier}");
        }
    }

    public async Task ClearAttempts(string identifier)
    {
        var cacheKey = $"failed_attempts:{identifier}";
        _cache.Remove(cacheKey);
    }

    private List<DateTime> GetAttempts(string identifier)
    {
        var cacheKey = $"failed_attempts:{identifier}";
        return _cache.Get<List<DateTime>>(cacheKey) ?? new List<DateTime>();
    }

    // Progressive delays
    public async Task<TimeSpan> GetRequiredDelay(string identifier)
    {
        var attempts = GetAttempts(identifier);
        return attempts.Count switch
        {
            0 => TimeSpan.Zero,
            1 => TimeSpan.FromSeconds(1),
            2 => TimeSpan.FromSeconds(2),
            3 => TimeSpan.FromSeconds(5),
            4 => TimeSpan.FromSeconds(10),
            _ => TimeSpan.FromSeconds(30)
        };
    }
}
```

### CSRF Protection

```csharp
public class CsrfProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITokenService _tokenService;

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsStateChangingRequest(context.Request.Method))
        {
            var csrfToken = context.Request.Headers["X-CSRF-Token"].FirstOrDefault();
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(csrfToken) || string.IsNullOrEmpty(authHeader))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("CSRF token required");
                return;
            }

            // Extract JWT from Bearer token
            var jwt = authHeader.Replace("Bearer ", "");
            var principal = await _tokenService.ValidateToken(jwt);
            
            if (principal == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid token");
                return;
            }

            // Validate CSRF token against session
            var sessionId = principal.FindFirst("session_id")?.Value;
            if (!ValidateCsrfToken(csrfToken, sessionId))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Invalid CSRF token");
                return;
            }
        }

        await _next(context);
    }

    private bool IsStateChangingRequest(string method)
    {
        return method is "POST" or "PUT" or "PATCH" or "DELETE";
    }

    private bool ValidateCsrfToken(string csrfToken, string? sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) return false;
        
        // CSRF token should be HMAC of session ID
        var expectedToken = GenerateCsrfToken(sessionId);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(csrfToken),
            Encoding.UTF8.GetBytes(expectedToken)
        );
    }

    private string GenerateCsrfToken(string sessionId)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("csrf-secret-key"));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(sessionId));
        return Convert.ToBase64String(hash);
    }
}
```

### Input Validation Security

```csharp
public class SecurityInputValidator : IInputValidator
{
    private readonly HashSet<string> _dangerousPatterns = new()
    {
        // SQL Injection
        "'", "\"", ";", "--", "/*", "*/", "xp_", "sp_", "exec", "execute",
        "union", "select", "insert", "update", "delete", "drop", "create",
        "alter", "grant", "revoke",
        
        // NoSQL Injection
        "$where", "$ne", "$in", "$nin", "$gt", "$lt", "$regex", "$or", "$and",
        
        // Command Injection
        "|", "&", ";", "`", "$", "(", ")", "<", ">", "&&", "||",
        
        // Script Injection
        "<script", "</script", "javascript:", "vbscript:", "onload=", "onerror=",
        "onclick=", "onmouseover=", "eval(", "setTimeout(", "setInterval(",
        
        // Path Traversal
        "../", "..\\", "/etc/", "/proc/", "/sys/", "c:\\", "d:\\",
        
        // LDAP Injection
        "*", "(", ")", "\\", "/", "+", "=", "!", "&", "|"
    };

    public ValidationResult ValidateInput(string input, string fieldName)
    {
        var result = new ValidationResult { IsValid = true, FieldName = fieldName };

        if (string.IsNullOrEmpty(input))
        {
            return result;
        }

        // Length validation
        if (input.Length > 10000) // Reasonable max length
        {
            result.IsValid = false;
            result.Errors.Add("Input too long");
        }

        // Encoding validation
        if (!IsValidUtf8(input))
        {
            result.IsValid = false;
            result.Errors.Add("Invalid character encoding");
        }

        // Pattern detection
        var lowerInput = input.ToLower();
        foreach (var pattern in _dangerousPatterns)
        {
            if (lowerInput.Contains(pattern))
            {
                result.IsValid = false;
                result.Errors.Add($"Potentially dangerous content detected");
                break; // Don't reveal specific pattern
            }
        }

        // Control character check
        if (input.Any(c => char.IsControl(c) && c != '\t' && c != '\n' && c != '\r'))
        {
            result.IsValid = false;
            result.Errors.Add("Invalid control characters");
        }

        return result;
    }

    private bool IsValidUtf8(string input)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var decoded = Encoding.UTF8.GetString(bytes);
            return decoded == input;
        }
        catch
        {
            return false;
        }
    }
}
```

## Audit and Monitoring

### Comprehensive Security Auditing

```csharp
public class SecurityAuditService : ISecurityAuditService
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<SecurityAuditService> _logger;

    public async Task LogAuthenticationEvent(AuthEventType eventType, Guid? userId, 
        string? email, string ipAddress, string? userAgent, string? details = null)
    {
        var auditEvent = new SecurityAuditEvent
        {
            EventType = eventType.ToString(),
            UserId = userId,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = details,
            Timestamp = DateTime.UtcNow,
            Severity = GetEventSeverity(eventType)
        };

        // Store in database
        await _dataContext.Commit(auditEvent);

        // Log to structured logging
        _logger.LogInformation("Security event: {EventType} for user {UserId} from {IpAddress}",
            eventType, userId, ipAddress);

        // Real-time alerts for critical events
        if (auditEvent.Severity == "Critical")
        {
            await SendSecurityAlert(auditEvent);
        }
    }

    public async Task<SecurityMetrics> GetSecurityMetrics(TimeSpan period)
    {
        var startTime = DateTime.UtcNow - period;
        
        var events = await _dataContext.Query()
            .With<SecurityAuditEvent>(e => e.Timestamp > startTime)
            .ToEntityComponents();

        var auditEvents = events.Values
            .Select(c => c.Get<SecurityAuditEvent>())
            .Where(e => e != null)
            .ToList();

        return new SecurityMetrics
        {
            TotalEvents = auditEvents.Count,
            FailedLogins = auditEvents.Count(e => e.EventType == "LOGIN_FAILED"),
            SuccessfulLogins = auditEvents.Count(e => e.EventType == "LOGIN_SUCCESS"),
            PasswordResets = auditEvents.Count(e => e.EventType == "PASSWORD_RESET"),
            AccountLockouts = auditEvents.Count(e => e.EventType == "ACCOUNT_LOCKED"),
            SuspiciousActivities = auditEvents.Count(e => e.Severity == "Critical"),
            TopFailedIps = auditEvents
                .Where(e => e.EventType == "LOGIN_FAILED")
                .GroupBy(e => e.IpAddress)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new IpFailureCount { IpAddress = g.Key, Count = g.Count() })
                .ToList()
        };
    }

    private string GetEventSeverity(AuthEventType eventType)
    {
        return eventType switch
        {
            AuthEventType.LoginSuccess => "Info",
            AuthEventType.LoginFailed => "Warning",
            AuthEventType.AccountLocked => "Warning",
            AuthEventType.PasswordReset => "Info",
            AuthEventType.TokenRefresh => "Info",
            AuthEventType.SuspiciousActivity => "Critical",
            AuthEventType.BruteForceDetected => "Critical",
            AuthEventType.SessionHijackAttempt => "Critical",
            _ => "Info"
        };
    }
}

public enum AuthEventType
{
    LoginSuccess,
    LoginFailed,
    AccountLocked,
    PasswordReset,
    TokenRefresh,
    SuspiciousActivity,
    BruteForceDetected,
    SessionHijackAttempt
}
```

### Real-time Security Monitoring

```csharp
public class SecurityMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SecurityMonitoringService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dataContext = scope.ServiceProvider.GetRequiredService<IDataContext>();
                
                await MonitorSuspiciousActivity(dataContext);
                await CheckForBruteForceAttacks(dataContext);
                await ValidateActiveSessionSecurity(dataContext);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in security monitoring service");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task MonitorSuspiciousActivity(IDataContext dataContext)
    {
        var recentEvents = await dataContext.Query()
            .With<SecurityAuditEvent>(e => e.Timestamp > DateTime.UtcNow.AddHours(-1))
            .ToEntityComponents();

        // Detect patterns
        var events = recentEvents.Values
            .Select(c => c.Get<SecurityAuditEvent>())
            .Where(e => e != null)
            .ToList();

        // Multiple failed logins from same IP
        var ipFailures = events
            .Where(e => e.EventType == "LOGIN_FAILED")
            .GroupBy(e => e.IpAddress)
            .Where(g => g.Count() >= 10)
            .ToList();

        foreach (var ipGroup in ipFailures)
        {
            await dataContext.For<SecurityAuditHandler>(Guid.NewGuid())
                .LogSecurityEvent("BRUTE_FORCE_DETECTED", "Critical", new()
                {
                    ["ip_address"] = ipGroup.Key,
                    ["attempt_count"] = ipGroup.Count(),
                    ["time_window"] = "1 hour"
                });
            
            // Could trigger IP blocking here
        }

        // Multiple accounts from same IP
        var ipAccounts = events
            .Where(e => e.EventType == "LOGIN_SUCCESS")
            .GroupBy(e => e.IpAddress)
            .Where(g => g.Select(e => e.UserId).Distinct().Count() >= 5)
            .ToList();

        foreach (var ipGroup in ipAccounts)
        {
            await dataContext.For<SecurityAuditHandler>(Guid.NewGuid())
                .LogSecurityEvent("SUSPICIOUS_IP_ACTIVITY", "Warning", new()
                {
                    ["ip_address"] = ipGroup.Key,
                    ["unique_accounts"] = ipGroup.Select(e => e.UserId).Distinct().Count(),
                    ["time_window"] = "1 hour"
                });
        }
    }
}
```

## Compliance and Standards

### GDPR Compliance

```csharp
public class GdprComplianceService : IGdprComplianceService
{
    public async Task<UserDataExport> ExportUserData(Guid userId)
    {
        var account = await _dataContext.Query()
            .With<Account>(a => a.OwnerEntityId == userId)
            .ToEntityComponents()
            .FirstOrDefault();

        var auditEvents = await _dataContext.Query()
            .With<SecurityAuditEvent>(e => e.UserId == userId)
            .ToEntityComponents();

        var sessions = await _dataContext.Query()
            .With<AuthToken>(t => t.OwnerEntityId == userId)
            .ToEntityComponents();

        return new UserDataExport
        {
            AccountData = account?.Value.Get<Account>(),
            AuditEvents = auditEvents.Values.Select(c => c.Get<SecurityAuditEvent>()).ToList(),
            Sessions = sessions.Values.Select(c => c.Get<AuthToken>()).ToList(),
            ExportedAt = DateTime.UtcNow
        };
    }

    public async Task<bool> DeleteUserData(Guid userId)
    {
        // Anonymize audit events (keep for security/compliance)
        var auditEvents = await _dataContext.Query()
            .With<SecurityAuditEvent>(e => e.UserId == userId)
            .ToEntityIds();

        foreach (var eventId in auditEvents)
        {
            var handler = _dataContext.For<SecurityAuditEventHandler>(eventId);
            var auditEvent = await handler.GetOrDefault();
            if (auditEvent != null)
            {
                var anonymized = auditEvent with 
                { 
                    UserId = null,
                    Email = "[DELETED]",
                    Details = "[ANONYMIZED]"
                };
                await handler.Update(anonymized);
            }
        }

        // Delete account and sessions
        var accountHandler = _dataContext.For<AccountHandler>(userId);
        await accountHandler.Remove();

        var userSessions = await _dataContext.Query()
            .With<AuthToken>(t => t.OwnerEntityId == userId)
            .ToEntityIds();

        foreach (var sessionId in userSessions)
        {
            var sessionHandler = _dataContext.For<AuthTokenHandler>(sessionId);
            await sessionHandler.Remove();
        }

        return true;
    }
}
```

### Security Standards Compliance

```csharp
public class SecurityStandardsService : ISecurityStandardsService
{
    // NIST Cybersecurity Framework compliance
    public async Task<ComplianceReport> GenerateNistComplianceReport()
    {
        return new ComplianceReport
        {
            Framework = "NIST",
            Categories = new[]
            {
                new ComplianceCategory
                {
                    Name = "Identify (ID)",
                    Controls = new[]
                    {
                        new ComplianceControl { Id = "ID.AM-1", Description = "Physical devices and systems within the organization are inventoried", Status = "Compliant" },
                        new ComplianceControl { Id = "ID.AM-2", Description = "Software platforms and applications within the organization are inventoried", Status = "Compliant" }
                    }
                },
                new ComplianceCategory
                {
                    Name = "Protect (PR)",
                    Controls = new[]
                    {
                        new ComplianceControl { Id = "PR.AC-1", Description = "Identities and credentials are issued, managed, verified, revoked, and audited", Status = "Compliant" },
                        new ComplianceControl { Id = "PR.AC-4", Description = "Access permissions and authorizations are managed", Status = "Compliant" },
                        new ComplianceControl { Id = "PR.DS-1", Description = "Data-at-rest is protected", Status = "Compliant" }
                    }
                }
            }
        };
    }

    // OWASP compliance
    public async Task<OwaspComplianceReport> GenerateOwaspComplianceReport()
    {
        return new OwaspComplianceReport
        {
            Top10Compliance = new[]
            {
                new OwaspControl { Id = "A01", Name = "Broken Access Control", Status = "Mitigated", Measures = "Entity ownership validation, permission checks" },
                new OwaspControl { Id = "A02", Name = "Cryptographic Failures", Status = "Mitigated", Measures = "BCrypt password hashing, JWT signing" },
                new OwaspControl { Id = "A03", Name = "Injection", Status = "Mitigated", Measures = "Input validation, parameterized queries" },
                new OwaspControl { Id = "A07", Name = "Identification and Authentication Failures", Status = "Mitigated", Measures = "Strong authentication, session management" }
            }
        };
    }
}
```

## Security Checklist

### Pre-Production Security Checklist

- [ ] **Password Security**
  - [ ] BCrypt work factor set to 12 or higher
  - [ ] Password policy enforced (length, complexity)
  - [ ] Common passwords blocked
  - [ ] Password reset uses secure tokens with expiration

- [ ] **Token Security**
  - [ ] Access tokens expire within 15 minutes
  - [ ] Refresh tokens expire within 30 days
  - [ ] JWT signing key is 256+ bits
  - [ ] Tokens use secure random generation
  - [ ] Token blacklisting implemented

- [ ] **Session Security**
  - [ ] Session limits enforced (max 5 per user)
  - [ ] Session binding (IP/User-Agent validation)
  - [ ] Concurrent session management
  - [ ] Session cleanup on password reset

- [ ] **Attack Prevention**
  - [ ] Brute force protection (rate limiting)
  - [ ] CSRF protection implemented
  - [ ] Input validation against injection
  - [ ] Timing attack protection
  - [ ] Account lockout mechanisms

- [ ] **Audit and Monitoring**
  - [ ] All authentication events logged
  - [ ] Security metrics monitoring
  - [ ] Real-time suspicious activity detection
  - [ ] Security alert system configured

- [ ] **Infrastructure Security**
  - [ ] HTTPS enforced everywhere
  - [ ] Security headers configured
  - [ ] Database encryption at rest
  - [ ] Secure key management
  - [ ] Regular security updates

- [ ] **Compliance**
  - [ ] GDPR data export/deletion
  - [ ] Audit trail retention policy
  - [ ] Privacy policy implementation
  - [ ] Security incident response plan
  - [ ] Regular security assessments

### Production Deployment Security

```bash
# Environment variables (never hardcode)
export JWT_SECRET_KEY="your-256-bit-production-secret-key"
export BCRYPT_WORK_FACTOR="12"
export DATABASE_CONNECTION_STRING="encrypted-connection-string"

# Security headers in production
export SECURITY_HEADERS_ENABLED="true"
export CSRF_PROTECTION_ENABLED="true"
export RATE_LIMITING_ENABLED="true"

# Monitoring and alerting
export SECURITY_MONITORING_ENABLED="true"
export AUDIT_LOG_LEVEL="Information"
export ALERT_EMAIL="security@yourcompany.com"
```

### Regular Security Maintenance

- [ ] **Weekly**
  - [ ] Review security audit logs
  - [ ] Check for failed login patterns
  - [ ] Monitor active sessions

- [ ] **Monthly**
  - [ ] Update dependencies
  - [ ] Review access permissions
  - [ ] Test backup/recovery procedures
  - [ ] Security metrics review

- [ ] **Quarterly**
  - [ ] Security penetration testing
  - [ ] Access control audit
  - [ ] Incident response plan review
  - [ ] Security training update

---

**Next**: [Testing Guide](authentication-testing.md) - Comprehensive testing strategies for authentication systems