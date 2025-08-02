using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.tests.Helpers;
using core.jarvis.Exceptions;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Unit.Handlers;

/// <summary>
/// Unit tests for UIStudioPermissionHandler.
/// Tests permission management, access control, and security validation.
/// </summary>
public class UIStudioPermissionHandlerTests : ApiIntegrationTestBase
{
    /// <summary>
    /// Tests granting a new permission with valid configuration.
    /// </summary>
    [Fact]
    public async Task GrantPermission_WithValidConfiguration_GrantsPermissionSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var resourceEntityId = Guid.NewGuid();
        var granteeEntityId = Guid.NewGuid();
        var granterEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(granteeEntityId);
        TrackEntity(granterEntityId);
        
        var handler = TestDataContext().For<UIStudioPermissionHandler>(entityId);
        var permission = new UIStudioPermission
        {
            OwnerEntityId = entityId,
            ResourceEntityId = resourceEntityId,
            ResourceType = "page",
            GranteeEntityId = granteeEntityId,
            GranteeType = "user",
            PermissionLevel = "read",
            GrantedByEntityId = granterEntityId,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };

        // Act
        var result = await handler.GrantPermission(permission);

        // Assert
        result.ShouldNotBeNull();
        result.ResourceEntityId.ShouldBe(resourceEntityId);
        result.ResourceType.ShouldBe("page");
        result.GranteeEntityId.ShouldBe(granteeEntityId);
        result.GranteeType.ShouldBe("user");
        result.PermissionLevel.ShouldBe("read");
        result.GrantedByEntityId.ShouldBe(granterEntityId);
        result.IsActive.ShouldBeTrue();
        result.ExpiresAt.ShouldNotBeNull();

