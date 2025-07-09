# Core.Jarvis.API Endpoints Summary

This document lists all HTTP endpoints found in the core.jarvis.api/Functions directory with their HttpTrigger attributes.

## Authentication Endpoints

### AuthFunction.cs
- **POST /api/security/auth** - Authenticate user with email/password, returns JWT tokens
  - Authorization: Anonymous
  - Function: auth
  - Description: Authenticates a user and returns access/refresh tokens

- **POST /api/security/refresh** - Refresh access token using refresh token
  - Authorization: Anonymous
  - Function: RefreshToken
  - Description: Refreshes expired access tokens

- **POST /api/security/validate** - Validate JWT token
  - Authorization: Anonymous
  - Function: ValidateToken
  - Description: Validates if a token is valid and not expired

### RegisterFunction.cs
- **POST /api/auth/register** - Register new user
  - Authorization: Anonymous
  - Function: RegisterUser
  - Description: Creates a new user account

### DeauthFunction.cs
- **POST /api/security/deauth** - Deauthenticate/logout user
  - Authorization: Function
  - Function: deauth
  - Description: Invalidates user session

### RefreshFunction.cs
- **POST /api/security/refresh** - Refresh token endpoint (duplicate route)
  - Authorization: Function
  - Function: refresh
  - Description: Alternative refresh token implementation

### ValidateFunction.cs
- **GET /api/security/validate** - Validate token via GET
  - Authorization: Anonymous
  - Function: validate
  - Description: GET endpoint for token validation

## Account Management

### AccountFunction.cs
- **GET /api/accounts/me** - Get current user profile
  - Authorization: Function
  - Function: GetCurrentUser
  - Description: Returns authenticated user's profile information

- **GET /api/accounts/navigation** - Get user navigation items
  - Authorization: Function
  - Function: GetUserNavigation
  - Description: Returns navigation items user has access to

- **PUT /api/accounts/me** - Update user profile
  - Authorization: Function
  - Function: UpdateUserProfile
  - Description: Updates authenticated user's profile

## Role Management

### RoleFunction.cs
- **GET /api/security/roles** - Get all roles
  - Authorization: Function
  - Function: GetRoles
  - Description: Lists all available roles

- **POST /api/security/roles** - Create new role
  - Authorization: Function
  - Function: CreateRole
  - Description: Creates a new role

- **PUT /api/security/roles/{roleId}** - Update existing role
  - Authorization: Function
  - Function: UpdateRole
  - Description: Updates role by ID

- **POST /api/security/roles/ensure-defaults** - Ensure default roles exist
  - Authorization: Function
  - Function: EnsureDefaultRoles
  - Description: Creates default system roles if missing

### RoleFunctionExample.cs (Example with Permissions)
- **GET /api/security/roles/example** - Get roles (requires admin.roles.read)
  - Authorization: Function
  - Function: GetRolesExample
  - RequirePermission: admin.roles.read

- **POST /api/security/roles/example** - Create role (requires admin.roles.write)
  - Authorization: Function
  - Function: CreateRoleExample
  - RequirePermission: admin.roles.write

- **PUT /api/security/roles/example/{roleId}** - Update role (requires admin.roles.write)
  - Authorization: Function
  - Function: UpdateRoleExample
  - RequirePermission: admin.roles.write

- **DELETE /api/security/roles/example/{roleId}** - Delete role (requires admin.roles.delete)
  - Authorization: Function
  - Function: DeleteRoleExample
  - RequirePermission: admin.roles.delete

- **GET /api/security/roles/example/public** - Get roles with OR permission
  - Authorization: Function
  - Function: GetRolesWithOrPermission
  - RequirePermission: roles.read (with OR operator)

## Navigation Management

### NavigationFunction.cs
- **POST /api/security/navigation/ensure-defaults** - Ensure default navigation items
  - Authorization: Function
  - Function: EnsureDefaultNavigation
  - Description: Creates default navigation structure

- **GET /api/security/navigation** - Get all navigation items
  - Authorization: Function
  - Function: GetNavigationItems
  - Description: Returns all navigation items

- **POST /api/security/navigation** - Create navigation item
  - Authorization: Function
  - Function: CreateNavigationItem
  - Description: Creates new navigation item

## Swagger/OpenAPI

### SwaggerFunctions.cs
- **GET /api/swagger/ui** - Swagger UI interface
  - Authorization: Anonymous
  - OpenApiIgnore: true
  - Description: Renders Swagger UI

- **GET /api/swagger.{extension}** - OpenAPI document
  - Authorization: Anonymous
  - OpenApiIgnore: true
  - Description: Returns OpenAPI spec in JSON or YAML format

## Summary Statistics

- Total Endpoints: 23
- Anonymous Access: 7
- Function Authorization: 16
- Security Domain: 18
- Account Domain: 3
- Swagger: 2

## Key Observations

1. Most endpoints use `AuthorizationLevel.Function` requiring API key authentication
2. Only auth/register/validate endpoints allow anonymous access
3. All security endpoints follow RESTful conventions
4. RoleFunctionExample demonstrates permission-based authorization using RequirePermission attribute
5. Duplicate refresh endpoints exist (need consolidation)
6. All endpoints accept/return IComponent objects following the Jarvis pattern