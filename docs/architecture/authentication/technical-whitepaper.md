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

[Content continues with remaining sections...]

---

*Document Version: 1.0*  
*Last Updated: January 2025*  
*Authors: Jarvis Security Team*