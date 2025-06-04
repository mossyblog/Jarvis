# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is the Jarvis ECS (Entity Component System) framework, a .NET 8 data orchestration library that provides:
- Entity-Component-System pattern for flexible data modeling
- Plugin-based handler architecture (currently being refactored - see `/docs/CR/`)
- Supabase (PostgreSQL) backend for persistence
- Event sourcing and audit trail capabilities
- Separate data access SDK (`core.jarvis.data`) for direct PostgreSQL access with JWT-based RLS

## IMPORTANT: Major Architecture Change in Progress

**The codebase is transitioning from a document-based (WorkingSet/PartitionDocument) architecture to a plugin-based handler model.**

See `/docs/CR/CR-datacontext.md` for the complete change request and `/docs/CR/tasks.md` for implementation tasks.

### Current Architecture (Being Removed)
- WorkingSet-based mutation tracking
- Commit() pattern for persistence
- Document-style storage approach

### New Architecture (Being Implemented)
- Plugin-based handler model
- No direct component references in core
- Handler registry pattern
- Transaction support via `InTransaction()`
- Optimized entity queries with batching

## Key Architecture Concepts

### Solution Structure

The solution contains two main SDKs:

1. **core.jarvis** - Main ECS framework with handler pattern
   - Entity-Component-System orchestration
   - Handler-based business logic
   - Supabase integration for component storage

2. **core.jarvis.data** - Low-level PostgreSQL data access
   - Direct Dapper-based data access
   - SDK-level Row Level Security (RLS)
   - JWT authentication and claim-based access control
   - Automatic PascalCase to snake_case mapping

### Core Components (core.jarvis)

1. **IDataContext**: Main entry point providing handler access
   ```csharp
   THandler For<THandler>(Guid entityId) where THandler : IComponentHandler;
   IEntityQuery Query();
   Task<T> InTransaction<T>(Func<ITransaction, Task<T>> action);
   ```

2. **IComponentHandler**: Base interface for all component operations
   - Handlers encapsulate all business logic
   - Live in plugin projects, not core
   - Responsible for validation, persistence, and audit

3. **IEntityQuery**: Cross-component query interface
   - `WithAll<T1, T2>()` for entities with all specified components
   - `WithAny<T1, T2>()` for entities with any specified components
   - `WithNone<T1, T2>()` for entities without specified components
   - Type-safe, reflection-free implementation

4. **Plugin Extension Pattern**: 
   - Core provides `_dataContext.For<InvoiceHandler>(id)`
   - Plugins provide `_dataContext.Invoice(id)` extension methods

### Data Access Components (core.jarvis.data)

1. **PgClient**: Secure PostgreSQL client with JWT-based RLS
   ```csharp
   var jwt = await client.Authenticate("user@example.com", "password");
   client.JWT(jwt);
   var data = await client.From<MyTable>().Get();
   ```

2. **PgTable<T>**: Type-safe table access with filtering
   ```csharp
   await client.From<Product>()
       .Filter("price", "gte", 100)
       .Filter("category", "eq", "electronics")
       .Get();
   ```

3. **RLS Policy System**: SDK-level access control
   - Multi-tenant isolation
   - User-level security
   - Role-based access control
   - Custom policy support

### Storage Architecture

- **Storage Backend**: Supabase (PostgreSQL)
- **One Table Per Component**: Table name matches component class (lowercase)
- **No SQL JOINs**: Entity relationships via shared entity_id
- **Handler-Owned Persistence**: Each handler manages its own SQL operations
- **SDK-Level RLS**: Access control enforced in SDK, not database

## Key Architectural Constraints

- **Components must be records not classes**
  - Ensures immutability and value-based equality
  - Supports clean state management
  - Facilitates easy serialization and comparison

[Rest of the document remains unchanged...]