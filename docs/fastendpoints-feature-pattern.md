# FastEndpoints Feature Pattern

This document describes the FastEndpoints API design pattern used in the Jarvis framework.

## Philosophy: Components Over DTOs

The Jarvis framework follows a **"Components First"** approach:

- **Default**: Use `IComponent` records directly as request/response types
- **Exception**: Create custom Request/Response classes only when Components don't fit

This minimizes boilerplate and ensures consistency between API contracts and domain models.

## Directory Structure

```
core.jarvis.api/
├── Features/                     # Vertical slices - folder per domain
│   ├── Auth/                     # Authentication domain
│   │   ├── Login/
│   │   │   ├── Endpoint.cs       # POST /security/auth
│   │   │   └── Validator.cs      # FluentValidation rules
│   │   ├── Logout/
│   │   │   └── Endpoint.cs       # POST /security/deauth
│   │   ├── Refresh/
│   │   │   ├── Endpoint.cs       # POST /security/refresh
│   │   │   └── Validator.cs
│   │   ├── Register/
│   │   │   ├── Endpoint.cs       # POST /auth/register
│   │   │   └── Validator.cs
│   │   └── Validate/
│   │       └── Endpoint.cs       # GET+POST /security/validate
│   │
│   ├── Account/                  # Account domain
│   │   ├── Profile/
│   │   │   └── Endpoint.cs       # GET /accounts/me
│   │   ├── ProfileUpdate/
│   │   │   ├── Endpoint.cs       # PUT /accounts/me
│   │   │   └── Validator.cs
│   │   └── Navigation/
│   │       └── Endpoint.cs       # GET /accounts/navigation
│   │
│   ├── Roles/                    # Role management domain
│   │   ├── Index/
│   │   │   └── Endpoint.cs       # GET /security/roles
│   │   ├── Create/
│   │   │   ├── Endpoint.cs       # POST /security/roles
│   │   │   └── Validator.cs
│   │   ├── Update/
│   │   │   ├── Endpoint.cs       # PUT /security/roles/{id}
│   │   │   └── Validator.cs
│   │   └── Defaults/
│   │       └── Endpoint.cs       # POST /security/roles/ensure-defaults
│   │
│   ├── GraphQL/                  # GraphQL endpoint
│   │   ├── Endpoint.cs           # POST /graphql
│   │   └── Validator.cs
│   │
│   └── Health/                   # System health
│       └── Endpoint.cs           # GET /health
│
├── Models/                       # IComponent records (shared)
│   ├── Account.cs
│   ├── AuthToken.cs
│   ├── Role.cs
│   └── SecurityProfile.cs
│
├── Handlers/                     # Business logic handlers
├── Systems/                      # Orchestration systems
├── Services/                     # Cross-cutting services
└── Processors/                   # FastEndpoints pre/post processors
```

## File Conventions

Each endpoint folder may contain:

| File | Required | Purpose |
|------|----------|---------|
| `Endpoint.cs` | **Always** | The HTTP handler |
| `Validator.cs` | When input validation needed | FluentValidation rules |
| `Request.cs` | When Component doesn't fit | Custom request type |
| `Response.cs` | When Component doesn't fit | Custom response type |
| `Mapper.cs` | When Request/Response exists | Type mapping logic |

## Pattern Examples

### Pattern 1: Component as Request AND Response

Most common pattern - use the same Component for input and output:

```csharp
// Features/Roles/Create/Endpoint.cs
public class Endpoint : Endpoint<Role, Role>
{
    public IDataContext DataContext { get; set; } = null!;

    public override void Configure()
    {
        Post("/security/roles");
        Validator<Validator>();
    }

    public override async Task HandleAsync(Role req, CancellationToken ct)
    {
        var roleHandler = DataContext.For<RoleHandler>(Guid.NewGuid());
        var role = await roleHandler.CreateRole(req);
        await SendAsync(role, 201, ct);
    }
}
```

**When to use**: CRUD operations where the domain model IS the API contract.

