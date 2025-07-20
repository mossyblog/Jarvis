# Azure Functions API Development Guidelines

## Core Principle: Ultra-Thin API Layer

The API layer (Azure Functions) must be **ultra-thin** with absolutely **NO business logic**. All business logic belongs in handlers accessed through the System layer.

## The System Pattern

### Purpose
The System layer acts as a middleware between Azure Functions and handlers, ensuring:
- Complete separation of HTTP concerns from business logic
- Testability of business logic without Azure Functions runtime
- Enforcement of handler-based architecture
- Prevention of direct DataContext usage in API layer

### Architecture Flow
```
Azure Function → System → Handler → Components
     ↑                        ↓
     └── HTTP Response ← ─────┘
```

## ✅ GOOD: Correct Usage

### 1. Simple Operations - Direct Handler Method Calls

```csharp
// GOOD: Get a component
var profile = await _system.ExecuteHandler<AccountProfileHandler, SecurityProfile>(
    userId,
    handler => handler.Get());

// GOOD: Create something
var role = await _system.ExecuteHandler<RoleHandler, Role>(
    entityId,
    handler => handler.CreateWithPermissions(name, description, permissionIds));

// GOOD: Update state
await _system.ExecuteHandler<AccountHandler>(
    accountId,
    handler => handler.Deactivate());

// GOOD: Get non-component results
var navigation = await _system.ExecuteHandlerWithResult<AccountProfileHandler, List<NavigationItem>>(
    userId,
    handler => handler.GetUserNavigation());
```

### 2. Handler Methods Encapsulate ALL Logic

```csharp
// GOOD: Handler method that handles complex logic
public class AuthHandler : ComponentHandler<AuthToken>
{
    public async Task<AuthToken> AuthenticateFromJson(string requestBody, string? ipAddress, string? userAgent)
    {
        // Parse JSON
        var request = JsonSerializer.Deserialize<AuthRequest>(requestBody);
        
        // Validate
        if (string.IsNullOrEmpty(request?.Email) || string.IsNullOrEmpty(request?.Password))
        {
            throw new ValidationException(...);
        }
        
        // Find account
        var account = await FindAccountByEmail(request.Email);
        
        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
        {
            await _auditService.LogFailedLogin(...);
            throw new UnauthorizedException(...);
        }
        
        // Create session
        var token = await CreateAuthToken(account.Id, ipAddress, userAgent);
        
        return token;
    }
}

// Azure Function just calls the handler method
var authToken = await _system.ExecuteHandler<AuthHandler, AuthToken>(
    authEntityId,
    handler => handler.AuthenticateFromJson(requestBody, ipAddress, userAgent));
```

### 3. Handler Methods for Complex Queries

```csharp
// GOOD: Handler encapsulates navigation filtering logic
public class AccountProfileHandler : ComponentHandler<SecurityProfile>
{
    public async Task<List<NavigationItem>> GetUserNavigation()
    {
        var profile = await GetOrDefault();
        if (profile == null) return new List<NavigationItem>();
        
        // Complex query logic
        var allNavItems = await DataContext.Query()
            .WithAll<NavigationItem>(n => true)
            .ToEntityComponents();
            
        // Business logic for filtering
        var navigation = new List<NavigationItem>();
        foreach (var entity in allNavItems)
        {
            var navItem = entity.Value.Get<NavigationItem>();
            if (navItem != null && CanUserAccess(navItem, profile))
            {
                navigation.Add(navItem);
            }
        }
        
        return navigation;
    }
    
    private bool CanUserAccess(NavigationItem item, SecurityProfile profile)
    {
        return !item.RequiredPermissionId.HasValue || 
               profile.PermissionIds.Contains(item.RequiredPermissionId.Value.ToString());
    }
}

// Azure Function just calls it
var navigation = await _system.ExecuteHandlerWithResult<AccountProfileHandler, List<NavigationItem>>(
    userId,
    handler => handler.GetUserNavigation());
```

## ❌ BAD: Incorrect Usage

### 1. Business Logic in Lambda

```csharp
// BAD: Logic inside the lambda
var result = await _system.ExecuteHandler<SomeHandler, SomeComponent>(
    entityId,
    async handler => {
        // ❌ NEVER DO THIS - Logic in lambda
        var data = await handler.Get();
        if (data.Status == "active")
        {
            data.LastAccessed = DateTime.UtcNow;
            await handler.Update(data);
        }
        return data;
    });

// GOOD: Create a handler method instead
public async Task<SomeComponent> GetAndUpdateLastAccessed()
{
    var data = await Get();
    if (data?.Status == "active")
    {
        var updated = data with { LastAccessed = DateTime.UtcNow };
        await DataContext.Commit(updated);
        return updated;
    }
    return data;
}

// Then call it
var result = await _system.ExecuteHandler<SomeHandler, SomeComponent>(
    entityId,
    handler => handler.GetAndUpdateLastAccessed());
```

