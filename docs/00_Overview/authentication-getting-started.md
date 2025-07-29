# Getting Started with Authentication

This guide will help you quickly implement authentication in your Jarvis ECS application. Authentication in Jarvis follows the same Component-Handler-System pattern as other features, making it consistent and predictable.

## Quick Start

### 1. Register Your First User

First, let's register a new user account. Accounts in Jarvis start as **inactive** and must be manually activated for security.

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

// Account is now registered but INACTIVE
Console.WriteLine($"Account created: {account.Email}, Active: {account.IsActive}");
// Output: Account created: user@example.com, Active: False
```

### 2. Activate the Account

New accounts start inactive for security. Activate them when ready:

```csharp
// Activate the account
var activatedAccount = await accountHandler.Activate();
Console.WriteLine($"Account activated: {activatedAccount.IsActive}");
// Output: Account activated: True
```

### 3. Authenticate the User

Now authenticate using the AuthHandler:

```csharp
// Create an AuthHandler (can use any entity ID for this operation)
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
    Console.WriteLine($"Expires At: {authResult.ExpiresAt}");
}
```

### 4. Use the API Endpoints

For web applications, use the built-in API endpoints:

```bash
# Register a new user
curl -X POST http://localhost:7071/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePassword123!"
  }'

# Authenticate user
curl -X POST http://localhost:7071/api/security/auth \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePassword123!"
  }'
```

## Understanding the Flow

### Registration → Activation → Authentication

1. **Registration**: Creates an inactive account with hashed password
2. **Activation**: Makes the account usable (manual step for security)
3. **Authentication**: Validates credentials and returns JWT tokens

### Component-Handler Pattern

Authentication follows Jarvis patterns:
- **Account Component**: Stores user data (email, passwordHash, isActive)
- **AuthToken Component**: Manages JWT sessions and refresh tokens
- **AccountHandler**: Manages account lifecycle (Register, Activate, Deactivate)
- **AuthHandler**: Handles authentication and token operations

## Next Steps

### Learn the Architecture
- [Authentication Architecture](../01_CurrentState/Services/authentication-rbac-rls-technical-whitepaper.md) - Deep dive into the security model
- [Handler Pattern](../05_Governance/system-handler-architecture.md) - Understanding the handler architecture

### Implement Features
- [API Integration Guide](authentication-guides.md#api-integration) - Integrate with web/mobile apps
- [Testing Authentication](authentication-testing.md) - Write tests for your auth flows
- [Security Best Practices](authentication-security.md) - Secure your implementation

### Common Scenarios
- [Frontend Integration](authentication-guides.md#frontend-integration) - React/JavaScript integration
- [Token Refresh](authentication-guides.md#token-refresh) - Implement automatic token refresh
- [User Management](authentication-guides.md#user-management) - Admin functions for managing users

## Key Security Features

- **BCrypt Password Hashing**: Cost factor 12 for strong protection
- **JWT Tokens**: 15-minute access tokens with 30-day refresh tokens
- **Inactive by Default**: New accounts require manual activation
- **Timing Attack Protection**: Constant-time authentication execution
- **Session Management**: Tracked sessions with revocation support
- **Security Auditing**: Complete audit trail of authentication events

## Troubleshooting

### Account Not Found
```csharp
// Check if account exists
var query = dataContext.Query()
    .With<Account>(a => a.Email == "user@example.com");
var accounts = await query.ToEntityComponents();

if (!accounts.Any())
{
    Console.WriteLine("Account does not exist");
}
```

### Authentication Returns Empty Token
This means authentication failed. Common causes:
- Wrong password
- Account is inactive
- Account doesn't exist

The AuthHandler always returns an empty `AuthToken` on failure for security.

### Need Help?
- [Troubleshooting Guide](authentication-troubleshooting.md) - Common issues and solutions
- [Security Considerations](authentication-security.md) - Understanding security features
- [API Reference](authentication-api-reference.md) - Complete method documentation

---

**Next**: [Authentication Architecture](authentication-architecture.md) - Learn how authentication fits into the ECS framework