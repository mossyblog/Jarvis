# Authentication Quick Start

Get authentication working in your Jarvis application in 5 minutes.

## Table of Contents
1. [Setup](#setup)
2. [Register Your First User](#register-your-first-user)
3. [Authenticate](#authenticate)
4. [Use the API Endpoints](#use-the-api-endpoints)
5. [Next Steps](#next-steps)

## Setup

### 1. Configure Services

Add authentication services to your `Program.cs`:

```csharp
// Register Jarvis with authentication
services.RegisterJarvis(LogLevel.Information, Configuration);

// Register authentication handlers
services.AddScoped<IComponentHandler, AccountHandler>();
services.AddScoped<AccountHandler>();

services.AddScoped<IComponentHandler, AuthHandler>();
services.AddScoped<AuthHandler>();

// Register authentication function (API endpoint)
services.AddScoped<RegisterFunction>();
services.AddScoped<AuthFunction>();
```

### 2. Configure JWT Settings

Add to your `appsettings.json`:

```json
{
  "Jwt": {
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 30,
    "SecretKey": "your-256-bit-secret-key-for-production",
    "Issuer": "YourApp",
    "Audience": "YourAppUsers"
  }
}
```

## Register Your First User

### Using Handlers Directly

```csharp
// Create a new entity for the user
var userEntityId = Guid.NewGuid();

// Use AccountHandler to register
var accountHandler = dataContext.For<AccountHandler>(userEntityId);

var account = await accountHandler.Register(new Account
{
    Email = "user@example.com",
    Password = "SecurePassword123!"
});

// Account is created but INACTIVE by default
Console.WriteLine($"Account created: {account.Email}, Active: {account.IsActive}");
// Output: Account created: user@example.com, Active: False

// Activate the account
var activatedAccount = await accountHandler.Activate();
Console.WriteLine($"Account activated: {activatedAccount.IsActive}");
// Output: Account activated: True
```

### Using the Registration API

```bash
# Register via API endpoint
curl -X POST http://localhost:7071/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePassword123!"
  }'
```

Response:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
  "email": "user@example.com",
  "isActive": false,
  "createdAt": "2025-01-29T10:00:00Z"
}
```

## Authenticate

### Using Handlers

```csharp
// Create an AuthHandler
var authHandler = dataContext.For<AuthHandler>(Guid.NewGuid());

// Authenticate with credentials
var authResult = await authHandler.Authenticate(new Account
{
    Email = "user@example.com",
    Password = "SecurePassword123!",
    IpAddress = "192.168.1.1",
    UserAgent = "MyApp/1.0"
});

// Check if authentication succeeded
if (!string.IsNullOrEmpty(authResult.AccessToken))
{
    Console.WriteLine("Authentication successful!");
    Console.WriteLine($"Access Token: {authResult.AccessToken}");
    Console.WriteLine($"Refresh Token: {authResult.RefreshToken}");
    Console.WriteLine($"Expires At: {authResult.ExpiresAt}");
}
```

### Using the API

```bash
# Authenticate via API
curl -X POST http://localhost:7071/api/security/auth \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePassword123!"
  }'
```

Response:
```json
{
  "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresAt": "2025-01-29T10:15:00Z",
  "refreshExpiresAt": "2025-02-28T10:00:00Z"
}
```

## Use the API Endpoints

### Making Authenticated Requests

```javascript
// JavaScript/Frontend example
const response = await fetch('/api/protected-endpoint', {
    headers: {
        'Authorization': `Bearer ${accessToken}`,
        'Content-Type': 'application/json'
    }
});
```

```csharp
// C# example
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", accessToken);

var response = await httpClient.GetAsync("/api/protected-endpoint");
```

### Refresh Tokens

```bash
# Refresh tokens when they expire
curl -X POST http://localhost:7071/api/security/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "your-refresh-token-here"
  }'
```

## Key Security Features

Your authentication system now includes:

- ✅ **BCrypt Password Hashing** - Cost factor 12 for strong protection
- ✅ **JWT Tokens** - 15-minute access tokens, 30-day refresh tokens
- ✅ **Inactive by Default** - New accounts require manual activation
- ✅ **Timing Attack Protection** - Constant-time authentication
- ✅ **Session Management** - Tracked sessions with revocation support
- ✅ **Security Auditing** - Complete audit trail of auth events

## Next Steps

You now have basic authentication working! Here's what to explore next:

### Essential Guides
- [Frontend Integration Guide](/docs/guides/authentication/frontend-integration.md)
- [Token Management](/docs/guides/authentication/token-management.md)
- [User Management](/docs/guides/authentication/user-management.md)

### Advanced Topics
- [Security Best Practices](/docs/architecture/authentication/security-best-practices.md)
- [Two-Factor Authentication](/docs/guides/authentication/two-factor-auth.md)
- [Session Management](/docs/guides/authentication/session-management.md)

### Testing & Troubleshooting
- [Testing Authentication](/docs/guides/authentication/testing.md)
- [Common Issues](/docs/troubleshooting/authentication-common-issues.md)

---

**Need Help?** Check the [Troubleshooting Guide](/docs/troubleshooting/authentication.md) or ask in the [Community Forum](https://github.com/jarvis/discussions)