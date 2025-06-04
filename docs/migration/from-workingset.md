# Migrating from WorkingSet

To migrate from the old WorkingSet architecture to Jarvis ECS:

- Replace WorkingSet usage with `IDataContext` and handler-based operations.
- Refactor state management to use handlers and components.
- Update queries to use the new query API.
- Review exception handling and validation patterns.

See the rest of this section for more details and examples. 