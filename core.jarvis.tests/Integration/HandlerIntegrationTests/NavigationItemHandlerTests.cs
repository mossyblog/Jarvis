using core.jarvis.Data;
using core.jarvis.Data.Query;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Integration.HandlerIntegrationTests;

/// <summary>
/// Simple component to create navigation_item_test_component table for testing table creation.
/// This mirrors the structure of the actual NavigationItem from the API project.
/// </summary>
public record NavigationItemTestComponent : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    public string MenuId { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string Href { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public Guid? RequiredPermissionId { get; init; }
    public bool IsActive { get; init; } = true;
    public string? BadgeConfig { get; init; }
}

/// <summary>
/// Integration tests to create a table for NavigationItem functionality.
/// The goal is to demonstrate table creation and prepare for the initialize-studio.csx script.
/// </summary>
public class NavigationItemHandlerTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Create a NavigationItem component to trigger automatic table creation.
    /// PURPOSE: Ensures a table similar to navigation_item_component is created in the database.
    /// BUSINESS CONTEXT: Required for testing table creation mechanism.
    /// WHY IMPORTANT: Validates the ECS framework's automatic table creation functionality.
    /// ARCHITECTURAL SIGNIFICANCE: Tests the ECS framework's automatic table creation.
    /// FUTURE RESILIENCE: Validates that NavigationItem-like components can be persisted.
    /// </summary>
    [Fact]
    public async Task NavigationItemTestComponent_CommitComponent_ShouldCreateTableAndPersistData()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        var navigationItem = new NavigationItemTestComponent
        {
            OwnerEntityId = entityId,
            MenuId = "test-menu-item",
            Label = "Test Menu Item",
            Icon = "test-icon",
            Href = "/test-route",
            SortOrder = 1,
            IsActive = true,
            LastUpdated = DateTime.UtcNow
        };

        // Act - This should trigger table creation if it doesn't exist
        var success = await TestDataContext().TryCommit(navigationItem);

        // Assert
        success.ShouldBeTrue("NavigationItem should be committed successfully");
        
        // Verify the component was persisted by retrieving it
        var result = await TestDataContext().Query()
            .WithAll<NavigationItemTestComponent>(Filter<NavigationItemTestComponent>.Eq(n => n.OwnerEntityId, entityId))
            .ToEntityComponents();
        
        result.ShouldNotBeEmpty("NavigationItem should be retrievable after commit");
        var retrievedItem = result.First().Value.Get<NavigationItemTestComponent>();
        
        retrievedItem.ShouldNotBeNull("NavigationItem should be retrievable after commit");
        retrievedItem.MenuId.ShouldBe("test-menu-item");
        retrievedItem.Label.ShouldBe("Test Menu Item");
        retrievedItem.Icon.ShouldBe("test-icon");
        retrievedItem.Href.ShouldBe("/test-route");
        retrievedItem.SortOrder.ShouldBe(1);
        retrievedItem.IsActive.ShouldBeTrue();
        retrievedItem.OwnerEntityId.ShouldBe(entityId);
    }

    /// <summary>
    /// INTENT: Create multiple NavigationItem components with different properties.
    /// PURPOSE: Ensures the table can handle various NavigationItem configurations.
    /// BUSINESS CONTEXT: Testing needs different types of navigation items.
    /// WHY IMPORTANT: Validates table schema supports all NavigationItem fields.
    /// ARCHITECTURAL SIGNIFICANCE: Tests the complete NavigationItem data model.
    /// FUTURE RESILIENCE: Ensures all NavigationItem properties persist correctly.
    /// </summary>
    [Fact]
    public async Task NavigationItemTestComponent_CommitMultipleComponents_ShouldPersistAllData()
    {
        // Arrange
        var entity1Id = Guid.NewGuid();
        var entity2Id = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        
        TrackEntity(entity1Id);
        TrackEntity(entity2Id);
        
        var navigationItem1 = new NavigationItemTestComponent
        {
            OwnerEntityId = entity1Id,
            MenuId = "dashboard",
            Label = "Dashboard",
            Icon = "dashboard-icon",
            Href = "/dashboard",
            SortOrder = 1,
            RequiredPermissionId = permissionId,
            IsActive = true,
            BadgeConfig = """{"type": "count", "color": "blue"}""",
            LastUpdated = DateTime.UtcNow
        };
        
        var navigationItem2 = new NavigationItemTestComponent
        {
            OwnerEntityId = entity2Id,
            MenuId = "settings",
            Label = "Settings",
            Icon = "settings-icon",
            Href = "/settings",
            SortOrder = 99,
            RequiredPermissionId = null,
            IsActive = false,
            BadgeConfig = null,
            LastUpdated = DateTime.UtcNow
        };

        // Act
        var success1 = await TestDataContext().TryCommit(navigationItem1);
        var success2 = await TestDataContext().TryCommit(navigationItem2);

        // Assert
        success1.ShouldBeTrue("First NavigationItem should be committed successfully");
        success2.ShouldBeTrue("Second NavigationItem should be committed successfully");
        
        // Verify both components were persisted correctly
        var result1 = await TestDataContext().Query()
            .WithAll<NavigationItemTestComponent>(Filter<NavigationItemTestComponent>.Eq(n => n.OwnerEntityId, entity1Id))
            .ToEntityComponents();
        var result2 = await TestDataContext().Query()
            .WithAll<NavigationItemTestComponent>(Filter<NavigationItemTestComponent>.Eq(n => n.OwnerEntityId, entity2Id))
            .ToEntityComponents();
            
        result1.ShouldNotBeEmpty();
        result2.ShouldNotBeEmpty();
        
        var retrievedItem1 = result1.First().Value.Get<NavigationItemTestComponent>();
        var retrievedItem2 = result2.First().Value.Get<NavigationItemTestComponent>();
        
        // Verify first item
        retrievedItem1.ShouldNotBeNull();
        retrievedItem1.MenuId.ShouldBe("dashboard");
        retrievedItem1.Label.ShouldBe("Dashboard");
        retrievedItem1.RequiredPermissionId.ShouldBe(permissionId);
        retrievedItem1.IsActive.ShouldBeTrue();
        retrievedItem1.BadgeConfig.ShouldBe("""{"type": "count", "color": "blue"}""");
        
        // Verify second item
        retrievedItem2.ShouldNotBeNull();
        retrievedItem2.MenuId.ShouldBe("settings");
        retrievedItem2.Label.ShouldBe("Settings");
        retrievedItem2.RequiredPermissionId.ShouldBeNull();
        retrievedItem2.IsActive.ShouldBeFalse();
        retrievedItem2.BadgeConfig.ShouldBeNull();
    }
}