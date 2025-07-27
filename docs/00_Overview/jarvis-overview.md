# Jarvis ECS Framework Overview

**Status:** Approved  
**Author:** Jarvis Team  
**Owner:** Technical Documentation Team  
**Last Updated:** 2025-07-27  
**Review Due:** 2025-10-27  
**Version:** 1.0  

**Tags:** ecs, documentation, jarvis, framework  
**Systems:** core.jarvis, core.jarvis.data, core.jarvis.api  
**Components:** IComponent, IComponentHandler, IDataContext  

---

## Purpose

This document provides a high-level overview of the Jarvis Entity Component System (ECS) framework, its architecture, and core concepts.

---

## Quick Facts

- **Current State:** Active
- **Dependencies:** .NET 8.0, PostgreSQL 14+, Azure Functions v4
- **Consumers:** Backend developers, system architects, DevOps engineers
- **SLA:** Documentation updated within 7 days of major releases

---

## What is Jarvis?

Jarvis is an Entity Component System (ECS) framework for .NET 8.0 that implements a sophisticated handler-based architecture for building scalable, maintainable applications. The project consists of three core SDKs:

1. **core.jarvis** - Main ECS framework with handler pattern
2. **core.jarvis.data** - PostgreSQL data access with JWT-based Row Level Security
3. **core.jarvis.api** - Azure Functions REST API layer

## Core Concepts

### Entity Component System (ECS)
- **Entities**: Just IDs (Guid) - the identity of things
- **Components**: Pure data structures (records) implementing `IComponent`
- **Handlers**: Business logic for single component type, inherit from `ComponentHandler<T>`
- **Systems**: Orchestrate workflows across multiple handlers

### Key Benefits
- **Separation of Concerns**: Data (components) separated from logic (handlers)
- **Testability**: No mocks needed, test against real database
- **Performance**: Efficient queries and batch operations
- **Security**: Row-level security at database level
- **Scalability**: Horizontal scaling through stateless handlers

## Architecture Overview

The framework follows a layered architecture:

1. **API Layer** (Azure Functions)
   - HTTP endpoints
   - Request/response handling
   - Authentication/authorization

2. **System Layer**
   - Business workflow orchestration
   - Cross-handler coordination
   - Transaction management

3. **Handler Layer**
   - Component-specific business logic
   - CRUD operations
   - Validation

4. **Data Layer**
   - PostgreSQL with Row Level Security
   - Automatic schema management
   - Optimistic concurrency control

## Getting Started

- **[Installation Guide](installation.md)** - Set up Jarvis in your project
- **[Your First Handler](first-handler.md)** - Build your first feature
- **[Examples](examples/)** - Working code examples

---

## Change History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-07-27 | Jarvis Team | Initial documentation |

---

## Related Documentation

- [Current State Architecture](../01_CurrentState/README.md)
- [ECS Principles](../05_Governance/ecs-principles.md)
- [Handler Pattern](../05_Governance/handler-pattern.md)