using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace core.jarvis.api.Services;

/// <summary>
/// Implementation of JWT token operations.
/// </summary>
public class TokenService : ITokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _secretKey;
    private readonly int _accessTokenExpirationMinutes;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public TokenService(string issuer, string audience, string secretKey, int accessTokenExpirationMinutes = 15)
    {
        _issuer = issuer;
        _audience = audience;
        _secretKey = secretKey;
        _accessTokenExpirationMinutes = accessTokenExpirationMinutes;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public string AccessToken(Guid userId, string email, Dictionary<string, string>? additionalClaims = null)
    {
        var key = Encoding.ASCII.GetBytes(_secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
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

    public string RefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

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
        catch (SecurityTokenException)
        {
            // Invalid token - return null instead of throwing
            return null;
        }
        catch (Exception)
        {
            // Token validation failed - return null instead of throwing
            return null;
        }
    }

    public string HashRefreshToken(string refreshToken)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

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
}