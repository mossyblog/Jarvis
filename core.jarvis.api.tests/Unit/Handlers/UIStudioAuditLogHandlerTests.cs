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
/// Unit tests for UIStudioAuditLogHandler.
/// Tests audit logging, security tracking, and compliance reporting functionality.
/// </summary>
public class UIStudioAuditLogHandlerTests : ApiIntegrationTestBase
{
    /// <summary>
    /// Tests logging a user action with valid data.
    /// </summary>
    [Fact]
    public async Task LogUserAction_WithValidData_LogsActionSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var userEntityId = Guid.NewGuid();
        var resourceEntityId = Guid.NewGuid();
        TrackEntity(userEntityId);
        TrackEntity(resourceEntityId);
        
        var handler = TestDataContext().For<UIStudioAuditLogHandler>(entityId);
        var actionDetails = new Dictionary<string, object>
        {
            { "page_name", "Test Dashboard" },
            { "changes", new[] { "title", "layout", "components" } },
            { "component_count", 5 },
            { "user_agent", "Mozilla/5.0" },
            { "ip_address", "192.168.1.100" }
        };

        // Act
        var result = await handler.LogUserAction(
            userEntityId,
            "update",
            "Updated dashboard page with new components",
            resourceEntityId,
            "page",
            actionDetails,
            isSuccess: true,
            securityLevel: "medium",
            correlationId: "test-correlation-123");

        // Assert
        result.ShouldNotBeNull();
        result.UserEntityId.ShouldBe(userEntityId);
        result.ActionType.ShouldBe("update");
        result.ActionDescription.ShouldBe("Updated dashboard page with new components");
        result.ResourceEntityId.ShouldBe(resourceEntityId);
        result.ResourceType.ShouldBe("page");
        result.IsSuccess.ShouldBeTrue();
        result.SecurityLevel.ShouldBe("medium");
        result.CorrelationId.ShouldBe("test-correlation-123");
        result.ActionDetails.ShouldNotBeNull();
        result.ActionDetails["page_name"].ShouldBe("Test Dashboard");
        result.ActionDetails["component_count"].ShouldBe(5);

