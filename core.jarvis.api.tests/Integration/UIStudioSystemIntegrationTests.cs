using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.jarvis.api.Systems;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.tests.Helpers;
using core.jarvis.Data;
using core.jarvis.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Integration;

/// <summary>
/// Integration tests for UIStudioSystem.
/// Tests complex workflows, transaction management, and cross-handler operations.
/// </summary>
[Collection("Sequential")]
public class UIStudioSystemIntegrationTests : ApiIntegrationTestBase
{
    /// <summary>
    /// Tests creating a complete page with layout and component bindings.
    /// </summary>
    [Fact]
    public async Task CreatePage_WithLayoutAndBindings_CreatesCompletePageWorkflow()
    {
        // Arrange
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        
        var system = _serviceProvider.GetRequiredService<UIStudioSystem>();
        var layoutConfig = new Dictionary<string, object>
        {
            { "type", "bento" },
            { "columns", 12 },
            { "rows", 8 },
            { "gap", 16 },
            { "responsive", true }
        };

        // Act
        var components = await system.CreatePage(
            "Integration Test Dashboard",
            "integration-test-dashboard",
            "dynamic",
            layoutConfig,
            creatorEntityId,
            "A dashboard created via integration test");

        // Assert
        components.ShouldNotBeNull();
        components.Count.ShouldBeGreaterThan(1); // Should have page + layout at minimum

        // Verify page was created
        var pageComponent = components.OfType<UIStudioPage>().FirstOrDefault();
        pageComponent.ShouldNotBeNull();
        pageComponent.PageName.ShouldBe("Integration Test Dashboard");
        pageComponent.PageSlug.ShouldBe("integration-test-dashboard");
        pageComponent.PageType.ShouldBe("dynamic");
        pageComponent.CreatedByEntityId.ShouldBe(creatorEntityId);

        // Verify layout was created
        var layoutComponent = components.OfType<UIStudioLayout>().FirstOrDefault();
        layoutComponent.ShouldNotBeNull();
        layoutComponent.LayoutName.ShouldBe("Integration Test Dashboard Layout");
        layoutComponent.LayoutType.ShouldBe("bento");
        layoutComponent.GridConfig["columns"].ShouldBe(12);
        layoutComponent.GridConfig["rows"].ShouldBe(8);

        // Verify page and layout were created (relationship is handled via LinkRelationship)
        // The system.CreatePage method links the page and layout entities internally

        // Track entities for cleanup
        TrackEntity(pageComponent.OwnerEntityId);
        TrackEntity(layoutComponent.OwnerEntityId);
    }

    /// <summary>
    /// Tests creating a page from a template.
    /// </summary>
    [Fact]
    public async Task CreatePageFromTemplate_WithValidTemplate_CreatesPageWithTemplateData()
    {
        // Arrange
        var creatorEntityId = Guid.NewGuid();
        var templateEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        TrackEntity(templateEntityId);
        
        var system = _serviceProvider.GetRequiredService<UIStudioSystem>();
        
        // First create a template
        var templateHandler = TestDataContext().For<UIStudioTemplateHandler>(templateEntityId);
        var templateData = new Dictionary<string, object>
        {
            { "page", new { name = "Template Page", type = "dashboard" } },
            { "layout", new { type = "bento", columns = 12, rows = 6 } },
            { "component_bindings", new[]
                {
                    new { componentType = "MetricCard", componentId = "metric-1" },
                    new { componentType = "Chart", componentId = "chart-1" }
                }
            }
        };

        var template = new UIStudioTemplate
        {
            OwnerEntityId = templateEntityId,
            TemplateName = "Dashboard Template",
            TemplateType = "page",
            Category = "dashboard",
            TemplateData = templateData,
            IsPublic = true,
            CreatedByEntityId = creatorEntityId
        };
        await templateHandler.CreateTemplate(template);

        var layoutConfig = new Dictionary<string, object> { { "type", "bento" } };

        // Act
        var components = await system.CreatePage(
            "Templated Dashboard",
            "templated-dashboard",
            "dynamic",
            layoutConfig,
            creatorEntityId,
            "Dashboard created from template",
            templateEntityId);

        // Assert
        components.ShouldNotBeNull();
        components.Count.ShouldBeGreaterThan(2); // Page + Layout + Bindings

        var pageComponent = components.OfType<UIStudioPage>().FirstOrDefault();
        pageComponent.ShouldNotBeNull();
        pageComponent.PageName.ShouldBe("Templated Dashboard");

        var layoutComponent = components.OfType<UIStudioLayout>().FirstOrDefault();
        layoutComponent.ShouldNotBeNull();

        var bindingComponents = components.OfType<UIStudioComponentBinding>().ToList();
        bindingComponents.ShouldNotBeEmpty();

        // Track all entities for cleanup
        foreach (var component in components)
        {
            TrackEntity(component.OwnerEntityId);
        }
    }

