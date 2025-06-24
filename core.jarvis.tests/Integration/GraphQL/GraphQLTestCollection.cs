using Xunit;

namespace core.jarvis.tests.Integration.GraphQL
{
    /// <summary>
    /// Test collection to ensure GraphQL tests run with proper JWT configuration.
    /// This prevents environment variable conflicts between tests.
    /// </summary>
    [CollectionDefinition("GraphQL Tests")]
    public class GraphQLTestCollection : ICollectionFixture<GraphQLTestFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    /// <summary>
    /// Shared fixture for GraphQL tests to manage JWT environment variables.
    /// </summary>
    public class GraphQLTestFixture : IDisposable, IAsyncLifetime
    {
        private readonly string? _originalSecretKey;
        private readonly string? _originalIssuer;
        private readonly string? _originalAudience;
        private readonly string _testSecretKey = "GraphQL-Test-Secret-Key-Must-Be-At-Least-256-Bits-For-Security";

        public GraphQLTestFixture()
        {
            // Save original environment variable values
            _originalSecretKey = Environment.GetEnvironmentVariable("Jwt__SecretKey");
            _originalIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer");
            _originalAudience = Environment.GetEnvironmentVariable("Jwt__Audience");
        }

        public Task InitializeAsync()
        {
            // Set test JWT environment variables
            Environment.SetEnvironmentVariable("Jwt__SecretKey", _testSecretKey);
            Environment.SetEnvironmentVariable("Jwt__Issuer", "graphql-test-issuer");
            Environment.SetEnvironmentVariable("Jwt__Audience", "graphql-test-audience");
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            // Restore original environment variable values
            Environment.SetEnvironmentVariable("Jwt__SecretKey", _originalSecretKey);
            Environment.SetEnvironmentVariable("Jwt__Issuer", _originalIssuer);
            Environment.SetEnvironmentVariable("Jwt__Audience", _originalAudience);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            // Cleanup is handled in DisposeAsync
        }
    }
}