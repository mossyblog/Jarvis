# ECS Principles for Jarvis API

This document outlines the strict Entity Component System (ECS) principles that MUST be followed in the Jarvis API layer.

## Core Principles

### 1. Components Only - No DTOs, No Anonymous Objects

The API receives components and returns components. There are NO Data Transfer Objects (DTOs) or anonymous objects.

**✅ Correct:**
```csharp
// Receive component
var auth = JsonSerializer.Deserialize<Auth>(requestBody);

// Return component
await response.WriteStringAsync(JsonSerializer.Serialize(auth));
```

**❌ Wrong:**
```csharp
// Never create anonymous objects
var result = new { id = entity.Id, name = entity.Name }; // WRONG!

// Never create DTOs
public class AuthRequest { } // WRONG!
public class AuthResponse { } // WRONG!
```

### 2. Entity-Bound Handlers Only

All handlers MUST extend `ComponentHandler<T>` and be bound to entities.

**✅ Correct:**
```csharp
public class AuthHandler : ComponentHandler<Auth>
{
    public AuthHandler(IDataContext dataContext, ILogger<AuthHandler> logger)
        : base(dataContext, logger)
    {
    }
    
    public async Task<Auth> Authenticate()
    {
        var auth = await GetOrDefault();
        // Process the component bound to this entity
        return auth;
    }
}
```

**❌ Wrong:**
```csharp
public class AuthService // WRONG - service pattern
{
    public async Task<AuthResponse> Authenticate(AuthRequest request) // WRONG - DTOs
    {
    }
}
```

### 3. No Handler Injection in Functions

Functions MUST use `IDataContext.For<THandler>(entityId)` to get handlers.

**✅ Correct:**
```csharp
public class AuthFunction
{
    private readonly IDataContext _dataContext;
    
    public AuthFunction(IDataContext dataContext, ILogger<AuthFunction> logger)
    {
        _dataContext = dataContext;
    }
    
    public async Task<HttpResponseData> Run(HttpRequestData req)
    {
        var auth = JsonSerializer.Deserialize<Auth>(requestBody);
        
        // Create entity and bind handler
        var entityId = Guid.NewGuid();
        auth.OwnerEntityId = entityId;
        await _dataContext.Commit(auth);
        
        var handler = _dataContext.For<AuthHandler>(entityId);
        var result = await handler.Authenticate();
    }
}
```

**❌ Wrong:**
```csharp
public class AuthFunction
{
    private readonly AuthHandler _authHandler; // WRONG - injected handler
    
    public AuthFunction(AuthHandler authHandler) // WRONG
    {
        _authHandler = authHandler;
    }
}
```

### 4. Components Are The API Contract

Components serve as both input and output. NO SPLITTING into request/response patterns.

**✅ Correct:**
```csharp
// Auth component serves as both input (email/password) and output (tokens)
public record Auth : IComponent
{
    // Input fields
    public string Email { get; init; }
    public string Password { get; init; }
    
    // Output fields
    public string AccessToken { get; init; }
    public string RefreshToken { get; init; }
}
```

**❌ Wrong:**
```csharp
// Never split into request/response
public record AuthRequest : IComponent { } // WRONG!
public record AuthResponse : IComponent { } // WRONG!
```

### 5. Error Handling via Components

Errors are returned as Error components, not anonymous objects.

**✅ Correct:**
```csharp
var error = new Error
{
    OwnerEntityId = Guid.NewGuid(),
    Code = "AUTH_FAILED",
    Message = "Invalid credentials",
    StatusCode = 401
};

await response.WriteStringAsync(JsonSerializer.Serialize(error));
```

**❌ Wrong:**
```csharp
// Never return anonymous error objects
await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "message" })); // WRONG!
```

### 6. No Data Transformation in API Layer

The API returns components exactly as they are. No shaping for UI.

**✅ Correct:**
```csharp
// Return UserProfile component directly
var userProfile = await handler.Get();
await response.WriteStringAsync(JsonSerializer.Serialize(userProfile));
```

**❌ Wrong:**
```csharp
// Never transform for UI
var uiResponse = new {
    id = userProfile.Id,
    displayName = userProfile.Name, // WRONG - renaming fields
    formattedRoles = FormatRoles(userProfile.RoleIds) // WRONG - formatting
};
```

