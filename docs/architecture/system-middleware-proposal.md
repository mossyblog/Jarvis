# System Middleware Layer for Jarvis ECS Framework

## Executive Summary

This proposal introduces a **System Layer** that serves as middleware between Azure Functions and the existing Handler/Component architecture. The System Layer orchestrates handler execution without touching components directly, providing clean separation of concerns and enabling comprehensive unit testing without Azure Functions runtime dependencies.

## Problem Statement

Currently, Azure Functions in `core.jarvis.api` carry too many responsibilities:
- HTTP request/response mapping
- Authorization logic
- Business validation
- Transaction management
- Handler orchestration
- Error handling and logging

This coupling makes it difficult to:
1. Unit test business logic without Azure Functions runtime
2. Reuse orchestration logic across different entry points
3. Apply consistent cross-cutting concerns
4. Maintain clear separation of responsibilities

## Proposed Architecture

### Core Concept: System as Handler Orchestrator

In the ECS pattern, the **System** represents the processing layer that orchestrates how handlers execute, without directly manipulating components. This aligns with ECS principles where:
- **Entities** = Identity (GUIDs)
- **Components** = Data (Records)
- **Handlers** = Domain Logic (Business Operations)
- **Systems** = Orchestration (Execution Infrastructure)

### Key Design Principles

1. **Systems never touch components directly** - Only handlers manipulate component data
2. **Systems orchestrate handler execution** - Managing flow, dependencies, and infrastructure
3. **Handlers own all domain logic** - Validation, authorization, business rules, and commits
4. **Azure Functions become thin HTTP adapters** - Only responsible for HTTP concerns

## Detailed Design

### Important Note on Handler Pattern

In the Jarvis framework:
- **Handlers only have a `Get()` method by default**
- **Creation and updates use `DataContext.Commit()` directly**
- **Handlers can define domain-specific methods** (like `Activate()`, `CompleteWorkOrder()`, etc.)
- **No generic CRUD operations** - each handler defines its own domain operations

### 1. Core System Interface

```csharp
namespace core.jarvis.Systems
{
    /// <summary>
    /// System orchestrates handler execution without exposing DataContext
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// Execute a handler method that returns an entity ID
        /// </summary>
        Task<Guid> ExecuteHandler<THandler>(Guid entityId, Func<THandler, Task<Guid>> handlerMethod) 
            where THandler : IComponentHandler;
        
        /// <summary>
        /// Execute a handler method that returns a component
        /// </summary>
        Task<TComponent> ExecuteHandler<THandler, TComponent>(Guid entityId, Func<THandler, Task<TComponent>> handlerMethod) 
            where THandler : IComponentHandler
            where TComponent : IComponent;
        
        /// <summary>
        /// Execute a handler method with no return value
        /// </summary>
        Task ExecuteHandler<THandler>(Guid entityId, Func<THandler, Task> handlerMethod) 
            where THandler : IComponentHandler;
        
        /// <summary>
        /// Create a new component (no handler needed for creation)
        /// </summary>
        Task<Guid> CreateComponent<TComponent>(TComponent component) 
            where TComponent : IComponent;
    }
}
```

### 2. Basic System Implementation

```csharp
namespace core.jarvis.Systems
{
    public class HandlerSystem : ISystem
    {
        private readonly IDataContext _dataContext;
        private readonly ILogger<HandlerSystem> _logger;
        
        public HandlerSystem(IDataContext dataContext, ILogger<HandlerSystem> logger)
        {
            _dataContext = dataContext;
            _logger = logger;
        }
        
        public async Task<Guid> ExecuteHandler<THandler>(Guid entityId, Func<THandler, Task<Guid>> handlerMethod) 
            where THandler : IComponentHandler
        {
            _logger.LogDebug("Executing {HandlerType} for entity {EntityId}", typeof(THandler).Name, entityId);
            
            var handler = _dataContext.For<THandler>(entityId);
            var result = await handlerMethod(handler);
            
            _logger.LogDebug("Handler operation completed: {EntityId}", result);
            return result;
        }
        
        public async Task<TComponent> ExecuteHandler<THandler, TComponent>(Guid entityId, Func<THandler, Task<TComponent>> handlerMethod) 
            where THandler : IComponentHandler
            where TComponent : IComponent
        {
            _logger.LogDebug("Executing {HandlerType} for entity {EntityId}", typeof(THandler).Name, entityId);
            
            var handler = _dataContext.For<THandler>(entityId);
            var result = await handlerMethod(handler);
            
            _logger.LogDebug("Handler operation completed");
            return result;
        }
        
        public async Task ExecuteHandler<THandler>(Guid entityId, Func<THandler, Task> handlerMethod) 
            where THandler : IComponentHandler
        {
            _logger.LogDebug("Executing {HandlerType} for entity {EntityId}", typeof(THandler).Name, entityId);
            
            var handler = _dataContext.For<THandler>(entityId);
            await handlerMethod(handler);
            
            _logger.LogDebug("Handler operation completed");
        }
        
        public async Task<Guid> CreateComponent<TComponent>(TComponent component) 
            where TComponent : IComponent
        {
            _logger.LogDebug("Creating {ComponentType}", typeof(TComponent).Name);
            
            var entityId = Guid.NewGuid();
            component.OwnerEntityId = entityId;
            component.Id = Guid.NewGuid();
            
            await _dataContext.Commit(component);
            
            _logger.LogDebug("Component created: {EntityId}", entityId);
            return entityId;
        }
    }
}
```

