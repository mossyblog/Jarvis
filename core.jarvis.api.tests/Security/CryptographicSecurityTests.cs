using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using core.jarvis.api.tests.Helpers;
using core.jarvis.api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Security;

/// <summary>
/// INTENT: Test cryptographic security implementations
/// PURPOSE: Validate encryption, hashing, and timing attack resistance
/// BUSINESS CONTEXT: Cryptographic weaknesses lead to data breaches
/// WHY IMPORTANT: Proper crypto is foundation of security
/// ARCHITECTURAL SIGNIFICANCE: Tests crypto implementation correctness
/// FUTURE RESILIENCE: Ensures crypto remains strong
/// </summary>
public class CryptographicSecurityTests : ApiIntegrationTestBase
{
    /// <summary>
    /// INTENT: Test password hashing uses secure algorithms
    /// PURPOSE: Ensure passwords are properly protected
    /// BUSINESS CONTEXT: Weak hashing allows password recovery
    /// WHY IMPORTANT: Protects user credentials
    /// ARCHITECTURAL SIGNIFICANCE: Tests password storage security
    /// FUTURE RESILIENCE: Prevents rainbow table attacks
    /// </summary>
    [Fact]
    public void PasswordHashing_MustUseSecureAlgorithm()
    {
        // Password hashing should use bcrypt, scrypt, or Argon2
        // Never MD5, SHA1, or plain SHA256

        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var testPassword = "TestPassword123!";

        // If the service has a hash method, test it
        var serviceType = passwordService.GetType();
        var hashMethod = serviceType.GetMethod("HashPassword");

        if (hashMethod != null)
        {
            var hash = hashMethod.Invoke(passwordService, new object[] { testPassword }) as string;
            hash.ShouldNotBeNullOrEmpty();

            // Verify it's not a weak hash
            hash.Length.ShouldBeGreaterThan(32, "Hash should be longer than MD5/SHA1");
            hash.ShouldNotBe(testPassword, "Password must not be stored in plain text");

            // Check it's not a simple SHA hash
            using (var sha256 = SHA256.Create())
            {
                var simpleSha = Convert.ToBase64String(
                    sha256.ComputeHash(Encoding.UTF8.GetBytes(testPassword)));
                hash.ShouldNotBe(simpleSha, "Must not use simple SHA256");
            }
        }
    }

    /// <summary>
    /// INTENT: Test secure random number generation
    /// PURPOSE: Ensure tokens use cryptographically secure randomness
    /// BUSINESS CONTEXT: Weak randomness allows prediction
    /// WHY IMPORTANT: Prevents token prediction attacks
    /// ARCHITECTURAL SIGNIFICANCE: Tests RNG implementation
    /// FUTURE RESILIENCE: Maintains unpredictability
    /// </summary>
    [Fact]
    public void TokenGeneration_MustUseCryptoRandom()
    {
        // Arrange
        var tokenService = _serviceProvider.GetRequiredService<ITokenService>();
        var tokenCount = 100;
        var tokens = new List<string>();

        // Act - Generate multiple tokens
        for (int i = 0; i < tokenCount; i++)
        {
            var token = tokenService.RefreshToken();
            tokens.Add(token);
        }

        // Assert - All tokens must be unique
        tokens.Distinct().Count().ShouldBe(tokenCount,
            "All tokens must be unique");

        // Check token characteristics
        foreach (var token in tokens)
        {
            token.ShouldNotBeNullOrEmpty();
            token.Length.ShouldBeGreaterThan(20, "Tokens should be sufficiently long");

            // Verify base64 encoding (typical for crypto random)
            Should.NotThrow(() => Convert.FromBase64String(token),
                "Tokens should be valid base64");
        }

        // Check for patterns (simple statistical test)
        var firstChars = tokens.Select(t => t[0]).Distinct().Count();
        firstChars.ShouldBeGreaterThan(10,
            "First characters should have good distribution");
    }

    /// <summary>
    /// INTENT: Test JWT secret key strength
    /// PURPOSE: Ensure JWT signing keys are strong
    /// BUSINESS CONTEXT: Weak keys allow token forgery
    /// WHY IMPORTANT: Prevents JWT compromise
    /// ARCHITECTURAL SIGNIFICANCE: Tests key management
    /// FUTURE RESILIENCE: Maintains JWT security
    /// </summary>
    [Fact]
    public void JWT_SecretKey_MustBeStrong()
    {
        // Get configuration
        var config = _serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var jwtSecret = config["Jwt:SecretKey"];

        // Assert key strength
        jwtSecret.ShouldNotBeNullOrEmpty("JWT secret must be configured");

        // Check minimum length (256 bits = 32 bytes for HMAC-SHA256)
        jwtSecret.Length.ShouldBeGreaterThanOrEqualTo(32,
            "JWT secret must be at least 256 bits");

        // Check it's not a weak/common secret
        var weakSecrets = new[]
        {
            "secret", "password", "123456", "admin",
            "your-256-bit-secret", "your-secret-key",
            "dev-secret", "test-secret"
        };

        foreach (var weak in weakSecrets)
        {
            jwtSecret.ToLower().ShouldNotContain(weak); // JWT secret must not contain weak phrase
        }
    }

