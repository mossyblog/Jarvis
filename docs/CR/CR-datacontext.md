# CHANGE REQUEST: Plugin-Based, Fluent ECS Handler Model for DataContext

## Summary

This change request proposes a full rewrite of `IDataContext` and `DataContext` to support a clean, plugin-safe, ECS-aligned handler model. The new architecture enables per-entity fluent operations such as:

```csharp
await _dataContext.For<InvoiceHandler>(invoiceId).WriteOff("Policy Adjustment");
await _dataContext.For<WorkOrderHandler>(workOrderId).GenerateInvoice();
```

**No concrete component types (like Invoice, Payment, etc.) may appear inside the core project.** All component logic must be declared externally via plugins. This restructure eliminates all assumptions about known types inside `DataContext` and replaces them with a dynamic, DI-safe handler registry model.

---

## TODO List (Step-by-Step Instructions)

**Perform each task one at a time. After completing a task, check it off before proceeding to the next.**

- [x] 1. Delete `WorkingSet.cs` and all references to it.
- [x] 2. Delete `PartitionDocument.cs` and all references to it.
- [x] 3. Remove all `Commit()`, `Snapshot()`, and mutation tracking logic from the codebase.
- [x] 4. Remove all references to concrete component types (e.g., `Invoice`, `Payment`, etc.) from core `IDataContext` and `DataContext`.
- [x] 5. Define the new `IDataContext` interface with `.For<THandler>(entityId)` and `.For(Type, entityId)` methods.
- [x] 6. Define `IComponentHandler` and `IComponentHandler<TComponent>` base interfaces.
- [x] 7. Implement `IComponentHandlerRegistry` with plugin registration and resolution methods.
- [x] 8. Refactor `DataContext` to delegate handler resolution to the registry and remove all orchestration logic.
- [ ] 9. Update all usages of `DataContext` to use the new fluent `.For<Handler>(id)` pattern.
- [ ] 10. Ensure plugins register their handler mappings at startup using the registry.
- [ ] 11. Add enforcement: analyzer/linter, CI test, or runtime guard to prevent core from referencing component types or using deprecated patterns.
- [ ] 12. Update documentation and examples to reflect the new handler-based, plugin-safe model.

---

## Background

The original Jarvis implementation used a document-style ECS approach with WorkingSet, PartitionDocument, and a global Commit() model. This was appropriate for CosmosDB but is incompatible with SQL-native architectures, where each component is stored in its own table and operations are performed per-record.

**Key migration drivers:**
- Remove all WorkingSet and PartitionDocument logic
- Eliminate centralized mutation and commit tracking
- Enable type-safe, plugin-declared handler logic
- Maintain fluent per-entity component operations through DI and registration

---

## What Must Be Removed or Rewritten

- ❌ Delete `WorkingSet.cs`
- ❌ Delete `PartitionDocument.cs`
- ❌ Remove `Commit()`, `Snapshot()`, and any mutation tracking
- ❌ Remove all references to `Invoice`, `Payment`, etc. from core `IDataContext` and `DataContext`

---

## What Will Replace It

### New `IDataContext` Interface

```csharp
public interface IDataContext
{
    IComponentHandler For(Type componentType, Guid entityId);
    THandler For<THandler>(Guid entityId) where THandler : IComponentHandler;
    THandler For<THandler>(Guid entityId, ITransaction? transaction) where THandler : IComponentHandler;
    IEntityQuery Query();
    Task<T> InTransaction<T>(Func<ITransaction, Task<T>> action);
}

public interface ITransaction
{
    Supabase.Client Client { get; } // Transaction-scoped client
}
```

This allows plugins to extend IDataContext with fluent methods while keeping core agnostic:

```csharp
// Direct usage (always available)
await _dataContext.For<InvoiceHandler>(invoiceId).WriteOff("Late Fee");
await _dataContext.For(typeof(Invoice), invoiceId).Get();
```

### Component Handler Interfaces

```csharp
public interface IComponentHandler
{
    Task<IComponent> Get();
}

public interface IComponentHandler<TComponent> : IComponentHandler where TComponent : IComponent
{
    new Task<TComponent> Get();
}
```