### 7. Handler Method Signatures

Handler methods should prefer components as parameters:
- IComponent implementations (preferred for domain operations)
- Guid (only for lookups or when component doesn't exist yet)
- Primitives (bool, int, etc.) only for simple queries
- Collections of the above

**✅ Correct:**
```csharp
public async Task<Auth> Authenticate()
public async Task<Role> GrantPermission(Permission permission)  // Component parameter
public async Task<UserProfile> AssignRole(Role role)           // Component parameter
public async Task<bool> HasPermission(Permission permission)    // Component for checking
```

**❌ Wrong:**
```csharp
public async Task<Role> GrantPermission(Guid permissionId)     // WRONG - use component
public async Task<Role> GrantPermission(string permissionName) // WRONG - use component
public async Task<(string token, DateTime expiry)> Authenticate() // WRONG - tuple
```

### 7.1 Handlers Are Intention Surfaces, Not Data Access Layers

**FUNDAMENTAL PRINCIPLE:** Handlers are NOT data access layers. They're intention surfaces. Every method on a handler should represent a domain operation the developer wants to perform on the component — not a database verb.

**Why This Matters:**
- Handlers express WHAT the developer wants to do, not HOW data is stored
- Method names should read like business operations, not SQL commands
- The handler encapsulates all the complex logic needed to fulfill that intention
- Handlers own the complete operation including persistence decisions

**✅ Correct - Domain Intentions:**
```csharp
public class UserHandler : ComponentHandler<User>
{
    public async Task<User> Ban() { }                      // Domain intention: ban this user
    public async Task<User> Reactivate() { }              // Domain intention: reactivate
    public async Task<User> LinkSocialAccount(SocialAccount account) { } // Takes component as parameter
}

public class PaymentHandler : ComponentHandler<Payment>
{
    public async Task<Payment> ProcessRefund() { }        // Domain intention: refund this payment
    public async Task<Payment> MarkAsFraudulent() { }     // Domain intention: flag as fraud
    public async Task<Payment> ReleaseHold() { }          // Domain intention: release held funds
}

public class RoleHandler : ComponentHandler<Role>
{
    public async Task<Role> GrantPermission(Permission permission) { }  // Takes component
    public async Task<Role> RevokePermission(Permission permission) { } // Takes component
}
```

**❌ Wrong - Database Verbs:**
```csharp
public class UserHandler : ComponentHandler<User>
{
    public async Task<User> Create() { }     // WRONG - database verb
    public async Task<User> Update() { }     // WRONG - database verb
    public async Task<User> Insert() { }     // WRONG - database verb
    public async Task<User> Delete() { }     // WRONG - database verb
    public async Task<User> Upsert() { }     // WRONG - database verb
}
```

**The Litmus Test:**
Ask yourself: "Am I DOING SOMETHING TO an entity, or am I STORING SOMETHING ABOUT it?"
- `BanUser()` = Doing something TO the user ✅
- `Update()` = Storing something ABOUT the user ❌
- `GrantAccess()` = Doing something TO the entity ✅
- `Insert()` = Storing something ABOUT the entity ❌

**Note on Save():**
If a `Save()` method only calls `Commit()` without any domain logic, it's redundant. Handlers should auto-commit when they mutate state. Only include `Save()` if it performs additional domain logic beyond simple persistence.

**The Golden Rule:**
"I'm doing something TO an entity, not storing something ABOUT it."

### 7.2 Handlers Control Mutation Through Domain Operations

**FUNDAMENTAL PRINCIPLE:** Handlers' SOLE job is to control mutation through well-defined domain operations. They encapsulate the business logic of HOW components should be mutated. Handlers own the complete operation including persistence.

Components represent the current state of an entity. Handlers provide the operations to change that state correctly:

**✅ Correct Pattern - Handler Controls Mutation:**
```csharp
// Handler encapsulates domain logic and persistence
public class UserProfileHandler : ComponentHandler<UserProfile>
{
    public async Task<UserProfile> AssignRole(Role role)  // Takes component, not primitive
    {
        var profile = await GetOrDefault() ?? throw new InvalidOperationException("Profile not found");
        
        // Handler controls the mutation logic
        var roleIds = profile.RoleIds.ToList();
        if (!roleIds.Contains(role.Id.ToString()))
        {
            roleIds.Add(role.Id.ToString());
        }
        
        // Calculate permissions based on new roles
        var permissions = await CalculatePermissionsForRoles(roleIds);
        
        // Handler performs the mutation AND persistence
        var updated = profile with 
        { 
            RoleIds = roleIds.ToArray(),
            PermissionIds = permissions.ToArray(),
            UpdatedAt = DateTime.UtcNow
        };
        
        await DataContext.Commit(updated);  // Handler owns persistence
        return updated;
    }
}

// Function just calls the domain operation
var handler = _dataContext.For<UserProfileHandler>(userId);
var updated = await handler.AssignRole(role);  // Pass component
```

**❌ Wrong Pattern - Function Doing Orchestration:**
```csharp
// WRONG - Function calculating and mutating
var profile = await GetUserProfile(userId);
var roleIds = profile.RoleIds.ToList();
roleIds.Add(newRoleId);
var permissions = CalculatePermissions(roleIds); // WRONG - orchestration in function
profile = profile with { RoleIds = roleIds, PermissionIds = permissions };
await _dataContext.Commit(profile);
```

**Remember:** 
- **Components** = State
- **Handlers** = Control mutation through domain operations, own persistence
- **Functions** = Call handler operations, don't orchestrate
- **If you're calling Commit() outside a handler, you're missing a verb**

### 7.3 The Missing Verb Principle

**FUNDAMENTAL PRINCIPLE:** If developers find themselves calling `Commit()` outside of a handler, it's a clear sign that a handler is missing a domain verb.

**Why This Matters:**
- Every meaningful state change should be expressed as a domain operation
- Handlers should encapsulate complete operations, not expose partial steps
- Direct commits bypass business logic and validation

**❌ Wrong - Commit outside handler:**
```csharp
// In a function or elsewhere
var user = await handler.Get();
user = user with { Status = "banned", BannedAt = DateTime.UtcNow };
await _dataContext.Commit(user);  // WRONG - missing Ban() verb on handler
```

**✅ Correct - Handler owns the verb:**
```csharp
// Handler has the domain verb
public class UserHandler : ComponentHandler<User>
{
    public async Task<User> Ban()
    {
        var user = await GetOrDefault() ?? throw new InvalidOperationException("User not found");
        var banned = user with 
        { 
            Status = "banned", 
            BannedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow 
        };
        await DataContext.Commit(banned);  // Handler owns the commit
        return banned;
    }
}

// Function just calls the verb
var handler = _dataContext.For<UserHandler>(userId);
var result = await handler.Ban();  // Clean, intention-revealing API
```

### 8. Entity IDs Are The Only References

Components store entity IDs, not object references or foreign keys.

**✅ Correct:**
```csharp
public record UserProfile : IComponent
{
    public string[] RoleIds { get; init; } // Entity IDs as strings
    public string[] PermissionIds { get; init; } // Denormalized for performance
}
```

**❌ Wrong:**
```csharp
public record UserProfile : IComponent
{
    public List<Role> Roles { get; init; } // WRONG - object references
    public int RoleId { get; init; } // WRONG - foreign key pattern
}
```

## Example: Correct Authentication Flow

```csharp
// 1. Function receives Auth component
var auth = JsonSerializer.Deserialize<Auth>(requestBody);

// 2. Create entity and get handler in one operation
var entityId = Guid.NewGuid();
auth.OwnerEntityId = entityId;
var handler = _dataContext.For<AuthHandler>(entityId);

// 3. Handler owns the complete operation including persistence
var result = await handler.Authenticate(auth);  // Pass component to handler

// 4. Return the component (handler already persisted it)
await response.WriteStringAsync(JsonSerializer.Serialize(result));
```

## Summary

- **NO DTOs** - Only components
- **NO Services** - Only entity-bound handlers
- **NO Anonymous Objects** - Only components or entity IDs
- **NO Transformation** - Return components as-is
- **NO Injection** - Use IDataContext.For<>()
- **NO Splitting** - One component per concept
- **NO Foreign Keys** - Only entity ID references