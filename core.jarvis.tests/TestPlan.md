# Jarvis Core Test Plan

## Test Framework
- **xUnit** - Test framework
- **Shouldly** - Assertions
- **Coverlet** - Code coverage

## Recommended Directory Structure

```
core.jarvis.tests/
├── Unit/                          # Fast, isolated unit tests
│   ├── Data/
│   │   ├── DataContextTests.cs
│   │   ├── ComponentHandlerRegistryTests.cs
│   │   ├── ComponentHandlerTests.cs
│   │   ├── AuditServiceTests.cs
│   │   └── Query/
│   │       └── EntityQueryTests.cs
│   ├── Exceptions/
│   │   ├── DomainExceptionTests.cs
│   │   ├── ValidationExceptionTests.cs
│   │   └── BusinessRuleExceptionTests.cs
│   └── Validation/
│       └── GuardTests.cs
│
├── Integration/                   # Tests with real dependencies
│   ├── SupabaseTests/
│   │   ├── ConnectionTests.cs
│   │   ├── TransactionTests.cs
│   │   └── QueryPerformanceTests.cs
│   └── HandlerIntegrationTests/
│       ├── HandlerRegistrationTests.cs
│       └── HandlerLifecycleTests.cs
│
├── Fixtures/                      # Test data and helpers
│   ├── TestComponents/
│   │   ├── TestComponent.cs
│   │   ├── TestHandler.cs
│   │   └── TestComponentBuilder.cs
│   ├── TestFixtures.cs
│   └── SupabaseTestFixture.cs
│
└── Scenarios/                     # End-to-end scenario tests
    ├── HandlerScenarios.cs
    ├── QueryScenarios.cs
    └── TransactionScenarios.cs
```

## Test Categories

### 1. Unit Tests (Fast, No External Dependencies)
- **DataContext Tests**
  - Handler resolution
  - Query creation
  - Transaction wrapper behavior

- **ComponentHandlerRegistry Tests**
  - Registration
  - Resolution by type
  - Resolution by generic
  - Error cases

- **Handler Base Class Tests**
  - Get() method
  - Validation helpers
  - Error handling

- **Query Tests**
  - Filter building
  - Include pattern
  - Batching logic

- **Exception Tests**
  - Proper inheritance
  - Context preservation
  - Serialization

- **Guard Tests**
  - All validation methods
  - Error messages

### 2. Integration Tests (With Real Services)
- **Supabase Connection**
  - Authentication
  - Table access
  - Error handling

- **Transaction Tests**
  - Rollback behavior
  - Multiple operations
  - Error propagation

- **Handler Integration**
  - DI container integration
  - Handler lifecycle
  - Transaction participation

### 3. Scenario Tests (End-to-End)
- Complete workflows
- Performance benchmarks
- Concurrency tests

## Test Patterns

### 1. Arrange-Act-Assert with Shouldly
```csharp
[Fact]
public async Task Get_WhenComponentExists_ShouldReturnComponent()
{
    // Arrange
    var entityId = Guid.NewGuid();
    var expected = new TestComponent { EntityId = entityId };
    _mockClient.Setup(/*...*/).ReturnsAsync(expected);
    
    var handler = new TestHandler(entityId, _mockClient.Object, _logger);
    
    // Act
    var result = await handler.Get();
    
    // Assert
    result.ShouldNotBeNull();
    result.EntityId.ShouldBe(entityId);
}
```

### 2. Test Naming Convention
`MethodName_StateUnderTest_ExpectedBehavior`

### 3. Test Organization
- One test class per production class
- Nested classes for method grouping
- Use test fixtures for shared setup

## Coverage Goals
- **Unit Tests**: 90%+ coverage
- **Integration Tests**: Key paths covered
- **Focus Areas**:
  - Handler registration/resolution
  - Query building and execution
  - Error handling paths
  - Transaction boundaries

## Mock Strategy
1. Mock Supabase.Client for unit tests
2. Mock ILogger for all tests
3. Use TestDoubles for handlers in registry tests
4. Real implementations for integration tests

## Test Data Management
1. Use builders for complex test objects
2. Randomize IDs to prevent collisions
3. Clean up after integration tests
4. Use transactions for test isolation