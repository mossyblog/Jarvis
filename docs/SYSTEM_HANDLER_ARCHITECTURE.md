# System + Handler Architecture Guide

## Table of Contents
1. [Overview](#overview)
2. [Core Principles](#core-principles)
3. [Architecture Components](#architecture-components)
4. [Implementation Patterns](#implementation-patterns)
5. [Good vs Bad Practices](#good-vs-bad-practices)
6. [Real-World Examples](#real-world-examples)
7. [Migration Guide](#migration-guide)
8. [Common Pitfalls](#common-pitfalls)

## Overview

The System + Handler architecture is a refined approach to implementing the Entity-Component-System (ECS) pattern in the Jarvis framework. This architecture separates business logic orchestration (Systems) from single-component operations (Handlers), resulting in cleaner, more maintainable code.

### Key Benefits
- **Clear Separation of Concerns**: Systems orchestrate workflows, Handlers manage component CRUD
- **No Double Orchestration**: Systems call handlers directly, not through abstraction layers
- **Type Safety**: All operations work with IComponent or Guid parameters
- **Testability**: Each layer can be tested independently
- **Scalability**: New workflows can be added without modifying existing handlers

## Core Principles

### 1. Systems Own Orchestration
Systems are responsible for coordinating multi-step workflows. They:
- Parse and validate input
- Coordinate multiple handler calls
- Manage transaction boundaries (when available)
- Handle cross-cutting concerns (logging, auditing)
- Return collections of IComponent

### 2. Handlers Own Single Component Operations
Handlers manage CRUD operations for a single component type. They:
- Create, read, update, delete components
- Enforce component-specific business rules
- Work with strongly-typed component instances
- Never orchestrate other handlers

### 3. Components Are Pure Data
Components are immutable data structures. They:
- Implement IComponent interface
- Use record types for immutability
- Contain no business logic
- Represent domain entities

### 4. Functions Are Thin HTTP Adapters
Azure Functions serve only as HTTP endpoints. They:
- Parse HTTP requests
- Call appropriate Systems
- Format responses
- Handle HTTP-specific concerns only

## Architecture Components

### System Class Structure
```csharp
public class RegistrationSystem
{
    private readonly IDataContext _dataContext;
    private readonly IPasswordPolicyService _passwordPolicy;
    private readonly ISecurityAuditService _securityAudit;
    private readonly ILogger<RegistrationSystem> _logger;

    public RegistrationSystem(
        IDataContext dataContext,
        IPasswordPolicyService passwordPolicy,
        ISecurityAuditService securityAudit,
        ILogger<RegistrationSystem> logger)
    {
        _dataContext = dataContext;
        _passwordPolicy = passwordPolicy;
        _securityAudit = securityAudit;
        _logger = logger;
    }

    public async Task<List<IComponent>> RegisterUser(string requestBody, string? ipAddress)
    {
        // Orchestration logic here
    }
}
```

Key points:
- Systems are plain classes (no interface implementation)
- Dependencies injected via constructor
- Methods return List<IComponent> or IComponent
- Methods accept primitive types or request objects

### Handler Class Structure
```csharp
public class AccountHandler : ComponentHandler<Account>
{
    public AccountHandler(
        IDataContext dataContext,
        ILogger<AccountHandler> logger)
        : base(dataContext, logger)
    {
    }

    public async Task<Account> CreateAccount(Account newAccount)
    {
        var account = newAccount with { OwnerEntityId = OwnerEntityId };
        await DataContext.Commit(account);
        Logger.LogInformation("Created account {AccountId}", account.Id);
        return account;
    }
}
```

Key points:
- Handlers extend ComponentHandler<T>
- Methods accept IComponent or Guid parameters
- Single responsibility for one component type
- No orchestration of other handlers

### Azure Function Structure
```csharp
[Function("register")]
public async Task<HttpResponseData> Register(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "security/register")] 
    HttpRequestData req)
{
    var requestBody = await req.ReadAsStringAsync();
    var components = await _registrationSystem.RegisterUser(
        requestBody, 
        req.GetClientIpAddress());
    
    var response = req.CreateResponse(HttpStatusCode.Created);
    await response.WriteAsJsonAsync(components);
    return response;
}
```

Key points:
- Functions are thin HTTP adapters
- No business logic in functions
- Delegate all work to Systems
- Return IComponent collections directly

## Implementation Patterns

### Pattern 1: Multi-Step Orchestration
```csharp
public async Task<List<IComponent>> CreateInvoiceWithWorkOrders(InvoiceRequest request)
{
    var components = new List<IComponent>();
    
    // Step 1: Create invoice
    var invoiceHandler = _dataContext.For<InvoiceHandler>(request.EntityId);
    var invoice = await invoiceHandler.CreateInvoice(new Invoice
    {
        Id = Guid.NewGuid(),
        CustomerName = request.CustomerName,
        TotalAmount = request.TotalAmount,
        DueDate = request.DueDate
    });
    components.Add(invoice);
    
    // Step 2: Create work orders
    foreach (var workOrderRequest in request.WorkOrders)
    {
        var workOrderHandler = _dataContext.For<WorkOrderHandler>(request.EntityId);
        var workOrder = await workOrderHandler.CreateWorkOrder(new WorkOrder
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            Description = workOrderRequest.Description,
            Amount = workOrderRequest.Amount
        });
        components.Add(workOrder);
    }
    
    // Step 3: Audit the operation
    await _auditService.LogInvoiceCreated(request.EntityId, invoice.Id);
    
    return components;
}
```

### Pattern 2: Validation Before Operation
```csharp
public async Task<List<IComponent>> RegisterUser(string requestBody, string? ipAddress)
{
    // Parse request
    var request = ParseRegistrationRequest(requestBody);
    
    // Validate business rules
    await ValidateRegistration(request);
    await CheckEmailAvailability(request.Email);
    
    // Create components
    var userEntityId = Guid.NewGuid();
    
    var accountHandler = _dataContext.For<AccountHandler>(userEntityId);
    var account = await accountHandler.CreateAccount(new Account
    {
        Id = Guid.NewGuid(),
        Email = request.Email.ToLower().Trim(),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
        AuthMethod = "password",
        IsActive = true
    });
    
    var profileHandler = _dataContext.For<AccountProfileHandler>(userEntityId);
    var profile = await profileHandler.CreateWithDefaults(request.Email);
    
    return new List<IComponent> { account, profile };
}
```

### Pattern 3: Query and Update
```csharp
public async Task<List<IComponent>> UpdateUserProfile(Guid userId, ProfileUpdateRequest request)
{
    // Query existing components
    var profileHandler = _dataContext.For<AccountProfileHandler>(userId);
    var currentProfile = await profileHandler.Get();
    
    if (currentProfile == null)
    {
        throw new BusinessRuleException("PROFILE_NOT_FOUND", "User profile not found");
    }
    
    // Update with new data
    var updatedProfile = currentProfile with
    {
        Name = request.Name ?? currentProfile.Name,
        Bio = request.Bio ?? currentProfile.Bio,
        UpdatedAt = DateTime.UtcNow
    };
    
    await _dataContext.Commit(updatedProfile);
    
    // Return updated component
    return new List<IComponent> { updatedProfile };
}
```

## Good vs Bad Practices

### ❌ BAD: Handler Orchestration Pattern (OLD WAY)
```csharp
// BAD: Handler doing orchestration
public class RegistrationHandler : ComponentHandler<Account>
{
    public async Task<RegistrationResult> RegisterFromJson(string json, string ip)
    {
        // Handler shouldn't parse JSON
        var request = JsonSerializer.Deserialize<RegistrationRequest>(json);
        
        // Handler shouldn't orchestrate other handlers
        var accountHandler = DataContext.For<AccountHandler>(entityId);
        var profileHandler = DataContext.For<ProfileHandler>(entityId);
        
        // Handler shouldn't return custom result objects
        return new RegistrationResult { Success = true };
    }
}

// BAD: Function calling handler directly for complex operations
public async Task<HttpResponseData> Register(HttpRequestData req)
{
    var requestBody = await req.ReadAsStringAsync();
    var handler = _dataContext.For<RegistrationHandler>(Guid.Empty);
    var result = await handler.RegisterFromJson(requestBody, ipAddress);
    
    return CreateResponse(result);
}

// BAD: Custom result objects instead of components
public class RegistrationResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public Guid AccountId { get; set; }
}
```

### ✅ GOOD: Direct System Orchestration (NEW WAY)
```csharp
// GOOD: System as plain class
public class RegistrationSystem
{
    private readonly IDataContext _dataContext;
    private readonly IPasswordPolicyService _passwordPolicy;
    
    public async Task<List<IComponent>> RegisterUser(string requestBody, string? ipAddress)
    {
        // System handles parsing
        var request = ParseRegistrationRequest(requestBody);
        
        // System handles validation
        await ValidateRegistration(request);
        
        // System orchestrates handlers directly
        var userEntityId = Guid.NewGuid();
        var accountHandler = _dataContext.For<AccountHandler>(userEntityId);
        var account = await accountHandler.CreateAccount(new Account { /* ... */ });
        
        var profileHandler = _dataContext.For<AccountProfileHandler>(userEntityId);
        var profile = await profileHandler.CreateWithDefaults(request.Email);
        
        // System returns component list
        return new List<IComponent> { account, profile };
    }
}

// GOOD: Handler focused on single component
public class AccountHandler : ComponentHandler<Account>
{
    public async Task<Account> CreateAccount(Account newAccount)
    {
        // Handler only manages Account components
        var account = newAccount with { OwnerEntityId = OwnerEntityId };
        await DataContext.Commit(account);
        return account;
    }
}

// GOOD: Function as thin adapter
[Function("register")]
public async Task<HttpResponseData> Register(HttpRequestData req)
{
    var requestBody = await req.ReadAsStringAsync();
    var components = await _registrationSystem.RegisterUser(requestBody, req.GetClientIpAddress());
    
    var response = req.CreateResponse(HttpStatusCode.Created);
    await response.WriteAsJsonAsync(components);
    return response;
}
```

### ❌ BAD: Handler Accepting Individual Fields
```csharp
// BAD: Handler method with individual parameters
public async Task<Account> CreateAccount(
    string email, 
    string passwordHash, 
    bool isActive, 
    string authMethod)
{
    var account = new Account
    {
        Email = email,
        PasswordHash = passwordHash,
        IsActive = isActive,
        AuthMethod = authMethod
    };
    // ...
}

// BAD: System passing individual fields
var account = await handler.CreateAccount(
    request.Email,
    hashedPassword,
    true,
    "password");
```

### ✅ GOOD: Handler Accepting Component
```csharp
// GOOD: Handler accepts component
public async Task<Account> CreateAccount(Account newAccount)
{
    var account = newAccount with { OwnerEntityId = OwnerEntityId };
    await DataContext.Commit(account);
    return account;
}

// GOOD: System creates complete component
var account = await handler.CreateAccount(new Account
{
    Id = Guid.NewGuid(),
    Email = request.Email,
    PasswordHash = hashedPassword,
    IsActive = true,
    AuthMethod = "password",
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
});
```

### ❌ BAD: Custom Result Objects
```csharp
// BAD: Custom result object
public class RegistrationResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public Guid AccountId { get; set; }
    public string Email { get; set; }
}

// BAD: System returning custom result
public async Task<RegistrationResult> Register(RegistrationRequest request)
{
    // ...
    return new RegistrationResult 
    { 
        Success = true,
        AccountId = account.Id,
        Email = account.Email
    };
}
```

### ✅ GOOD: Returning Components
```csharp
// GOOD: Return component list
public async Task<List<IComponent>> RegisterUser(string requestBody, string? ipAddress)
{
    // ...
    return new List<IComponent> { account, profile };
}

// GOOD: Client can access all component data
var components = await system.RegisterUser(json, ip);
var account = components.OfType<Account>().First();
var profile = components.OfType<SecurityProfile>().First();
```

## Real-World Examples

### Example 1: Invoice Creation System
```csharp
public class InvoiceSystem
{
    private readonly IDataContext _dataContext;
    private readonly IPdfService _pdfService;
    private readonly IEmailService _emailService;
    private readonly ILogger<InvoiceSystem> _logger;

    public async Task<List<IComponent>> CreateInvoiceWithLineItems(
        InvoiceCreationRequest request)
    {
        var components = new List<IComponent>();
        var invoiceId = Guid.NewGuid();
        
        // Create invoice
        var invoiceHandler = _dataContext.For<InvoiceHandler>(request.CustomerId);
        var invoice = await invoiceHandler.CreateInvoice(new Invoice
        {
            Id = invoiceId,
            InvoiceNumber = await GenerateInvoiceNumber(),
            CustomerId = request.CustomerId,
            DueDate = DateTime.UtcNow.AddDays(request.PaymentTermDays),
            Status = InvoiceStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        components.Add(invoice);
        
        // Create line items
        decimal totalAmount = 0;
        foreach (var item in request.LineItems)
        {
            var lineItemHandler = _dataContext.For<InvoiceLineItemHandler>(invoiceId);
            var lineItem = await lineItemHandler.CreateLineItem(new InvoiceLineItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.Quantity * item.UnitPrice
            });
            components.Add(lineItem);
            totalAmount += lineItem.TotalPrice;
        }
        
        // Update invoice with total
        var updatedInvoice = invoice with 
        { 
            TotalAmount = totalAmount,
            UpdatedAt = DateTime.UtcNow
        };
        await _dataContext.Commit(updatedInvoice);
        
        // Replace original invoice in components list
        components[0] = updatedInvoice;
        
        _logger.LogInformation("Created invoice {InvoiceNumber} with {ItemCount} items", 
            invoice.InvoiceNumber, request.LineItems.Count);
        
        return components;
    }

    public async Task<List<IComponent>> FinalizeAndSendInvoice(Guid invoiceId)
    {
        // Get invoice and line items
        var invoiceHandler = _dataContext.For<InvoiceHandler>(invoiceId);
        var invoice = await invoiceHandler.Get();
        
        if (invoice == null)
        {
            throw new BusinessRuleException("INVOICE_NOT_FOUND", "Invoice not found");
        }
        
        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new BusinessRuleException("INVALID_STATUS", 
                "Only draft invoices can be finalized");
        }
        
        // Update status
        var finalizedInvoice = invoice with 
        { 
            Status = InvoiceStatus.Sent,
            SentAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _dataContext.Commit(finalizedInvoice);
        
        // Generate PDF
        var lineItems = await _dataContext.Query()
            .WithAll<InvoiceLineItem>(item => item.InvoiceId == invoiceId)
            .ToList<InvoiceLineItem>();
            
        var pdfBytes = await _pdfService.GenerateInvoicePdf(finalizedInvoice, lineItems);
        
        // Create document record
        var documentHandler = _dataContext.For<DocumentHandler>(invoiceId);
        var document = await documentHandler.CreateDocument(new Document
        {
            Id = Guid.NewGuid(),
            EntityId = invoiceId,
            FileName = $"Invoice-{finalizedInvoice.InvoiceNumber}.pdf",
            ContentType = "application/pdf",
            Size = pdfBytes.Length,
            StorageUrl = await StoreDocument(pdfBytes)
        });
        
        // Send email
        await _emailService.SendInvoiceEmail(finalizedInvoice, pdfBytes);
        
        return new List<IComponent> { finalizedInvoice, document };
    }
}
```

### Example 2: User Permission System
```csharp
public class PermissionSystem
{
    private readonly IDataContext _dataContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<PermissionSystem> _logger;

    public async Task<List<IComponent>> AssignRoleToUser(
        Guid userId, 
        Guid roleId, 
        Guid assignedBy)
    {
        // Validate role exists
        var roleHandler = _dataContext.For<RoleHandler>(roleId);
        var role = await roleHandler.Get();
        
        if (role == null)
        {
            throw new BusinessRuleException("ROLE_NOT_FOUND", "Role not found");
        }
        
        // Get user's current profile
        var profileHandler = _dataContext.For<AccountProfileHandler>(userId);
        var profile = await profileHandler.Get();
        
        if (profile == null)
        {
            throw new BusinessRuleException("PROFILE_NOT_FOUND", "User profile not found");
        }
        
        // Check if already assigned
        if (profile.RoleIds.Contains(roleId))
        {
            _logger.LogInformation("User {UserId} already has role {RoleId}", 
                userId, roleId);
            return new List<IComponent> { profile };
        }
        
        // Update profile with new role
        var updatedProfile = profile with
        {
            RoleIds = profile.RoleIds.Concat(new[] { roleId }).ToArray(),
            UpdatedAt = DateTime.UtcNow
        };
        await _dataContext.Commit(updatedProfile);
        
        // Audit the change
        await _auditService.LogRoleAssignment(userId, roleId, assignedBy);
        
        _logger.LogInformation("Assigned role {RoleName} to user {UserId}", 
            role.Name, userId);
        
        return new List<IComponent> { updatedProfile, role };
    }

    public async Task<List<IComponent>> CreateRoleWithPermissions(
        RoleCreationRequest request)
    {
        var components = new List<IComponent>();
        var roleId = Guid.NewGuid();
        
        // Create role
        var roleHandler = _dataContext.For<RoleHandler>(roleId);
        var role = await roleHandler.CreateRole(new Role
        {
            Id = roleId,
            Name = request.Name,
            Description = request.Description,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        components.Add(role);
        
        // Validate and assign permissions
        var validPermissionIds = new List<Guid>();
        foreach (var permissionId in request.PermissionIds)
        {
            var permHandler = _dataContext.For<PermissionHandler>(permissionId);
            var permission = await permHandler.Get();
            
            if (permission != null)
            {
                validPermissionIds.Add(permissionId);
                components.Add(permission);
            }
            else
            {
                _logger.LogWarning("Permission {PermissionId} not found, skipping", 
                    permissionId);
            }
        }
        
        // Update role with valid permissions
        if (validPermissionIds.Any())
        {
            var updatedRole = role with
            {
                PermissionIds = validPermissionIds.ToArray(),
                UpdatedAt = DateTime.UtcNow
            };
            await _dataContext.Commit(updatedRole);
            components[0] = updatedRole; // Replace original role
        }
        
        return components;
    }
}
```

## Migration Guide

### Step 1: Identify Existing Patterns
Look for these patterns in your codebase:
- Handlers doing orchestration
- ExecuteHandler methods
- Handlers doing orchestration
- Custom result objects

### Step 2: Create New System Classes
```csharp
// Old pattern - Handler doing orchestration
public class InvoiceHandler : ComponentHandler<Invoice>
{
    public async Task<InvoiceResult> CreateCompleteInvoice(InvoiceRequest request)
    {
        // Handler shouldn't orchestrate multiple components
        var invoice = new Invoice { /* ... */ };
        await DataContext.Commit(invoice);
        
        // Creating line items too - this is orchestration!
        var itemHandler = DataContext.For<LineItemHandler>(invoice.Id);
        // ...
        
        return new InvoiceResult { Success = true };
    }
}

// New pattern
public class InvoiceSystem
{
    private readonly IDataContext _dataContext;
    
    public async Task<List<IComponent>> CreateInvoice(InvoiceRequest request)
    {
        var handler = _dataContext.For<InvoiceHandler>(request.EntityId);
        var invoice = await handler.CreateInvoice(new Invoice { /* ... */ });
        return new List<IComponent> { invoice };
    }
}
```

### Step 3: Refactor Handlers
```csharp
// Old handler with orchestration
public class InvoiceHandler : ComponentHandler<Invoice>
{
    public async Task<InvoiceResult> CreateFullInvoice(InvoiceRequest request)
    {
        // Creating multiple components
        var invoice = new Invoice { /* ... */ };
        await DataContext.Commit(invoice);
        
        // Orchestrating other handlers - BAD!
        var lineItemHandler = DataContext.For<LineItemHandler>(invoice.Id);
        // ...
        
        return new InvoiceResult { Success = true, InvoiceId = invoice.Id };
    }
}

// New handler - single responsibility
public class InvoiceHandler : ComponentHandler<Invoice>
{
    public async Task<Invoice> CreateInvoice(Invoice newInvoice)
    {
        var invoice = newInvoice with { OwnerEntityId = OwnerEntityId };
        await DataContext.Commit(invoice);
        return invoice;
    }
    
    public async Task<Invoice> UpdateInvoiceStatus(InvoiceStatus newStatus)
    {
        var invoice = await Get();
        if (invoice == null) throw new InvalidOperationException("Invoice not found");
        
        var updated = invoice with 
        { 
            Status = newStatus, 
            UpdatedAt = DateTime.UtcNow 
        };
        await DataContext.Commit(updated);
        return updated;
    }
}
```

### Step 4: Update Azure Functions
```csharp
// Old function
[Function("CreateInvoice")]
public async Task<HttpResponseData> CreateInvoice(HttpRequestData req)
{
    var result = await _system.ExecuteHandler<InvoiceResult>(
        entityId,
        handler => handler.CreateFullInvoice(request));
    
    if (!result.Success)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest);
    }
    
    return CreateResponse(new { invoiceId = result.InvoiceId });
}

// New function
[Function("CreateInvoice")]
public async Task<HttpResponseData> CreateInvoice(HttpRequestData req)
{
    var requestBody = await req.ReadAsStringAsync();
    var components = await _invoiceSystem.CreateInvoiceWithLineItems(requestBody);
    
    var response = req.CreateResponse(HttpStatusCode.Created);
    await response.WriteAsJsonAsync(components);
    return response;
}
```

## Common Pitfalls

### Pitfall 1: System Doing Too Much
```csharp
// BAD: System handling HTTP concerns
public class InvoiceSystem
{
    public async Task<HttpResponseData> CreateInvoice(HttpRequestData req)
    {
        // Systems shouldn't know about HTTP!
    }
}

// GOOD: System handles business logic only
public class InvoiceSystem
{
    public async Task<List<IComponent>> CreateInvoice(InvoiceRequest request)
    {
        // Business logic only
    }
}
```

### Pitfall 2: Handler Orchestration
```csharp
// BAD: Handler calling other handlers
public class OrderHandler : ComponentHandler<Order>
{
    public async Task<Order> CreateOrderWithPayment(OrderRequest request)
    {
        var order = new Order { /* ... */ };
        await DataContext.Commit(order);
        
        // Handler shouldn't orchestrate!
        var paymentHandler = DataContext.For<PaymentHandler>(order.Id);
        await paymentHandler.CreatePayment(/* ... */);
        
        return order;
    }
}
```

### Pitfall 3: Returning Wrong Types
```csharp
// BAD: Returning anonymous objects
public async Task<object> GetUserProfile(Guid userId)
{
    var profile = await handler.Get();
    return new { name = profile.Name, email = profile.Email };
}

// GOOD: Return components
public async Task<List<IComponent>> GetUserProfile(Guid userId)
{
    var profile = await handler.Get();
    return new List<IComponent> { profile };
}
```

### Pitfall 4: Mixing Concerns
```csharp
// BAD: Handler doing validation
public class AccountHandler : ComponentHandler<Account>
{
    public async Task<Account> CreateAccount(Account account)
    {
        // Handlers shouldn't validate business rules
        if (await EmailExists(account.Email))
        {
            throw new Exception("Email exists");
        }
        
        await DataContext.Commit(account);
        return account;
    }
}

// GOOD: System handles validation
public class RegistrationSystem
{
    private async Task CheckEmailAvailability(string email)
    {
        var existing = await _dataContext.Query()
            .WithAll<Account>(a => a.Email == email)
            .ToEntityIds();
            
        if (existing.Any())
        {
            throw new BusinessRuleException("EMAIL_EXISTS", "Email already in use");
        }
    }
}
```

## Summary

The System + Handler architecture provides a clean, scalable approach to building business applications. By following these patterns:

1. **Systems orchestrate** - They coordinate workflows and enforce business rules
2. **Handlers operate** - They perform CRUD on single component types
3. **Components store data** - They are immutable data structures
4. **Functions adapt** - They translate HTTP to system calls

This separation ensures each layer has a single responsibility, making the codebase easier to understand, test, and maintain.