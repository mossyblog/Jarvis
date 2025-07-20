# Documentation Information Architecture

This document outlines the information architecture approach for the Jarvis ECS framework documentation, designed to optimize developer experience and knowledge discovery.

## Primary IA Approach: Task-Based Information Architecture

The documentation follows **task-oriented hierarchical design** with several key principles that prioritize developer workflow over internal system organization.

### 1. User Journey Mapping

**Progressive Learning Path**: getting-started → guides → api-reference
- **Role-Based Access**: Different entry points for different developer types
- **Context-Sensitive Grouping**: Information organized by what users need to accomplish

### 2. Hierarchical + Faceted Classification

- **Primary Hierarchy**: By user intent (learn → understand → implement → troubleshoot)
- **Secondary Facets**: By topic (architecture, API, migration, etc.)
- **Cross-References**: Related concepts linked across sections

### 3. Mental Model Alignment

```
Developer Mental Model → Documentation Structure
"How do I start?" → /getting-started/
"How does this work?" → /architecture/
"What can I call?" → /api-reference/
"How do I build X?" → /guides/
"It's broken, help!" → /troubleshooting/
```

### 4. Information Scent Design

- **Descriptive Folder Names**: Clear purpose without jargon
- **Predictable Navigation**: README.md in every folder as wayfinding
- **Semantic Grouping**: Related concepts physically co-located

### 5. Hybrid Approach Elements

- **Topic-Based**: Core architecture concepts grouped together
- **Audience-Based**: Different paths for different experience levels
- **Chronological**: Historical decisions preserved in `/CR/`
- **Functional**: Organized by what developers do with the system

## Recommended Documentation Structure

```
/docs/
├── README.md                          # Navigation hub for all documentation
├── getting-started/
│   ├── README.md                      # Quick start guide
│   ├── installation.md               # Setup and installation
│   ├── first-handler.md              # Creating your first handler
│   └── examples/                     # Code examples
├── architecture/
│   ├── README.md                     # Architecture overview
│   ├── handler-pattern.md            # Core handler pattern
│   ├── ecs-principles.md             # Entity-Component-System concepts
│   ├── data-flow.md                  # How data flows through the system
│   └── postgresql-integration.md     # Storage layer details
├── api-reference/
│   ├── README.md                     # API documentation index
│   ├── core-interfaces.md            # IDataContext, IComponentHandler, etc.
│   ├── query-api.md                  # IEntityQuery and filtering
│   ├── transaction-api.md            # Transaction handling
│   └── extension-methods.md          # Plugin extension patterns
├── guides/
│   ├── handler-development.md        # Writing effective handlers
│   ├── testing-strategies.md         # Testing without mocks
│   ├── performance-optimization.md   # Query batching, N+1 prevention
│   ├── error-handling.md             # Exception patterns
│   └── plugin-architecture.md        # Creating plugin projects
├── migration/
│   ├── README.md                     # Migration guide overview
│   ├── from-workingset.md           # Migrating from old architecture
│   └── breaking-changes.md          # Version upgrade notes
├── CR/                               # Keep existing change requests
│   ├── CR-datacontext.md            # Historical architecture decisions
│   └── [other CR files]
└── troubleshooting/
    ├── README.md                     # Common issues
    ├── performance-issues.md         # Performance debugging
    └── postgresql-connection.md      # Database connectivity issues
```

## Key Documentation Principles

### 1. Progressive Disclosure
Start simple (`getting-started/`) then dive deeper (`architecture/`, `guides/`). This allows developers to build understanding incrementally without being overwhelmed.

### 2. Task-Oriented Structure
Organize by what developers need to accomplish, not by internal code structure. This reduces cognitive load and improves findability.

### 3. Clear Navigation
Each folder should have a README.md that explains its contents and links to other relevant sections. This creates predictable wayfinding patterns.

### 4. Keep Historical Context
Preserve `/CR/` folder to document architectural decisions and evolution. This provides valuable context for understanding "why" decisions were made.

### 5. Living Documentation
Documentation should be maintained alongside code changes. Consider this documentation as much a part of the codebase as the code itself.

## Benefits of This Approach

- **Findability**: Developers can locate information based on their current task
- **Usability**: Information architecture matches developer mental models
- **Scalability**: Structure supports growth without reorganization
- **Maintainability**: Clear ownership and logical grouping
- **Onboarding**: Progressive learning paths for new team members

This approach prioritizes **findability** and **usability** over pure logical categorization, making it easier for developers to locate information based on their current task rather than having to understand the system's internal organization first.