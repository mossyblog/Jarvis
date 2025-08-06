# Authentication, RBAC, and RLS in Jarvis: Technical Whitepaper

## Abstract

This whitepaper provides a comprehensive technical analysis of the multi-layered security architecture in the Jarvis framework. We examine how authentication flows from the application level through API endpoints, how Role-Based Access Control (RBAC) governs functionality access, and how Row-Level Security (RLS) in PostgreSQL ensures data isolation. The integration of JWT tokens, handler-based authorization, and database-level security policies creates a defense-in-depth approach that protects both operations and data.

## Table of Contents

1. [Introduction](#introduction)
2. [Security Architecture Overview](#security-architecture-overview)
3. [Authentication Flow](#authentication-flow)
4. [JWT Token Management](#jwt-token-management)
5. [Role-Based Access Control (RBAC)](#role-based-access-control-rbac)
6. [Row-Level Security (RLS)](#row-level-security-rls)
7. [Component Security Model](#component-security-model)
8. [Handler Authorization](#handler-authorization)
9. [API Security Layer](#api-security-layer)
10. [Database Security Policies](#database-security-policies)
11. [Session Management](#session-management)
12. [Security Audit Trail](#security-audit-trail)
13. [Testing Security](#testing-security)
14. [Common Attack Vectors](#common-attack-vectors)
15. [Best Practices](#best-practices)
16. [Conclusion](#conclusion)

## Introduction

Security in Jarvis is implemented through multiple layers, each providing specific protections:

1. **Application Level**: JWT-based authentication and session management
2. **API Level**: Azure Functions authorization and input validation
3. **System Level**: Role-based access control and operation authorization
4. **Handler Level**: Entity ownership and permission verification
5. **Database Level**: Row-level security policies enforced by PostgreSQL

This multi-layered approach ensures that even if one layer is compromised, others continue to protect the system.

## Security Architecture Overview

### Layered Security Model

```
┌─────────────────────────────────────────────────┐
│                Client Application                │
│            (Web, Mobile, Desktop)                │
└─────────────────────┬───────────────────────────┘
                      │ HTTPS + JWT
┌─────────────────────▼───────────────────────────┐
│              API Gateway Layer                   │
│         (Azure Functions + Auth)                 │
│  • JWT Validation  • Rate Limiting              │
│  • CORS Policy     • Input Validation           │
└─────────────────────┬───────────────────────────┘
                      │ Authenticated Context
┌─────────────────────▼───────────────────────────┐
│               System Layer                       │
│  • RBAC Enforcement • Operation Authorization   │
│  • Audit Logging    • Transaction Boundaries    │
└─────────────────────┬───────────────────────────┘
                      │ Authorized Operations
┌─────────────────────▼───────────────────────────┐
│              Handler Layer                       │
│  • Entity Ownership • Permission Checks         │
│  • Business Rules   • Data Validation           │
└─────────────────────┬───────────────────────────┘
                      │ Validated Commands
┌─────────────────────▼───────────────────────────┐
│            Database Layer (PostgreSQL)           │
│  • Row-Level Security  • Column Encryption      │
│  • Audit Triggers      • Access Policies        │
└─────────────────────────────────────────────────┘
```

### Security Components

```csharp
// Core security components in Jarvis
public record Account : IComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; init; }
    public string Email { get; init; }
    public string PasswordHash { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}

public record SecurityProfile : IComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; init; } // Links to Account
    public bool TwoFactorEnabled { get; init; }
    public string TwoFactorSecret { get; init; }
    public int FailedLoginAttempts { get; init; }
    public DateTime? LockedUntil { get; init; }
}

public record Role : IComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public List<Permission> Permissions { get; init; }
}

public record AuthToken : IComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; init; } // Links to Account
    public string TokenHash { get; init; }
    public string TokenType { get; init; } // Access, Refresh
    public DateTime ExpiresAt { get; init; }
    public bool IsRevoked { get; init; }
}
```

## Authentication Flow

### 1. Registration Flow

```csharp
public class RegistrationSystem : SystemBase
{
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    
    public async Task<RegistrationResult> RegisterAccount(RegistrationRequest request)
    {
        // 1. Validate request
        await ValidateRegistrationRequest(request);
        
        // 2. Check for existing account
        var existing = await DataContext.CreateQuery()
            .WithComponent<Account>()
            .Where<Account>(a => a.Email == request.Email)
            .ExecuteAsync();
            
        if (existing.Any())
        {
            throw new ConflictException("Account already exists");
        }
        
        // 3. Create account entity and components
        var accountId = Guid.NewGuid();
        
        return await DataContext.InTransaction(async tx =>
        {
            // Create account
            var accountHandler = tx.For<AccountHandler>(accountId);
            var passwordHash = await _passwordService.HashPassword(request.Password);
            await accountHandler.CreateAccount(request.Email, passwordHash);
            
            // Create security profile
            var securityHandler = tx.For<SecurityProfileHandler>(accountId);
            await securityHandler.Initialize();
            
            // Assign default role
            var roleHandler = tx.For<RoleHandler>(accountId);
            await roleHandler.AssignRole("User");
            
            // Generate verification token
            var verificationToken = await _tokenService.GenerateVerificationToken(accountId);
            
            // Audit registration
            var auditHandler = tx.For<SecurityAuditHandler>(accountId);
            await auditHandler.LogRegistration(request.Email, request.IpAddress);
            
            return new RegistrationResult
            {
                AccountId = accountId,
                VerificationToken = verificationToken,
                RequiresEmailVerification = true
            };
        });
    }
}
```

### 2. Login Flow

```csharp
public class AuthenticationSystem : SystemBase
{
    private readonly ITokenService _tokenService;
    private readonly IPasswordService _passwordService;
    private readonly IConstantTimeService _constantTime;
    
    public async Task<LoginResult> Login(LoginRequest request)
    {
        // 1. Find account (constant time to prevent timing attacks)
        var accounts = await DataContext.CreateQuery()
            .WithComponent<Account>()
            .Where<Account>(a => a.Email == request.Email)
            .ExecuteAsync();
        
        if (!accounts.Any())
        {
            // Perform dummy password hash to maintain constant time
            await _constantTime.DummyPasswordHash();
            throw new AuthenticationException("Invalid credentials");
        }
        
        var accountId = accounts.First();
        var accountHandler = DataContext.For<AccountHandler>(accountId);
        var account = await accountHandler.Get();
        
        // 2. Check account status
        if (!account.IsActive)
        {
            throw new AuthenticationException("Account is disabled");
        }
        
        // 3. Check lockout
        var securityHandler = DataContext.For<SecurityProfileHandler>(accountId);
        var securityProfile = await securityHandler.Get();
        
        if (securityProfile.LockedUntil > DateTime.UtcNow)
        {
            throw new AccountLockedException(
                $"Account locked until {securityProfile.LockedUntil}");
        }
        
        // 4. Verify password
        var isValidPassword = await _passwordService.VerifyPassword(
            request.Password, 
            account.PasswordHash);
            
        if (!isValidPassword)
        {
            await securityHandler.RecordFailedLogin();
            
            // Check if we should lock the account
            if (securityProfile.FailedLoginAttempts >= 5)
            {
                await securityHandler.LockAccount(TimeSpan.FromMinutes(15));
                throw new AccountLockedException("Too many failed attempts");
            }
            
            throw new AuthenticationException("Invalid credentials");
        }
        
        // 5. Check 2FA if enabled
        if (securityProfile.TwoFactorEnabled)
        {
            if (string.IsNullOrEmpty(request.TwoFactorCode))
            {
                return new LoginResult
                {
                    RequiresTwoFactor = true,
                    TwoFactorMethod = "TOTP"
                };
            }
            
            var isValid2FA = await securityHandler.Verify2FACode(request.TwoFactorCode);
            if (!isValid2FA)
            {
                throw new AuthenticationException("Invalid 2FA code");
            }
        }
        
        // 6. Generate tokens
        var tokens = await _tokenService.GenerateTokenPair(accountId, account.Email);
        
        // 7. Update login info
        await accountHandler.UpdateLastLogin();
        await securityHandler.ClearFailedAttempts();
        
        // 8. Store refresh token
        var tokenHandler = DataContext.For<AuthTokenHandler>(Guid.NewGuid());
        await tokenHandler.StoreRefreshToken(
            accountId, 
            tokens.RefreshToken,
            TimeSpan.FromDays(30));
        
        // 9. Audit successful login
        var auditHandler = DataContext.For<SecurityAuditHandler>(accountId);
        await auditHandler.LogSuccessfulLogin(request.IpAddress, request.UserAgent);
        
        return new LoginResult
        {
            Success = true,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresIn = tokens.ExpiresIn
        };
    }
}
```

### 3. Token Refresh Flow

```csharp
public class TokenRefreshSystem : SystemBase
{
    public async Task<TokenRefreshResult> RefreshToken(string refreshToken)
    {
        // 1. Validate refresh token format
        var tokenClaims = await _tokenService.ValidateRefreshToken(refreshToken);
        if (tokenClaims == null)
        {
            throw new AuthenticationException("Invalid refresh token");
        }
        
        var accountId = Guid.Parse(tokenClaims.Subject);
        
        // 2. Check if token exists and is valid
        var tokenHash = _tokenService.HashToken(refreshToken);
        var storedTokens = await DataContext.CreateQuery()
            .WithComponent<AuthToken>()
            .Where<AuthToken>(t => t.TokenHash == tokenHash)
            .Where<AuthToken>(t => t.TokenType == "Refresh")
            .Where<AuthToken>(t => !t.IsRevoked)
            .ExecuteAsync();
            
        if (!storedTokens.Any())
        {
            throw new AuthenticationException("Refresh token not found or revoked");
        }
        
        var tokenId = storedTokens.First();
        var tokenHandler = DataContext.For<AuthTokenHandler>(tokenId);
        var storedToken = await tokenHandler.Get();
        
        // 3. Check expiration
        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            await tokenHandler.RevokeToken();
            throw new AuthenticationException("Refresh token expired");
        }
        
        // 4. Get account info
        var accountHandler = DataContext.For<AccountHandler>(accountId);
        var account = await accountHandler.Get();
        
        if (!account.IsActive)
        {
            await tokenHandler.RevokeToken();
            throw new AuthenticationException("Account is disabled");
        }
        
        // 5. Generate new token pair
        var newTokens = await _tokenService.GenerateTokenPair(accountId, account.Email);
        
        // 6. Revoke old refresh token
        await tokenHandler.RevokeToken();
        
        // 7. Store new refresh token
        var newTokenHandler = DataContext.For<AuthTokenHandler>(Guid.NewGuid());
        await newTokenHandler.StoreRefreshToken(
            accountId,
            newTokens.RefreshToken,
            TimeSpan.FromDays(30));
        
        return new TokenRefreshResult
        {
            AccessToken = newTokens.AccessToken,
            RefreshToken = newTokens.RefreshToken,
            ExpiresIn = newTokens.ExpiresIn
        };
    }
}
```

## JWT Token Management

### Token Service Implementation

```csharp
public class TokenService : ITokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _secretKey;
    private readonly int _accessTokenExpirationMinutes;
    
    public async Task<TokenPair> GenerateTokenPair(Guid accountId, string email)
    {
        var roles = await GetUserRoles(accountId);
        var permissions = await GetUserPermissions(accountId);
        
        // Access Token Claims
        var accessClaims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, 
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), 
                ClaimValueTypes.Integer64),
            new Claim("roles", JsonSerializer.Serialize(roles)),
            new Claim("permissions", JsonSerializer.Serialize(permissions))
        };
        
        // Create access token
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var accessExpires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);
        
        var accessToken = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: accessClaims,
            expires: accessExpires,
            signingCredentials: creds);
        
        // Create refresh token (longer lived, minimal claims)
        var refreshClaims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("token_type", "refresh")
        };
        
        var refreshExpires = DateTime.UtcNow.AddDays(30);
        var refreshToken = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: refreshClaims,
            expires: refreshExpires,
            signingCredentials: creds);
        
        return new TokenPair
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(accessToken),
            RefreshToken = new JwtSecurityTokenHandler().WriteToken(refreshToken),
            ExpiresIn = (int)(accessExpires - DateTime.UtcNow).TotalSeconds
        };
    }
}
```

### Token Validation Middleware

```csharp
public class AuthorizationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ITokenService _tokenService;
    
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext == null)
        {
            await next(context);
            return;
        }
        
        // Skip auth for specific endpoints
        if (IsPublicEndpoint(httpContext.Request.Path))
        {
            await next(context);
            return;
        }
        
        try
        {
            // Extract token from Authorization header
            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                httpContext.Response.StatusCode = 401;
                await httpContext.Response.WriteAsync("Missing or invalid authorization header");
                return;
            }
            
            var token = authHeader.Substring("Bearer ".Length).Trim();
            
            // Validate token
            var principal = await _tokenService.ValidateAccessToken(token);
            if (principal == null)
            {
                httpContext.Response.StatusCode = 401;
                await httpContext.Response.WriteAsync("Invalid token");
                return;
            }
            
            // Set user context
            context.Items["UserId"] = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            context.Items["Email"] = principal.FindFirst(ClaimTypes.Email)?.Value;
            context.Items["Roles"] = JsonSerializer.Deserialize<List<string>>(
                principal.FindFirst("roles")?.Value ?? "[]");
            context.Items["Permissions"] = JsonSerializer.Deserialize<List<string>>(
                principal.FindFirst("permissions")?.Value ?? "[]");
            
            // Continue execution
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed");
            httpContext.Response.StatusCode = 401;
            await httpContext.Response.WriteAsync("Authentication failed");
        }
    }
}
```

## Role-Based Access Control (RBAC)

### Role and Permission Model

```csharp
public class RoleHandler : ComponentHandler<Role>
{
    public async Task AssignRole(string roleName)
    {
        var role = await GetRoleByName(roleName);
        if (role == null)
        {
            throw new NotFoundException($"Role '{roleName}' not found");
        }
        
        var currentRoles = await Get() ?? new Role 
        { 
            OwnerEntityId = EntityId,
            Permissions = new List<Permission>()
        };
        
        // Add role permissions
        var updatedPermissions = currentRoles.Permissions
            .Union(role.Permissions)
            .Distinct()
            .ToList();
        
        await Update(currentRoles with 
        { 
            Name = roleName,
            Permissions = updatedPermissions 
        });
        
        // Audit role assignment
        await DataContext.For<SecurityAuditHandler>(EntityId)
            .LogRoleAssignment(roleName);
    }
    
    public async Task<bool> HasPermission(string permission)
    {
        var role = await Get();
        if (role == null) return false;
        
        return role.Permissions.Any(p => 
            p.Name == permission || 
            p.Name == "*" || // Wildcard permission
            permission.StartsWith(p.Name + ".") // Hierarchical permission
        );
    }
}
```

### System-Level Authorization

```csharp
public abstract class SecureSystemBase : SystemBase
{
    protected async Task RequirePermission(string permission)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            throw new UnauthorizedException("User not authenticated");
        }
        
        var roleHandler = DataContext.For<RoleHandler>(userId.Value);
        if (!await roleHandler.HasPermission(permission))
        {
            Logger.LogWarning(
                "Permission denied: User {UserId} lacks permission {Permission}",
                userId, permission);
            
            throw new ForbiddenException($"Permission required: {permission}");
        }
    }
    
    protected async Task RequireAnyPermission(params string[] permissions)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            throw new UnauthorizedException("User not authenticated");
        }
        
        var roleHandler = DataContext.For<RoleHandler>(userId.Value);
        
        foreach (var permission in permissions)
        {
            if (await roleHandler.HasPermission(permission))
            {
                return; // User has at least one required permission
            }
        }
        
        throw new ForbiddenException(
            $"One of these permissions required: {string.Join(", ", permissions)}");
    }
}

// Usage in systems
public class AdminSystem : SecureSystemBase
{
    public async Task<List<UserInfo>> GetAllUsers()
    {
        await RequirePermission("admin.users.read");
        
        // Implementation
    }
    
    public async Task SuspendUser(Guid userId)
    {
        await RequirePermission("admin.users.suspend");
        
        // Implementation
    }
}
```

### Permission Hierarchy

```csharp
public static class Permissions
{
    // Hierarchical permission structure
    public const string Admin = "admin";
    public const string AdminUsers = "admin.users";
    public const string AdminUsersRead = "admin.users.read";
    public const string AdminUsersWrite = "admin.users.write";
    public const string AdminUsersDelete = "admin.users.delete";
    
    public const string Orders = "orders";
    public const string OrdersRead = "orders.read";
    public const string OrdersWrite = "orders.write";
    public const string OrdersApprove = "orders.approve";
    public const string OrdersCancel = "orders.cancel";
    
    // Permission checking with hierarchy
    public static bool IsPermissionGranted(string requiredPermission, List<string> userPermissions)
    {
        // Direct match
        if (userPermissions.Contains(requiredPermission))
            return true;
        
        // Wildcard match
        if (userPermissions.Contains("*"))
            return true;
        
        // Hierarchical match (e.g., "admin" grants "admin.users.read")
        var parts = requiredPermission.Split('.');
        for (int i = 1; i <= parts.Length; i++)
        {
            var parentPermission = string.Join(".", parts.Take(i));
            if (userPermissions.Contains(parentPermission))
                return true;
        }
        
        return false;
    }
}
```

## Row-Level Security (RLS)

### Database Setup

```sql
-- Enable RLS on all component tables
ALTER TABLE account_component ENABLE ROW LEVEL SECURITY;
ALTER TABLE order_component ENABLE ROW LEVEL SECURITY;
ALTER TABLE invoice_component ENABLE ROW LEVEL SECURITY;

-- Create security policies
CREATE POLICY account_isolation ON account_component
    FOR ALL
    TO authenticated
    USING (owner_entity_id = current_setting('app.current_user_id')::uuid);

CREATE POLICY order_visibility ON order_component
    FOR SELECT
    TO authenticated
    USING (
        -- Users can see their own orders
        owner_entity_id = current_setting('app.current_user_id')::uuid
        OR
        -- Users with permission can see all orders
        EXISTS (
            SELECT 1 FROM user_permissions
            WHERE user_id = current_setting('app.current_user_id')::uuid
            AND permission_name = 'orders.read.all'
        )
    );

-- Function to set user context
CREATE OR REPLACE FUNCTION set_user_context(user_id uuid, permissions text[])
RETURNS void AS $$
BEGIN
    PERFORM set_config('app.current_user_id', user_id::text, true);
    PERFORM set_config('app.current_permissions', array_to_string(permissions, ','), true);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
```

### PgClient RLS Integration

```csharp
public class PgClientWrapper : IPgClient
{
    private readonly NpgsqlConnection _connection;
    private readonly ICurrentUser _currentUser;
    
    public async Task AuthenticateConnection(string jwtToken)
    {
        // Validate JWT
        var principal = await _tokenService.ValidateAccessToken(jwtToken);
        if (principal == null)
        {
            throw new UnauthorizedException("Invalid token");
        }
        
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var permissions = JsonSerializer.Deserialize<List<string>>(
            principal.FindFirst("permissions")?.Value ?? "[]");
        
        // Set PostgreSQL session variables for RLS
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT set_user_context(@userId, @permissions)";
        cmd.Parameters.AddWithValue("userId", Guid.Parse(userId));
        cmd.Parameters.AddWithValue("permissions", permissions.ToArray());
        
        await cmd.ExecuteNonQueryAsync();
        
        // Store in context
        _currentUser.SetUser(userId, permissions);
    }
    
    public ITable<T> From<T>() where T : class, new()
    {
        // All queries will now be filtered by RLS policies
        return new PostgrestTable<T>(_connection, _currentUser);
    }
}
```

### RLS Policy Examples

```sql
-- Multi-tenant isolation
CREATE POLICY tenant_isolation ON all_tables
    FOR ALL
    TO authenticated
    USING (tenant_id = current_setting('app.current_tenant_id')::uuid);

-- Department-based access
CREATE POLICY department_access ON employee_component
    FOR SELECT
    TO authenticated
    USING (
        department_id IN (
            SELECT department_id 
            FROM employee_departments
            WHERE employee_id = current_setting('app.current_user_id')::uuid
        )
    );

-- Time-based access
CREATE POLICY business_hours_only ON sensitive_data
    FOR ALL
    TO authenticated
    USING (
        EXTRACT(hour FROM CURRENT_TIME) BETWEEN 9 AND 17
        AND EXTRACT(dow FROM CURRENT_DATE) BETWEEN 1 AND 5
    );

-- Hierarchical access
CREATE POLICY manager_access ON employee_component
    FOR ALL
    TO authenticated
    USING (
        owner_entity_id = current_setting('app.current_user_id')::uuid
        OR
        owner_entity_id IN (
            WITH RECURSIVE subordinates AS (
                SELECT child_entity_id
                FROM entity_relationships
                WHERE parent_entity_id = current_setting('app.current_user_id')::uuid
                AND parent_role = 'Manager'
                
                UNION ALL
                
                SELECT r.child_entity_id
                FROM entity_relationships r
                INNER JOIN subordinates s ON r.parent_entity_id = s.child_entity_id
                WHERE r.parent_role = 'Manager'
            )
            SELECT child_entity_id FROM subordinates
        )
    );
```

## Component Security Model

### Secure Component Pattern

```csharp
public interface ISecureComponent : IComponent
{
    Guid OwnerId { get; init; }
    List<Guid> SharedWith { get; init; }
    string Classification { get; init; } // Public, Internal, Confidential, Secret
}

public record SecureDocument : BaseComponent, ISecureComponent
{
    public Guid OwnerId { get; init; }
    public List<Guid> SharedWith { get; init; } = new();
    public string Classification { get; init; } = "Internal";
    public string Content { get; init; }
    public byte[] EncryptedContent { get; init; }
}

public class SecureDocumentHandler : ComponentHandler<SecureDocument>
{
    private readonly IEncryptionService _encryption;
    
    public async Task<SecureDocument> CreateDocument(string content, string classification)
    {
        var currentUserId = GetCurrentUserId();
        
        // Encrypt sensitive content
        byte[] encryptedContent = null;
        if (classification is "Confidential" or "Secret")
        {
            encryptedContent = await _encryption.Encrypt(
                Encoding.UTF8.GetBytes(content),
                EntityId);
            content = null; // Don't store plaintext
        }
        
        var document = new SecureDocument
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = EntityId,
            OwnerId = currentUserId,
            Content = content,
            EncryptedContent = encryptedContent,
            Classification = classification
        };
        
        await DataContext.Commit(document);
        
        // Audit document creation
        await DataContext.For<SecurityAuditHandler>(EntityId)
            .LogDocumentCreation(classification);
        
        return document;
    }
    
    public async Task<string> GetDocumentContent()
    {
        var document = await Get();
        if (document == null)
        {
            throw new NotFoundException("Document not found");
        }
        
        // Check access rights
        var currentUserId = GetCurrentUserId();
        if (document.OwnerId != currentUserId && 
            !document.SharedWith.Contains(currentUserId))
        {
            throw new ForbiddenException("Access denied");
        }
        
        // Decrypt if necessary
        if (document.EncryptedContent != null)
        {
            var decrypted = await _encryption.Decrypt(
                document.EncryptedContent,
                EntityId);
            return Encoding.UTF8.GetString(decrypted);
        }
        
        return document.Content;
    }
}
```

## Handler Authorization

### Authorization Attributes

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class RequirePermissionAttribute : Attribute
{
    public string Permission { get; }
    
    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class RequireOwnershipAttribute : Attribute
{
    public bool AllowShared { get; set; } = false;
}

public abstract class AuthorizedHandler<T> : ComponentHandler<T> 
    where T : class, IComponent, new()
{
    protected override async Task<TResult> ExecuteMethod<TResult>(
        string methodName,
        Func<Task<TResult>> method)
    {
        // Check method attributes
        var methodInfo = GetType().GetMethod(methodName);
        
        // Check permission requirements
        var permissionAttr = methodInfo?.GetCustomAttribute<RequirePermissionAttribute>();
        if (permissionAttr != null)
        {
            await RequirePermission(permissionAttr.Permission);
        }
        
        // Check ownership requirements
        var ownershipAttr = methodInfo?.GetCustomAttribute<RequireOwnershipAttribute>();
        if (ownershipAttr != null)
        {
            await RequireOwnership(ownershipAttr.AllowShared);
        }
        
        // Execute method
        return await method();
    }
}
```

### Handler Usage

```csharp
public class OrderHandler : AuthorizedHandler<OrderComponent>
{
    [RequirePermission("orders.create")]
    public async Task<OrderComponent> CreateOrder(CreateOrderRequest request)
    {
        // Permission already checked by base class
        // Implementation
    }
    
    [RequireOwnership(AllowShared = true)]
    public async Task<OrderComponent> GetOrder()
    {
        // Ownership already verified
        return await Get();
    }
    
    [RequirePermission("orders.approve")]
    [RequireOwnership]
    public async Task ApproveOrder()
    {
        // Both permission and ownership required
        var order = await Get();
        await Update(order with { Status = "Approved" });
    }
}
```

## API Security Layer

### Security Headers Middleware

```csharp
public class SecurityHeadersMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext != null)
        {
            // Security headers
            httpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            httpContext.Response.Headers.Add("X-Frame-Options", "DENY");
            httpContext.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            httpContext.Response.Headers.Add("Strict-Transport-Security", 
                "max-age=31536000; includeSubDomains");
            httpContext.Response.Headers.Add("Content-Security-Policy", 
                "default-src 'self'; script-src 'self' 'unsafe-inline'");
            
            // Remove server information
            httpContext.Response.Headers.Remove("Server");
            httpContext.Response.Headers.Remove("X-Powered-By");
        }
        
        await next(context);
    }
}
```

### Input Validation

```csharp
public class InputValidationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IValidator _validator;
    
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext?.Request.Body != null && httpContext.Request.ContentLength > 0)
        {
            // Read and validate input
            var body = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
            
            // Check for common attack patterns
            if (ContainsSqlInjection(body))
            {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsync("Invalid input detected");
                return;
            }
            
            if (ContainsXss(body))
            {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsync("Invalid input detected");
                return;
            }
            
            // Validate against schema
            var validationResult = await _validator.ValidateJson(body, context.FunctionDefinition.Name);
            if (!validationResult.IsValid)
            {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    errors = validationResult.Errors
                });
                return;
            }
            
            // Reset stream for next middleware
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        }
        
        await next(context);
    }
}
```

### Rate Limiting

```csharp
public class RateLimitingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IMemoryCache _cache;
    private readonly RateLimitOptions _options;
    
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext == null)
        {
            await next(context);
            return;
        }
        
        // Get client identifier
        var clientId = GetClientIdentifier(httpContext);
        var endpoint = httpContext.Request.Path.Value;
        var key = $"rate_limit:{clientId}:{endpoint}";
        
        // Check rate limit
        var requestCount = _cache.Get<int>(key);
        if (requestCount >= _options.MaxRequests)
        {
            httpContext.Response.StatusCode = 429;
            httpContext.Response.Headers.Add("Retry-After", _options.Window.TotalSeconds.ToString());
            await httpContext.Response.WriteAsync("Rate limit exceeded");
            return;
        }
        
        // Increment counter
        _cache.Set(key, requestCount + 1, _options.Window);
        
        await next(context);
    }
    
    private string GetClientIdentifier(HttpContext context)
    {
        // Use authenticated user ID if available
        if (context.Items.TryGetValue("UserId", out var userId))
        {
            return $"user:{userId}";
        }
        
        // Fall back to IP address
        return $"ip:{context.Connection.RemoteIpAddress}";
    }
}
```

## Database Security Policies

### Advanced RLS Patterns

```sql
-- 1. Column-level security
CREATE POLICY salary_visibility ON employee_component
    FOR SELECT
    TO authenticated
    USING (true)  -- All can see the row
    WITH CHECK (
        -- But salary column is nullified unless you have permission
        CASE 
            WHEN current_setting('app.current_permissions') LIKE '%hr.salaries.read%'
                OR owner_entity_id = current_setting('app.current_user_id')::uuid
            THEN true
            ELSE false
        END
    );

-- Create view to enforce column security
CREATE VIEW employee_view AS
SELECT 
    id,
    owner_entity_id,
    name,
    department,
    CASE 
        WHEN current_setting('app.current_permissions') LIKE '%hr.salaries.read%'
            OR owner_entity_id = current_setting('app.current_user_id')::uuid
        THEN salary
        ELSE NULL
    END as salary
FROM employee_component;

-- 2. Temporal access control
CREATE POLICY temporal_access ON document_component
    FOR ALL
    TO authenticated
    USING (
        -- Documents expire after 90 days unless archived
        (created_at > CURRENT_TIMESTAMP - INTERVAL '90 days')
        OR 
        (status = 'Archived' AND 
         current_setting('app.current_permissions') LIKE '%documents.archived.read%')
    );

-- 3. Workflow-based access
CREATE POLICY workflow_access ON order_component
    FOR UPDATE
    TO authenticated
    USING (
        CASE status
            WHEN 'Draft' THEN 
                owner_entity_id = current_setting('app.current_user_id')::uuid
            WHEN 'Submitted' THEN
                current_setting('app.current_permissions') LIKE '%orders.approve%'
            WHEN 'Approved' THEN
                current_setting('app.current_permissions') LIKE '%orders.process%'
            ELSE false
        END
    );

-- 4. Delegated access
CREATE TABLE access_delegation (
    id uuid PRIMARY KEY,
    delegator_id uuid NOT NULL,
    delegate_id uuid NOT NULL,
    resource_type text NOT NULL,
    permissions text[] NOT NULL,
    valid_from timestamptz NOT NULL,
    valid_until timestamptz NOT NULL
);

CREATE POLICY delegated_access ON sensitive_component
    FOR SELECT
    TO authenticated
    USING (
        owner_entity_id = current_setting('app.current_user_id')::uuid
        OR
        EXISTS (
            SELECT 1 FROM access_delegation
            WHERE delegator_id = owner_entity_id
            AND delegate_id = current_setting('app.current_user_id')::uuid
            AND resource_type = 'sensitive_component'
            AND 'read' = ANY(permissions)
            AND CURRENT_TIMESTAMP BETWEEN valid_from AND valid_until
        )
    );
```

### Security Functions

```sql
-- Audit function
CREATE OR REPLACE FUNCTION audit_changes() 
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO audit_log (
        table_name,
        operation,
        user_id,
        entity_id,
        old_data,
        new_data,
        timestamp
    ) VALUES (
        TG_TABLE_NAME,
        TG_OP,
        current_setting('app.current_user_id')::uuid,
        COALESCE(NEW.owner_entity_id, OLD.owner_entity_id),
        to_jsonb(OLD),
        to_jsonb(NEW),
        CURRENT_TIMESTAMP
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Apply audit trigger to all tables
DO $$
DECLARE
    tbl text;
BEGIN
    FOR tbl IN 
        SELECT table_name 
        FROM information_schema.tables 
        WHERE table_schema = 'public' 
        AND table_name LIKE '%_component'
    LOOP
        EXECUTE format('
            CREATE TRIGGER audit_%I
            AFTER INSERT OR UPDATE OR DELETE ON %I
            FOR EACH ROW EXECUTE FUNCTION audit_changes()
        ', tbl, tbl);
    END LOOP;
END $$;
```

## Session Management

### Session Component

```csharp
public record Session : IComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; init; } // Account ID
    public string SessionToken { get; init; }
    public string IpAddress { get; init; }
    public string UserAgent { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastActivityAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsActive { get; init; }
    public Dictionary<string, object> Metadata { get; init; }
}

public class SessionHandler : ComponentHandler<Session>
{
    public async Task<Session> CreateSession(string ipAddress, string userAgent)
    {
        // Invalidate existing sessions from same device
        await InvalidateExistingSessions(userAgent);
        
        var session = new Session
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = EntityId,
            SessionToken = GenerateSecureToken(),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            IsActive = true,
            Metadata = new Dictionary<string, object>
            {
                ["device_fingerprint"] = GenerateDeviceFingerprint(userAgent),
                ["geo_location"] = await GetGeoLocation(ipAddress)
            }
        };
        
        await DataContext.Commit(session);
        
        // Audit session creation
        await DataContext.For<SecurityAuditHandler>(EntityId)
            .LogSessionCreated(ipAddress, userAgent);
        
        return session;
    }
    
    public async Task ValidateSession(string sessionToken)
    {
        var sessions = await DataContext.CreateQuery()
            .WithComponent<Session>()
            .Where<Session>(s => s.SessionToken == sessionToken)
            .Where<Session>(s => s.IsActive)
            .ExecuteAsync();
            
        if (!sessions.Any())
        {
            throw new UnauthorizedException("Invalid session");
        }
        
        var sessionId = sessions.First();
        var sessionHandler = DataContext.For<SessionHandler>(sessionId);
        var session = await sessionHandler.Get();
        
        // Check expiration
        if (session.ExpiresAt < DateTime.UtcNow)
        {
            await sessionHandler.InvalidateSession();
            throw new UnauthorizedException("Session expired");
        }
        
        // Check for suspicious activity
        if (await IsSessionSuspicious(session))
        {
            await sessionHandler.InvalidateSession();
            await DataContext.For<SecurityAuditHandler>(session.OwnerEntityId)
                .LogSuspiciousActivity("Suspicious session detected");
            throw new UnauthorizedException("Session terminated due to suspicious activity");
        }
        
        // Update last activity
        await sessionHandler.UpdateActivity();
    }
}
```

## Security Audit Trail

### Comprehensive Audit System

```csharp
public class SecurityAuditHandler : ComponentHandler<SecurityAuditEvent>
{
    public async Task LogSecurityEvent(
        string eventType,
        string severity,
        Dictionary<string, object> details)
    {
        var auditEvent = new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = EntityId,
            EventType = eventType,
            Severity = severity,
            Timestamp = DateTime.UtcNow,
            UserId = GetCurrentUserId() ?? Guid.Empty,
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent(),
            Details = details,
            CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString()
        };
        
        await DataContext.Commit(auditEvent);
        
        // Alert on critical events
        if (severity == "Critical")
        {
            await AlertSecurityTeam(auditEvent);
        }
    }
    
    // Specific audit methods
    public async Task LogSuccessfulLogin(string ipAddress, string userAgent)
    {
        await LogSecurityEvent("LOGIN_SUCCESS", "Info", new()
        {
            ["ip_address"] = ipAddress,
            ["user_agent"] = userAgent,
            ["timestamp"] = DateTime.UtcNow
        });
    }
    
    public async Task LogFailedLogin(string ipAddress, string reason)
    {
        await LogSecurityEvent("LOGIN_FAILED", "Warning", new()
        {
            ["ip_address"] = ipAddress,
            ["reason"] = reason,
            ["timestamp"] = DateTime.UtcNow
        });
    }
    
    public async Task LogPermissionDenied(string permission, string resource)
    {
        await LogSecurityEvent("PERMISSION_DENIED", "Warning", new()
        {
            ["permission"] = permission,
            ["resource"] = resource,
            ["user_id"] = GetCurrentUserId()
        });
    }
    
    public async Task LogDataAccess(string dataType, string operation, int recordCount)
    {
        await LogSecurityEvent("DATA_ACCESS", "Info", new()
        {
            ["data_type"] = dataType,
            ["operation"] = operation,
            ["record_count"] = recordCount,
            ["timestamp"] = DateTime.UtcNow
        });
    }
}
```

### Security Monitoring

```csharp
public class SecurityMonitoringSystem : SystemBase
{
    public async Task<SecurityDashboard> GetSecurityMetrics(TimeSpan period)
    {
        var startTime = DateTime.UtcNow - period;
        
        // Failed login attempts
        var failedLogins = await DataContext.CreateQuery()
            .WithComponent<SecurityAuditEvent>()
            .Where<SecurityAuditEvent>(e => e.EventType == "LOGIN_FAILED")
            .Where<SecurityAuditEvent>(e => e.Timestamp > startTime)
            .ExecuteAsync();
        
        // Permission denials
        var permissionDenials = await DataContext.CreateQuery()
            .WithComponent<SecurityAuditEvent>()
            .Where<SecurityAuditEvent>(e => e.EventType == "PERMISSION_DENIED")
            .Where<SecurityAuditEvent>(e => e.Timestamp > startTime)
            .ExecuteAsync();
        
        // Suspicious activities
        var suspiciousActivities = await DataContext.CreateQuery()
            .WithComponent<SecurityAuditEvent>()
            .Where<SecurityAuditEvent>(e => e.Severity == "Critical")
            .Where<SecurityAuditEvent>(e => e.Timestamp > startTime)
            .ExecuteAsync();
        
        return new SecurityDashboard
        {
            FailedLoginAttempts = failedLogins.Count,
            PermissionDenials = permissionDenials.Count,
            SuspiciousActivities = suspiciousActivities.Count,
            TopFailedUsers = await GetTopFailedUsers(failedLogins),
            TopDeniedPermissions = await GetTopDeniedPermissions(permissionDenials),
            SecurityScore = CalculateSecurityScore(failedLogins.Count, permissionDenials.Count)
        };
    }
}
```

## Testing Security

### Security Test Base

```csharp
public abstract class SecurityTestBase : IntegrationTestBase
{
    protected async Task<string> GetAuthToken(string email, string password)
    {
        var authSystem = new AuthenticationSystem(TestDataContext(), Logger<AuthenticationSystem>());
        var result = await authSystem.Login(new LoginRequest
        {
            Email = email,
            Password = password
        });
        
        return result.AccessToken;
    }
    
    protected async Task<Guid> CreateTestUser(string role = "User")
    {
        var accountId = Guid.NewGuid();
        TrackEntity(accountId);
        
        var accountHandler = TestDataContext().For<AccountHandler>(accountId);
        await accountHandler.CreateAccount(
            $"test-{Guid.NewGuid()}@example.com",
            BCrypt.Net.BCrypt.HashPassword("TestPassword123!"));
        
        var roleHandler = TestDataContext().For<RoleHandler>(accountId);
        await roleHandler.AssignRole(role);
        
        return accountId;
    }
}
```

### Security Integration Tests

```csharp
public class SecurityIntegrationTests : SecurityTestBase
{
    [Fact]
    public async Task Should_Enforce_RLS_On_Components()
    {
        // Arrange
        var user1 = await CreateTestUser();
        var user2 = await CreateTestUser();
        
        // Create data for user1
        var order1 = Guid.NewGuid();
        TrackEntity(order1);
        var orderHandler1 = TestDataContext().For<OrderHandler>(order1);
        await orderHandler1.CreateOrder(user1, new CreateOrderRequest());
        
        // Authenticate as user2
        var pgClient = TestDataContext().GetPgClient();
        await pgClient.AuthenticateAs(user2);
        
        // Act - Try to access user1's order
        var orders = await pgClient.From<OrderComponent>()
            .Filter("owner_entity_id", "eq", order1)
            .Get();
        
        // Assert - Should not see user1's data
        orders.Count.ShouldBe(0);
    }
    
    [Fact]
    public async Task Should_Enforce_Permission_Requirements()
    {
        // Arrange
        var user = await CreateTestUser("User"); // Basic user role
        var adminSystem = new AdminSystem(TestDataContext(), Logger<AdminSystem>());
        adminSystem.SetCurrentUser(user);
        
        // Act & Assert
        await Should.ThrowAsync<ForbiddenException>(async () =>
        {
            await adminSystem.GetAllUsers(); // Requires admin.users.read
        });
    }
}
```

## Common Attack Vectors

### SQL Injection Prevention

```csharp
// ❌ NEVER do this
public async Task<List<Order>> GetOrdersUnsafe(string status)
{
    var sql = $"SELECT * FROM orders WHERE status = '{status}'";
    return await _connection.QueryAsync<Order>(sql);
}

// ✅ Always use parameterized queries
public async Task<List<Order>> GetOrdersSafe(string status)
{
    var sql = "SELECT * FROM orders WHERE status = @status";
    return await _connection.QueryAsync<Order>(sql, new { status });
}

// ✅ Jarvis components are safe by default
public async Task<List<Guid>> GetOrdersSafest(string status)
{
    return await DataContext.CreateQuery()
        .WithComponent<OrderComponent>()
        .Where<OrderComponent>(o => o.Status == status)
        .ExecuteAsync();
}
```

### XSS Prevention

```csharp
public class XssProtectionMiddleware
{
    private readonly IHtmlSanitizer _sanitizer;
    
    public async Task SanitizeInput(object input)
    {
        foreach (var property in input.GetType().GetProperties())
        {
            if (property.PropertyType == typeof(string))
            {
                var value = property.GetValue(input) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    var sanitized = _sanitizer.Sanitize(value);
                    property.SetValue(input, sanitized);
                }
            }
        }
    }
}
```

### CSRF Protection

```csharp
public class CsrfProtectionMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        
        if (IsStateChangingMethod(httpContext.Request.Method))
        {
            var csrfToken = httpContext.Request.Headers["X-CSRF-Token"].FirstOrDefault();
            var sessionToken = context.Items["SessionToken"] as string;
            
            if (!ValidateCsrfToken(csrfToken, sessionToken))
            {
                httpContext.Response.StatusCode = 403;
                await httpContext.Response.WriteAsync("CSRF validation failed");
                return;
            }
        }
        
        await next(context);
    }
}
```

## Best Practices

### 1. Defense in Depth

```csharp
// Multiple layers of security
public class SecureOrderSystem : SecureSystemBase
{
    public async Task<OrderResult> ProcessOrder(OrderRequest request)
    {
        // Layer 1: API authentication (handled by middleware)
        
        // Layer 2: System-level permission check
        await RequirePermission("orders.create");
        
        // Layer 3: Input validation
        await ValidateOrderRequest(request);
        
        // Layer 4: Business rule validation
        var customerHandler = DataContext.For<CustomerHandler>(request.CustomerId);
        if (!await customerHandler.CanPlaceOrder())
        {
            throw new BusinessRuleException("Customer cannot place orders");
        }
        
        // Layer 5: Handler-level authorization
        var orderHandler = DataContext.For<OrderHandler>(Guid.NewGuid());
        // Handler will check ownership and permissions
        
        // Layer 6: Database RLS will enforce data isolation
        return await orderHandler.CreateOrder(request);
    }
}
```

### 2. Principle of Least Privilege

```csharp
// Grant minimal permissions required
public static class DefaultRoles
{
    public static readonly Role User = new()
    {
        Name = "User",
        Permissions = new()
        {
            new("profile.read.own"),
            new("profile.write.own"),
            new("orders.read.own"),
            new("orders.create")
        }
    };
    
    public static readonly Role Manager = new()
    {
        Name = "Manager",
        Permissions = new()
        {
            new("profile.read.own"),
            new("profile.write.own"),
            new("orders.read.all"),
            new("orders.create"),
            new("orders.approve"),
            new("reports.read")
        }
    };
}
```

### 3. Secure by Default

```csharp
// Components are secure by default
public record SecureComponent : BaseComponent, IComponent
{
    // No data is public by default
    private string SensitiveData { get; init; }
    
    // Explicit methods for controlled access
    public string GetSensitiveData(ISecurityContext context)
    {
        if (!context.HasPermission("sensitive.read"))
        {
            throw new ForbiddenException();
        }
        return SensitiveData;
    }
}
```

### 4. Security Monitoring

```csharp
// Continuous security monitoring
public class SecurityMonitor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Check for brute force attempts
            await DetectBruteForceAttempts();
            
            // Check for privilege escalation
            await DetectPrivilegeEscalation();
            
            // Check for data exfiltration
            await DetectDataExfiltration();
            
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

## Conclusion

The security architecture in Jarvis provides comprehensive protection through multiple layers:

### Key Achievements

1. **Multi-Layer Security**: Protection at every level from API to database
2. **Zero Trust Model**: Every operation is authenticated and authorized
3. **Data Isolation**: RLS ensures users only see their authorized data
4. **Audit Compliance**: Complete audit trail for all security events
5. **Attack Prevention**: Protection against common vulnerabilities

### Security Principles

1. **Authentication**: JWT-based with refresh tokens and 2FA support
2. **Authorization**: RBAC with hierarchical permissions
3. **Data Security**: RLS policies enforced at database level
4. **Audit Trail**: Every operation is logged and traceable
5. **Defense in Depth**: Multiple security layers provide redundancy

### Best Practices Summary

1. **Always use Systems**: Never bypass the security layers
2. **Validate Everything**: Input validation at every layer
3. **Least Privilege**: Grant minimal required permissions
4. **Monitor Continuously**: Watch for security anomalies
5. **Test Security**: Include security tests in your suite

The combination of application-level authentication, role-based access control, and database-level row security creates a robust security posture that protects both the operations users can perform and the data they can access.

---

*Document Version: 1.0*  
*Last Updated: January 2025*  
*Authors: Jarvis Security Team*