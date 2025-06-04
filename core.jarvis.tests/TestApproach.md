# Test Approach Without Moq

## Overview
We've moved away from Moq to a more reliable testing approach using:
- **Test Doubles**: Simple fake implementations
- **Stub Objects**: Pre-configured responses
- **State-Based Testing**: Focus on outcomes rather than interactions

## Benefits of This Approach

1. **No False Positives**: Tests verify actual behavior, not mock setup
2. **Better Refactoring**: Tests don't break when implementation details change
3. **Clearer Intent**: Test doubles show exactly what they do
4. **Faster Tests**: No reflection/proxy overhead from mocking frameworks

## Test Double Types Used

### 1. Fakes
```csharp
public class FakeComponentHandlerRegistry : IComponentHandlerRegistry
{
    // Simple in-memory implementation
    // Stores handlers and returns them when requested
}
```

### 2. Stubs
```csharp
public class StubTestHandler : IComponentHandler<TestComponent>
{
    // Returns pre-configured responses
    public void SetupGet(TestComponent component) { ... }
    public void SetupGetThrows(Exception ex) { ... }
}
```

### 3. Test-Specific Implementations
```csharp
public class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> Logs { get; } = new();
    // Captures log entries for verification
}
```

## Testing Patterns

### 1. Constructor Validation
```csharp
[Fact]
public void WithNullRegistry_ShouldThrowArgumentNullException()
{
    Should.Throw<ArgumentNullException>(() => 
        new DataContext(null!, client, logger))
        .ParamName.ShouldBe("registry");
}
```

### 2. Behavior Verification
```csharp
[Fact]
public void For_ShouldDelegateToRegistry()
{
    // Arrange - Set up test double
    var handler = new StubTestHandler(entityId);
    registry.AddHandler(entityId, handler);

    // Act
    var result = dataContext.For<StubTestHandler>(entityId);

    // Assert - Verify outcome
    result.ShouldBe(handler);
}
```

### 3. State Verification
```csharp
[Fact]
public void ShouldLogDebugMessage()
{
    // Act
    _dataContext.Query();

    // Assert - Check captured state
    _logger.Logs.ShouldContain(log => 
        log.LogLevel == LogLevel.Debug && 
        log.Message.Contains("Creating entity query"));
}
```

## When to Use Each Approach

1. **Fakes**: When you need a working implementation (e.g., Registry)
2. **Stubs**: When you need specific responses (e.g., Handler.Get())
3. **Null Objects**: When the dependency isn't relevant (e.g., NullLogger)
4. **Real Objects**: When they're simple and fast (e.g., Exceptions)

## Key Principles

1. **Test Behavior, Not Implementation**
2. **Keep Test Doubles Simple**
3. **Make Tests Readable**
4. **Avoid Over-Specification**
5. **Focus on Outcomes**