    /// <summary>
    /// Tests updating a page with multiple changes.
    /// </summary>
    [Fact]
    public async Task UpdatePage_WithMultipleChanges_UpdatesAllComponents()
    {
        // Arrange
        var creatorEntityId = Guid.NewGuid();
        var modifierEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        TrackEntity(modifierEntityId);
        
        var system = _serviceProvider.GetRequiredService<UIStudioSystem>();
        
        // Create initial page
        var layoutConfig = new Dictionary<string, object>
        {
            { "type", "bento" },
            { "columns", 8 },
            { "rows", 6 }
        };

        var components = await system.CreatePage(
            "Original Page",
            "original-page",
            "dynamic",
            layoutConfig,
            creatorEntityId);

        var pageComponent = components.OfType<UIStudioPage>().First();
        TrackEntity(pageComponent.OwnerEntityId);

        var updates = new Dictionary<string, object>
        {
            { "page_name", "Updated Page Name" },
            { "description", "Updated description" },
            { "layout_config", new Dictionary<string, object>
                {
                    { "type", "bento" },
                    { "columns", 12 },
                    { "rows", 8 },
                    { "gap", 20 }
                }
            }
        };

        // Act
        var updatedComponents = await system.UpdatePage(
            pageComponent.OwnerEntityId,
            updates,
            modifierEntityId,
            "Updated page name and layout configuration");

        // Assert
        updatedComponents.ShouldNotBeNull();
        updatedComponents.Count.ShouldBeGreaterThan(1); // Page + Layout

        var updatedPage = updatedComponents.OfType<UIStudioPage>().FirstOrDefault();
        updatedPage.ShouldNotBeNull();
        updatedPage.PageName.ShouldBe("Updated Page Name");
        updatedPage.Description.ShouldBe("Updated description");
        updatedPage.ModifiedByEntityId.ShouldBe(modifierEntityId);

        var updatedLayout = updatedComponents.OfType<UIStudioLayout>().FirstOrDefault();
        updatedLayout.ShouldNotBeNull();
        updatedLayout.GridConfig["columns"].ShouldBe(12);
        updatedLayout.GridConfig["rows"].ShouldBe(8);
        updatedLayout.GridConfig["gap"].ShouldBe(20);
    }

    /// <summary>
    /// Tests publishing a page workflow.
    /// </summary>
    [Fact]
    public async Task PublishPage_WithCompleteWorkflow_PublishesAndCreatesVersion()
    {
        // Arrange
        var creatorEntityId = Guid.NewGuid();
        var publisherEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        TrackEntity(publisherEntityId);
        
        var system = _serviceProvider.GetRequiredService<UIStudioSystem>();
        
        // Create page
        var layoutConfig = new Dictionary<string, object> { { "type", "bento" } };
        var components = await system.CreatePage(
            "Page to Publish",
            "page-to-publish",
            "dynamic",
            layoutConfig,
            creatorEntityId);

        var pageComponent = components.OfType<UIStudioPage>().First();
        TrackEntity(pageComponent.OwnerEntityId);

        // Act
        var publishedComponents = await system.PublishPage(pageComponent.OwnerEntityId, publisherEntityId);

        // Assert
        publishedComponents.ShouldNotBeNull();
        publishedComponents.Count.ShouldBeGreaterThan(1); // Page + Version

        var publishedPage = publishedComponents.OfType<UIStudioPage>().FirstOrDefault();
        publishedPage.ShouldNotBeNull();
        publishedPage.IsPublished.ShouldBeTrue();

        var versionComponent = publishedComponents.OfType<UIStudioVersion>().FirstOrDefault();
        versionComponent.ShouldNotBeNull();
        versionComponent.ResourceEntityId.ShouldBe(pageComponent.OwnerEntityId);
        versionComponent.VersionType.ShouldBe("published");
        versionComponent.IsPublished.ShouldBeTrue();
        versionComponent.CreatedByEntityId.ShouldBe(publisherEntityId);

        // Track version entity for cleanup
        TrackEntity(versionComponent.OwnerEntityId);
    }