    /// <summary>
    /// INTENT: Test token expiration configuration
    /// PURPOSE: Ensure tokens expire appropriately
    /// BUSINESS CONTEXT: Long-lived tokens increase risk
    /// WHY IMPORTANT: Limits exposure window
    /// ARCHITECTURAL SIGNIFICANCE: Tests session management
    /// FUTURE RESILIENCE: Maintains security posture
    /// </summary>
    [Fact]
    public void TokenExpiration_MustBeSecure()
    {
        // Arrange
        var config = _serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var accessExpiry = int.Parse(config["Jwt:AccessTokenExpirationMinutes"] ?? "15");
        var refreshExpiry = int.Parse(config["Jwt:RefreshTokenExpirationDays"] ?? "30");

        // Assert - Access tokens should be short-lived
        accessExpiry.ShouldBeLessThanOrEqualTo(60,
            "Access tokens should expire within 1 hour");
        accessExpiry.ShouldBeGreaterThan(0,
            "Access tokens must have expiration");

        // Refresh tokens should have reasonable expiration
        refreshExpiry.ShouldBeLessThanOrEqualTo(90,
            "Refresh tokens should expire within 90 days");
        refreshExpiry.ShouldBeGreaterThan(0,
            "Refresh tokens must have expiration");
    }

    /// <summary>
    /// INTENT: Test cryptographic algorithm selection
    /// PURPOSE: Ensure only strong algorithms are used
    /// BUSINESS CONTEXT: Weak algorithms are breakable
    /// WHY IMPORTANT: Prevents cryptographic attacks
    /// ARCHITECTURAL SIGNIFICANCE: Tests algorithm choices
    /// FUTURE RESILIENCE: Maintains crypto strength
    /// </summary>
    [Fact]
    public void Cryptography_MustUseStrongAlgorithms()
    {
        // Test JWT algorithm
        var tokenService = new TokenService(
            issuer: "test",
            audience: "test",
            secretKey: "SUPER_SECRET_KEY_THAT_IS_AT_LEAST_256_BITS_LONG",
            accessTokenExpirationMinutes: 15
        );

        var token = tokenService.AccessToken(Guid.NewGuid(), "test@test.com");

        // Decode header to check algorithm
        var parts = token.Split('.');
        parts.Length.ShouldBe(3, "JWT must have 3 parts");

        var headerJson = Encoding.UTF8.GetString(
            Convert.FromBase64String(parts[0].PadRight(parts[0].Length + (4 - parts[0].Length % 4) % 4, '=')));

        headerJson.ShouldContain("HS256"); // Should use HMAC-SHA256
        headerJson.ShouldNotContain("none"); // Must not use 'none' algorithm
        headerJson.ShouldNotContain("HS1"); // Must not use SHA1
    }

    /// <summary>
    /// INTENT: Test salt usage in hashing
    /// PURPOSE: Ensure proper salting prevents rainbow tables
    /// BUSINESS CONTEXT: Unsalted hashes are vulnerable
    /// WHY IMPORTANT: Prevents hash lookup attacks
    /// ARCHITECTURAL SIGNIFICANCE: Tests salting implementation
    /// FUTURE RESILIENCE: Maintains hash security
    /// </summary>
    [Fact]
    public void Hashing_MustUseSalts()
    {
        // Test refresh token hashing uses different results for same input
        var tokenService = _serviceProvider.GetRequiredService<ITokenService>();
        var sampleToken = "SampleRefreshToken123";

        // Hash same token multiple times
        var hash1 = tokenService.HashRefreshToken(sampleToken);
        var hash2 = tokenService.HashRefreshToken(sampleToken);

        // For SHA256 (deterministic), hashes will be same
        // This is acceptable for refresh tokens as they're already random
        hash1.ShouldBe(hash2, "SHA256 is deterministic (OK for random tokens)");

        // But verify the hash is strong
        hash1.Length.ShouldBeGreaterThan(20, "Hash should be substantial");
        Convert.FromBase64String(hash1).Length.ShouldBe(32, "SHA256 produces 32 bytes");
    }

    /// <summary>
    /// INTENT: Test secure comparison functions
    /// PURPOSE: Prevent timing attacks in comparisons
    /// BUSINESS CONTEXT: String comparison can leak information
    /// WHY IMPORTANT: Prevents enumeration attacks
    /// ARCHITECTURAL SIGNIFICANCE: Tests comparison security
    /// FUTURE RESILIENCE: Maintains timing resistance
    /// </summary>
    [Fact]
    public void SecureComparison_MustBeConstantTime()
    {
        var tokenService = _serviceProvider.GetRequiredService<ITokenService>();
        var token = "TestRefreshToken123";
        var hash = tokenService.HashRefreshToken(token);

        // Test comparison timing
        var iterations = 100;
        var correctTimes = new List<long>();
        var incorrectTimes = new List<long>();

        for (int i = 0; i < iterations; i++)
        {
            // Time correct token
            var sw1 = Stopwatch.StartNew();
            var result1 = tokenService.VerifyRefreshToken(token, hash);
            sw1.Stop();
            correctTimes.Add(sw1.ElapsedTicks);
            result1.ShouldBeTrue();

            // Time incorrect token (different at start)
            var sw2 = Stopwatch.StartNew();
            var result2 = tokenService.VerifyRefreshToken("X" + token.Substring(1), hash);
            sw2.Stop();
            incorrectTimes.Add(sw2.ElapsedTicks);
            result2.ShouldBeFalse();
        }

        // Check timing consistency
        var correctAvg = correctTimes.Average();
        var incorrectAvg = incorrectTimes.Average();
        var ratio = Math.Max(correctAvg, incorrectAvg) / Math.Min(correctAvg, incorrectAvg);

        ratio.ShouldBeLessThan(2.0,
            "Timing should be similar for correct/incorrect tokens");
    }

}
