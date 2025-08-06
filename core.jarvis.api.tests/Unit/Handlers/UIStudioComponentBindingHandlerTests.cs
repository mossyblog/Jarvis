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
/// Unit tests for UIStudioComponentBindingHandler.
/// Tests component binding operations, field mappings, and data source connections.
/// </summary>
public class UIStudioComponentBindingHandlerTests : ApiIntegrationTestBase
{
    /// <summary>
    /// Tests creating a new component binding with valid configuration.
    /// </summary>
    [Fact]
    public async Task CreateBinding_WithValidConfiguration_CreatesBindingSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var pageEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(pageEntityId);
        TrackEntity(creatorEntityId);
        
        var handler = TestDataContext().For<UIStudioComponentBindingHandler>(entityId);
        var fieldMappings = new Dictionary<string, object>
        {
            { "title", "$.data.title" },
            { "description", "$.data.description" },
            { "imageUrl", "$.data.image.url" },
            { "status", "$.data.status" }
        };
        
        var binding = new UIStudioComponentBinding
        {
            OwnerEntityId = entityId,
            PageSlug = "test-page",
            ComponentType = "MetricCard",
            ComponentInstanceId = "metric-card-1",
            BoundComponentType = "MetricComponent",
            DataSourceType = "api",
            DataSourceConfig = new Dictionary<string, object>
            {
                { "endpoint", "/api/metrics/dashboard" },
                { "method", "GET" },
                { "refreshInterval", 30000 }
            },
            FieldMappings = fieldMappings,
            GridPosition = new Dictionary<string, object>
            {
                { "x", 0 },
                { "y", 0 },
                { "width", 4 },
                { "height", 2 }
            },
            CreatedByEntityId = creatorEntityId
        };

        // Act
        var result = await handler.CreateBinding(binding);

        // Assert
        result.ShouldNotBeNull();
        result.ComponentType.ShouldBe("MetricCard");
        result.ComponentInstanceId.ShouldBe("metric-card-1");
        result.DataSourceType.ShouldBe("api");
        result.FieldMappings.ShouldNotBeNull();
        result.FieldMappings["title"].ShouldBe("$.data.title");
        result.GridPosition.ShouldNotBeNull();
        result.GridPosition["width"].ShouldBe(4);
        result.IsEnabled.ShouldBeTrue();