### 3. System with Transaction Support

```csharp
namespace core.jarvis.Systems
{
    /// <summary>
    /// System that ensures operations run in transactions
    /// </summary>
    public class TransactionalSystem : ISystem
    {
        private readonly IDataContext _dataContext;
        private readonly ILogger<TransactionalSystem> _logger;
        
        public TransactionalSystem(IDataContext dataContext, ILogger<TransactionalSystem> logger)
        {
            _dataContext = dataContext;
            _logger = logger;
        }
        
        public async Task<Guid> ExecuteHandler<THandler>(Guid entityId, Func<THandler, Task<Guid>> handlerMethod) 
            where THandler : IComponentHandler
        {
            _logger.LogDebug("Starting transactional handler execution");
            
            return await _dataContext.InTransaction(async tx =>
            {
                var handler = tx.For<THandler>(entityId);
                var result = await handlerMethod(handler);
                _logger.LogDebug("Transaction completed successfully");
                return result;
            });
        }
        
        public async Task<TComponent> ExecuteHandler<THandler, TComponent>(Guid entityId, Func<THandler, Task<TComponent>> handlerMethod) 
            where THandler : IComponentHandler
            where TComponent : IComponent
        {
            _logger.LogDebug("Starting transactional handler execution for {ComponentType}", typeof(TComponent).Name);
            
            return await _dataContext.InTransaction(async tx =>
            {
                var handler = tx.For<THandler>(entityId);
                var result = await handlerMethod(handler);
                _logger.LogDebug("Transaction completed successfully");
                return result;
            });
        }
        
        public async Task ExecuteHandler<THandler>(Guid entityId, Func<THandler, Task> handlerMethod) 
            where THandler : IComponentHandler
        {
            _logger.LogDebug("Starting transactional handler execution");
            
            await _dataContext.InTransaction(async tx =>
            {
                var handler = tx.For<THandler>(entityId);
                await handlerMethod(handler);
                _logger.LogDebug("Transaction completed successfully");
            });
        }
        
        public async Task<Guid> CreateComponent<TComponent>(TComponent component) 
            where TComponent : IComponent
        {
            _logger.LogDebug("Starting transactional component creation");
            
            return await _dataContext.InTransaction(async tx =>
            {
                var entityId = Guid.NewGuid();
                component.OwnerEntityId = entityId;
                component.Id = Guid.NewGuid();
                
                await tx.Commit(component);
                _logger.LogDebug("Transaction completed successfully");
                return entityId;
            });
        }
    }
}
```

### 4. Azure Function Integration

