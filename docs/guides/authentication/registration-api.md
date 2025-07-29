# Registration API Implementation Guide

Learn how to implement user registration using the new direct API→Handler pattern introduced in v2.1.2.

## Overview

The Registration API now follows a simplified pattern where the API Function directly calls the Handler, eliminating the need for an intermediate System layer for simple operations.

## Architecture

### Direct API→Handler Pattern

```
┌─────────────────┐
│ RegisterFunction│ ─── HTTP Request ──→ 
└────────┬────────┘
         │ Direct Call (No System)
         ▼
┌─────────────────┐
│ AccountHandler  │ ─── Component Ops ──→ Database
└─────────────────┘
```

## Implementation

### 1. Register Function

```csharp
[Function("RegisterUser")]
public class RegisterFunction
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<RegisterFunction> _logger;

    public RegisterFunction(
        IDataContext dataContext,
        ILogger<RegisterFunction> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    [Function("RegisterUser")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] 
        HttpRequestData req)
    {
        try
        {
            // Parse request
            var requestBody = await req.ReadAsStringAsync();
            var accountComponent = JsonSerializer.Deserialize<Account>(
                requestBody, 
                JsonOptions.Default);

            if (accountComponent == null)
            {
                return await CreateBadRequest(req, "Invalid request body");
            }

            // Create new entity for the user
            var entityId = Guid.NewGuid();
            
            // Direct handler call - no system layer
            var accountHandler = _dataContext.For<AccountHandler>(entityId);
            var registeredAccount = await accountHandler.Register(accountComponent);

            // Return success response
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(registeredAccount, JsonOptions.Default);
            return response;
        }
        catch (BusinessRuleException ex) when (ex.Code == "EMAIL_EXISTS")
        {
            return await CreateBadRequest(req, "Email already registered");
        }
        catch (ValidationException ex)
        {
            return await CreateValidationError(req, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
}
```

### 2. Account Handler Implementation

```csharp
public class AccountHandler : ComponentHandler<Account>
{
    private readonly IPasswordService _passwordService;
    
    public AccountHandler(
        IDataContext dataContext,
        ILogger<AccountHandler> logger,
        IServiceProvider serviceProvider)
        : base(dataContext, logger)
    {
        _passwordService = serviceProvider.GetRequiredService<IPasswordService>();
    }

    public async Task<Account> Register(Account accountComponent)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(accountComponent.Email))
        {
            throw new ValidationException("Email is required");
        }

        if (string.IsNullOrWhiteSpace(accountComponent.Password))
        {
            throw new ValidationException("Password is required");
        }

        // Check if email already exists
        var existingAccounts = await DataContext.Query()
            .With<Account>(a => a.Email == accountComponent.Email)
            .ToEntityComponents();

        if (existingAccounts.Any())
        {
            throw new BusinessRuleException("EMAIL_EXISTS", "An account with this email already exists");
        }

        // Hash password
        var passwordHash = _passwordService.HashPassword(accountComponent.Password);

        // Create account (inactive by default)
        var account = accountComponent with
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = OwnerEntityId,
            PasswordHash = passwordHash,
            Password = string.Empty, // Clear plain password
            IsActive = false, // Security: require manual activation
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(account);

        _logger.LogInformation("Account registered for {Email}", account.Email);

        return account;
    }
}
```

### 3. Dependency Injection Setup

```csharp
// In Program.cs
public class Program
{
    public static void Main()
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureServices((context, services) =>
            {
                // Register Jarvis
                services.RegisterJarvis(LogLevel.Information, context.Configuration);

                // Register handlers - BOTH registrations required
                services.AddScoped<IComponentHandler, AccountHandler>();
                services.AddScoped<AccountHandler>();

                // Register function
                services.AddScoped<RegisterFunction>();

                // Register services
                services.AddScoped<IPasswordService, PasswordService>();
            })
            .Build();

        host.Run();
    }
}
```

## Usage Examples

### Basic Registration

```bash
curl -X POST http://localhost:7071/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "newuser@example.com",
    "password": "SecurePassword123!"
  }'
```

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
  "email": "newuser@example.com",
  "passwordHash": "$2a$12$...",
  "password": "",
  "isActive": false,
  "createdAt": "2025-01-29T10:00:00Z",
  "lastUpdated": "2025-01-29T10:00:00Z"
}
```

### Frontend Integration

```javascript
async function registerUser(email, password) {
    try {
        const response = await fetch('/api/auth/register', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ email, password })
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Registration failed');
        }

        const account = await response.json();
        console.log('Registration successful:', account);
        
        // Note: Account is inactive, inform user
        alert('Registration successful! Please wait for account activation.');
        
        return account;
    } catch (error) {
        console.error('Registration error:', error);
        throw error;
    }
}
```

### React Hook Example

```typescript
import { useState } from 'react';