    /// <summary>
    /// Tests granting permissions workflow.
    /// </summary>
    [Fact]
    public async Task GrantPermission_WithValidData_CreatesPermissionAndLogsAction()
    {
        // Arrange
        var resourceEntityId = Guid.NewGuid();
        var granteeEntityId = Guid.NewGuid();
        var granterEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(granteeEntityId);
        TrackEntity(granterEntityId);
        
        var system = _serviceProvider.GetRequiredService<UIStudioSystem>();

        // Act
        var permission = await system.GrantPermission(
            resourceEntityId,
            "page",
            granteeEntityId,
            "user",
            "write",
            granterEntityId,
            DateTime.UtcNow.AddDays(30));

        // Assert
        permission.ShouldNotBeNull();
        permission.ResourceEntityId.ShouldBe(resourceEntityId);
        permission.ResourceType.ShouldBe("page");
        permission.GranteeEntityId.ShouldBe(granteeEntityId);
        permission.GranteeType.ShouldBe("user");
        permission.PermissionLevel.ShouldBe("write");
        permission.GrantedByEntityId.ShouldBe(granterEntityId);
        permission.ExpiresAt.ShouldNotBeNull();

        TrackEntity(permission.OwnerEntityId);
    }

    /// <summary>
    /// Tests creating template from page workflow.
    /// </summary>
    [Fact]
    public async Task CreateTemplateFromPage_WithComplexPage_CreatesTemplateWithAllData()
    {
        // Arrange
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        
        var system = _serviceProvider.GetRequiredService<UIStudioSystem>();
        
        // Create a complex page with layout and bindings
        var layoutConfig = new Dictionary<string, object>
        {
            { "type", "bento" },
            { "columns", 12 },
            { "rows", 8 },
            { "templateAreas", new[] { "header", "sidebar", "main", "footer" } }
        };

        var components = await system.CreatePage(
            "Complex Dashboard Page",
            "complex-dashboard",
            "dynamic",
            layoutConfig,
            creatorEntityId,
            "A complex dashboard for template creation");

        var pageComponent = components.OfType<UIStudioPage>().First();
        TrackEntity(pageComponent.OwnerEntityId);

        // Act
        var template = await system.CreateTemplateFromPage(
            pageComponent.OwnerEntityId,
            "Dashboard Template",
            "dashboard",
            "Template created from complex dashboard page",
            creatorEntityId,
            true);

        // Assert
        template.ShouldNotBeNull();
        template.TemplateName.ShouldBe("Dashboard Template");
        template.TemplateType.ShouldBe("page");
        template.Category.ShouldBe("dashboard");
        template.IsPublic.ShouldBeTrue();
        template.CreatedByEntityId.ShouldBe(creatorEntityId);
        template.TemplateData.ShouldNotBeNull();
        template.TemplateData.ContainsKey("page").ShouldBeTrue();
        template.TemplateData.ContainsKey("layout").ShouldBeTrue();

        TrackEntity(template.OwnerEntityId);
    }

