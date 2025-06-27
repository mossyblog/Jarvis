# Transaction Support for Jarvis ECS Framework

## Executive Summary

This proposal outlines the implementation of transaction support in the Jarvis ECS framework, enabling atomic multi-handler operations with full PostgreSQL transaction capabilities. This feature is critical for maintaining data integrity when orchestrating complex operations across multiple handlers.

## Problem Statement

Currently, the Jarvis framework lacks transaction support, which means:
- Multiple handler operations cannot be atomically grouped
- Failed operations may leave data in inconsistent states
- Complex workflows risk partial completion
- No rollback capability for multi-step processes

## Proposed Solution

Implement an `InTransaction` method on `IDataContext` that provides:
- Full PostgreSQL transaction support (BEGIN/COMMIT/ROLLBACK)
- Transparent transaction propagation to all handlers
- Automatic rollback on exceptions
- Support for the proposed System orchestration layer

## Architecture Overview

```
┌─────────────────────┐
│   Azure Function    │
└──────────┬──────────┘
           │
┌──────────▼──────────┐
│       System        │
└──────────┬──────────┘
           │
┌──────────▼──────────┐
│    DataContext      │
│   InTransaction()   │
└──────────┬──────────┘
           │
┌──────────▼──────────┐
│  TransactionalPg    │
│      Client         │
└──────────┬──────────┘
           │
┌──────────▼──────────┐
│    PostgreSQL       │
│    Transaction      │
└─────────────────────┘
```

## Detailed Design

### Core Components

#### 1. IDataContext Extension
```csharp
public interface IDataContext
{
    // Existing methods...
    
    /// <summary>
    /// Executes an operation within a database transaction.
    /// All database operations within the operation delegate participate in the same transaction.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute within the transaction.</param>
    /// <param name="isolationLevel">Optional transaction isolation level.</param>
    /// <returns>The result of the operation.</returns>
    Task<T> InTransaction<T>(
        Func<IDataContext, Task<T>> operation, 
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    
    /// <summary>
    /// Executes an operation within a database transaction without a return value.
    /// </summary>
    Task InTransaction(
        Func<IDataContext, Task> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
}
```

#### 2. Transaction Context Management
```csharp
public interface ITransactionContext
{
    /// <summary>
    /// Gets the current transaction if one is active.
    /// </summary>
    NpgsqlTransaction? CurrentTransaction { get; }
    
    /// <summary>
    /// Indicates whether a transaction is currently active.
    /// </summary>
    bool IsInTransaction { get; }
}
```

## Implementation Tasks

### Task 1: Create Transaction Infrastructure
**Description**: Build the foundational transaction support classes and interfaces.

**Subtasks**:
1. Add `InTransaction` methods to `IDataContext` interface
2. Create `ITransactionContext` interface
3. Create `TransactionScope` class for managing transaction lifecycle
4. Update `DataContext` to implement transaction methods

**Success Criteria**:
- [ ] `IDataContext` interface includes transaction methods
- [ ] `DataContext` compiles with transaction method stubs
- [ ] Unit tests verify transaction methods are callable
- [ ] No breaking changes to existing code

**Estimated Effort**: 2-3 hours

---

### Task 2: Implement TransactionalPgClient
**Description**: Create a wrapper for `IPgClient` that propagates transaction context.

**Subtasks**:
1. Create `TransactionalPgClient` class implementing `IPgClient`
2. Override `From<T>()` to return transactional table accessors
3. Ensure JWT and connection methods delegate correctly
4. Handle transaction disposal and cleanup

**Success Criteria**:
- [ ] All `IPgClient` methods work within transaction context
- [ ] Transaction is propagated to all table operations
- [ ] Unit tests verify transaction participation
- [ ] No connection leaks in error scenarios

**Estimated Effort**: 4-5 hours

---

### Task 3: Implement TransactionalPgTable
**Description**: Create table accessor that uses transaction for all operations.

**Subtasks**:
1. Create `TransactionalPgTable<T>` extending `PgTable<T>`
2. Override Insert, Update, Delete, Select methods
3. Ensure all operations use transaction's connection
4. Maintain compatibility with RLS policies

**Success Criteria**:
- [ ] All CRUD operations participate in transaction
- [ ] RLS policies still apply within transactions
- [ ] Bulk operations work correctly
- [ ] Performance impact is minimal (<5% overhead)

**Estimated Effort**: 6-8 hours

---

### Task 4: Update DataContext InTransaction Implementation
**Description**: Implement the actual transaction orchestration logic.

**Subtasks**:
1. Implement `InTransaction<T>` method
2. Implement non-generic `InTransaction` method
3. Handle transaction lifecycle (BEGIN/COMMIT/ROLLBACK)
4. Create transactional DataContext instance for operation scope
5. Implement isolation level support

**Success Criteria**:
- [ ] Transactions commit on success
- [ ] Transactions rollback on exception
- [ ] Nested transaction calls are detected and handled
- [ ] Isolation levels are properly set
- [ ] Audit events are included in transaction

**Estimated Effort**: 4-5 hours

---

### Task 5: Add Transaction Context to Handlers
**Description**: Ensure handlers can detect and utilize transaction context.

