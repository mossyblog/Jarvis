# Testing Strategies

Jarvis ECS is designed for testability. To test handlers and components:

- Use the test project as a reference for integration and unit tests.
- Test handlers in isolation by providing fake dependencies.
- Avoid mocks when possible; use in-memory or test implementations.
- Test business rules and edge cases.

See the test project for example test cases and approaches. 