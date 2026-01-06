# Testing Guide

Jarvis testing patterns - from first test to complex business scenarios.

## The No Mocks Policy

Jarvis enforces a strict NO MOCKS policy.

**Mocks test implementations, not behavior.** When you mock a dependency, you test whether your code calls the mock correctly - not whether it actually works.

**Mocks create false confidence.** Tests pass because mocks return what you told them to. The real system might behave differently.

**Mocks hide integration issues.** The most costly bugs occur at boundaries between components. Mocks eliminate those boundaries in tests.

Instead, Jarvis uses:
- Real PostgreSQL database (Docker-provisioned)
- Real DataContext with actual persistence
- Real handlers with actual business logic

## IntegrationTestBase Setup

All integration tests inherit from `IntegrationTestBase`:

```csharp
using core.jarvis.tests.Helpers;
using Shouldly;

public class OrderHandlerTests : IntegrationTestBase
{
    [Fact]
    public async Task CanCreateOrder()
    {
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);

        var order = await TestDataContext().For<OrderHandler>(entityId)
            .CreateOrder("Customer-123", 100.00m);

        order.CustomerId.ShouldBe("Customer-123");
        order.Status.ShouldBe("PENDING");
    }
}
```

`IntegrationTestBase` provides:
- `TestDataContext()` - Configured DataContext with real PostgreSQL
- `TrackEntity(Guid)` - Registers entities for automatic cleanup
- `Logger()` - Test-scoped logger for debugging

## Writing Your First Test

Follow Arrange-Act-Assert:

```csharp
[Fact]
public async Task BlogHandler_CanCreateBlogAndGeneratePost()
{
    // Arrange
    var entityId = Guid.NewGuid();
    TrackEntity(entityId);

    // Act
    var blog = await TestDataContext().For<BlogComponentHandler>(entityId)
        .CreateBlog("My Blog", "A test blog");

    var post = await TestDataContext().For<BlogHandler>(entityId)
        .GeneratePost(new BlogPostGenerationRequest { Topic = "Testing" });

    // Assert
    blog.ShouldNotBeNull();
    blog.Name.ShouldBe("My Blog");
    post.Title.ShouldContain("Testing");
    post.Status.ShouldBe("draft");
}
```

## Testing Validation Rules

Test that invalid input throws `ValidationException`:

```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
public void Guard_AgainstEmpty_ThrowsForInvalidStrings(string? value)
{
    var exception = Should.Throw<ValidationException>(() =>
        Guard.AgainstEmpty(value, "customerName"));

    exception.Errors.ShouldContainKey("customerName");
}

[Fact]
public void Guard_AgainstOutOfRange_ThrowsForInvalidValues()
{
    var exception = Should.Throw<ValidationException>(() =>
        Guard.AgainstOutOfRange(150, 0, 100, "percentage"));

    exception.Errors["percentage"].ShouldContain("percentage must be between 0 and 100");
}
```

Test that valid input passes:

```csharp
[Theory]
[InlineData(0, 0, 100)]
[InlineData(50, 0, 100)]
[InlineData(100, 0, 100)]
public void Guard_AgainstOutOfRange_AllowsValidValues(int value, int min, int max)
{
    Should.NotThrow(() => Guard.AgainstOutOfRange(value, min, max, "value"));
}
```

## Testing Business Logic

Test happy path scenarios:

```csharp
[Fact]
public async Task BlogHandler_CanPublishPost()
{
    var entityId = Guid.NewGuid();
    TrackEntity(entityId);

    await TestDataContext().For<BlogComponentHandler>(entityId)
        .CreateBlog("Test Blog");

    var post = await TestDataContext().For<BlogHandler>(entityId)
        .GeneratePost(new BlogPostGenerationRequest { Topic = "Test" });

    await TestDataContext().For<BlogHandler>(entityId).PublishPost(post.Id);

    // Reload from database to verify persistence
    var allPosts = await TestDataContext().For<BlogHandler>(entityId).GetAllPosts();
    var published = allPosts.First(p => p.Id == post.Id);

    published.IsPublished.ShouldBeTrue();
    published.Status.ShouldBe("published");
}
```

Test business rule violations:

```csharp
[Fact]
public async Task BlogHandler_ThrowsWhenPublishingNonexistentPost()
{
    var entityId = Guid.NewGuid();
    TrackEntity(entityId);

    await TestDataContext().For<BlogComponentHandler>(entityId)
        .CreateBlog("Test Blog");

    var ex = await Should.ThrowAsync<EntityNotFoundException>(async () =>
    {
        await TestDataContext().For<BlogHandler>(entityId)
            .PublishPost(Guid.NewGuid());
    });

    ex.ShouldNotBeNull();
}

[Fact]
public async Task OrderHandler_ThrowsWhenConfirmingPaidOrder()
{
    var entityId = Guid.NewGuid();
    TrackEntity(entityId);

    var handler = TestDataContext().For<OrderHandler>(entityId);
    await handler.CreateOrder("customer", 100m);
    await handler.MarkAsPaid();

    var ex = await Should.ThrowAsync<BusinessRuleException>(
        () => handler.ConfirmOrder());

    ex.Code.ShouldBe("ORDER_INVALID_STATE");
}
```

## Test Utilities

### TrackEntity for Cleanup

Always track entities created during tests:

```csharp
var entityId = Guid.NewGuid();
TrackEntity(entityId);  // Automatically cleaned up after test
```

### TestDataContext for Real Operations

`TestDataContext()` returns a fully configured `IDataContext`:

```csharp
// Get a handler for an entity
var handler = TestDataContext().For<OrderHandler>(entityId);

// Query entities
var entities = await TestDataContext().Query()
    .WithAll<OrderComponent>(o => o.Status == "PENDING")
    .ToList();
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific project
dotnet test core.jarvis.tests

# Run tests matching filter
dotnet test --filter "FullyQualifiedName~BlogHandler"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Summary

- NO MOCKS: Test real behavior with real dependencies
- Inherit from `IntegrationTestBase` for all integration tests
- Use `TrackEntity()` for cleanup, `TestDataContext()` for operations
- Test happy paths, validation errors, and business rule violations

**Next:** [06-database.md](06-database.md) - Database and data access
