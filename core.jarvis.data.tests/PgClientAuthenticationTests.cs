using Npgsql;
using Shouldly;
using Xunit;
using Dapper;

namespace core.jarvis.data.tests;

// NOTE: These tests have been disabled because PgClient.Authenticate has been removed.
// Authentication is now handled through the ECS framework's AuthHandler.
// See core.jarvis.api.tests/Integration/AuthSystemIntegrationTests.cs for authentication tests.
/*
public class PgClientAuthenticationTests : IAsyncLifetime
{
    private NpgsqlConnection _connection = null!;
    private PgClient _pgClient = null!;
    
    public async Task InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_URL") ?? 
            "Host=localhost;Port=5432;Database=jarvis;Username=postgres;Password=postgres";
        
        _connection = new NpgsqlConnection(connectionString);
        await _connection.OpenAsync();
        
        // Ensure account table exists with proper structure
        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS account (
                id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
                owner_entity_id UUID NOT NULL,
                email VARCHAR(255) NOT NULL UNIQUE,
                password_hash VARCHAR(255) NOT NULL,
                password VARCHAR(255) DEFAULT '',
                auth_method VARCHAR(50) NOT NULL DEFAULT 'password',
                is_active BOOLEAN NOT NULL DEFAULT true,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
            )");
        
        // Enable RLS with permissive policy
        await _connection.ExecuteAsync(@"
            ALTER TABLE account ENABLE ROW LEVEL SECURITY;
            DROP POLICY IF EXISTS account_all_access ON account;
            CREATE POLICY account_all_access ON account FOR ALL USING (true) WITH CHECK (true);");
        
        _pgClient = new PgClient(_connection);
    }
    
    public async Task DisposeAsync()
    {
        await _connection.CloseAsync();
        _connection.Dispose();
    }
    
    /// <summary>
    /// INTENT: Verify that PgClient.Authenticate returns null when account doesn't exist
    /// PURPOSE: Ensure authentication fails gracefully for non-existent users
    /// BUSINESS CONTEXT: Security requires proper handling of invalid login attempts
    /// WHY IMPORTANT: Prevents security vulnerabilities from improper error handling
    /// ARCHITECTURAL SIGNIFICANCE: Validates PgClient's authentication contract
    /// FUTURE RESILIENCE: Protects against authentication bypass attempts
    /// </summary>
    [Fact]
    public async Task Authenticate_WithNonExistentEmail_ReturnsNull()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "anypassword";
        
        // Act
        var result = await _pgClient.Authenticate(email, password);
        
        // Assert
        result.ShouldBeNull();
    }
    
    /// <summary>
    /// INTENT: Verify that PgClient.Authenticate works with valid credentials
    /// PURPOSE: Ensure authentication succeeds for valid users with correct passwords
    /// BUSINESS CONTEXT: Core authentication flow must work for users to access the system
    /// WHY IMPORTANT: This is the primary authentication mechanism for the entire platform
    /// ARCHITECTURAL SIGNIFICANCE: Validates the core authentication pipeline
    /// FUTURE RESILIENCE: Ensures authentication continues to work as expected
    /// </summary>
    [Fact]
    public async Task Authenticate_WithValidCredentials_ReturnsAuthToken()
    {
        // Arrange
        var email = "test@example.com";
        var password = "TestPassword123!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, 10);
        var ownerEntityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        // Clean up any existing test account
        await _connection.ExecuteAsync("DELETE FROM account WHERE email = @email", new { email });
        
        // Insert test account
        await _connection.ExecuteAsync(@"
            INSERT INTO account (
                owner_entity_id, email, password_hash, auth_method, is_active
            ) VALUES (
                @ownerEntityId, @email, @passwordHash, 'password', true
            )", new { ownerEntityId, email, passwordHash = hashedPassword });
        
        // Act
        var result = await _pgClient.Authenticate(email, password);
        
        // Assert
        result.ShouldNotBeNull();
        result.ShouldStartWith("auth.success.");
        result.ShouldBe($"auth.success.{ownerEntityId}");
    }
    
    /// <summary>
    /// INTENT: Verify that PgClient.Authenticate returns null for incorrect password
    /// PURPOSE: Ensure authentication fails when password doesn't match
    /// BUSINESS CONTEXT: Security requires rejecting invalid passwords
    /// WHY IMPORTANT: Prevents unauthorized access with wrong credentials
    /// ARCHITECTURAL SIGNIFICANCE: Validates BCrypt password verification
    /// FUTURE RESILIENCE: Ensures password security remains intact
    /// </summary>
    [Fact]
    public async Task Authenticate_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var email = "test2@example.com";
        var correctPassword = "CorrectPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(correctPassword, 10);
        var ownerEntityId = Guid.NewGuid();
        
        await _connection.ExecuteAsync("DELETE FROM account WHERE email = @email", new { email });
        await _connection.ExecuteAsync(@"
            INSERT INTO account (
                owner_entity_id, email, password_hash, auth_method, is_active
            ) VALUES (
                @ownerEntityId, @email, @passwordHash, 'password', true
            )", new { ownerEntityId, email, passwordHash = hashedPassword });
        
        // Act
        var result = await _pgClient.Authenticate(email, wrongPassword);
        
        // Assert
        result.ShouldBeNull();
    }
    
    /// <summary>
    /// INTENT: Verify that PgClient.Authenticate returns null for inactive accounts
    /// PURPOSE: Ensure authentication fails for deactivated users
    /// BUSINESS CONTEXT: Administrators must be able to disable accounts
    /// WHY IMPORTANT: Prevents access by suspended or terminated users
    /// ARCHITECTURAL SIGNIFICANCE: Validates account status checking
    /// FUTURE RESILIENCE: Ensures account deactivation remains effective
    /// </summary>
    [Fact]
    public async Task Authenticate_WithInactiveAccount_ReturnsNull()
    {
        // Arrange
        var email = "inactive@example.com";
        var password = "TestPassword123!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, 10);
        var ownerEntityId = Guid.NewGuid();
        
        await _connection.ExecuteAsync("DELETE FROM account WHERE email = @email", new { email });
        await _connection.ExecuteAsync(@"
            INSERT INTO account (
                owner_entity_id, email, password_hash, auth_method, is_active
            ) VALUES (
                @ownerEntityId, @email, @passwordHash, 'password', false
            )", new { ownerEntityId, email, passwordHash = hashedPassword, isActive = false });
        
        // Act
        var result = await _pgClient.Authenticate(email, password);
        
        // Assert
        result.ShouldBeNull();
    }
}
*/