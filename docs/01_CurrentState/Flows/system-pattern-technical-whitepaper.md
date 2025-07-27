# The System Pattern in Jarvis: Technical Whitepaper

## Abstract

This whitepaper provides a comprehensive technical analysis of the System pattern in the Jarvis framework, focusing on its critical role as the orchestration layer between thin API endpoints and the handler-component-entity architecture. The System pattern enforces separation of concerns by ensuring Azure Functions remain pure HTTP adapters while Systems own all business logic orchestration. We examine how this pattern enables testability, maintainability, and scalability in serverless architectures.

## Table of Contents

1. [Introduction](#introduction)
2. [The Problem: Fat Controllers](#the-problem-fat-controllers)
3. [The Solution: System Pattern](#the-solution-system-pattern)
4. [Architecture Overview](#architecture-overview)
5. [System Implementation](#system-implementation)
6. [Handler Orchestration](#handler-orchestration)
7. [API Layer Design](#api-layer-design)
8. [Transaction Management](#transaction-management)
9. [Error Handling and Validation](#error-handling-and-validation)
10. [Testing Strategy](#testing-strategy)
11. [Performance Considerations](#performance-considerations)
12. [Security Integration](#security-integration)
13. [Real-World Examples](#real-world-examples)
14. [Anti-Patterns and Pitfalls](#anti-patterns-and-pitfalls)
15. [Future Evolution](#future-evolution)
16. [Conclusion](#conclusion)

## Introduction

The System pattern in Jarvis represents a fundamental architectural principle: **Azure Functions should be thin HTTP adapters, and Systems should orchestrate all business logic through handlers**. This separation ensures that business logic remains testable, reusable, and independent of the transport layer.

### Core Principles

1. **Functions are HTTP Adapters Only**: No business logic in Azure Functions
2. **Systems Own Orchestration**: All handler coordination happens in Systems
3. **Handlers Encapsulate Logic**: Business rules live in handlers, not Systems
4. **Components Store State**: Data is immutable and stored in components
5. **Entities Provide Identity**: GUIDs link components and enable relationships

## The Problem: Fat Controllers

### Traditional Azure Function Approach

```csharp
// ❌ ANTI-PATTERN: Fat Controller
[FunctionName("ProcessOrder")]
public async Task<IActionResult> ProcessOrder(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
    ILogger log)
{
    // Problem 1: Parsing and validation in function
    var requestBody = await req.ReadAsStringAsync();
    var orderRequest = JsonSerializer.Deserialize<OrderRequest>(requestBody);
    
    if (orderRequest == null || !IsValid(orderRequest))
    {
        return new BadRequestResult();
    }
    
    // Problem 2: Direct database access
    using var connection = new NpgsqlConnection(_connectionString);
    await connection.OpenAsync();
    
    // Problem 3: Business logic in function
    var order = new Order
    {
        Id = Guid.NewGuid(),
        CustomerId = orderRequest.CustomerId,
        Total = CalculateTotal(orderRequest.Items),
        Status = "Pending"
    };
    
    // Problem 4: Complex orchestration in function
    await connection.ExecuteAsync("INSERT INTO orders...", order);
    
    foreach (var item in orderRequest.Items)
    {
        await UpdateInventory(connection, item);
        await CreateOrderItem(connection, order.Id, item);
    }
    
    await SendOrderConfirmation(order);
    await NotifyWarehouse(order);
    
    return new OkObjectResult(order);
}
```

### Problems with Fat Controllers

1. **Untestable**: Requires HTTP context and real database
2. **Tight Coupling**: Business logic tied to Azure Functions
3. **No Reusability**: Logic can't be called from other contexts
4. **Poor Separation**: Mixing HTTP concerns with business logic
5. **Difficult Maintenance**: Changes require modifying Function code

## The Solution: System Pattern

### The Jarvis Approach

```csharp
// ✅ CORRECT: Thin Function + System
[FunctionName("ProcessOrder")]
public async Task<IActionResult> ProcessOrder(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
    OrderSystem orderSystem)
{
    // Function only handles HTTP concerns
    var orderRequest = await req.ReadFromJsonAsync<OrderRequest>();
    
    // Direct call to system method
    var orderId = await orderSystem.ProcessOrder(orderRequest);
    
    return new OkObjectResult(new { orderId });
}
```

### System Implementation

```csharp
public class OrderSystem : SystemBase
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<OrderSystem> _logger;
    
    public OrderSystem(IDataContext dataContext, ILogger<OrderSystem> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }
    
    public async Task<Guid> ProcessOrder(OrderRequest request)
    {
        // Validation (can be extracted to validator)
        ValidateOrderRequest(request);
        
        // Create order entity
        var orderId = Guid.NewGuid();
        
        // Orchestrate handlers
        var orderHandler = _dataContext.For<OrderHandler>(orderId);
        await orderHandler.Initialize(request.CustomerId, request.Items);
        
        // Process each item through handlers
        foreach (var item in request.Items)
        {
            var inventoryHandler = _dataContext.For<InventoryHandler>(item.ProductId);
            await inventoryHandler.Reserve(item.Quantity);
            
            var orderItemHandler = _dataContext.For<OrderItemHandler>(Guid.NewGuid());
            await orderItemHandler.Create(orderId, item);
        }
        
        // Update order status
        await orderHandler.UpdateStatus(OrderStatus.Confirmed);
        
        // Trigger side effects through handlers
        var notificationHandler = _dataContext.For<NotificationHandler>(orderId);
        await notificationHandler.SendOrderConfirmation();
        
        return orderId;
    }
}
```

## Architecture Overview

### Layered Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    HTTP Request                         │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│              Azure Function (Thin Layer)                │
│  - HTTP parsing        - Response formatting           │
│  - Route handling      - Status codes                  │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                   ISystem Interface                     │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                System Implementation                    │
│  - Orchestration       - Transaction boundaries        │
│  - Validation          - Error handling                │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                     DataContext                         │
│  - Handler resolution  - Component operations          │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                      Handlers                           │
│  - Business logic      - Component manipulation        │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                    Components                           │
│  - Data storage        - Immutable state              │
└─────────────────────────────────────────────────────────┘
```

### Key Relationships

```csharp
// System owns handler orchestration
System 
  → DataContext.For<THandler>(entityId)
    → Handler.Method(parameters)
      → Component operations

// Function directly calls System methods
Function 
  → System.Method(parameters)
    → Result
```

## System Implementation

### System Interface Pattern

Systems are concrete classes injected directly into Azure Functions:

```csharp
// Each system defines its own interface
public interface IOrderSystem
{
    Task<Guid> ProcessOrder(OrderRequest request);
    Task<OrderDetails> GetOrderDetails(Guid orderId);
    Task CancelOrder(Guid orderId);
}

// Or systems can be concrete classes without interfaces
public class OrderSystem : SystemBase
{
    // Direct methods that Functions call
    public async Task<Guid> ProcessOrder(OrderRequest request) { }
    public async Task<OrderDetails> GetOrderDetails(Guid orderId) { }
    public async Task CancelOrder(Guid orderId) { }
}
```

### System Base Class

```csharp
public abstract class SystemBase : ISystemBase
{
    protected IDataContext DataContext { get; private set; }
    protected ILogger Logger { get; private set; }
    protected IMetrics Metrics { get; private set; }
    
    public virtual void Initialize(
        IDataContext dataContext, 
        ILogger logger,
        IMetrics metrics)
    {
        DataContext = dataContext;
        Logger = logger;
        Metrics = metrics;
    }
    
    /// <summary>
    /// Execute operation with consistent error handling
    /// </summary>
    protected async Task<T> ExecuteWithMetrics<T>(
        string operationName,
        Func<Task<T>> operation)
    {
        using var activity = Activity.StartActivity($"System.{operationName}");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            Logger.LogDebug("Starting system operation: {Operation}", operationName);
            var result = await operation();
            
            Metrics.RecordSystemOperation(
                GetType().Name, 
                operationName, 
                stopwatch.ElapsedMilliseconds,
                success: true);
            
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "System operation failed: {Operation}", operationName);
            
            Metrics.RecordSystemOperation(
                GetType().Name, 
                operationName, 
                stopwatch.ElapsedMilliseconds,
                success: false);
            
            throw new SystemOperationException(
                $"Operation {operationName} failed", ex);
        }
    }
}
```

### System Registration

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJarvisSystems(
        this IServiceCollection services)
    {
        // Register systems as scoped services
        services.AddScoped<OrderSystem>();
        services.AddScoped<AuthenticationSystem>();
        services.AddScoped<PaymentSystem>();
        services.AddScoped<RegistrationSystem>();
        
        // Or auto-register all systems in assembly
        var systemTypes = Assembly.GetCallingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && 
                       t.Name.EndsWith("System"));
        
        foreach (var systemType in systemTypes)
        {
            services.AddScoped(systemType);
        }
        
        return services;
    }
}
```

## Handler Orchestration

### System as Handler Coordinator

```csharp
public class RegistrationSystem : SystemBase
{
    public async Task<RegistrationResult> RegisterAccount(
        RegistrationRequest request)
    {
        return await ExecuteWithMetrics("RegisterAccount", async () =>
        {
            // 1. Validation phase
            await ValidateRegistrationRequest(request);
            
            // 2. Create account entity
            var accountId = Guid.NewGuid();
            
            // 3. Transaction boundary at system level
            return await DataContext.InTransaction(async tx =>
            {
                // 4. Orchestrate multiple handlers
                
                // Create account through handler
                var accountHandler = tx.For<AccountHandler>(accountId);
                await accountHandler.CreateAccount(
                    request.Email, 
                    request.PasswordHash);
                
                // Create security profile
                var securityHandler = tx.For<SecurityProfileHandler>(accountId);
                await securityHandler.Initialize(
                    twoFactorEnabled: request.RequireTwoFactor);
                
                // Assign default role
                var roleHandler = tx.For<RoleHandler>(accountId);
                await roleHandler.AssignRole("User");
                
                // Create audit trail
                var auditHandler = tx.For<AuditHandler>(accountId);
                await auditHandler.LogRegistration(request.Email);
                
                return new RegistrationResult
                {
                    AccountId = accountId,
                    Email = request.Email,
                    RequireEmailVerification = true
                };
            });
        });
    }
    
    private async Task ValidateRegistrationRequest(RegistrationRequest request)
    {
        // Validation is system's responsibility, not handler's
        if (string.IsNullOrEmpty(request.Email))
            throw new ValidationException("Email is required");
        
        if (!EmailValidator.IsValid(request.Email))
            throw new ValidationException("Invalid email format");
        
        // Check for existing account
        var existingAccount = await DataContext.CreateQuery()
            .WithComponent<AccountComponent>()
            .Where<AccountComponent>(a => a.Email == request.Email)
            .ExecuteAsync();
        
        if (existingAccount.Any())
            throw new ConflictException("Account already exists");
    }
}
```

### Handler Communication Through System

```csharp
public class OrderFulfillmentSystem : SystemBase
{
    public async Task<FulfillmentResult> FulfillOrder(Guid orderId)
    {
        // System coordinates multiple handlers
        var orderHandler = DataContext.For<OrderHandler>(orderId);
        var order = await orderHandler.Get();
        
        if (order.Status != OrderStatus.Paid)
        {
            throw new InvalidOperationException(
                "Order must be paid before fulfillment");
        }
        
        var fulfillmentTasks = new List<Task<ShipmentInfo>>();
        
        // Process each order item
        foreach (var itemId in order.ItemIds)
        {
            fulfillmentTasks.Add(ProcessOrderItem(itemId));
        }
        
        var shipments = await Task.WhenAll(fulfillmentTasks);
        
        // Update order status
        await orderHandler.UpdateStatus(OrderStatus.Fulfilled);
        
        // Notify customer
        var notificationHandler = DataContext.For<NotificationHandler>(orderId);
        await notificationHandler.SendFulfillmentNotification(shipments);
        
        return new FulfillmentResult
        {
            OrderId = orderId,
            Shipments = shipments,
            FulfilledAt = DateTime.UtcNow
        };
    }
    
    private async Task<ShipmentInfo> ProcessOrderItem(Guid itemId)
    {
        var itemHandler = DataContext.For<OrderItemHandler>(itemId);
        var item = await itemHandler.Get();
        
        // Allocate inventory
        var inventoryHandler = DataContext.For<InventoryHandler>(item.ProductId);
        var allocation = await inventoryHandler.Allocate(item.Quantity);
        
        // Create shipment
        var shipmentId = Guid.NewGuid();
        var shipmentHandler = DataContext.For<ShipmentHandler>(shipmentId);
        await shipmentHandler.Create(itemId, allocation);
        
        return new ShipmentInfo
        {
            ShipmentId = shipmentId,
            TrackingNumber = await shipmentHandler.GenerateTrackingNumber()
        };
    }
}
```

## API Layer Design

### Thin Azure Functions

```csharp
public class OrderFunctions
{
    private readonly OrderSystem _orderSystem;
    
    public OrderFunctions(OrderSystem orderSystem)
    {
        _orderSystem = orderSystem;
    }
    
    [FunctionName("CreateOrder")]
    [OpenApiOperation("CreateOrder", tags: new[] { "Orders" })]
    [OpenApiRequestBody("application/json", typeof(CreateOrderRequest))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(OrderResponse))]
    public async Task<IActionResult> CreateOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] 
        HttpRequest req)
    {
        // Function only handles HTTP concerns
        var request = await req.ReadFromJsonAsync<CreateOrderRequest>();
        
        // Direct call to system method
        var orderId = await _orderSystem.CreateOrder(request);
        
        return new OkObjectResult(new OrderResponse { OrderId = orderId });
    }
    
    [FunctionName("GetOrder")]
    [OpenApiOperation("GetOrder", tags: new[] { "Orders" })]
    [OpenApiParameter("orderId", In = ParameterLocation.Path, Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(OrderDetails))]
    public async Task<IActionResult> GetOrder(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{orderId:guid}")] 
        HttpRequest req,
        Guid orderId)
    {
        // Direct call to system method
        var order = await _orderSystem.GetOrderDetails(orderId);
        
        return new OkObjectResult(order);
    }
    
    [FunctionName("CancelOrder")]
    [OpenApiOperation("CancelOrder", tags: new[] { "Orders" })]
    [OpenApiParameter("orderId", In = ParameterLocation.Path, Required = true)]
    [OpenApiResponseWithoutBody(HttpStatusCode.NoContent)]
    public async Task<IActionResult> CancelOrder(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "orders/{orderId:guid}")] 
        HttpRequest req,
        Guid orderId)
    {
        // Direct call to system method
        await _orderSystem.CancelOrder(orderId);
        
        return new NoContentResult();
    }
}
```

### API Guidelines

1. **No Business Logic**: Functions only parse HTTP and format responses
2. **No Direct Handler Access**: Always go through Systems
3. **No DataContext Injection**: Functions only inject concrete System classes
4. **Consistent Error Handling**: Let System handle business exceptions
5. **Clear HTTP Semantics**: Use proper verbs and status codes
6. **Direct System Calls**: Functions call system methods directly, no Execute wrapper

## Transaction Management

### System-Level Transactions

```csharp
public class PaymentSystem : SystemBase
{
    public async Task<PaymentResult> ProcessPayment(PaymentRequest request)
    {
        // Transaction boundary at system level
        return await DataContext.InTransaction(async tx =>
        {
            // All operations within same transaction
            var paymentId = Guid.NewGuid();
            
            // Create payment record
            var paymentHandler = tx.For<PaymentHandler>(paymentId);
            await paymentHandler.Create(request);
            
            // Update account balance
            var accountHandler = tx.For<AccountBalanceHandler>(request.AccountId);
            await accountHandler.Debit(request.Amount);
            
            // Update order status
            var orderHandler = tx.For<OrderHandler>(request.OrderId);
            await orderHandler.MarkAsPaid(paymentId);
            
            // Create ledger entries
            var ledgerHandler = tx.For<LedgerHandler>(Guid.NewGuid());
            await ledgerHandler.CreateDoubleEntry(
                debitAccount: request.AccountId,
                creditAccount: SystemAccounts.Revenue,
                amount: request.Amount);
            
            return new PaymentResult
            {
                PaymentId = paymentId,
                Status = PaymentStatus.Completed,
                TransactionId = GenerateTransactionId()
            };
        });
    }
}
```

### Distributed Transaction Patterns

```csharp
public class SagaSystem : SystemBase
{
    public async Task<BookingResult> BookTrip(TripBookingRequest request)
    {
        var bookingId = Guid.NewGuid();
        var compensations = new Stack<Func<Task>>();
        
        try
        {
            // Book flight
            var flightHandler = DataContext.For<FlightHandler>(bookingId);
            var flightReservation = await flightHandler.Reserve(request.Flight);
            compensations.Push(async () => await flightHandler.CancelReservation());
            
            // Book hotel
            var hotelHandler = DataContext.For<HotelHandler>(bookingId);
            var hotelReservation = await hotelHandler.Reserve(request.Hotel);
            compensations.Push(async () => await hotelHandler.CancelReservation());
            
            // Process payment
            var paymentHandler = DataContext.For<PaymentHandler>(bookingId);
            var payment = await paymentHandler.Process(request.Payment);
            compensations.Push(async () => await paymentHandler.Refund());
            
            // All successful - confirm bookings
            await flightHandler.Confirm();
            await hotelHandler.Confirm();
            
            return new BookingResult
            {
                BookingId = bookingId,
                FlightConfirmation = flightReservation.ConfirmationNumber,
                HotelConfirmation = hotelReservation.ConfirmationNumber
            };
        }
        catch (Exception ex)
        {
            // Compensate in reverse order
            Logger.LogError(ex, "Booking failed, executing compensations");
            
            while (compensations.Count > 0)
            {
                var compensation = compensations.Pop();
                try
                {
                    await compensation();
                }
                catch (Exception compEx)
                {
                    Logger.LogError(compEx, "Compensation failed");
                }
            }
            
            throw new BookingFailedException("Unable to complete booking", ex);
        }
    }
}
```

## Error Handling and Validation

### System-Level Error Handling

Systems handle their own errors internally:

```csharp
public abstract class SystemBase
{
    protected async Task<T> HandleErrors<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (ValidationException ex)
        {
            Logger.LogWarning(ex, "Validation failed");
            throw; // Let Function handle HTTP response
        }
        catch (NotFoundException ex)
        {
            Logger.LogWarning(ex, "Resource not found");
            throw; // Let Function return 404
        }
        catch (ConflictException ex)
        {
            Logger.LogWarning(ex, "Conflict detected");
            throw; // Let Function return 409
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unhandled error in system operation");
            throw new SystemException($"Operation failed: {ex.Message}", ex);
        }
    }
}

// In the system implementation
public class OrderSystem : SystemBase
{
    public async Task<Guid> CreateOrder(CreateOrderRequest request)
    {
        return await HandleErrors(async () =>
        {
            // Validation
            await ValidateOrderRequest(request);
            
            // Business logic
            // ...
            
            return orderId;
        });
    }
}
```

### Validation in Systems

```csharp
public abstract class ValidatingSystemBase : SystemBase
{
    private readonly IValidator _validator;
    
    protected async Task ValidateAsync<T>(T request) where T : class
    {
        var validationResult = await _validator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
            
            throw new ValidationException("Validation failed", errors);
        }
    }
}

public class OrderSystem : ValidatingSystemBase
{
    public async Task<Guid> CreateOrder(CreateOrderRequest request)
    {
        // Validation happens at system level
        await ValidateAsync(request);
        
        // Business rule validation
        if (request.Items.Sum(i => i.Quantity) == 0)
        {
            throw new BusinessRuleException("Order must contain at least one item");
        }
        
        // Proceed with handler orchestration
        // ...
    }
}
```

## Testing Strategy

### System Unit Tests

```csharp
public class OrderSystemTests
{
    private readonly Mock<IDataContext> _mockDataContext;
    private readonly OrderSystem _system;
    
    public OrderSystemTests()
    {
        _mockDataContext = new Mock<IDataContext>();
        var logger = new NullLogger<OrderSystem>();
        var metrics = new Mock<IMetrics>();
        
        _system = new OrderSystem();
        _system.Initialize(_mockDataContext.Object, logger, metrics.Object);
    }
    
    [Fact]
    public async Task CreateOrder_Should_Orchestrate_Handlers_Correctly()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Items = new[] { new OrderItem { ProductId = Guid.NewGuid(), Quantity = 2 } }
        };
        
        var mockOrderHandler = new Mock<IOrderHandler>();
        var mockInventoryHandler = new Mock<IInventoryHandler>();
        
        _mockDataContext
            .Setup(dc => dc.For<OrderHandler>(It.IsAny<Guid>()))
            .Returns(mockOrderHandler.Object);
        
        _mockDataContext
            .Setup(dc => dc.For<InventoryHandler>(It.IsAny<Guid>()))
            .Returns(mockInventoryHandler.Object);
        
        // Act
        var orderId = await _system.CreateOrder(request);
        
        // Assert
        orderId.ShouldNotBe(Guid.Empty);
        
        mockOrderHandler.Verify(h => h.Initialize(
            request.CustomerId, 
            It.IsAny<IEnumerable<OrderItem>>()), 
            Times.Once);
        
        mockInventoryHandler.Verify(h => h.Reserve(2), Times.Once);
    }
}
```

### Integration Tests

```csharp
public class OrderSystemIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateOrder_Should_Create_All_Components()
    {
        // Arrange
        var system = new OrderSystem();
        system.Initialize(TestDataContext(), Logger(), Metrics());
        
        var customerId = Guid.NewGuid();
        TrackEntity(customerId);
        
        var productId = Guid.NewGuid();
        TrackEntity(productId);
        
        // Setup inventory
        var inventoryHandler = TestDataContext().For<InventoryHandler>(productId);
        await inventoryHandler.SetStock(100);
        
        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            Items = new[]
            {
                new OrderItem { ProductId = productId, Quantity = 5, Price = 10.00m }
            }
        };
        
        // Act
        var orderId = await system.CreateOrder(request);
        TrackEntity(orderId);
        
        // Assert
        var orderHandler = TestDataContext().For<OrderHandler>(orderId);
        var order = await orderHandler.Get();
        
        order.ShouldNotBeNull();
        order.CustomerId.ShouldBe(customerId);
        order.Status.ShouldBe(OrderStatus.Confirmed);
        order.Total.ShouldBe(50.00m);
        
        // Verify inventory was updated
        var remainingStock = await inventoryHandler.GetAvailableStock();
        remainingStock.ShouldBe(95);
    }
}
```

### Function Tests

```csharp
public class OrderFunctionTests
{
    [Fact]
    public async Task CreateOrder_Function_Should_Only_Handle_HTTP()
    {
        // Arrange
        var mockOrderSystem = new Mock<OrderSystem>();
        mockOrderSystem
            .Setup(s => s.CreateOrder(It.IsAny<CreateOrderRequest>()))
            .ReturnsAsync(Guid.NewGuid());
        
        var function = new OrderFunctions(mockOrderSystem.Object);
        
        var request = new DefaultHttpRequest(new DefaultHttpContext());
        request.Body = new MemoryStream(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new CreateOrderRequest())));
        
        // Act
        var result = await function.CreateOrder(request);
        
        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        mockOrderSystem.Verify(s => s.CreateOrder(
            It.IsAny<CreateOrderRequest>()), 
            Times.Once);
    }
}
```

## Performance Considerations

### System Caching

```csharp
public class CachedSystem : SystemBase
{
    private readonly IMemoryCache _cache;
    
    protected async Task<T> GetOrAddAsync<T>(
        string key, 
        Func<Task<T>> factory, 
        TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue<T>(key, out var cached))
        {
            Metrics.RecordCacheHit(key);
            return cached;
        }
        
        var value = await factory();
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = expiration ?? TimeSpan.FromMinutes(5)
        };
        
        _cache.Set(key, value, cacheOptions);
        Metrics.RecordCacheMiss(key);
        
        return value;
    }
}

public class ProductCatalogSystem : CachedSystem
{
    public async Task<ProductCatalog> GetCatalog(string category)
    {
        return await GetOrAddAsync(
            $"catalog:{category}",
            async () =>
            {
                var products = await DataContext.CreateQuery()
                    .WithComponent<ProductComponent>()
                    .Where<ProductComponent>(p => p.Category == category)
                    .Where<ProductComponent>(p => p.IsActive)
                    .ExecuteAsync();
                
                return new ProductCatalog
                {
                    Category = category,
                    Products = await LoadProductDetails(products)
                };
            },
            TimeSpan.FromMinutes(15));
    }
}
```

### Parallel Handler Execution

```csharp
public class BulkOperationSystem : SystemBase
{
    public async Task<BulkUpdateResult> BulkUpdatePrices(
        Dictionary<Guid, decimal> priceUpdates)
    {
        const int batchSize = 10;
        var batches = priceUpdates
            .Select((kvp, index) => new { kvp, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.kvp).ToDictionary(x => x.Key, x => x.Value));
        
        var results = new ConcurrentBag<UpdateResult>();
        
        await Parallel.ForEachAsync(batches, async (batch, ct) =>
        {
            foreach (var (productId, newPrice) in batch)
            {
                try
                {
                    var handler = DataContext.For<ProductHandler>(productId);
                    await handler.UpdatePrice(newPrice);
                    
                    results.Add(new UpdateResult 
                    { 
                        ProductId = productId, 
                        Success = true 
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to update price for {ProductId}", productId);
                    results.Add(new UpdateResult 
                    { 
                        ProductId = productId, 
                        Success = false, 
                        Error = ex.Message 
                    });
                }
            }
        });
        
        return new BulkUpdateResult
        {
            TotalUpdates = priceUpdates.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            Results = results.ToList()
        };
    }
}
```

## Security Integration

### Security System Pattern

```csharp
public class SecureSystemBase : SystemBase
{
    protected ICurrentUser CurrentUser { get; private set; }
    protected IAuthorizationService AuthorizationService { get; private set; }
    
    public override void Initialize(
        IDataContext dataContext, 
        ILogger logger,
        IMetrics metrics)
    {
        base.Initialize(dataContext, logger, metrics);
        
        // Additional security initialization
        CurrentUser = ServiceProvider.GetRequiredService<ICurrentUser>();
        AuthorizationService = ServiceProvider.GetRequiredService<IAuthorizationService>();
    }
    
    protected async Task RequirePermissionAsync(string permission)
    {
        if (!await AuthorizationService.HasPermission(CurrentUser.Id, permission))
        {
            throw new ForbiddenException($"Permission required: {permission}");
        }
    }
    
    protected async Task RequireOwnershipAsync(Guid entityId)
    {
        var owner = await DataContext.GetOwner(entityId);
        if (owner != CurrentUser.Id)
        {
            throw new ForbiddenException("You don't own this resource");
        }
    }
}

public class AdminSystem : SecureSystemBase
{
    public async Task<UserList> GetAllUsers()
    {
        // Require admin permission
        await RequirePermissionAsync("admin.users.read");
        
        var users = await DataContext.CreateQuery()
            .WithComponent<UserComponent>()
            .ExecuteAsync();
        
        return new UserList { Users = await LoadUserDetails(users) };
    }
    
    public async Task SuspendUser(Guid userId)
    {
        // Require specific permission
        await RequirePermissionAsync("admin.users.suspend");
        
        // Audit the action
        var auditHandler = DataContext.For<AuditHandler>(userId);
        await auditHandler.LogAdminAction(
            CurrentUser.Id, 
            "UserSuspended",
            new { Reason = "Terms violation" });
        
        // Perform suspension
        var userHandler = DataContext.For<UserHandler>(userId);
        await userHandler.Suspend();
    }
}
```

## Real-World Examples

### 1. E-Commerce Order Processing

```csharp
public class EcommerceOrderSystem : SystemBase
{
    public async Task<OrderConfirmation> ProcessOrder(OrderRequest request)
    {
        // Complex orchestration of multiple concerns
        return await DataContext.InTransaction(async tx =>
        {
            // 1. Create order
            var orderId = Guid.NewGuid();
            var orderHandler = tx.For<OrderHandler>(orderId);
            await orderHandler.Create(request.CustomerId, request.Items);
            
            // 2. Validate and reserve inventory
            foreach (var item in request.Items)
            {
                var inventoryHandler = tx.For<InventoryHandler>(item.ProductId);
                
                if (!await inventoryHandler.HasStock(item.Quantity))
                {
                    throw new OutOfStockException($"Product {item.ProductId} is out of stock");
                }
                
                await inventoryHandler.Reserve(item.Quantity, orderId);
            }
            
            // 3. Calculate pricing
            var pricingHandler = tx.For<PricingHandler>(orderId);
            var pricing = await pricingHandler.Calculate(request.Items, request.PromoCode);
            
            // 4. Process payment
            var paymentHandler = tx.For<PaymentHandler>(Guid.NewGuid());
            var payment = await paymentHandler.Process(
                request.PaymentMethod,
                pricing.Total);
            
            // 5. Update order with payment
            await orderHandler.AttachPayment(payment.Id);
            await orderHandler.UpdateStatus(OrderStatus.Paid);
            
            // 6. Trigger fulfillment
            var fulfillmentHandler = tx.For<FulfillmentHandler>(orderId);
            await fulfillmentHandler.InitiateFulfillment();
            
            // 7. Send confirmation
            var notificationHandler = tx.For<NotificationHandler>(orderId);
            await notificationHandler.SendOrderConfirmation();
            
            return new OrderConfirmation
            {
                OrderId = orderId,
                OrderNumber = await orderHandler.GenerateOrderNumber(),
                Total = pricing.Total,
                EstimatedDelivery = await fulfillmentHandler.EstimateDelivery()
            };
        });
    }
}
```

### 2. User Authentication System

```csharp
public class AuthenticationSystem : SystemBase
{
    private readonly ITokenService _tokenService;
    private readonly IPasswordService _passwordService;
    
    public async Task<AuthenticationResult> Authenticate(LoginRequest request)
    {
        // Find account
        var accounts = await DataContext.CreateQuery()
            .WithComponent<AccountComponent>()
            .Where<AccountComponent>(a => a.Email == request.Email)
            .ExecuteAsync();
        
        if (!accounts.Any())
        {
            // Constant time delay to prevent timing attacks
            await _passwordService.HashPassword("dummy");
            throw new AuthenticationException("Invalid credentials");
        }
        
        var accountId = accounts.First();
        var accountHandler = DataContext.For<AccountHandler>(accountId);
        var account = await accountHandler.Get();
        
        // Verify password
        if (!await _passwordService.VerifyPassword(
            request.Password, 
            account.PasswordHash))
        {
            // Log failed attempt
            var securityHandler = DataContext.For<SecurityHandler>(accountId);
            await securityHandler.LogFailedLogin(request.IpAddress);
            
            // Check for account lockout
            if (await securityHandler.IsLockedOut())
            {
                throw new AccountLockedException("Account is locked");
            }
            
            throw new AuthenticationException("Invalid credentials");
        }
        
        // Check 2FA requirement
        var securityProfile = await DataContext.For<SecurityProfileHandler>(accountId).Get();
        if (securityProfile.TwoFactorEnabled && string.IsNullOrEmpty(request.TwoFactorCode))
        {
            return new AuthenticationResult
            {
                RequiresTwoFactor = true,
                TwoFactorMethod = securityProfile.TwoFactorMethod
            };
        }
        
        // Verify 2FA if provided
        if (securityProfile.TwoFactorEnabled)
        {
            var twoFactorHandler = DataContext.For<TwoFactorHandler>(accountId);
            if (!await twoFactorHandler.VerifyCode(request.TwoFactorCode))
            {
                throw new AuthenticationException("Invalid 2FA code");
            }
        }
        
        // Generate tokens
        var tokens = await _tokenService.GenerateTokens(accountId, account.Email);
        
        // Update last login
        await accountHandler.UpdateLastLogin();
        
        // Clear any lockout
        var lockoutHandler = DataContext.For<LockoutHandler>(accountId);
        await lockoutHandler.Clear();
        
        return new AuthenticationResult
        {
            Success = true,
            Tokens = tokens,
            AccountId = accountId
        };
    }
}
```

### 3. Workflow Orchestration System

```csharp
public class WorkflowSystem : SystemBase
{
    public async Task<WorkflowExecutionResult> ExecuteWorkflow(
        Guid workflowId, 
        Dictionary<string, object> parameters)
    {
        var workflowHandler = DataContext.For<WorkflowHandler>(workflowId);
        var workflow = await workflowHandler.Get();
        
        var executionId = Guid.NewGuid();
        var context = new WorkflowContext
        {
            WorkflowId = workflowId,
            ExecutionId = executionId,
            Parameters = parameters,
            State = new Dictionary<string, object>()
        };
        
        // Execute each step in order
        foreach (var stepId in workflow.Steps)
        {
            var stepHandler = DataContext.For<WorkflowStepHandler>(stepId);
            var step = await stepHandler.Get();
            
            try
            {
                // Check conditions
                if (!await EvaluateConditions(step.Conditions, context))
                {
                    Logger.LogInformation("Skipping step {StepId} due to conditions", stepId);
                    continue;
                }
                
                // Execute step
                var stepResult = await ExecuteStep(step, context);
                context.State[step.Name] = stepResult;
                
                // Log execution
                await stepHandler.LogExecution(executionId, stepResult);
            }
            catch (Exception ex)
            {
                // Handle step failure
                if (step.ContinueOnError)
                {
                    Logger.LogWarning(ex, "Step {StepId} failed but continuing", stepId);
                    context.State[step.Name] = new { Error = ex.Message };
                }
                else
                {
                    throw new WorkflowExecutionException(
                        $"Step {step.Name} failed", ex);
                }
            }
        }
        
        return new WorkflowExecutionResult
        {
            ExecutionId = executionId,
            Status = WorkflowStatus.Completed,
            Output = context.State
        };
    }
}
```

## Anti-Patterns and Pitfalls

### ❌ Anti-Pattern: Business Logic in Functions

```csharp
// WRONG: Function contains business logic
[FunctionName("ProcessPayment")]
public async Task<IActionResult> ProcessPayment(
    [HttpTrigger] HttpRequest req,
    IDataContext dataContext) // ❌ Should not inject DataContext
{
    var request = await req.ReadFromJsonAsync<PaymentRequest>();
    
    // ❌ Business logic in function
    if (request.Amount <= 0)
        return new BadRequestResult();
    
    // ❌ Direct handler access
    var paymentHandler = dataContext.For<PaymentHandler>(Guid.NewGuid());
    var accountHandler = dataContext.For<AccountHandler>(request.AccountId);
    
    // ❌ Orchestration in function
    var balance = await accountHandler.GetBalance();
    if (balance < request.Amount)
        return new BadRequestObjectResult("Insufficient funds");
    
    await accountHandler.Debit(request.Amount);
    await paymentHandler.Process(request);
    
    return new OkResult();
}
```

### ✅ Correct Pattern: System Orchestration

```csharp
// CORRECT: Function is thin, System orchestrates
[FunctionName("ProcessPayment")]
public async Task<IActionResult> ProcessPayment(
    [HttpTrigger] HttpRequest req,
    PaymentSystem paymentSystem) // ✅ Inject concrete system
{
    var request = await req.ReadFromJsonAsync<PaymentRequest>();
    
    // ✅ Direct call to system method
    var result = await paymentSystem.ProcessPayment(request);
    
    return new OkObjectResult(result);
}
```

### Common Pitfalls

1. **Mixing Concerns**: Don't put HTTP handling in Systems
2. **Skipping Systems**: Don't let Functions access handlers directly
3. **Fat Systems**: Systems orchestrate, they don't implement business logic
4. **Transaction Scope**: Keep transactions at the System level
5. **Error Handling**: Let Systems handle business errors, Functions handle HTTP

## Future Evolution

### 1. System Composition

```csharp
public interface IComposableSystem
{
    Task<T> Compose<T>(params ISystemOperation[] operations);
}

public class OrderSystemV2 : ComposableSystem
{
    public async Task<OrderResult> CreateOrder(OrderRequest request)
    {
        return await Compose<OrderResult>(
            new ValidateOrderOperation(request),
            new ReserveInventoryOperation(request.Items),
            new CalculatePricingOperation(request),
            new ProcessPaymentOperation(request.Payment),
            new CreateOrderOperation(request),
            new NotifyCustomerOperation()
        );
    }
}
```

### 2. System Metadata and Discovery

```csharp
[SystemMetadata(
    Name = "Order Management",
    Version = "2.0",
    Description = "Handles order lifecycle",
    Tags = new[] { "ecommerce", "orders" })]
public class OrderSystem : SystemBase
{
    [SystemOperation(
        Name = "CreateOrder",
        Description = "Creates a new order",
        RequiredPermissions = new[] { "orders.create" })]
    public async Task<Guid> CreateOrder(CreateOrderRequest request)
    {
        // Implementation
    }
}
```

### 3. System Event Sourcing

```csharp
public abstract class EventSourcedSystem : SystemBase
{
    protected async Task<T> ExecuteWithEvents<T>(
        string aggregateId,
        Func<Task<T>> operation,
        params DomainEvent[] events)
    {
        var result = await operation();
        
        foreach (var @event in events)
        {
            await EventStore.Append(aggregateId, @event);
        }
        
        return result;
    }
}
```

## Conclusion

The System pattern is fundamental to building maintainable, testable, and scalable applications with Jarvis. By enforcing strict separation between HTTP concerns and business logic, Systems enable:

### Key Benefits

1. **Testability**: Systems can be tested without HTTP context
2. **Reusability**: Business logic can be called from any context
3. **Maintainability**: Clear separation of concerns
4. **Scalability**: Systems can be optimized independently
5. **Security**: Centralized authorization and validation

### Best Practices

1. **Keep Functions Thin**: Only HTTP parsing and response formatting
2. **Direct System Injection**: Functions inject and call Systems directly
3. **Systems Orchestrate**: Coordinate handlers, don't implement logic
4. **Handlers Encapsulate**: Business rules live in handlers
5. **Transaction Boundaries**: Manage transactions at System level
6. **Error Handling**: Business errors in Systems, HTTP errors in Functions

### Architecture Principles

1. **Single Responsibility**: Each layer has one clear purpose
2. **Dependency Direction**: Functions → Systems → Handlers → Components
3. **Testability First**: Every layer independently testable
4. **Explicit Over Implicit**: Clear contracts between layers
5. **Composition Over Inheritance**: Prefer composition patterns

The System pattern ensures that Jarvis applications remain maintainable and evolvable as complexity grows, providing a solid foundation for enterprise-scale applications.

---

*Document Version: 1.0*  
*Last Updated: January 2025*  
*Authors: Jarvis Development Team*