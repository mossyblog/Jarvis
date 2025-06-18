using core.jarvis.api.Models;

namespace core.jarvis.api.Services;

/// <summary>
/// Service for handling authentication operations.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with credentials.
    /// </summary>
    /// <param name="request">The authentication request.</param>
    /// <param name="ipAddress">Client IP address.</param>
    /// <param name="userAgent">Client user agent.</param>
    /// <returns>Authentication response with tokens, or null if authentication fails.</returns>
    Task<AuthResponse?> AuthenticateAsync(AuthRequest request, string? ipAddress = null, string? userAgent = null);
    
    /// <summary>
    /// Deauthenticates a session.
    /// </summary>
    /// <param name="sessionId">The session ID to revoke.</param>
    /// <returns>True if successfully revoked.</returns>
    Task<bool> DeauthenticateAsync(Guid sessionId);
    
    /// <summary>
    /// Refreshes tokens using a refresh token.
    /// </summary>
    /// <param name="request">The refresh request.</param>
    /// <returns>New authentication response, or null if refresh fails.</returns>
    Task<AuthResponse?> RefreshTokenAsync(RefreshTokenRequest request);
    
    /// <summary>
    /// Validates an access token.
    /// </summary>
    /// <param name="tokenId">The token ID to validate.</param>
    /// <returns>Validation response with token details.</returns>
    Task<ValidationResponse> ValidateTokenAsync(Guid tokenId);
}