        // Verify persistence
        var retrievedPermission = await handler.Get();
        retrievedPermission.ShouldNotBeNull();
        retrievedPermission.PermissionLevel.ShouldBe("read");
    }

    /// <summary>
    /// Tests granting permission with different permission levels.
    /// </summary>
    [Theory]
    [InlineData("read")]
    [InlineData("write")]
    [InlineData("admin")]
    [InlineData("owner")]
    public async Task GrantPermission_WithDifferentLevels_GrantsCorrectLevel(string permissionLevel)
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var resourceEntityId = Guid.NewGuid();
        var granteeEntityId = Guid.NewGuid();
        var granterEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(granteeEntityId);
        TrackEntity(granterEntityId);
        
        var handler = TestDataContext().For<UIStudioPermissionHandler>(entityId);
        var permission = new UIStudioPermission
        {
            OwnerEntityId = entityId,
            ResourceEntityId = resourceEntityId,
            ResourceType = "page",
            GranteeEntityId = granteeEntityId,
            GranteeType = "user",
            PermissionLevel = permissionLevel,
            GrantedByEntityId = granterEntityId
        };

        // Act
        var result = await handler.GrantPermission(permission);

        // Assert
        result.ShouldNotBeNull();
        result.PermissionLevel.ShouldBe(permissionLevel);
    }

    /// <summary>
    /// Tests granting permission to different grantee types.
    /// </summary>
    [Theory]
    [InlineData("user")]
    [InlineData("role")]
    [InlineData("group")]
    [InlineData("service")]
    public async Task GrantPermission_WithDifferentGranteeTypes_GrantsCorrectly(string granteeType)
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var resourceEntityId = Guid.NewGuid();
        var granteeEntityId = Guid.NewGuid();
        var granterEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(granteeEntityId);
        TrackEntity(granterEntityId);
        
        var handler = TestDataContext().For<UIStudioPermissionHandler>(entityId);
        var permission = new UIStudioPermission
        {
            OwnerEntityId = entityId,
            ResourceEntityId = resourceEntityId,
            ResourceType = "layout",
            GranteeEntityId = granteeEntityId,
            GranteeType = granteeType,
            PermissionLevel = "read",
            GrantedByEntityId = granterEntityId
        };

        // Act
        var result = await handler.GrantPermission(permission);

        // Assert
        result.ShouldNotBeNull();
        result.GranteeType.ShouldBe(granteeType);
    }

    /// <summary>
    /// Tests updating an existing permission.
    /// </summary>
    [Fact]
    public async Task UpdatePermission_WithValidChanges_UpdatesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var resourceEntityId = Guid.NewGuid();
        var granteeEntityId = Guid.NewGuid();
        var granterEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(granteeEntityId);
        TrackEntity(granterEntityId);

        var handler = TestDataContext().For<UIStudioPermissionHandler>(entityId);
        var originalPermission = new UIStudioPermission
        {
            OwnerEntityId = entityId,
            ResourceEntityId = resourceEntityId,
            ResourceType = "page",
            GranteeEntityId = granteeEntityId,
            GranteeType = "user",
            PermissionLevel = "read",
            GrantedByEntityId = granterEntityId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await handler.GrantPermission(originalPermission);

        var updatedPermission = originalPermission with
        {
            PermissionLevel = "write",
            ExpiresAt = DateTime.UtcNow.AddDays(90),
            PermissionMetadata = new Dictionary<string, object>
            {
                { "updated_reason", "role_promotion" },
                { "updated_by", "admin" }
            }
        };

        // Act
        var result = await handler.UpdatePermission(updatedPermission);

        // Assert
        result.ShouldNotBeNull();
        result.PermissionLevel.ShouldBe("write");
        result.ExpiresAt.ShouldBeGreaterThan(originalPermission.ExpiresAt);
        result.PermissionMetadata.ShouldNotBeNull();
        result.PermissionMetadata["updated_reason"].ShouldBe("role_promotion");
        result.LastUpdated.ShouldBeGreaterThan(originalPermission.LastUpdated);
    }

    /// <summary>
    /// Tests revoking a permission.
    /// </summary>
    [Fact]
    public async Task RevokePermission_WithValidPermission_RevokesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var resourceEntityId = Guid.NewGuid();
        var granteeEntityId = Guid.NewGuid();
        var granterEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(granteeEntityId);
        TrackEntity(granterEntityId);

        var handler = TestDataContext().For<UIStudioPermissionHandler>(entityId);
        var permission = new UIStudioPermission
        {
            OwnerEntityId = entityId,
            ResourceEntityId = resourceEntityId,
            ResourceType = "page",
            GranteeEntityId = granteeEntityId,
            GranteeType = "user",
            PermissionLevel = "read",
            GrantedByEntityId = granterEntityId,
            IsActive = true
        };

        await handler.GrantPermission(permission);

        // Act
        await handler.RevokePermission("access_no_longer_needed");

        // Assert
        var revokedPermission = await handler.Get();
        revokedPermission.ShouldNotBeNull();
        revokedPermission.IsActive.ShouldBeFalse();
        revokedPermission.RevokedAt.ShouldNotBeNull();
        revokedPermission.RevocationReason.ShouldBe("access_no_longer_needed");
    }

    /// <summary>
    /// Tests checking if a permission is valid and active.
    /// </summary>
    [Fact]
    public async Task IsValidPermission_WithActiveNonExpiredPermission_ReturnsTrue()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var resourceEntityId = Guid.NewGuid();
        var granteeEntityId = Guid.NewGuid();
        var granterEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(granteeEntityId);
        TrackEntity(granterEntityId);

        var handler = TestDataContext().For<UIStudioPermissionHandler>(entityId);
        var permission = new UIStudioPermission
        {
            OwnerEntityId = entityId,
            ResourceEntityId = resourceEntityId,
            ResourceType = "page",
            GranteeEntityId = granteeEntityId,
            GranteeType = "user",
            PermissionLevel = "read",
            GrantedByEntityId = granterEntityId,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };

        await handler.GrantPermission(permission);

        // Act
        var result = await handler.IsValidPermission();

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests checking expired permission returns false.
    /// </summary>
    [Fact]
    public async Task IsValidPermission_WithExpiredPermission_ReturnsFalse()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var resourceEntityId = Guid.NewGuid();
        var granteeEntityId = Guid.NewGuid();
        var granterEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(granteeEntityId);
        TrackEntity(granterEntityId);

        var handler = TestDataContext().For<UIStudioPermissionHandler>(entityId);
        var permission = new UIStudioPermission
        {
            OwnerEntityId = entityId,
            ResourceEntityId = resourceEntityId,
            ResourceType = "page",
            GranteeEntityId = granteeEntityId,
            GranteeType = "user",
            PermissionLevel = "read",
            GrantedByEntityId = granterEntityId,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            IsActive = true
        };

        await handler.GrantPermission(permission);

        // Act
        var result = await handler.IsValidPermission();

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// Tests getting permissions for a specific resource.
    /// </summary>
    [Fact]
    public async Task GetPermissionsForResource_WithMultiplePermissions_ReturnsResourcePermissions()
    {
        // Arrange
        var resourceEntityId = Guid.NewGuid();
        var otherResourceEntityId = Guid.NewGuid();
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(otherResourceEntityId);
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);
        var granterEntityId = Guid.NewGuid();
        TrackEntity(granterEntityId);

        var handler1 = TestDataContext().For<UIStudioPermissionHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioPermissionHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioPermissionHandler>(entityId3);

        // Create permissions for target resource
        var permission1 = new UIStudioPermission
        {
            OwnerEntityId = entityId1,
            ResourceEntityId = resourceEntityId,
            ResourceType = "page",
            GranteeEntityId = Guid.NewGuid(),
            GranteeType = "user",
            PermissionLevel = "read",
            GrantedByEntityId = granterEntityId
        };

        var permission2 = new UIStudioPermission
        {
            OwnerEntityId = entityId2,
            ResourceEntityId = resourceEntityId,
            ResourceType = "page",
            GranteeEntityId = Guid.NewGuid(),
            GranteeType = "user",
            PermissionLevel = "write",
            GrantedByEntityId = granterEntityId
        };

        // Create permission for different resource
        var permission3 = new UIStudioPermission
        {
            OwnerEntityId = entityId3,
            ResourceEntityId = otherResourceEntityId,
            ResourceType = "page",
            GranteeEntityId = Guid.NewGuid(),
            GranteeType = "user",
            PermissionLevel = "admin",
            GrantedByEntityId = granterEntityId
        };

        await handler1.GrantPermission(permission1);
        await handler2.GrantPermission(permission2);
        await handler3.GrantPermission(permission3);

        // Act
        var result = await handler1.GetPermissionsForResource(resourceEntityId);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.All(p => p.ResourceEntityId == resourceEntityId).ShouldBeTrue();
        result.Any(p => p.PermissionLevel == "read").ShouldBeTrue();
        result.Any(p => p.PermissionLevel == "write").ShouldBeTrue();
        result.Any(p => p.PermissionLevel == "admin").ShouldBeFalse();
    }

    /// <summary>
    /// Tests getting permissions for a specific grantee.
    /// </summary>
    [Fact]
    public async Task GetPermissionsForGrantee_WithMultipleGrantees_ReturnsGranteePermissions()
    {
        // Arrange
        var granteeEntityId = Guid.NewGuid();
        var otherGranteeEntityId = Guid.NewGuid();
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(granteeEntityId);
        TrackEntity(otherGranteeEntityId);
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);
        var granterEntityId = Guid.NewGuid();
        TrackEntity(granterEntityId);

        var handler1 = TestDataContext().For<UIStudioPermissionHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioPermissionHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioPermissionHandler>(entityId3);

        // Create permissions for target grantee
        var permission1 = new UIStudioPermission
        {
            OwnerEntityId = entityId1,
            ResourceEntityId = Guid.NewGuid(),
            ResourceType = "page",
            GranteeEntityId = granteeEntityId,
            GranteeType = "user",
            PermissionLevel = "read",
            GrantedByEntityId = granterEntityId
        };

        var permission2 = new UIStudioPermission
        {
            OwnerEntityId = entityId2,
            ResourceEntityId = Guid.NewGuid(),
            ResourceType = "layout",
            GranteeEntityId = granteeEntityId,
            GranteeType = "user",
            PermissionLevel = "write",
            GrantedByEntityId = granterEntityId
        };

        // Create permission for different grantee
        var permission3 = new UIStudioPermission
        {
            OwnerEntityId = entityId3,
            ResourceEntityId = Guid.NewGuid(),
            ResourceType = "page",
            GranteeEntityId = otherGranteeEntityId,
            GranteeType = "user",
            PermissionLevel = "admin",
            GrantedByEntityId = granterEntityId
        };

        await handler1.GrantPermission(permission1);
        await handler2.GrantPermission(permission2);
        await handler3.GrantPermission(permission3);

        // Act
        var result = await handler1.GetPermissionsForGrantee(granteeEntityId);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.All(p => p.GranteeEntityId == granteeEntityId).ShouldBeTrue();
        result.Any(p => p.ResourceType == "page" && p.PermissionLevel == "read").ShouldBeTrue();
        result.Any(p => p.ResourceType == "layout" && p.PermissionLevel == "write").ShouldBeTrue();
    }

    /// <summary>
    /// Tests checking if grantee has specific permission level.
    /// </summary>
    [Fact]
    public async Task HasPermissionLevel_WithSufficientPermission_ReturnsTrue()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var resourceEntityId = Guid.NewGuid();
        var granteeEntityId = Guid.NewGuid();
        var granterEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(granteeEntityId);
        TrackEntity(granterEntityId);

        var handler = TestDataContext().For<UIStudioPermissionHandler>(entityId);
        var permission = new UIStudioPermission
        {
            OwnerEntityId = entityId,
            ResourceEntityId = resourceEntityId,
            ResourceType = "page",
            GranteeEntityId = granteeEntityId,
            GranteeType = "user",
            PermissionLevel = "write", // Has write permission
            GrantedByEntityId = granterEntityId,
            IsActive = true
        };

        await handler.GrantPermission(permission);

        // Act & Assert - Should have read (lower level)
        var hasRead = await handler.HasPermissionLevel(granteeEntityId, resourceEntityId, "read");
        hasRead.ShouldBeTrue();

        // Act & Assert - Should have write (same level)
        var hasWrite = await handler.HasPermissionLevel(granteeEntityId, resourceEntityId, "write");
        hasWrite.ShouldBeTrue();

        // Act & Assert - Should NOT have admin (higher level)
        var hasAdmin = await handler.HasPermissionLevel(granteeEntityId, resourceEntityId, "admin");
        hasAdmin.ShouldBeFalse();
    }

    /// <summary>
    /// Tests extending permission expiration.
    /// </summary>
    [Fact]
    public async Task ExtendExpiration_WithValidExtension_ExtendsSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var resourceEntityId = Guid.NewGuid();
        var granteeEntityId = Guid.NewGuid();
        var granterEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(granteeEntityId);
        TrackEntity(granterEntityId);

        var handler = TestDataContext().For<UIStudioPermissionHandler>(entityId);
        var originalExpiry = DateTime.UtcNow.AddDays(7);
        var permission = new UIStudioPermission
        {
            OwnerEntityId = entityId,
            ResourceEntityId = resourceEntityId,
            ResourceType = "page",
            GranteeEntityId = granteeEntityId,
            GranteeType = "user",
            PermissionLevel = "read",
            GrantedByEntityId = granterEntityId,
            ExpiresAt = originalExpiry
        };

        await handler.GrantPermission(permission);

        var newExpiry = DateTime.UtcNow.AddDays(90);

        // Act
        var result = await handler.ExtendExpiration(newExpiry, "contract_renewal");

        // Assert
        result.ShouldNotBeNull();
        result.ExpiresAt.ShouldBe(newExpiry);
        result.ExpiresAt.ShouldBeGreaterThan(originalExpiry);
        result.PermissionMetadata.ShouldNotBeNull();
        result.PermissionMetadata.ContainsKey("extension_reason").ShouldBeTrue();
    }

    /// <summary>
    /// Tests getting expiring permissions.
    /// </summary>
    [Fact]
    public async Task GetExpiringPermissions_WithinTimeWindow_ReturnsExpiringPermissions()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);
        var granterEntityId = Guid.NewGuid();
        TrackEntity(granterEntityId);

        var handler1 = TestDataContext().For<UIStudioPermissionHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioPermissionHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioPermissionHandler>(entityId3);

        // Permission expiring soon (within 7 days)
        var soonExpiring = new UIStudioPermission
        {
            OwnerEntityId = entityId1,
            ResourceEntityId = Guid.NewGuid(),
            ResourceType = "page",
            GranteeEntityId = Guid.NewGuid(),
            GranteeType = "user",
            PermissionLevel = "read",
            GrantedByEntityId = granterEntityId,
            ExpiresAt = DateTime.UtcNow.AddDays(3),
            IsActive = true
        };

        // Permission expiring later (beyond 7 days)
        var laterExpiring = new UIStudioPermission
        {
            OwnerEntityId = entityId2,
            ResourceEntityId = Guid.NewGuid(),
            ResourceType = "page",
            GranteeEntityId = Guid.NewGuid(),
            GranteeType = "user",
            PermissionLevel = "write",
            GrantedByEntityId = granterEntityId,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };

        // Permission with no expiry
        var noExpiry = new UIStudioPermission
        {
            OwnerEntityId = entityId3,
            ResourceEntityId = Guid.NewGuid(),
            ResourceType = "page",
            GranteeEntityId = Guid.NewGuid(),
            GranteeType = "user",
            PermissionLevel = "admin",
            GrantedByEntityId = granterEntityId,
            ExpiresAt = null,
            IsActive = true
        };

        await handler1.GrantPermission(soonExpiring);
        await handler2.GrantPermission(laterExpiring);
        await handler3.GrantPermission(noExpiry);

        // Act
        var result = await handler1.GetExpiringPermissions(7); // Within 7 days

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].ExpiresAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddDays(7));
        result[0].PermissionLevel.ShouldBe("read");
    }

    /// <summary>
    /// Tests bulk permission operations.
    /// </summary>
    [Fact]
    public async Task BulkGrantPermissions_WithMultiplePermissions_GrantsAllSuccessfully()
    {
        // Arrange
        var granterEntityId = Guid.NewGuid();
        TrackEntity(granterEntityId);
        
        var permissions = new List<UIStudioPermission>();
        for (int i = 0; i < 3; i++)
        {
            var entityId = Guid.NewGuid();
            TrackEntity(entityId);
            
            permissions.Add(new UIStudioPermission
            {
                OwnerEntityId = entityId,
                ResourceEntityId = Guid.NewGuid(),
                ResourceType = "page",
                GranteeEntityId = Guid.NewGuid(),
                GranteeType = "user",
                PermissionLevel = i == 0 ? "read" : i == 1 ? "write" : "admin",
                GrantedByEntityId = granterEntityId
            });
        }

        var handler = TestDataContext().For<UIStudioPermissionHandler>(permissions[0].OwnerEntityId);

        // Act
        var results = await handler.BulkGrantPermissions(permissions);

        // Assert
        results.ShouldNotBeNull();
        results.Count.ShouldBe(3);
        results.All(r => r.GrantedByEntityId == granterEntityId).ShouldBeTrue();
        results.Any(r => r.PermissionLevel == "read").ShouldBeTrue();
        results.Any(r => r.PermissionLevel == "write").ShouldBeTrue();
        results.Any(r => r.PermissionLevel == "admin").ShouldBeTrue();
    }

    /// <summary>
    /// Tests permission inheritance from parent resources.
    /// </summary>
    [Fact]
    public async Task GetInheritedPermissions_WithHierarchy_ReturnsInheritedPermissions()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var parentResourceId = Guid.NewGuid();
        var childResourceId = Guid.NewGuid();
        var granteeEntityId = Guid.NewGuid();
        var granterEntityId = Guid.NewGuid();
        TrackEntity(parentResourceId);
        TrackEntity(childResourceId);
        TrackEntity(granteeEntityId);
        TrackEntity(granterEntityId);

        var handler = TestDataContext().For<UIStudioPermissionHandler>(entityId);
        
        // Create permission on parent resource
        var parentPermission = new UIStudioPermission
        {
            OwnerEntityId = entityId,
            ResourceEntityId = parentResourceId,
            ResourceType = "layout",
            GranteeEntityId = granteeEntityId,
            GranteeType = "user",
            PermissionLevel = "write",
            GrantedByEntityId = granterEntityId,
            IsInheritable = true,
            IsActive = true
        };

        await handler.GrantPermission(parentPermission);

        // Act
        var result = await handler.GetInheritedPermissions(childResourceId, granteeEntityId);

        // Assert
        result.ShouldNotBeNull();
        result.ContainsKey("inherited").ShouldBeTrue();
        result.ContainsKey("effectiveLevel").ShouldBeTrue();
    }
}