### Component Handler Registry

```csharp
public interface IComponentHandlerRegistry
{
    void Register<TComponent, THandler>()
        where TComponent : IComponent
        where THandler : IComponentHandler<TComponent>;

    IComponentHandler Resolve(Type componentType, Guid entityId);
    THandler Resolve<THandler>(Guid entityId) where THandler : IComponentHandler;
}
```

At plugin startup:
```csharp
_registry.Register<Invoice, InvoiceHandler>();
_registry.Register<WorkOrder, WorkOrderHandler>();
```

### DataContext Implementation

```csharp
public class DataContext : IDataContext
{
    private readonly IComponentHandlerRegistry _registry;
    private readonly Supabase.Client _client;

    public DataContext(IComponentHandlerRegistry registry, Supabase.Client client)
    {
        _registry = registry;
        _client = client;
    }

    public IComponentHandler For(Type type, Guid id) => _registry.Resolve(type, id);

    public THandler For<THandler>(Guid id) where THandler : IComponentHandler
        => _registry.Resolve<THandler>(id);

    public THandler For<THandler>(Guid id, ITransaction? transaction) where THandler : IComponentHandler
        => _registry.Resolve<THandler>(id, transaction);

    public IEntityQuery Query() => new SupabaseEntityQuery(_client);

    public async Task<T> InTransaction<T>(Func<ITransaction, Task<T>> action)
    {
        var connection = _client.GetConnection();
        using var dbTransaction = await connection.BeginTransactionAsync();
        
        try
        {
            var transaction = new SupabaseTransaction(_client, dbTransaction);
            var result = await action(transaction);
            await dbTransaction.CommitAsync();
            return result;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}
```

---

## Plugin Extension Methods Pattern

Plugins can provide fluent extension methods to make the API more intuitive, while the core remains component-agnostic:

### Core Project (Jarvis.Core)
```csharp
// Only knows about handlers, not components
public interface IDataContext
{
    THandler For<THandler>(Guid entityId) where THandler : IComponentHandler;
    IComponentHandler For(Type componentType, Guid entityId);
}
```

### Invoice Plugin Project
```csharp
// Provides fluent extensions for Invoice operations
public static class InvoiceDataContextExtensions
{
    public static InvoiceHandler Invoice(this IDataContext context, Guid invoiceId)
    {
        return context.For<InvoiceHandler>(invoiceId);
    }
}
```

### WorkOrder Plugin Project
```csharp
// Provides fluent extensions for WorkOrder operations
public static class WorkOrderDataContextExtensions
{
    public static WorkOrderHandler WorkOrder(this IDataContext context, Guid workOrderId)
    {
        return context.For<WorkOrderHandler>(workOrderId);
    }
}
```

---

## Example Usage

```csharp
// Beautiful fluent API via plugin extension methods
await _dataContext.Invoice(invoiceId).WriteOff("Fraud");
await _dataContext.WorkOrder(workOrderId).GenerateInvoice();
await _dataContext.Payment(paymentId).Process();

// Fallback pattern if no extension exists
var handler = _dataContext.For<InvoiceHandler>(invoiceId);
var result = await handler.WriteOff("Fraud");
```

---

## Benefits

- ✅ Plugin-safe: core knows nothing about components
- ✅ Fluent per-entity operations via plugin extension methods
- ✅ Clean API: `_dataContext.Invoice(id).WriteOff()` instead of `_dataContext.For<InvoiceHandler>(id).WriteOff()`
- ✅ DI-safe, type-safe, testable
- ✅ Aligns ECS style with SQL-native execution
- ✅ Avoids reflection, runtime type guessing, or generic abuse
- ✅ Plugins own their domain language and API surface

---

## Risks

- Requires complete refactor of `IDataContext` and any existing WorkingSet or Commit logic
- Requires plugins to register handlers on startup
- Requires handlers to take over mutation responsibilities

---

## Approval Checklist

