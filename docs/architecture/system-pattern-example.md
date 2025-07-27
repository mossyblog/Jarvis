# System Pattern Implementation Example

This document demonstrates the CORRECT way to implement the System pattern with a complete example.

## Example: User Registration Flow

### ❌ WRONG WAY (What NOT to do)

```csharp
// BAD: Function with business logic
public class BadAuthFunction
{
    private readonly IDataContext _dataContext; // ❌ NO!
    private readonly ISystem _system;
    
    [Function("Register")]
    public async Task<HttpResponseData> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequestData req)
    {
        var requestBody = await req.ReadAsStringAsync();
        var request = JsonSerializer.Deserialize<RegisterRequest>(requestBody);
        
        // ❌ Validation logic in function
        if (string.IsNullOrEmpty(request.Email) || !request.Email.Contains("@"))
        {
            return CreateBadRequest(req, "Invalid email");
        }
        
        // ❌ Direct database query in function
        var existing = await _dataContext.Query()
            .WithAll<Account>(a => a.Email == request.Email)
            .ToEntityComponents();
            
        if (existing.Any())
        {
            return CreateBadRequest(req, "Email already exists");
        }
        
        // ❌ Complex logic in lambda
        var result = await _system.ExecuteHandler<AccountHandler, Account>(
            Guid.NewGuid(),
            async handler => {
                // ❌ Business logic in lambda
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                var account = new Account
                {
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                
                await _dataContext.Commit(account); // ❌ Direct commit
                
                // ❌ More business logic
                var profile = new SecurityProfile
                {
                    OwnerEntityId = account.OwnerEntityId,
                    Name = request.Email.Split('@')[0]
                };
                
                await _dataContext.Commit(profile); // ❌ Another direct commit
                
                return account;
            });
        
        return CreateOkResponse(req, result);
    }
}
```

### ✅ CORRECT WAY

#### 1. The Handler (where ALL logic belongs)

