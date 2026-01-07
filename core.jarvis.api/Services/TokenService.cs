using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using core.jarvis.Security;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace core.jarvis.api.Services;

/// <summary>
/// Implementation of JWT token operations with full signature validation.
/// </summary>
public class TokenService : ITokenService, IJwtValidator
{
    /// <summary>
    /// Default access token expiration time in minutes.
    /// Balances security and user experience for JWT tokens.
    /// </summary>
    private const int DefaultAccessTokenExpirationMinutes = 15;
    
    /// <summary>
    /// Size in bytes for cryptographically secure refresh tokens.
    /// Provides sufficient entropy for secure random token generation.
    /// </summary>
    private const int RefreshTokenSizeBytes = 32;
    
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _secretKey;
    private readonly int _accessTokenExpirationMinutes;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly ILogger<TokenService> _logger;

    /// <summary>
    /// Initializes a new instance of the TokenService with the specified configuration.
    /// </summary>
    /// <param name="issuer">The token issuer identifier used in JWT claims</param>
    /// <param name="audience">The intended audience for the tokens</param>
    /// <param name="secretKey">The secret key used for token signing and validation</param>
    /// <param name="accessTokenExpirationMinutes">The expiration time for access tokens in minutes (default: 15 minutes)</param>
    /// <param name="logger">Optional logger instance for logging token operations</param>
    public TokenService(string issuer, string audience, string secretKey, int accessTokenExpirationMinutes = DefaultAccessTokenExpirationMinutes, ILogger<TokenService>? logger = null)
    {
        _issuer = issuer;
        _audience = audience;
        _secretKey = secretKey;
        _accessTokenExpirationMinutes = accessTokenExpirationMinutes;
        _tokenHandler = new JwtSecurityTokenHandler();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TokenService>.Instance;
    }

    /// <summary>
    /// Creates a JWT access token with the specified user information and optional additional claims.
    /// The token includes standard claims (user ID, email, JTI, IAT) and is signed with HMAC-SHA256.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to include in the token</param>
    /// <param name="email">The user's email address to include in the token</param>
    /// <param name="additionalClaims">Optional dictionary of additional claims to include in the token</param>
    /// <returns>A signed JWT access token string valid for the configured expiration time</returns>
    public string AccessToken(Guid userId, string email, Dictionary<string, string>? additionalClaims = null)
    {
        var key = Encoding.ASCII.GetBytes(_secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            }),
            Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        // Add additional claims if provided
        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                tokenDescriptor.Subject.AddClaim(new Claim(claim.Key, claim.Value));
            }
        }

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically secure refresh token using a random number generator.
    /// The token is base64-encoded and contains 32 bytes of entropy for maximum security.
    /// </summary>
    /// <returns>A base64-encoded refresh token string suitable for secure storage and transmission</returns>
    public string RefreshToken()
    {
        var randomNumber = new byte[RefreshTokenSizeBytes];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Validates a JWT token and extracts the claims principal if the token is valid.
    /// Performs comprehensive validation including signature, issuer, audience, lifetime, and algorithm verification.
    /// </summary>
    /// <param name="token">The JWT token string to validate</param>
    /// <returns>
    /// ClaimsPrincipal containing the token's claims if validation succeeds; null if validation fails.
    /// Returns null for invalid, expired, or malformed tokens.
    /// </returns>
    /// <remarks>
    /// This method validates:
    /// - Token signature using the configured secret key
    /// - Issuer and audience claims
    /// - Token expiration (with zero clock skew)
    /// - Signing algorithm (must be HMAC-SHA256)
    /// - Token structure and format
    /// All validation failures are logged for security monitoring.
    /// </remarks>
    public ClaimsPrincipal? Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var key = Encoding.ASCII.GetBytes(_secretKey);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            
            // Additional validation to ensure it's a JWT
            if (!(validatedToken is JwtSecurityToken jwtToken))
            {
                return null;
            }

            // Verify the signing algorithm
            if (!jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch (SecurityTokenException ex)
        {
            // Invalid token - log the security issue and return null instead of throwing
            _logger.LogWarning(ex, "Security token validation failed for token");
            return null;
        }
        catch (Exception ex)
        {
            // Unexpected token validation error - log the issue and return null instead of throwing
            _logger.LogError(ex, "Unexpected error during token validation");
            return null;
        }
    }

    /// <summary>
    /// Computes a SHA-256 hash of the refresh token for secure storage in the database.
    /// The hash is base64-encoded for storage and lookup operations.
    /// </summary>
    /// <param name="refreshToken">The refresh token string to hash</param>
    /// <returns>A base64-encoded SHA-256 hash of the refresh token</returns>
    public string HashRefreshToken(string refreshToken)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Verifies a refresh token against its stored hash using constant-time comparison to prevent timing attacks.
    /// Computes the hash of the provided token and compares it with the stored hash.
    /// </summary>
    /// <param name="refreshToken">The refresh token to verify</param>
    /// <param name="hashedToken">The stored hash to compare against</param>
    /// <returns>True if the refresh token matches the hash; false otherwise</returns>
    /// <remarks>
    /// Uses constant-time comparison to prevent timing-based attacks that could leak information
    /// about the stored hash through execution time differences.
    /// </remarks>
    public bool VerifyRefreshToken(string refreshToken, string hashedToken)
    {
        var computedHash = HashRefreshToken(refreshToken);

        // Use constant-time comparison to prevent timing attacks
        if (computedHash.Length != hashedToken.Length)
        {
            return false;
        }

        var result = 0;
        for (int i = 0; i < computedHash.Length; i++)
        {
            result |= computedHash[i] ^ hashedToken[i];
        }

        return result == 0;
    }

    #region IJwtValidator Implementation

    /// <summary>
    /// Validates a JWT token including signature verification.
    /// Implementation of IJwtValidator.ValidateToken - delegates to Validate method.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>ClaimsPrincipal if token is valid and signature is verified, null otherwise.</returns>
    ClaimsPrincipal? IJwtValidator.ValidateToken(string token)
    {
        return Validate(token);
    }

    /// <summary>
    /// Extracts claims from a validated JWT token as a dictionary.
    /// Only returns claims if the token signature is valid.
    /// </summary>
    /// <param name="token">The JWT token to validate and extract claims from.</param>
    /// <returns>Dictionary of claims if token is valid, null otherwise.</returns>
    public Dictionary<string, string>? GetValidatedClaims(string token)
    {
        var principal = Validate(token);
        if (principal == null)
        {
            return null;
        }

        // Convert ClaimsPrincipal to Dictionary<string, string>
        var claims = new Dictionary<string, string>();
        foreach (var claim in principal.Claims)
        {
            // Handle duplicate claim types by taking the first value
            if (!claims.ContainsKey(claim.Type))
            {
                claims[claim.Type] = claim.Value;
            }

            // Also map standard claim types to their short names for compatibility
            // e.g., http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier -> sub
            var shortName = GetShortClaimName(claim.Type);
            if (shortName != claim.Type && !claims.ContainsKey(shortName))
            {
                claims[shortName] = claim.Value;
            }
        }

        return claims;
    }

    /// <summary>
    /// Maps long claim type URIs to short claim names for compatibility.
    /// </summary>
    private static string GetShortClaimName(string claimType)
    {
        return claimType switch
        {
            ClaimTypes.NameIdentifier => "sub",
            ClaimTypes.Email => "email",
            ClaimTypes.Role => "role",
            ClaimTypes.Name => "name",
            _ => claimType
        };
    }

    #endregion
}