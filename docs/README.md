# Jarvis Documentation

**Status:** Approved  
**Author:** Jarvis Team  
**Owner:** Technical Documentation Team  
**Last Updated:** 2025-07-27  
**Review Due:** 2025-10-27  
**Version:** 2.0  

**Tags:** ecs, documentation, jarvis, framework, togaf  
**Systems:** core.jarvis, core.jarvis.data, core.jarvis.api  
**Components:** IComponent, IComponentHandler, IDataContext  

---

## Purpose

This document serves as the main entry point for the Jarvis ECS SDK documentation, organized according to TOGAF architecture framework principles. The documentation is structured to support architectural decision-making, implementation guidance, and operational excellence.

---

## Quick Facts

- **Current State:** Active - Reorganized to TOGAF structure
- **Dependencies:** .NET 8.0, PostgreSQL 14+, Azure Functions v4
- **Consumers:** Backend developers, system architects, DevOps engineers
- **SLA:** Documentation updated within 7 days of major releases

---

## Documentation Structure (TOGAF-Based)

### [00 - Overview](00_Overview/README.md)
High-level overview documentation for the Jarvis ECS framework.
- [Jarvis Overview](00_Overview/jarvis-overview.md) - Introduction to the framework
- [Installation Guide](00_Overview/installation.md) - Setting up your environment
- [First Handler](00_Overview/first-handler.md) - Quick start guide
- [Examples](00_Overview/examples/) - Working code examples

### [01 - Current State](01_CurrentState/README.md)
Documentation of the current architecture and implementation.

#### [Services](01_CurrentState/Services/README.md)
- [Authentication, RBAC & RLS](01_CurrentState/Services/authentication-rbac-rls-technical-whitepaper.md)
- [Connection Pooling](01_CurrentState/Services/connection-pooling-technical-whitepaper.md)

#### [Components](01_CurrentState/Components/README.md)
- Core component documentation
- Handler implementations
- Component relationships

#### [Flows](01_CurrentState/Flows/README.md)
- Data flow patterns
- System workflows
- Process documentation

#### [Mappings](01_CurrentState/Mappings/README.md)
- Entity relationships
- Component dependencies
- System integrations

#### [Technology](01_CurrentState/Technology/README.md)
- PostgreSQL integration
- .NET 8.0 framework usage
- Azure Functions deployment

### [02 - Target State](02_TargetState/README.md)
Future architecture and planned enhancements.

### [03 - Gap Analysis](03_GapAnalysis/README.md)
Analysis of gaps between current and target states.

### [04 - Roadmap](04_Roadmap/README.md)
Implementation roadmap and timeline.

### [05 - Governance](05_Governance/README.md)
Architecture principles, standards, and guidelines.
- [Architecture Decisions](05_Governance/decisions/README.md)

### [06 - Catalogs](06_Catalogs/README.md)
Reusable components, patterns, and best practices.

### [07 - Projects](07_Projects/README.md)
Project-specific documentation and case studies.

### [08 - Change Requests](08_ChangeRequests/README.md)
Change management and request tracking.

### [09 - Diagrams](09_Diagrams/README.md)
Architecture diagrams and visual documentation.

### [10 - Vocabulary](10_Vocabulary/README.md)
Glossary and terminology definitions.

### Additional Resources

#### [Troubleshooting](troubleshooting/README.md)
Common issues and solutions (maintained separately from TOGAF structure for quick access).

---

## Navigation Guide

### For New Users
1. Start with [Overview](00_Overview/README.md)
2. Follow the [Installation Guide](00_Overview/installation.md)
3. Build your [First Handler](00_Overview/first-handler.md)
4. Explore [Examples](00_Overview/examples/)

### For Architects
1. Review [Current State Architecture](01_CurrentState/README.md)
2. Understand [Target State](02_TargetState/README.md)
3. Analyze [Gaps](03_GapAnalysis/README.md)
4. Check [Governance](05_Governance/README.md)

### For Developers
1. Browse [Component Catalogs](06_Catalogs/README.md)
2. Study [Current Implementation](01_CurrentState/Components/README.md)
3. Review [Best Practices](05_Governance/README.md)
4. Check [Troubleshooting](troubleshooting/README.md)

---

## Quick Links

### Essential Documentation
- [ECS Architecture Principles](05_Governance/ecs-principles.md)
- [Handler Pattern Guide](05_Governance/handler-pattern.md)
- [Comprehensive Handler Guide](01_CurrentState/Components/comprehensive-handler-guide.md)
- [Security Model](01_CurrentState/Services/authentication-rbac-rls-technical-whitepaper.md)

### API References
- [Core Interfaces](01_CurrentState/Components/core-interfaces.md)
- [DataContext API](01_CurrentState/Mappings/datacontext-api.md)
- [Query API](01_CurrentState/Technology/query-api.md)

### Development Resources
- [Testing Strategies](07_Projects/testing-strategies.md)
- [Performance Optimization](05_Governance/performance-optimization.md)
- [Error Handling](05_Governance/error-handling.md)

---

## Documentation Standards

All documentation in this repository follows:
- TOGAF architecture framework principles
- Mandatory template structure with metadata headers
- Clear ownership and review cycles
- Consistent naming conventions (kebab-case)
- Cross-references using relative paths

For documentation guidelines, see [Information Architecture](05_Governance/information_architecture.md).

---

## Contributing

When contributing documentation:
1. Follow the TOGAF structure
2. Use the mandatory template format
3. Update cross-references
4. Maintain consistency with existing documentation
5. Submit for review according to governance processes

---

## Change History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 2.0 | 2025-07-27 | Technical Documentation Team | Reorganized to TOGAF structure |
| 1.0 | 2025-01-01 | Jarvis Team | Initial documentation |

---

## Related Documentation

- [Information Architecture Guidelines](05_Governance/information_architecture.md)
- [Documentation Audit Report](05_Governance/documentation-audit-report.md)
- [CLAUDE.md](../CLAUDE.md) - AI assistant guidelines