```csharp
// GOOD: Handler with all business logic
public class RegistrationHandler : IComponentHandler
{
    private readonly IDataContext _dataContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<RegistrationHandler> _logger;
    
    public Guid OwnerEntityId { get; set; }
    
    public RegistrationHandler(
        IDataContext dataContext,
        IAuditService auditService,
        ILogger<RegistrationHandler> logger)
    {
        _dataContext = dataContext;
        _auditService = auditService;
        _logger = logger;
    }
    
    /// <summary>
    /// Handles complete user registration flow from JSON input
    /// </summary>
    public async Task<RegistrationResult> RegisterFromJson(string requestBody, string? ipAddress)
    {
        // 1. Parse and validate input
        RegisterRequest request;
        try
        {
            request = JsonSerializer.Deserialize<RegisterRequest>(requestBody) 
                ?? throw new ValidationException("Invalid request body");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in registration request");
            throw new ValidationException("Invalid request format");
        }
        
        // 2. Validate business rules
        ValidateRegistrationRequest(request);
        
        // 3. Check if email already exists
        await CheckEmailAvailability(request.Email);
        
        // 4. Create account
        var account = await CreateAccount(request.Email, request.Password, ipAddress);
        
        // 5. Create security profile
        var profile = await CreateSecurityProfile(account.OwnerEntityId, request.Email, request.FullName);
        
        // 6. Assign default role
        await AssignDefaultRole(profile.OwnerEntityId);
        
        // 7. Send welcome email (or queue it)
        await QueueWelcomeEmail(account.Email, profile.Name);
        
        // 8. Audit the registration
        await _auditService.LogRegistration(account.OwnerEntityId, account.Email, ipAddress);
        
        // 9. Return result
        return new RegistrationResult
        {
            AccountId = account.OwnerEntityId,
            Email = account.Email,
            ProfileId = profile.OwnerEntityId,
            RequiresEmailVerification = true
        };
    }
    
    private void ValidateRegistrationRequest(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["email"] = new[] { "Email is required" };
        }
        else if (!IsValidEmail(request.Email))
        {
            errors["email"] = new[] { "Invalid email format" };
        }
        
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = new[] { "Password is required" };
        }
        else if (request.Password.Length < 8)
        {
            errors["password"] = new[] { "Password must be at least 8 characters" };
        }
        
        if (!string.IsNullOrEmpty(request.FullName) && request.FullName.Length > 100)
        {
            errors["fullName"] = new[] { "Name too long (max 100 characters)" };
        }
        
        if (errors.Any())
        {
            throw new ValidationException(errors);
        }
    }
    
    private async Task CheckEmailAvailability(string email)
    {
        var existing = await _dataContext.Query()
            .WithAll<Account>(a => a.Email.ToLower() == email.ToLower())
            .ToEntityIds();
            
        if (existing.Any())
        {
            _logger.LogInformation("Registration attempt with existing email: {Email}", email);
            throw new BusinessRuleException("Email address is already registered");
        }
    }
    
    private async Task<Account> CreateAccount(string email, string password, string? ipAddress)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = Guid.NewGuid(),
            Email = email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12),
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            IpAddress = ipAddress
        };
        
        await _dataContext.Commit(account);
        _logger.LogInformation("Created account for {Email}", email);
        
        return account;
    }
    
    private async Task<SecurityProfile> CreateSecurityProfile(Guid accountId, string email, string? fullName)
    {
        var profile = new SecurityProfile
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = accountId,
            Name = fullName ?? email.Split('@')[0],
            Avatar = null,
            RoleIds = Array.Empty<string>(),
            PermissionIds = Array.Empty<string>(),
            Preferences = new Dictionary<string, object>(),
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        
        await _dataContext.Commit(profile);
        _logger.LogInformation("Created security profile for account {AccountId}", accountId);
        
        return profile;
    }
    
    private async Task AssignDefaultRole(Guid profileId)
    {
        // Find default role
        var defaultRoles = await _dataContext.Query()
            .WithAll<Role>(r => r.Name == "user")
            .ToEntityComponents();
            
        var defaultRole = defaultRoles.FirstOrDefault().Value?.Get<Role>();
        if (defaultRole == null)
        {
            _logger.LogWarning("Default 'user' role not found");
            return;
        }
        
        // Assign role to profile
        var profileHandler = _dataContext.For<AccountProfileHandler>(profileId);
        await profileHandler.AssignRole(defaultRole.OwnerEntityId);
        
        _logger.LogInformation("Assigned default role to profile {ProfileId}", profileId);
    }
    
    private async Task QueueWelcomeEmail(string email, string name)
    {
        // In real implementation, this would queue to a message bus
        _logger.LogInformation("Queued welcome email for {Email}", email);
        await Task.CompletedTask;
    }
    
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

// Result class for registration
public class RegistrationResult
{
    public Guid AccountId { get; init; }
    public string Email { get; init; } = string.Empty;
    public Guid ProfileId { get; init; }
    public bool RequiresEmailVerification { get; init; }
}
```

#### 2. The Azure Function (ultra-thin, NO logic)

