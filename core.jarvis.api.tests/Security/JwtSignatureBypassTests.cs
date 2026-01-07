using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using core.jarvis.api.Services;
using core.jarvis.Security;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace core.jarvis.api.tests.Security;

/// <summary>
/// INTENT: Verify JWT signature validation prevents forged token attacks.
/// PURPOSE: Security validation for CRIT-02 vulnerability fix (jarvis-40).
/// BUSINESS CONTEXT: Attackers could forge JWTs to impersonate users or elevate privileges.
/// WHY IMPORTANT: JWT signature validation is the only defense against forged tokens.
/// ARCHITECTURAL SIGNIFICANCE: Authentication integrity depends on signature validation.
/// FUTURE RESILIENCE: Ensures JWT validation remains secure against bypass attempts.
/// </summary>
public class JwtSignatureBypassTests
{
    private const int TestAccessTokenExpirationMinutes = 15;
    private readonly string _testSecretKey = "TEST_SECRET_KEY_WITH_AT_LEAST_256_BITS_FOR_SECURITY_TESTING_PURPOSES";
    private readonly string _wrongSecretKey = "WRONG_SECRET_KEY_WITH_AT_LEAST_256_BITS_FOR_SECURITY_TESTING_ATTACKER";
    private readonly TokenService _tokenService;
    private readonly IJwtValidator _jwtValidator;

    public JwtSignatureBypassTests()
    {
        _tokenService = new TokenService("test-issuer", "test-audience", _testSecretKey, TestAccessTokenExpirationMinutes);
        _jwtValidator = _tokenService;
    }

    #region Unsigned JWT Rejection Tests

