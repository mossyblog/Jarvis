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
/// Unit tests for UIStudioPageHandler.
/// Tests CRUD operations, business logic, and validation rules for page management.
/// </summary>
public class UIStudioPageHandlerTests : ApiIntegrationTestBase
{
    /// <summary>
    /// Tests creating a new page with valid data.
    /// </summary>
    [Fact]
    public async Task CreatePage_WithValidData_CreatesPageSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        
        var handler = TestDataContext().For<UIStudioPageHandler>(entityId);
        var page = new UIStudioPage
        {
            OwnerEntityId = entityId,
            PageName = "Test Page",
            PageSlug = "test-page",
            PageType = "dynamic",
            Description = "A test page",
            CreatedByEntityId = creatorEntityId,
            IsPublished = false
        };

        // Act
        var result = await handler.CreatePage(page);

        // Assert
        result.ShouldNotBeNull();
        result.PageName.ShouldBe("Test Page");
        result.PageSlug.ShouldBe("test-page");
        result.PageType.ShouldBe("dynamic");
        result.CreatedByEntityId.ShouldBe(creatorEntityId);
        result.IsPublished.ShouldBeFalse();
        result.IsDefault.ShouldBeFalse();

        // Verify persistence
        var retrievedPage = await handler.Get();
        retrievedPage.ShouldNotBeNull();
        retrievedPage.PageName.ShouldBe("Test Page");
    }

    /// <summary>
    /// Tests that creating a page with duplicate slug throws exception.
    /// </summary>
    [Fact]
    public async Task CreatePage_WithDuplicateSlug_ThrowsException()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler1 = TestDataContext().For<UIStudioPageHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioPageHandler>(entityId2);

        var page1 = new UIStudioPage
        {
            OwnerEntityId = entityId1,
            PageName = "Page 1",
            PageSlug = "duplicate-slug",
            CreatedByEntityId = creatorEntityId
        };

        var page2 = new UIStudioPage
        {
            OwnerEntityId = entityId2,
            PageName = "Page 2",
            PageSlug = "duplicate-slug",
            CreatedByEntityId = creatorEntityId
        };

        // Act & Assert
        await handler1.CreatePage(page1);
        
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await handler2.CreatePage(page2));
        
        exception.Message.ShouldContain("already exists");
    }

    /// <summary>
    /// Tests that only one page can be set as default.
    /// </summary>
    [Fact]
    public async Task CreatePage_AsDefault_ClearsOtherDefaultPages()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler1 = TestDataContext().For<UIStudioPageHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioPageHandler>(entityId2);

        var page1 = new UIStudioPage
        {
            OwnerEntityId = entityId1,
            PageName = "First Default",
            PageSlug = "first-default",
            CreatedByEntityId = creatorEntityId,
            IsDefault = true
        };

        var page2 = new UIStudioPage
        {
            OwnerEntityId = entityId2,
            PageName = "Second Default",
            PageSlug = "second-default",
            CreatedByEntityId = creatorEntityId,
            IsDefault = true
        };

        // Act
        await handler1.CreatePage(page1);
        await handler2.CreatePage(page2);

        // Assert
        var firstPage = await handler1.Get();
        var secondPage = await handler2.Get();

        firstPage!.IsDefault.ShouldBeFalse(); // Should be cleared
        secondPage!.IsDefault.ShouldBeTrue();  // Should remain true
    }

    /// <summary>
    /// Tests updating an existing page.
    /// </summary>
    [Fact]
    public async Task UpdatePage_WithValidChanges_UpdatesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        var modifierEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        TrackEntity(modifierEntityId);

        var handler = TestDataContext().For<UIStudioPageHandler>(entityId);
        var originalPage = new UIStudioPage
        {
            OwnerEntityId = entityId,
            PageName = "Original Name",
            PageSlug = "original-slug",
            CreatedByEntityId = creatorEntityId
        };

        await handler.CreatePage(originalPage);

        var updatedPage = originalPage with
        {
            PageName = "Updated Name",
            Description = "Updated description",
            ModifiedByEntityId = modifierEntityId
        };

        // Act
        var result = await handler.UpdatePage(updatedPage);

        // Assert
        result.ShouldNotBeNull();
        result.PageName.ShouldBe("Updated Name");
        result.Description.ShouldBe("Updated description");
        result.ModifiedByEntityId.ShouldBe(modifierEntityId);
        result.PageSlug.ShouldBe("original-slug"); // Should remain unchanged
    }

    /// <summary>
    /// Tests updating a page that doesn't exist throws exception.
    /// </summary>
    [Fact]
    public async Task UpdatePage_WhenPageNotFound_ThrowsException()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var handler = TestDataContext().For<UIStudioPageHandler>(entityId);

        var page = new UIStudioPage
        {
            OwnerEntityId = entityId,
            PageName = "Non-existent Page"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await handler.UpdatePage(page));
        
        exception.Message.ShouldContain("not found");
    }

    /// <summary>
    /// Tests publishing a page.
    /// </summary>
    [Fact]
    public async Task PublishPage_WhenPageExists_PublishesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioPageHandler>(entityId);
        var page = new UIStudioPage
        {
            OwnerEntityId = entityId,
            PageName = "Test Page",
            PageSlug = "test-page",
            CreatedByEntityId = creatorEntityId,
            IsPublished = false
        };

        await handler.CreatePage(page);

        // Act
        var result = await handler.PublishPage();

        // Assert
        result.ShouldNotBeNull();
        result.IsPublished.ShouldBeTrue();
        result.LastUpdated.ShouldBeGreaterThan(page.LastUpdated);
    }

    /// <summary>
    /// Tests unpublishing a page.
    /// </summary>
    [Fact]
    public async Task UnpublishPage_WhenPageExists_UnpublishesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioPageHandler>(entityId);
        var page = new UIStudioPage
        {
            OwnerEntityId = entityId,
            PageName = "Test Page",
            PageSlug = "test-page",
            CreatedByEntityId = creatorEntityId,
            IsPublished = true
        };

        await handler.CreatePage(page);

        // Act
        var result = await handler.UnpublishPage();

        // Assert
        result.ShouldNotBeNull();
        result.IsPublished.ShouldBeFalse();
    }

    /// <summary>
    /// Tests setting a page as default.
    /// </summary>
    [Fact]
    public async Task SetAsDefault_WhenPageExists_SetsAsDefaultSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioPageHandler>(entityId);
        var page = new UIStudioPage
        {
            OwnerEntityId = entityId,
            PageName = "Test Page",
            PageSlug = "test-page",
            CreatedByEntityId = creatorEntityId,
            IsDefault = false
        };

        await handler.CreatePage(page);

        // Act
        var result = await handler.SetAsDefault();

        // Assert
        result.ShouldNotBeNull();
        result.IsDefault.ShouldBeTrue();
    }

    /// <summary>
    /// Tests finding a page by slug.
    /// </summary>
    [Fact]
    public async Task FindBySlug_WhenPageExists_ReturnsPage()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioPageHandler>(entityId);
        var page = new UIStudioPage
        {
            OwnerEntityId = entityId,
            PageName = "Test Page",
            PageSlug = "unique-test-slug",
            CreatedByEntityId = creatorEntityId
        };

        await handler.CreatePage(page);

        // Act
        var result = await handler.FindBySlug("unique-test-slug");

        // Assert
        result.ShouldNotBeNull();
        result.PageName.ShouldBe("Test Page");
        result.PageSlug.ShouldBe("unique-test-slug");
    }

    /// <summary>
    /// Tests finding a page by non-existent slug returns null.
    /// </summary>
    [Fact]
    public async Task FindBySlug_WhenPageNotExists_ReturnsNull()
    {
        // Arrange
        var handler = TestDataContext().For<UIStudioPageHandler>(Guid.NewGuid());

        // Act
        var result = await handler.FindBySlug("non-existent-slug");

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests getting published pages.
    /// </summary>
    [Fact]
    public async Task GetPublishedPages_WithMixedPages_ReturnsOnlyPublished()
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

        var handler1 = TestDataContext().For<UIStudioPageHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioPageHandler>(entityId2);
        var handler3 = TestDataContext().For<UIStudioPageHandler>(entityId3);

        // Create published page
        var publishedPage = new UIStudioPage
        {
            OwnerEntityId = entityId1,
            PageName = "Published Page",
            PageSlug = "published-page",
            CreatedByEntityId = creatorEntityId,
            IsPublished = true,
            SortOrder = 1
        };

        // Create unpublished page
        var unpublishedPage = new UIStudioPage
        {
            OwnerEntityId = entityId2,
            PageName = "Unpublished Page",
            PageSlug = "unpublished-page",
            CreatedByEntityId = creatorEntityId,
            IsPublished = false,
            SortOrder = 2
        };

        // Create another published page
        var anotherPublishedPage = new UIStudioPage
        {
            OwnerEntityId = entityId3,
            PageName = "Another Published Page",
            PageSlug = "another-published-page",
            CreatedByEntityId = creatorEntityId,
            IsPublished = true,
            SortOrder = 0
        };

        await handler1.CreatePage(publishedPage);
        await handler2.CreatePage(unpublishedPage);
        await handler3.CreatePage(anotherPublishedPage);

        // Act
        var result = await handler1.GetPublishedPages();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.All(p => p.IsPublished).ShouldBeTrue();
        
        // Should be ordered by SortOrder
        result[0].PageName.ShouldBe("Another Published Page"); // SortOrder = 0
        result[1].PageName.ShouldBe("Published Page"); // SortOrder = 1
    }

    /// <summary>
    /// Tests getting the default page.
    /// </summary>
    [Fact]
    public async Task GetDefaultPage_WhenDefaultExists_ReturnsDefaultPage()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler = TestDataContext().For<UIStudioPageHandler>(entityId);
        var page = new UIStudioPage
        {
            OwnerEntityId = entityId,
            PageName = "Default Page",
            PageSlug = "default-page",
            CreatedByEntityId = creatorEntityId,
            IsDefault = true,
            IsPublished = true
        };

        await handler.CreatePage(page);

        // Act
        var result = await handler.GetDefaultPage();

        // Assert
        result.ShouldNotBeNull();
        result.PageName.ShouldBe("Default Page");
        result.IsDefault.ShouldBeTrue();
        result.IsPublished.ShouldBeTrue();
    }

    /// <summary>
    /// Tests searching pages by query string.
    /// </summary>
    [Fact]
    public async Task SearchPages_WithMatchingQuery_ReturnsMatchingPages()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler1 = TestDataContext().For<UIStudioPageHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioPageHandler>(entityId2);

        var searchablePage = new UIStudioPage
        {
            OwnerEntityId = entityId1,
            PageName = "Searchable Page",
            PageSlug = "searchable-page",
            Description = "This page contains search terms",
            CreatedByEntityId = creatorEntityId
        };

        var otherPage = new UIStudioPage
        {
            OwnerEntityId = entityId2,
            PageName = "Other Page",
            PageSlug = "other-page",
            Description = "Different content",
            CreatedByEntityId = creatorEntityId
        };

        await handler1.CreatePage(searchablePage);
        await handler2.CreatePage(otherPage);

        // Act
        var result = await handler1.SearchPages("search");

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].PageName.ShouldBe("Searchable Page");
    }

    /// <summary>
    /// Tests page filtering with various criteria.
    /// </summary>
    [Fact]
    public async Task GetPagesWithFilters_WithVariousCriteria_ReturnsFilteredResults()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        var creatorEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);

        var handler1 = TestDataContext().For<UIStudioPageHandler>(entityId1);
        var handler2 = TestDataContext().For<UIStudioPageHandler>(entityId2);

        var dynamicPage = new UIStudioPage
        {
            OwnerEntityId = entityId1,
            PageName = "Dynamic Page",
            PageSlug = "dynamic-page",
            PageType = "dynamic",
            CreatedByEntityId = creatorEntityId,
            IsPublished = true
        };

        var fixedPage = new UIStudioPage
        {
            OwnerEntityId = entityId2,
            PageName = "Fixed Page",
            PageSlug = "fixed-page",
            PageType = "fixed",
            CreatedByEntityId = creatorEntityId,
            IsPublished = false
        };

        await handler1.CreatePage(dynamicPage);
        await handler2.CreatePage(fixedPage);

        var filters = new Dictionary<string, object>
        {
            { "PageType", "dynamic" },
            { "IsPublished", true },
            { "Limit", 10 },
            { "Offset", 0 },
            { "SortBy", "name" },
            { "SortOrder", "asc" }
        };

        // Act
        var result = await handler1.GetPagesWithFilters(filters);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].PageName.ShouldBe("Dynamic Page");
        result[0].PageType.ShouldBe("dynamic");
        result[0].IsPublished.ShouldBeTrue();
    }

    /// <summary>
    /// Tests restoring a page from snapshot data.
    /// </summary>
    [Fact]
    public async Task RestoreFromSnapshot_WithValidSnapshot_RestoresSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var creatorEntityId = Guid.NewGuid();
        var restorerEntityId = Guid.NewGuid();
        TrackEntity(creatorEntityId);
        TrackEntity(restorerEntityId);

        var handler = TestDataContext().For<UIStudioPageHandler>(entityId);
        var originalPage = new UIStudioPage
        {
            OwnerEntityId = entityId,
            PageName = "Original Page",
            PageSlug = "original-page",
            CreatedByEntityId = creatorEntityId
        };

        await handler.CreatePage(originalPage);

        var snapshotData = originalPage with
        {
            PageName = "Restored Page Name",
            Description = "Restored from snapshot"
        };

        // Act
        var result = await handler.RestoreFromSnapshot(snapshotData, restorerEntityId);

        // Assert
        result.ShouldNotBeNull();
        result.PageName.ShouldBe("Restored Page Name");
        result.Description.ShouldBe("Restored from snapshot");
        result.ModifiedByEntityId.ShouldBe(restorerEntityId);
    }

    /// <summary>
    /// Tests restoring with invalid snapshot data throws exception.
    /// </summary>
    [Fact]
    public async Task RestoreFromSnapshot_WithInvalidSnapshot_ThrowsException()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var handler = TestDataContext().For<UIStudioPageHandler>(entityId);
        var invalidSnapshot = "invalid data";

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await handler.RestoreFromSnapshot(invalidSnapshot, Guid.NewGuid()));
        
        exception.Message.ShouldContain("Invalid snapshot data format");
    }
}

/// <summary>
/// Helper class for page query filters used in testing.
/// </summary>
public class PageQueryFilters
{
    public string? PageType { get; set; }
    public bool? IsPublished { get; set; }
    public Guid? CreatedByEntityId { get; set; }
    public string? Search { get; set; }
    public int Limit { get; set; } = 20;
    public int Offset { get; set; } = 0;
    public string SortBy { get; set; } = "created";
    public string SortOrder { get; set; } = "desc";
}

/// <summary>
/// Helper class for paged results used in testing.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public bool HasMore { get; set; }
}