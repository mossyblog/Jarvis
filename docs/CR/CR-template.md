# 🔧 C# Project Change Request Template

## 🧾 Metadata

- **Change Request Title**: `SHORT_SUMMARY_OF_CHANGE`
- **Author**: `Your Name or Team`
- **Date Created**: `YYYY-MM-DD`
- **Status**: `Draft | In Review | Approved | Rejected`
- **Target Branch/Environment**: `e.g., main, develop, staging`
- **Related Tickets/References**:
  - `[JIRA-123]`
  - `RFC-Change-AuthFlow.md`

---

## 🎯 Objective

**Clearly describe the purpose of the change.**  
> What functionality is being changed, added, or removed?  
> Is this a bugfix, enhancement, feature, or refactor?

---

## 📦 Scope of Change

### 1. **Affected Components/Namespaces**

List the primary components and namespaces that will be affected:
- `Namespace.ServiceLayer.Authentication`
- `Namespace.DataAccess.SqlServer`
- `Namespace.API.Controllers.AuthController`

### 2. **New Classes / Interfaces (if any)**

Specify class/interface names and brief description:
- `TokenRefresher`: Handles JWT refresh logic.
- `IAuditableEntity`: Interface to track created/modified timestamps.

### 3. **Modified Classes / Methods**

For each class or method to be changed:

<pre>
<b>// BEFORE</b>
public class AuthService {
    public string Login(string username, string password) { ... }
}

<b>// AFTER</b>
public class AuthService {
    public LoginResult Login(string username, string password, string ipAddress) { ... }
}
</pre>

---

## ✅ Acceptance Criteria

List the **business** and **technical** criteria that must be satisfied:
- [ ] Login records user IP address.
- [ ] Backward compatibility for old clients.
- [ ] Unit tests cover all added branches.
- [ ] Adheres to existing code standards and architecture.

---

## 🧪 Testing Plan

### Unit Tests
- `TestAuthService_Login_ReturnsExpectedClaims()`
- `TestAuditLogger_WritesEntry()`

### Integration Tests
- `POST /api/login` returns `200 OK` with correct token and audit entry.

### Manual Tests (If Required)
- Login with expired credentials.
- Audit logs created with accurate timestamp and IP.

---

## 📚 Data Changes

> Describe any schema changes, migrations, or seed data modifications.

- **New Tables**: `AuditLog`
- **Altered Tables**: `User` → added `LastLoginIp`
- **Migrations**: `AddAuditLogTable_2025_05_26.cs`

---

## 🔐 Security & AuthZ Impact

> Describe any new permission boundaries, roles, encryption logic, or token behavior.

- Introduces new claim `login_ip`.
- Only admins can access new `GET /audit/logs`.
- Uses `RSA256` for new token signing logic.

---

## ⏱️ Performance Considerations

> Note any impact on response time, memory usage, or concurrency behavior.

- Logging adds ~5ms latency per login request.
- Query on `AuditLog` table paginated and indexed.

---

## 🔄 Backward Compatibility

> Is anything deprecated? Does old code break?

- Maintains `Login(string, string)` overload for older clients.
- Response format versioned with `"version": "2"` header.
- No DB column drops — all non-destructive.

---

## 🧠 AI Notes for Code Generation

### 1. **Constraints**
- No hardcoded secrets or tokens.
- All public APIs must use dependency injection.
- Follow C# 10 syntax and nullable reference types (`#nullable enable`).

### 2. **Naming Conventions**
- PascalCase for classes/methods.
- `I` prefix for interfaces.
- Async methods must end with `Async`.

### 3. **Architecture Pattern**
- Enforce Clean Architecture principles.
- Use MediatR for commands/queries.
- Use Entity Framework Core with repository pattern for data access.

---

## 🔄 Rollback Strategy

> If something fails, how do we recover?

- Rollback migration `RemoveAuditLogTable`.
- Restore backup of `User` table before `LastLoginIp` was added.
- Revert PR using `git revert --no-commit <commit-range>`

---

## 📎 Related Files to Update

- [ ] `/README.md`
- [ ] `/docs/architecture.md`
- [ ] `/docs/api/AuthService.md`
- [ ] `/tests/AuthServiceTests.cs`

---

## 🧠 Final Checklist for AI Execution

- [ ] Generate modified class definitions with complete method bodies.
- [ ] Ensure all added code includes XML documentation.
- [ ] Output migration script as `.cs` file using EF Core.
- [ ] Write at least 3 unit tests and 1 integration test stub.
- [ ] Assume dependency injection is already configured.
- [ ] Don’t skip updating DTOs if any API shape changes.

---

> **NOTE**: This template is meant to be consumed both by developers and AI assistants. Each section should be fully filled to avoid assumptions and hallucinated behavior.