### Pattern 2: Component Request, Different Component Response

When the input and output are different domain concepts:

```csharp
// Features/Auth/Login/Endpoint.cs
public class Endpoint : Endpoint<Account, AuthToken>
{
    public AuthSystem AuthSystem { get; set; } = null!;

    public override void Configure()
    {
        Post("/security/auth");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Account req, CancellationToken ct)
    {
        req = req with {
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
        };
        var authToken = await AuthSystem.AuthenticateUser(req);
        await SendAsync(authToken, cancellation: ct);
    }
}
```

**When to use**: Operations that transform one domain concept into another.

### Pattern 3: No Request, Component Response

For GET endpoints that return a Component:

```csharp
// Features/Account/Profile/Endpoint.cs
public class Endpoint : EndpointWithoutRequest<SecurityProfile>
{
    public IDataContext DataContext { get; set; } = null!;

    public override void Configure()
    {
        Get("/accounts/me");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirst("sub")?.Value;
        // ... fetch and return SecurityProfile
        await SendAsync(profile, cancellation: ct);
    }
}
```

**When to use**: Resource retrieval where identity comes from JWT claims.

### Pattern 4: Custom Request (Exception Case)

When external protocols require specific shapes that don't match Components:

```csharp
// Features/GraphQL/Endpoint.cs

// Custom request - NOT a Component because it follows GraphQL spec
public class GraphQLRequest
{
    public string Query { get; set; } = string.Empty;
    public object? Variables { get; set; }
    public string? OperationName { get; set; }
}

public class Endpoint : Endpoint<GraphQLRequest, GraphQLResult>
{
    public override void Configure()
    {
        Post("/graphql");
        AllowAnonymous();
    }
    // ...
}
```

**When to use**:
- External API integrations (Stripe webhooks, OAuth callbacks)
- Protocol requirements (GraphQL, WebSocket messages)
- Legacy API compatibility

### Pattern 5: Inline Response (Simple Endpoints)

For trivial responses, define inline within the endpoint file:

```csharp
// Features/Health/Endpoint.cs
public class Endpoint : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        Get("/health");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        return SendAsync(new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        }, cancellation: ct);
    }
}

// OK to define inline for simple, endpoint-specific types
public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
```

**When to use**: System endpoints with simple, endpoint-specific responses.

## Validator Pattern

Validators use FluentValidation and validate against the Component:

```csharp
// Features/Auth/Login/Validator.cs
public class Validator : Validator<Account>
{
    public Validator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
```

Register in endpoint via `Validator<Validator>()` in Configure().

## Permission Attributes

Use `[RequirePermission]` attribute for authorization:

```csharp
[RequirePermission("admin.roles.write")]
public class Endpoint : Endpoint<Role, Role>
{
    // ...
}
```

The `PermissionProcessor` handles permission checks before the endpoint runs.

## Component Requirements

Components used as request/response types must implement `IComponent`:

```csharp
public record Role : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Domain properties
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string[] PermissionIds { get; init; } = Array.Empty<string>();
}
```

Key properties:
- `Id` - Component identifier
- `OwnerEntityId` - Entity that owns this component (for RLS)
- `LastUpdated` - Optimistic concurrency tracking

## Decision Matrix

| Scenario | Request Type | Response Type |
|----------|--------------|---------------|
| CRUD Create | Component | Component |
| CRUD Read | None | Component |
| CRUD Update | Component | Component |
| Authentication | Account Component | AuthToken Component |
| GraphQL | Custom GraphQLRequest | GraphQLResult |
| Health check | None | Inline HealthResponse |
| Webhook | Custom WebhookPayload | Custom WebhookResponse |

## Anti-Patterns to Avoid

1. **Creating DTOs that mirror Components** - Use the Component directly
2. **Separate Request/Response for every endpoint** - Only when truly needed
3. **Business logic in Endpoint** - Delegate to Handlers/Systems
4. **Mixing validation in HandleAsync** - Use Validator class
5. **Inline types for reused responses** - Promote to Models/
