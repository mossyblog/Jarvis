# Authentication Architecture

This section provides a deep understanding of how authentication works in the Jarvis ECS framework.

## Overview

Authentication in Jarvis follows a multi-layered approach that aligns with the ECS architecture:

```
┌─────────────────────────────────────────────────┐
│                 API Layer                        │
│              (Azure Functions)                   │
│  • RegisterFunction  • AuthFunction             │
│  • Token validation • HTTPS termination         │
└─────────────────────┬───────────────────────────┘
                      │ HTTP + JSON
┌─────────────────────▼───────────────────────────┐
│               System Layer                       │
│              (AuthSystem - Future)               │
│  • Orchestrate workflows • Business rules       │
│  • Multi-handler operations                     │
└─────────────────────┬───────────────────────────┘
                      │ Component Operations
┌─────────────────────▼───────────────────────────┐
│              Handler Layer                       │
│       (AccountHandler + AuthHandler)            │
│  • Account lifecycle  • Authentication logic    │
│  • Password management • Token generation       │
└─────────────────────┬───────────────────────────┘
                      │ Component CRUD
┌─────────────────────▼───────────────────────────┐
│             Component Layer                      │
│          (Account + AuthToken)                   │
│  • Account data      • Session data             │
│  • Authentication    • Token storage            │
└─────────────────────┬───────────────────────────┘
                      │ Database Operations
┌─────────────────────▼───────────────────────────┐
│            Database Layer                        │
│              (PostgreSQL)                        │
│  • account_component • auth_token_component      │
│  • BCrypt password   • JWT session storage      │
└─────────────────────────────────────────────────┘
```

## Navigation

### Core Concepts
- [Security Model](security-model.md) - Multi-layered security approach
- [Component Design](component-design.md) - Account and AuthToken components
- [Handler Pattern](handler-pattern.md) - AccountHandler and AuthHandler details

### Advanced Topics
- [JWT Token Architecture](jwt-architecture.md) - Token generation and validation
- [Row-Level Security](rls-integration.md) - PostgreSQL RLS integration
- [Session Management](session-architecture.md) - Session tracking and security

### Integration Points
- [API Layer Integration](api-integration.md) - Azure Functions setup
- [Database Schema](database-schema.md) - PostgreSQL table structure

## Key Architecture Decisions

### 1. Component-Based Authentication

Authentication follows the same ECS patterns as the rest of the framework:
- **Account Component**: Stores user identity and credentials
- **AuthToken Component**: Manages JWT sessions and refresh tokens
- **Security Components**: Additional components for profiles, roles, permissions

### 2. Handler Responsibility Separation

- **AccountHandler**: Manages account lifecycle (register, activate, deactivate)
- **AuthHandler**: Handles authentication operations (login, token refresh)
- **Future AuthSystem**: Will orchestrate complex workflows across handlers

### 3. Security First Design

- Accounts start **inactive** by default
- Passwords use BCrypt with configurable work factors
- Timing attack protection on all authentication operations
- Complete audit trail for security events

## Quick Links

- [Getting Started](/docs/getting-started/authentication/) - Begin implementing authentication
- [Implementation Guides](/docs/guides/authentication/) - Step-by-step guides
- [API Reference](/docs/api-reference/authentication/) - Complete API documentation
- [Troubleshooting](/docs/troubleshooting/authentication.md) - Common issues and solutions

---

**Deep Dive**: For the complete technical analysis, see the [Authentication Technical Whitepaper](technical-whitepaper.md)