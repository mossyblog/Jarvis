# ECS Principles

Entity Component System (ECS) is a design pattern that separates data (components) from logic (systems/handlers) and identity (entities).

## In Jarvis ECS:

- **Entity**: Represents a unique object in the system. (See `Entity.cs`)
- **Component**: Holds data for a specific aspect of an entity.
- **Handler (System)**: Encapsulates logic for a component type. (See `ComponentHandler`)

Entities are composed of components. Handlers operate on components, not entities directly. This enables flexible, modular, and testable code. 