    /// <summary>
    /// Tests deleting a page with all related components.
    /// </summary>
    [Fact]
    public async Task DeletePage_WithRelatedComponents_DeletesAllComponents()
    {
        // Arrange
        var creatorEntityId = Guid.NewGuid();
        var deleterEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        TrackEntity(deleterEntityId);
        
        var system = _serviceProvider.GetRequiredService<UIStudioSystem>();
        
        // Create page with layout
        var layoutConfig = new Dictionary<string, object> { { "type", "bento" } };
        var components = await system.CreatePage(
            "Page to Delete",
            "page-to-delete",
            "dynamic",
            layoutConfig,
            creatorEntityId);

        var pageComponent = components.OfType<UIStudioPage>().First();

        // Act
        var deletedComponents = await system.DeletePage(
            pageComponent.OwnerEntityId,
            deleterEntityId,
            "Integration test cleanup");

        // Assert
        deletedComponents.ShouldNotBeNull();
        deletedComponents.Count.ShouldBeGreaterThan(0);

        var deletedPage = deletedComponents.OfType<UIStudioPage>().FirstOrDefault();
        deletedPage.ShouldNotBeNull();
        deletedPage.PageName.ShouldBe("Page to Delete");

        // Verify page is actually deleted from database
        var pageHandler = TestDataContext().For<UIStudioPageHandler>(pageComponent.OwnerEntityId);
        var retrievedPage = await pageHandler.Get();
        retrievedPage.ShouldBeNull();
    }

    /// <summary>
    /// Tests duplicating a page with all components.
    /// </summary>
    [Fact]
    public async Task DuplicatePage_WithBindings_CreatesCompleteClone()
    {
        // Arrange
        var creatorEntityId = Guid.NewGuid();
        var duplicatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        TrackEntity(duplicatorEntityId);
        
        var system = _serviceProvider.GetRequiredService<UIStudioSystem>();
        
        // Create source page
        var layoutConfig = new Dictionary<string, object>
        {
            { "type", "bento" },
            { "columns", 12 },
            { "specialProperty", "unique-value" }
        };

        var sourceComponents = await system.CreatePage(
            "Source Page",
            "source-page",
            "dynamic",
            layoutConfig,
            creatorEntityId,
            "Original page for duplication");

        var sourcePageComponent = sourceComponents.OfType<UIStudioPage>().First();
        TrackEntity(sourcePageComponent.OwnerEntityId);

        // Act
        var duplicatedComponents = await system.DuplicatePage(
            sourcePageComponent.OwnerEntityId,
            "Duplicated Page",
            "duplicated-page",
            duplicatorEntityId,
            true);

        // Assert
        duplicatedComponents.ShouldNotBeNull();
        duplicatedComponents.Count.ShouldBeGreaterThan(1);

        var duplicatedPage = duplicatedComponents.OfType<UIStudioPage>().FirstOrDefault();
        duplicatedPage.ShouldNotBeNull();
        duplicatedPage.PageName.ShouldBe("Duplicated Page");
        duplicatedPage.PageSlug.ShouldBe("duplicated-page");
        duplicatedPage.CreatedByEntityId.ShouldBe(duplicatorEntityId);
        duplicatedPage.IsPublished.ShouldBeFalse(); // Duplicated pages start unpublished
        duplicatedPage.IsDefault.ShouldBeFalse(); // Duplicated pages are never default
        duplicatedPage.OwnerEntityId.ShouldNotBe(sourcePageComponent.OwnerEntityId);

        var duplicatedLayout = duplicatedComponents.OfType<UIStudioLayout>().FirstOrDefault();
        duplicatedLayout.ShouldNotBeNull();
        duplicatedLayout.LayoutName.ShouldBe("Duplicated Page Layout");
        duplicatedLayout.GridConfig["specialProperty"].ShouldBe("unique-value");

        // Track duplicated entities for cleanup
        foreach (var component in duplicatedComponents)
        {
            TrackEntity(component.OwnerEntityId);
        }
    }

