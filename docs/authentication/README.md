# Jarvis Authentication Documentation

Welcome to the comprehensive authentication documentation for the Jarvis ECS framework. This documentation is organized following task-based information architecture principles for optimal developer experience.

## 🚀 Quick Start

**New to Jarvis Authentication?** Start here:
1. [Quick Start Guide](/docs/getting-started/authentication/quick-start.md) - Get authentication working in 5 minutes
2. [First Authentication](/docs/getting-started/authentication/first-authentication.md) - Build your first login flow
3. [Examples](/docs/getting-started/authentication/examples/) - Complete working examples

## 📚 Documentation Structure

Our documentation follows a task-oriented approach:

### [Getting Started](/docs/getting-started/authentication/)
Begin your authentication journey with quick tutorials and examples.
- Quick start guides
- First implementation
- Code examples

### [Architecture](/docs/architecture/authentication/)
Understand how authentication works under the hood.
- Security model and layers
- Component design
- Technical whitepaper

### [Implementation Guides](/docs/guides/authentication/)
Step-by-step guides for common authentication tasks.
- Frontend integration
- User management
- Security features
- Advanced patterns

### [API Reference](/docs/api-reference/authentication/)
Complete reference for all authentication APIs.
- Components
- Handlers
- Endpoints
- Services

### [Troubleshooting](/docs/troubleshooting/)
Solve common authentication problems quickly.
- Common issues
- Performance problems
- Debugging tools

## 🔍 Find What You Need

### By Task

**"I want to..."**

#### Add authentication to my app
- [Quick Start Guide](/docs/getting-started/authentication/quick-start.md)
- [Frontend Integration](/docs/guides/authentication/frontend-integration.md)
- [API Integration](/docs/guides/authentication/api-integration.md)

#### Manage users
- [User Registration](/docs/guides/authentication/registration-api.md)
- [User Management](/docs/guides/authentication/user-management.md)
- [Profile Management](/docs/guides/authentication/profile-management.md)

#### Enhance security
- [Two-Factor Authentication](/docs/guides/authentication/two-factor-auth.md)
- [Password Reset](/docs/guides/authentication/password-reset.md)
- [Session Management](/docs/guides/authentication/session-management.md)

#### Build dynamic UIs
- [Navigation System](/docs/guides/authentication/navigation-system.md)
- [Permission-Based UI](/docs/guides/authentication/permission-based-ui.md)
- [Role Management](/docs/guides/authentication/role-management.md)

### By Feature

#### Core Features
- **Registration** - User signup with validation
- **Authentication** - Login with JWT tokens
- **Authorization** - Role and permission based access
- **Session Management** - Secure session handling

#### Security Features
- **BCrypt Password Hashing** - Industry standard hashing
- **JWT Tokens** - Stateless authentication
- **Row-Level Security** - Database enforced isolation
- **Audit Trail** - Complete security logging

#### Advanced Features
- **Two-Factor Auth** - TOTP/SMS support
- **Social Login** - OAuth2 integration
- **Multi-Tenant** - Tenant isolation
- **Federation** - SAML/OIDC support

## 🆕 Recent Updates (v2.1.2+)

### New Features
- ✨ **Direct API→Handler Pattern** - Simplified architecture without System layer
- ✨ **Navigation System** - Dynamic menus based on permissions
- ✨ **GraphQL Support** - GraphQL endpoint infrastructure
- ✨ **Real Integration** - No mocks, direct handler usage

### Documentation Updates
- 📝 [Registration API Guide](/docs/guides/authentication/registration-api.md) - New pattern
- 📝 [Navigation System Guide](/docs/guides/authentication/navigation-system.md) - Dynamic menus
- 📝 [GraphQL Integration](/docs/guides/authentication/graphql-integration.md) - Coming soon

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────┐
│                 API Layer                        │
│              (Azure Functions)                   │
│  • RegisterFunction  • AuthFunction             │
│  • NavigationFunction • GraphQLFunction         │
└─────────────────────┬───────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────┐
│              Handler Layer                       │
│       (AccountHandler + AuthHandler)            │
│  • Direct API integration (no System layer)     │
│  • Business logic and validation               │
└─────────────────────┬───────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────┐
│             Component Layer                      │
│     (Account + AuthToken + Navigation)          │
│  • Data models and persistence                  │
└─────────────────────┬───────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────┐
│            Database Layer                        │
│         (PostgreSQL with RLS)                   │
│  • Row-level security enforcement               │
└─────────────────────────────────────────────────┘
```

## 📖 Learning Paths

### For Frontend Developers
1. [Quick Start](/docs/getting-started/authentication/quick-start.md)
2. [Frontend Integration](/docs/guides/authentication/frontend-integration.md)
3. [Token Management](/docs/guides/authentication/token-management.md)
4. [Navigation System](/docs/guides/authentication/navigation-system.md)

### For Backend Developers
1. [Architecture Overview](/docs/architecture/authentication/)
2. [Handler Implementation](/docs/guides/authentication/handler-implementation.md)
3. [Security Model](/docs/architecture/authentication/security-model.md)
4. [Testing Guide](/docs/guides/authentication/testing.md)

### For Security Engineers
1. [Security Model](/docs/architecture/authentication/security-model.md)
2. [Technical Whitepaper](/docs/architecture/authentication/technical-whitepaper.md)
3. [Security Testing](/docs/guides/authentication/security-testing.md)
4. [Compliance Guide](/docs/architecture/authentication/compliance.md)

## 🔧 Common Integration Patterns

### SPA (React/Vue/Angular)
- JWT token storage in memory/secure storage
- Automatic token refresh
- Protected routes
- Dynamic navigation

### Mobile Apps
- Secure token storage
- Biometric authentication
- Offline token validation
- Push notification auth

### Microservices
- Service-to-service auth
- API key management
- Token propagation
- Distributed sessions

## 📚 Additional Resources

### Related Documentation
- [Jarvis Overview](/docs/00_Overview/jarvis-overview.md)
- [Handler Architecture](/docs/05_Governance/system-handler-architecture.md)
- [Database Architecture](/docs/01_CurrentState/Components/datacontext-technical-whitepaper.md)

### External Resources
- [OWASP Authentication Guide](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
- [BCrypt Security](https://auth0.com/blog/hashing-in-action-understanding-bcrypt/)

### Community
- [GitHub Discussions](https://github.com/jarvis/discussions)
- [Stack Overflow](https://stackoverflow.com/questions/tagged/jarvis-framework)
- [Discord Server](https://discord.gg/jarvis)

## 🆘 Getting Help

### Quick Help
- [Troubleshooting Guide](/docs/troubleshooting/authentication.md)
- [Common Issues](/docs/troubleshooting/authentication-common-issues.md)
- [FAQ](/docs/troubleshooting/authentication-faq.md)

### Support Channels
- **GitHub Issues** - Bug reports and feature requests
- **Community Forum** - Questions and discussions
- **Enterprise Support** - Priority support for enterprise customers

---

**Version**: 2.1.2 | **Last Updated**: January 2025

**Maintainers**: Jarvis Security Team | **License**: MIT