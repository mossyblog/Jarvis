# UIStudio ECS Components Design Summary

## Overview

This document outlines the complete ECS component system designed for UIStudio to persist dynamic pages, fixed pages, and hybrid pages. The design follows the Jarvis ECS patterns and provides comprehensive functionality for page management, layout configuration, component bindings, permissions, versioning, templates, and audit logging.

## Component Architecture

### Core Components

#### 1. UIStudioPage (`/mnt/c/code/risksec/jarvis/core.jarvis.api/Models/UIStudioPage.cs`)
**Purpose**: Main page definition and metadata storage
- **Implements**: `IComponent`, `IVersionedComponent`
- **Key Features**:
  - Page metadata (name, slug, type, description)
  - Publishing and default page management
  - Layout entity reference
  - User tracking (created by, modified by)
  - Tag-based categorization
  - Sort order for listing

#### 2. UIStudioLayout (`/mnt/c/code/risksec/jarvis/core.jarvis.api/Models/UIStudioLayout.cs`)
**Purpose**: Grid layout configuration and responsive design settings
- **Implements**: `IComponent`, `IVersionedComponent`
- **Key Features**:
  - Multiple layout types (bento, grid, flex, absolute)
  - JSON-based grid configuration
  - Responsive breakpoint definitions
  - Container settings and custom styles
  - Template capabilities
  - Schema versioning for migrations

#### 3. UIStudioComponentBinding (`/mnt/c/code/risksec/jarvis/core.jarvis.api/Models/UIStudioComponentBinding.cs`)
**Purpose**: Binding between UI components and ECS components with field mappings
- **Implements**: `IComponent`, `IVersionedComponent`
- **Key Features**:
  - Component type and instance identification
  - ECS component type binding
  - JSON-based field mappings
  - Data source configuration
  - Position and style configuration
  - Behavior and interaction settings
  - Visibility and permissions control

#### 4. UIStudioPermission (`/mnt/c/code/risksec/jarvis/core.jarvis.api/Models/UIStudioPermission.cs`)
**Purpose**: Access control and sharing settings for UIStudio resources
- **Implements**: `IComponent`, `IVersionedComponent`
- **Key Features**:
  - Resource-based permission model
  - Multiple grantee types (user, role, group)
  - Permission levels (view, edit, admin, owner)
  - Inheritance support
  - Expiration and conditional permissions
  - Activity tracking

#### 5. UIStudioVersion (`/mnt/c/code/risksec/jarvis/core.jarvis.api/Models/UIStudioVersion.cs`)
**Purpose**: Version history and snapshot tracking for all UIStudio resources
- **Implements**: `IComponent` (intentionally not versioned to avoid recursion)
- **Key Features**:
  - Complete snapshot storage with JSON data
  - Change tracking and summaries
  - Published vs auto-generated versions
  - Branch and parent version support
  - Retention policies and cleanup
  - Integrity verification with checksums

#### 6. UIStudioTemplate (`/mnt/c/code/risksec/jarvis/core.jarvis.api/Models/UIStudioTemplate.cs`)
**Purpose**: Reusable templates for quick page and layout creation
- **Implements**: `IComponent`, `IVersionedComponent`
- **Key Features**:
  - Multiple template types (page, layout, component_set)
  - Category and subcategory organization
  - Public vs private templates
  - Featured template promotion
  - Usage tracking and rating system
  - Requirements specification

#### 7. UIStudioAuditLog (`/mnt/c/code/risksec/jarvis/core.jarvis.api/Models/UIStudioAuditLog.cs`)
**Purpose**: Comprehensive audit logging for security and compliance
- **Implements**: `IComponent` (intentionally not versioned)
- **Key Features**:
  - User and system action tracking
  - Security level classification
  - Detailed action information with JSON metadata
  - Session and context tracking
  - Retention policies
  - Correlation ID for related actions

## Handler Layer

### Core Handlers

Each component has a corresponding handler that provides business logic and CRUD operations:

1. **UIStudioPageHandler** - Page management with slug validation and default page logic
2. **UIStudioLayoutHandler** - Layout configuration with template creation and validation
3. **UIStudioComponentBindingHandler** - Component binding with field mapping and positioning
4. **UIStudioPermissionHandler** - Permission management with hierarchy and expiration
5. **UIStudioVersionHandler** - Version control with snapshots and comparison
6. **UIStudioTemplateHandler** - Template management with rating and usage tracking
7. **UIStudioAuditLogHandler** - Audit logging with filtering and cleanup

### Handler Features

All handlers follow Jarvis patterns:
- Inherit from `ComponentHandler<T>`
- Provide both simple CRUD and business logic methods
- Include comprehensive validation
- Support querying and filtering
- Implement proper error handling and logging