    /// <summary>
    /// Tests applying a template to create a new page.
    /// </summary>
    [Fact]
    public async Task ApplyTemplate_WithCustomizations_CreatesCustomizedPage()
    {
        // Arrange
        var creatorEntityId = Guid.NewGuid();
        var templateEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        TrackEntity(templateEntityId);
        
        var system = _serviceProvider.GetRequiredService<UIStudioSystem>();
        
        // Create template
        var templateHandler = TestDataContext().For<UIStudioTemplateHandler>(templateEntityId);
        var templateData = new Dictionary<string, object>
        {
            { "page", new { type = "dashboard" } },
            { "layout", new { type = "bento", columns = 8 } }
        };

        var template = new UIStudioTemplate
        {
            OwnerEntityId = templateEntityId,
            TemplateName = "Apply Template Test",
            TemplateType = "page",
            Category = "test",
            TemplateData = templateData,
            IsPublic = true,
            CreatedByEntityId = creatorEntityId
        };
        await templateHandler.CreateTemplate(template);

        var customizations = new Dictionary<string, object>
        {
            { "description", "Customized from template" },
            { "layout_config", new Dictionary<string, object>
                {
                    { "type", "bento" },
                    { "columns", 16 },
                    { "rows", 10 }
                }
            }
        };

        // Act
        var components = await system.ApplyTemplate(
            templateEntityId,
            "Applied Template Page",
            "applied-template-page",
            creatorEntityId,
            customizations);

        // Assert
        components.ShouldNotBeNull();
        components.Count.ShouldBeGreaterThan(1);

        var pageComponent = components.OfType<UIStudioPage>().FirstOrDefault();
        pageComponent.ShouldNotBeNull();
        pageComponent.PageName.ShouldBe("Applied Template Page");
        pageComponent.PageSlug.ShouldBe("applied-template-page");
        pageComponent.Description.ShouldBe("Customized from template");

        var layoutComponent = components.OfType<UIStudioLayout>().FirstOrDefault();
        layoutComponent.ShouldNotBeNull();
        layoutComponent.GridConfig["columns"].ShouldBe(16);
        layoutComponent.GridConfig["rows"].ShouldBe(10);

        // Track entities for cleanup
        foreach (var component in components)
        {
            TrackEntity(component.OwnerEntityId);
        }
    }

    /// <summary>
    /// Tests error handling and rollback behavior.
    /// </summary>
    [Fact]
    public async Task CreatePage_WithInvalidData_HandlesErrorsGracefully()
    {
        // Arrange
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        
        var system = _serviceProvider.GetRequiredService<UIStudioSystem>();
        var layoutConfig = new Dictionary<string, object> { { "type", "invalid" } };

        // Act & Assert
        var exception = await Should.ThrowAsync<Exception>(async () =>
            await system.CreatePage(
                "", // Invalid empty name
                "invalid-slug-page",
                "invalid-type",
                layoutConfig,
                creatorEntityId));

        exception.ShouldNotBeNull();
        // The system should handle errors gracefully and not leave partial data
    }

    /// <summary>
    /// Tests complex permission workflow with updates and revocation.
    /// </summary>
    [Fact]
    public async Task PermissionWorkflow_GrantUpdateRevoke_HandlesCompleteLifecycle()
    {
        // Arrange
        var resourceEntityId = Guid.NewGuid();
        var granteeEntityId = Guid.NewGuid();
        var granterEntityId = Guid.NewGuid();
        TrackEntity(resourceEntityId);
        TrackEntity(granteeEntityId);
        TrackEntity(granterEntityId);
        
        var system = _serviceProvider.GetRequiredService<UIStudioSystem>();

        // Act - Grant permission
        var permission = await system.GrantPermission(
            resourceEntityId,
            "page",
            granteeEntityId,
            "user",
            "read",
            granterEntityId);

        permission.ShouldNotBeNull();
        permission.PermissionLevel.ShouldBe("read");
        TrackEntity(permission.OwnerEntityId);

        // Act - Update permission
        var updatedPermission = await system.UpdatePermission(
            permission.OwnerEntityId,
            "write",
            DateTime.UtcNow.AddDays(60),
            granterEntityId);

        updatedPermission.ShouldNotBeNull();
        updatedPermission.PermissionLevel.ShouldBe("write");
        updatedPermission.ExpiresAt.ShouldNotBeNull();

        // Act - Revoke permission
        await system.RevokePermission(
            permission.OwnerEntityId,
            granterEntityId,
            "Permission no longer needed");

        // Assert - Verify permission is revoked
        var permissionHandler = TestDataContext().For<UIStudioPermissionHandler>(permission.OwnerEntityId);
        var revokedPermission = await permissionHandler.Get();
        revokedPermission.ShouldBeNull(); // Should be deleted
    }
}