```csharp
namespace core.jarvis.api.Functions
{
    public class WorkOrderFunctions
    {
        private readonly ISystem _system;
        
        public WorkOrderFunctions(ISystem system)
        {
            _system = system;
        }
        
        [Function("CreateWorkOrder")]
        public async Task<IActionResult> CreateWorkOrder(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "workorders")] 
            HttpRequest req)
        {
            // ONLY parse JSON - no logic
            var component = await req.ReadJsonAsync<WorkOrderComponent>();
            
            // System handles creation - no business logic in Function
            var entityId = await _system.CreateComponent(component);
            
            return new OkObjectResult(new { id = entityId });
        }
        
        [Function("GetWorkOrder")]
        public async Task<IActionResult> GetWorkOrder(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "workorders/{id}")] 
            HttpRequest req,
            Guid id)
        {
            // Call handler method - no inline logic
            var component = await _system.ExecuteHandler<WorkOrderHandler, WorkOrderComponent>(
                id, 
                handler => handler.Get());
            
            return new OkObjectResult(component);
        }
        
        [Function("CompleteWorkOrder")]
        public async Task<IActionResult> CompleteWorkOrder(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "workorders/{id}/complete")] 
            HttpRequest req,
            Guid id)
        {
            // Call domain-specific handler method - all logic in handler
            await _system.ExecuteHandler<WorkOrderHandler>(
                id,
                handler => handler.CompleteWorkOrder());
            
            return new NoContentResult();
        }
        
        [Function("AssignWorkOrder")]
        public async Task<IActionResult> AssignWorkOrder(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "workorders/{id}/assign")] 
            HttpRequest req,
            Guid id)
        {
            // Parse request data
            var assignment = await req.ReadJsonAsync<WorkOrderAssignment>();
            
            // Call handler method with parameters - logic stays in handler
            await _system.ExecuteHandler<WorkOrderHandler>(
                id,
                handler => handler.AssignTo(assignment.TechnicianId, assignment.ScheduledDate));
            
            return new NoContentResult();
        }
    }
}
```

### 5. Complex Operations - Use Workflow Handlers

For complex operations that involve multiple entities, create dedicated workflow handlers:

```csharp
namespace core.jarvis.api.Handlers
{
    /// <summary>
    /// Workflow handler that orchestrates complex multi-entity operations
    /// </summary>
    public class WorkOrderWorkflowHandler : IComponentHandler
    {
        private readonly IDataContext _dataContext;
        private readonly ILogger<WorkOrderWorkflowHandler> _logger;
        private Guid _entityId;
        
        public WorkOrderWorkflowHandler(IDataContext dataContext, ILogger<WorkOrderWorkflowHandler> logger)
        {
            _dataContext = dataContext;
            _logger = logger;
        }
        
        public void InitializeContext(Guid entityId)
        {
            _entityId = entityId;
        }
        
        public Task<IComponent> Get() => throw new NotSupportedException("Workflow handlers don't have components");
        
        /// <summary>
        /// Creates a work order with an initial invoice
        /// </summary>
        public async Task<WorkOrderCreationResult> CreateWithInvoice(
            WorkOrderComponent workOrder, 
            InvoiceComponent invoice)
        {
            // Create work order
            var workOrderId = Guid.NewGuid();
            workOrder.OwnerEntityId = workOrderId;
            workOrder.Id = Guid.NewGuid();
            await _dataContext.Commit(workOrder);
            
            // Create invoice linked to work order
            var invoiceId = Guid.NewGuid();
            invoice.OwnerEntityId = invoiceId;
            invoice.Id = Guid.NewGuid();
            invoice.WorkOrderId = workOrderId;
            await _dataContext.Commit(invoice);
            
            // Establish relationship
            await _dataContext.LinkRelationship(workOrderId, invoiceId, "WorkOrder", "Invoice");
            
            _logger.LogInformation("Created work order {WorkOrderId} with invoice {InvoiceId}", 
                workOrderId, invoiceId);
            
            return new WorkOrderCreationResult(workOrderId, invoiceId);
        }
    }
}

namespace core.jarvis.api.Functions
{
    public class WorkOrderWorkflowFunctions
    {
        private readonly ISystem _system;
        
        public WorkOrderWorkflowFunctions(ISystem system)
        {
            _system = system;
        }
        
        [Function("CreateWorkOrderWithInvoice")]
        public async Task<IActionResult> CreateWorkOrderWithInvoice(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "workorders/with-invoice")] 
            HttpRequest req)
        {
            // Parse request - NO LOGIC
            var request = await req.ReadJsonAsync<CreateWorkOrderWithInvoiceRequest>();
            
            // Call workflow handler - ALL LOGIC IN HANDLER
            var result = await _system.ExecuteHandler<WorkOrderWorkflowHandler, WorkOrderCreationResult>(
                Guid.Empty, // Workflow handlers don't need entity ID for creation
                handler => handler.CreateWithInvoice(request.WorkOrder, request.Invoice));
            
            return new OkObjectResult(result);
        }
    }
}

// Example of a handler with domain-specific methods
public class WorkOrderHandler : ComponentHandler<WorkOrderComponent>
{
    public WorkOrderHandler(IDataContext dataContext, ILogger<WorkOrderHandler> logger)
        : base(dataContext, logger)
    {
    }
    
    /// <summary>
    /// Domain-specific method to complete a work order
    /// </summary>
    public async Task CompleteWorkOrder()
    {
        var workOrder = await Get();
        
        // Business validation
        if (workOrder.Status == "Completed")
        {
            throw new BusinessRuleException("WorkOrder", "Work order is already completed");
        }
        
        if (string.IsNullOrEmpty(workOrder.AssignedTo))
        {
            throw new BusinessRuleException("WorkOrder", "Cannot complete unassigned work order");
        }
        
        // Update status
        var completed = workOrder with 
        { 
            Status = "Completed",
            CompletedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        
        await DataContext.Commit(completed);
        
        // Emit domain event
        await DataContext.Emit(new WorkOrderCompletedEvent(workOrder.OwnerEntityId));
    }
    
    /// <summary>
    /// Assigns work order to a technician
    /// </summary>
    public async Task AssignTo(Guid technicianId, DateTime scheduledDate)
    {
        var workOrder = await Get();
        
        // Business rules
        if (workOrder.Status == "Completed")
        {
            throw new BusinessRuleException("WorkOrder", "Cannot assign completed work order");
        }
        
        if (scheduledDate < DateTime.UtcNow.Date)
        {
            throw new BusinessRuleException("WorkOrder", "Cannot schedule work order in the past");
        }
        
        // Update assignment
        var assigned = workOrder with
        {
            AssignedTo = technicianId.ToString(),
            ScheduledDate = scheduledDate,
            Status = "Assigned",
            LastUpdated = DateTime.UtcNow
        };
        
        await DataContext.Commit(assigned);
        
        Logger.LogInformation("Work order {WorkOrderId} assigned to {TechnicianId}", 
            OwnerEntityId, technicianId);
    }
}
```

