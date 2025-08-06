using core.jarvis.api.Models;
using core.jarvis.api.tests.Helpers;
using core.jarvis.api.Handlers;
using core.jarvis.api.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace core.jarvis.api.tests.Integration;

/// <summary>
/// Integration tests for JWT token generation with minimal claims
/// </summary>
public class JwtTokenIntegrationTests : ApiIntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify that JWT tokens only contain minimal claims (no roles)
    /// PURPOSE: To ensure token size is minimized by excluding roles
    /// BUSINESS CONTEXT: Large tokens cause performance and storage issues
    /// WHY IMPORTANT: Token bloat was identified as a key problem to solve
    /// ARCHITECTURAL SIGNIFICANCE: Validates the minimal JWT claim design
    /// FUTURE RESILIENCE: Ensures tokens remain lightweight
    /// </summary>
    [Fact]
    public async Task Authenticate_Should_Generate_Token_Without_Roles()
    {
        // Arrange
        var account = await CreateTestAccount("minimal@example.com", "TestPassword123!");
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        var profile = await profileHandler.CreateWithDefaults("minimal@example.com");
        
        // Add roles to the profile
        var roleId = Guid.NewGuid();
        await profileHandler.AssignRole(roleId);
        
        TrackEntity(account.OwnerEntityId);

        var authHandler = TestDataContext().For<AuthHandler>(account.OwnerEntityId);
        var authRequest = new Account { Email = "minimal@example.com", Password = "TestPassword123!" };

        // Act
        var authToken = await authHandler.Authenticate(authRequest);

        // Assert
        authToken.ShouldNotBeNull();
        authToken.AccessToken.ShouldNotBeNullOrEmpty();
        
        // Decode token and check claims
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(authToken.AccessToken);
        
        
        // Should have minimal claims (JWT uses short claim names)
        jsonToken.Claims.Any(c => c.Type == "nameid" || c.Type == ClaimTypes.NameIdentifier).ShouldBeTrue();
        jsonToken.Claims.Any(c => c.Type == "email" || c.Type == ClaimTypes.Email).ShouldBeTrue();
        jsonToken.Claims.Any(c => c.Type == "sessionId").ShouldBeTrue();
        
        // Should NOT have role claims
        jsonToken.Claims.Any(c => c.Type == ClaimTypes.Role).ShouldBeFalse();
        jsonToken.Claims.Any(c => c.Type == "roles").ShouldBeFalse();
    }

    /// <summary>
    /// INTENT: Verify that JWT tokens include sessionId for revocation tracking
    /// PURPOSE: To enable token revocation by tracking sessions
    /// BUSINESS CONTEXT: Compromised tokens must be revocable immediately
    /// WHY IMPORTANT: Security requires token revocation capability
    /// ARCHITECTURAL SIGNIFICANCE: Validates session tracking design
    /// FUTURE RESILIENCE: Ensures revocation mechanism is in place
    /// </summary>
    [Fact]
    public async Task Authenticate_Should_Include_SessionId_In_Token()
    {
        // Arrange
        var account = await CreateTestAccount("session@example.com", "TestPassword123!");
        TrackEntity(account.OwnerEntityId);

        var authHandler = TestDataContext().For<AuthHandler>(account.OwnerEntityId);
        var authRequest = new Account { Email = "session@example.com", Password = "TestPassword123!" };

        // Act
        var authToken = await authHandler.Authenticate(authRequest);

        // Assert
        authToken.SessionId.ShouldNotBe(Guid.Empty);
        
        // Verify sessionId is in JWT claims
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(authToken.AccessToken);
        var sessionIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "sessionId");
        
        sessionIdClaim.ShouldNotBeNull();
        sessionIdClaim.Value.ShouldBe(authToken.SessionId.ToString());
    }

    /// <summary>
    /// INTENT: Verify that permissions are NOT included in JWT tokens
    /// PURPOSE: To ensure permissions are resolved server-side
    /// BUSINESS CONTEXT: Permissions change frequently and shouldn't be cached in tokens
    /// WHY IMPORTANT: Prevents stale permission issues
    /// ARCHITECTURAL SIGNIFICANCE: Validates server-side permission resolution design
    /// FUTURE RESILIENCE: Ensures permissions remain dynamic
    /// </summary>
    [Fact]
    public async Task Authenticate_Should_Not_Include_Permissions_In_Token()
    {
        // Arrange
        var account = await CreateTestAccount("noperms@example.com", "TestPassword123!");
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        var profile = await profileHandler.CreateWithDefaults("noperms@example.com");
        
        // Add permissions to the profile
        var updatedProfile = profile with 
        { 
            PermissionIds = new[] { "admin.users.read", "admin.users.write" }
        };
        await TestDataContext().Commit(updatedProfile);
        
        TrackEntity(account.OwnerEntityId);

        var authHandler = TestDataContext().For<AuthHandler>(account.OwnerEntityId);
        var authRequest = new Account { Email = "noperms@example.com", Password = "TestPassword123!" };

        // Act
        var authToken = await authHandler.Authenticate(authRequest);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(authToken.AccessToken);
        
        // Should NOT have permission claims
        jsonToken.Claims.Any(c => c.Type == "permissions").ShouldBeFalse();
        jsonToken.Claims.Any(c => c.Type == "permission").ShouldBeFalse();
        jsonToken.Claims.Any(c => c.Value.Contains("admin.users")).ShouldBeFalse();
    }

    /// <summary>
    /// INTENT: Verify that token size is significantly smaller without roles/permissions
    /// PURPOSE: To validate that token bloat issue is resolved
    /// BUSINESS CONTEXT: Large tokens cause performance issues in HTTP headers
    /// WHY IMPORTANT: Token size was a primary concern in the refactoring
    /// ARCHITECTURAL SIGNIFICANCE: Validates the minimal token design
    /// FUTURE RESILIENCE: Ensures tokens remain efficient
    /// </summary>
    [Fact]
    public async Task Authenticate_Should_Generate_Small_Tokens()
    {
        // Arrange
        var account = await CreateTestAccount("small@example.com", "TestPassword123!");
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        var profile = await profileHandler.CreateWithDefaults("small@example.com");
        
        // Add many roles and permissions to show they don't affect token size
        for (int i = 0; i < 10; i++)
        {
            await profileHandler.AssignRole(Guid.NewGuid());
        }
        
        var updatedProfile = await profileHandler.Get();
        var manyPermissions = Enumerable.Range(1, 50)
            .Select(i => $"permission.{i}")
            .ToArray();
        updatedProfile = updatedProfile! with { PermissionIds = manyPermissions };
        await TestDataContext().Commit(updatedProfile);
        
        TrackEntity(account.OwnerEntityId);

        var authHandler = TestDataContext().For<AuthHandler>(account.OwnerEntityId);
        var authRequest = new Account { Email = "small@example.com", Password = "TestPassword123!" };

        // Act
        var authToken = await authHandler.Authenticate(authRequest);

        // Assert
        // Token should be relatively small despite many roles/permissions
        authToken.AccessToken.Length.ShouldBeLessThan(1000); // Typical JWT with minimal claims
    }

    private async Task<Account> CreateTestAccount(string email, string password)
    {
        var entityId = Guid.NewGuid();
        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword(password);
        
        var account = new Account
        {
            OwnerEntityId = entityId,
            Email = email,
            PasswordHash = hashedPassword,
            Password = "", // Not persisted
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(account);
        return account;
    }
}