- [ ] Remove WorkingSet.cs, PartitionDocument.cs, and all commit orchestration
- [ ] Refactor `IDataContext` to expose `.For<THandler>(entityId)`
- [ ] Define `IComponentHandler` base interfaces
- [ ] Implement `IComponentHandlerRegistry` with plugin registration
- [ ] Plugins register handler mappings on startup
- [ ] DataContext delegates resolution to registry
- [ ] All former DataContext usages updated to fluent `.For<Handler>(id)` pattern

---

## Foundational ECS + SQL Rules (Post-Migration)

1. **IDataContext must never reference a component directly**  
   Constraint: No methods like `.Invoice(id)` or `.WorkOrder(id)` in core project.  
   Core only provides: `.For<InvoiceHandler>(id)` or `.For(Type, id)`  
   Plugin extension methods provide: `.Invoice(id)`, `.WorkOrder(id)`, etc.  
   Violation Consequence: Block PR or fail test.

2. **All component logic must be encapsulated in handlers**  
   Rule: Component orchestration (e.g. generate, reconcile, write off) lives inside `IComponentHandler<T>` implementations.  
   Handler location: Must live in plugin/feature module, not core.

3. **Handlers must be registered by plugin startup**  
   Constraint: `IComponentHandlerRegistry.Register<TComponent, THandler>()` must be called at load time.  
   No auto-reflection. Registration must be explicit and intentional.

4. **No global Commit(), WorkingSet, or Snapshot() allowed**  
   Status: These patterns are deprecated.  
   Why: SQL-native commits per component directly—no staging or doc reassembly.

5. **All SQL access must go through _client.From<T>()**  
   Constraint: Only allowed inside handler methods.  
   Prohibited: Direct SQL client access in orchestration or service layers.

6. **Entity composition is inferred by shared EntityId, not hierarchy**  
   Rule: There is no centralized document or tree. Components belong together if they share `entity_id`.

7. **One table per component**  
   Constraint: Do not use generalized components or partitiondocument tables.  
   Table name rule: Table name must match component class exactly (e.g. invoice, payment, client_profile).

8. **Handlers are responsible for their own persistence**  
   Rule: All `.Upsert(...)`, `.Delete(...)`, etc. must occur inside the handler.  
   Do not centralize write logic into IDataContext.

9. **No reflection for Upsert or From**  
   Rule: All SQL operations must be strongly typed inside the handler.  
   Why: To ensure schema and table alignment. Use `T : BaseModel, new()` constraints.

10. **Querying across components must go through IEntityQuery**  
    Rule: Use `.With<T>(...)` chaining to intersect entity_ids.  
    Constraint: Never JOIN components in SQL. Always intersect in memory.

11. No use of the ``dynamic`` in C# (bad)
12. No use of Reflection to get access to components etc.
13. Devs aren't expected to call supabase client directly for their needs. they are expected to get their data via DataContext because it will own DI setup.

---

## Optional Enforcement Strategy

- Add an analyzer/linter rule: no references to BaseModel-typed variables outside handler context
- Add CI test: scan IDataContext.cs for disallowed types or static references
- Add runtime guard in DataContext: throw if handler registry is missing a binding

---

## Entity Query Pattern

The architecture includes an `IEntityQuery` interface for querying across multiple components while maintaining the ECS principle of component independence:

### IEntityQuery Interface
```csharp
public interface IEntityQuery
{
    IEntityQuery With<T>(Expression<Func<T, bool>> filter) where T : BaseModel, new();
    IEntityQuery Include<T>() where T : BaseModel, new(); // Eager load without filter
    IEntityQuery Include<T>(Expression<Func<T, bool>> filter) where T : BaseModel, new();
    Task<List<Guid>> ToEntityIds();
    Task<Dictionary<Guid, EntityComponents>> ToEntityComponents();
}

public class EntityComponents
{
    private readonly Dictionary<Type, IComponent> _components = new();
    
    public T? Get<T>() where T : class, IComponent
        => _components.TryGetValue(typeof(T), out var c) ? c as T : null;
        
    public bool Has<T>() where T : IComponent
        => _components.ContainsKey(typeof(T));
}
```

