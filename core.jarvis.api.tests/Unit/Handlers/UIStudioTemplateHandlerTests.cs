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
/// Unit tests for UIStudioTemplateHandler.
/// Tests template management, categorization, and template application functionality.
/// </summary>
public class UIStudioTemplateHandlerTests : ApiIntegrationTestBase
{
    /// <summary>
    /// Tests creating a new template with valid configuration.
    /// </summary>
    [Fact]
    public async Task CreateTemplate_WithValidConfiguration_CreatesTemplateSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        
        var handler = TestDataContext().For<UIStudioTemplateHandler>(entityId);
        var templateData = new Dictionary<string, object>
        {
            { "page", new { name = "Dashboard Template", type = "dashboard" } },
            { "layout", new { type = "bento", columns = 12, rows = 8 } },
            { "components", new[]
                {
                    new { type = "MetricCard", position = new { x = 0, y = 0, width = 4, height = 2 } },
                    new { type = "Chart", position = new { x = 4, y = 0, width = 8, height = 4 } },
                    new { type = "Table", position = new { x = 0, y = 2, width = 12, height = 6 } }
                }
            }
        };
        
        var template = new UIStudioTemplate
        {
            OwnerEntityId = entityId,
            TemplateName = "Executive Dashboard",
            TemplateType = "page",
            Category = "dashboard",
            Description = "A comprehensive executive dashboard template with key metrics and charts",
            TemplateData = templateData,
            IsPublic = true,
            CreatedByEntityId = creatorEntityId,
            Tags = new[] { "dashboard", "executive", "metrics", "charts" }
        };

        // Act
        var result = await handler.CreateTemplate(template);

        // Assert
        result.ShouldNotBeNull();
        result.TemplateName.ShouldBe("Executive Dashboard");
        result.TemplateType.ShouldBe("page");
        result.Category.ShouldBe("dashboard");
        result.IsPublic.ShouldBeTrue();
        result.TemplateData.ShouldNotBeNull();
        result.TemplateData.ContainsKey("page").ShouldBeTrue();
        result.TemplateData.ContainsKey("layout").ShouldBeTrue();
        result.TemplateData.ContainsKey("components").ShouldBeTrue();
        result.Tags.ShouldNotBeNull();
        result.Tags.Length.ShouldBe(4);