### 6. Dependency Injection

```csharp
namespace core.jarvis.api
{
    public static class SystemServiceExtensions
    {
        public static IServiceCollection AddJarvisSystem(this IServiceCollection services)
        {
            // Register system based on configuration
            services.AddScoped<ISystem>(sp =>
            {
                var dataContext = sp.GetRequiredService<IDataContext>();
                var logger = sp.GetRequiredService<ILogger<HandlerSystem>>();
                var config = sp.GetRequiredService<IConfiguration>();
                
                // Use transactional system if configured
                if (config.GetValue<bool>("System:UseTransactions", true))
                {
                    return new TransactionalSystem(
                        dataContext, 
                        sp.GetRequiredService<ILogger<TransactionalSystem>>());
                }
                
                return new HandlerSystem(dataContext, logger);
            });
            
            return services;
        }
    }
}
```

## Implementation Tasks

### Task 1: Create System Interface and Base Implementation
**Description**: Build the foundational `ISystem` interface and `HandlerSystem` implementation.

**Subtasks**:
1. Create `ISystem` interface in `core.jarvis.Systems`
2. Implement `HandlerSystem` with basic logging
3. Add unit tests for HandlerSystem
4. Create `SystemServiceExtensions` for DI registration

**Success Criteria**:
- [ ] `ISystem` interface is clean and focused
- [ ] `HandlerSystem` implements all interface methods
- [ ] Unit tests cover happy path and error scenarios
- [ ] DI registration works correctly

**Estimated Effort**: 3-4 hours

---

### Task 2: Implement TransactionalSystem
**Description**: Create system variant that wraps all operations in transactions.

**Subtasks**:
1. Create `TransactionalSystem` class
2. Implement transaction wrapping for all execute methods
3. Add configuration support for transaction behavior
4. Add unit tests for transaction scenarios

**Success Criteria**:
- [ ] All operations run within transactions
- [ ] Transaction isolation levels are configurable
- [ ] Rollback occurs on exceptions
- [ ] Tests verify transaction behavior

**Estimated Effort**: 4-5 hours

---

### Task 3: Refactor Existing Azure Functions
**Description**: Update existing Azure Functions to use the System layer.

**Subtasks**:
1. Identify all Azure Functions with handler orchestration
2. Refactor to use `ISystem.Execute()`
3. Remove business logic from Functions
4. Update Function tests

**Success Criteria**:
- [ ] All Functions use System for handler execution
- [ ] Functions only handle HTTP concerns
- [ ] No business logic remains in Functions
- [ ] All existing tests still pass

