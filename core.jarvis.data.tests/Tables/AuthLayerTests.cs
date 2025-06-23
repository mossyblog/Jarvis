using Npgsql;
using Shouldly;

namespace core.jarvis.data.tests.Tables
{
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

            // Clean up test users before each test for isolation
            var cleanupSql = "DELETE FROM users WHERE email LIKE 'testuser_%@example.com';";
            using var cmd = new NpgsqlCommand(cleanupSql, _conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DisposeAsync()
        {
            // Clean up test users after each test
            var cleanupSql = "DELETE FROM users WHERE email LIKE 'testuser_%@example.com';";
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

            // First insert the user
            var insertSql = "DELETE FROM users WHERE email = @email; INSERT INTO users (email, password_hash) VALUES (@email, @hash) RETURNING id;";
            Guid userId;
            using (var cmd = new NpgsqlCommand(insertSql, _conn))
            {
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("hash", hash);
                userId = (Guid)(await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Failed to insert user"));
            }

            // Act
            var result = await _client.Authenticate(email, password);

            // Assert
            result.ShouldNotBeNull();
            // PgClient.Authenticate returns "auth.success.{userId}" on success
            result.ShouldBe($"auth.success.{userId}");
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

            var insertSql = "DELETE FROM users WHERE email = @email; INSERT INTO users (email, password_hash) VALUES (@email, @hash);";
            using (var cmd = new NpgsqlCommand(insertSql, _conn))
            {
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

        /// <summary>
        /// INTENT: Ensure minimum schema and RLS policy are present.
        /// PURPOSE: Prevents misconfiguration and enforces security.
        /// </summary>
        [Fact]
        public async Task MinimumSchema_And_RlsPolicy_Are_Enforced()
        {
            // Act
            // (Initialization already ensures schema and policy)
            var tableCheck = "SELECT to_regclass('public.users') IS NOT NULL";
            var hasUsersTable = (bool)(await new NpgsqlCommand(tableCheck, _conn).ExecuteScalarAsync() ?? false);
            var colCheck = "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='password_hash')";
            var hasPasswordHash = (bool)(await new NpgsqlCommand(colCheck, _conn).ExecuteScalarAsync() ?? false);
            var policyCheck = "SELECT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'users')";
            var hasPolicy = (bool)(await new NpgsqlCommand(policyCheck, _conn).ExecuteScalarAsync() ?? false);

            // Assert
            hasUsersTable.ShouldBeTrue();
            hasPasswordHash.ShouldBeTrue();
            hasPolicy.ShouldBeTrue();
        }
    }
}