using core.jarvis.data.Exceptions;
using core.jarvis.Exceptions;
using core.jarvis.tests.Examples.Blog;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Integration.HandlerIntegrationTests;

/// <summary>
/// Integration tests for the BlogHandler business API.
/// Verifies end-to-end blog creation, post generation, publishing, and error handling using the real DataContext and Supabase backend.
/// Ensures handler-based orchestration and business contract compliance for the blog domain.
/// </summary>
public class BlogHandlerIntegrationTests : IntegrationTestBase
{

    /// <summary>
    /// INTENT: Verify that a blog can be created and a post generated via the BlogHandler API.
    /// PURPOSE: Ensure the core business workflow for blog and post creation is functional.
    /// BUSINESS CONTEXT: Supports the scenario of a user starting a new blog and generating content.
    /// WHY IMPORTANT: Validates the main business value proposition of the blog module.
    /// ARCHITECTURAL SIGNIFICANCE: Enforces handler-based orchestration and DataContext contract.
    /// FUTURE RESILIENCE: Protects against regressions in blog creation and post generation logic.
    /// </summary>
    [Fact]
    public async Task BlogHandler_CanCreateBlogAndGeneratePost()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        // Act
        var blog = await TestDataContext().For<BlogComponentHandler>(entityId)
            .CreateBlog("Integration Test Blog", "Blog for integration testing");
        // Ensure the blog is committed and visible
        await TestDataContext().For<BlogComponentHandler>(entityId).Get();
        var post = await TestDataContext().For<BlogHandler>(entityId)
            .GeneratePost(new BlogPostGenerationRequest { Topic = "Integration Testing" });
        
        // Assert
        blog.ShouldNotBeNull();
        blog.Name.ShouldBe("Integration Test Blog");
        post.ShouldNotBeNull();
        post.Title.ShouldContain("Integration Testing");
        post.Content.ShouldNotBeNullOrWhiteSpace();
        post.Status.ShouldBe("draft");
    }

    /// <summary>
    /// INTENT: Verify that a generated blog post can be published via the BlogHandler API.
    /// PURPOSE: Ensure the business workflow for post publication is functional.
    /// BUSINESS CONTEXT: Supports the scenario of a user publishing a draft post.
    /// WHY IMPORTANT: Validates the transition from draft to published state.
    /// ARCHITECTURAL SIGNIFICANCE: Enforces handler-based state transitions and DataContext contract.
    /// FUTURE RESILIENCE: Protects against regressions in post publishing logic.
    /// </summary>
    [Fact]
    public async Task BlogHandler_CanPublishPost()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        await TestDataContext().For<BlogComponentHandler>(entityId)
            .CreateBlog("Publish Test Blog");
        // Ensure the blog is committed and visible
        await TestDataContext().For<BlogComponentHandler>(entityId).Get();
        var post = await TestDataContext().For<BlogHandler>(entityId)
            .GeneratePost(new BlogPostGenerationRequest { Topic = "Publish Test" });
        
        // Act
        var published = await TestDataContext().For<BlogHandler>(entityId).PublishPost(post.Id);
        // Reload the post from the database to ensure we have the persisted state
        var allPosts = await TestDataContext().For<BlogHandler>(entityId).GetAllPosts();
        var reloaded = allPosts.FirstOrDefault(p => p.Id == post.Id);
        reloaded.ShouldNotBeNull();
        reloaded.IsPublished.ShouldBeTrue();
        reloaded.Status.ShouldBe("published");
    }

    /// <summary>
    /// INTENT: Verify that publishing a non-existent post throws EntityNotFoundException.
    /// PURPOSE: Ensure error handling is robust for invalid operations.
    /// BUSINESS CONTEXT: Supports the scenario of a user attempting to publish a missing post.
    /// WHY IMPORTANT: Validates business error contracts and exception propagation.
    /// ARCHITECTURAL SIGNIFICANCE: Enforces handler-based error handling and DataContext contract.
    /// FUTURE RESILIENCE: Protects against silent failures and ensures correct error signaling.
    /// </summary>
    [Fact]
    public async Task BlogHandler_ErrorOnPublishNonexistentPost()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        await TestDataContext().For<BlogComponentHandler>(entityId)
            .CreateBlog("Error Test Blog");
        // Ensure the blog is committed and visible
        await TestDataContext().For<BlogComponentHandler>(entityId).Get();
            
        // Act & Assert
        var ex = await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await TestDataContext().For<BlogHandler>(entityId)
                .PublishPost(Guid.NewGuid());
        });
        ex.ShouldNotBeNull();
    }

    /// <summary>
    /// INTENT: Verify that multiple posts can be generated in batch via the BlogHandler API.
    /// PURPOSE: Ensure the business workflow for batch post generation is functional.
    /// BUSINESS CONTEXT: Supports the scenario of a user generating several posts at once.
    /// WHY IMPORTANT: Validates the scalability and correctness of the batch generation logic.
    /// ARCHITECTURAL SIGNIFICANCE: Enforces handler-based orchestration and DataContext contract for batch operations.
    /// FUTURE RESILIENCE: Protects against regressions in multi-post generation workflows.
    /// </summary>
    [Fact]
    public async Task BlogHandler_CanGenerateMultiplePosts()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        await TestDataContext().For<BlogComponentHandler>(entityId)
            .CreateBlog("Batch Test Blog");
        // Ensure the blog is committed and visible
        await TestDataContext().For<BlogComponentHandler>(entityId).Get();
        var requests = new List<BlogPostGenerationRequest>
        {
            new() { Topic = "First Post" },
            new() { Topic = "Second Post" },
            new() { Topic = "Third Post" }
        };
        // Act
        var posts = await TestDataContext().For<BlogHandler>(entityId)
            .GeneratePosts(requests);
        // Assert
        posts.Count.ShouldBe(3);
        posts.All(p => !string.IsNullOrWhiteSpace(p.Title)).ShouldBeTrue();
    }
} 