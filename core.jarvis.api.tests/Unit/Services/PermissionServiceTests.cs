using core.jarvis.api.Services;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.api.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Unit.Services;

/// <summary>
/// Tests for the PermissionService implementation
/// </summary>
public class PermissionServiceTests : ApiIntegrationTestBase
{
    private IPermissionService _permissionService = null!;
    private IMemoryCache _memoryCache = null!;

    public PermissionServiceTests()
    {
        _permissionService = _serviceProvider.GetRequiredService<IPermissionService>();
        _memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
    }

    /// <summary>
    /// INTENT: Verify that PermissionService correctly retrieves a user's SecurityProfile
    /// PURPOSE: To ensure the service can fetch user security data from the database
    /// BUSINESS CONTEXT: Permission checks require loading user's security profile to determine access rights
    /// WHY IMPORTANT: Without proper profile retrieval, permission checks cannot function
    /// ARCHITECTURAL SIGNIFICANCE: Validates the integration between PermissionService and data layer
    /// FUTURE RESILIENCE: Ensures changes to data access don't break permission resolution
    /// </summary>
    [Fact]
    public async Task GetSecurityProfileAsync_Should_Return_Profile_For_Valid_User()
    {
        // Arrange
        var email = $"test_profile_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email, "TestPassword123!");
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        var expectedProfile = await profileHandler.CreateWithDefaults(email);
        TrackEntity(account.OwnerEntityId);

        // Act
        var profile = await _permissionService.GetSecurityProfileAsync(account.OwnerEntityId);

        // Assert
        profile.ShouldNotBeNull();
        profile.OwnerEntityId.ShouldBe(account.OwnerEntityId);
        profile.Name.ShouldBe(expectedProfile.Name);
    }

    /// <summary>
    /// INTENT: Verify that PermissionService returns null for non-existent users
    /// PURPOSE: To ensure the service handles missing users gracefully
    /// BUSINESS CONTEXT: System must handle requests for users that don't exist without errors
    /// WHY IMPORTANT: Prevents crashes when checking permissions for invalid user IDs
    /// ARCHITECTURAL SIGNIFICANCE: Validates error handling in the permission layer
    /// FUTURE RESILIENCE: Ensures robust handling of edge cases in permission checks
    /// </summary>
    [Fact]
    public async Task GetSecurityProfileAsync_Should_Return_Null_For_NonExistent_User()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var profile = await _permissionService.GetSecurityProfileAsync(nonExistentUserId);