        // Verify persistence
        var retrievedTemplate = await handler.Get();
        retrievedTemplate.ShouldNotBeNull();
        retrievedTemplate.TemplateName.ShouldBe("Executive Dashboard");
    }

    /// <summary>
    /// Tests creating private templates.
    /// </summary>
    [Fact]
    public async Task CreateTemplate_AsPrivate_CreatesPrivateTemplate()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        
        var handler = TestDataContext().For<UIStudioTemplateHandler>(entityId);
        var template = new UIStudioTemplate
        {
            OwnerEntityId = entityId,
            TemplateName = "Private Dashboard",
            TemplateType = "page",
            Category = "personal",
            Description = "Personal dashboard template",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = false,
            CreatedByEntityId = creatorEntityId
        };

        // Act
        var result = await handler.CreateTemplate(template);

        // Assert
        result.ShouldNotBeNull();
        result.IsPublic.ShouldBeFalse();
        result.Category.ShouldBe("personal");
    }

    /// <summary>
    /// Tests updating an existing template.
    /// </summary>
    [Fact]
    public async Task UpdateTemplate_WithValidChanges_UpdatesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        var modifierEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        TrackEntity(modifierEntityId);

        var handler = TestDataContext().For<UIStudioTemplateHandler>(entityId);
        var originalTemplate = new UIStudioTemplate
        {
            OwnerEntityId = entityId,
            TemplateName = "Original Template",
            TemplateType = "page",
            Category = "basic",
            Description = "Original description",
            TemplateData = new Dictionary<string, object> { { "version", 1 } },
            IsPublic = false,
            CreatedByEntityId = creatorEntityId,
            Tags = new[] { "basic" }
        };

        await handler.CreateTemplate(originalTemplate);

        var updatedTemplate = originalTemplate with
        {
            TemplateName = "Updated Template",
            Description = "Updated description with more details",
            Category = "advanced",
            TemplateData = new Dictionary<string, object> 
            { 
                { "version", 2 }, 
                { "features", new[] { "responsive", "interactive" } } 
            },
            IsPublic = true,
            ModifiedByEntityId = modifierEntityId,
            Tags = new[] { "basic", "advanced", "responsive" }
        };

        // Act
        var result = await handler.UpdateTemplate(updatedTemplate);

        // Assert
        result.ShouldNotBeNull();
        result.TemplateName.ShouldBe("Updated Template");
        result.Description.ShouldBe("Updated description with more details");
        result.Category.ShouldBe("advanced");
        result.IsPublic.ShouldBeTrue();
        result.TemplateData["version"].ShouldBe(2);
        result.ModifiedByEntityId.ShouldBe(modifierEntityId);
        result.Tags.Length.ShouldBe(3);
    }

    /// <summary>
    /// Tests cloning a template.
    /// </summary>
    [Fact]
    public async Task CloneTemplate_WithValidSource_CreatesCopySuccessfully()
    {
        // Arrange
        var sourceEntityId = Guid.NewGuid();
        var targetEntityId = Guid.NewGuid();
        TrackEntity(sourceEntityId);
        TrackEntity(targetEntityId);
        var creatorEntityId = Guid.NewGuid();
        var clonerEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        TrackEntity(clonerEntityId);

        var sourceHandler = TestDataContext().For<UIStudioTemplateHandler>(sourceEntityId);
        var targetHandler = TestDataContext().For<UIStudioTemplateHandler>(targetEntityId);

        var sourceTemplate = new UIStudioTemplate
        {
            OwnerEntityId = sourceEntityId,
            TemplateName = "Source Template",
            TemplateType = "page",
            Category = "dashboard",
            Description = "Original template to clone",
            TemplateData = new Dictionary<string, object>
            {
                { "layout", new { type = "bento", columns = 12 } },
                { "components", new[] { new { type = "MetricCard" } } }
            },
            IsPublic = true,
            CreatedByEntityId = creatorEntityId,
            Tags = new[] { "dashboard", "metrics" }
        };

        await sourceHandler.CreateTemplate(sourceTemplate);

        // Act
        var result = await targetHandler.CloneTemplate(
            sourceEntityId, 
            "Cloned Template", 
            clonerEntityId,
            "Personal copy of dashboard template");

        // Assert
        result.ShouldNotBeNull();
        result.TemplateName.ShouldBe("Cloned Template");
        result.TemplateType.ShouldBe("page");
        result.Category.ShouldBe("dashboard");
        result.Description.ShouldBe("Personal copy of dashboard template");
        result.TemplateData.ShouldNotBeNull();
        result.TemplateData.ContainsKey("layout").ShouldBeTrue();
        result.TemplateData.ContainsKey("components").ShouldBeTrue();
        result.CreatedByEntityId.ShouldBe(clonerEntityId);
        result.OwnerEntityId.ShouldBe(targetEntityId);
        result.OwnerEntityId.ShouldNotBe(sourceEntityId);
    }

    /// <summary>
    /// Tests publishing and unpublishing templates.
    /// </summary>
    [Fact]
    public async Task TogglePublicStatus_ChangesVisibility()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioTemplateHandler>(entityId);
        var template = new UIStudioTemplate
        {
            OwnerEntityId = entityId,
            TemplateName = "Toggle Template",
            TemplateType = "page",
            Category = "test",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = false,
            CreatedByEntityId = creatorEntityId
        };

        await handler.CreateTemplate(template);

        // Act - Publish
        var published = await handler.PublishTemplate();

        // Assert
        published.ShouldNotBeNull();
        published.IsPublic.ShouldBeTrue();
        published.PublishedAt.ShouldNotBeNull();

        // Act - Unpublish
        var unpublished = await handler.UnpublishTemplate();

        // Assert
        unpublished.ShouldNotBeNull();
        unpublished.IsPublic.ShouldBeFalse();
    }

    /// <summary>
    /// Tests getting templates by category.
    /// </summary>
    [Fact]
    public async Task GetTemplatesByCategory_WithMixedCategories_ReturnsCorrectTemplates()
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

        var handler1 = TestDataContext().For<UIStudioTemplateHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioTemplateHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioTemplateHandler>(entityId3);

        // Create dashboard templates
        var dashboardTemplate1 = new UIStudioTemplate
        {
            OwnerEntityId = entityId1,
            TemplateName = "Dashboard 1",
            TemplateType = "page",
            Category = "dashboard",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = true,
            CreatedByEntityId = creatorEntityId
        };

        var dashboardTemplate2 = new UIStudioTemplate
        {
            OwnerEntityId = entityId2,
            TemplateName = "Dashboard 2",
            TemplateType = "page",
            Category = "dashboard",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = true,
            CreatedByEntityId = creatorEntityId
        };

        // Create form template
        var formTemplate = new UIStudioTemplate
        {
            OwnerEntityId = entityId3,
            TemplateName = "Contact Form",
            TemplateType = "page",
            Category = "form",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = true,
            CreatedByEntityId = creatorEntityId
        };

        await handler1.CreateTemplate(dashboardTemplate1);
        await handler2.CreateTemplate(dashboardTemplate2);
        await handler3.CreateTemplate(formTemplate);

        // Act
        var dashboardTemplates = await handler1.GetTemplatesByCategory("dashboard");
        var formTemplates = await handler1.GetTemplatesByCategory("form");

        // Assert
        dashboardTemplates.ShouldNotBeNull();
        dashboardTemplates.Count.ShouldBe(2);
        dashboardTemplates.All(t => t.Category == "dashboard").ShouldBeTrue();

        formTemplates.ShouldNotBeNull();
        formTemplates.Count.ShouldBe(1);
        formTemplates[0].Category.ShouldBe("form");
        formTemplates[0].TemplateName.ShouldBe("Contact Form");
    }

    /// <summary>
    /// Tests getting public templates only.
    /// </summary>
    [Fact]
    public async Task GetPublicTemplates_WithMixedVisibility_ReturnsOnlyPublic()
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

        var handler1 = TestDataContext().For<UIStudioTemplateHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioTemplateHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioTemplateHandler>(entityId3);

        // Create public template
        var publicTemplate = new UIStudioTemplate
        {
            OwnerEntityId = entityId1,
            TemplateName = "Public Template",
            TemplateType = "page",
            Category = "public",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = true,
            CreatedByEntityId = creatorEntityId
        };

        // Create private templates
        var privateTemplate1 = new UIStudioTemplate
        {
            OwnerEntityId = entityId2,
            TemplateName = "Private Template 1",
            TemplateType = "page",
            Category = "private",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = false,
            CreatedByEntityId = creatorEntityId
        };

        var privateTemplate2 = new UIStudioTemplate
        {
            OwnerEntityId = entityId3,
            TemplateName = "Private Template 2",
            TemplateType = "page",
            Category = "private",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = false,
            CreatedByEntityId = creatorEntityId
        };

        await handler1.CreateTemplate(publicTemplate);
        await handler2.CreateTemplate(privateTemplate1);
        await handler3.CreateTemplate(privateTemplate2);

        // Act
        var result = await handler1.GetPublicTemplates();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].IsPublic.ShouldBeTrue();
        result[0].TemplateName.ShouldBe("Public Template");
    }

    /// <summary>
    /// Tests searching templates by query.
    /// </summary>
    [Fact]
    public async Task SearchTemplates_WithMatchingQuery_ReturnsMatchingTemplates()
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

        var handler1 = TestDataContext().For<UIStudioTemplateHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioTemplateHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioTemplateHandler>(entityId3);

        // Create templates with searchable content
        var dashboardTemplate = new UIStudioTemplate
        {
            OwnerEntityId = entityId1,
            TemplateName = "Executive Dashboard",
            TemplateType = "page",
            Category = "dashboard",
            Description = "Executive level dashboard with KPIs and metrics",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = true,
            CreatedByEntityId = creatorEntityId,
            Tags = new[] { "dashboard", "executive", "kpi" }
        };

        var analyticsTemplate = new UIStudioTemplate
        {
            OwnerEntityId = entityId2,
            TemplateName = "Analytics Dashboard",
            TemplateType = "page",
            Category = "analytics",
            Description = "Data analytics and visualization dashboard",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = true,
            CreatedByEntityId = creatorEntityId,
            Tags = new[] { "analytics", "data", "visualization" }
        };

        var formTemplate = new UIStudioTemplate
        {
            OwnerEntityId = entityId3,
            TemplateName = "Contact Form",
            TemplateType = "page",
            Category = "form",
            Description = "Simple contact form template",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = true,
            CreatedByEntityId = creatorEntityId,
            Tags = new[] { "form", "contact" }
        };

        await handler1.CreateTemplate(dashboardTemplate);
        await handler2.CreateTemplate(analyticsTemplate);
        await handler3.CreateTemplate(formTemplate);

        // Act
        var dashboardResults = await handler1.SearchTemplates("dashboard");
        var analyticsResults = await handler1.SearchTemplates("analytics");
        var formResults = await handler1.SearchTemplates("contact");

        // Assert
        dashboardResults.ShouldNotBeNull();
        dashboardResults.Count.ShouldBe(2); // Both dashboard templates should match
        dashboardResults.Any(t => t.TemplateName.Contains("Executive")).ShouldBeTrue();
        dashboardResults.Any(t => t.TemplateName.Contains("Analytics")).ShouldBeTrue();

        analyticsResults.ShouldNotBeNull();
        analyticsResults.Count.ShouldBe(1);
        analyticsResults[0].TemplateName.ShouldBe("Analytics Dashboard");

        formResults.ShouldNotBeNull();
        formResults.Count.ShouldBe(1);
        formResults[0].TemplateName.ShouldBe("Contact Form");
    }

    /// <summary>
    /// Tests getting template statistics.
    /// </summary>
    [Fact]
    public async Task GetTemplateStats_WithMultipleTemplates_ReturnsStatistics()
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

        var handler1 = TestDataContext().For<UIStudioTemplateHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioTemplateHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioTemplateHandler>(entityId3);

        // Create templates with different properties
        var publicTemplate = new UIStudioTemplate
        {
            OwnerEntityId = entityId1,
            TemplateName = "Public Dashboard",
            TemplateType = "page",
            Category = "dashboard",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = true,
            CreatedByEntityId = creatorEntityId,
            UsageCount = 15
        };

        var privateTemplate = new UIStudioTemplate
        {
            OwnerEntityId = entityId2,
            TemplateName = "Private Form",
            TemplateType = "component",
            Category = "form",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = false,
            CreatedByEntityId = creatorEntityId,
            UsageCount = 5
        };

        var popularTemplate = new UIStudioTemplate
        {
            OwnerEntityId = entityId3,
            TemplateName = "Popular Layout",
            TemplateType = "layout",
            Category = "layout",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = true,
            CreatedByEntityId = creatorEntityId,
            UsageCount = 50
        };

        await handler1.CreateTemplate(publicTemplate);
        await handler2.CreateTemplate(privateTemplate);
        await handler3.CreateTemplate(popularTemplate);

        // Act
        var result = await handler1.GetTemplateStats();

        // Assert
        result.ShouldNotBeNull();
        result.ContainsKey("totalTemplates").ShouldBeTrue();
        result.ContainsKey("publicTemplates").ShouldBeTrue();
        result.ContainsKey("privateTemplates").ShouldBeTrue();
        result.ContainsKey("categoryCounts").ShouldBeTrue();
        result.ContainsKey("typeCounts").ShouldBeTrue();
        result.ContainsKey("totalUsage").ShouldBeTrue();
        result.ContainsKey("averageUsage").ShouldBeTrue();
        
        result["totalTemplates"].ShouldBe(3);
        result["publicTemplates"].ShouldBe(2);
        result["privateTemplates"].ShouldBe(1);
        result["totalUsage"].ShouldBe(70); // 15 + 5 + 50
    }

    /// <summary>
    /// Tests validating template data.
    /// </summary>
    [Fact]
    public async Task ValidateTemplateData_WithValidData_ReturnsTrue()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var handler = TestDataContext().For<UIStudioTemplateHandler>(entityId);
        
        var validTemplateData = new Dictionary<string, object>
        {
            { "page", new { name = "Valid Page", type = "dashboard" } },
            { "layout", new { type = "bento", columns = 12, rows = 8 } },
            { "components", new[]
                {
                    new { type = "MetricCard", id = "metric-1", position = new { x = 0, y = 0 } }
                }
            }
        };

        // Act
        var result = await handler.ValidateTemplateData(validTemplateData);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests validating invalid template data.
    /// </summary>
    [Fact]
    public async Task ValidateTemplateData_WithInvalidData_ReturnsFalse()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var handler = TestDataContext().For<UIStudioTemplateHandler>(entityId);
        
        var invalidTemplateData = new Dictionary<string, object>
        {
            { "page", null! },              // Missing required page data
            { "layout", new { } },          // Empty layout
            { "components", new[] { 
                new { /* missing required fields */ }
            }}
        };

        // Act
        var result = await handler.ValidateTemplateData(invalidTemplateData);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// Tests incrementing template usage count.
    /// </summary>
    [Fact]
    public async Task IncrementUsageCount_UpdatesCountCorrectly()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioTemplateHandler>(entityId);
        var template = new UIStudioTemplate
        {
            OwnerEntityId = entityId,
            TemplateName = "Usage Test Template",
            TemplateType = "page",
            Category = "test",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = true,
            CreatedByEntityId = creatorEntityId,
            UsageCount = 10
        };

        await handler.CreateTemplate(template);

        // Act
        var result = await handler.IncrementUsageCount();

        // Assert
        result.ShouldNotBeNull();
        result.UsageCount.ShouldBe(11);
        result.LastUsedAt.ShouldNotBeNull();
        result.LastUsedAt.ShouldBeGreaterThan(template.LastUsedAt ?? DateTime.MinValue);
    }

    /// <summary>
    /// Tests getting template usage history.
    /// </summary>
    [Fact]
    public async Task GetUsageHistory_WithUsageData_ReturnsHistory()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioTemplateHandler>(entityId);
        var template = new UIStudioTemplate
        {
            OwnerEntityId = entityId,
            TemplateName = "History Test Template",
            TemplateType = "page",
            Category = "test",
            TemplateData = new Dictionary<string, object> { { "test", "data" } },
            IsPublic = true,
            CreatedByEntityId = creatorEntityId,
            UsageCount = 25,
            UsageHistory = new Dictionary<string, object>
            {
                { "2024-01", 5 },
                { "2024-02", 8 },
                { "2024-03", 12 }
            }
        };

        await handler.CreateTemplate(template);

        // Act
        var result = await handler.GetUsageHistory();

        // Assert
        result.ShouldNotBeNull();
        result.ContainsKey("totalUsage").ShouldBeTrue();
        result.ContainsKey("monthlyUsage").ShouldBeTrue();
        result.ContainsKey("trends").ShouldBeTrue();
        result["totalUsage"].ShouldBe(25);
    }
}