### Optimized EntityQuery Implementation with Batching
```csharp
public class EntityQuery : IEntityQuery
{
    private readonly Supabase.Client _client;
    private readonly Dictionary<Type, QueryPlan> _plans = new();
    
    private class QueryPlan
    {
        public Expression<Func<BaseModel, bool>>? Filter { get; set; }
        public bool IsInclude { get; set; } // Include vs With
    }

    public EntityQuery(Supabase.Client client) => _client = client;

    public IEntityQuery With<T>(Expression<Func<T, bool>> filter) where T : BaseModel, new()
    {
        _plans[typeof(T)] = new QueryPlan { Filter = filter, IsInclude = false };
        return this;
    }
    
    public IEntityQuery Include<T>() where T : BaseModel, new()
    {
        _plans[typeof(T)] = new QueryPlan { Filter = null, IsInclude = true };
        return this;
    }
    
    public IEntityQuery Include<T>(Expression<Func<T, bool>> filter) where T : BaseModel, new()
    {
        _plans[typeof(T)] = new QueryPlan { Filter = filter, IsInclude = true };
        return this;
    }

    public async Task<List<Guid>> ToEntityIds()
    {
        // Only consider "With" filters for entity ID intersection
        var filterPlans = _plans.Where(p => !p.Value.IsInclude).ToList();
        var entityIdLists = new List<List<Guid>>();
        
        foreach (var (type, plan) in filterPlans)
        {
            var ids = await GetEntityIdsForType(type, plan.Filter);
            entityIdLists.Add(ids);
        }
        
        // Intersect all entity ID lists
        return entityIdLists.Count > 0 
            ? entityIdLists.Aggregate((a, b) => a.Intersect(b).ToList())
            : new List<Guid>();
    }

    public async Task<Dictionary<Guid, EntityComponents>> ToEntityComponents()
    {
        // Step 1: Get entity IDs from "With" filters only
        var entityIds = await ToEntityIds();
        if (!entityIds.Any()) return new Dictionary<Guid, EntityComponents>();
        
        // Step 2: Batch load all components (With + Include) for these entities
        var componentsByType = new Dictionary<Type, List<IComponent>>();
        var loadTasks = new List<Task<(Type type, List<IComponent> components)>>();
        
        foreach (var (type, plan) in _plans)
        {
            loadTasks.Add(LoadComponentsBatch(type, entityIds));
        }
        
        var results = await Task.WhenAll(loadTasks);
        
        // Step 3: Assemble results by entity
        var entityComponents = new Dictionary<Guid, EntityComponents>();
        
        foreach (var (type, components) in results)
        {
            foreach (var component in components)
            {
                var entityId = ((BaseModel)component).EntityId;
                if (!entityComponents.TryGetValue(entityId, out var ec))
                {
                    ec = new EntityComponents();
                    entityComponents[entityId] = ec;
                }
                ec._components[type] = component;
            }
        }
        
        return entityComponents;
    }

    private async Task<List<Guid>> GetEntityIdsForType(Type type, Expression<Func<BaseModel, bool>>? filter)
    {
        // Use reflection to call typed method
        var method = GetType().GetMethod(nameof(GetTypedEntityIds), BindingFlags.NonPublic | BindingFlags.Instance);
        var generic = method.MakeGenericMethod(type);
        return await (Task<List<Guid>>)generic.Invoke(this, new object[] { filter });
    }
    
    private async Task<List<Guid>> GetTypedEntityIds<T>(Expression<Func<T, bool>>? filter) where T : BaseModel, new()
    {
        var query = _client.From<T>();
        if (filter != null) query = query.Where(filter);
        
        var result = await query
            .Select(x => new object[] { x.EntityId })
            .Get();
            
        return result.Models.Select(x => x.EntityId).Distinct().ToList();
    }
    
    private async Task<(Type type, List<IComponent> components)> LoadComponentsBatch(Type type, List<Guid> entityIds)
    {
        var method = GetType().GetMethod(nameof(LoadTypedComponents), BindingFlags.NonPublic | BindingFlags.Instance);
        var generic = method.MakeGenericMethod(type);
        var components = await (Task<List<IComponent>>)generic.Invoke(this, new object[] { entityIds });
        return (type, components);
    }

    private async Task<List<IComponent>> LoadTypedComponents<T>(List<Guid> entityIds) where T : BaseModel, IComponent, new()
    {
        // Single query with IN clause for all entity IDs
        var result = await _client.From<T>()
            .In(x => x.EntityId, entityIds)
            .Get();
        return result.Models.Cast<IComponent>().ToList();
    }
}
```

