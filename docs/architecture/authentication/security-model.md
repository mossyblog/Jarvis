# Authentication Security Model

This document explains the multi-layered security architecture that protects authentication in Jarvis applications.

## Defense in Depth

Jarvis implements multiple security layers, ensuring that even if one layer is compromised, others continue to protect the system.

### Security Layers

1. **Network Layer**
   - HTTPS/TLS 1.3 encryption
   - WAF protection (when deployed)
   - Rate limiting at edge
   - DDoS protection

2. **Application Layer**
   - Input validation and sanitization
   - CSRF protection
   - XSS prevention
   - Injection attack prevention

3. **Authentication Layer**
   - BCrypt password hashing (cost factor 12+)
   - JWT token security
   - Timing attack protection
   - Session management

4. **Authorization Layer**
   - Component access control
   - Entity ownership validation
   - Handler-level permissions
   - Business rule enforcement

5. **Data Layer**
   - Row-Level Security (RLS) in PostgreSQL
   - Encrypted data storage
   - Audit logging
   - Access control policies

## Key Security Features

### Password Security

#### BCrypt Hashing
```csharp
// Passwords are hashed with BCrypt using a configurable work factor
public class PasswordService : IPasswordService
{
    private readonly int _workFactor = 12; // Minimum recommended

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, _workFactor);
    }
}
```

**Key Points:**
- Cost factor 12 in production (10-12 in development for speed)
- Automatic salt generation
- Resistant to rainbow table attacks
- Computationally expensive to prevent brute force

#### Password Policy
- Minimum 8 characters (configurable)
- Requires uppercase, lowercase, numbers, and special characters
- Prevents common passwords
- No personal information (email parts)

### Token Security

#### Access Tokens
- **Lifetime**: 15 minutes (configurable)
- **Algorithm**: HMAC-SHA256
- **Claims**: User ID, email, roles, permissions
- **Storage**: Memory only (never persisted)

#### Refresh Tokens
- **Lifetime**: 30 days (configurable)
- **Rotation**: New token on each refresh
- **Storage**: Hashed in database
- **Revocation**: Can be invalidated immediately

### Account Security

#### Inactive by Default
New accounts require manual activation:
```csharp
// Accounts start inactive
var account = await accountHandler.Register(credentials);
// account.IsActive == false

// Must be explicitly activated
await accountHandler.Activate();
```

This prevents:
- Automated account creation attacks
- Unverified email registrations
- Bot registrations

#### Timing Attack Protection
All authentication operations execute in constant time:
```csharp
public async Task<AuthToken> Authenticate(Account credentials)
{
    return await _constantTimeService.ExecuteWithMinimumTime(
        async () => await AuthenticateInternal(credentials),
        minimumMilliseconds: 500
    );
}
```

### Session Security

#### Session Binding
Sessions are bound to:
- IP address (with subnet flexibility)
- User agent string
- Device fingerprint

#### Session Limits
- Maximum 5 active sessions per user (configurable)
- Automatic cleanup of expired sessions
- Oldest session revoked when limit exceeded

### Audit Trail

Every security event is logged:
```csharp
public enum SecurityEventType
{
    LoginSuccess,
    LoginFailed,
    AccountLocked,
    PasswordReset,
    TokenRefresh,
    SuspiciousActivity,
    PermissionDenied
}
```

## Attack Prevention

### Brute Force Protection
- Account lockout after 5 failed attempts
- Progressive delays between attempts
- IP-based rate limiting
- CAPTCHA integration support

### SQL Injection Prevention
- Parameterized queries only
- Input validation at every layer
- Dapper with parameter binding
- No dynamic SQL construction

### XSS Prevention
- Input sanitization
- Content Security Policy headers
- Output encoding
- React's built-in XSS protection

### CSRF Protection
- Double-submit cookie pattern
- Custom headers validation
- SameSite cookie attributes
- Origin verification

## Security Headers

All responses include:
```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Strict-Transport-Security: max-age=31536000
Content-Security-Policy: default-src 'self'
```

## Monitoring and Alerts

### Real-time Monitoring
- Failed login patterns
- Unusual IP addresses
- Session anomalies
- Permission violations

### Security Metrics
- Failed login rate
- Account lockout frequency
- Token refresh patterns
- Suspicious activity score

## Best Practices

1. **Never Store Plain Passwords**
   - Always hash with BCrypt
   - Never log passwords
   - Clear from memory after use

2. **Token Handling**
   - Store tokens securely (HttpOnly cookies or secure storage)
   - Never expose tokens in URLs
   - Implement automatic refresh

3. **Session Management**
   - Invalidate sessions on logout
   - Clean up expired sessions
   - Monitor for session hijacking

4. **Error Messages**
   - Use generic error messages
   - Don't reveal if email exists
   - Log detailed errors internally only

## Related Documentation

- [Security Best Practices](/docs/guides/authentication/security-best-practices.md)
- [Security Testing Guide](/docs/guides/authentication/security-testing.md)
- [Compliance and Standards](/docs/architecture/authentication/compliance.md)
- [Incident Response](/docs/guides/authentication/incident-response.md)

---

**Need Help?** For security concerns, contact the security team or check the [Security FAQ](/docs/troubleshooting/security-faq.md)