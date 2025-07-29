# Authentication Architecture Overview

This document explains how authentication integrates with the Jarvis ECS framework, following the same Component-Handler-System patterns used throughout the framework.

## Architecture Layers

Authentication in Jarvis follows a multi-layered approach that aligns with the ECS architecture:

```
┌─────────────────────────────────────────────────┐
│                 API Layer                        │
│              (Azure Functions)                   │
│  • RegisterFunction  • AuthFunction             │
│  • Token validation • HTTPS termination         │
└─────────────────────┬───────────────────────────┘
                      │ HTTP + JSON
┌─────────────────────▼───────────────────────────┐
│               System Layer                       │
│              (Future: AuthSystem)                │
│  • Orchestrate workflows • Business rules       │
│  • Multi-handler operations                     │
└─────────────────────┬───────────────────────────┘
                      │ Component Operations
┌─────────────────────▼───────────────────────────┐
│              Handler Layer                       │
│       (AccountHandler + AuthHandler)            │
│  • Account lifecycle  • Authentication logic    │
│  • Password management • Token generation       │
└─────────────────────┬───────────────────────────┘
                      │ Component CRUD
┌─────────────────────▼───────────────────────────┐
│             Component Layer                      │
│          (Account + AuthToken)                   │
│  • Account data      • Session data             │
│  • Authentication    • Token storage            │
└─────────────────────┬───────────────────────────┘
                      │ Database Operations
┌─────────────────────▼───────────────────────────┐
│            Database Layer                        │
│              (PostgreSQL)                        │
│  • account_component • auth_token_component      │
│  • BCrypt password   • JWT session storage      │
└─────────────────────────────────────────────────┘
```

## Core Components

### Account Component
The Account component stores user identity and authentication data:

```csharp
public record Account : IComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }  // The user's entity ID
    public DateTime LastUpdated { get; set; }
    
    // Identity
    public string Email { get; init; }
    public string PasswordHash { get; init; }  // BCrypt hash
    public bool IsActive { get; init; }        // Security feature
    public DateTime CreatedAt { get; init; }
    
    // Authentication context
    public string? IpAddress { get; init; }    // For audit/security
    public string? UserAgent { get; init; }    // For audit/security
    public string? ClientId { get; init; }     // For session tracking
}
```

### AuthToken Component
The AuthToken component manages JWT sessions and refresh tokens:

```csharp
public record AuthToken : IComponent, IVersionedComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }   // Links to Account owner
    public DateTime LastUpdated { get; set; }
    public int? Version { get; set; }          // For concurrency control
    
    // JWT tokens (access token not persisted for security)
    public string AccessToken { get; init; }    // Only in memory
    public string RefreshToken { get; init; }   // Only in memory  
    public string RefreshTokenHash { get; set; } // Stored in DB
    
    // Session management
    public DateTime ExpiresAt { get; init; }      // Access token expiry
    public DateTime RefreshExpiresAt { get; init; } // Refresh token expiry
    public Guid SessionId { get; init; }          // Unique session ID
    public bool IsRevoked { get; set; }           // For security
    public DateTime? RevokedAt { get; set; }
}
```

## Handler Layer

### AccountHandler
Manages the account lifecycle following the ComponentHandler pattern:

```csharp
public class AccountHandler : ComponentHandler<Account>
{
    // Account Registration (starts inactive for security)
    public async Task<Account> Register(Account accountComponent)
    {
        // 1. Validate email/password
        // 2. Check for existing account
        // 3. Hash password with BCrypt (cost factor 12)
        // 4. Create inactive account
        // 5. Log registration
    }
    
    // Manual activation required for security
    public async Task<Account> Activate()
    public async Task<Account> Deactivate()
}
```

### AuthHandler
Handles authentication and token operations:

```csharp
public class AuthHandler : ComponentHandler<Account>
{
    // Secure authentication with timing attack protection
    public async Task<AuthToken> Authenticate(Account credentials)
    {
        // 1. Constant-time execution wrapper
        // 2. Find account by email
        // 3. Verify account is active
        // 4. BCrypt password verification
        // 5. Generate JWT tokens
        // 6. Create AuthToken with session data
        // 7. Security audit logging
    }
    
    // Token refresh with security validation
    public async Task<AuthToken> RefreshToken(string refreshToken)
    {
        // 1. Hash and lookup refresh token
        // 2. Validate expiration and revocation
        // 3. Generate new token pair
        // 4. Revoke old refresh token
        // 5. Return new AuthToken
    }
}
```

## API Layer

### Thin API Functions
Following Jarvis patterns, API functions are thin wrappers that delegate to handlers:

```csharp
// RegisterFunction - delegates to AccountHandler
[Function("RegisterUser")]
public async Task<HttpResponseData> Register(HttpRequestData req)
{
    var accountComponent = JsonSerializer.Deserialize<Account>(requestBody);
    var entityId = Guid.NewGuid();
    var handler = _dataContext.For<AccountHandler>(entityId);
    var result = await handler.Register(accountComponent);
    return CreateResponse(result);
}

// AuthFunction - delegates to AuthHandler  
[Function("auth")]
public async Task<HttpResponseData> Run(HttpRequestData req)
{
    var credentials = JsonSerializer.Deserialize<Account>(requestBody);
    var handler = _dataContext.For<AuthHandler>(Guid.NewGuid());
    var authToken = await handler.Authenticate(credentials);
    return CreateResponse(authToken);
}
```

## Entity Relationships

Authentication uses entity relationships to link related data:

```
┌─────────────────┐
│   User Entity   │ ◄─── OwnerEntityId ────┐
│   (Guid ID)     │                        │
└─────────────────┘                        │
          │                                │
          │ OwnerEntityId                  │
          ▼                                │
┌─────────────────┐              ┌─────────────────┐
│ Account         │              │ AuthToken       │
│ Component       │              │ Component       │
│                 │              │                 │
│ • Email         │              │ • SessionId     │
│ • PasswordHash  │              │ • RefreshToken  │
│ • IsActive      │              │ • ExpiresAt     │
└─────────────────┘              └─────────────────┘
```

The User Entity ID serves as the primary key that links:
- Account component (stores identity)
- AuthToken components (multiple sessions per user)
- Any other user-related components in your application

## Security Features

### Password Security
- **BCrypt Hashing**: Cost factor 12 (adjustable via configuration)
- **Salt Generation**: Automatic unique salt per password
- **Timing Attack Protection**: Constant-time authentication execution

### Token Security
- **Short-Lived Access Tokens**: 15-minute expiration (configurable)
- **Refresh Token Rotation**: New refresh token on each use
- **Token Storage**: Only hashed refresh tokens stored in database
- **Session Limits**: Maximum active sessions per user (configurable)

### Account Security
- **Inactive by Default**: New accounts require manual activation
- **Security Auditing**: Complete audit trail of authentication events
- **Input Validation**: Protection against injection attacks
- **Rate Limiting**: Protection against brute force attacks

## Integration Patterns

### Direct Handler Usage (Internal APIs)
```csharp
// For internal services or background jobs
var accountHandler = dataContext.For<AccountHandler>(userEntityId);
var account = await accountHandler.GetOrDefault();
if (account?.IsActive == true) 
{
    // User is active
}
```

### HTTP API Usage (External Clients)
```csharp
// For web/mobile applications
var response = await httpClient.PostAsync("/api/security/auth", 
    JsonContent.Create(new { email = "user@example.com", password = "secret" }));
var authToken = await response.Content.ReadFromJsonAsync<AuthToken>();
```

### Entity Query Usage (Complex Queries)
```csharp
// For reporting or admin functions
var activeUsers = await dataContext.Query()
    .With<Account>(a => a.IsActive)
    .ToEntityComponents();
```

## Testing Architecture

Authentication testing follows Jarvis patterns:

```csharp
public class AuthenticationTests : IntegrationTestBase
{
    [Fact]
    public async Task Should_Register_And_Authenticate_User()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId); // For cleanup
        
        var accountHandler = TestDataContext().For<AccountHandler>(entityId);
        
        // Act - Register
        var account = await accountHandler.Register(new Account 
        { 
            Email = "test@example.com", 
            Password = "TestPassword123!" 
        });
        
        // Activate for testing
        await accountHandler.Activate();
        
        // Act - Authenticate
        var authHandler = TestDataContext().For<AuthHandler>(Guid.NewGuid());
        var authResult = await authHandler.Authenticate(new Account
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        });
        
        // Assert
        authResult.AccessToken.ShouldNotBeEmpty();
        authResult.OwnerEntityId.ShouldBe(entityId);
    }
}
```

## Next Steps

### Implementation Guides
- [Getting Started Guide](authentication-getting-started.md) - Quick setup and first authentication
- [API Integration](authentication-guides.md#api-integration) - Web/mobile app integration
- [Testing Patterns](authentication-testing.md) - Write effective authentication tests

### Advanced Topics
- [Security Best Practices](authentication-security.md) - Secure your implementation
- [Troubleshooting](authentication-troubleshooting.md) - Common issues and solutions
- [API Reference](authentication-api-reference.md) - Complete method documentation

### Framework Integration
- [Handler Pattern](../05_Governance/system-handler-architecture.md) - Understanding handlers
- [Component Design](../01_CurrentState/Components/README.md) - ECS component principles
- [Database Integration](../01_CurrentState/Components/datacontext-technical-whitepaper.md) - Data persistence

---

**Next**: [API Reference](authentication-api-reference.md) - Complete authentication API documentation