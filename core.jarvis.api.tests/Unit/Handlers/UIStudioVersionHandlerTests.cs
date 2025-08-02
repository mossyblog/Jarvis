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
/// Unit tests for UIStudioVersionHandler.
/// Tests version control operations, snapshots, and rollback functionality.
/// </summary>
public class UIStudioVersionHandlerTests : ApiIntegrationTestBase
{
    /// <summary>
    /// Tests creating an automatic version snapshot.
    /// </summary>
    [Fact]
    public async Task CreateAutoVersion_WithValidData_CreatesVersionSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var resourceEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(creatorEntityId);
        
        var handler = TestDataContext().For<UIStudioVersionHandler>(entityId);
        var snapshotData = new Dictionary<string, object>
        {
            { "page", new { name = "Test Page", slug = "test-page" } },
            { "layout", new { type = "bento", columns = 12, rows = 8 } },
            { "components", new[] { 
                new { type = "MetricCard", id = "metric-1" },
                new { type = "Chart", id = "chart-1" }
            }}
        };

        // Act
        var result = await handler.CreateAutoVersion(
            resourceEntityId,
            "page",
            snapshotData,
            creatorEntityId,
            "Initial page creation");

        // Assert
        result.ShouldNotBeNull();
        result.ResourceEntityId.ShouldBe(resourceEntityId);
        result.ResourceType.ShouldBe("page");
        result.VersionType.ShouldBe("auto");
        result.SnapshotData.ShouldNotBeNull();
        result.ChangeSummary.ShouldBe("Initial page creation");
        result.CreatedByEntityId.ShouldBe(creatorEntityId);
        result.VersionNumber.ShouldBe(1);