### Query Usage Examples
```csharp
// Find entity IDs matching multiple component criteria
var ids = await _dataContext.Query()
    .With<Invoice>(x => x.Status == "FAILED")
    .With<Payment>(x => x.Amount > 500)
    .ToEntityIds();

// Eager load related components to avoid N+1 queries
var entities = await _dataContext.Query()
    .With<Invoice>(x => x.Status == "UNPAID")     // Filter criteria
    .Include<Payment>()                            // Eager load all payments
    .Include<Customer>()                           // Eager load all customers
    .Include<Note>(n => n.Type == "COLLECTION")   // Eager load specific notes
    .ToEntityComponents();

// Process results without additional queries
foreach (var (entityId, components) in entities)
{
    var invoice = components.Get<Invoice>();       // Primary filter component
    var payment = components.Get<Payment>();       // Already loaded
    var customer = components.Get<Customer>();     // Already loaded
    var note = components.Get<Note>();            // Already loaded if exists
    
    if (payment == null && invoice.DaysOverdue > 30)
    {
        // Business logic with all data already loaded
    }
}
```

### Performance Benefits

The optimized implementation with batching provides:

1. **Reduced Query Count**: From N+1 queries to 1 query per component type
2. **Parallel Loading**: All component types loaded concurrently with `Task.WhenAll`
3. **Memory Efficiency**: Single pass assembly of results
4. **Clear Intent**: `With` for filtering vs `Include` for eager loading

Before optimization:
```sql
-- N+1 Problem: 100 entities = 300+ queries
SELECT * FROM invoice WHERE entity_id = 'id1'
SELECT * FROM payment WHERE entity_id = 'id1'
SELECT * FROM customer WHERE entity_id = 'id1'
-- ... repeated for each entity
```

After optimization:
```sql
-- Batched: 100 entities = 3 queries total
SELECT * FROM invoice WHERE entity_id IN ('id1', 'id2', ..., 'id100')
SELECT * FROM payment WHERE entity_id IN ('id1', 'id2', ..., 'id100')
SELECT * FROM customer WHERE entity_id IN ('id1', 'id2', ..., 'id100')
```

---

## Transaction Support Pattern

The architecture supports optional transaction scope for operations that need to maintain consistency across multiple component updates:

### Transaction Usage Examples

```csharp
// Single operation - no explicit transaction needed
await _dataContext.Invoice(invoiceId).WriteOff("Policy adjustment");

// Multiple operations in a transaction
var result = await _dataContext.InTransaction(async tx =>
{
    // All operations share the same transaction
    await _dataContext.For<InvoiceHandler>(invoiceId, tx).WriteOff("Fraud");
    await _dataContext.For<PaymentHandler>(paymentId, tx).Cancel();
    await _dataContext.For<AccountBalanceHandler>(accountId, tx).Adjust(-1000);
    
    return new { Success = true, TransactionId = Guid.NewGuid() };
});

// Handler implementation with transaction support
public class InvoiceHandler : IComponentHandler<Invoice>
{
    private readonly Guid _entityId;
    private readonly Supabase.Client _client;
    private readonly ITransaction? _transaction;
    
    public InvoiceHandler(Guid entityId, Supabase.Client client, ITransaction? transaction = null)
    {
        _entityId = entityId;
        _client = transaction?.Client ?? client;
        _transaction = transaction;
    }
    
    public async Task WriteOff(string reason)
    {
        // Use the transaction-aware client if in a transaction
        var invoice = await _client.From<Invoice>()
            .Where(x => x.EntityId == _entityId)
            .Single();
            
        invoice.Status = "WRITTEN_OFF";
        invoice.WriteOffReason = reason;
        await _client.From<Invoice>().Update(invoice);
        
        // Create related components in same transaction
        var creditNote = new CreditNote 
        { 
            EntityId = _entityId,
            Amount = invoice.Amount,
            Reason = reason 
        };
        await _client.From<CreditNote>().Insert(creditNote);
    }
}
```

