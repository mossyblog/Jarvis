# Data Flow

Jarvis ECS manages data flow through a combination of context, registry, and storage.

## Key Components

- **DataContext**: Main entry point for handler-based operations. Resolves handlers and manages transactions.
- **IComponentHandlerRegistry**: Keeps track of available handlers and resolves them for entities.
- **Supabase**: Provides persistent storage for entities and components.

## Flow Example

1. `DataContext` is resolved from DI.
2. You request a handler for an entity/component.
3. The registry provides the correct handler.
4. The handler performs operations using the Supabase client.

This design enables modular, testable, and scalable data operations. 