**Estimated Effort**: 6-8 hours

---

### Task 4: Create System Variants
**Description**: Implement specialized systems for different scenarios.

**Subtasks**:
1. Create `MonitoredSystem` with telemetry
2. Create `ResilientSystem` with retry logic
3. Create `CachedSystem` with result caching
4. Add configuration for system selection

**Success Criteria**:
- [ ] Each system variant works correctly
- [ ] Systems are composable (can wrap each other)
- [ ] Configuration drives system selection
- [ ] Performance overhead is acceptable

**Estimated Effort**: 8-10 hours

---

### Task 5: Integration Testing
**Description**: Comprehensive testing of System layer with real handlers.

**Test Scenarios**:
1. Simple handler execution
2. Multi-handler orchestration
3. Transaction rollback scenarios
4. Error propagation
5. Concurrent system usage
6. Performance benchmarks

**Success Criteria**:
- [ ] All scenarios pass
- [ ] No memory leaks
- [ ] Performance meets requirements
- [ ] Error handling is robust

**Estimated Effort**: 6-8 hours

---

### Task 6: Documentation and Examples
**Description**: Create comprehensive documentation.

**Deliverables**:
1. System layer architecture guide
2. Migration guide from direct handler usage
3. Example: Simple CRUD with System
4. Example: Complex workflow orchestration
5. Best practices document

**Success Criteria**:
- [ ] Documentation is clear and complete
- [ ] Examples compile and run
- [ ] Migration path is well-defined
- [ ] Common patterns are documented

**Estimated Effort**: 4-5 hours

---

## Testing Strategy

### Integration Testing

```csharp
public class SystemIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task HandlerSystem_ExecutesHandlerSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var account = new Account
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId,
            Email = "test@example.com",
            IsActive = false
        };
        await TestDataContext().Commit(account);
        
        var system = new HandlerSystem(TestDataContext(), NullLogger<HandlerSystem>.Instance);
        
        // Act
        var result = await system.ExecuteHandler<AccountHandler, Account>(
            entityId,
            handler => handler.Activate());
        
        // Assert
        result.IsActive.ShouldBeTrue();
        
        // Verify persisted
        var updated = await TestDataContext().Get<Account>(entityId);
        updated.IsActive.ShouldBeTrue();
    }
    
    [Fact]
    public async Task TransactionalSystem_RollsBackOnFailure()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var system = new TransactionalSystem(TestDataContext(), NullLogger<TransactionalSystem>.Instance);
        
        // Act & Assert
        await Should.ThrowAsync<BusinessRuleException>(async () =>
        {
            await system.ExecuteHandler<WorkOrderHandler>(entityId,
                handler => handler.CompleteWorkOrder()); // Will fail - no work order exists
        });
        
        // Verify nothing was persisted
        var workOrders = await TestDataContext().Query()
            .WithAll<WorkOrderComponent>()
            .ToList();
        workOrders.ShouldBeEmpty();
    }
}
```

### Testing Business Logic Without Azure Functions

```csharp
public class BusinessLogicTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateWorkOrderWithInvoice_CreatesAndLinks()
    {
        // Arrange
        var system = new TransactionalSystem(TestDataContext(), NullLogger<TransactionalSystem>.Instance);
        var workOrderComponent = new WorkOrderComponent
        {
            Description = "Test Work Order",
            Amount = 1000
        };
        var invoiceComponent = new InvoiceComponent
        {
            Amount = 1000,
            DueDate = DateTime.UtcNow.AddDays(30)
        };
        
        // Act - Test business logic directly without HTTP
        var (workOrderId, invoiceId) = await system.Execute(async dataContext =>
        {
            var woId = await dataContext
                .For<WorkOrderHandler>(Guid.NewGuid())
                .Create(workOrderComponent);
            
            invoiceComponent = invoiceComponent with { WorkOrderId = woId };
            
            var invId = await dataContext
                .For<InvoiceHandler>(Guid.NewGuid())
                .Create(invoiceComponent);
            
            await dataContext.LinkRelationship(woId, invId, "WorkOrder", "Invoice");
            
            return (woId, invId);
        });
        
        // Assert
        workOrderId.ShouldNotBe(Guid.Empty);
        invoiceId.ShouldNotBe(Guid.Empty);
        
        var children = await TestDataContext().Children(workOrderId);
        children.ShouldContain(invoiceId);
    }
}
```