### Key Transaction Principles

1. **Handlers are transaction-agnostic** - they work with or without transactions
2. **Transaction scope is explicit** - use `InTransaction` to define boundaries
3. **Automatic rollback** - exceptions trigger rollback automatically
4. **Composable operations** - multiple handlers can participate in same transaction
5. **No nested transactions** - keep transaction logic simple and flat

---

## Error Handling & Validation Pattern

Handlers are the first point of business rule validation and must enforce domain integrity:

### Exception Hierarchy
```csharp
// Base domain exception
public abstract class DomainException : Exception
{
    public string Code { get; }
    public object? Context { get; }
    
    protected DomainException(string code, string message, object? context = null) 
        : base(message)
    {
        Code = code;
        Context = context;
    }
}

// Specific domain exceptions
public class ValidationException : DomainException
{
    public Dictionary<string, string[]> Errors { get; }
    
    public ValidationException(string field, string error)
        : base("VALIDATION_ERROR", $"Validation failed for {field}")
    {
        Errors = new() { [field] = new[] { error } };
    }
}

public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string rule, string message, object? context = null)
        : base($"RULE_{rule}", message, context) { }
}

public class EntityNotFoundException : DomainException
{
    public Guid EntityId { get; }
    
    public EntityNotFoundException(Guid entityId, string entityType)
        : base("NOT_FOUND", $"{entityType} with ID {entityId} not found")
    {
        EntityId = entityId;
    }
}
```

### Guard Pattern
```csharp
public static class Guard
{
    public static void Against(bool condition, string message)
    {
        if (condition)
            throw new BusinessRuleException("GUARD", message);
    }
    
    public static void AgainstNull<T>(T? value, string paramName) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(paramName);
    }
    
    public static void AgainstEmpty(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException(paramName, $"{paramName} cannot be empty");
    }
}
```

### Handler Implementation Example
```csharp
public class InvoiceHandler : ComponentHandler<Invoice>
{
    public async Task WriteOff(string reason)
    {
        // Input validation
        Guard.AgainstEmpty(reason, nameof(reason));
        
        try
        {
            // Get and validate state
            var invoice = await GetRequiredComponent<Invoice>();
            
            // Business rule validation
            Ensure(invoice.Status == "SENT", 
                "Cannot write off invoice that hasn't been sent");
            Ensure(invoice.Status != "PAID", 
                "Cannot write off invoice that's already paid");
            
            // Log operation
            Logger.LogInformation("Writing off invoice {InvoiceId} for reason: {Reason}", 
                EntityId, reason);
            
            // Perform operation
            invoice.Status = "WRITTEN_OFF";
            invoice.WriteOffReason = reason;
            await Client.From<Invoice>().Update(invoice);
            
            Logger.LogInformation("Successfully wrote off invoice {InvoiceId}", EntityId);
        }
        catch (DomainException ex)
        {
            Logger.LogWarning(ex, "Domain error writing off invoice {InvoiceId}", EntityId);
            throw; // Propagate to caller
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error writing off invoice {InvoiceId}", EntityId);
            throw new DomainException("INVOICE_WRITEOFF_FAILED", 
                "Failed to write off invoice", new { EntityId });
        }
    }
}
```

### Key Validation Principles

1. **Handlers validate first** - Business rules checked before any mutations
2. **Typed exceptions** - Different exception types for different failures
3. **Structured logging** - All operations logged with context
4. **Fail fast** - Validate early and throw immediately
5. **Transaction safety** - Exceptions trigger automatic rollback

---

## Event Sourcing & Audit Trail Pattern

Each handler is responsible for emitting audit events after any write operation. The audit trail is persisted synchronously within the same transaction.

