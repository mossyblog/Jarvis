# Authentication API Reference

Complete reference for all authentication components, handlers, and endpoints in the Jarvis ECS framework.

## Table of Contents

1. [Components](#components)
2. [Handlers](#handlers) 
3. [HTTP Endpoints](#http-endpoints)
4. [Services](#services)
5. [Models](#models)
6. [Error Handling](#error-handling)

## Components

### Account Component

Stores user identity and authentication data.

```csharp
public record Account : IComponent
{
    // IComponent properties
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }        // User's entity ID
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Core identity
    public string Email { get; init; } = string.Empty;           // User's email address
    public string PasswordHash { get; init; } = string.Empty;    // BCrypt hash (stored)
    public string Password { get; init; } = string.Empty;        // Plain password (not persisted)
    public bool IsActive { get; init; } = true;                  // Account active status
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;  // Creation timestamp
    
    // Authentication context (optional)
    public string? TwoFactorCode { get; init; }    // 2FA code for validation
    public string AuthMethod { get; init; } = "password";  // Auth method type
    public string? ClientId { get; init; }         // Client identifier
    public string? IpAddress { get; init; }        // Request IP address
    public string? UserAgent { get; init; }        // Request user agent
}
```

**Storage**: `account_component` table in PostgreSQL with automatic snake_case mapping.

### AuthToken Component

Manages JWT sessions and refresh tokens.

```csharp
public record AuthToken : IComponent, IVersionedComponent
{
    // IComponent properties
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }        // Links to Account owner
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public int? Version { get; set; }               // Concurrency control
    
    // JWT tokens
    public string AccessToken { get; init; } = string.Empty;     // JWT access token (not persisted)
    public string RefreshToken { get; init; } = string.Empty;    // JWT refresh token (not persisted)
    public string RefreshTokenHash { get; set; } = string.Empty; // Hashed refresh token (persisted)
    
    // Token lifecycle
    public DateTime ExpiresAt { get; init; }           // Access token expiration
    public DateTime RefreshExpiresAt { get; init; }    // Refresh token expiration
    public string TokenType { get; init; } = "Bearer"; // Token type
    public bool IsRevoked { get; set; }                // Revocation status
    public DateTime? RevokedAt { get; set; }           // Revocation timestamp
    public DateTime IssuedAt { get; init; } = DateTime.UtcNow; // Issue timestamp
    
    // Session tracking
    public Guid SessionId { get; init; } = Guid.NewGuid(); // Unique session ID
    public string? ClientId { get; init; }                 // Client identifier
    public string? IpAddress { get; init; }                // Client IP
    public string? UserAgent { get; init; }                // Client user agent
}
```

**Storage**: `auth_token_component` table with versioning support.

## Handlers

### AccountHandler

Manages account lifecycle operations.

#### Constructor

```csharp
public AccountHandler(IDataContext dataContext, ILogger<AccountHandler> logger)
    : base(dataContext, logger)
```

#### Methods

##### Register
Registers a new user account with validation and password hashing.

```csharp
public async Task<Account> Register(Account accountComponent)
```

**Parameters:**
- `accountComponent`: Account data with email and plain password

**Returns:** Registered Account with hashed password and inactive status

**Behavior:**
- Validates email and password are provided
- Checks for existing email addresses
- Hashes password using BCrypt (cost factor 12)
- Creates account as **inactive** (security feature)
- Clears plain password from returned object

**Exceptions:**
- `ValidationException`: Invalid email/password or missing fields
- `BusinessRuleException`: Email already exists (code: "EMAIL_EXISTS")

**Example:**
```csharp
var accountHandler = dataContext.For<AccountHandler>(userEntityId);
var account = await accountHandler.Register(new Account
{
    Email = "user@example.com",
    Password = "SecurePassword123!"
});
// account.IsActive == false (must be manually activated)
```

##### Activate
Activates an inactive account.

```csharp
public async Task<Account> Activate()
```

**Returns:** Account with IsActive set to true

**Exceptions:**
- `InvalidOperationException`: Account component not found

**Example:**
```csharp
var activatedAccount = await accountHandler.Activate();
// activatedAccount.IsActive == true
```

##### Deactivate
Deactivates an active account.

```csharp
public async Task<Account> Deactivate()
```

**Returns:** Account with IsActive set to false

**Exceptions:**
- `InvalidOperationException`: Account component not found

**Example:**
```csharp
var deactivatedAccount = await accountHandler.Deactivate();
// deactivatedAccount.IsActive == false
```

### AuthHandler

Handles authentication and token operations.

#### Constructor

```csharp
public AuthHandler(
    IDataContext dataContext,
    ILogger<AuthHandler> logger,
    IServiceProvider serviceProvider)
    : base(dataContext, logger)
```

**Dependencies:**
- `ITokenService`: JWT token generation and validation
- `IPasswordPolicyService`: Password policy enforcement  
- `ISecurityAuditService`: Security event logging
- `IConstantTimeService`: Timing attack protection

#### Methods

##### Authenticate
Authenticates user credentials with comprehensive security features.

```csharp
public async Task<AuthToken> Authenticate(Account accountCredentials)
```

**Parameters:**
- `accountCredentials`: Account with email, password, and optional metadata (IP, UserAgent, ClientId)

**Returns:** 
- `AuthToken` with tokens and session data if successful
- Empty `AuthToken` if authentication fails (for security)

**Security Features:**
- Constant-time execution (500ms minimum in production, 10ms in test)
- Random delays to prevent timing analysis
- Input validation against injection attacks
- BCrypt password verification
- Comprehensive security audit logging

**Example:**
```csharp
var authHandler = dataContext.For<AuthHandler>(Guid.NewGuid());
var authResult = await authHandler.Authenticate(new Account
{
    Email = "user@example.com",
    Password = "SecurePassword123!",
    IpAddress = "192.168.1.1",
    UserAgent = "MyApp/1.0",
    ClientId = "web-client"
});

if (!string.IsNullOrEmpty(authResult.AccessToken))
{
    // Authentication successful
    Console.WriteLine($"Token expires: {authResult.ExpiresAt}");
}
```

##### RefreshToken
Refreshes JWT tokens using a valid refresh token.

```csharp
public async Task<AuthToken> RefreshToken(string refreshToken)
```

**Parameters:**
- `refreshToken`: Valid refresh token string

**Returns:**
- New `AuthToken` with fresh access and refresh tokens
- Empty `AuthToken` if refresh fails

**Behavior:**
- Hashes refresh token for secure lookup
- Validates token exists and is not revoked
- Checks refresh token expiration
- Generates new token pair
- Revokes old refresh token (prevents reuse)
- Maintains same session ID for continuity

**Example:**
```csharp
var newTokens = await authHandler.RefreshToken(oldRefreshToken);
if (!string.IsNullOrEmpty(newTokens.AccessToken))
{
    // Refresh successful
    // Old refresh token is now invalid
}
```

##### IsAuthenticated
Checks if an AuthToken represents successful authentication.

```csharp
public bool IsAuthenticated(AuthToken authToken)
```

**Parameters:**
- `authToken`: AuthToken to validate

**Returns:** True if token contains valid access token and owner entity ID

**Example:**
```csharp
if (authHandler.IsAuthenticated(authToken))
{
    // Token is valid for API access
}
```

##### PersistSession
Persists authenticated session with security controls.

```csharp
public async Task<bool> PersistSession(AuthToken authToken)
```

**Parameters:**
- `authToken`: Authenticated token to persist

**Returns:** True if session was successfully persisted

**Security Features:**
- Validates token before persisting
- Cleans up expired tokens
- Enforces session limit (5 active sessions per user)
- Stores only refresh token hash (not plain tokens)

**Example:**
```csharp
if (await authHandler.PersistSession(authToken))
{
    // Session persisted successfully
}
```

## HTTP Endpoints

### Registration Endpoint

#### POST /api/auth/register

Registers a new user account.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response (201 Created):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001", 
  "email": "user@example.com",
  "passwordHash": "$2a$12$...",
  "password": "",
  "isActive": false,
  "createdAt": "2025-01-29T10:00:00Z",
  "lastUpdated": "2025-01-29T10:00:00Z"
}
```

**Error Responses:**
- `400 Bad Request`: Validation errors or email already exists
- `500 Internal Server Error`: Server error during registration

### Authentication Endpoints

#### POST /api/security/auth

Authenticates user credentials.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresAt": "2025-01-29T10:15:00Z",
  "refreshExpiresAt": "2025-02-28T10:00:00Z",
  "sessionId": "550e8400-e29b-41d4-a716-446655440002"
}
```

**Error Responses:**
- `400 Bad Request`: Missing or invalid credentials
- `401 Unauthorized`: Authentication failed
- `500 Internal Server Error`: Server error

#### POST /api/security/refresh

Refreshes authentication tokens.

**Request:**
```json
{
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response (200 OK):**
```json
{
  "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer", 
  "expiresAt": "2025-01-29T10:30:00Z",
  "refreshExpiresAt": "2025-03-01T10:15:00Z",
  "sessionId": "550e8400-e29b-41d4-a716-446655440002"
}
```

**Error Responses:**
- `400 Bad Request`: Missing refresh token
- `401 Unauthorized`: Invalid or expired refresh token
- `500 Internal Server Error`: Server error

#### POST /api/security/validate

Validates an authentication token.

**Request:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response (200 OK):**
```json
{
  "isValid": true,
  "userId": "550e8400-e29b-41d4-a716-446655440001",
  "email": "user@example.com",
  "expiresAt": "2025-01-29T10:15:00Z"
}
```

**Error Responses:**
- `400 Bad Request`: Missing token
- `401 Unauthorized`: Invalid or expired token
- `500 Internal Server Error`: Server error

## Services

### ITokenService

JWT token generation and validation service.

#### Methods

##### AccessToken
Generates JWT access token.

```csharp
string AccessToken(Guid userId, string email, Dictionary<string, string>? additionalClaims = null)
```

##### RefreshToken
Generates refresh token.

```csharp
string RefreshToken()
```

##### HashRefreshToken
Generates secure hash of refresh token for storage.

```csharp
string HashRefreshToken(string refreshToken)
```

### IPasswordPolicyService

Password policy validation service.

### ISecurityAuditService

Security event logging service.

#### Methods

##### LogSuccessfulAuthentication
```csharp
Task LogSuccessfulAuthentication(Guid userId, string email, string ipAddress, string? userAgent)
```

##### LogFailedAuthentication
```csharp
Task LogFailedAuthentication(string email, string ipAddress, string? userAgent, string reason)
```

### IConstantTimeService

Timing attack protection service.

#### Methods

##### ExecuteWithMinimumTime
```csharp
Task<T> ExecuteWithMinimumTime<T>(Func<Task<T>> operation, int minimumMilliseconds)
```

##### AddRandomDelay
```csharp
Task AddRandomDelay(int minMilliseconds, int maxMilliseconds)
```

## Models

### Error Model

Standard error response format.

```csharp
public class Error
{
    public string Code { get; set; }                           // Error code
    public string Message { get; set; }                        // Human-readable message
    public int StatusCode { get; set; }                        // HTTP status code
    public Dictionary<string, string[]>? Details { get; set; } // Validation details
}
```

### RefreshTokenRequest

Request model for token refresh.

```csharp
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; }
}
```

### ValidateTokenRequest

Request model for token validation.

```csharp
public class ValidateTokenRequest
{
    public string Token { get; set; }
}
```

### TokenValidationResult

Response model for token validation.

```csharp
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
```

## Error Handling

### Exception Types

#### ValidationException
Thrown when input validation fails.

```csharp
public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }
}
```

#### BusinessRuleException
Thrown when business rules are violated.

```csharp
public class BusinessRuleException : Exception
{
    public string Code { get; }  // E.g., "EMAIL_EXISTS"
}
```

#### UnauthorizedException
Thrown when authentication fails.

```csharp
public class UnauthorizedException : Exception
```

### Error Response Format

All API endpoints return errors in consistent format:

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

## Configuration

### JWT Settings

Configure in `appsettings.json`:

```json
{
  "Jwt": {
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 30,
    "SecretKey": "your-256-bit-secret",
    "Issuer": "your-app",
    "Audience": "your-app-users"
  }
}
```

### Password Policy

Configure password requirements:

```json
{
  "PasswordPolicy": {
    "MinLength": 8,
    "RequireUppercase": true,
    "RequireLowercase": true, 
    "RequireNumbers": true,
    "RequireSpecialChars": true
  }
}
```

## Usage Examples

### Complete Registration and Authentication Flow

```csharp
// 1. Register user
var userEntityId = Guid.NewGuid();
var accountHandler = dataContext.For<AccountHandler>(userEntityId);

var account = await accountHandler.Register(new Account
{
    Email = "user@example.com",
    Password = "SecurePassword123!"
});

// 2. Activate account (admin function)
await accountHandler.Activate();

// 3. Authenticate user
var authHandler = dataContext.For<AuthHandler>(Guid.NewGuid());
var authResult = await authHandler.Authenticate(new Account
{
    Email = "user@example.com", 
    Password = "SecurePassword123!",
    IpAddress = "192.168.1.1",
    UserAgent = "MyApp/1.0"
});

// 4. Use access token for API calls
if (!string.IsNullOrEmpty(authResult.AccessToken))
{
    httpClient.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
}

// 5. Refresh tokens when needed
var newTokens = await authHandler.RefreshToken(authResult.RefreshToken);
```

---

**Next**: [Implementation Guides](authentication-guides.md) - Common authentication scenarios and integration patterns