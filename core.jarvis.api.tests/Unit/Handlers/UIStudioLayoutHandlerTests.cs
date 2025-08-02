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
/// Unit tests for UIStudioLayoutHandler.
/// Tests CRUD operations, grid configuration management, and responsive layout handling.
/// </summary>
public class UIStudioLayoutHandlerTests : ApiIntegrationTestBase
{
    /// <summary>
    /// Tests creating a new layout with valid configuration.
    /// </summary>
    [Fact]
    public async Task CreateLayout_WithValidConfiguration_CreatesLayoutSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        
        var handler = TestDataContext().For<UIStudioLayoutHandler>(entityId);
        var gridConfig = new Dictionary<string, object>
        {
            { "type", "bento" },
            { "columns", 12 },
            { "rows", 8 },
            { "gap", 16 },
            { "responsive", true }
        };
        
        var layout = new UIStudioLayout
        {
            OwnerEntityId = entityId,
            LayoutName = "Test Bento Layout",
            LayoutType = "bento",
            GridConfig = gridConfig,
            CreatedByEntityId = creatorEntityId,
            IsTemplate = false
        };

        // Act
        var result = await handler.CreateLayout(layout);

        // Assert
        result.ShouldNotBeNull();
        result.LayoutName.ShouldBe("Test Bento Layout");
        result.LayoutType.ShouldBe("bento");
        result.GridConfig.ShouldNotBeNull();
        result.GridConfig["type"].ShouldBe("bento");
        result.GridConfig["columns"].ShouldBe(12);
        result.CreatedByEntityId.ShouldBe(creatorEntityId);
        result.IsTemplate.ShouldBeFalse();