**Subtasks**:
1. Update `ComponentHandler` base class to expose transaction state
2. Add `IsInTransaction` property to handlers
3. Ensure handlers use transactional context when available
4. Update handler initialization to accept transaction context

**Success Criteria**:
- [ ] Handlers can detect if running in transaction
- [ ] Handler operations automatically participate in active transaction
- [ ] No changes required to existing handler implementations
- [ ] Transaction context is thread-safe

**Estimated Effort**: 3-4 hours

---

### Task 6: Integration Testing
**Description**: Comprehensive testing of transaction scenarios.

**Test Scenarios**:
1. Simple transaction with single handler
2. Multi-handler transaction with success
3. Multi-handler transaction with rollback
4. Nested transaction detection
5. Concurrent transactions
6. Transaction timeout handling
7. Deadlock scenarios
8. Large transaction performance

**Success Criteria**:
- [ ] All test scenarios pass
- [ ] No data corruption in failure cases
- [ ] Performance regression < 5%
- [ ] Memory usage is stable
- [ ] Connection pool behaves correctly

**Estimated Effort**: 8-10 hours

---

### Task 7: Update System Layer Integration
**Description**: Integrate transaction support with the proposed System layer.

**Subtasks**:
1. Create `TransactionalSystem` implementation
2. Update `ISystem` to support transaction configuration
3. Add transaction middleware support
4. Document transaction usage patterns

**Success Criteria**:
- [ ] System layer can orchestrate transactional operations
- [ ] Transaction boundaries are clearly defined
- [ ] Middleware can participate in transactions
- [ ] Examples demonstrate proper usage

**Estimated Effort**: 4-5 hours

---

### Task 8: Documentation and Examples
**Description**: Create comprehensive documentation and examples.

**Deliverables**:
1. API documentation for all transaction methods
2. Transaction best practices guide
3. Example: Multi-handler workflow with transaction
4. Example: Error handling and rollback
5. Migration guide for existing code
6. Performance tuning guide

**Success Criteria**:
- [ ] All public APIs are documented
- [ ] Examples compile and run
- [ ] Common pitfalls are documented
- [ ] Performance implications are clear

**Estimated Effort**: 4-5 hours

---

## Testing Strategy

### Unit Tests
- Mock transaction behavior
- Verify method signatures and contracts
- Test error conditions
- Validate transaction state management

### Integration Tests
- Real PostgreSQL transactions
- Multi-table operations
- Concurrent transaction handling
- Performance benchmarks

### Example Test Case
```csharp
[Fact]
public async Task InTransaction_RollsBackOnException()
{
    // Arrange
    var workOrder = new WorkOrderComponent { Amount = 1000 };
    var invoice = new InvoiceComponent { Amount = -100 }; // Invalid
    
    // Act & Assert
    await Should.ThrowAsync<ValidationException>(async () =>
    {
        await TestDataContext().InTransaction(async tx =>
        {
            var workOrderId = await tx.For<WorkOrderHandler>(Guid.NewGuid())
                .Create(workOrder);
            
            // This should fail validation and rollback entire transaction
            await tx.For<InvoiceHandler>(Guid.NewGuid())
                .Create(invoice);
        });
    });
    
    // Verify work order was not created
    var workOrders = await TestDataContext().Query()
        .WithAll<WorkOrderComponent>()
        .ToList();
    workOrders.ShouldBeEmpty();
}
```

## Rollout Plan

### Phase 1: Core Implementation (Tasks 1-5)
- Implement basic transaction support
- No breaking changes to existing code
- Feature flag for opt-in usage

### Phase 2: Testing and Hardening (Task 6)
- Comprehensive integration testing
- Performance validation
- Bug fixes and optimizations

### Phase 3: System Integration (Task 7)
- Integrate with System orchestration layer
- Update middleware to support transactions
- Validate with real workflows

### Phase 4: Documentation and Adoption (Task 8)
- Complete documentation
- Team training
- Gradual adoption in existing code

## Success Metrics

1. **Functionality**
   - All transaction operations work as specified
   - No breaking changes to existing code
   - Proper error handling and rollback

2. **Performance**
   - Transaction overhead < 5%
   - No connection pool exhaustion
   - Acceptable performance under load

3. **Reliability**
   - No data corruption
   - Proper isolation between transactions
   - Graceful handling of edge cases

4. **Adoption**
   - Clear documentation
   - Easy integration path
   - Positive developer feedback

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Connection pool exhaustion | High | Implement connection pool monitoring and transaction timeouts |
| Performance degradation | Medium | Comprehensive performance testing and optimization |
| Complex error scenarios | Medium | Extensive error case testing and clear error messages |
| Breaking changes | Low | Feature flag and backwards compatibility |

## Total Estimated Effort

- Development: 35-45 hours
- Testing: 10-15 hours
- Documentation: 5-8 hours
- **Total: 50-68 hours**

## Conclusion

Transaction support is a critical feature for maintaining data integrity in complex multi-handler operations. This implementation provides a clean, testable approach that integrates seamlessly with the existing architecture while preparing for the System orchestration layer.

The phased approach ensures we can deliver value incrementally while maintaining system stability.