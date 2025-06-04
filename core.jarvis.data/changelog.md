# Changelog

## 2.0.0

### Added
- **SDK-Level Row Level Security (RLS)**: Complete RLS enforcement within the SDK, independent of database permissions
  - `RLSPolicy` class for defining table-specific access policies
  - `RLSPolicyRegistry` for managing and applying policies
  - Default policies for multi-tenant isolation, user-level security, and role-based access
- **Enhanced JWT Support**: Automatic JWT claim extraction and propagation to RLS policies
  - Support for standard claims (sub, tenant_id, role) and custom claims
  - Claims used in WHERE clause generation and operation validation
- **Improved PascalCase to snake_case Mapping**: Enhanced algorithm handles complex cases
  - Acronyms: `XMLContent` → `xml_content`, `APIKey` → `api_key`
  - ID suffixes: `CustomerID` → `customer_id`
  - Boolean prefixes: `IsActive` → `is_active`, `HasChildren` → `has_children`
- **Comprehensive Test Suite**: Added 23 new tests for RLS scenarios
  - Multi-tenant data isolation tests
  - User-level security with public/private data tests
  - Role-based access control tests
  - JWT claim propagation tests

### Changed
- `PgClient` constructor now accepts optional `RLSPolicyRegistry` for custom policies
- `PgTable<T>` now enforces RLS policies before executing queries
- Tables without RLS policies continue to work normally (backward compatible)
- `StringExtensions.ToSnakeCase` rewritten to handle all edge cases correctly
- **Idiomatic method naming**: Removed `Async` suffix from methods
  - `PgClientFactory.CreateAsync()` → `PgClientFactory.Create()`
  - `EnsureMinimumSchemaAsync()` → `EnsureMinimumSchema()`
  - `SetJWTClaimsAsync()` → `SetJWTClaims()`
  - All internal async methods follow the same pattern

### Security
- **SDK-Level RLS**: Security policies enforced before queries reach the database
- **Default Deny**: Tables with policies deny access unless explicitly allowed
- **Claim-Based Access**: All access decisions based on JWT claims
- **SQL Injection Prevention**: Enhanced with claim value escaping

## 1.0.0

### Added
- `PgClient` class: Secure, convention-based PostgreSQL client with JWT-based RLS support, authentication, and table access.
- `PgTable<T>` class: Strongly-typed, secure table interface with snake_case mapping, parameterized queries, and column/operator whitelisting.
- `StringExtensions.ToSnakeCase`: Extension method for converting PascalCase/camelCase to snake_case for all entity/table mapping.
- Integration with `BCrypt.Net-Next` for secure password hashing and verification.
- Full XML documentation for all public methods and classes.
- Test coverage for CRUD, mapping, and security scenarios.
- `.env.local` support for test configuration.

### Changed
- Refactored test structure to follow feature/domain-based directories and guidelines.
- Improved security: parameterized all SQL, whitelisted columns/operators, and sanitized JWT handling.

### Fixed
- Resolved duplicate/ambiguous extension method issues.
- Fixed connection state handling and ensured robust session management.

### Security
- Enforced RBAC/RLS at the database level, with JWT session variable support.
- Prevented SQL injection via strict validation and parameterization.

---

For earlier changes, see project history or previous release notes.