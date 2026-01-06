# Examples

Working code examples demonstrating Jarvis EHS patterns.

## Available Examples

All examples are extracted from the test suite in `core.jarvis.tests/`.

| Example | Location | Key Concepts |
|---------|----------|--------------|
| OrderHandler | `Examples/OrderHandler.cs` | State transitions, validation |
| BlogHandler | `Examples/Blog/` | Component + Handler pattern |
| WorkOrderHandler | `Examples/WorkOrder/` | State machine pattern |

## Running Examples

```bash
# Run all integration tests
dotnet test core.jarvis.tests

# Run specific handler tests
dotnet test core.jarvis.tests --filter "OrderHandler"
```

## Example Structure

Each example follows the same pattern:

```
Component (data) -> Handler (logic) -> System (orchestration) -> Test (verification)
```

See [Core Concepts](../02-core-concepts.md) for architecture details.
