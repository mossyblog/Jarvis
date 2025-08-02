# Changelog

## 2.1.4

### Fixed
- **UIStudio ECS Pattern Violations**: Critical architectural fix to enforce proper Entity Component System patterns
  - **REMOVED** `UIStudioPage.LayoutEntityId` - eliminated direct component-to-component references
  - **REPLACED** `UIStudioComponentBinding.PageEntityId` with `PageSlug` - components now use business keys instead of entity references
  - **UPDATED** `UIStudioSystem` to use `LinkRelationship()` for proper parent-child entity relationships
  - **MIGRATED** all handlers from entity reference queries to `Children()` and business key patterns
  - **ENFORCED** SOLID/SRP principles - components are now completely independent with no inter-component coupling

### Changed
- **UIStudio API Architecture**: Updated all API endpoints to work with new ECS-compliant patterns
  - `UIStudioComponentBindingHandler.GetByPageSlug()` replaces entity-based page queries
  - `UIStudioFunction.CreateComponentBindings()` now uses `LinkRelationship` for page-binding associations
  - `UIStudioQueryFunction.GetPageBindings()` queries by PageSlug instead of entity references
  - `UIStudioVersionFunction.AddPageSnapshotData()` uses `Children()` for related component discovery
  - All Functions updated to maintain API compatibility while using proper ECS patterns internally

### Architecture
- **Entity Component System Compliance**: Full adherence to ECS architectural principles
  - **Entities**: Pure identifiers (Guid) with no business logic
  - **Components**: Independent data structures with no references to other entities
  - **Systems**: Orchestrate workflows using `LinkRelationship` for entity associations
  - **Handlers**: Manage single component types following Single Responsibility Principle
  - **Link Relationships**: Parent-child entity relationships replace direct component references

## 2.1.3

### Added
- **Registration API**: Implemented proper API→Handler pattern for user registration
  - `RegisterFunction` follows thin API layer pattern accepting only IComponent
  - `AccountHandler.Register()` method handles all business logic including BCrypt password hashing
  - Users created as inactive by default, requiring manual activation for security
  - Fixed ValidationException handling with proper Dictionary<string, string[]> format
  
- **Navigation System**: Complete ECS-based navigation menu implementation
  - `NavigationItem` component model with menu metadata (icon, href, sort order, permissions)
  - `NavigationHandler` for navigation business logic following single responsibility principle
  - Navigation items stored in `navigation_item_component` table with automatic creation
  - Permission-based navigation filtering integrated with security profiles
  
- **GraphQL Infrastructure**: Prepared foundation for direct UI→GraphQL→PostgreSQL architecture
  - `GraphQLService` for TypeScript/React to execute GraphQL queries
  - `GraphQLFunction` as temporary bridge endpoint until direct PostgreSQL GraphQL access
  - GraphQL queries for navigation with permission filtering and RLS support
  - JWT token extraction and user context propagation to GraphQL

### Changed
- **Authentication Flow**: Fixed authentication to work with real API backend
  - Removed mock authentication in favor of real JWT-based authentication
  - Fixed login form credentials to match actual test user (TestPassword123!)
  - Added activation step requirement between registration and authentication
  - Proven working with definitive curl testing (1/0 proof)

- **API Service**: Migrated from mock data to real API integration
  - Updated `apiService.ts` to fetch navigation from `/api/security/navigation`
  - Proper error handling and data transformation for API responses
  - JWT token management with automatic refresh scheduling
  - Real-time navigation loading based on user permissions

### Fixed
- **Database Consistency**: Consolidated to single jarvis_test database
  - Cleaned up multiple database references (jarvis, jarvis_graphql, etc.)
  - All components now use consistent jarvis_test database
  - Fixed navigation_item_component table creation through ECS auto-schema

- **Compilation Errors**: Resolved multiple API project build issues
  - Added missing `using core.jarvis.api.Middleware` for extension methods
  - Fixed `GetUserNavigation` method not found by using profile retrieval pattern
  - Resolved `GetOrDefault()` accessibility issues with proper try-catch approach

### Architecture
- **ECS Pattern Enforcement**: Strict adherence to API→Handler→ECS architecture
  - API layer (Functions) remain thin, only accepting GUID or IComponent
  - Handlers contain all business logic and component operations
  - Systems orchestrate workflows across multiple handlers
  - GraphQL positioned as direct data access layer bypassing Functions