interface RegistrationData {
    email: string;
    password: string;
}

export function useRegistration() {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const register = async (data: RegistrationData) => {
        setLoading(true);
        setError(null);

        try {
            const response = await fetch('/api/auth/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Registration failed');
            }

            const account = await response.json();
            return { success: true, account };
        } catch (err) {
            const message = err instanceof Error ? err.message : 'Unknown error';
            setError(message);
            return { success: false, error: message };
        } finally {
            setLoading(false);
        }
    };

    return { register, loading, error };
}
```

## Error Handling

### Common Errors

| Error | Status Code | Response |
|-------|-------------|----------|
| Email already exists | 400 | `{"message": "Email already registered"}` |
| Missing email | 400 | `{"errors": {"email": ["Email is required"]}}` |
| Weak password | 400 | `{"errors": {"password": ["Password must be at least 8 characters"]}}` |
| Server error | 500 | Generic error (check logs) |

### Validation Errors

The API returns detailed validation errors:

```json
{
  "errors": {
    "email": ["Email is required", "Invalid email format"],
    "password": ["Password must be at least 8 characters", "Password must contain numbers"]
  }
}
```

## Security Considerations

### 1. Inactive by Default

New accounts are created as inactive:
```csharp
IsActive = false // Requires manual activation
```

This prevents:
- Automated bot registrations
- Unverified email accounts
- Mass account creation attacks

### 2. Password Security

Passwords are hashed using BCrypt:
```csharp
var passwordHash = _passwordService.HashPassword(accountComponent.Password);
```

- Cost factor 12 (configurable)
- Unique salt per password
- Plain password never stored

### 3. Email Uniqueness

The handler checks for existing emails:
```csharp
if (existingAccounts.Any())
{
    throw new BusinessRuleException("EMAIL_EXISTS", "An account with this email already exists");
}
```

### 4. Input Validation

All inputs are validated:
- Email format validation
- Password policy enforcement
- SQL injection prevention
- XSS protection

## Testing

### Unit Test Example

```csharp
public class RegisterFunctionTests
{
    [Fact]
    public async Task Register_ValidInput_ShouldCreateAccount()
    {
        // Arrange
        var dataContext = CreateTestDataContext();
        var function = new RegisterFunction(dataContext, NullLogger<RegisterFunction>.Instance);
        
        var request = CreateHttpRequest(new
        {
            email = "test@example.com",
            password = "TestPassword123!"
        });

        // Act
        var response = await function.Run(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        var account = await ReadResponse<Account>(response);
        account.Email.ShouldBe("test@example.com");
        account.IsActive.ShouldBeFalse();
    }
}
```

### Integration Test Example

```csharp
public class RegistrationIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task FullRegistrationFlow_ShouldWork()
    {
        // Arrange
        var client = CreateClient();
        
        // Act - Register
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "integration@example.com",
            password = "IntegrationTest123!"
        });

        // Assert
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        var account = await registerResponse.Content.ReadFromJsonAsync<Account>();
        account.ShouldNotBeNull();
        account.IsActive.ShouldBeFalse();
        
        // Verify in database
        var dbAccount = await TestDataContext().Query()
            .With<Account>(a => a.Email == "integration@example.com")
            .ToEntityComponents();
            
        dbAccount.ShouldNotBeEmpty();
    }
}
```

## Best Practices

### 1. Always Validate Input

```csharp
if (string.IsNullOrWhiteSpace(accountComponent.Email))
{
    throw new ValidationException("Email is required");
}
```

### 2. Use Business Exceptions

```csharp
throw new BusinessRuleException("EMAIL_EXISTS", "An account with this email already exists");
```

### 3. Clear Sensitive Data

```csharp
Password = string.Empty // Never return plain password
```

### 4. Log Important Events

```csharp
_logger.LogInformation("Account registered for {Email}", account.Email);
```

### 5. Handle Errors Gracefully

```csharp
catch (BusinessRuleException ex) when (ex.Code == "EMAIL_EXISTS")
{
    return await CreateBadRequest(req, "Email already registered");
}
```

## Migration from System Pattern

If migrating from the old System→Handler pattern:

**Before (with System):**
```csharp
var registrationSystem = new RegistrationSystem(dataContext);
var result = await registrationSystem.RegisterUser(request);
```

**After (direct Handler):**
```csharp
var accountHandler = dataContext.For<AccountHandler>(entityId);
var account = await accountHandler.Register(accountComponent);
```

Benefits:
- Simpler architecture
- Less code to maintain
- Direct testing of handlers
- Better performance

## Next Steps

- [Account Activation Guide](account-activation.md)
- [Login Implementation](login-implementation.md)
- [Email Verification](email-verification.md)
- [Admin Account Management](admin-management.md)

---

**Related:** [Authentication Architecture](/docs/architecture/authentication/) | [API Reference](/docs/api-reference/authentication/)