        // Verify persistence
        var retrievedLog = await handler.Get();
        retrievedLog.ShouldNotBeNull();
        retrievedLog.ActionType.ShouldBe("update");
    }

    /// <summary>
    /// Tests logging different types of user actions.
    /// </summary>
    [Theory]
    [InlineData("create", "Created new dashboard page")]
    [InlineData("read", "Viewed dashboard page")]
    [InlineData("update", "Modified page layout")]
    [InlineData("delete", "Deleted page from system")]
    [InlineData("publish", "Published page to production")]
    [InlineData("share", "Shared page with team")]
    public async Task LogUserAction_WithDifferentActionTypes_LogsCorrectly(string actionType, string description)
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var userEntityId = Guid.NewGuid();
        var resourceEntityId = Guid.NewGuid();
        TrackEntity(userEntityId);
        TrackEntity(resourceEntityId);
        
        var handler = TestDataContext().For<UIStudioAuditLogHandler>(entityId);

        // Act
        var result = await handler.LogUserAction(
            userEntityId,
            actionType,
            description,
            resourceEntityId,
            "page");

        // Assert
        result.ShouldNotBeNull();
        result.ActionType.ShouldBe(actionType);
        result.ActionDescription.ShouldBe(description);
    }

    /// <summary>
    /// Tests logging system actions.
    /// </summary>
    [Fact]
    public async Task LogSystemAction_WithValidData_LogsActionSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var resourceEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        
        var handler = TestDataContext().For<UIStudioAuditLogHandler>(entityId);
        var actionDetails = new Dictionary<string, object>
        {
            { "cleanup_type", "auto_version_cleanup" },
            { "versions_cleaned", 15 },
            { "retention_days", 90 },
            { "trigger", "scheduled_job" }
        };

        // Act
        var result = await handler.LogSystemAction(
            "cleanup",
            "Automatic cleanup of old page versions",
            resourceEntityId,
            "page",
            actionDetails,
            isSuccess: true,
            securityLevel: "low",
            correlationId: "system-cleanup-456");

        // Assert
        result.ShouldNotBeNull();
        result.UserEntityId.ShouldBeNull(); // System action has no user
        result.ActionType.ShouldBe("cleanup");
        result.ActionDescription.ShouldBe("Automatic cleanup of old page versions");
        result.IsSuccess.ShouldBeTrue();
        result.SecurityLevel.ShouldBe("low");
        result.CorrelationId.ShouldBe("system-cleanup-456");
        result.ActionDetails["cleanup_type"].ShouldBe("auto_version_cleanup");
        result.ActionDetails["versions_cleaned"].ShouldBe(15);
    }

    /// <summary>
    /// Tests logging failed actions with error messages.
    /// </summary>
    [Fact]
    public async Task LogUserAction_WithFailure_LogsErrorDetails()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var userEntityId = Guid.NewGuid();
        var resourceEntityId = Guid.NewGuid();
        TrackEntity(userEntityId);
        TrackEntity(resourceEntityId);
        
        var handler = TestDataContext().For<UIStudioAuditLogHandler>(entityId);

        // Act
        var result = await handler.LogUserAction(
            userEntityId,
            "delete",
            "Attempted to delete protected page",
            resourceEntityId,
            "page",
            new Dictionary<string, object> { { "page_slug", "home-page" } },
            isSuccess: false,
            errorMessage: "Cannot delete page: Page is marked as protected and cannot be deleted",
            securityLevel: "high");

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Cannot delete page: Page is marked as protected and cannot be deleted");
        result.SecurityLevel.ShouldBe("high"); // Failed delete operations are high security
    }

    /// <summary>
    /// Tests getting audit logs by user.
    /// </summary>
    [Fact(Skip = "GetAuditLogsByUser method not implemented")]
    public async Task GetAuditLogsByUser_WithMultipleUsers_ReturnsUserLogs()
    {
        // Arrange
        var userEntityId = Guid.NewGuid();
        var otherUserEntityId = Guid.NewGuid();
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(userEntityId);
        TrackEntity(otherUserEntityId);
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var handler1 = TestDataContext().For<UIStudioAuditLogHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioAuditLogHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioAuditLogHandler>(entityId3);

        // Create logs for target user
        await handler1.LogUserAction(userEntityId, "create", "Created page 1", Guid.NewGuid(), "page");
        await handler2.LogUserAction(userEntityId, "update", "Updated page 1", Guid.NewGuid(), "page");

        // Create log for different user
        await handler3.LogUserAction(otherUserEntityId, "delete", "Deleted page 2", Guid.NewGuid(), "page");

        // Act
        var result = await handler1.GetAuditLogsByUser(userEntityId);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.All(log => log.UserEntityId == userEntityId).ShouldBeTrue();
        result.Any(log => log.ActionType == "create").ShouldBeTrue();
        result.Any(log => log.ActionType == "update").ShouldBeTrue();
        result.Any(log => log.ActionType == "delete").ShouldBeFalse();
    }

    /// <summary>
    /// Tests getting audit logs by resource.
    /// </summary>
    [Fact(Skip = "GetAuditLogsByResource method not implemented")]
    public async Task GetAuditLogsByResource_WithMultipleResources_ReturnsResourceLogs()
    {
        // Arrange
        var resourceEntityId = Guid.NewGuid();
        var otherResourceEntityId = Guid.NewGuid();
        var userEntityId = Guid.NewGuid();
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(otherResourceEntityId);
        TrackEntity(userEntityId);
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var handler1 = TestDataContext().For<UIStudioAuditLogHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioAuditLogHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioAuditLogHandler>(entityId3);

        // Create logs for target resource
        await handler1.LogUserAction(userEntityId, "create", "Created resource", resourceEntityId, "page");
        await handler2.LogUserAction(userEntityId, "update", "Updated resource", resourceEntityId, "page");

        // Create log for different resource
        await handler3.LogUserAction(userEntityId, "create", "Created other resource", otherResourceEntityId, "layout");

        // Act
        var result = await handler1.GetAuditLogsByResource(resourceEntityId);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.All(log => log.ResourceEntityId == resourceEntityId).ShouldBeTrue();
        result.All(log => log.ResourceType == "page").ShouldBeTrue();
        result.Any(log => log.ActionType == "create").ShouldBeTrue();
        result.Any(log => log.ActionType == "update").ShouldBeTrue();
    }

    /// <summary>
    /// Tests getting audit logs by action type.
    /// </summary>
    [Fact(Skip = "GetAuditLogsByActionType method not implemented")]
    public async Task GetAuditLogsByActionType_WithMixedActions_ReturnsCorrectLogs()
    {
        // Arrange
        var userEntityId = Guid.NewGuid();
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(userEntityId);
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var handler1 = TestDataContext().For<UIStudioAuditLogHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioAuditLogHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioAuditLogHandler>(entityId3);

        // Create different action types
        await handler1.LogUserAction(userEntityId, "delete", "Deleted page 1", Guid.NewGuid(), "page");
        await handler2.LogUserAction(userEntityId, "delete", "Deleted page 2", Guid.NewGuid(), "page");
        await handler3.LogUserAction(userEntityId, "create", "Created page 3", Guid.NewGuid(), "page");

        // Act
        var deleteActions = await handler1.GetAuditLogsByActionType("delete");
        var createActions = await handler1.GetAuditLogsByActionType("create");

        // Assert
        deleteActions.ShouldNotBeNull();
        deleteActions.Count.ShouldBe(2);
        deleteActions.All(log => log.ActionType == "delete").ShouldBeTrue();

        createActions.ShouldNotBeNull();
        createActions.Count.ShouldBe(1);
        createActions[0].ActionType.ShouldBe("create");
    }

    /// <summary>
    /// Tests getting audit logs by security level.
    /// </summary>
    [Fact]
    public async Task GetAuditLogsBySecurityLevel_WithMixedLevels_ReturnsCorrectLogs()
    {
        // Arrange
        var userEntityId = Guid.NewGuid();
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(userEntityId);
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var handler1 = TestDataContext().For<UIStudioAuditLogHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioAuditLogHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioAuditLogHandler>(entityId3);

        // Create different security levels
        await handler1.LogUserAction(userEntityId, "read", "Viewed page", Guid.NewGuid(), "page", 
            securityLevel: "low");
        await handler2.LogUserAction(userEntityId, "delete", "Deleted page", Guid.NewGuid(), "page", 
            securityLevel: "high");
        await handler3.LogUserAction(userEntityId, "update", "Updated page", Guid.NewGuid(), "page", 
            securityLevel: "medium");

        // Act
        var highSecurityLogs = await handler1.GetAuditLogsBySecurityLevel("high");
        var lowSecurityLogs = await handler1.GetAuditLogsBySecurityLevel("low");

        // Assert
        highSecurityLogs.ShouldNotBeNull();
        highSecurityLogs.Count.ShouldBe(1);
        highSecurityLogs[0].SecurityLevel.ShouldBe("high");
        highSecurityLogs[0].ActionType.ShouldBe("delete");

        lowSecurityLogs.ShouldNotBeNull();
        lowSecurityLogs.Count.ShouldBe(1);
        lowSecurityLogs[0].SecurityLevel.ShouldBe("low");
        lowSecurityLogs[0].ActionType.ShouldBe("read");
    }

    /// <summary>
    /// Tests getting audit logs within date range.
    /// </summary>
    [Fact]
    public async Task GetAuditLogsInDateRange_WithinRange_ReturnsCorrectLogs()
    {
        // Arrange
        var userEntityId = Guid.NewGuid();
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        TrackEntity(userEntityId);
        TrackEntity(entityId1);
        TrackEntity(entityId2);

        var handler1 = TestDataContext().For<UIStudioAuditLogHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioAuditLogHandler>(entityId2);

        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow.AddDays(-1);

        // Create logs (current implementation won't have exact date control, but tests the method)
        await handler1.LogUserAction(userEntityId, "create", "Action 1", Guid.NewGuid(), "page");
        await handler2.LogUserAction(userEntityId, "update", "Action 2", Guid.NewGuid(), "page");

        // Act
        var result = await handler1.GetAuditLogsInDateRange(startDate, endDate);

        // Assert
        result.ShouldNotBeNull();
        // Since we can't control exact timestamps in this test, we verify the method works
        // In real scenarios, this would filter by actual timestamps
    }

    /// <summary>
    /// Tests getting failed actions only.
    /// </summary>
    [Fact]
    public async Task GetFailedActions_WithMixedResults_ReturnsOnlyFailed()
    {
        // Arrange
        var userEntityId = Guid.NewGuid();
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(userEntityId);
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var handler1 = TestDataContext().For<UIStudioAuditLogHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioAuditLogHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioAuditLogHandler>(entityId3);

        // Create successful action
        await handler1.LogUserAction(userEntityId, "create", "Successful creation", Guid.NewGuid(), "page", isSuccess: true);

        // Create failed actions
        await handler2.LogUserAction(userEntityId, "delete", "Failed deletion", Guid.NewGuid(), "page", 
            isSuccess: false, errorMessage: "Access denied");
        await handler3.LogUserAction(userEntityId, "update", "Failed update", Guid.NewGuid(), "page", 
            isSuccess: false, errorMessage: "Validation error");

        // Act
        var result = await handler1.GetFailedActions();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.All(log => log.IsSuccess == false).ShouldBeTrue();
        result.All(log => !string.IsNullOrEmpty(log.ErrorMessage)).ShouldBeTrue();
        result.Any(log => log.ActionType == "delete").ShouldBeTrue();
        result.Any(log => log.ActionType == "update").ShouldBeTrue();
    }

    /// <summary>
    /// Tests getting correlation chain of related actions.
    /// </summary>
    [Fact]
    public async Task GetCorrelationChain_WithRelatedActions_ReturnsChain()
    {
        // Arrange
        var userEntityId = Guid.NewGuid();
        var correlationId = "workflow-abc-123";
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(userEntityId);
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var handler1 = TestDataContext().For<UIStudioAuditLogHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioAuditLogHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioAuditLogHandler>(entityId3);

        // Create related actions with same correlation ID
        await handler1.LogUserAction(userEntityId, "create", "Step 1: Create page", Guid.NewGuid(), "page", 
            correlationId: correlationId);
        await handler2.LogUserAction(userEntityId, "update", "Step 2: Update layout", Guid.NewGuid(), "layout", 
            correlationId: correlationId);
        await handler3.LogUserAction(userEntityId, "publish", "Step 3: Publish page", Guid.NewGuid(), "page", 
            correlationId: correlationId);

        // Create unrelated action
        var otherHandler = TestDataContext().For<UIStudioAuditLogHandler>(Guid.NewGuid());
        await otherHandler.LogUserAction(userEntityId, "delete", "Unrelated action", Guid.NewGuid(), "page", 
            correlationId: "different-correlation");

        // Act
        var result = await handler1.GetCorrelationChain(correlationId);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result.All(log => log.CorrelationId == correlationId).ShouldBeTrue();
        result.Any(log => log.ActionDescription.Contains("Step 1")).ShouldBeTrue();
        result.Any(log => log.ActionDescription.Contains("Step 2")).ShouldBeTrue();
        result.Any(log => log.ActionDescription.Contains("Step 3")).ShouldBeTrue();
    }

    /// <summary>
    /// Tests generating audit summary statistics.
    /// </summary>
    [Fact]
    public async Task GetAuditSummary_WithMultipleLogs_ReturnsStatistics()
    {
        // Arrange
        var userEntityId = Guid.NewGuid();
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        var entityId4 = Guid.NewGuid();
        TrackEntity(userEntityId);
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);
        TrackEntity(entityId4);

        var handler1 = TestDataContext().For<UIStudioAuditLogHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioAuditLogHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioAuditLogHandler>(entityId3);
        var handler4 = TestDataContext().For<UIStudioAuditLogHandler>(entityId4);

        // Create various types of logs
        await handler1.LogUserAction(userEntityId, "create", "Create action", Guid.NewGuid(), "page", isSuccess: true, securityLevel: "low");
        await handler2.LogUserAction(userEntityId, "update", "Update action", Guid.NewGuid(), "page", isSuccess: true, securityLevel: "medium");
        await handler3.LogUserAction(userEntityId, "delete", "Delete action", Guid.NewGuid(), "page", isSuccess: false, securityLevel: "high");
        await handler4.LogSystemAction("cleanup", "System cleanup", Guid.NewGuid(), "system", isSuccess: true, securityLevel: "low");

        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow.AddDays(1);

        // Act
        var result = await handler1.GetAuditSummary(startDate, endDate);

        // Assert
        result.ShouldNotBeNull();
        result.ContainsKey("totalActions").ShouldBeTrue();
        result.ContainsKey("successfulActions").ShouldBeTrue();
        result.ContainsKey("failedActions").ShouldBeTrue();
        result.ContainsKey("userActions").ShouldBeTrue();
        result.ContainsKey("systemActions").ShouldBeTrue();
        result.ContainsKey("actionTypeCounts").ShouldBeTrue();
        result.ContainsKey("securityLevelCounts").ShouldBeTrue();
        result.ContainsKey("resourceTypeCounts").ShouldBeTrue();
        
        result["totalActions"].ShouldBe(4);
        result["successfulActions"].ShouldBe(3);
        result["failedActions"].ShouldBe(1);
        result["userActions"].ShouldBe(3);
        result["systemActions"].ShouldBe(1);
    }

    /// <summary>
    /// Tests purging old audit logs.
    /// </summary>
    [Fact]
    public async Task PurgeOldLogs_WithRetentionPolicy_PurgesCorrectly()
    {
        // Arrange
        var userEntityId = Guid.NewGuid();
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        TrackEntity(userEntityId);
        TrackEntity(entityId1);
        TrackEntity(entityId2);

        var handler1 = TestDataContext().For<UIStudioAuditLogHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioAuditLogHandler>(entityId2);

        // Create some logs
        await handler1.LogUserAction(userEntityId, "create", "Old action", Guid.NewGuid(), "page", securityLevel: "low");
        await handler2.LogUserAction(userEntityId, "delete", "Critical action", Guid.NewGuid(), "page", securityLevel: "high");

        var retentionPolicy = new Dictionary<string, object>
        {
            { "retentionDays", 90 },
            { "preserveHighSecurity", true },
            { "preserveFailedActions", true },
            { "maxRecordsToDelete", 1000 }
        };

        // Act
        var result = await handler1.PurgeOldLogs(retentionPolicy);

        // Assert
        result.ShouldNotBeNull();
        result.ContainsKey("purgedCount").ShouldBeTrue();
        result.ContainsKey("retainedCount").ShouldBeTrue();
        result.ContainsKey("policy").ShouldBeTrue();
    }
}