## System Orchestration

### UIStudioSystem (`/mnt/c/code/risksec/jarvis/core.jarvis.api/Systems/UIStudioSystem.cs`)

The system layer orchestrates complex operations across multiple handlers:

#### Key Operations
- **CreatePage**: Creates page with layout and bindings in single transaction
- **UpdatePage**: Updates page and creates version snapshot
- **PublishPage**: Publishes page and creates published version
- **CreateTemplateFromPage**: Converts existing page to reusable template
- **GrantPermission**: Manages resource access control

#### Features
- **Transaction Management**: Ensures consistency across multiple components
- **Version Control**: Automatic snapshot creation for all changes
- **Audit Logging**: Comprehensive action tracking with correlation IDs
- **Error Handling**: Proper rollback and error reporting
- **Security**: Permission validation and security level classification

## Integration with Jarvis Patterns

### ECS Compliance
- All components implement `IComponent` interface
- Version-controlled components implement `IVersionedComponent`
- Handlers inherit from `ComponentHandler<T>`
- System follows orchestration patterns

### Database Integration
- Automatic snake_case mapping (e.g., `PageName` → `page_name`)
- PostgreSQL JSON support for complex configurations
- Optimistic concurrency with version tracking
- Row-level security through JWT context

### Naming Conventions
- Follows modern C# idioms (concise, intention-revealing names)
- Consistent method naming without redundant prefixes
- Clear property names with database mapping comments

## Usage Scenarios

### Dynamic Pages
- Create page with bento grid layout
- Bind UI components to ECS data sources
- Configure field mappings and data filtering
- Set up responsive breakpoints

### Fixed Pages
- Define static layout with positioned components
- Configure read-only component bindings
- Set up navigation and routing

### Hybrid Pages
- Combine static and dynamic sections
- Use conditional component visibility
- Implement progressive enhancement

### Template System
- Create reusable page templates
- Organize by category and subcategory
- Track usage and ratings
- Enable public sharing

### Permission Management
- Resource-level access control
- Time-based permissions
- Inheritance from parent resources
- Audit trail for all permission changes

### Version Control
- Automatic snapshots on changes
- Published version tracking
- Change comparison and rollback
- Retention policy management

## File Locations

All components are located in the Jarvis API project:

### Models (Components)
- `/mnt/c/code/risksec/jarvis/core.jarvis.api/Models/UIStudioPage.cs`
- `/mnt/c/code/risksec/jarvis/core.jarvis.api/Models/UIStudioLayout.cs`
- `/mnt/c/code/risksec/jarvis/core.jarvis.api/Models/UIStudioComponentBinding.cs`
- `/mnt/c/code/risksec/jarvis/core.jarvis.api/Models/UIStudioPermission.cs`
- `/mnt/c/code/risksec/jarvis/core.jarvis.api/Models/UIStudioVersion.cs`
- `/mnt/c/code/risksec/jarvis/core.jarvis.api/Models/UIStudioTemplate.cs`
- `/mnt/c/code/risksec/jarvis/core.jarvis.api/Models/UIStudioAuditLog.cs`

### Handlers
- `/mnt/c/code/risksec/jarvis/core.jarvis.api/Handlers/UIStudioPageHandler.cs`
- `/mnt/c/code/risksec/jarvis/core.jarvis.api/Handlers/UIStudioLayoutHandler.cs`
- `/mnt/c/code/risksec/jarvis/core.jarvis.api/Handlers/UIStudioComponentBindingHandler.cs`
- `/mnt/c/code/risksec/jarvis/core.jarvis.api/Handlers/UIStudioPermissionHandler.cs`
- `/mnt/c/code/risksec/jarvis/core.jarvis.api/Handlers/UIStudioVersionHandler.cs`
- `/mnt/c/code/risksec/jarvis/core.jarvis.api/Handlers/UIStudioTemplateHandler.cs`
- `/mnt/c/code/risksec/jarvis/core.jarvis.api/Handlers/UIStudioAuditLogHandler.cs`

### Systems
- `/mnt/c/code/risksec/jarvis/core.jarvis.api/Systems/UIStudioSystem.cs`

## Next Steps

To complete the implementation:

1. **Register Components**: Add all handlers to dependency injection in `Program.cs`
2. **Create Functions**: Build Azure Functions for HTTP endpoints
3. **Add Validation**: Implement additional business rule validation
4. **Create Tests**: Write integration tests following Jarvis patterns
5. **Add Migrations**: Create database schema migrations if needed
6. **Documentation**: Create API documentation and usage examples

This design provides a comprehensive, scalable foundation for UIStudio's page management system while maintaining full compliance with Jarvis ECS patterns and conventions.