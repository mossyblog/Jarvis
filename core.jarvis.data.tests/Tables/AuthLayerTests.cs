using Npgsql;
using Shouldly;

namespace core.jarvis.data.tests.Tables
{
    // NOTE: These tests have been disabled because PgClient.Authenticate has been removed.
    // Authentication is now handled through the ECS framework's AuthHandler.
    // See core.jarvis.api.tests/Integration/AuthSystemIntegrationTests.cs for authentication tests.
    /*
    /// <summary>
    /// INTENT: Validate authentication layer, schema enforcement, and password hashing.
    /// PURPOSE: Ensure PgClientFactory creates/updates schema and PgClient authenticates users securely.
    /// BUSINESS CONTEXT: Foundation for secure user authentication and RBAC/RLS enforcement.
    /// WHY IMPORTANT: Prevents misconfiguration and enforces security best practices.
    /// ARCHITECTURAL SIGNIFICANCE: Ensures minimum schema and policies are always present.
    /// FUTURE RESILIENCE: Protects against regressions in authentication and schema setup.
    /// </summary>
    public class AuthLayerTests : IAsyncLifetime
    {
        private NpgsqlConnection _conn = null!;
        private PgClient _client = null!;

        public async Task InitializeAsync()
        {
            var connString = TestHelpers.GetConnectionStringFromEnv();
            _conn = new NpgsqlConnection(connString);
            // Ensure schema and create PgClient
            _client = await PgClientFactory.Create(_conn);

            // Clean up test accounts before each test for isolation
            var cleanupSql = "DELETE FROM \"account\" WHERE email LIKE 'testuser_%@example.com';";
            using var cmd = new NpgsqlCommand(cleanupSql, _conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DisposeAsync()
        {
            // Clean up test accounts after each test
            var cleanupSql = "DELETE FROM \"account\" WHERE email LIKE 'testuser_%@example.com';";
            using var cmd = new NpgsqlCommand(cleanupSql, _conn);
            await cmd.ExecuteNonQueryAsync();
            await _conn.CloseAsync();
        }

        /// <summary>
        /// INTENT: Validate user creation, password hashing, and authentication.
        /// PURPOSE: Ensure only correct credentials succeed.
        /// </summary>
        [Fact]
        public async Task User_Can_Be_Created_And_Authenticated()
        {
            // Arrange
            var email = $"testuser_{Guid.NewGuid():N}@example.com";
            var password = "SuperSecret123!";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            // First insert the account (account table requires owner_entity_id and is_active)
            var userId = Guid.NewGuid();
            var insertSql = "DELETE FROM \"account\" WHERE email = @email; INSERT INTO \"account\" (owner_entity_id, email, password_hash, is_active) VALUES (@userId, @email, @hash, true) RETURNING owner_entity_id;";
            using (var cmd = new NpgsqlCommand(insertSql, _conn))
            {
                cmd.Parameters.AddWithValue("userId", userId);
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("hash", hash);
                var result = await cmd.ExecuteScalarAsync();
                userId = (Guid)(result ?? throw new InvalidOperationException("Failed to insert account"));
            }

            // Act
            var authResult = await _client.Authenticate(email, password);

            // Assert
            authResult.ShouldNotBeNull();
            // PgClient.Authenticate returns "auth.success.{userId}" on success
            authResult.ShouldBe($"auth.success.{userId}");
        }

        /// <summary>
        /// INTENT: Ensure authentication fails with wrong password.
        /// PURPOSE: Prevents unauthorized access.
        /// </summary>
        [Fact]
        public async Task Authentication_Fails_With_Wrong_Password()
        {
            // Arrange
            var email = $"testuser_{Guid.NewGuid():N}@example.com";
            var password = "CorrectPassword!";
            var wrongPassword = "WrongPassword!";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            var userId = Guid.NewGuid();
            var insertSql = "DELETE FROM \"account\" WHERE email = @email; INSERT INTO \"account\" (owner_entity_id, email, password_hash, is_active) VALUES (@userId, @email, @hash, true);";
            using (var cmd = new NpgsqlCommand(insertSql, _conn))
            {
                cmd.Parameters.AddWithValue("userId", userId);
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("hash", hash);
                await cmd.ExecuteNonQueryAsync();
            }

            // Act
            var jwt = await _client.Authenticate(email, wrongPassword);

            // Assert
            jwt.ShouldBeNull();
        }

        /// <summary>
        /// INTENT: Ensure authentication fails for non-existent user.
        /// PURPOSE: Prevents authentication bypass.
        /// </summary>
        [Fact]
        public async Task Authentication_Fails_For_NonExistent_User()
        {
            // Arrange
            var email = $"testuser_{Guid.NewGuid():N}@example.com";
            var password = "DoesNotMatter";

            // Act
            var jwt = await _client.Authenticate(email, password);

            // Assert
            jwt.ShouldBeNull();
        }
    }
    */
}