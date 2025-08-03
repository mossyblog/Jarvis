# Code Style and Conventions

## C# Conventions
- Target Framework: .NET 8.0
- Language Version: latest (C# 12)
- Nullable reference types: enabled
- Implicit usings: enabled
- PascalCase for classes, methods, properties
- camelCase for local variables and parameters
- Interfaces prefixed with 'I' (e.g., IComponent, IDataContext)

## Naming Patterns
- Components: `{Name}Component` (e.g., OrderComponent)
- Handlers: `{Name}Handler` (e.g., OrderHandler)
- Systems: `{Name}System` (e.g., OrderSystem)
- Tests: `{Method}_Should_{Expectation}` or `{Method}_With{Condition}_Should{Result}`

## ECS Architecture Rules
1. **Entities**: Just Guid IDs - the identity of things
2. **Components**: Pure data structures (records) implementing IComponent
3. **Handlers**: Business logic for single component type, inherit from ComponentHandler<T>
4. **Systems**: Orchestrate workflows across multiple handlers

## Key Interfaces
```csharp
public interface IComponent
{
    Guid Id { get; init; }
    Guid OwnerEntityId { get; set; }
    DateTime LastUpdated { get; set; }
}

public interface IVersionedComponent : IComponent
{
    int? Version { get; set; }
}
```

## TypeScript/React Conventions
- Functional components with TypeScript
- Use React 19 features
- Props interfaces named `{Component}Props`
- Hooks start with 'use' (e.g., useUIStudio)
- Types in separate files or colocated
- Prefer composition over inheritance

## Frontend File Structure
- Components in `src/components/`
- Hooks in `src/hooks/`
- Types in `src/types/`
- Services in `src/services/`
- Utilities in `src/utils/`

## Important Patterns
- Use early returns to reduce nesting
- Delete old code when replacing (no versioned functions)
- Prefer concrete types over interfaces
- No sleeping/waiting - use proper async patterns
- Always handle errors appropriately