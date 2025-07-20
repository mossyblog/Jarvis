# Jarvis Documentation

Welcome to the Jarvis ECS SDK documentation. This guide will help you understand and effectively use the framework.

## 📚 Documentation Structure

### Getting Started
- **[Installation Guide](getting-started/installation.md)** - Set up Jarvis in your project
- **[Your First Handler](getting-started/first-handler.md)** - Build your first feature
- **[Examples](getting-started/examples/)** - Working code examples

### Core Concepts
- **[ECS Architecture](architecture/ecs-principles.md)** - Understanding Entity Component System
- **[Handler Pattern](architecture/handler-pattern.md)** - How handlers encapsulate business logic
- **[Data Flow](architecture/data-flow.md)** - Request lifecycle through the system
- **[Security Model](AUTHENTICATION_RBAC_RLS_TECHNICAL_WHITEPAPER.md)** - JWT, RLS, and permissions

### Development Guides
- **[Handler Development](guides/handler-development.md)** - Best practices for handlers
- **[Testing Strategies](guides/testing-strategies.md)** - Unit and integration testing
- **[Error Handling](guides/error-handling.md)** - Exception patterns and recovery
- **[Performance Optimization](guides/performance-optimization.md)** - Scaling and optimization

### API Reference
- **[Core Interfaces](api-reference/core-interfaces.md)** - IDataContext, IComponentHandler
- **[Query API](api-reference/query-api.md)** - Entity querying and filtering
- **[Extension Methods](api-reference/extension-methods.md)** - Plugin extension patterns
- **[Snapshot API](api-reference/snapshot-api.md)** - Component versioning

### Advanced Topics
- **[System Pattern](architecture/system-pattern-example.md)** - Orchestration layer
- **[Connection Pooling](CONNECTION_POOLING_TECHNICAL_WHITEPAPER.md)** - Database optimization
- **[Plugin Architecture](guides/plugin-architecture.md)** - Extending the framework
- **[Migration Guide](migration/README.md)** - Upgrading from older versions

### Troubleshooting
- **[Common Issues](troubleshooting/README.md)** - Solutions to frequent problems
- **[Performance Issues](troubleshooting/performance-issues.md)** - Debugging slow queries
- **[Database Connection](troubleshooting/postgresql-connection.md)** - Connection troubleshooting

## 🎯 Quick Links by Role

### For Backend Developers
1. Start with [Installation](getting-started/installation.md)
2. Build [Your First Handler](getting-started/first-handler.md)
3. Learn [Handler Development](guides/handler-development.md)
4. Master [Testing Strategies](guides/testing-strategies.md)

### For Architects
1. Understand [ECS Principles](architecture/ecs-principles.md)
2. Review [System Architecture](SYSTEM_HANDLER_ARCHITECTURE.md)
3. Explore [Security Model](AUTHENTICATION_RBAC_RLS_TECHNICAL_WHITEPAPER.md)
4. Plan [Plugin Architecture](guides/plugin-architecture.md)

### For DevOps Engineers
1. Configure [Database Connection](getting-started/installation.md#database-setup)
2. Optimize [Connection Pooling](CONNECTION_POOLING_TECHNICAL_WHITEPAPER.md)
3. Monitor [Performance](guides/performance-optimization.md)
4. Deploy [API Layer](../core.jarvis.api/README.md)

## 📖 Reading Order

For comprehensive understanding, we recommend this order:

1. **Concepts First**
   - [ECS Principles](architecture/ecs-principles.md)
   - [Handler Pattern](architecture/handler-pattern.md)
   
2. **Hands-On Practice**
   - [Installation](getting-started/installation.md)
   - [First Handler](getting-started/first-handler.md)
   - [Examples](getting-started/examples/)

3. **Deep Dive**
   - [Handler Development](guides/handler-development.md)
   - [Query API](api-reference/query-api.md)
   - [Testing Strategies](guides/testing-strategies.md)

4. **Advanced Topics**
   - [System Pattern](architecture/system-pattern-example.md)
   - [Plugin Architecture](guides/plugin-architecture.md)
   - [Performance Optimization](guides/performance-optimization.md)

## 🔍 Finding Information

### By Task
- **"How do I create a handler?"** → [Handler Development](guides/handler-development.md)
- **"How do I query entities?"** → [Query API](api-reference/query-api.md)
- **"How do I handle errors?"** → [Error Handling](guides/error-handling.md)
- **"How do I test my code?"** → [Testing Strategies](guides/testing-strategies.md)

### By Problem
- **"My queries are slow"** → [Performance Issues](troubleshooting/performance-issues.md)
- **"Can't connect to database"** → [Database Connection](troubleshooting/postgresql-connection.md)
- **"Tests are failing"** → [Testing Strategies](guides/testing-strategies.md#troubleshooting)

### By Component
- **Data Layer** → [core.jarvis.data README](../core.jarvis.data/README.md)
- **API Layer** → [core.jarvis.api README](../core.jarvis.api/README.md)
- **Core Framework** → [Architecture Overview](architecture/ecs-principles.md)

## 💡 Best Practices

1. **Start Small**: Begin with simple handlers before complex orchestration
2. **Test Early**: Write tests alongside your handlers
3. **Track Relationships**: Use LinkRelationship for entity hierarchies
4. **Audit Everything**: Use the built-in audit service for compliance
5. **Optimize Later**: Get it working first, then optimize queries

## 🤝 Contributing to Documentation

Found an error or want to improve the docs? 
- Submit a PR with your changes
- Focus on clarity and real-world examples
- Test all code examples before submitting
- Keep the same friendly, practical tone

## 📞 Need Help?

- Check [Troubleshooting](troubleshooting/README.md) first
- Search existing [GitHub Issues](https://github.com/yourusername/jarvis/issues)
- Join our [Discussions](https://github.com/yourusername/jarvis/discussions)
- Review [Examples](getting-started/examples/) for patterns