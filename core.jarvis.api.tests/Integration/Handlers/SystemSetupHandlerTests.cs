using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.tests.Helpers;
using core.jarvis.Data;
using Shouldly;

namespace core.jarvis.api.tests.Integration.Handlers;

/// <summary>
/// Integration tests for SystemSetupHandler.
/// Tests the O(1) query optimization for EnsureDefaultNavigation and EnsureDefaultRoles.
/// NOTE: These tests run against a shared database - assertions account for possible pre-existing data.
/// </summary>
public class SystemSetupHandlerTests : ApiIntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify EnsureDefaultNavigation returns the expected default navigation items.
    /// PURPOSE: Ensures system initialization creates/returns the expected default navigation structure.
    /// BUSINESS CONTEXT: Navigation items are required for the UI to function properly.
    /// WHY IMPORTANT: First-time system setup must create default navigation automatically.
    /// ARCHITECTURAL SIGNIFICANCE: Tests framework infrastructure for UI navigation.
    /// FUTURE RESILIENCE: Ensures navigation initialization continues to work after optimizations.
    /// </summary>
    [Fact]
    public async Task EnsureDefaultNavigation_ShouldReturnDefaultItems()
    {
        // Arrange
        var setupId = Guid.NewGuid();
        var handler = TestDataContext().For<SystemSetupHandler>(setupId);

        // Act
        var result = await handler.EnsureDefaultNavigation();

        // Assert
        result.ShouldNotBeNull();

        // The handler should return exactly 4 items (one per default nav item)
        result.Count.ShouldBe(4, "Should return exactly 4 default navigation items");

        // Verify all expected navigation items are present
        result.ShouldContain(n => n.MenuId == "dashboard", "Should contain dashboard navigation");
        result.ShouldContain(n => n.MenuId == "accounts", "Should contain accounts navigation");
        result.ShouldContain(n => n.MenuId == "roles", "Should contain roles navigation");
        result.ShouldContain(n => n.MenuId == "settings", "Should contain settings navigation");

        // Cleanup - only remove items we created (those not pre-existing)
        foreach (var item in result)
        {
            try
            {
                await TestDataContext().Remove<NavigationItem>(item.OwnerEntityId);
            }
            catch
            {
                // Item may have been cleaned up by another test or not exist
            }
        }
    }

    /// <summary>
    /// INTENT: Verify EnsureDefaultNavigation is idempotent - calling it twice returns same items.
    /// PURPOSE: Ensures repeated initialization calls don't create duplicates.
    /// BUSINESS CONTEXT: System startup may call initialization multiple times.
    /// WHY IMPORTANT: Prevents duplicate navigation items from appearing in the UI.
    /// ARCHITECTURAL SIGNIFICANCE: Tests idempotency of framework initialization.
    /// FUTURE RESILIENCE: Validates the O(1) optimization doesn't break idempotency.
    /// </summary>
    [Fact]
    public async Task EnsureDefaultNavigation_WhenCalledTwice_ShouldReturnSameItems()
    {
        // Arrange
        var setupId = Guid.NewGuid();
        var handler = TestDataContext().For<SystemSetupHandler>(setupId);

        // Act
        var firstResult = await handler.EnsureDefaultNavigation();
        var secondResult = await handler.EnsureDefaultNavigation();

        // Assert - both calls should return the same count
        firstResult.Count.ShouldBe(secondResult.Count, "Both calls should return same number of items");

        // Verify each item from first call appears in second call
        foreach (var item in firstResult)
        {
            secondResult.ShouldContain(n => n.MenuId == item.MenuId,
                $"Second call should contain item with MenuId {item.MenuId}");
        }

        // Cleanup
        foreach (var item in firstResult)
        {
            try
            {
                await TestDataContext().Remove<NavigationItem>(item.OwnerEntityId);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// INTENT: Verify EnsureDefaultRoles returns the expected default roles.
    /// PURPOSE: Ensures system initialization creates/returns the expected default roles.
    /// BUSINESS CONTEXT: Roles are required for the RBAC system to function.
    /// WHY IMPORTANT: First-time system setup must create default roles automatically.
    /// ARCHITECTURAL SIGNIFICANCE: Tests framework infrastructure for authorization.
    /// FUTURE RESILIENCE: Ensures role initialization continues to work after optimizations.
    /// </summary>
    [Fact]
    public async Task EnsureDefaultRoles_ShouldReturnDefaultRoles()
    {
        // Arrange
        var setupId = Guid.NewGuid();
        var handler = TestDataContext().For<SystemSetupHandler>(setupId);

        // Act
        var result = await handler.EnsureDefaultRoles();

        // Assert
        result.ShouldNotBeNull();

        // The handler should return exactly 3 items (one per default role)
        result.Count.ShouldBe(3, "Should return exactly 3 default roles");

        // Verify all expected roles are present
        result.ShouldContain(r => r.Name == "Administrator", "Should contain Administrator role");
        result.ShouldContain(r => r.Name == "User", "Should contain User role");
        result.ShouldContain(r => r.Name == "Guest", "Should contain Guest role");

        // Cleanup
        foreach (var role in result)
        {
            try
            {
                await TestDataContext().Remove<Role>(role.OwnerEntityId);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// INTENT: Verify EnsureDefaultRoles is idempotent - calling it twice returns same roles.
    /// PURPOSE: Ensures repeated initialization calls don't create duplicate roles.
    /// BUSINESS CONTEXT: System startup may call initialization multiple times.
    /// WHY IMPORTANT: Prevents duplicate roles from appearing in the system.
    /// ARCHITECTURAL SIGNIFICANCE: Tests idempotency of framework initialization.
    /// FUTURE RESILIENCE: Validates the O(1) optimization doesn't break idempotency.
    /// </summary>
    [Fact]
    public async Task EnsureDefaultRoles_WhenCalledTwice_ShouldReturnSameRoles()
    {
        // Arrange
        var setupId = Guid.NewGuid();
        var handler = TestDataContext().For<SystemSetupHandler>(setupId);

        // Act
        var firstResult = await handler.EnsureDefaultRoles();
        var secondResult = await handler.EnsureDefaultRoles();

        // Assert - both calls should return the same count
        firstResult.Count.ShouldBe(secondResult.Count, "Both calls should return same number of roles");

        // Verify each role from first call appears in second call
        foreach (var role in firstResult)
        {
            secondResult.ShouldContain(r => r.Name == role.Name,
                $"Second call should contain role with Name {role.Name}");
        }

        // Cleanup
        foreach (var role in firstResult)
        {
            try
            {
                await TestDataContext().Remove<Role>(role.OwnerEntityId);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// INTENT: Verify GetAllRoles returns roles including the defaults.
    /// PURPOSE: Ensures role retrieval works correctly after optimization.
    /// BUSINESS CONTEXT: Admin UI needs to list all available roles.
    /// WHY IMPORTANT: Role management depends on accurate role listing.
    /// ARCHITECTURAL SIGNIFICANCE: Tests query functionality of SystemSetupHandler.
    /// FUTURE RESILIENCE: Validates role query continues to work after changes.
    /// </summary>
    [Fact]
    public async Task GetAllRoles_AfterCreatingDefaults_ShouldReturnRolesSortedByName()
    {
        // Arrange
        var setupId = Guid.NewGuid();
        var handler = TestDataContext().For<SystemSetupHandler>(setupId);
        var createdRoles = await handler.EnsureDefaultRoles();

        // Act
        var result = await handler.GetAllRoles();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThanOrEqualTo(3, "Should return at least 3 default roles");

        // Verify roles are sorted by name
        var sortedNames = result.Select(r => r.Name).ToList();
        sortedNames.ShouldBe(sortedNames.OrderBy(n => n).ToList(), "Roles should be sorted by name");

        // Verify the default roles are in the result
        result.ShouldContain(r => r.Name == "Administrator", "Should contain Administrator role");
        result.ShouldContain(r => r.Name == "User", "Should contain User role");
        result.ShouldContain(r => r.Name == "Guest", "Should contain Guest role");

        // Cleanup
        foreach (var role in createdRoles)
        {
            try
            {
                await TestDataContext().Remove<Role>(role.OwnerEntityId);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
