# Comprehensive Handler Guide

This guide consolidates all handler-related documentation for the Jarvis framework, providing a complete reference for developing with the System + Handler architecture.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Quick Reference](#quick-reference)
3. [Getting Started](#getting-started)
4. [Core Concepts](#core-concepts)
5. [Implementation Patterns](#implementation-patterns)
6. [Component Development](#component-development)
7. [Handler Development](#handler-development)
8. [System Development](#system-development)
9. [Azure Functions](#azure-functions)
10. [Advanced Patterns](#advanced-patterns)
11. [Testing Strategies](#testing-strategies)
12. [Migration Guide](#migration-guide)
13. [Common Pitfalls](#common-pitfalls)
14. [Debugging Tips](#debugging-tips)

## Architecture Overview

The System + Handler architecture is a refined approach to implementing the Entity-Component-System (ECS) pattern in the Jarvis framework. This architecture separates business logic orchestration (Systems) from single-component operations (Handlers), resulting in cleaner, more maintainable code.

### Key Benefits

- **Clear Separation of Concerns**: Systems orchestrate workflows, Handlers manage component CRUD
- **No Double Orchestration**: Systems call handlers directly, not through abstraction layers
- **Type Safety**: All operations work with IComponent or Guid parameters
- **Testability**: Each layer can be tested independently
- **Scalability**: New workflows can be added without modifying existing handlers

### Architecture Layers

| Component | Purpose | Returns | Accepts |
|-----------|---------|---------|---------|
| **System** | Orchestrates workflows | `List<IComponent>` or `IComponent` | Primitives, request objects, JSON |
| **Handler** | CRUD for one component | `TComponent` | `IComponent` or `Guid` |
| **Function** | HTTP adapter | `HttpResponseData` | `HttpRequestData` |
| **Component** | Data structure | N/A | N/A |

## Quick Reference

### Do's and Don'ts

#### ✅ DO

- Systems call handlers directly via `_dataContext.For<THandler>(entityId)`
- Handlers accept complete component objects as parameters
- Systems return `List<IComponent>` for multi-component operations
- Functions delegate all logic to Systems
- Use `record` types for components
- Validate in Systems, not Handlers
- Use dependency injection for all services

#### ❌ DON'T

- Have Handlers do orchestration
- Use ExecuteHandler or similar double-orchestration patterns
- Have Handlers accept individual field parameters
- Create custom result objects (use IComponent collections)
- Put business logic in Functions
- Have Handlers call other Handlers
- Mix HTTP concerns into Systems

## Getting Started

### 1. Configure Dependency Injection

Add Jarvis ECS to your DI setup:

```csharp
// In Program.cs or Startup.cs
services.RegisterJarvis(LogLevel.Information, Configuration);

// Register Systems
services.AddScoped<InvoiceSystem>();
services.AddScoped<OrderSystem>();
services.AddScoped<UserSystem>();

// Register Handlers (both interface and concrete)
services.AddScoped<IComponentHandler, OrderHandler>();
services.AddScoped<OrderHandler>(); // Required for DataContext.For<T>()
```

### 2. Create Your First Component

Components are immutable data structures that implement `IComponent`:

```csharp
public record OrderComponent : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Domain properties
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public string Status { get; init; } = "PENDING";
    public decimal Amount { get; init; }
}
```

### 3. Create Your First Handler

Handlers manage CRUD operations for a single component type:

```csharp
public class OrderHandler : ComponentHandler<OrderComponent>
{
    public OrderHandler(
        IDataContext dataContext, 
        ILogger<OrderHandler> logger)
        : base(dataContext, logger)
    {
    }

    public async Task<OrderComponent> CreateOrder(OrderComponent newOrder)
    {
        var order = newOrder with { OwnerEntityId = OwnerEntityId };
        await DataContext.Commit(order);
        Logger.LogInformation("Created order {OrderNumber}", order.OrderNumber);
        return order;
    }

    public async Task<OrderComponent> UpdateStatus(string newStatus)
    {
        var order = await Get() ?? throw new InvalidOperationException("Order not found");
        var updated = order with 
        { 
            Status = newStatus, 
            LastUpdated = DateTime.UtcNow 
        };
        await DataContext.Commit(updated);
        return updated;
    }
}
```

### 4. Create Your First System

Systems orchestrate workflows and coordinate multiple handlers:

```csharp
public class OrderSystem
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<OrderSystem> _logger;

    public OrderSystem(IDataContext dataContext, ILogger<OrderSystem> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    public async Task<List<IComponent>> CreateOrderWithItems(OrderRequest request)
    {
        // Validate request
        if (request.Items?.Any() != true)
            throw new ValidationException("Order must have at least one item");

        var components = new List<IComponent>();
        
        // Create order
        var orderId = Guid.NewGuid();
        var orderHandler = _dataContext.For<OrderHandler>(orderId);
        var order = await orderHandler.CreateOrder(new OrderComponent
        {
            Id = orderId,
            OrderNumber = GenerateOrderNumber(),
            CustomerId = request.CustomerId,
            Status = "PENDING",
            Amount = request.Items.Sum(i => i.Quantity * i.Price)
        });
        components.Add(order);
        
        // Create order items
        foreach (var item in request.Items)
        {
            var itemHandler = _dataContext.For<OrderItemHandler>(Guid.NewGuid());
            var orderItem = await itemHandler.CreateItem(new OrderItemComponent
            {
                OrderId = orderId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Price
            });
            components.Add(orderItem);
        }
        
        return components;
    }
}
```

### 5. Create Azure Function

Functions serve as thin HTTP adapters:

```csharp
public class OrderFunction
{
    private readonly OrderSystem _orderSystem;

    public OrderFunction(OrderSystem orderSystem)
    {
        _orderSystem = orderSystem;
    }

    [Function("CreateOrder")]
    public async Task<HttpResponseData> CreateOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] 
        HttpRequestData req)
    {
        var requestBody = await req.ReadAsStringAsync();
        var components = await _orderSystem.CreateOrderWithItems(requestBody);
        
        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(components);
        return response;
    }
}
```

## Core Concepts

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

## Implementation Patterns

### Pattern 1: Multi-Step Orchestration

```csharp
public async Task<List<IComponent>> CreateInvoiceWithLineItems(InvoiceRequest request)
{
    var components = new List<IComponent>();
    var invoiceId = Guid.NewGuid();
    
    // Step 1: Create invoice
    var invoiceHandler = _dataContext.For<InvoiceHandler>(invoiceId);
    var invoice = await invoiceHandler.CreateInvoice(new Invoice
    {
        Id = invoiceId,
        InvoiceNumber = await GenerateInvoiceNumber(),
        CustomerId = request.CustomerId,
        DueDate = DateTime.UtcNow.AddDays(request.PaymentTermDays),
        Status = InvoiceStatus.Draft
    });
    components.Add(invoice);
    
    // Step 2: Create line items
    decimal totalAmount = 0;
    foreach (var item in request.LineItems)
    {
        var lineItemHandler = _dataContext.For<InvoiceLineItemHandler>(Guid.NewGuid());
        var lineItem = await lineItemHandler.CreateLineItem(new InvoiceLineItem
        {
            InvoiceId = invoiceId,
            Description = item.Description,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            TotalPrice = item.Quantity * item.UnitPrice
        });
        components.Add(lineItem);
        totalAmount += lineItem.TotalPrice;
    }
    
    // Step 3: Update invoice with total
    var updatedInvoice = invoice with 
    { 
        TotalAmount = totalAmount,
        LastUpdated = DateTime.UtcNow
    };
    await _dataContext.Commit(updatedInvoice);
    
    // Replace original invoice in components
    components[0] = updatedInvoice;
    
    // Step 4: Audit the operation
    await _auditService.LogInvoiceCreated(invoiceId, totalAmount);
    
    return components;
}
```

### Pattern 2: Validation Before Operation

```csharp
public async Task<List<IComponent>> RegisterUser(string requestBody, string? ipAddress)
{
    // Parse request
    var request = ParseRegistrationRequest(requestBody);
    
    // Validate business rules in System
    await ValidateRegistration(request);
    await CheckEmailAvailability(request.Email);
    
    // Create components
    var userEntityId = Guid.NewGuid();
    
    var accountHandler = _dataContext.For<AccountHandler>(userEntityId);
    var account = await accountHandler.CreateAccount(new Account
    {
        Email = request.Email.ToLower().Trim(),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
        AuthMethod = "password",
        IsActive = true
    });
    
    var profileHandler = _dataContext.For<AccountProfileHandler>(userEntityId);
    var profile = await profileHandler.CreateWithDefaults(request.Email);
    
    return new List<IComponent> { account, profile };
}

private async Task CheckEmailAvailability(string email)
{
    var existing = await _dataContext.Query()
        .WithAll<Account>(a => a.Email == email.ToLower())
        .Any();
        
    if (existing)
    {
        throw new BusinessRuleException("EMAIL_EXISTS", "Email already registered");
    }
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
        LastUpdated = DateTime.UtcNow
    };
    
    await _dataContext.Commit(updatedProfile);
    
    return new List<IComponent> { updatedProfile };
}
```

### Pattern 4: Handle Relationships

```csharp
public async Task<List<IComponent>> CreateOrderWithRelationships(OrderRequest request)
{
    var components = new List<IComponent>();
    
    // Create parent order
    var orderId = Guid.NewGuid();
    var orderHandler = _dataContext.For<OrderHandler>(orderId);
    var order = await orderHandler.CreateOrder(new OrderComponent
    {
        OrderNumber = request.OrderNumber,
        CustomerId = request.CustomerId,
        Amount = request.TotalAmount
    });
    components.Add(order);
    
    // Create child items and link them
    foreach (var item in request.Items)
    {
        var itemId = Guid.NewGuid();
        var itemHandler = _dataContext.For<OrderItemHandler>(itemId);
        var orderItem = await itemHandler.CreateItem(new OrderItemComponent
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            Price = item.Price
        });
        components.Add(orderItem);
        
        // Link item to order
        await _dataContext.LinkRelationship(orderId, itemId, "Order", "OrderItem");
    }
    
    return components;
}
```

## Component Development

### Basic Component Structure

```csharp
public record OrderComponent : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Domain properties
    public string OrderNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public OrderStatus Status { get; init; } = OrderStatus.Pending;
}
```

### Versioned Components

For components requiring strict concurrency control:

```csharp
public record CriticalComponent : IComponent, IVersionedComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }
    public int? Version { get; set; }  // Automatic version management
    
    // Critical business data
    public decimal AccountBalance { get; set; }
    public string AccountStatus { get; set; }
}
```

### Component Guidelines

- Use `record` types for immutability
- Initialize collections to empty, not null
- Use meaningful property names
- Include proper defaults
- Follow `LastUpdated` naming convention (not UpdatedAt)

## Handler Development

### Basic Handler Structure

```csharp
public class AccountHandler : ComponentHandler<Account>
{
    private readonly IPasswordService _passwordService;
    private readonly ILogger<AccountHandler> _logger;

    public AccountHandler(
        IDataContext dataContext,
        IPasswordService passwordService,
        ILogger<AccountHandler> logger)
        : base(dataContext, logger)
    {
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task<Account> CreateAccount(Account newAccount)
    {
        // Handler sets OwnerEntityId
        var account = newAccount with { OwnerEntityId = OwnerEntityId };
        
        // Save to database
        await DataContext.Commit(account);
        
        _logger.LogInformation("Created account for {Email}", account.Email);
        return account;
    }

    public async Task<Account> UpdatePassword(string newPassword)
    {
        var account = await Get() ?? throw new InvalidOperationException("Account not found");
        
        var updated = account with
        {
            PasswordHash = _passwordService.HashPassword(newPassword),
            LastUpdated = DateTime.UtcNow
        };
        
        await DataContext.Commit(updated);
        return updated;
    }
}
```

### Working with Entities

#### Creating New Entities

```csharp
public async Task<OrderComponent> CreateOrder(string orderNumber, decimal amount)
{
    // Create new entity with unique ID
    var entity = DataContext.Entity();
    
    var order = new OrderComponent
    {
        Id = Guid.NewGuid(),
        OwnerEntityId = entity.Id,
        OrderNumber = orderNumber,
        Amount = amount,
        LastUpdated = DateTime.UtcNow
    };
    
    await DataContext.Commit(order);
    return order;
}
```

### Schema Management

For handlers that need schema validation:

```csharp
public class OrderHandler : ComponentHandler<OrderComponent>
{
    private readonly ITableManager _tableManager;
    
    public OrderHandler(IDataContext dataContext, ITableManager tableManager) 
        : base(dataContext)
    {
        _tableManager = tableManager;
    }
    
    public async Task Initialize()
    {
        await _tableManager.EnsureTableExists<OrderComponent>();
    }
}
```

### Concurrency Handling

#### With Versioned Components

```csharp
try 
{
    await DataContext.Commit(versionedComponent);
}
catch (ConcurrencyException ex)
{
    // Handle version conflict
    throw new BusinessException("Component was modified by another user");
}
```

#### With Standard Components

```csharp
// Use TryCommit for graceful handling
if (!await DataContext.TryCommit(component))
{
    // Reload and retry
    var current = await Get();
    var updated = current with { PropertyToUpdate = newValue };
    if (!await DataContext.TryCommit(updated))
    {
        throw new BusinessException("Unable to update due to concurrent modifications");
    }
}
```

### Handler Best Practices

1. **Single Responsibility**: Each handler manages one component type
2. **No Orchestration**: Handlers never call other handlers
3. **Accept Components**: Methods accept IComponent, not individual fields
4. **Return Components**: Always return the component, not custom objects
5. **Use Base Methods**: Leverage ComponentHandler base class methods

## System Development

### Basic System Structure

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
        // Parse input
        var request = JsonSerializer.Deserialize<RegistrationRequest>(requestBody);
        
        // Validate
        await ValidateRegistration(request);
        
        // Orchestrate handlers
        var components = new List<IComponent>();
        var userId = Guid.NewGuid();
        
        // Create account
        var accountHandler = _dataContext.For<AccountHandler>(userId);
        var account = await accountHandler.CreateAccount(new Account
        {
            Email = request.Email,
            PasswordHash = HashPassword(request.Password)
        });
        components.Add(account);
        
        // Create profile
        var profileHandler = _dataContext.For<ProfileHandler>(userId);
        var profile = await profileHandler.CreateProfile(new Profile
        {
            Name = request.Name,
            Email = request.Email
        });
        components.Add(profile);
        
        // Audit
        await _securityAudit.LogRegistration(userId, ipAddress);
        
        return components;
    }
}
```

### System Patterns

#### Cross-Component Queries

```csharp
public async Task<List<IComponent>> GetUserDashboard(Guid userId)
{
    var components = new List<IComponent>();
    
    // Get user account
    var accountHandler = _dataContext.For<AccountHandler>(userId);
    var account = await accountHandler.Get();
    if (account == null) throw new NotFoundException("User not found");
    components.Add(account);
    
    // Get user orders
    var orders = await _dataContext.Query()
        .WithAll<OrderComponent>(o => o.CustomerId == userId.ToString())
        .ToComponents<OrderComponent>();
    components.AddRange(orders);
    
    // Get user notifications
    var notifications = await _dataContext.Query()
        .WithAll<NotificationComponent>(n => n.UserId == userId && !n.IsRead)
        .ToComponents<NotificationComponent>();
    components.AddRange(notifications);
    
    return components;
}
```

#### Transaction Patterns

```csharp
public async Task<List<IComponent>> TransferFunds(TransferRequest request)
{
    // Validate
    if (request.Amount <= 0)
        throw new ValidationException("Amount must be positive");
    
    // Get accounts
    var sourceHandler = _dataContext.For<AccountHandler>(request.SourceAccountId);
    var sourceAccount = await sourceHandler.Get() 
        ?? throw new NotFoundException("Source account not found");
    
    var targetHandler = _dataContext.For<AccountHandler>(request.TargetAccountId);
    var targetAccount = await targetHandler.Get() 
        ?? throw new NotFoundException("Target account not found");
    
    // Check balance
    if (sourceAccount.Balance < request.Amount)
        throw new BusinessRuleException("INSUFFICIENT_FUNDS", "Insufficient balance");
    
    // Update balances
    var updatedSource = sourceAccount with 
    { 
        Balance = sourceAccount.Balance - request.Amount,
        LastUpdated = DateTime.UtcNow
    };
    
    var updatedTarget = targetAccount with 
    { 
        Balance = targetAccount.Balance + request.Amount,
        LastUpdated = DateTime.UtcNow
    };
    
    // Commit both (ideally in transaction)
    await _dataContext.Commit(updatedSource);
    await _dataContext.Commit(updatedTarget);
    
    // Create transfer record
    var transferHandler = _dataContext.For<TransferHandler>(Guid.NewGuid());
    var transfer = await transferHandler.CreateTransfer(new Transfer
    {
        SourceAccountId = request.SourceAccountId,
        TargetAccountId = request.TargetAccountId,
        Amount = request.Amount,
        Reference = request.Reference
    });
    
    return new List<IComponent> { updatedSource, updatedTarget, transfer };
}
```

### System Best Practices

1. **Parse and Validate**: Systems handle input parsing and validation
2. **Orchestrate**: Coordinate multiple handlers for complex workflows
3. **Business Rules**: Enforce cross-component business rules
4. **Return Components**: Always return List<IComponent>
5. **Error Handling**: Use specific exceptions (ValidationException, BusinessRuleException)

## Azure Functions

### Basic Function Structure

```csharp
public class OrderFunctions
{
    private readonly OrderSystem _orderSystem;
    private readonly ILogger<OrderFunctions> _logger;

    public OrderFunctions(OrderSystem orderSystem, ILogger<OrderFunctions> logger)
    {
        _orderSystem = orderSystem;
        _logger = logger;
    }

    [Function("CreateOrder")]
    public async Task<HttpResponseData> CreateOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] 
        HttpRequestData req)
    {
        try
        {
            var requestBody = await req.ReadAsStringAsync();
            var components = await _orderSystem.CreateOrder(requestBody);
            
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(components);
            return response;
        }
        catch (ValidationException ex)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
        catch (BusinessRuleException ex)
        {
            var response = req.CreateResponse(HttpStatusCode.UnprocessableEntity);
            await response.WriteAsJsonAsync(new { error = ex.Message, code = ex.Code });
            return response;
        }
    }

    [Function("GetOrder")]
    public async Task<HttpResponseData> GetOrder(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{orderId}")] 
        HttpRequestData req,
        string orderId)
    {
        if (!Guid.TryParse(orderId, out var id))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Invalid order ID" });
            return badRequest;
        }

        var components = await _orderSystem.GetOrder(id);
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(components);
        return response;
    }
}
```

### Function Best Practices

1. **Thin Adapters**: Functions only handle HTTP concerns
2. **Delegate to Systems**: All business logic in Systems
3. **Error Mapping**: Map exceptions to appropriate HTTP status codes
4. **No Business Logic**: Never put business logic in functions
5. **Direct Response**: Return component collections directly

## Advanced Patterns

### Event Sourcing Pattern

```csharp
public class EventSourcedOrderSystem
{
    public async Task<List<IComponent>> ProcessOrder(ProcessOrderRequest request)
    {
        var components = new List<IComponent>();
        var orderId = request.OrderId;
        
        // Get current state
        var orderHandler = _dataContext.For<OrderHandler>(orderId);
        var order = await orderHandler.Get() 
            ?? throw new NotFoundException("Order not found");
        
        // Record event
        var eventHandler = _dataContext.For<OrderEventHandler>(Guid.NewGuid());
        var orderEvent = await eventHandler.CreateEvent(new OrderEvent
        {
            OrderId = orderId,
            EventType = "OrderProcessed",
            EventData = JsonSerializer.Serialize(new
            {
                PreviousStatus = order.Status,
                NewStatus = "PROCESSED",
                ProcessedBy = request.UserId,
                ProcessedAt = DateTime.UtcNow
            })
        });
        components.Add(orderEvent);
        
        // Update state
        var updatedOrder = order with 
        { 
            Status = "PROCESSED",
            LastUpdated = DateTime.UtcNow
        };
        await _dataContext.Commit(updatedOrder);
        components.Add(updatedOrder);
        
        return components;
    }
}
```

### Saga Pattern

```csharp
public class OrderFulfillmentSaga
{
    public async Task<List<IComponent>> FulfillOrder(Guid orderId)
    {
        var components = new List<IComponent>();
        var sagaId = Guid.NewGuid();
        
        try
        {
            // Step 1: Reserve inventory
            var inventoryResult = await _inventorySystem.ReserveItems(orderId);
            components.AddRange(inventoryResult);
            
            // Step 2: Process payment
            var paymentResult = await _paymentSystem.ProcessPayment(orderId);
            components.AddRange(paymentResult);
            
            // Step 3: Create shipment
            var shipmentResult = await _shippingSystem.CreateShipment(orderId);
            components.AddRange(shipmentResult);
            
            // Step 4: Update order status
            var orderHandler = _dataContext.For<OrderHandler>(orderId);
            var order = await orderHandler.UpdateStatus("FULFILLED");
            components.Add(order);
            
            // Record saga completion
            await RecordSagaCompletion(sagaId, orderId, "SUCCESS");
        }
        catch (Exception ex)
        {
            // Compensate
            await CompensateSaga(sagaId, orderId, components);
            await RecordSagaCompletion(sagaId, orderId, "FAILED", ex.Message);
            throw;
        }
        
        return components;
    }
}
```

### CQRS Pattern

```csharp
// Command Side
public class OrderCommandSystem
{
    public async Task<List<IComponent>> CreateOrder(CreateOrderCommand command)
    {
        // Validate command
        await ValidateCreateOrder(command);
        
        // Execute command
        var components = new List<IComponent>();
        var orderId = Guid.NewGuid();
        
        var handler = _dataContext.For<OrderHandler>(orderId);
        var order = await handler.CreateOrder(new OrderComponent
        {
            OrderNumber = GenerateOrderNumber(),
            CustomerId = command.CustomerId,
            Amount = command.Amount
        });
        components.Add(order);
        
        // Publish event
        await _eventBus.PublishAsync(new OrderCreatedEvent
        {
            OrderId = orderId,
            CustomerId = command.CustomerId,
            Amount = command.Amount
        });
        
        return components;
    }
}

// Query Side
public class OrderQuerySystem
{
    public async Task<OrderSummaryDto> GetOrderSummary(Guid orderId)
    {
        // Use optimized read model
        var summary = await _readModelDb.OrderSummaries
            .FirstOrDefaultAsync(s => s.OrderId == orderId);
            
        if (summary == null)
        {
            // Fallback to ECS if not in read model
            var components = await _dataContext.Query()
                .WithAll<OrderComponent>(o => o.Id == orderId)
                .ToComponents<OrderComponent>();
                
            var order = components.FirstOrDefault();
            if (order == null) throw new NotFoundException("Order not found");
            
            summary = MapToSummary(order);
        }
        
        return summary;
    }
}
```

## Testing Strategies

### Testing Systems

```csharp
[Fact]
public async Task CreateOrder_Should_Create_Order_And_Items()
{
    // Arrange
    var system = _serviceProvider.GetRequiredService<OrderSystem>();
    var request = new OrderRequest
    {
        CustomerId = Guid.NewGuid(),
        Items = new[]
        {
            new OrderItemRequest { ProductId = "PROD1", Quantity = 2, Price = 10.00m },
            new OrderItemRequest { ProductId = "PROD2", Quantity = 1, Price = 20.00m }
        }
    };
    
    // Act
    var components = await system.CreateOrderWithItems(request);
    
    // Assert
    components.Count.ShouldBe(3); // 1 order + 2 items
    
    var order = components.OfType<OrderComponent>().First();
    order.CustomerId.ShouldBe(request.CustomerId);
    order.Amount.ShouldBe(40.00m);
    
    var items = components.OfType<OrderItemComponent>().ToList();
    items.Count.ShouldBe(2);
    items.All(i => i.OrderId == order.Id).ShouldBeTrue();
    
    // Cleanup
    TrackEntity(order.OwnerEntityId);
    items.ForEach(i => TrackEntity(i.OwnerEntityId));
}
```

### Testing Handlers

```csharp
[Fact]
public async Task Handler_Should_Update_Component()
{
    // Arrange
    var entityId = Guid.NewGuid();
    var handler = TestDataContext().For<OrderHandler>(entityId);
    
    // Create initial order
    var order = await handler.CreateOrder(new OrderComponent
    {
        OrderNumber = "ORD-001",
        Status = "PENDING",
        Amount = 100.00m
    });
    
    // Act
    var updated = await handler.UpdateStatus("CONFIRMED");
    
    // Assert
    updated.Status.ShouldBe("CONFIRMED");
    updated.OrderNumber.ShouldBe("ORD-001");
    updated.Amount.ShouldBe(100.00m);
    updated.LastUpdated.ShouldBeGreaterThan(order.LastUpdated);
    
    // Cleanup
    TrackEntity(entityId);
}
```

### Testing with Mocks

```csharp
[Fact]
public async Task System_Should_Validate_Before_Creating()
{
    // Arrange
    var mockDataContext = new Mock<IDataContext>();
    var mockLogger = new Mock<ILogger<OrderSystem>>();
    var system = new OrderSystem(mockDataContext.Object, mockLogger.Object);
    
    var request = new OrderRequest
    {
        CustomerId = Guid.NewGuid(),
        Items = new List<OrderItemRequest>() // Empty items
    };
    
    // Act & Assert
    await Should.ThrowAsync<ValidationException>(
        async () => await system.CreateOrderWithItems(request)
    );
    
    // Verify no handler was called
    mockDataContext.Verify(dc => dc.For<OrderHandler>(It.IsAny<Guid>()), Times.Never);
}
```

### Integration Testing

```csharp
public class OrderSystemIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Full_Order_Workflow_Should_Complete()
    {
        // Arrange
        var system = GetRequiredService<OrderSystem>();
        var customerId = Guid.NewGuid();
        
        // Act - Create order
        var createResult = await system.CreateOrderWithItems(new OrderRequest
        {
            CustomerId = customerId,
            Items = new[]
            {
                new OrderItemRequest { ProductId = "PROD1", Quantity = 2, Price = 10.00m }
            }
        });
        
        var orderId = createResult.OfType<OrderComponent>().First().OwnerEntityId;
        
        // Act - Process order
        var processResult = await system.ProcessOrder(new ProcessOrderRequest
        {
            OrderId = orderId,
            PaymentMethod = "CREDIT_CARD"
        });
        
        // Assert
        var processedOrder = processResult.OfType<OrderComponent>().First();
        processedOrder.Status.ShouldBe("PROCESSED");
        
        // Cleanup
        TrackEntity(orderId);
    }
}
```

## Migration Guide

### Step 1: Identify Existing Patterns

Look for these patterns in your codebase:
- Handlers doing orchestration
- ExecuteHandler methods
- Custom result objects
- Handlers calling other handlers

### Step 2: Create New System Classes

```csharp
// Old pattern - Handler doing orchestration
public class InvoiceHandler : ComponentHandler<Invoice>
{
    public async Task<InvoiceResult> CreateCompleteInvoice(InvoiceRequest request)
    {
        // Handler shouldn't orchestrate
        var invoice = new Invoice { /* ... */ };
        await DataContext.Commit(invoice);
        
        // Creating line items too - this is orchestration!
        var itemHandler = DataContext.For<LineItemHandler>(invoice.Id);
        foreach (var item in request.Items)
        {
            await itemHandler.CreateItem(item);
        }
        
        return new InvoiceResult { Success = true, InvoiceId = invoice.Id };
    }
}

// New pattern - System orchestrates
public class InvoiceSystem
{
    private readonly IDataContext _dataContext;
    
    public async Task<List<IComponent>> CreateInvoice(InvoiceRequest request)
    {
        var components = new List<IComponent>();
        
        // Create invoice
        var invoiceHandler = _dataContext.For<InvoiceHandler>(request.EntityId);
        var invoice = await invoiceHandler.CreateInvoice(new Invoice
        {
            CustomerName = request.CustomerName,
            DueDate = request.DueDate
        });
        components.Add(invoice);
        
        // Create line items
        foreach (var item in request.Items)
        {
            var itemHandler = _dataContext.For<LineItemHandler>(Guid.NewGuid());
            var lineItem = await itemHandler.CreateItem(new LineItem
            {
                InvoiceId = invoice.Id,
                Description = item.Description,
                Amount = item.Amount
            });
            components.Add(lineItem);
        }
        
        return components;
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
        // Multiple responsibilities
        var invoice = new Invoice { /* ... */ };
        await DataContext.Commit(invoice);
        
        // BAD: Orchestrating other handlers
        var lineItemHandler = DataContext.For<LineItemHandler>(invoice.Id);
        // ...
        
        return new InvoiceResult { Success = true };
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
    
    public async Task<Invoice> UpdateStatus(InvoiceStatus newStatus)
    {
        var invoice = await Get() 
            ?? throw new InvalidOperationException("Invoice not found");
        
        var updated = invoice with 
        { 
            Status = newStatus, 
            LastUpdated = DateTime.UtcNow 
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
    var request = await req.ReadFromJsonAsync<InvoiceRequest>();
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

### Migration Checklist

- [ ] Move orchestration from Handlers to Systems
- [ ] Remove ExecuteHandler methods
- [ ] Change Handler methods to accept IComponent parameters
- [ ] Replace custom result objects with List<IComponent>
- [ ] Move validation from Handlers to Systems
- [ ] Update Functions to call Systems directly
- [ ] Update tests to use new patterns
- [ ] Register Systems in DI container

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

// GOOD: Handler focuses on single component
public class OrderHandler : ComponentHandler<Order>
{
    public async Task<Order> CreateOrder(Order newOrder)
    {
        var order = newOrder with { OwnerEntityId = OwnerEntityId };
        await DataContext.Commit(order);
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
            .Any();
            
        if (existing)
        {
            throw new BusinessRuleException("EMAIL_EXISTS", "Email already in use");
        }
    }
}
```

### Pitfall 5: Wrong Parameter Types

```csharp
// BAD: Handler accepting individual fields
public async Task<Account> CreateAccount(
    string email, 
    string passwordHash, 
    bool isActive)
{
    var account = new Account
    {
        Email = email,
        PasswordHash = passwordHash,
        IsActive = isActive
    };
    // ...
}

// GOOD: Handler accepts component
public async Task<Account> CreateAccount(Account newAccount)
{
    var account = newAccount with { OwnerEntityId = OwnerEntityId };
    await DataContext.Commit(account);
    return account;
}
```

## Debugging Tips

### Component Not Saving?

1. **Check OwnerEntityId**: Ensure it's set and not Guid.Empty
2. **Verify Id**: Component Id should not be empty Guid
3. **Commit Called**: Ensure `await DataContext.Commit()` is called
4. **Check Logs**: Look for database errors in logs

```csharp
// Debug helper
_logger.LogDebug("Saving component: {ComponentType} {Id} for entity {EntityId}", 
    component.GetType().Name, component.Id, component.OwnerEntityId);
```

### Handler Not Found?

1. **Check Registration**: Verify handler is registered in DI
2. **Both Registrations**: Need both interface and concrete registration
3. **Namespace**: Ensure correct namespace in using statements

```csharp
// Required registrations
services.AddScoped<IComponentHandler, OrderHandler>();
services.AddScoped<OrderHandler>(); // Don't forget this!
```

### Validation Errors?

1. **Location**: Validation should be in System, not Handler
2. **Exception Types**: Use appropriate exception types
3. **Error Messages**: Provide clear, actionable error messages

```csharp
// Use specific exceptions
throw new ValidationException("Email is required"); // For input errors
throw new BusinessRuleException("ORDER_LOCKED", "Order cannot be modified"); // For business rules
throw new NotFoundException("Order not found"); // For missing resources
```

### Concurrency Issues?

1. **Version Check**: For versioned components, check Version property
2. **Retry Logic**: Implement retry for concurrent updates
3. **TryCommit**: Use TryCommit for graceful handling

```csharp
// Retry pattern
for (int i = 0; i < 3; i++)
{
    var current = await handler.Get();
    var updated = current with { Property = newValue };
    
    if (await DataContext.TryCommit(updated))
        break;
        
    await Task.Delay(100 * (i + 1)); // Exponential backoff
}
```

### Test Cleanup Failing?

1. **Track Entities**: Always use `TrackEntity(entityId)`
2. **Correct ID**: Track the OwnerEntityId, not component Id
3. **Cleanup Order**: Clean up child entities before parents

```csharp
// Correct cleanup
TrackEntity(order.OwnerEntityId); // Not order.Id!
```

## Summary

The System + Handler architecture provides a clean, scalable approach to building business applications. By following these patterns:

1. **Systems orchestrate** - They coordinate workflows and enforce business rules
2. **Handlers operate** - They perform CRUD on single component types
3. **Components store data** - They are immutable data structures
4. **Functions adapt** - They translate HTTP to system calls

This separation ensures each layer has a single responsibility, making the codebase easier to understand, test, and maintain.

For additional information, see:
- [ECS Principles](../architecture/ecs-principles.md)
- [API Reference](../api-reference/core-interfaces.md)
- [Testing Strategies](./testing-strategies.md)