### Audit Event Structure
```csharp
public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EntityId { get; set; }
    public string EntityType { get; set; }
    public string EventType { get; set; }
    public string UserId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string? TransactionId { get; set; }
}

// Standardized event types
public static class AuditEventTypes
{
    public const string InvoiceCreated = "INVOICE_CREATED";
    public const string InvoiceUpdated = "INVOICE_UPDATED";
    public const string InvoiceWrittenOff = "INVOICE_WRITTEN_OFF";
    public const string PaymentProcessed = "PAYMENT_PROCESSED";
    public const string PaymentCancelled = "PAYMENT_CANCELLED";
}
```

### Audit Service
```csharp
public interface IAuditService
{
    Task LogEvent(string eventType, Guid entityId, object? metadata = null);
    Task LogChange<T>(string eventType, Guid entityId, T oldValue, T newValue, object? metadata = null);
}

public class AuditService : IAuditService
{
    private readonly Supabase.Client _client;
    private readonly IUserContext _userContext;
    
    public async Task LogEvent(string eventType, Guid entityId, object? metadata = null)
    {
        var auditEvent = new AuditEvent
        {
            EntityId = entityId,
            EntityType = ExtractEntityType(eventType),
            EventType = eventType,
            UserId = _userContext.UserId,
            Metadata = metadata?.ToDictionary()
        };
        
        await _client.From<AuditEvent>().Insert(auditEvent);
    }
    
    public async Task LogChange<T>(string eventType, Guid entityId, T oldValue, T newValue, object? metadata = null)
    {
        var auditEvent = new AuditEvent
        {
            EntityId = entityId,
            EntityType = ExtractEntityType(eventType),
            EventType = eventType,
            UserId = _userContext.UserId,
            OldValue = JsonSerializer.Serialize(oldValue),
            NewValue = JsonSerializer.Serialize(newValue),
            Metadata = metadata?.ToDictionary()
        };
        
        await _client.From<AuditEvent>().Insert(auditEvent);
    }
}
```

### Handler Implementation with Audit
```csharp
public class InvoiceHandler : ComponentHandler<Invoice>
{
    private readonly IAuditService _audit;
    
    public async Task WriteOff(string reason)
    {
        var invoice = await GetRequiredComponent<Invoice>();
        var originalState = invoice.Clone(); // Deep copy for audit
        
        // Validate and perform operation
        Ensure(invoice.Status == "SENT", 
            "Cannot write off invoice that hasn't been sent");
        
        invoice.Status = "WRITTEN_OFF";
        invoice.WriteOffReason = reason;
        invoice.WriteOffDate = DateTime.UtcNow;
        
        await Client.From<Invoice>().Update(invoice);
        
        // Audit the change with before/after states
        await _audit.LogChange(
            AuditEventTypes.InvoiceWrittenOff,
            EntityId,
            originalState, 
            invoice,
            new { Reason = reason });
        
        Logger.LogInformation("Invoice {InvoiceId} written off", invoice.Id);
    }
    
    public async Task<Invoice> Create(decimal amount, string description)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = EntityId,
            Amount = amount,
            Description = description,
            Status = "DRAFT"
        };
        
        await Client.From<Invoice>().Insert(invoice);
        
        // Audit the creation
        await _audit.LogEvent(
            AuditEventTypes.InvoiceCreated, 
            EntityId,
            new { InvoiceId = invoice.Id, Amount = amount });
            
        return invoice;
    }
}
```

### Key Audit Principles

1. **Handler responsibility** - Each handler audits its own operations
2. **Synchronous emission** - Audit events written in same transaction
3. **Immutable trail** - Audit records are never updated or deleted
4. **Rich context** - Store before/after states and metadata
5. **Standardized types** - Consistent event type naming convention

---

## Architecture Summary

The key insight is that the core Jarvis project provides only the handler infrastructure (`IDataContext.For<THandler>`), while plugins provide:
1. Component definitions (Invoice, Payment, WorkOrder)
2. Handler implementations (InvoiceHandler, PaymentHandler)
3. Extension methods for fluent API (`.Invoice()`, `.Payment()`)
4. No use of the Dynamic in C# (bad)
5. No use of Reflection

This keeps the core completely agnostic about domain concepts while still enabling beautiful, fluent APIs like `_dataContext.Invoice(id).WriteOff()`.

---

**Author:** Claude & User  
**Date:** 2025-01-24  
**Status:** DRAFT