        // Verify persistence
        var retrievedBinding = await handler.Get();
        retrievedBinding.ShouldNotBeNull();
        retrievedBinding.ComponentType.ShouldBe("MetricCard");
    }

    /// <summary>
    /// Tests creating a binding with static data source.
    /// </summary>
    [Fact]
    public async Task CreateBinding_WithStaticDataSource_CreatesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var pageEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(pageEntityId);
        TrackEntity(creatorEntityId);
        
        var handler = TestDataContext().For<UIStudioComponentBindingHandler>(entityId);
        var staticData = new Dictionary<string, object>
        {
            { "title", "Static Dashboard" },
            { "metrics", new[]
                {
                    new { name = "Users", value = 1250, trend = "up" },
                    new { name = "Revenue", value = 45000, trend = "up" },
                    new { name = "Orders", value = 320, trend = "down" }
                }
            }
        };
        
        var binding = new UIStudioComponentBinding
        {
            OwnerEntityId = entityId,
            PageSlug = "test-page",
            ComponentType = "Dashboard",
            ComponentInstanceId = "dashboard-1",
            BoundComponentType = "DashboardComponent",
            DataSourceType = "static",
            DataSourceConfig = new Dictionary<string, object>
            {
                { "data", staticData }
            },
            FieldMappings = new Dictionary<string, object>
            {
                { "title", "$.title" },
                { "metrics", "$.metrics" }
            },
            CreatedByEntityId = creatorEntityId
        };

        // Act
        var result = await handler.CreateBinding(binding);

        // Assert
        result.ShouldNotBeNull();
        result.DataSourceType.ShouldBe("static");
        result.DataSourceConfig["data"].ShouldNotBeNull();
    }

    /// <summary>
    /// Tests updating an existing component binding.
    /// </summary>
    [Fact]
    public async Task UpdateBinding_WithValidChanges_UpdatesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var pageEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        var modifierEntityId = Guid.NewGuid();
        TrackEntity(pageEntityId);
        TrackEntity(creatorEntityId);
        TrackEntity(modifierEntityId);

        var handler = TestDataContext().For<UIStudioComponentBindingHandler>(entityId);
        var originalBinding = new UIStudioComponentBinding
        {
            OwnerEntityId = entityId,
            PageSlug = "test-page",
            ComponentType = "MetricCard",
            ComponentInstanceId = "metric-card-1",
            BoundComponentType = "MetricComponent",
            DataSourceType = "api",
            DataSourceConfig = new Dictionary<string, object>
            {
                { "endpoint", "/api/metrics/old" },
                { "method", "GET" }
            },
            FieldMappings = new Dictionary<string, object>
            {
                { "title", "$.data.oldTitle" }
            },
            CreatedByEntityId = creatorEntityId
        };

        await handler.CreateBinding(originalBinding);

        var updatedBinding = originalBinding with
        {
            DataSourceConfig = new Dictionary<string, object>
            {
                { "endpoint", "/api/metrics/new" },
                { "method", "GET" },
                { "refreshInterval", 60000 }
            },
            FieldMappings = new Dictionary<string, object>
            {
                { "title", "$.data.newTitle" },
                { "subtitle", "$.data.subtitle" }
            },
            ModifiedByEntityId = modifierEntityId
        };

        // Act
        var result = await handler.UpdateBinding(updatedBinding);

        // Assert
        result.ShouldNotBeNull();
        result.DataSourceConfig["endpoint"].ShouldBe("/api/metrics/new");
        result.DataSourceConfig["refreshInterval"].ShouldBe(60000);
        result.FieldMappings["title"].ShouldBe("$.data.newTitle");
        result.FieldMappings.ContainsKey("subtitle").ShouldBeTrue();
        result.ModifiedByEntityId.ShouldBe(modifierEntityId);
    }

    /// <summary>
    /// Tests updating field mappings separately.
    /// </summary>
    [Fact]
    public async Task UpdateFieldMappings_WithNewMappings_UpdatesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var pageEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(pageEntityId);
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioComponentBindingHandler>(entityId);
        var binding = new UIStudioComponentBinding
        {
            OwnerEntityId = entityId,
            PageSlug = "test-page",
            ComponentType = "DataTable",
            ComponentInstanceId = "data-table-1",
            BoundComponentType = "TableComponent",
            DataSourceType = "api",
            DataSourceConfig = new Dictionary<string, object>
            {
                { "endpoint", "/api/users" }
            },
            FieldMappings = new Dictionary<string, object>
            {
                { "name", "$.data.name" },
                { "email", "$.data.email" }
            },
            CreatedByEntityId = creatorEntityId
        };

        await handler.CreateBinding(binding);

        var newMappings = new Dictionary<string, object>
        {
            { "name", "$.data.fullName" },
            { "email", "$.data.emailAddress" },
            { "phone", "$.data.phoneNumber" },
            { "role", "$.data.userRole" },
            { "lastLogin", "$.data.lastLoginDate" }
        };

        // Act
        var result = await handler.UpdateFieldMappings(newMappings);

        // Assert
        result.ShouldNotBeNull();
        result.FieldMappings.Count.ShouldBe(5);
        result.FieldMappings["name"].ShouldBe("$.data.fullName");
        result.FieldMappings["phone"].ShouldBe("$.data.phoneNumber");
        result.FieldMappings["role"].ShouldBe("$.data.userRole");
    }

    /// <summary>
    /// Tests updating position configuration (replaces UpdateGridPosition).
    /// </summary>
    [Fact]
    public async Task UpdatePosition_WithNewPosition_UpdatesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var pageEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(pageEntityId);
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioComponentBindingHandler>(entityId);
        var binding = new UIStudioComponentBinding
        {
            OwnerEntityId = entityId,
            PageSlug = "test-page",
            ComponentType = "Chart",
            ComponentInstanceId = "chart-1",
            BoundComponentType = "ChartComponent",
            DataSourceType = "api",
            PositionConfig = new Dictionary<string, object>
            {
                { "x", 0 },
                { "y", 0 },
                { "width", 6 },
                { "height", 4 }
            },
            CreatedByEntityId = creatorEntityId
        };

        await handler.CreateBinding(binding);

        var newPosition = new Dictionary<string, object>
        {
            { "x", 6 },
            { "y", 2 },
            { "width", 8 },
            { "height", 6 }
        };

        // Act
        var result = await handler.UpdatePosition(newPosition);

        // Assert
        result.ShouldNotBeNull();
        result.PositionConfig["x"].ShouldBe(6);
        result.PositionConfig["y"].ShouldBe(2);
        result.PositionConfig["width"].ShouldBe(8);
        result.PositionConfig["height"].ShouldBe(6);
    }

    /// <summary>
    /// Tests activating and deactivating bindings.
    /// </summary>
    [Fact]
    public async Task ToggleActivation_ChangesActiveStatus()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var pageEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(pageEntityId);
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioComponentBindingHandler>(entityId);
        var binding = new UIStudioComponentBinding
        {
            OwnerEntityId = entityId,
            PageSlug = "test-page",
            ComponentType = "Widget",
            ComponentInstanceId = "widget-1",
            BoundComponentType = "WidgetComponent",
            DataSourceType = "api",
            IsEnabled = true,
            CreatedByEntityId = creatorEntityId
        };

        await handler.CreateBinding(binding);

        // Act - Deactivate
        var deactivated = await handler.SetEnabled(false);

        // Assert
        deactivated.ShouldNotBeNull();
        deactivated.IsEnabled.ShouldBeFalse();

        // Act - Activate
        var activated = await handler.SetEnabled(true);

        // Assert
        activated.ShouldNotBeNull();
        activated.IsEnabled.ShouldBeTrue();
    }

    /// <summary>
    /// Tests getting bindings by page slug.
    /// </summary>
    [Fact]
    public async Task GetByPageSlug_WithMultipleBindings_ReturnsPageBindings()
    {
        // Arrange
        var pageEntityId = Guid.NewGuid();
        var otherPageEntityId = Guid.NewGuid();
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(pageEntityId);
        TrackEntity(otherPageEntityId);
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler1 = TestDataContext().For<UIStudioComponentBindingHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioComponentBindingHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioComponentBindingHandler>(entityId3);

        // Create bindings for target page
        var binding1 = new UIStudioComponentBinding
        {
            OwnerEntityId = entityId1,
            PageSlug = "test-page",
            ComponentType = "MetricCard",
            ComponentInstanceId = "metric-1",
            BoundComponentType = "MetricComponent",
            DataSourceType = "api",
            CreatedByEntityId = creatorEntityId
        };

        var binding2 = new UIStudioComponentBinding
        {
            OwnerEntityId = entityId2,
            PageSlug = "test-page",
            ComponentType = "Chart",
            ComponentInstanceId = "chart-1",
            BoundComponentType = "ChartComponent",
            DataSourceType = "api",
            CreatedByEntityId = creatorEntityId
        };

        // Create binding for different page
        var binding3 = new UIStudioComponentBinding
        {
            OwnerEntityId = entityId3,
            PageSlug = "other-test-page",
            ComponentType = "Table",
            ComponentInstanceId = "table-1",
            BoundComponentType = "TableComponent",
            DataSourceType = "api",
            CreatedByEntityId = creatorEntityId
        };

        await handler1.CreateBinding(binding1);
        await handler2.CreateBinding(binding2);
        await handler3.CreateBinding(binding3);

        // Act
        var result = await handler1.GetByPageSlug("test-page");

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.All(b => b.PageSlug == "test-page").ShouldBeTrue();
        result.Any(b => b.ComponentType == "MetricCard").ShouldBeTrue();
        result.Any(b => b.ComponentType == "Chart").ShouldBeTrue();
        result.Any(b => b.ComponentType == "Table").ShouldBeFalse();
    }

    /// <summary>
    /// Tests getting bindings by component type.
    /// </summary>
    [Fact]
    public async Task GetByComponentType_WithMixedTypes_ReturnsCorrectBindings()
    {
        // Arrange
        var pageEntityId = Guid.NewGuid();
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(pageEntityId);
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler1 = TestDataContext().For<UIStudioComponentBindingHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioComponentBindingHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioComponentBindingHandler>(entityId3);

        // Create MetricCard bindings
        var metricCard1 = new UIStudioComponentBinding
        {
            OwnerEntityId = entityId1,
            PageSlug = "test-page",
            ComponentType = "MetricCard",
            ComponentInstanceId = "metric-1",
            BoundComponentType = "MetricComponent",
            DataSourceType = "api",
            CreatedByEntityId = creatorEntityId
        };

        var metricCard2 = new UIStudioComponentBinding
        {
            OwnerEntityId = entityId2,
            PageSlug = "test-page",
            ComponentType = "MetricCard",
            ComponentInstanceId = "metric-2",
            BoundComponentType = "MetricComponent",
            DataSourceType = "api",
            CreatedByEntityId = creatorEntityId
        };

        // Create different component type
        var chart = new UIStudioComponentBinding
        {
            OwnerEntityId = entityId3,
            PageSlug = "test-page",
            ComponentType = "Chart",
            ComponentInstanceId = "chart-1",
            BoundComponentType = "ChartComponent",
            DataSourceType = "api",
            CreatedByEntityId = creatorEntityId
        };

        await handler1.CreateBinding(metricCard1);
        await handler2.CreateBinding(metricCard2);
        await handler3.CreateBinding(chart);

        // Act
        var result = await handler1.GetByComponentType("MetricCard");

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.All(b => b.ComponentType == "MetricCard").ShouldBeTrue();
        result.Any(b => b.ComponentInstanceId == "metric-1").ShouldBeTrue();
        result.Any(b => b.ComponentInstanceId == "metric-2").ShouldBeTrue();
    }

    /// <summary>
    /// Tests validating field mappings.
    /// </summary>
    [Fact(Skip = "ValidateFieldMappings method not implemented")]
    public async Task ValidateFieldMappings_WithValidMappings_ReturnsTrue()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var handler = TestDataContext().For<UIStudioComponentBindingHandler>(entityId);
        
        var validMappings = new Dictionary<string, object>
        {
            { "title", "$.data.title" },
            { "description", "$.data.description" },
            { "value", "$.data.metrics.total" },
            { "trend", "$.data.metrics.trend" }
        };

        // Act
        var result = await handler.ValidateFieldMappings(validMappings);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests validating invalid field mappings.
    /// </summary>
    [Fact(Skip = "ValidateFieldMappings method not implemented")]
    public async Task ValidateFieldMappings_WithInvalidMappings_ReturnsFalse()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var handler = TestDataContext().For<UIStudioComponentBindingHandler>(entityId);
        
        var invalidMappings = new Dictionary<string, object>
        {
            { "", "$.data.title" },                    // Empty field name
            { "title", "" },                           // Empty JSONPath
            { "invalid", "not.a.jsonpath" },          // Invalid JSONPath syntax
            { "another", null! }                       // Null mapping
        };

        // Act
        var result = await handler.ValidateFieldMappings(invalidMappings);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// Tests cloning a binding to a different page.
    /// </summary>
    [Fact(Skip = "CloneToPage method not implemented")]
    public async Task CloneToPage_WithValidBinding_CreatesCloneSuccessfully()
    {
        // Arrange
        var sourceEntityId = Guid.NewGuid();
        var targetEntityId = Guid.NewGuid();
        var sourcePageEntityId = Guid.NewGuid();
        var targetPageEntityId = Guid.NewGuid();
        TrackEntity(sourceEntityId);
        TrackEntity(targetEntityId);
        TrackEntity(sourcePageEntityId);
        TrackEntity(targetPageEntityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var sourceHandler = TestDataContext().For<UIStudioComponentBindingHandler>(sourceEntityId);
        var targetHandler = TestDataContext().For<UIStudioComponentBindingHandler>(targetEntityId);

        var sourceBinding = new UIStudioComponentBinding
        {
            OwnerEntityId = sourceEntityId,
            PageSlug = "source-page",
            ComponentType = "MetricCard",
            ComponentInstanceId = "metric-original",
            BoundComponentType = "MetricComponent",
            DataSourceType = "api",
            DataSourceConfig = new Dictionary<string, object>
            {
                { "endpoint", "/api/metrics/source" },
                { "method", "GET" }
            },
            FieldMappings = new Dictionary<string, object>
            {
                { "title", "$.data.title" },
                { "value", "$.data.value" }
            },
            GridPosition = new Dictionary<string, object>
            {
                { "x", 0 },
                { "y", 0 },
                { "width", 4 },
                { "height", 2 }
            },
            CreatedByEntityId = creatorEntityId
        };

        await sourceHandler.CreateBinding(sourceBinding);

        // Act
        var result = await targetHandler.CloneToPage(sourceEntityId, "target-page", "metric-cloned");

        // Assert
        result.ShouldNotBeNull();
        result.PageSlug.ShouldBe("target-page");
        result.ComponentInstanceId.ShouldBe("metric-cloned");
        result.ComponentType.ShouldBe("MetricCard");
        result.DataSourceConfig.ShouldNotBeNull();
        result.DataSourceConfig["endpoint"].ShouldBe("/api/metrics/source");
        result.FieldMappings["title"].ShouldBe("$.data.title");
        result.OwnerEntityId.ShouldBe(targetEntityId);
        result.OwnerEntityId.ShouldNotBe(sourceEntityId);
    }

    /// <summary>
    /// Tests testing data source connectivity.
    /// </summary>
    [Fact(Skip = "TestDataSourceConnection method not implemented")]
    public async Task TestDataSourceConnection_WithValidConfig_ReturnsConnectionResult()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var handler = TestDataContext().For<UIStudioComponentBindingHandler>(entityId);
        
        var dataSourceConfig = new Dictionary<string, object>
        {
            { "endpoint", "/api/test/connection" },
            { "method", "GET" },
            { "timeout", 5000 }
        };

        // Act
        var result = await handler.TestDataSourceConnection(dataSourceConfig);

        // Assert
        result.ShouldNotBeNull();
        result.ContainsKey("isConnected").ShouldBeTrue();
        result.ContainsKey("responseTime").ShouldBeTrue();
        result.ContainsKey("statusCode").ShouldBeTrue();
    }

    /// <summary>
    /// Tests getting binding dependencies.
    /// </summary>
    [Fact(Skip = "GetDependencies method not implemented")]
    public async Task GetDependencies_WithBindingHavingDependencies_ReturnsDependencies()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var pageEntityId = Guid.NewGuid();
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(pageEntityId);
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioComponentBindingHandler>(entityId);
        var binding = new UIStudioComponentBinding
        {
            OwnerEntityId = entityId,
            PageSlug = "test-page",
            ComponentType = "DependentChart",
            ComponentInstanceId = "dependent-chart-1",
            BoundComponentType = "ChartComponent",
            DataSourceType = "computed",
            DataSourceConfig = new Dictionary<string, object>
            {
                { "dependsOn", new[] { "metric-1", "metric-2" } },
                { "computation", "sum" }
            },
            FieldMappings = new Dictionary<string, object>
            {
                { "value", "$.computed.total" }
            },
            CreatedByEntityId = creatorEntityId
        };

        await handler.CreateBinding(binding);

        // Act
        var result = await handler.GetDependencies();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThan(0);
    }
}