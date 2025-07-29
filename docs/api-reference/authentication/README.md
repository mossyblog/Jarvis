# Authentication API Reference

Complete API reference for authentication components, handlers, and endpoints in Jarvis.

## Quick Links

### Components
- [Account Component](components.md#account-component) - User identity and credentials
- [AuthToken Component](components.md#authtoken-component) - JWT session management

### Handlers
- [AccountHandler](handlers.md#accounthandler) - Account lifecycle operations
- [AuthHandler](handlers.md#authhandler) - Authentication and token operations

### HTTP Endpoints
- [Registration API](endpoints.md#registration) - User registration endpoints
- [Authentication API](endpoints.md#authentication) - Login and token endpoints
- [Navigation API](endpoints.md#navigation) - Navigation menu endpoints

### Services
- [ITokenService](services.md#itokenservice) - JWT token generation
- [IPasswordService](services.md#ipasswordservice) - Password hashing
- [ISecurityAuditService](services.md#isecurityauditservice) - Security logging

## Recent Updates

### New Features (v2.1.2+)
- ✨ **Registration API** - Direct API→Handler pattern without System layer
- ✨ **Navigation System** - Dynamic menu generation based on permissions
- ✨ **GraphQL Support** - GraphQL endpoint infrastructure
- ✨ **Real Integration** - No mocks, direct handler usage

## Component Overview

### Account Component

Stores user identity and authentication data:

```csharp
public record Account : IComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }
    
    // Core identity
    public string Email { get; init; }
    public string PasswordHash { get; init; }
    public string Password { get; init; }  // Not persisted
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

[Full Component Reference →](components.md)

## Handler Overview

### AccountHandler

Manages account lifecycle:

```csharp
public class AccountHandler : ComponentHandler<Account>
{
    public async Task<Account> Register(Account account);
    public async Task<Account> Activate();
    public async Task<Account> Deactivate();
}
```

### AuthHandler

Handles authentication operations:

```csharp
public class AuthHandler : ComponentHandler<Account>
{
    public async Task<AuthToken> Authenticate(Account credentials);
    public async Task<AuthToken> RefreshToken(string refreshToken);
    public bool IsAuthenticated(AuthToken token);
}
```

[Full Handler Reference →](handlers.md)

## Endpoint Overview

### Core Endpoints

| Method | Endpoint | Description | Handler |
|--------|----------|-------------|---------|
| POST | `/api/auth/register` | Register new user | RegisterFunction → AccountHandler |
| POST | `/api/security/auth` | Authenticate user | AuthFunction → AuthHandler |
| POST | `/api/security/refresh` | Refresh tokens | AuthFunction → AuthHandler |
| GET | `/api/navigation` | Get navigation menu | NavigationFunction → NavigationHandler |

[Full Endpoint Reference →](endpoints.md)

## Quick Examples

### Register a User

```bash
curl -X POST http://localhost:7071/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePassword123!"
  }'
```

### Authenticate

```bash
curl -X POST http://localhost:7071/api/security/auth \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePassword123!"
  }'
```

### Use Authentication

```javascript
// Include token in requests
fetch('/api/protected-endpoint', {
    headers: {
        'Authorization': `Bearer ${accessToken}`
    }
});
```

## Error Handling

All endpoints return consistent error formats:

```json
{
  "code": "ERROR_CODE",
  "message": "Human readable message",
  "statusCode": 400,
  "details": {
    "field": ["validation error"]
  }
}
```

### Common Error Codes

| Code | Description | HTTP Status |
|------|-------------|-------------|
| `EMAIL_EXISTS` | Email already registered | 400 |
| `INVALID_CREDENTIALS` | Wrong email/password | 401 |
| `ACCOUNT_LOCKED` | Too many failed attempts | 423 |
| `TOKEN_EXPIRED` | Access token expired | 401 |
| `INVALID_TOKEN` | Malformed or invalid token | 401 |

## Configuration

### JWT Configuration

```json
{
  "Jwt": {
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 30,
    "SecretKey": "your-256-bit-secret",
    "Issuer": "YourApp",
    "Audience": "YourAppUsers"
  }
}
```

### Security Configuration

```json
{
  "Security": {
    "BCryptWorkFactor": 12,
    "MaxFailedAttempts": 5,
    "LockoutDurationMinutes": 15,
    "MaxActiveSessions": 5
  }
}
```

## Related Documentation

- [Getting Started](/docs/getting-started/authentication/) - Quick start guide
- [Implementation Guides](/docs/guides/authentication/) - Step-by-step tutorials
- [Architecture](/docs/architecture/authentication/) - How it works
- [Troubleshooting](/docs/troubleshooting/authentication.md) - Common issues

---

**API Version**: v2.1.2  
**Last Updated**: January 2025