# ECS Principles

Entity Component System (ECS) is a design pattern that separates data (components) from logic (systems/handlers) and identity (entities).

## In Jarvis ECS:

- **Entity**: Represents a unique object in the system. Created via `dataContext.Entity()` (See `Entity.cs`)
- **Component**: Holds data for a specific aspect of an entity. All components implement `IComponent` with `Id`, `OwnerEntityId`, and `LastUpdated` properties
- **Handler (System)**: Encapsulates logic for a component type. (See `ComponentHandler`)

## Component Types

### Standard Components
Basic components implement `IComponent` and use timestamp-based concurrency control via the `LastUpdated` field.

### Versioned Components  
Components implementing `IVersionedComponent` get automatic version incrementing and enhanced concurrency control. Use for critical data requiring strict consistency.

## Key Features

- **Automatic Schema Management**: Tables are created and validated automatically via `ITableManager`
- **Flexible Concurrency Control**: Version-based for versioned components, timestamp-based for others
- **Enhanced Audit Trail**: Pre-change state capture for better tracking
- **Entity Creation**: Generate unique entities with `dataContext.Entity()`

Entities are composed of components. Handlers operate on components, not entities directly. This enables flexible, modular, and testable code with strong data consistency guarantees. 