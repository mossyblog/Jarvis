# Jarvis EHS Framework Documentation

A three-layer architecture for .NET 8.0 applications: **Entities/Components** (data) -> **Handlers** (logic) -> **Systems** (orchestration).

## Quick Navigation

| Guide | Description | Time |
|-------|-------------|------|
| [01-quick-start.md](01-quick-start.md) | Setup and first handler | 10 min |
| [02-core-concepts.md](02-core-concepts.md) | EHS architecture explained | 15 min |
| [03-handlers.md](03-handlers.md) | Building business logic | 20 min |
| [04-systems.md](04-systems.md) | Workflow orchestration | 15 min |
| [05-testing.md](05-testing.md) | Testing without mocks | 15 min |
| [06-database.md](06-database.md) | PostgreSQL and data access | 15 min |

## The Three Layers

```
+----------------------------------------------------------+
|                     Azure Functions                       |
|                  (HTTP parsing only)                      |
+----------------------------------------------------------+
                           |
                           v
+----------------------------------------------------------+
|                        SYSTEMS                            |
|         Orchestrate handlers - NO business logic          |
+----------------------------------------------------------+
                           |
                           v
+----------------------------------------------------------+
|                       HANDLERS                            |
|           ALL business logic lives here                   |
|           Validation, state transitions, rules            |
+----------------------------------------------------------+
                           |
                           v
+----------------------------------------------------------+
|                 ENTITIES & COMPONENTS                     |
|                Pure data - no behavior                    |
+----------------------------------------------------------+
                           |
                           v
+----------------------------------------------------------+
|                      POSTGRESQL                           |
|                Row-Level Security via JWT                 |
+----------------------------------------------------------+
```

## Critical Rules

1. **No try-catch in handlers** - Let exceptions bubble to API middleware
2. **No mocks in tests** - Use real PostgreSQL, real handlers
3. **Handlers own ALL business logic** - Systems only orchestrate
4. **DataContext is the ONLY data access** - No direct database queries

## Start Here

**New to Jarvis?** Start with [01-quick-start.md](01-quick-start.md)

**Need architecture understanding?** Read [02-core-concepts.md](02-core-concepts.md)

**Building a feature?** Follow [03-handlers.md](03-handlers.md) then [04-systems.md](04-systems.md)

**Writing tests?** See [05-testing.md](05-testing.md)

**Working with data?** Reference [06-database.md](06-database.md)

## Examples

Working code samples: [examples/](examples/)

## Project Structure

```
jarvis/
  core.jarvis/           # EHS framework - handlers, systems, data context
  core.jarvis.data/      # PostgreSQL client with RLS support
  core.jarvis.api/       # Azure Functions API layer
  core.jarvis.tests/     # Integration tests (no mocks)
```

## Archive

Historical documentation moved to [_archive/](_archive/) during the January 2025 documentation restructure.