### 2. Direct DataContext Usage in Functions

```csharp
// BAD: Using DataContext in Function
public class BadFunction
{
    private readonly IDataContext _dataContext; // ❌ NEVER inject DataContext
    
    public async Task<HttpResponseData> BadEndpoint(...)
    {
        // ❌ NEVER query directly
        var items = await _dataContext.Query()
            .WithAll<SomeComponent>(x => x.IsActive)
            .ToEntityComponents();
            
        // ❌ NEVER commit directly
        await _dataContext.Commit(new SomeComponent { ... });
    }
}

// GOOD: Remove DataContext, use System only
public class GoodFunction
{
    private readonly ISystem _system; // ✅ Only inject System
    
    public async Task<HttpResponseData> GoodEndpoint(...)
    {
        var items = await _system.ExecuteHandlerWithResult<SomeHandler, List<SomeComponent>>(
            entityId,
            handler => handler.GetActiveItems());
    }
}
```

### 3. Complex Logic in Functions

```csharp
// BAD: Business logic in Function
public async Task<HttpResponseData> UpdateUserProfile(...)
{
    var requestBody = await req.ReadAsStringAsync();
    var updateProfile = JsonSerializer.Deserialize<SecurityProfile>(requestBody);
    
    // ❌ Validation logic in Function
    if (string.IsNullOrEmpty(updateProfile.Name))
    {
        updateProfile.Name = "Default Name";
    }
    
    // ❌ Business rules in Function
    if (updateProfile.RoleIds.Length > 5)
    {
        throw new Exception("Too many roles");
    }
    
    // ❌ Direct update
    updateProfile.OwnerEntityId = userId;
    await _dataContext.Commit(updateProfile);
}

// GOOD: All logic in handler
public async Task<SecurityProfile> UpdateProfile(ProfileUpdateRequest request)
{
    // All validation in handler
    Guard.AgainstEmpty(request.Name, nameof(request.Name));
    
    // All business rules in handler
    if (request.RoleIds?.Length > 5)
    {
        throw new ValidationException("Maximum 5 roles allowed");
    }
    
    var profile = await GetRequired();
    var updated = profile with
    {
        Name = request.Name,
        RoleIds = request.RoleIds ?? profile.RoleIds,
        UpdatedAt = DateTime.UtcNow
    };
    
    await DataContext.Commit(updated);
    return updated;
}
```

## Handler Method Guidelines

### 1. One Method Per Business Operation

```csharp
// GOOD: Clear, focused methods
public class OrderHandler : ComponentHandler<OrderComponent>
{
    public async Task<OrderComponent> CreateDraft(CreateOrderRequest request) { }
    public async Task<OrderComponent> Submit() { }
    public async Task<OrderComponent> Approve(Guid approverId) { }
    public async Task<OrderComponent> Ship(ShippingInfo info) { }
    public async Task<OrderComponent> Cancel(string reason) { }
    public async Task<List<OrderComponent>> GetPendingOrders() { }
    public async Task<OrderStats> GetStatistics(DateRange range) { }
}
```

### 2. Handler Methods Handle Everything

```csharp
// GOOD: Complete encapsulation
public async Task<AuthToken> RefreshToken(string refreshToken)
{
    // 1. Validate token
    if (string.IsNullOrEmpty(refreshToken))
        throw new ValidationException("Refresh token required");
    
    // 2. Find existing session
    var sessions = await DataContext.Query()
        .WithAll<AuthToken>(t => t.RefreshToken == refreshToken)
        .ToEntityComponents();
    
    // 3. Verify session
    var session = sessions.FirstOrDefault().Value?.Get<AuthToken>();
    if (session == null || session.IsRevoked)
        throw new UnauthorizedException("Invalid refresh token");
    
    // 4. Check expiration
    if (session.RefreshExpiresAt < DateTime.UtcNow)
        throw new UnauthorizedException("Refresh token expired");
    
    // 5. Generate new tokens
    var newAccessToken = GenerateAccessToken(session.AccountId);
    var newRefreshToken = GenerateRefreshToken();
    
    // 6. Update session
    var updated = session with
    {
        AccessToken = newAccessToken,
        RefreshToken = newRefreshToken,
        ExpiresAt = DateTime.UtcNow.AddHours(1),
        RefreshExpiresAt = DateTime.UtcNow.AddDays(30),
        UpdatedAt = DateTime.UtcNow
    };
    
    await DataContext.Commit(updated);
    
    // 7. Audit
    await _auditService.LogTokenRefresh(session.AccountId);
    
    return updated;
}
```

