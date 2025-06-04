# Error Handling

Jarvis ECS uses custom exceptions for robust error handling:

- **DomainException**: Base for domain-specific errors.
- **ValidationException**: Thrown for invalid input or state.
- **BusinessRuleException**: Thrown when a business rule is violated.
- **ComponentOperationException**: For component handler errors.

Always catch and handle exceptions at appropriate layers. Use meaningful messages to aid debugging. 