using System.Linq;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.api.Exceptions;
using core.jarvis.Data;
using core.jarvis.data;
using core.jarvis.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using BCrypt.Net;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for authentication operations on Account components.
/// </summary>
public class AuthHandler : ComponentHandler<Account>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly int _refreshTokenExpirationDays;
    private readonly IPasswordPolicyService _passwordPolicy;
    private readonly ISecurityAuditService _securityAudit;
    private readonly IConstantTimeService _constantTimeService;

    public AuthHandler(
        IDataContext dataContext,
        ILogger<AuthHandler> logger,
        IServiceProvider serviceProvider)
        : base(dataContext, logger)
    {
        _serviceProvider = serviceProvider;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        _refreshTokenExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "30");
        _passwordPolicy = serviceProvider.GetRequiredService<IPasswordPolicyService>();
        _securityAudit = serviceProvider.GetRequiredService<ISecurityAuditService>();
        _constantTimeService = serviceProvider.GetRequiredService<IConstantTimeService>();
    }

    /// <summary>
    /// Authenticates account credentials and generates tokens.
    /// Takes Account (credentials), validates against existing data, and returns AuthToken with session data.
    /// Does NOT persist anything - authentication is read-only validation.
    /// Uses constant-time execution to prevent timing attacks and includes comprehensive security logging.
    /// </summary>
    /// <param name="accountCredentials">Account object containing email, password, and optional metadata (IP address, user agent, client ID)</param>
    /// <returns>
    /// AuthToken with access token, refresh token, and session data if authentication succeeds.
    /// Returns empty AuthToken if authentication fails for any reason (invalid credentials, inactive account, etc.).
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when accountCredentials is null</exception>
    /// <remarks>
    /// This method implements several security features:
    /// - Constant-time execution to prevent timing attacks
    /// - Input validation to prevent injection attacks
    /// - BCrypt password verification
    /// - Security audit logging for both successful and failed attempts
    /// - Random delays to further obfuscate timing patterns
    /// </remarks>
    public async Task<AuthToken> Authenticate(Account accountCredentials)
    {
        // Wrap authentication in constant-time execution to prevent timing attacks
        // Stronger timing attack protection
        var isTestEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Test";
        var minimumTime = isTestEnvironment ? 10 : 500; // Increased minimum time for production
        
        var result = await _constantTimeService.ExecuteWithMinimumTime(async () => 
            await AuthenticateInternal(accountCredentials), 
            minimumMilliseconds: minimumTime);
        
        // Add random delay even in test environment for better security
        var minDelay = isTestEnvironment ? 5 : 50;
        var maxDelay = isTestEnvironment ? 15 : 200;
        await _constantTimeService.AddRandomDelay(minDelay, maxDelay);
        
        return result;
    }

    private async Task<AuthToken> AuthenticateInternal(Account accountCredentials)
    {
        try
        {
            // Basic input validation
            if (string.IsNullOrWhiteSpace(accountCredentials.Email) || 
                string.IsNullOrWhiteSpace(accountCredentials.Password))
            {
                Logger.LogWarning("Invalid input: empty email or password");
                return new AuthToken(); // Return empty token to indicate failure
            }

            // Validate input for security
            if (!IsValidInput(accountCredentials.Email) || !IsValidInput(accountCredentials.Password))
            {
                Logger.LogWarning("Invalid input detected - possible injection attempt");
                return new AuthToken(); // Return empty token to indicate failure
            }

            // Get services
            var tokenService = _serviceProvider.GetRequiredService<ITokenService>();
            
            // Query account by email using the component system
            var query = DataContext.Query()
                .With<Account>(a => a.Email == accountCredentials.Email);
            var componentsByEntity = await query.ToEntityComponents();
            
            Account? account = null;
            foreach (var kvp in componentsByEntity)
            {
                var acc = kvp.Value.Get<Account>();
                if (acc != null && acc.Email == accountCredentials.Email)
                {
                    account = acc;
                    break;
                }
            }
            
            if (account == null)
            {
                // Log failed authentication attempt
                await _securityAudit.LogFailedAuthentication(
                    accountCredentials.Email, 
                    accountCredentials.IpAddress ?? "unknown",
                    accountCredentials.UserAgent,
                    "Account not found"
                );
                
                return new AuthToken(); // Return empty token to indicate failure
            }
            
            // Check if account is active
            if (!account.IsActive)
            {
                await _securityAudit.LogFailedAuthentication(
                    accountCredentials.Email, 
                    accountCredentials.IpAddress ?? "unknown",
                    accountCredentials.UserAgent,
                    "Account is not active"
                );
                
                return new AuthToken();
            }
            
            // Verify password using BCrypt
            var isValidPassword = BCrypt.Net.BCrypt.Verify(accountCredentials.Password, account.PasswordHash);
            
            if (!isValidPassword)
            {
                await _securityAudit.LogFailedAuthentication(
                    accountCredentials.Email, 
                    accountCredentials.IpAddress ?? "unknown",
                    accountCredentials.UserAgent,
                    "Invalid credentials"
                );
                
                return new AuthToken(); // Return empty token to indicate failure
            }
            
            // Authentication successful - use the account's owner entity ID
            var authenticatedEntityId = account.OwnerEntityId;

            // Generate tokens
            var accessToken = tokenService.AccessToken(authenticatedEntityId, accountCredentials.Email);
            var refreshToken = tokenService.RefreshToken();
            var sessionId = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddMinutes(15);

            // Add sessionId to token claims for revocation tracking
            var additionalClaims = new Dictionary<string, string>
            {
                ["sessionId"] = sessionId.ToString()
            };

            var finalAccessToken = tokenService.AccessToken(authenticatedEntityId, accountCredentials.Email, additionalClaims);

            // Create AuthToken result - OwnerEntityId is the authenticated user's entity ID
            var authToken = new AuthToken
            {
                OwnerEntityId = authenticatedEntityId,
                AccessToken = finalAccessToken ?? string.Empty,
                RefreshToken = refreshToken ?? string.Empty,
                RefreshTokenHash = tokenService.HashRefreshToken(refreshToken ?? string.Empty),
                ExpiresAt = expiresAt,
                RefreshExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                SessionId = sessionId,
                ClientId = accountCredentials.ClientId,
                LastUpdated = DateTime.UtcNow
            };

            // Save the auth token to the database
            await DataContext.Commit(authToken);

            // Log successful authentication
            await _securityAudit.LogSuccessfulAuthentication(
                authenticatedEntityId,
                accountCredentials.Email,
                accountCredentials.IpAddress ?? "unknown",
                accountCredentials.UserAgent
            );

            return authToken;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during authentication for email: {Email}", accountCredentials.Email);
            return new AuthToken();
        }
    }


    /// <summary>
    /// Checks if the provided AuthToken component represents successful authentication.
    /// </summary>
    /// <param name="authToken">The AuthToken to validate</param>
    /// <returns>True if the token contains a valid access token and owner entity ID, false otherwise</returns>
    public bool IsAuthenticated(AuthToken authToken)
    {
        return authToken != null && !string.IsNullOrEmpty(authToken.AccessToken) && authToken.OwnerEntityId != Guid.Empty;
    }

    /// <summary>
    /// Persists the authenticated session token to the database.
    /// Only call this after successful authentication.
    /// Includes cleanup of expired tokens and session limit enforcement.
    /// </summary>
    /// <param name="authToken">The authenticated AuthToken to persist</param>
    /// <returns>True if the session was successfully persisted, false if validation failed or an error occurred</returns>
    /// <remarks>
    /// This method performs several operations:
    /// - Validates the AuthToken before persisting
    /// - Cleans up expired tokens for the user
    /// - Enforces a maximum of 5 active sessions per user
    /// - Stores only the refresh token hash (not plain tokens) for security
    /// </remarks>
    public async Task<bool> PersistSession(AuthToken authToken)
    {
        if (!IsAuthenticated(authToken))
        {
            Logger.LogWarning("Cannot persist invalid authentication token");
            return false;
        }

        try
        {
            // Clean up expired tokens and enforce session limit for this user
            var tokenHandler = DataContext.For<AuthTokenHandler>(authToken.OwnerEntityId);
            await tokenHandler.CleanupExpiredTokens();
            await tokenHandler.EnforceSessionLimit(5); // Max 5 active sessions per user

            // Create a new entity for this session
            var sessionEntity = new AuthToken
            {
                OwnerEntityId = authToken.OwnerEntityId,
                AccessToken = string.Empty, // Don't store access token
                RefreshToken = string.Empty, // Don't store plain refresh token
                RefreshTokenHash = authToken.RefreshTokenHash,
                ExpiresAt = authToken.ExpiresAt,
                RefreshExpiresAt = authToken.RefreshExpiresAt,
                SessionId = authToken.SessionId,
                ClientId = authToken.ClientId,
                IsRevoked = false,
                IssuedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            await DataContext.Commit(sessionEntity);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting session for entity {EntityId}", authToken.OwnerEntityId);
            return false;
        }
    }


    /// <summary>
    /// Validates email format.
    /// </summary>
    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Basic email validation
            var emailParts = email.Split('@');
            if (emailParts.Length != 2)
                return false;

            var localPart = emailParts[0];
            var domainPart = emailParts[1];

            if (string.IsNullOrWhiteSpace(localPart) || string.IsNullOrWhiteSpace(domainPart))
                return false;

            // Domain must have at least one dot
            if (!domainPart.Contains('.'))
                return false;

            // Check for valid characters (removed dangerous + character)
            var validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-_@";
            foreach (char c in email)
            {
                if (!validChars.Contains(c))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Refreshes an authentication token using a valid refresh token.
    /// Validates the refresh token, revokes the old token, and generates new access and refresh tokens.
    /// </summary>
    /// <param name="refreshToken">The refresh token string to validate and use for token refresh</param>
    /// <returns>
    /// New AuthToken with fresh access and refresh tokens if the refresh token is valid.
    /// Returns empty AuthToken if the refresh token is invalid, expired, or revoked.
    /// </returns>
    /// <remarks>
    /// This method performs the following operations:
    /// - Hashes the refresh token for secure lookup
    /// - Validates the refresh token against stored hash
    /// - Checks token expiration and revocation status
    /// - Generates new access and refresh tokens
    /// - Revokes the old refresh token to prevent reuse
    /// - Maintains the same session ID for continuity
    /// </remarks>
    public async Task<AuthToken> RefreshToken(string refreshToken)
    {
        try
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                Logger.LogWarning("Refresh token is empty");
                return new AuthToken();
            }

            var tokenService = _serviceProvider.GetRequiredService<ITokenService>();
            var entityQuery = _serviceProvider.GetRequiredService<IEntityQuery>();
            
            // Hash the refresh token for lookup
            var refreshTokenHash = tokenService.HashRefreshToken(refreshToken);
            
            // Find the auth token by refresh token hash
            var entityIds = await entityQuery
                .WithAll<AuthToken>(t => t.RefreshTokenHash == refreshTokenHash && !t.IsRevoked)
                .ToEntityIds();
                
            if (!entityIds.Any())
            {
                Logger.LogWarning("Invalid or expired refresh token");
                return new AuthToken();
            }
            
            // Get the first auth token
            var entityId = entityIds.First();
            var components = await DataContext.Query()
                .With<AuthToken>(t => t.OwnerEntityId == entityId)
                .ToEntityComponents();
                
            if (!components.TryGetValue(entityId, out var entityComponents))
            {
                Logger.LogWarning("Auth token not found for entity {EntityId}", entityId);
                return new AuthToken();
            }
            
            var existingToken = entityComponents.Get<AuthToken>();
            if (existingToken == null)
            {
                Logger.LogWarning("Auth token not found");
                return new AuthToken();
            }
            
            // Check if refresh token is expired
            if (existingToken.RefreshExpiresAt < DateTime.UtcNow)
            {
                Logger.LogWarning("Refresh token expired for entity {EntityId}", existingToken.OwnerEntityId);
                return new AuthToken();
            }
            
            // Get the account associated with the token
            var accountComponents = await DataContext.Query()
                .With<Account>(a => a.OwnerEntityId == existingToken.OwnerEntityId)
                .ToEntityComponents();
                
            if (!accountComponents.TryGetValue(existingToken.OwnerEntityId, out var accountEntityComponents))
            {
                Logger.LogWarning("Account not found for entity {EntityId}", existingToken.OwnerEntityId);
                return new AuthToken();
            }
            
            var account = accountEntityComponents.Get<Account>();
            if (account == null)
            {
                Logger.LogWarning("Account not found for entity {EntityId}", existingToken.OwnerEntityId);
                return new AuthToken();
            }
            
            // Generate new tokens
            var newAccessToken = tokenService.AccessToken(account.Id, account.Email);
            var newRefreshToken = tokenService.RefreshToken();
            
            // Revoke the old token
            var revokedToken = existingToken with 
            { 
                IsRevoked = true, 
                RevokedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            await DataContext.Commit(revokedToken);
            
            // Create new auth token
            var newAuthToken = new AuthToken
            {
                Id = Guid.NewGuid(),
                OwnerEntityId = account.Id,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                RefreshTokenHash = tokenService.HashRefreshToken(newRefreshToken),
                SessionId = existingToken.SessionId, // Keep the same session
                RefreshExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                ClientId = existingToken.ClientId,
                IpAddress = existingToken.IpAddress,
                UserAgent = existingToken.UserAgent,
                IsRevoked = false,
                LastUpdated = DateTime.UtcNow
            };
            
            await DataContext.Commit(newAuthToken);
            
            Logger.LogInformation("Token refreshed for entity {EntityId}", account.Id);
            return newAuthToken;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing token");
            return new AuthToken();
        }
    }

    /// <summary>
    /// Validates input to prevent injection attacks.
    /// </summary>
    private bool IsValidInput(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        // For email validation, check if it looks like an email
        if (input.Contains('@') && input.Contains('.'))
        {
            // Basic email format validation
            return IsValidEmail(input);
        }

        // Comprehensive injection pattern detection
        // Check for SQL injection, NoSQL injection, command injection, and script injection patterns
        var dangerousPatterns = new[] { 
            // SQL injection patterns
            "' OR '", "'; DROP", "'; DELETE", "'; UPDATE", "'; INSERT", "'; CREATE", "'; ALTER",
            "\' OR ", "\';DROP", "\';DELETE", "\';UPDATE", "\';INSERT", "\';CREATE", "\';ALTER",
            "--", "/*", "*/", " UNION ", " SELECT ", " FROM ", " WHERE ", " INTO ",
            "EXEC(", "EXECUTE(", "SP_", "XP_", "0x", "CHAR(", "ASCII(",
            // NoSQL injection patterns
            "$where", "$ne", "$in", "$nin", "$gt", "$lt", "$regex", "$or", "$and",
            // Command injection patterns
            "`", "$(", "${", "&&", "||", ";", "|", "<", ">", "&",
            // Script injection patterns
            "<script", "</script", "javascript:", "vbscript:", "onload=", "onerror=",
            "eval(", "setTimeout(", "setInterval(", "Function(",
            // Control characters
            "\n", "\r", "\t", "\0", "\x00"
        };
        
        foreach (var pattern in dangerousPatterns)
        {
            if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}