## Testing Benefits

### Unit Testing Handlers Without API Layer

```csharp
[Fact]
public async Task Account_RegistrationFlow_WorksCorrectly()
{
    // Arrange
    var system = new HandlerSystem(TestDataContext(), NullLogger<HandlerSystem>.Instance);
    
    // Act - Test complete workflow without HTTP concerns
    var registrationResult = await system.ExecuteHandlerWithResult<RegistrationHandler, RegistrationResult>(
        Guid.Empty,
        handler => handler.RegisterFromJson(requestJson, "127.0.0.1"));
    
    var account = await TestDataContext().For<AccountHandler>(registrationResult.AccountId).Get();
    var profile = await TestDataContext().For<AccountProfileHandler>(registrationResult.AccountId).Get();
    
    // Assert
    registrationResult.Success.ShouldBeTrue();
    account.Email.ShouldBe("test@example.com");
    profile.Name.ShouldNotBeEmpty();
}
```

### Testing Through System Layer

```csharp
[Fact]
public async Task System_ExecutesHandlerMethods_Correctly()
{
    // Arrange
    var system = Services.GetRequiredService<ISystem>();
    var entityId = Guid.NewGuid();
    
    // Act - Test through System without HTTP
    var role = await system.ExecuteHandler<RoleHandler, Role>(
        entityId,
        handler => handler.CreateRole("Test Role", "Test Description"));
    
    // Assert
    role.ShouldNotBeNull();
    role.Name.ShouldBe("Test Role");
}
```

## Common Anti-Patterns to Avoid

### 1. "Helper" Methods in Functions
```csharp
// ❌ BAD: Helper methods indicate logic that belongs in handlers
public class BadFunction
{
    private bool ValidateUserPermissions(SecurityProfile profile, string resource)
    {
        // This logic belongs in a handler!
    }
    
    private async Task<List<Item>> FilterItemsByUser(List<Item> items, Guid userId)
    {
        // This logic belongs in a handler!
    }
}
```

### 2. Multi-Step Operations in Lambdas
```csharp
// ❌ BAD: Complex multi-step logic
await _system.ExecuteHandler<Handler>(id, async handler => {
    var data = await handler.Get();
    var processed = ProcessData(data); // Where is this method?
    var validated = ValidateData(processed); // This shouldn't be here!
    return await handler.Update(validated);
});

// ✅ GOOD: Single handler method call
await _system.ExecuteHandler<Handler>(id, 
    handler => handler.ProcessAndValidate());
```

### 3. Conditional Logic Based on Request Data
```csharp
// ❌ BAD: Decision logic in Function
if (request.Type == "express")
{
    await _system.ExecuteHandler<OrderHandler>(id, h => h.SetExpressShipping());
}
else
{
    await _system.ExecuteHandler<OrderHandler>(id, h => h.SetStandardShipping());
}

// ✅ GOOD: Let handler decide
await _system.ExecuteHandler<OrderHandler>(id, 
    handler => handler.SetShippingType(request.Type));
```

## Summary Rules

1. **Azure Functions = HTTP adapter only** (parse request, call System, return response)
2. **System = Handler orchestrator only** (no business logic)
3. **Handlers = ALL business logic** (validation, queries, updates, rules)
4. **No DataContext in Functions** (remove the dependency entirely)
5. **No logic in lambdas** (only direct method calls)
6. **One handler method per business operation** (clear, testable, reusable)
7. **Handler methods are self-contained** (handle everything internally)

## Red Flags in Code Review

- 🚩 `IDataContext` injected into any Function
- 🚩 Lambda expressions with more than one line
- 🚩 `if` statements in Functions (except for auth checks)
- 🚩 `foreach` loops in Functions
- 🚩 Direct `JsonSerializer.Deserialize` for business objects (should be in handler)
- 🚩 Any calculation or transformation in Functions
- 🚩 Functions longer than ~50 lines (excluding error handling)
- 🚩 Helper methods in Function classes
- 🚩 `using core.jarvis.Data` in any Function file

Remember: If you're writing code in an Azure Function and wondering "where should this logic go?" - the answer is ALWAYS "in a handler method".