```csharp
// GOOD: Function with NO business logic
public class AuthFunction
{
    private readonly ISystem _system;  // ✅ Only System, no DataContext
    private readonly ILogger<AuthFunction> _logger;
    
    public AuthFunction(
        ISystem system,
        ILogger<AuthFunction> logger)
    {
        _system = system;
        _logger = logger;
    }
    
    [Function("Register")]
    public async Task<HttpResponseData> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequestData req)
    {
        try
        {
            // 1. Get request data
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            var ipAddress = req.Headers.GetValues("X-Forwarded-For").FirstOrDefault() 
                ?? req.FunctionContext.BindingContext.BindingData["ClientIp"]?.ToString();
            
            // 2. Call handler through System - ONE LINE!
            var result = await _system.ExecuteHandlerWithResult<RegistrationHandler, RegistrationResult>(
                Guid.NewGuid(), // New entity for registration
                handler => handler.RegisterFromJson(requestBody, ipAddress));
            
            // 3. Return success response
            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteAsJsonAsync(new
            {
                success = true,
                accountId = result.AccountId,
                email = result.Email,
                requiresEmailVerification = result.RequiresEmailVerification
            });
            
            return response;
        }
        catch (ValidationException vex)
        {
            // Validation errors - return 400
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteAsJsonAsync(new
            {
                error = "VALIDATION_ERROR",
                message = "Validation failed",
                details = vex.Errors
            });
            return response;
        }
        catch (BusinessRuleException brex)
        {
            // Business rule violations - return 409
            var response = req.CreateResponse(HttpStatusCode.Conflict);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteAsJsonAsync(new
            {
                error = "BUSINESS_RULE_VIOLATION",
                message = brex.Message
            });
            return response;
        }
        catch (Exception ex)
        {
            // Unexpected errors - log and return 500
            _logger.LogError(ex, "Registration failed");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteAsJsonAsync(new
            {
                error = "INTERNAL_ERROR",
                message = "Registration failed"
            });
            return response;
        }
    }
}
```

## Key Differences

### 1. Separation of Concerns

**WRONG WAY:**
- Function has 100+ lines of mixed concerns
- Business logic scattered throughout
- Direct database access
- Validation mixed with HTTP handling

**CORRECT WAY:**
- Function has ~50 lines of ONLY HTTP concerns
- Handler has ALL business logic in organized methods
- Database access only through DataContext in handlers
- Clear separation of validation, business rules, and persistence

### 2. Testability

**WRONG WAY:**
```csharp
// How do you test the registration logic?
// You need to mock HttpRequestData, FunctionContext, etc.
// You can't test business logic in isolation
```

**CORRECT WAY:**
```csharp
[Fact]
public async Task Registration_ValidRequest_CreatesAccountAndProfile()
{
    // Arrange
    var handler = TestDataContext().For<RegistrationHandler>(Guid.NewGuid());
    var json = JsonSerializer.Serialize(new
    {
        email = "newuser@example.com",
        password = "SecurePass123!",
        fullName = "New User"
    });
    
    // Act
    var result = await handler.RegisterFromJson(json, "127.0.0.1");
    
    // Assert
    result.ShouldNotBeNull();
    result.Email.ShouldBe("newuser@example.com");
    result.RequiresEmailVerification.ShouldBeTrue();
    
    // Verify account created
    var account = await TestDataContext()
        .For<AccountHandler>(result.AccountId)
        .Get();
    account.ShouldNotBeNull();
    account.Email.ShouldBe("newuser@example.com");
}
```

### 3. Reusability

**WRONG WAY:**
- Logic tied to HTTP context
- Can't reuse registration logic in other contexts (CLI, background jobs, etc.)

**CORRECT WAY:**
- Handler method can be called from anywhere
- Could use same logic in:
  - HTTP API
  - CLI tool
  - Background job
  - Integration from another system
  - Unit tests

### 4. Maintainability

**WRONG WAY:**
- Changes require modifying Azure Function
- Business logic changes mixed with HTTP changes
- Hard to find where specific logic lives

**CORRECT WAY:**
- Azure Function rarely changes
- All business logic in one cohesive handler
- Easy to find and modify business rules
- Can add new business logic without touching Function

## Summary

The System pattern ensures:
1. **Azure Functions** = Thin HTTP adapters (request → handler → response)
2. **System** = Handler orchestrator (no logic)
3. **Handlers** = ALL business logic (cohesive, testable, reusable)

Follow this pattern and your code will be:
- ✅ Testable without HTTP context
- ✅ Reusable across different entry points
- ✅ Maintainable with clear separation
- ✅ Scalable with consistent patterns