        // Verify persistence
        var retrievedVersion = await handler.Get();
        retrievedVersion.ShouldNotBeNull();
        retrievedVersion.VersionType.ShouldBe("auto");
    }

    /// <summary>
    /// Tests creating a manual version snapshot.
    /// </summary>
    [Fact]
    public async Task CreateManualVersion_WithValidData_CreatesVersionSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var resourceEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(creatorEntityId);
        
        var handler = TestDataContext().For<UIStudioVersionHandler>(entityId);
        var snapshotData = new Dictionary<string, object>
        {
            { "page", new { name = "Updated Page", slug = "updated-page" } }
        };

        // Act
        var result = await handler.CreateManualVersion(
            resourceEntityId,
            "page",
            snapshotData,
            creatorEntityId,
            "v1.2.0",
            "Major feature update with new components");

        // Assert
        result.ShouldNotBeNull();
        result.ResourceEntityId.ShouldBe(resourceEntityId);
        result.ResourceType.ShouldBe("page");
        result.VersionType.ShouldBe("manual");
        result.VersionTag.ShouldBe("v1.2.0");
        result.ChangeSummary.ShouldBe("Major feature update with new components");
        result.CreatedByEntityId.ShouldBe(creatorEntityId);
    }

    /// <summary>
    /// Tests creating a published version snapshot.
    /// </summary>
    [Fact]
    public async Task CreatePublishedVersion_WithValidData_CreatesVersionSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var resourceEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(creatorEntityId);
        
        var handler = TestDataContext().For<UIStudioVersionHandler>(entityId);
        var snapshotData = new Dictionary<string, object>
        {
            { "page", new { name = "Production Page", slug = "production-page", isPublished = true } }
        };

        // Act
        var result = await handler.CreatePublishedVersion(
            resourceEntityId,
            "page",
            snapshotData,
            creatorEntityId,
            "v1.0.0",
            "Initial production release");

        // Assert
        result.ShouldNotBeNull();
        result.ResourceEntityId.ShouldBe(resourceEntityId);
        result.ResourceType.ShouldBe("page");
        result.VersionType.ShouldBe("published");
        result.IsPublished.ShouldBeTrue();
        result.PublishedAt.ShouldNotBeNull();
        result.VersionTag.ShouldBe("v1.0.0");
        result.ChangeSummary.ShouldBe("Initial production release");
    }

    /// <summary>
    /// Tests getting version history for a resource.
    /// </summary>
    [Fact]
    public async Task GetVersionHistory_WithMultipleVersions_ReturnsOrderedHistory()
    {
        // Arrange
        var resourceEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(creatorEntityId);

        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var handler1 = TestDataContext().For<UIStudioVersionHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioVersionHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioVersionHandler>(entityId3);

        var snapshotData = new Dictionary<string, object> { { "test", "data" } };

        // Create versions in sequence
        await handler1.CreateAutoVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "Version 1");
        await Task.Delay(100); // Ensure different timestamps
        
        await handler2.CreateManualVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "v1.1.0", "Version 2");
        await Task.Delay(100);
        
        await handler3.CreatePublishedVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "v1.2.0", "Version 3");

        // Act
        var result = await handler1.GetVersionHistory(resourceEntityId);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        
        // Should be ordered by creation date (newest first)
        result[0].ChangeSummary.ShouldBe("Version 3");
        result[0].VersionType.ShouldBe("published");
        
        result[1].ChangeSummary.ShouldBe("Version 2");
        result[1].VersionType.ShouldBe("manual");
        
        result[2].ChangeSummary.ShouldBe("Version 1");
        result[2].VersionType.ShouldBe("auto");
    }

    /// <summary>
    /// Tests getting versions by type.
    /// </summary>
    [Fact]
    public async Task GetVersionsByType_WithMixedTypes_ReturnsCorrectVersions()
    {
        // Arrange
        var resourceEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(creatorEntityId);

        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var handler1 = TestDataContext().For<UIStudioVersionHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioVersionHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioVersionHandler>(entityId3);

        var snapshotData = new Dictionary<string, object> { { "test", "data" } };

        // Create different version types
        await handler1.CreateAutoVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "Auto version 1");
        await handler2.CreateAutoVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "Auto version 2");
        await handler3.CreateManualVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "v1.0.0", "Manual version");

        // Act
        var autoVersions = await handler1.GetVersionsByType(resourceEntityId, "auto");
        var manualVersions = await handler1.GetVersionsByType(resourceEntityId, "manual");

        // Assert
        autoVersions.ShouldNotBeNull();
        autoVersions.Count.ShouldBe(2);
        autoVersions.All(v => v.VersionType == "auto").ShouldBeTrue();

        manualVersions.ShouldNotBeNull();
        manualVersions.Count.ShouldBe(1);
        manualVersions[0].VersionType.ShouldBe("manual");
        manualVersions[0].VersionTag.ShouldBe("v1.0.0");
    }

    /// <summary>
    /// Tests getting the latest version.
    /// </summary>
    [Fact]
    public async Task GetLatestVersion_WithMultipleVersions_ReturnsNewest()
    {
        // Arrange
        var resourceEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(creatorEntityId);

        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);

        var handler1 = TestDataContext().For<UIStudioVersionHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioVersionHandler>(entityId2);

        var snapshotData = new Dictionary<string, object> { { "test", "data" } };

        // Create versions with delay to ensure different timestamps
        await handler1.CreateAutoVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "Older version");
        await Task.Delay(100);
        await handler2.CreateManualVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "v1.0.0", "Latest version");

        // Act
        var result = await handler1.GetLatestVersion(resourceEntityId);

        // Assert
        result.ShouldNotBeNull();
        result.ChangeSummary.ShouldBe("Latest version");
        result.VersionType.ShouldBe("manual");
        result.VersionTag.ShouldBe("v1.0.0");
    }

    /// <summary>
    /// Tests restoring from a version.
    /// </summary>
    [Fact]
    public async Task RestoreFromVersion_WithValidVersion_RestoresSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var resourceEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        var restorerEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(creatorEntityId);
        TrackEntity(restorerEntityId);
        
        var handler = TestDataContext().For<UIStudioVersionHandler>(entityId);
        var originalSnapshotData = new Dictionary<string, object>
        {
            { "page", new { name = "Original Page", version = 1 } }
        };

        // Create original version
        var originalVersion = await handler.CreateManualVersion(
            resourceEntityId,
            "page",
            originalSnapshotData,
            creatorEntityId,
            "v1.0.0",
            "Original version");

        // Act
        var result = await handler.RestoreFromVersion(originalVersion.Id, restorerEntityId, "Restored to v1.0.0");

        // Assert
        result.ShouldNotBeNull();
        result.ResourceEntityId.ShouldBe(resourceEntityId);
        result.VersionType.ShouldBe("restore");
        result.ParentVersionId.ShouldBe(originalVersion.Id);
        result.CreatedByEntityId.ShouldBe(restorerEntityId);
        result.ChangeSummary.ShouldBe("Restored to v1.0.0");
    }

    /// <summary>
    /// Tests comparing versions.
    /// </summary>
    [Fact]
    public async Task CompareVersions_WithTwoVersions_ReturnsComparison()
    {
        // Arrange
        var resourceEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(creatorEntityId);

        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);

        var handler1 = TestDataContext().For<UIStudioVersionHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioVersionHandler>(entityId2);

        var snapshot1 = new Dictionary<string, object>
        {
            { "page", new { name = "Version 1", components = new[] { "comp1", "comp2" } } }
        };

        var snapshot2 = new Dictionary<string, object>
        {
            { "page", new { name = "Version 2", components = new[] { "comp1", "comp2", "comp3" } } }
        };

        var version1 = await handler1.CreateAutoVersion(resourceEntityId, "page", snapshot1, creatorEntityId, "Version 1");
        var version2 = await handler2.CreateAutoVersion(resourceEntityId, "page", snapshot2, creatorEntityId, "Version 2");

        // Act
        var result = await handler1.CompareVersions(version1.Id, version2.Id);

        // Assert
        result.ShouldNotBeNull();
        result.ContainsKey("differences").ShouldBeTrue();
        result.ContainsKey("changeType").ShouldBeTrue();
        result.ContainsKey("summary").ShouldBeTrue();
    }

    /// <summary>
    /// Tests getting version statistics.
    /// </summary>
    [Fact]
    public async Task GetVersionStats_WithMultipleVersions_ReturnsStatistics()
    {
        // Arrange
        var resourceEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(creatorEntityId);

        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var handler1 = TestDataContext().For<UIStudioVersionHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioVersionHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioVersionHandler>(entityId3);

        var snapshotData = new Dictionary<string, object> { { "test", "data" } };

        // Create different version types
        await handler1.CreateAutoVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "Auto 1");
        await handler2.CreateManualVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "v1.0.0", "Manual 1");
        await handler3.CreatePublishedVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "v1.0.0", "Published 1");

        // Act
        var result = await handler1.GetVersionStats(resourceEntityId);

        // Assert
        result.ShouldNotBeNull();
        result.ContainsKey("totalVersions").ShouldBeTrue();
        result.ContainsKey("autoVersions").ShouldBeTrue();
        result.ContainsKey("manualVersions").ShouldBeTrue();
        result.ContainsKey("publishedVersions").ShouldBeTrue();
        result.ContainsKey("latestVersion").ShouldBeTrue();
        
        result["totalVersions"].ShouldBe(3);
        result["autoVersions"].ShouldBe(1);
        result["manualVersions"].ShouldBe(1);
        result["publishedVersions"].ShouldBe(1);
    }

    /// <summary>
    /// Tests creating branched versions.
    /// </summary>
    [Fact]
    public async Task CreateBranch_WithValidParent_CreatesBranchSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var branchEntityId = Guid.NewGuid();
        TrackEntity(entityId);
        TrackEntity(branchEntityId);
        var resourceEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(creatorEntityId);
        
        var handler = TestDataContext().For<UIStudioVersionHandler>(entityId);
        var branchHandler = TestDataContext().For<UIStudioVersionHandler>(branchEntityId);
        
        var snapshotData = new Dictionary<string, object>
        {
            { "page", new { name = "Main Branch", version = 1 } }
        };

        // Create parent version
        var parentVersion = await handler.CreateManualVersion(
            resourceEntityId,
            "page",
            snapshotData,
            creatorEntityId,
            "v1.0.0",
            "Main branch version");

        var branchData = new Dictionary<string, object>
        {
            { "page", new { name = "Feature Branch", version = 1, feature = "new-component" } }
        };

        // Act
        var result = await branchHandler.CreateBranch(
            parentVersion.Id,
            "feature/new-component",
            branchData,
            creatorEntityId,
            "Created feature branch for new component");

        // Assert
        result.ShouldNotBeNull();
        result.ResourceEntityId.ShouldBe(resourceEntityId);
        result.VersionType.ShouldBe("branch");
        result.ParentVersionId.ShouldBe(parentVersion.Id);
        result.BranchName.ShouldBe("feature/new-component");
        result.ChangeSummary.ShouldBe("Created feature branch for new component");
    }

    /// <summary>
    /// Tests merging branch versions.
    /// </summary>
    [Fact]
    public async Task MergeBranch_WithValidBranch_MergesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var branchEntityId = Guid.NewGuid();
        var mergeEntityId = Guid.NewGuid();
        TrackEntity(entityId);
        TrackEntity(branchEntityId);
        TrackEntity(mergeEntityId);
        var resourceEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(creatorEntityId);
        
        var handler = TestDataContext().For<UIStudioVersionHandler>(entityId);
        var branchHandler = TestDataContext().For<UIStudioVersionHandler>(branchEntityId);
        var mergeHandler = TestDataContext().For<UIStudioVersionHandler>(mergeEntityId);
        
        var snapshotData = new Dictionary<string, object>
        {
            { "page", new { name = "Main Branch" } }
        };

        // Create parent and branch versions
        var parentVersion = await handler.CreateManualVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "v1.0.0", "Main");
        var branchVersion = await branchHandler.CreateBranch(parentVersion.Id, "feature/test", snapshotData, creatorEntityId, "Branch");

        // Act
        var result = await mergeHandler.MergeBranch(
            branchVersion.Id,
            parentVersion.Id,
            creatorEntityId,
            "Merged feature/test into main",
            new Dictionary<string, object> { { "conflicts", "none" } });

        // Assert
        result.ShouldNotBeNull();
        result.VersionType.ShouldBe("merge");
        result.ParentVersionId.ShouldBe(parentVersion.Id);
        result.BranchVersionId.ShouldBe(branchVersion.Id);
        result.ChangeSummary.ShouldBe("Merged feature/test into main");
    }

    /// <summary>
    /// Tests getting next version number.
    /// </summary>
    [Fact]
    public async Task GetNextVersionNumber_WithExistingVersions_ReturnsCorrectNumber()
    {
        // Arrange
        var resourceEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(creatorEntityId);

        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);

        var handler1 = TestDataContext().For<UIStudioVersionHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioVersionHandler>(entityId2);

        var snapshotData = new Dictionary<string, object> { { "test", "data" } };

        // Create two versions
        await handler1.CreateAutoVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "Version 1");
        await handler2.CreateAutoVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "Version 2");

        // Act
        var nextVersionNumber = await handler1.GetNextVersionNumber(resourceEntityId);

        // Assert
        nextVersionNumber.ShouldBe(3);
    }

    /// <summary>
    /// Tests cleanup old versions.
    /// </summary>
    [Fact]
    public async Task CleanupOldVersions_WithRetentionPolicy_CleansUpCorrectly()
    {
        // Arrange
        var resourceEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(creatorEntityId);

        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var handler1 = TestDataContext().For<UIStudioVersionHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioVersionHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioVersionHandler>(entityId3);

        var snapshotData = new Dictionary<string, object> { { "test", "data" } };

        // Create versions with different types and dates
        await handler1.CreateAutoVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "Old auto version");
        await handler2.CreateManualVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "v1.0.0", "Manual version");
        await handler3.CreatePublishedVersion(resourceEntityId, "page", snapshotData, creatorEntityId, "v1.1.0", "Published version");

        var retentionPolicy = new Dictionary<string, object>
        {
            { "keepAutoVersions", 5 },
            { "keepManualVersions", 10 },
            { "keepPublishedVersions", -1 }, // Keep all published
            { "olderThanDays", 30 }
        };

        // Act
        var result = await handler1.CleanupOldVersions(resourceEntityId, retentionPolicy);

        // Assert
        result.ShouldNotBeNull();
        result.ContainsKey("cleanedCount").ShouldBeTrue();
        result.ContainsKey("retainedCount").ShouldBeTrue();
        result.ContainsKey("policy").ShouldBeTrue();
    }
}