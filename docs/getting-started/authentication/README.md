# Authentication - Getting Started

Welcome to the Jarvis authentication documentation. This guide helps you quickly implement authentication in your Jarvis applications.

## Quick Navigation

### 🚀 Start Here
- [Quick Start Guide](quick-start.md) - Get authentication working in 5 minutes
- [First Authentication](first-authentication.md) - Your first login implementation
- [Examples](examples/) - Complete working examples

### 📚 Learn More
- [Architecture Overview](/docs/architecture/authentication/README.md) - How authentication works
- [Security Model](/docs/architecture/authentication/security-model.md) - Understanding the security layers

### 🛠️ Implementation
- [Complete Implementation Guide](/docs/guides/authentication/README.md) - Step-by-step guides
- [API Integration](/docs/guides/authentication/api-integration.md) - Frontend integration patterns

## What You'll Learn

This section covers:
1. Setting up basic authentication
2. Creating your first user account
3. Implementing login/logout flows
4. Understanding tokens and sessions

## Prerequisites

Before starting, ensure you have:
- Jarvis framework installed
- PostgreSQL database configured
- Basic understanding of the [Handler Pattern](/docs/architecture/handler-pattern.md)

## Quick Example

Here's the simplest authentication flow:

```csharp
// 1. Register a user
var userEntityId = Guid.NewGuid();
var accountHandler = dataContext.For<AccountHandler>(userEntityId);

var account = await accountHandler.Register(new Account
{
    Email = "user@example.com",
    Password = "SecurePassword123!"
});

// 2. Activate the account
await accountHandler.Activate();

// 3. Authenticate
var authHandler = dataContext.For<AuthHandler>(Guid.NewGuid());
var authToken = await authHandler.Authenticate(new Account
{
    Email = "user@example.com",
    Password = "SecurePassword123!"
});

// Success! You have tokens
Console.WriteLine($"Access Token: {authToken.AccessToken}");
```

## Next Steps

Ready to get started? Jump to:
- [Quick Start Guide](quick-start.md) - Complete setup walkthrough
- [First Authentication](first-authentication.md) - Build your first auth flow
- [Examples](examples/) - See full implementations

---

**Need Help?** Check the [Troubleshooting Guide](/docs/troubleshooting/authentication.md) or [Common Issues](/docs/troubleshooting/authentication-common-issues.md)