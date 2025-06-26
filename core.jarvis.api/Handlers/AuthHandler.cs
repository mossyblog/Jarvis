using System.Linq;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.Data;
using core.jarvis.data;
using core.jarvis.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

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
    /// </summary>
    public async Task<AuthToken> Authenticate(Account accountCredentials)
    {
        // Wrap authentication in constant-time execution to prevent timing attacks
        return await _constantTimeService.ExecuteWithMinimumTime(async () => 
            await AuthenticateInternal(accountCredentials), 
            minimumMilliseconds: 100);
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

            // Get services
            var tokenService = _serviceProvider.GetRequiredService<ITokenService>();
            var pgClient = _serviceProvider.GetRequiredService<IPgClient>();
            
            // Validate credentials using PgClient - it uses parameterized queries for SQL injection protection
            var authResult = await pgClient.Client.Authenticate(accountCredentials.Email, accountCredentials.Password);
            if (string.IsNullOrEmpty(authResult))
            {
                
                // Log failed authentication attempt
                await _securityAudit.LogFailedAuthentication(
                    accountCredentials.Email, 
                    accountCredentials.IpAddress ?? "unknown",
                    accountCredentials.UserAgent,
                    "Invalid credentials"
                );
                
                return new AuthToken(); // Return empty token to indicate failure
            }

            // Extract entity ID from auth result (format: "auth.success.{entityId}")
            Guid authenticatedEntityId;
            if (authResult.StartsWith("auth.success."))
            {
                var entityIdStr = authResult.Substring("auth.success.".Length);
                if (!Guid.TryParse(entityIdStr, out authenticatedEntityId))
                {
                    Logger.LogError("Failed to parse entity ID from auth result: {Result}", authResult);
                    return new AuthToken();
                }
            }
            else
            {
                Logger.LogError("Unexpected auth result format: {Result}", authResult);
                return new AuthToken();
            }

            // Generate tokens
            var accessToken = tokenService.GenerateAccessToken(authenticatedEntityId, accountCredentials.Email);
            var refreshToken = tokenService.GenerateRefreshToken();
            var sessionId = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddMinutes(15);

            // Try to get existing security profile for role claims
            var additionalClaims = new Dictionary<string, string>();
            try
            {
                var profileHandler = DataContext.For<AccountProfileHandler>(authenticatedEntityId);
                var securityProfile = await profileHandler.Get();
                
                if (securityProfile?.RoleIds?.Any() == true)
                {
                    additionalClaims["roles"] = string.Join(",", securityProfile.RoleIds);
                }
            }
            catch (Exception ex)
            {
                // User doesn't have a security profile yet, which is fine for new users
                Logger.LogDebug(ex, "No security profile found for user {UserId}, proceeding without roles", authenticatedEntityId);
            }

            var finalAccessToken = tokenService.GenerateAccessToken(authenticatedEntityId, accountCredentials.Email, additionalClaims);

            // Create AuthToken result - OwnerEntityId is the authenticated user's entity ID
            var authToken = new AuthToken
            {
                OwnerEntityId = authenticatedEntityId,
                AccessToken = finalAccessToken,
                RefreshToken = refreshToken,
                RefreshTokenHash = tokenService.HashRefreshToken(refreshToken),
                ExpiresAt = expiresAt,
                RefreshExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                SessionId = sessionId,
                ClientId = accountCredentials.ClientId,
                UpdatedAt = DateTime.UtcNow
            };

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
    public bool IsAuthenticated(AuthToken authToken)
    {
        return authToken != null && !string.IsNullOrEmpty(authToken.AccessToken) && authToken.OwnerEntityId != Guid.Empty;
    }

    /// <summary>
    /// Persists the authenticated session token to the database.
    /// Only call this after successful authentication.
    /// </summary>
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
                UpdatedAt = DateTime.UtcNow
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
    /// Authenticates a user from JSON request and returns the auth token.
    /// This method handles all authentication logic to keep the API layer thin.
    /// </summary>
    public async Task<AuthToken> AuthenticateFromJson(string requestBody, string? ipAddress, string? userAgent)
    {
        try
        {
            // Parse request body
            if (string.IsNullOrEmpty(requestBody))
            {
                Logger.LogInformation("Auth request body is empty");
                return new AuthToken(); // Empty token indicates failure
            }

            Account? accountRequest;
            try
            {
                // First try with default (PascalCase) deserialization
                accountRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<Account>(requestBody);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                try
                {
                    // If that fails, try with camelCase
                    var camelCaseSettings = new Newtonsoft.Json.JsonSerializerSettings
                    {
                        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                    };
                    accountRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<Account>(requestBody, camelCaseSettings);
                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    Logger.LogWarning("JSON deserialization failed: {Message}. Request body: {Body}", ex.Message, requestBody);
                    return new AuthToken();
                }
            }

            if (accountRequest == null)
            {
                return new AuthToken();
            }

            // Validate required fields
            if (string.IsNullOrEmpty(accountRequest.Email) || string.IsNullOrEmpty(accountRequest.Password))
            {
                return new AuthToken();
            }

            // Add IP and User-Agent to the request
            accountRequest = accountRequest with 
            { 
                IpAddress = ipAddress ?? "unknown",
                UserAgent = userAgent
            };

            // Authenticate and return result
            var authToken = await Authenticate(accountRequest);
            
            // If authentication succeeded, persist the session
            if (IsAuthenticated(authToken))
            {
                await PersistSession(authToken);
            }
            
            return authToken;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in authentication from JSON");
            return new AuthToken();
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

            // Check for valid characters
            var validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-_@+";
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
    /// </summary>
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
            var newAccessToken = tokenService.GenerateAccessToken(account.Id, account.Email);
            var newRefreshToken = tokenService.GenerateRefreshToken();
            
            // Revoke the old token
            var revokedToken = existingToken with 
            { 
                IsRevoked = true, 
                RevokedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                UpdatedAt = DateTime.UtcNow
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
}