        // Assert
        profile.ShouldBeNull();
    }

    /// <summary>
    /// INTENT: Verify that PermissionService correctly caches SecurityProfile data
    /// PURPOSE: To ensure repeated permission checks don't hit the database unnecessarily
    /// BUSINESS CONTEXT: High-frequency permission checks need to be performant
    /// WHY IMPORTANT: Caching reduces database load and improves API response times
    /// ARCHITECTURAL SIGNIFICANCE: Validates the caching layer implementation
    /// FUTURE RESILIENCE: Ensures performance optimizations work as designed
    /// </summary>
    [Fact]
    public async Task GetSecurityProfileAsync_Should_Cache_Profile_Data()
    {
        // Arrange
        var email = $"test_cache_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email, "TestPassword123!");
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.CreateWithDefaults(email);
        TrackEntity(account.OwnerEntityId);

        // Act - First call should hit database
        var profile1 = await _permissionService.GetSecurityProfileAsync(account.OwnerEntityId);
        
        // Modify the profile directly in database
        var updatedProfile = profile1! with { Name = "ModifiedName" };
        await TestDataContext().Commit(updatedProfile);
        
        // Second call should return cached value (not the modified one)
        var profile2 = await _permissionService.GetSecurityProfileAsync(account.OwnerEntityId);

        // Assert
        profile2.ShouldNotBeNull();
        profile2.Name.ShouldBe(profile1.Name); // Should be cached value, not modified
        profile2.Name.ShouldNotBe("ModifiedName");
    }

    /// <summary>
    /// INTENT: Verify that cache invalidation removes cached SecurityProfile data
    /// PURPOSE: To ensure permission changes take effect immediately when cache is invalidated
    /// BUSINESS CONTEXT: Admin changes to user permissions must be reflected immediately
    /// WHY IMPORTANT: Stale cached permissions could lead to security vulnerabilities
    /// ARCHITECTURAL SIGNIFICANCE: Validates cache invalidation mechanism
    /// FUTURE RESILIENCE: Ensures permission updates propagate correctly
    /// </summary>
    [Fact]
    public async Task InvalidateCacheAsync_Should_Remove_Cached_Profile()
    {
        // Arrange
        var email = $"test_invalidate_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email, "TestPassword123!");
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.CreateWithDefaults(email);
        TrackEntity(account.OwnerEntityId);

        // Act
        // First call to cache the profile
        var profile1 = await _permissionService.GetSecurityProfileAsync(account.OwnerEntityId);
        
        // Modify the profile
        var updatedProfile = profile1! with { Name = "UpdatedName" };
        await TestDataContext().Commit(updatedProfile);
        
        // Invalidate cache
        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);
        
        // Get profile again - should hit database and get updated value
        var profile2 = await _permissionService.GetSecurityProfileAsync(account.OwnerEntityId);

        // Assert
        profile2.ShouldNotBeNull();
        profile2.Name.ShouldBe("UpdatedName");
    }

    /// <summary>
    /// INTENT: Verify that HasPermissionAsync correctly checks direct permissions
    /// PURPOSE: To ensure users with explicit permissions are granted access
    /// BUSINESS CONTEXT: Users need access to resources based on assigned permissions
    /// WHY IMPORTANT: Core authorization functionality depends on accurate permission checks
    /// ARCHITECTURAL SIGNIFICANCE: Validates the permission checking logic
    /// FUTURE RESILIENCE: Ensures permission-based access control works correctly
    /// </summary>
    [Fact]
    public async Task HasPermissionAsync_Should_Return_True_For_Direct_Permission()
    {
        // Arrange
        var email = $"test_direct_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email, "TestPassword123!");
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        var profile = await profileHandler.CreateWithDefaults(email);
        
        // Add a specific permission
        var permissionId = "test.permission.read";
        var updatedProfile = profile with 
        { 
            PermissionIds = profile.PermissionIds.Concat(new[] { permissionId }).ToArray() 
        };
        await TestDataContext().Commit(updatedProfile);
        TrackEntity(account.OwnerEntityId);
        
        // Invalidate cache to ensure fresh data
        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Act
        var hasPermission = await _permissionService.HasPermissionAsync(account.OwnerEntityId, permissionId);

        // Assert
        hasPermission.ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify that HasPermissionAsync returns false for missing permissions
    /// PURPOSE: To ensure users without specific permissions are denied access
    /// BUSINESS CONTEXT: Access control must deny users without proper permissions
    /// WHY IMPORTANT: Security depends on denying access when permissions are absent
    /// ARCHITECTURAL SIGNIFICANCE: Validates permission denial logic
    /// FUTURE RESILIENCE: Ensures unauthorized access is prevented
    /// </summary>
    [Fact]
    public async Task HasPermissionAsync_Should_Return_False_For_Missing_Permission()
    {
        // Arrange
        var email = $"test_missing_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email, "TestPassword123!");
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.CreateWithDefaults(email);
        TrackEntity(account.OwnerEntityId);

        // Act
        var hasPermission = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "non.existent.permission");

        // Assert
        hasPermission.ShouldBeFalse();
    }

    /// <summary>
    /// INTENT: Verify that hierarchical permissions work correctly (admin grants admin.users.read)
    /// PURPOSE: To ensure parent permissions grant access to child permissions
    /// BUSINESS CONTEXT: Admin users should have access to all admin sub-permissions
    /// WHY IMPORTANT: Simplifies permission management by using hierarchical structures
    /// ARCHITECTURAL SIGNIFICANCE: Validates the hierarchical permission matching logic
    /// FUTURE RESILIENCE: Ensures permission inheritance works as designed
    /// </summary>
    [Fact]
    public async Task HasPermissionAsync_Should_Support_Hierarchical_Permissions()
    {
        // Arrange
        var email = $"test_hierarchy_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email, "TestPassword123!");
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        var profile = await profileHandler.CreateWithDefaults(email);
        
        // Add parent permission "admin"
        var updatedProfile = profile with 
        { 
            PermissionIds = new[] { "admin" }
        };
        await TestDataContext().Commit(updatedProfile);
        TrackEntity(account.OwnerEntityId);

        // Act - Check for child permissions
        var hasAdminUsers = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "admin.users");
        var hasAdminUsersRead = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "admin.users.read");
        var hasAdminRoles = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "admin.roles");

        // Assert
        hasAdminUsers.ShouldBeTrue();
        hasAdminUsersRead.ShouldBeTrue();
        hasAdminRoles.ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify that wildcard permissions work correctly (admin.users.* grants all user operations)
    /// PURPOSE: To ensure wildcard permissions grant access to all matching sub-permissions
    /// BUSINESS CONTEXT: Admins may need all permissions within a specific domain
    /// WHY IMPORTANT: Simplifies permission assignment for broad access needs
    /// ARCHITECTURAL SIGNIFICANCE: Validates wildcard permission matching
    /// FUTURE RESILIENCE: Ensures flexible permission patterns work correctly
    /// </summary>
    [Fact]
    public async Task HasPermissionAsync_Should_Support_Wildcard_Permissions()
    {
        // Arrange
        var email = $"test_wildcard_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email, "TestPassword123!");
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        var profile = await profileHandler.CreateWithDefaults(email);
        
        // Add wildcard permission
        var updatedProfile = profile with 
        { 
            PermissionIds = new[] { "admin.users.*" }
        };
        await TestDataContext().Commit(updatedProfile);
        TrackEntity(account.OwnerEntityId);

        // Act
        var hasRead = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "admin.users.read");
        var hasWrite = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "admin.users.write");
        var hasDelete = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "admin.users.delete");
        var hasOther = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "admin.roles.read");

        // Assert
        hasRead.ShouldBeTrue();
        hasWrite.ShouldBeTrue();
        hasDelete.ShouldBeTrue();
        hasOther.ShouldBeFalse(); // Should not match different domain
    }

    /// <summary>
    /// INTENT: Verify that HasAnyPermissionAsync returns true if user has any of the requested permissions
    /// PURPOSE: To support OR logic in permission checks
    /// BUSINESS CONTEXT: Some operations may be allowed with any of several permissions
    /// WHY IMPORTANT: Enables flexible permission requirements for endpoints
    /// ARCHITECTURAL SIGNIFICANCE: Validates OR permission logic
    /// FUTURE RESILIENCE: Ensures multiple permission patterns work correctly
    /// </summary>
    [Fact]
    public async Task HasAnyPermissionAsync_Should_Return_True_If_User_Has_Any_Permission()
    {
        // Arrange
        var email = $"test_any_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email, "TestPassword123!");
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        var profile = await profileHandler.CreateWithDefaults(email);
        
        // Add one of the permissions
        var updatedProfile = profile with 
        { 
            PermissionIds = new[] { "users.read" }
        };
        await TestDataContext().Commit(updatedProfile);
        TrackEntity(account.OwnerEntityId);

        // Act
        var hasAny = await _permissionService.HasAnyPermissionAsync(
            account.OwnerEntityId, 
            "users.read", 
            "users.write", 
            "admin.users"
        );

        // Assert
        hasAny.ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify that HasAllPermissionsAsync returns false if user lacks any permission
    /// PURPOSE: To support AND logic in permission checks
    /// BUSINESS CONTEXT: Some operations require multiple permissions simultaneously
    /// WHY IMPORTANT: Enables strict permission requirements for sensitive operations
    /// ARCHITECTURAL SIGNIFICANCE: Validates AND permission logic
    /// FUTURE RESILIENCE: Ensures combined permission requirements work correctly
    /// </summary>
    [Fact]
    public async Task HasAllPermissionsAsync_Should_Return_False_If_User_Lacks_Any_Permission()
    {
        // Arrange
        var email = $"test_all_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email, "TestPassword123!");
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        var profile = await profileHandler.CreateWithDefaults(email);
        
        // Add only some of the required permissions
        var updatedProfile = profile with 
        { 
            PermissionIds = new[] { "users.read", "users.write" }
        };
        await TestDataContext().Commit(updatedProfile);
        TrackEntity(account.OwnerEntityId);

        // Act
        var hasAll = await _permissionService.HasAllPermissionsAsync(
            account.OwnerEntityId, 
            "users.read", 
            "users.write", 
            "users.delete" // Missing this one
        );

        // Assert
        hasAll.ShouldBeFalse();
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
            UpdatedAt = DateTime.UtcNow
        };
        await TestDataContext().Commit(account);
        return account;
    }
}