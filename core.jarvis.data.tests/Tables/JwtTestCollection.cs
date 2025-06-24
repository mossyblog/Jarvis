using Xunit;

namespace core.jarvis.data.tests.Tables
{
    /// <summary>
    /// Test collection to ensure JWT-related tests run sequentially.
    /// This prevents environment variable conflicts between tests.
    /// </summary>
    [CollectionDefinition("JWT Tests")]
    public class JwtTestCollection : ICollectionFixture<JwtTestFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    /// <summary>
    /// Shared fixture for JWT tests to manage environment variables.
    /// </summary>
    public class JwtTestFixture : IDisposable
    {
        private readonly string? _originalSecretKey;
        private readonly string? _originalIssuer;
        private readonly string? _originalAudience;

        public JwtTestFixture()
        {
            // Save original environment variable values
            _originalSecretKey = Environment.GetEnvironmentVariable("Jwt__SecretKey");
            _originalIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer");
            _originalAudience = Environment.GetEnvironmentVariable("Jwt__Audience");
        }

        public void Dispose()
        {
            // Restore original environment variable values
            Environment.SetEnvironmentVariable("Jwt__SecretKey", _originalSecretKey);
            Environment.SetEnvironmentVariable("Jwt__Issuer", _originalIssuer);
            Environment.SetEnvironmentVariable("Jwt__Audience", _originalAudience);
        }
    }
}