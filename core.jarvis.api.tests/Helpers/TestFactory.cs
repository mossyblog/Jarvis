using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using core.jarvis.tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace core.jarvis.api.tests.Helpers;

/// <summary>
/// Factory for creating test objects for FastEndpoints-based API testing.
/// </summary>
public static class TestFactory
{
    /// <summary>
    /// Creates an HTTP client with optional JWT authorization.
    /// </summary>
    public static HttpClient CreateClient(WebApplicationFactory<Program> factory, string? jwt = null)
    {
        var client = factory.CreateClient();

        if (!string.IsNullOrEmpty(jwt))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        }

        return client;
    }

    /// <summary>
    /// Creates an HTTP request message for testing.
    /// </summary>
    public static HttpRequestMessage CreateRequest(HttpMethod method, string url, string? body = null, string? jwt = null)
    {
        var request = new HttpRequestMessage(method, url);

        if (!string.IsNullOrEmpty(body))
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        if (!string.IsNullOrEmpty(jwt))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        }

        return request;
    }

    /// <summary>
    /// Gets the response body as a string.
    /// </summary>
    public static async Task<string> GetResponseBodyAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Creates a test JWT token for testing purposes.
    /// </summary>
    public static string CreateTestJwt(Guid userId, string email, Dictionary<string, string>? additionalClaims = null)
    {
        // For testing, create a simple base64 encoded payload (not a real JWT)
        // Real JWT validation is done by the TokenService in integration tests
        var claims = new Dictionary<string, object>
        {
            ["sub"] = userId.ToString(),
            ["email"] = email,
            ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        };

        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                claims[claim.Key] = claim.Value;
            }
        }

        var payload = System.Text.Json.JsonSerializer.Serialize(claims);
        var base64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));

        // Return a fake JWT structure for testing
        return $"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.{base64Payload}.test_signature";
    }
}

/// <summary>
/// Custom WebApplicationFactory for testing FastEndpoints API.
/// Configures test-specific settings including JWT keys and database connections.
/// </summary>
public class JarvisApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        // Inject test configuration BEFORE the app builds
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Get the test database connection string
            var testConnectionString = TestDatabaseSetup.GetConnectionString();

            // Add test configuration values
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:JarvisDb"] = testConnectionString,
                ["Jwt:Issuer"] = "jarvis-api-test",
                ["Jwt:Audience"] = "jarvis-test-clients",
                ["Jwt:SecretKey"] = "TEST_JARVIS_KEY_FOR_UNIT_TESTING_PURPOSES_ONLY_MINIMUM_256_BITS_LONG_TO_MEET_ALL_REQUIREMENTS",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "30"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Override services for testing as needed
        });
    }
}
