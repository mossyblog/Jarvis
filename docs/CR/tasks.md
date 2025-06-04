# Implementation Tasks for Plugin-Based Handler Architecture

## Overview
This document outlines the step-by-step tasks to refactor Jarvis from a document-based (WorkingSet/PartitionDocument) architecture to a plugin-based handler model. The core project will know nothing about domain components (Invoice, Payment, etc.) - those will be implemented later as plugins.

## Phase 1: Remove Legacy Architecture ✅

### 1.1 Remove WorkingSet and PartitionDocument
- [x] Delete `/core.jarvis/Data/Storage/WorkingSet.cs`
- [x] Delete `/core.jarvis/Data/Storage/IWorkingSet.cs`
- [x] Delete `/core.jarvis/Data/Storage/PartitionDocument.cs`
- [x] Remove all imports and usages of these classes
- [x] Update any tests that depend on these classes

### 1.2 Remove Commit/Snapshot Logic
- [x] Remove `Commit()` method from `IDataContext`
- [x] Remove `Commit()` implementation from `DataContext`
- [x] Remove `Snapshot()` methods and related logic
- [x] Remove mutation tracking code
- [x] Remove dirty state management
- [x] Update all tests to not use Commit()

### 1.3 Remove Entity Creation Methods
- [x] Remove `Entity()` method from `IDataContext`
- [x] Remove `Entity()` implementation from `DataContext`
- [x] Remove direct entity manipulation methods
- [x] Update EntityContext to not depend on WorkingSet

### 1.4 Remove Component References from Core
- [x] Remove all references to concrete components (Invoice, Payment, etc.) from core project
- [x] Ensure no test components remain in production code
- [x] Move any test components to test project only

## Phase 2: Build Core Handler Infrastructure ✅

### 2.1 Define Handler Interfaces
- [x] Create `IComponentHandler` base interface
  ```csharp
  public interface IComponentHandler
  {
      Task<IComponent> Get();
  }
  ```
- [x] Create `IComponentHandler<TComponent>` generic interface
- [x] Define handler lifecycle methods

### 2.2 Create Handler Registry
- [x] Create `IComponentHandlerRegistry` interface
  ```csharp
  public interface IComponentHandlerRegistry
  {
      void Register<TComponent, THandler>();
      IComponentHandler Resolve(Type componentType, Guid entityId);
      THandler Resolve<THandler>(Guid entityId);
  }
  ```
- [x] Implement `ComponentHandlerRegistry` class
- [x] Add DI registration for registry

### 2.3 Update IDataContext Interface
- [x] Add `For<THandler>(Guid entityId)` method
- [x] Add `For<THandler>(Guid entityId, ITransaction? transaction)` overload
- [x] Add `For(Type componentType, Guid entityId)` method
- [x] Add `Query()` method returning `IEntityQuery`
- [x] Add `InTransaction<T>(Func<ITransaction, Task<T>> action)` method

### 2.4 Implement New DataContext
- [x] Update DataContext to use IComponentHandlerRegistry
- [x] Implement all new IDataContext methods
- [x] Remove all orchestration logic
- [x] DataContext becomes a thin delegation layer

## Phase 3: Implement Query Infrastructure ✅

### 3.1 Create Entity Query Interfaces
- [x] Define `IEntityQuery` interface with fluent methods
- [x] Add `With<T>()` for filtering
- [x] Add `Include<T>()` for eager loading
- [x] Add `ToEntityIds()` method
- [x] Add `ToEntityComponents()` method

### 3.2 Implement EntityComponents
- [x] Create `EntityComponents` class
- [x] Add `Get<T>()` method
- [x] Add `Has<T>()` method
- [x] Implement type-safe component storage

### 3.3 Build Optimized Query Implementation
- [x] Implement batched loading with IN clauses
- [x] Add parallel loading with Task.WhenAll
- [x] Implement query plan optimization
- [x] Add Include vs With distinction

## Phase 4: Add Transaction Support ✅

