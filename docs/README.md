# Jarvis.Data Documentation Suite

Welcome to the comprehensive documentation for Jarvis.data - a secure, convention-based PostgreSQL data access library for .NET with SDK-level Row Level Security (RLS), JWT authentication, and automatic PascalCase to snake_case mapping.

## 🚀 Quick Start

**New to Jarvis.data?** Start here:

1. 📖 **[Quick Reference Guide](jarvis-data-quick-reference.md)** - Get up and running in minutes with common patterns and examples
2. 📚 **[Complete Documentation](jarvis-data-documentation.md)** - Deep dive into all features, architecture, and usage patterns
3. 🔧 **[API Reference](jarvis-data-api-reference.md)** - Detailed technical reference and implementation guide

## 📋 Documentation Overview

### [jarvis-data-documentation.md](jarvis-data-documentation.md) 
**📚 Complete User Guide (Recommended for new users)**

Comprehensive documentation covering:
- ✅ Architecture and design principles
- ✅ Installation and setup
- ✅ Core concepts and security features  
- ✅ Usage patterns and advanced features
- ✅ Testing best practices
- ✅ Migration guide and troubleshooting

**Best for:** Developers new to the library, understanding concepts, learning best practices

### [jarvis-data-quick-reference.md](jarvis-data-quick-reference.md)
**⚡ Quick Reference Guide (Great for daily use)**

Concise reference covering:
- ✅ Common CRUD operations
- ✅ Filtering and querying patterns
- ✅ RLS policy examples
- ✅ Error handling and solutions
- ✅ Performance tips and security checklist

**Best for:** Daily development tasks, quick lookups, copy-paste examples

### [jarvis-data-api-reference.md](jarvis-data-api-reference.md)
**🔧 Technical Implementation Guide (For advanced users)**

Detailed technical reference covering:
- ✅ Class and method specifications
- ✅ Security implementation details
- ✅ String mapping algorithms
- ✅ RLS policy engine internals
- ✅ Testing patterns and implementation details

**Best for:** Understanding internals, extending the library, security reviews

## 🎯 Find What You Need

### I want to...

| Goal | Documentation | Section |
|------|---------------|---------|
| **Get started quickly** | [Quick Reference](jarvis-data-quick-reference.md) | Setup & CRUD Operations |
| **Understand the architecture** | [Complete Guide](jarvis-data-documentation.md) | Architecture & Core Concepts |
| **Set up authentication** | [Complete Guide](jarvis-data-documentation.md) | JWT-Based Authentication |
| **Configure security policies** | [Complete Guide](jarvis-data-documentation.md) | Security Features & RLS Policies |
| **Learn CRUD operations** | [Quick Reference](jarvis-data-quick-reference.md) | CRUD Operations |
| **Master filtering & querying** | [Quick Reference](jarvis-data-quick-reference.md) | Filtering |
| **Implement multi-tenancy** | [Complete Guide](jarvis-data-documentation.md) | Multi-Tenant Patterns |
| **Handle errors and troubleshoot** | [Complete Guide](jarvis-data-documentation.md) | Troubleshooting |
| **Understand security internals** | [API Reference](jarvis-data-api-reference.md) | Security Implementation |
| **Extend with custom policies** | [Complete Guide](jarvis-data-documentation.md) | Custom RLS Policies |
| **Write tests** | [API Reference](jarvis-data-api-reference.md) | Testing Patterns |
| **Optimize performance** | [Quick Reference](jarvis-data-quick-reference.md) | Performance Tips |
| **Migrate from v1.x** | [Complete Guide](jarvis-data-documentation.md) | Migration Guide |

### By Role

**🆕 New Developer**
1. [Quick Reference](jarvis-data-quick-reference.md) - Setup & Basic Operations
2. [Complete Guide](jarvis-data-documentation.md) - Core Concepts & Security

**👨‍💻 Application Developer**
1. [Quick Reference](jarvis-data-quick-reference.md) - Daily development patterns
2. [Complete Guide](jarvis-data-documentation.md) - Advanced features & best practices

**🔒 Security Engineer**
1. [API Reference](jarvis-data-api-reference.md) - Security Implementation
2. [Complete Guide](jarvis-data-documentation.md) - Security Features & RLS Policies

**🏗️ Platform Engineer**
1. [Complete Guide](jarvis-data-documentation.md) - Architecture & Installation
2. [API Reference](jarvis-data-api-reference.md) - Implementation Details

**🧪 QA Engineer**
1. [API Reference](jarvis-data-api-reference.md) - Testing Patterns
2. [Quick Reference](jarvis-data-quick-reference.md) - Common Issues & Solutions

## 🌟 Key Features

### 🔐 SDK-Level Row Level Security (RLS)
- Security policies enforced within the SDK, not the database
- Works with any database user - no special permissions required
- Complete control over data access logic
- Easy testing and debugging

### 🎫 JWT-Based Authentication
- Built-in JWT parsing and claim extraction
- Automatic propagation to RLS policies
- Support for standard and custom claims
- Seamless integration with existing auth systems

### 🐍 Automatic snake_case Mapping
- PascalCase C# properties → snake_case PostgreSQL columns
- Handles complex cases: `XMLData` → `xml_data`, `CustomerID` → `customer_id`
- Write idiomatic C# without database naming concerns

### 🏢 Multi-Tenant Data Isolation
- Complete data separation between tenants
- Automatic tenant scoping with JWT claims
- Built-in policies for common multi-tenant scenarios

### 🛡️ Security First Design
- Multiple layers of SQL injection protection
- Column and operator whitelisting
- Parameterized queries throughout
- Default deny security principle

### ⚡ Type-Safe Operations
- Strongly-typed table interfaces
- Compile-time safety and IntelliSense support
- Automatic column validation

## 🚦 Getting Started

### 1. Installation
```xml
<PackageReference Include="core.jarvis.data" Version="2.0.0" />
```

### 2. Basic Setup
```csharp
var conn = new NpgsqlConnection(connectionString);
var client = await PgClientFactory.Create(conn);
```

### 3. Authentication
```csharp
var jwt = await client.Authenticate("user@example.com", "password");
client.JWT(jwt);
```

### 4. Secure Data Access
```csharp
// All operations automatically secured by RLS policies
var products = await client.From<Product>()
    .Filter("category", "eq", "Electronics")
    .Filter("price", "gte", 100)
    .Get();
```

**➡️ Continue with the [Quick Reference Guide](jarvis-data-quick-reference.md) for more examples!**

## 📊 Security & Compliance

Jarvis.data provides enterprise-grade security features:

- **✅ GDPR Compliant**: Multi-tenant data isolation prevents data leakage
- **✅ SOC 2 Ready**: Comprehensive audit trails and access controls
- **✅ Zero Trust**: Every operation validated against JWT claims and policies
- **✅ SQL Injection Protection**: Multiple layers of protection
- **✅ Principle of Least Privilege**: Default deny with explicit allow policies

## 🤝 Contributing

We welcome contributions! Please see the contributing guidelines in the main repository.

### Development Setup
1. Clone the repository
2. Set up PostgreSQL database
3. Configure connection string in `.env.local`
4. Run tests: `dotnet test`

## 📞 Support

- **📖 Documentation**: You're here! Start with the [Quick Reference](jarvis-data-quick-reference.md)
- **🐛 Issues**: Report bugs and feature requests in the main repository
- **💬 Discussions**: Join community discussions for questions and ideas

## 📝 License

[Your License Here]

---

**Documentation Version**: 2.0.0  
**Library Version**: 2.0.0  
**Last Updated**: [Current Date]

**Happy coding with Jarvis.data! 🚀**