    /// <summary>
    /// INTENT: Verify unsigned JWTs (alg: none) are rejected.
    /// PURPOSE: Prevent algorithm confusion attacks where attacker sets alg to "none".
    /// </summary>
    [Fact]
    public void ValidateToken_WithUnsignedJwt_ReturnsNull()
    {
        // Arrange - Create an unsigned JWT (alg: none attack)
        var header = Base64UrlEncode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = Base64UrlEncode($"{{\"sub\":\"{Guid.NewGuid()}\",\"email\":\"attacker@evil.com\",\"role\":\"admin\"}}");
        var unsignedJwt = $"{header}.{payload}.";

        // Act
        var result = _jwtValidator.ValidateToken(unsignedJwt);

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// INTENT: Verify GetValidatedClaims rejects unsigned JWTs.
    /// </summary>
    [Fact]
    public void GetValidatedClaims_WithUnsignedJwt_ReturnsNull()
    {
        // Arrange - Create an unsigned JWT
        var header = Base64UrlEncode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = Base64UrlEncode($"{{\"sub\":\"{Guid.NewGuid()}\",\"role\":\"admin\"}}");
        var unsignedJwt = $"{header}.{payload}.";

        // Act
        var claims = _jwtValidator.GetValidatedClaims(unsignedJwt);

        // Assert
        claims.ShouldBeNull();
    }

    #endregion

    #region Tampered JWT Rejection Tests

    /// <summary>
    /// INTENT: Verify JWTs with modified payloads are rejected.
    /// PURPOSE: Detect tampering with JWT claims (e.g., changing role to admin).
    /// </summary>
    [Fact]
    public void ValidateToken_WithTamperedPayload_ReturnsNull()
    {
        // Arrange - Create valid token then tamper with payload
        var userId = Guid.NewGuid();
        var validToken = _tokenService.AccessToken(userId, "user@example.com", new Dictionary<string, string> { { "role", "user" } });
        var parts = validToken.Split('.');

        // Tamper: change the payload to grant admin role
        var tamperedPayload = Base64UrlEncode($"{{\"sub\":\"{userId}\",\"email\":\"user@example.com\",\"role\":\"admin\"}}");
        var tamperedToken = $"{parts[0]}.{tamperedPayload}.{parts[2]}";

        // Act
        var result = _jwtValidator.ValidateToken(tamperedToken);

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// INTENT: Verify GetValidatedClaims rejects tampered JWTs.
    /// </summary>
    [Fact]
    public void GetValidatedClaims_WithTamperedPayload_ReturnsNull()
    {
        // Arrange - Create valid token then tamper
        var userId = Guid.NewGuid();
        var validToken = _tokenService.AccessToken(userId, "user@example.com");
        var parts = validToken.Split('.');

        // Tamper the payload
        var tamperedPayload = Base64UrlEncode($"{{\"sub\":\"{Guid.NewGuid()}\",\"email\":\"hacker@evil.com\"}}");
        var tamperedToken = $"{parts[0]}.{tamperedPayload}.{parts[2]}";

        // Act
        var claims = _jwtValidator.GetValidatedClaims(tamperedToken);

        // Assert
        claims.ShouldBeNull();
    }

    /// <summary>
    /// INTENT: Verify JWTs signed with wrong key are rejected.
    /// PURPOSE: Detect tokens signed by attackers with their own keys.
    /// </summary>
    [Fact]
    public void ValidateToken_WithWrongSigningKey_ReturnsNull()
    {
        // Arrange - Create token with a different secret key
        var attackerService = new TokenService("test-issuer", "test-audience", _wrongSecretKey, TestAccessTokenExpirationMinutes);
        var forgedToken = attackerService.AccessToken(Guid.NewGuid(), "attacker@evil.com", new Dictionary<string, string> { { "role", "admin" } });

        // Act - Try to validate with the correct service
        var result = _jwtValidator.ValidateToken(forgedToken);

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// INTENT: Verify GetValidatedClaims rejects tokens with wrong signing key.
    /// </summary>
    [Fact]
    public void GetValidatedClaims_WithWrongSigningKey_ReturnsNull()
    {
        // Arrange
        var attackerService = new TokenService("test-issuer", "test-audience", _wrongSecretKey, TestAccessTokenExpirationMinutes);
        var forgedToken = attackerService.AccessToken(Guid.NewGuid(), "attacker@evil.com");

        // Act
        var claims = _jwtValidator.GetValidatedClaims(forgedToken);

        // Assert
        claims.ShouldBeNull();
    }

    #endregion

    #region Valid JWT Acceptance Tests

    /// <summary>
    /// INTENT: Verify valid JWTs with correct signature are accepted.
    /// </summary>
    [Fact]
    public void ValidateToken_WithValidJwt_ReturnsClaimsPrincipal()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "valid@example.com";
        var validToken = _tokenService.AccessToken(userId, email);

        // Act
        var result = _jwtValidator.ValidateToken(validToken);

        // Assert
        result.ShouldNotBeNull();
        result.FindFirst(ClaimTypes.Email)?.Value.ShouldBe(email);
    }

    /// <summary>
    /// INTENT: Verify GetValidatedClaims returns claims for valid JWTs.
    /// </summary>
    [Fact]
    public void GetValidatedClaims_WithValidJwt_ReturnsClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "valid@example.com";
        var validToken = _tokenService.AccessToken(userId, email, new Dictionary<string, string>
        {
            { "role", "admin" },
            { "tenant_id", Guid.NewGuid().ToString() }
        });

        // Act
        var claims = _jwtValidator.GetValidatedClaims(validToken);

        // Assert
        claims.ShouldNotBeNull();
        claims.ShouldContainKey("email");
        claims["email"].ShouldBe(email);
        claims.ShouldContainKey("role");
        claims["role"].ShouldBe("admin");
        claims.ShouldContainKey("sub"); // Mapped from NameIdentifier
    }

    #endregion

    #region Malformed JWT Tests

    /// <summary>
    /// INTENT: Verify completely invalid JWT strings are rejected.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-jwt")]
    [InlineData("only.two.parts")]
    [InlineData("too.many.parts.here.now")]
    [InlineData("invalid.base64.here!@#$%")]
    public void ValidateToken_WithMalformedJwt_ReturnsNull(string malformedJwt)
    {
        // Act
        var result = _jwtValidator.ValidateToken(malformedJwt);

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// INTENT: Verify GetValidatedClaims handles malformed JWTs.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("a.b")]
    public void GetValidatedClaims_WithMalformedJwt_ReturnsNull(string malformedJwt)
    {
        // Act
        var claims = _jwtValidator.GetValidatedClaims(malformedJwt);

        // Assert
        claims.ShouldBeNull();
    }

    /// <summary>
    /// INTENT: Verify null token returns null.
    /// </summary>
    [Fact]
    public void ValidateToken_WithNullToken_ReturnsNull()
    {
        // Act
        var result = _jwtValidator.ValidateToken(null!);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region Algorithm Attack Tests

    /// <summary>
    /// INTENT: Verify JWTs with algorithm mismatch are rejected.
    /// PURPOSE: Prevent algorithm confusion/downgrade attacks.
    /// </summary>
    [Fact]
    public void ValidateToken_WithRS256Algorithm_WhenHS256Expected_ReturnsNull()
    {
        // Arrange - Create a JWT header claiming RS256 but signing won't match
        var header = Base64UrlEncode("{\"alg\":\"RS256\",\"typ\":\"JWT\"}");
        var payload = Base64UrlEncode($"{{\"sub\":\"{Guid.NewGuid()}\",\"role\":\"admin\"}}");
        // Sign with HS256 key but claim RS256 - signature won't validate
        var forgedToken = $"{header}.{payload}.forged_signature";

        // Act
        var result = _jwtValidator.ValidateToken(forgedToken);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region Helper Methods

    private static string Base64UrlEncode(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var base64 = Convert.ToBase64String(bytes);
        // Convert to Base64Url
        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    #endregion
}
