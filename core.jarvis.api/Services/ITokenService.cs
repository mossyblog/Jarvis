using System.Security.Claims;

namespace core.jarvis.api.Services;

/// <summary>
/// Service for JWT token operations.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT access token.
    /// </summary>
    /// <param name="userId">User ID to include in token.</param>
    /// <param name="email">User email to include in token.</param>
    /// <param name="claims">Additional claims to include.</param>
    /// <returns>JWT token string.</returns>
    string GenerateAccessToken(Guid userId, string email, Dictionary<string, string>? claims = null);

    /// <summary>
    /// Generates a refresh token.
    /// </summary>
    /// <returns>Refresh token string.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates and extracts claims from a JWT token.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>Claims principal if valid, null otherwise.</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Hashes a refresh token for secure storage.
    /// </summary>
    /// <param name="refreshToken">The refresh token to hash.</param>
    /// <returns>Hashed token.</returns>
    string HashRefreshToken(string refreshToken);

    /// <summary>
    /// Verifies a refresh token against its hash.
    /// </summary>
    /// <param name="refreshToken">The refresh token to verify.</param>
    /// <param name="hashedToken">The stored hash.</param>
    /// <returns>True if tokens match.</returns>
    bool VerifyRefreshToken(string refreshToken, string hashedToken);
}