## 2.1.2

### Fixed
- **Registration System Transaction Handling**: Fixed PostgreSQL transaction abort errors
  - Added defensive try-catch around `SecurityAuditService.LogSuccessfulAuthentication`
  - Modified `AccountProfileHandler.CreateWithDefaults` to accept optional fullName parameter
  - Eliminated separate profile update operations within registration transaction
  - Prevents "current transaction is aborted" errors during user registration
- **Audit Service Reliability**: Enhanced audit logging robustness
  - Added retry logic with exponential backoff for `LogChange` method
  - Improved error handling in test environments while maintaining production behavior
  - Fixed special character handling in audit metadata with proper JSON escaping

### Enhanced
- **Snapshot System**: Strengthened snapshot creation and audit functionality
  - Enhanced `DataContext.cs` snapshot logic with comprehensive logging
  - Improved error handling - failures are audited instead of throwing
  - Added retry logic when retrieving existing snapshots for updates
- **Connection Pooling**: Improved connection handling in stress scenarios
  - Added exponential backoff retry logic for transient failures
  - Better verification of component existence before operations
  - Enhanced error recovery mechanisms

## 2.1.1

### Fixed
- **PostgreSQL Duplicate Key Constraint**: Fixed race condition during concurrent schema creation
  - Implemented PostgreSQL advisory locks in `PgClientFactory.EnsureMinimumSchema`
  - Prevents "duplicate key value violates unique constraint pg_type_typname_nsp_index" errors
  - Ensures safe concurrent initialization of database schema
- **PostgreSQL Column Naming**: Fixed incorrect column reference in RLS status check
  - Changed "row_security" to correct PostgreSQL system column "rowsecurity"
  - Resolves "column row_security does not exist" error when checking RLS status
  - Properly queries `pg_tables.rowsecurity` for table RLS configuration

### Documentation
- **Windows Development Environment**: Added PowerShell development setup in CLAUDE.md
  - Instructions for configuring .env.local on Windows
  - PowerShell script examples for environment variable setup
  - Cross-platform development support documentation

## 2.1.0

### Added
- **Optional Component Versioning**: New `IVersionedComponent` interface for opt-in version control
  - Components can now selectively implement versioning for snapshot tracking and optimistic concurrency
  - `Version` property automatically increments on updates for versioned components  
  - Non-versioned components (like `Account`) work without version column requirements
- **Schema Management System**: Automatic table validation and creation
  - `ITableManager` interface for component table schema management
  - `PostgreSqlTableManager` implementation with automatic field detection and creation
  - Schema validation prevents incompatible data type changes
- **Enhanced Snapshot System**: Improved snapshot capture for audit trails
  - UPDATE snapshots now correctly capture state BEFORE changes for compliance
  - CREATE snapshots capture initial component state after insertion
  - Snapshots are only created for components implementing `IVersionedComponent`

### Changed
- **DataContext Database Strategy**: Different approaches for versioned vs non-versioned components
  - Versioned components: Use `Upsert()` with full version column support
  - Non-versioned components: Use separate `Insert()` and `Update()` calls to avoid version column issues
  - Existing component queries only performed for versioned components
- **Property Name Consistency**: Fixed property naming across snapshot components
  - `ComponentSnapshots.UpdatedAt` → `ComponentSnapshots.LastUpdated`
  - Aligns with standard `IComponent.LastUpdated` naming convention
- **Test Expectations**: Updated snapshot test assertions for correct behavior
  - Tests now properly validate that UPDATE snapshots capture pre-change state
  - Enhanced test documentation for snapshot capture timing

### Fixed
- **Version Column Compatibility**: Resolved `column "version" does not exist` error
  - Non-versioned components (like Account, SecurityProfile) no longer attempt version column access
  - Database operations properly handle mixed versioned/non-versioned component scenarios
- **Snapshot Test Logic**: Fixed failing test expectations for snapshot capture timing
  - `Commit_Should_Capture_Previous_State_On_Update` now correctly validates pre-update state capture
  - All 3 snapshot creation tests pass with proper state validation

### Architecture
- **Selective Versioning**: Clear separation between versioned and non-versioned components
  - Maintains backward compatibility for existing non-versioned components
  - Provides full versioning features only where needed for audit requirements
  - Follows principle of least surprise - components work as expected without mandatory versioning

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