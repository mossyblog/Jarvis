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
        _pgClient = new PgClient(connection);
        _logger = logger;
        _refreshTokenExpirationDays = refreshTokenExpirationDays;
    }

    public async Task<AuthResponse?> AuthenticateAsync(AuthRequest request, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            // Validate credentials using PgClient
            var jwt = await _pgClient.Authenticate(request.Email, request.Password);
            if (string.IsNullOrEmpty(jwt))
            {
                _logger.LogWarning("Authentication failed for email: {Email}", request.Email);
                return null;
            }

            // Get user ID from the database
            var userId = await GetUserIdByEmail(request.Email);
            if (!userId.HasValue)
            {
                _logger.LogError("User ID not found for authenticated email: {Email}", request.Email);
                return null;
            }

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(userId.Value, request.Email);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var sessionId = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddMinutes(15); // Match token service default

            // Store the session as a SecurityToken component
            var securityToken = new SecurityToken
            {
                OwnerEntityId = userId.Value,
                UserId = userId.Value,
                SessionId = sessionId,
                RefreshTokenHash = _tokenService.HashRefreshToken(refreshToken),
                RefreshExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                ClientId = request.ClientId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsRevoked = false
            };

            // Store via handler pattern (would need SecurityTokenHandler)
            await StoreSecurityToken(securityToken);

            return new AuthResponse
            {
                OwnerEntityId = userId.Value,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                UserId = userId.Value,
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
            // Find and revoke the security token
            var token = await GetSecurityTokenBySessionId(sessionId);
            if (token == null)
            {
                _logger.LogWarning("Session not found for deauth: {SessionId}", sessionId);
                return false;
            }

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            
            await UpdateSecurityToken(token);
            
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
            // Find the security token by refresh token
            var securityTokens = await GetActiveSecurityTokens();
            SecurityToken? matchingToken = null;

            foreach (var token in securityTokens)
            {
                if (_tokenService.VerifyRefreshToken(request.RefreshToken, token.RefreshTokenHash))
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
            matchingToken.RefreshTokenHash = _tokenService.HashRefreshToken(newRefreshToken);
            matchingToken.UpdatedAt = DateTime.UtcNow;
            
            await UpdateSecurityToken(matchingToken);

            return new AuthResponse
            {
                OwnerEntityId = matchingToken.UserId,
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
            var token = await GetSecurityTokenById(tokenId);
            
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

    // Helper methods - these would typically use handlers in a real implementation
    private async Task<Guid?> GetUserIdByEmail(string email)
    {
        // This would use a UserHandler or similar
        // For now, returning a placeholder
        await Task.CompletedTask;
        return Guid.NewGuid(); // Placeholder
    }

    private async Task<string?> GetUserEmailById(Guid userId)
    {
        // This would use a UserHandler or similar
        await Task.CompletedTask;
        return "user@example.com"; // Placeholder
    }

    private async Task StoreSecurityToken(SecurityToken token)
    {
        // This would use a SecurityTokenHandler
        await Task.CompletedTask;
    }

    private async Task UpdateSecurityToken(SecurityToken token)
    {
        // This would use a SecurityTokenHandler
        await Task.CompletedTask;
    }

    private async Task<SecurityToken?> GetSecurityTokenBySessionId(Guid sessionId)
    {
        // This would use a SecurityTokenHandler or query
        await Task.CompletedTask;
        return null; // Placeholder
    }

    private async Task<SecurityToken?> GetSecurityTokenById(Guid id)
    {
        // This would use a SecurityTokenHandler
        await Task.CompletedTask;
        return null; // Placeholder
    }

    private async Task<IEnumerable<SecurityToken>> GetActiveSecurityTokens()
    {
        // This would use a query to get non-revoked tokens
        await Task.CompletedTask;
        return Enumerable.Empty<SecurityToken>(); // Placeholder
    }
}