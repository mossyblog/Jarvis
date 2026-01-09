# Security Architecture Documentation

## Implemented Security Controls

### Password Security
- **Algorithm**: BCrypt with cost factor 12
- **Salt**: Automatically generated per-password
- **Strength**: Approximately 2^12 iterations, providing strong protection against brute force attacks

### Token Security
- **JWT Signing**: HMAC-SHA256 with configurable secret key
- **Refresh Tokens**: Implemented with rotation on each use
- **Token Expiry**: Configurable access token and refresh token lifetimes
- **Session Tracking**: Sessions tracked for token revocation capabilities

### Rate Limiting
- **Per-IP Limits**: 5 attempts per minute, 20 attempts per hour
- **Account Lockout**: 5 failed attempts triggers 30-minute lockout
- **Progressive Delay**: Failed attempts add increasing delays (up to 10 seconds)
- **Retry-After Header**: Standard HTTP 429 responses with retry guidance

### Row-Level Security (RLS)
- **Dual-Layer Protection**: Both SDK-level and PostgreSQL policy enforcement
- **Entity Isolation**: Users can only access data within their entity scope
- **Audit Trail**: All security-relevant operations are logged

### Input Validation
- **GraphQL Query Limits**: Maximum depth (10), field count (100), aliases (10), query length (10KB)
- **Introspection Blocking**: Disabled in production environments
- **Mass Assignment Prevention**: DTOs used to limit user-modifiable fields
- **JSON Deserialization**: Safe settings with type checking enabled

## Known Limitations

### Rate Limiting (In-Memory)
Rate limiting uses in-memory storage (`ConcurrentDictionary`). For multi-instance deployments:
- Rate limits apply per-instance, not globally
- Users can exceed intended limits by hitting different instances
- Account lockouts are not shared across instances

**Recommendation**: For multi-instance deployments, implement distributed caching with Redis.
See: https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed

### IP Address Handling
- X-Forwarded-For headers are NOT trusted by default to prevent IP spoofing
- Configure `TrustedProxyOptions` if deploying behind trusted reverse proxies
- Azure Functions use X-Azure-ClientIP for socket-level IP

### Features Not Yet Implemented

#### Email Verification
- Users can register without email confirmation
- No email confirmation flow implemented
- **Risk**: Accounts may be created with invalid or malicious email addresses

#### Multi-Factor Authentication (MFA/2FA)
- `Account.TwoFactorCode` field exists but is not enforced
- No TOTP or SMS verification implemented
- **Risk**: Account compromise via password-only authentication

#### Password Reset
- No self-service password reset flow
- Password changes require current password or admin intervention
- **Risk**: Users locked out with no recovery path

#### JWT Key Rotation
- Single signing key configured at deployment
- No automatic key rotation mechanism
- **Risk**: Key compromise requires manual rotation and token invalidation

## Recommendations for Production

### 1. Reverse Proxy Configuration
Deploy behind a reverse proxy (nginx, Azure Front Door, Cloudflare) with:
- Trusted IP configuration for proper client IP detection
- SSL/TLS termination with modern cipher suites
- DDoS protection at the edge

### 2. Distributed Rate Limiting
Implement Redis for distributed rate limiting:
```csharp
// Example Redis-backed rate limiting
services.AddStackExchangeRedisCache(options => {
    options.Configuration = "your-redis-connection";
});
services.AddRateLimiting(options => {
    options.UseDistributedCache = true;
});
```

### 3. Email Verification
Before account activation:
- Send verification email with secure token
- Block login until email is verified
- Implement email change verification

### 4. TOTP-Based MFA for Admin Accounts
Implement Time-based One-Time Password (TOTP):
- Require MFA for accounts with admin roles
- Support authenticator apps (Google Authenticator, Authy)
- Provide backup codes for account recovery

### 5. Monitoring and Alerting
Set up alerts for:
- Multiple failed authentication attempts
- Account lockouts
- Rate limit threshold approaches
- Suspicious activity patterns (logged via `LogSuspiciousActivity`)

### 6. Regular Security Audits
- Review audit logs periodically
- Test rate limiting effectiveness
- Validate RLS policies with penetration testing
- Update dependencies for security patches

## Security Event Types

The following events are logged for audit purposes:

| Event Type | Severity | Description |
|------------|----------|-------------|
| AUTHENTICATION_FAILED | MEDIUM | Failed login attempt |
| AUTHENTICATION_SUCCESS | INFO | Successful login |
| PASSWORD_CHANGED | MEDIUM | Password update |
| ACCOUNT_LOCKED | HIGH | Account lockout triggered |
| TOKEN_REFRESHED | INFO | Token refresh operation |
| TOKEN_REVOKED | MEDIUM | Token explicitly revoked |
| ROLE_UPDATED | MEDIUM | Role permissions modified |
| PERMISSION_GRANTED | MEDIUM | Permission assigned |
| PERMISSION_REVOKED | MEDIUM | Permission removed |
| SUSPICIOUS_* | CRITICAL | Potential security threat |

## Reporting Security Issues

If you discover a security vulnerability, please report it responsibly:
1. Do not disclose publicly until addressed
2. Contact the security team directly
3. Provide detailed reproduction steps
4. Allow reasonable time for remediation