        // Verify persistence
        var retrievedLayout = await handler.Get();
        retrievedLayout.ShouldNotBeNull();
        retrievedLayout.LayoutName.ShouldBe("Test Bento Layout");
    }

    /// <summary>
    /// Tests creating a layout with responsive breakpoints.
    /// </summary>
    [Fact]
    public async Task CreateLayout_WithResponsiveBreakpoints_CreatesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        
        var handler = TestDataContext().For<UIStudioLayoutHandler>(entityId);
        var responsiveBreakpoints = new Dictionary<string, object>
        {
            { "mobile", new Dictionary<string, object> { { "columns", 4 }, { "rows", 12 } } },
            { "tablet", new Dictionary<string, object> { { "columns", 8 }, { "rows", 10 } } },
            { "desktop", new Dictionary<string, object> { { "columns", 12 }, { "rows", 8 } } }
        };
        
        var gridConfig = new Dictionary<string, object>
        {
            { "type", "bento" },
            { "columns", 12 },
            { "rows", 8 },
            { "responsive", true },
            { "breakpoints", responsiveBreakpoints }
        };
        
        var layout = new UIStudioLayout
        {
            OwnerEntityId = entityId,
            LayoutName = "Responsive Layout",
            LayoutType = "bento",
            GridConfig = gridConfig,
            ResponsiveBreakpoints = responsiveBreakpoints,
            CreatedByEntityId = creatorEntityId
        };

        // Act
        var result = await handler.CreateLayout(layout);

        // Assert
        result.ShouldNotBeNull();
        result.ResponsiveBreakpoints.ShouldNotBeNull();
        result.ResponsiveBreakpoints.Count.ShouldBe(3);
        result.ResponsiveBreakpoints.ContainsKey("mobile").ShouldBeTrue();
        result.ResponsiveBreakpoints.ContainsKey("tablet").ShouldBeTrue();
        result.ResponsiveBreakpoints.ContainsKey("desktop").ShouldBeTrue();
    }

    /// <summary>
    /// Tests updating an existing layout configuration.
    /// </summary>
    [Fact]
    public async Task UpdateLayout_WithValidChanges_UpdatesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        var modifierEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        TrackEntity(modifierEntityId);

        var handler = TestDataContext().For<UIStudioLayoutHandler>(entityId);
        var originalConfig = new Dictionary<string, object>
        {
            { "type", "bento" },
            { "columns", 8 },
            { "rows", 6 }
        };
        
        var originalLayout = new UIStudioLayout
        {
            OwnerEntityId = entityId,
            LayoutName = "Original Layout",
            LayoutType = "bento",
            GridConfig = originalConfig,
            CreatedByEntityId = creatorEntityId
        };

        await handler.CreateLayout(originalLayout);

        var updatedConfig = new Dictionary<string, object>
        {
            { "type", "bento" },
            { "columns", 12 },
            { "rows", 8 },
            { "gap", 20 }
        };

        var updatedLayout = originalLayout with
        {
            LayoutName = "Updated Layout",
            GridConfig = updatedConfig,
            ModifiedByEntityId = modifierEntityId
        };

        // Act
        var result = await handler.UpdateLayout(updatedLayout);

        // Assert
        result.ShouldNotBeNull();
        result.LayoutName.ShouldBe("Updated Layout");
        result.GridConfig["columns"].ShouldBe(12);
        result.GridConfig["rows"].ShouldBe(8);
        result.GridConfig["gap"].ShouldBe(20);
        result.ModifiedByEntityId.ShouldBe(modifierEntityId);
    }

    /// <summary>
    /// Tests updating grid configuration separately.
    /// </summary>
    [Fact]
    public async Task UpdateGridConfig_WithNewConfiguration_UpdatesConfigSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioLayoutHandler>(entityId);
        var originalConfig = new Dictionary<string, object>
        {
            { "type", "bento" },
            { "columns", 8 },
            { "rows", 6 }
        };
        
        var layout = new UIStudioLayout
        {
            OwnerEntityId = entityId,
            LayoutName = "Test Layout",
            LayoutType = "bento",
            GridConfig = originalConfig,
            CreatedByEntityId = creatorEntityId
        };

        await handler.CreateLayout(layout);

        var newConfig = new Dictionary<string, object>
        {
            { "type", "bento" },
            { "columns", 16 },
            { "rows", 10 },
            { "gap", 24 },
            { "autoFit", true }
        };

        // Act
        var result = await handler.UpdateGridConfig(newConfig);

        // Assert
        result.ShouldNotBeNull();
        result.GridConfig["columns"].ShouldBe(16);
        result.GridConfig["rows"].ShouldBe(10);
        result.GridConfig["gap"].ShouldBe(24);
        result.GridConfig["autoFit"].ShouldBe(true);
        result.LastUpdated.ShouldBeGreaterThan(layout.LastUpdated);
    }

    /// <summary>
    /// Tests updating responsive breakpoints.
    /// </summary>
    [Fact]
    public async Task UpdateResponsiveBreakpoints_WithNewBreakpoints_UpdatesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioLayoutHandler>(entityId);
        var originalBreakpoints = new Dictionary<string, object>
        {
            { "mobile", new Dictionary<string, object> { { "columns", 4 } } }
        };
        
        var layout = new UIStudioLayout
        {
            OwnerEntityId = entityId,
            LayoutName = "Test Layout",
            LayoutType = "bento",
            GridConfig = new Dictionary<string, object> { { "type", "bento" } },
            ResponsiveBreakpoints = originalBreakpoints,
            CreatedByEntityId = creatorEntityId
        };

        await handler.CreateLayout(layout);

        var newBreakpoints = new Dictionary<string, object>
        {
            { "mobile", new Dictionary<string, object> { { "columns", 4 }, { "rows", 12 } } },
            { "tablet", new Dictionary<string, object> { { "columns", 8 }, { "rows", 10 } } },
            { "desktop", new Dictionary<string, object> { { "columns", 12 }, { "rows", 8 } } },
            { "xl", new Dictionary<string, object> { { "columns", 16 }, { "rows", 8 } } }
        };

        // Act
        var result = await handler.UpdateResponsiveBreakpoints(newBreakpoints);

        // Assert
        result.ShouldNotBeNull();
        result.ResponsiveBreakpoints.ShouldNotBeNull();
        result.ResponsiveBreakpoints.Count.ShouldBe(4);
        result.ResponsiveBreakpoints.ContainsKey("xl").ShouldBeTrue();
    }

    /// <summary>
    /// Tests creating a layout template.
    /// </summary>
    [Fact]
    public async Task CreateAsTemplate_WithValidLayout_CreatesTemplateSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioLayoutHandler>(entityId);
        var gridConfig = new Dictionary<string, object>
        {
            { "type", "bento" },
            { "columns", 12 },
            { "rows", 8 },
            { "templateAreas", new[] { "header", "sidebar", "main", "footer" } }
        };
        
        var layout = new UIStudioLayout
        {
            OwnerEntityId = entityId,
            LayoutName = "Dashboard Template",
            LayoutType = "bento",
            GridConfig = gridConfig,
            CreatedByEntityId = creatorEntityId,
            IsTemplate = false
        };

        await handler.CreateLayout(layout);

        // Act
        var result = await handler.CreateAsTemplate("Dashboard Template", "dashboard");

        // Assert
        result.ShouldNotBeNull();
        result.IsTemplate.ShouldBeTrue();
        result.TemplateName.ShouldBe("Dashboard Template");
        result.TemplateCategory.ShouldBe("dashboard");
    }

    /// <summary>
    /// Tests duplicating a layout.
    /// </summary>
    [Fact]
    public async Task DuplicateLayout_WithValidSource_CreatesDuplicateSuccessfully()
    {
        // Arrange
        var sourceEntityId = Guid.NewGuid();
        var targetEntityId = Guid.NewGuid();
        TrackEntity(sourceEntityId);
        TrackEntity(targetEntityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var sourceHandler = TestDataContext().For<UIStudioLayoutHandler>(sourceEntityId);
        var targetHandler = TestDataContext().For<UIStudioLayoutHandler>(targetEntityId);
        
        var gridConfig = new Dictionary<string, object>
        {
            { "type", "bento" },
            { "columns", 12 },
            { "rows", 8 },
            { "specialProperty", "unique-value" }
        };
        
        var sourceLayout = new UIStudioLayout
        {
            OwnerEntityId = sourceEntityId,
            LayoutName = "Source Layout",
            LayoutType = "bento",
            GridConfig = gridConfig,
            CreatedByEntityId = creatorEntityId
        };

        await sourceHandler.CreateLayout(sourceLayout);

        // Act
        var result = await targetHandler.DuplicateLayout(sourceEntityId, "Duplicated Layout");

        // Assert
        result.ShouldNotBeNull();
        result.LayoutName.ShouldBe("Duplicated Layout");
        result.LayoutType.ShouldBe("bento");
        result.GridConfig["specialProperty"].ShouldBe("unique-value");
        result.OwnerEntityId.ShouldBe(targetEntityId);
        result.OwnerEntityId.ShouldNotBe(sourceEntityId);
    }

    /// <summary>
    /// Tests validating grid configuration.
    /// </summary>
    [Fact]
    public async Task ValidateGridConfig_WithValidConfiguration_ReturnsTrue()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var handler = TestDataContext().For<UIStudioLayoutHandler>(entityId);
        
        var validConfig = new Dictionary<string, object>
        {
            { "type", "bento" },
            { "columns", 12 },
            { "rows", 8 },
            { "gap", 16 }
        };

        // Act
        var result = await handler.ValidateGridConfig(validConfig);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests validating invalid grid configuration.
    /// </summary>
    [Fact]
    public async Task ValidateGridConfig_WithInvalidConfiguration_ReturnsFalse()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var handler = TestDataContext().For<UIStudioLayoutHandler>(entityId);
        
        var invalidConfig = new Dictionary<string, object>
        {
            { "columns", -1 }, // Invalid: negative columns
            { "rows", 0 }       // Invalid: zero rows
        };

        // Act
        var result = await handler.ValidateGridConfig(invalidConfig);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// Tests getting layouts by type.
    /// </summary>
    [Fact]
    public async Task GetLayoutsByType_WithMixedTypes_ReturnsCorrectLayouts()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler1 = TestDataContext().For<UIStudioLayoutHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioLayoutHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioLayoutHandler>(entityId3);

        // Create bento layout
        var bentoLayout = new UIStudioLayout
        {
            OwnerEntityId = entityId1,
            LayoutName = "Bento Layout",
            LayoutType = "bento",
            GridConfig = new Dictionary<string, object> { { "type", "bento" } },
            CreatedByEntityId = creatorEntityId
        };

        // Create grid layout
        var gridLayout = new UIStudioLayout
        {
            OwnerEntityId = entityId2,
            LayoutName = "Grid Layout",
            LayoutType = "grid",
            GridConfig = new Dictionary<string, object> { { "type", "grid" } },
            CreatedByEntityId = creatorEntityId
        };

        // Create another bento layout
        var anotherBentoLayout = new UIStudioLayout
        {
            OwnerEntityId = entityId3,
            LayoutName = "Another Bento Layout",
            LayoutType = "bento",
            GridConfig = new Dictionary<string, object> { { "type", "bento" } },
            CreatedByEntityId = creatorEntityId
        };

        await handler1.CreateLayout(bentoLayout);
        await handler2.CreateLayout(gridLayout);
        await handler3.CreateLayout(anotherBentoLayout);

        // Act
        var result = await handler1.GetLayoutsByType("bento");

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.All(l => l.LayoutType == "bento").ShouldBeTrue();
        result.Any(l => l.LayoutName == "Bento Layout").ShouldBeTrue();
        result.Any(l => l.LayoutName == "Another Bento Layout").ShouldBeTrue();
    }

    /// <summary>
    /// Tests getting layout templates.
    /// </summary>
    [Fact]
    public async Task GetLayoutTemplates_WithMixedLayouts_ReturnsOnlyTemplates()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler1 = TestDataContext().For<UIStudioLayoutHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioLayoutHandler>(entityId2);

        // Create template layout
        var templateLayout = new UIStudioLayout
        {
            OwnerEntityId = entityId1,
            LayoutName = "Template Layout",
            LayoutType = "bento",
            GridConfig = new Dictionary<string, object> { { "type", "bento" } },
            CreatedByEntityId = creatorEntityId,
            IsTemplate = true,
            TemplateName = "Dashboard Template",
            TemplateCategory = "dashboard"
        };

        // Create regular layout
        var regularLayout = new UIStudioLayout
        {
            OwnerEntityId = entityId2,
            LayoutName = "Regular Layout",
            LayoutType = "bento",
            GridConfig = new Dictionary<string, object> { { "type", "bento" } },
            CreatedByEntityId = creatorEntityId,
            IsTemplate = false
        };

        await handler1.CreateLayout(templateLayout);
        await handler2.CreateLayout(regularLayout);

        // Act
        var result = await handler1.GetLayoutTemplates();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].IsTemplate.ShouldBeTrue();
        result[0].LayoutName.ShouldBe("Template Layout");
        result[0].TemplateName.ShouldBe("Dashboard Template");
    }

    /// <summary>
    /// Tests generating grid preview data.
    /// </summary>
    [Fact]
    public async Task GenerateGridPreview_WithValidConfiguration_ReturnsPreviewData()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var handler = TestDataContext().For<UIStudioLayoutHandler>(entityId);
        
        var gridConfig = new Dictionary<string, object>
        {
            { "type", "bento" },
            { "columns", 12 },
            { "rows", 8 },
            { "gap", 16 }
        };

        // Act
        var result = await handler.GenerateGridPreview(gridConfig);

        // Assert
        result.ShouldNotBeNull();
        result.ContainsKey("gridData").ShouldBeTrue();
        result.ContainsKey("dimensions").ShouldBeTrue();
        result.ContainsKey("cellSize").ShouldBeTrue();
    }

    /// <summary>
    /// Tests calculating layout metrics.
    /// </summary>
    [Fact]
    public async Task CalculateLayoutMetrics_WithValidLayout_ReturnsMetrics()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioLayoutHandler>(entityId);
        var gridConfig = new Dictionary<string, object>
        {
            { "type", "bento" },
            { "columns", 12 },
            { "rows", 8 },
            { "gap", 16 }
        };
        
        var layout = new UIStudioLayout
        {
            OwnerEntityId = entityId,
            LayoutName = "Metrics Test Layout",
            LayoutType = "bento",
            GridConfig = gridConfig,
            CreatedByEntityId = creatorEntityId
        };

        await handler.CreateLayout(layout);

        // Act
        var result = await handler.CalculateLayoutMetrics();

        // Assert
        result.ShouldNotBeNull();
        result.ContainsKey("totalCells").ShouldBeTrue();
        result.ContainsKey("usableCells").ShouldBeTrue();
        result.ContainsKey("efficiency").ShouldBeTrue();
        result["totalCells"].ShouldBe(96); // 12 * 8
    }
}