### 4.1 Define Transaction Interfaces
- [x] Create `ITransaction` interface
- [x] Add transaction-scoped client property
- [x] Define transaction lifecycle

### 4.2 Implement Transaction Management
- [x] Update handler factory to accept ITransaction
- [x] Implement InTransaction method in DataContext
- [x] Add automatic commit/rollback logic
- [x] Ensure handler transaction awareness

## Phase 5: Error Handling & Validation ✅

### 5.1 Create Exception Hierarchy
- [x] Create `DomainException` base class
- [x] Add `ValidationException`
- [x] Add `BusinessRuleException`
- [x] Add `EntityNotFoundException`
- [x] Add structured error context

### 5.2 Implement Guard Pattern
- [x] Create `Guard` static class
- [x] Add common validation methods
- [x] Add fluent validation extensions
- [x] Document usage patterns

### 5.3 Create Handler Base Class
- [x] Create `ComponentHandler<T>` base class
- [x] Add validation helper methods
- [x] Add structured logging
- [x] Add error handling patterns

## Phase 6: Audit Trail Infrastructure ✅

### 6.1 Define Audit Structure
- [x] Create `AuditEvent` class
- [x] Define standardized event types
- [x] Add metadata support
- [x] Design immutable storage

### 6.2 Build Audit Service
- [x] Create `IAuditService` interface
- [x] Implement `AuditService`
- [x] Add LogEvent methods
- [x] Add LogChange methods
- [x] Integrate with user context

### 6.3 Integrate Audit into Handlers
- [x] Update handler base class to include IAuditService
- [x] Add audit helper methods
- [x] Document audit patterns
- [x] Ensure transaction participation

## Phase 7: Testing & Documentation 📚

### 7.1 Update Core Tests
- [ ] Remove all WorkingSet-based tests
- [ ] Remove all Commit-based tests
- [ ] Add handler registry tests
- [ ] Add transaction tests
- [ ] Add query optimization tests

### 7.2 Create Example Handlers
- [ ] Create example handler in test project
- [ ] Show validation patterns
- [ ] Show transaction usage
- [ ] Show audit integration

### 7.3 Update Documentation
- [ ] Update API reference docs
- [ ] Create handler development guide
- [ ] Document plugin registration
- [ ] Add migration guide from old pattern

## Phase 8: Enforcement & Quality Gates 🚦

### 8.1 Add Build-Time Checks
- [ ] Create analyzer to prevent component references in core
- [ ] Add rule to prevent "dynamic" keyword usage
- [ ] Add rule to prevent reflection for components
- [ ] Configure as errors, not warnings

### 8.2 Add Runtime Guards
- [ ] Add guard for missing handler registration
- [ ] Add transaction state validation
- [ ] Add audit trail verification
- [ ] Log helpful error messages

## Phase 9: Plugin Preparation (Not Core) 🔌 (DO NOT START UNTIL I SAY SO)

### 9.1 Create Plugin Template
- [ ] Document plugin project structure
- [ ] Show component definition pattern
- [ ] Show handler implementation pattern
- [ ] Show extension method pattern

### 9.2 Example Plugin Structure
```
MyApp.Invoicing/
├── Components/
│   ├── Invoice.cs
│   ├── CreditNote.cs
├── Handlers/
│   ├── InvoiceHandler.cs
│   ├── CreditNoteHandler.cs
├── Extensions/
│   └── InvoiceDataContextExtensions.cs
└── Registration/
    └── InvoicingPlugin.cs
```

## Success Criteria ✅

1. Core project has zero references to domain components
2. All WorkingSet/PartitionDocument code removed
3. No Commit() or mutation tracking remains
4. Handler registry fully functional
5. Transaction support working
6. Query optimization implemented
7. Audit trail integrated
8. All tests passing
9. Documentation complete

## Notes

- Each task should be completed and tested before moving to the next
- Core must remain completely agnostic to domain concepts
- Plugin examples will be created AFTER core is complete
- Focus on infrastructure, not domain implementation