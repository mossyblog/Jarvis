using System.Linq;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.Data;
using core.jarvis.data;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace core.jarvis.api.Services;

/// <summary>
/// Implementation of authentication operations.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IDataContext _dataContext;
    private readonly ITokenService _tokenService;
    private readonly PgClient _pgClient;
    private readonly NpgsqlConnection _connection;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly int _refreshTokenExpirationDays;

    public AuthenticationService(
        IDataContext dataContext,
        ITokenService tokenService,
        NpgsqlConnection connection,
        ILogger<AuthenticationService> logger,
        int refreshTokenExpirationDays = 30)
    {
        _dataContext = dataContext;
        _tokenService = tokenService;
        _connection = connection;
        _pgClient = new PgClient(connection);
        _logger = logger;
        _refreshTokenExpirationDays = refreshTokenExpirationDays;
    }

    public async Task<AuthResponse?> AuthenticateAsync(AuthRequest request, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            // Validate credentials using PgClient
            var authResult = await _pgClient.Authenticate(request.Email, request.Password);
            _logger.LogInformation("PgClient.Authenticate returned: {Result} for email: {Email}", authResult, request.Email);
            if (string.IsNullOrEmpty(authResult))
            {
                _logger.LogWarning("Authentication failed for email: {Email}", request.Email);
                return null;
            }

            // Extract user ID from auth result (format: "auth.success.{userId}")
            Guid userId;
            if (authResult.StartsWith("auth.success."))
            {
                var userIdStr = authResult.Substring("auth.success.".Length);
                if (!Guid.TryParse(userIdStr, out userId))
                {
                    _logger.LogError("Failed to parse user ID from auth result: {Result}", authResult);
                    return null;
                }
            }
            else
            {
                _logger.LogError("Unexpected auth result format: {Result}", authResult);
                return null;
            }

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(userId, request.Email);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var sessionId = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddMinutes(15); // Match token service default

            // Store the session as a SecurityToken component
            // Use a new entity ID for each session to allow multiple sessions per user
            var tokenEntityId = Guid.NewGuid();
            var securityToken = new SecurityToken
            {
                OwnerEntityId = tokenEntityId,
                UserId = userId,
                SessionId = sessionId,
                RefreshTokenHash = _tokenService.HashRefreshToken(refreshToken),
                RefreshExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                ClientId = request.ClientId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsRevoked = false
            };

            // Store via handler pattern
            var tokenHandler = _dataContext.For<SecurityTokenHandler>(tokenEntityId);
            await tokenHandler.CreateAsync(securityToken);

            return new AuthResponse
            {
                OwnerEntityId = tokenEntityId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                UserId = userId,
                SessionId = sessionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for email: {Email}", request.Email);
            return null;
        }
    }

    public async Task<bool> DeauthenticateAsync(Guid sessionId)
    {
        try
        {
            // Use EntityQuery to find the token by session ID
            var entityComponents = await _dataContext.Query()
                .WithAll<SecurityToken>(t => t.SessionId == sessionId)
                .ToEntityComponents();
            
            var tokenEntry = entityComponents.FirstOrDefault();
            SecurityToken? token = null;
            if (tokenEntry.Key != Guid.Empty)
            {
                token = tokenEntry.Value?.Get<SecurityToken>();
            }
            
            if (token == null)
            {
                _logger.LogWarning("Session not found for deauth: {SessionId}", sessionId);
                return false;
            }
            
            // Check if already revoked
            if (token.IsRevoked)
            {
                _logger.LogWarning("Session already revoked: {SessionId}", sessionId);
                return false;
            }

            // Revoke the token
            var updatedToken = token with 
            { 
                IsRevoked = true,
                RevokedAt = DateTime.UtcNow 
            };
            
            await _dataContext.Commit(updatedToken);
            
            _logger.LogInformation("Session revoked: {SessionId}", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during deauthentication for session: {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<AuthResponse?> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            // Find all active security tokens
            var entityComponents = await _dataContext.Query()
                .WithAll<SecurityToken>(t => !t.IsRevoked && t.RefreshExpiresAt > DateTime.UtcNow)
                .ToEntityComponents();
            
            SecurityToken? matchingToken = null;

            foreach (var kvp in entityComponents)
            {
                var token = kvp.Value.Get<SecurityToken>();
                if (token != null && _tokenService.VerifyRefreshToken(request.RefreshToken, token.RefreshTokenHash))
                {
                    matchingToken = token;
                    break;
                }
            }

            if (matchingToken == null || matchingToken.IsRevoked || matchingToken.RefreshExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired refresh token");
                return null;
            }

            // Validate client ID if provided
            if (!string.IsNullOrEmpty(request.ClientId) && matchingToken.ClientId != request.ClientId)
            {
                _logger.LogWarning("Client ID mismatch during token refresh");
                return null;
            }

            // Get user email
            var userEmail = await GetUserEmailById(matchingToken.UserId);
            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogError("User email not found for ID: {UserId}", matchingToken.UserId);
                return null;
            }

            // Generate new tokens
            var newAccessToken = _tokenService.GenerateAccessToken(matchingToken.UserId, userEmail);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(15);

            // Update the security token with new refresh token
            var updatedToken = matchingToken with 
            {
                RefreshTokenHash = _tokenService.HashRefreshToken(newRefreshToken),
                UpdatedAt = DateTime.UtcNow
            };
            
            await _dataContext.Commit(updatedToken);

            return new AuthResponse
            {
                OwnerEntityId = matchingToken.OwnerEntityId,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = expiresAt,
                UserId = matchingToken.UserId,
                SessionId = matchingToken.SessionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return null;
        }
    }

    public async Task<ValidationResponse> ValidateTokenAsync(Guid tokenId)
    {
        try
        {
            // Get the security token by entity ID - we need to find the token where its entity ID matches
            // Since we don't have a direct way to query by entity ID in the query system,
            // we'll need to get the token using the handler pattern for read operations
            var tokenHandler = _dataContext.For<SecurityTokenHandler>(tokenId);
            var token = await tokenHandler.GetAsync(tokenId);
            
            if (token == null)
            {
                return new ValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = "Token not found"
                };
            }

            if (token.IsRevoked)
            {
                return new ValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = "Token has been revoked"
                };
            }

            return new ValidationResponse
            {
                IsValid = true,
                UserId = token.UserId,
                ExpiresAt = token.RefreshExpiresAt,
                Claims = new Dictionary<string, string>
                {
                    ["SessionId"] = token.SessionId.ToString(),
                    ["ClientId"] = token.ClientId ?? string.Empty
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token validation for ID: {TokenId}", tokenId);
            return new ValidationResponse
            {
                IsValid = false,
                ErrorMessage = "Validation error occurred"
            };
        }
    }

    // Helper methods to interact with users table
    private async Task<Guid?> GetUserIdByEmail(string email)
    {
        try
        {
            // Query the users table directly
            var sql = "SELECT id FROM users WHERE email = @email LIMIT 1";
            
            // Ensure connection is open
            if (_connection.State != System.Data.ConnectionState.Open)
                await _connection.OpenAsync();
            
            using var command = new NpgsqlCommand(sql, _connection);
            command.Parameters.AddWithValue("@email", email);
            
            var result = await command.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                return (Guid)result;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user ID by email: {Email}", email);
            return null;
        }
    }

    private async Task<string?> GetUserEmailById(Guid userId)
    {
        try
        {
            // Query the users table directly
            var sql = "SELECT email FROM users WHERE id = @userId LIMIT 1";
            
            // Ensure connection is open
            if (_connection.State != System.Data.ConnectionState.Open)
                await _connection.OpenAsync();
            
            using var command = new NpgsqlCommand(sql, _connection);
            command.Parameters.AddWithValue("@userId", userId);
            
            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user email by ID: {UserId}", userId);
            return null;
        }
    }
}