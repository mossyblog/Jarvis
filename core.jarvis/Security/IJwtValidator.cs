using System.Security.Claims;

namespace core.jarvis.Security;

/// <summary>
/// Interface for JWT token validation with signature verification.
/// This provides secure JWT validation for the core layer, with implementations
/// in the API layer that have access to the signing keys.
/// </summary>
public interface IJwtValidator
{
    /// <summary>
    /// Validates a JWT token including signature verification.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>ClaimsPrincipal if token is valid and signature is verified, null otherwise.</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Extracts claims from a validated JWT token as a dictionary.
    /// </summary>
    /// <param name="token">The JWT token to validate and extract claims from.</param>
    /// <returns>Dictionary of claims if token is valid, null otherwise.</returns>
    Dictionary<string, string>? GetValidatedClaims(string token);
}