## Key Benefits of the Revised Approach

### 1. Forces Proper Separation of Concerns

The `ExecuteHandler` pattern **prevents developers from putting logic in Functions**:
- Functions can ONLY parse JSON and call handler methods
- No access to DataContext means no inline business logic
- All validation, rules, and operations MUST be in handlers

```csharp
// BAD - Not possible with new System interface
await _system.Execute(async dataContext => {
    // Can't do this anymore - no DataContext access!
    var existing = await dataContext.Get<WorkOrder>(id);
    if (existing.Status == "Completed") { ... }
});

// GOOD - Forced to use handlers
await _system.ExecuteHandler<WorkOrderHandler>(id, 
    handler => handler.CompleteWorkOrder());
```

### 2. Enhanced Testability

```csharp
// Integration test handler without any HTTP context
public class WorkOrderHandlerTests : IntegrationTestBase
{
    [Fact]
    public async Task CompleteWorkOrder_ValidatesUnassignedStatus()
    {
        // Arrange
        var workOrderId = Guid.NewGuid();
        var workOrder = new WorkOrderComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = workOrderId,
            Status = "Open",
            AssignedTo = null // Unassigned
        };
        await TestDataContext().Commit(workOrder);
        
        // Act & Assert
        var handler = TestDataContext().For<WorkOrderHandler>(workOrderId);
        await Should.ThrowAsync<BusinessRuleException>(() => 
            handler.CompleteWorkOrder());
    }
}

// Test System orchestration with real components
public class SystemTests : IntegrationTestBase
{
    [Fact]
    public async Task System_ExecuteHandler_CompletesWorkOrder()
    {
        // Arrange
        var workOrderId = Guid.NewGuid();
        var workOrder = new WorkOrderComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = workOrderId,
            Status = "Assigned",
            AssignedTo = "tech-123"
        };
        await TestDataContext().Commit(workOrder);
        
        var system = new HandlerSystem(TestDataContext(), NullLogger<HandlerSystem>.Instance);
        
        // Act
        await system.ExecuteHandler<WorkOrderHandler>(workOrderId, 
            h => h.CompleteWorkOrder());
        
        // Assert
        var updated = await TestDataContext().Get<WorkOrderComponent>(workOrderId);
        updated.Status.ShouldBe("Completed");
        updated.CompletedAt.ShouldNotBeNull();
    }
}
```

### 3. Clear Architectural Boundaries

- **Azure Functions**: HTTP mapping ONLY
- **System Layer**: Handler orchestration and infrastructure
- **Handlers**: ALL business logic and domain operations
- **Components**: Pure data

### 4. Prevents Common Anti-Patterns

- No business logic in Functions
- No direct DataContext manipulation in Functions
- No inline validation or rules
- No scattered transaction management

### 5. Consistency Across the Codebase

Every operation follows the same pattern:
1. Function parses request
2. Function calls System.ExecuteHandler
3. Handler performs domain operation
4. Function returns response

No exceptions, no shortcuts.

## Migration Strategy

### Phase 1: Add System Layer
- Implement core System interfaces and classes
- No changes to existing code
- Add to DI container

### Phase 2: New Features Use System
- All new Azure Functions use System
- Demonstrate value with new features
- Build team familiarity

### Phase 3: Gradual Migration
- Refactor existing Functions one at a time
- Maintain backwards compatibility
- Track migration progress

### Phase 4: Remove Legacy Patterns
- Remove direct handler usage from Functions
- Standardize on System pattern
- Update documentation

## Success Metrics

1. **Code Quality**
   - Reduced complexity in Azure Functions
   - Clear separation of concerns
   - Improved testability

2. **Developer Experience**
   - Easier to test business logic
   - Consistent patterns
   - Less boilerplate

3. **System Reliability**
   - Better error handling
   - Consistent logging
   - Transaction safety

## Total Estimated Effort

- Development: 25-35 hours
- Testing: 8-10 hours
- Documentation: 4-5 hours
- **Total: 37-50 hours**

## Conclusion

The System layer provides a clean, minimal abstraction that:
- Separates HTTP concerns from handler orchestration
- Enables testing without Azure Functions runtime
- Maintains the component-only philosophy
- Keeps handlers in control of all domain logic

This approach solves the middleware problem without introducing complex patterns or request/response objects, staying true to the ECS architecture principles.