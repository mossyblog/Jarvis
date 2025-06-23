using core.jarvis.Data;
using core.jarvis.Exceptions;
using core.jarvis.tests.Fixtures.Components;
using core.jarvis.tests.Fixtures.Handlers;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace core.jarvis.tests.Integration.HandlerIntegrationTests;

/// <summary>
/// Integration tests for handler access through IDataContext.
/// Tests the complete flow from TestDataContext to handlers and back.
/// </summary>
public class DataContextHandlerTests : IntegrationTestBase
{
    [Fact]
    public void DataContext_For_WithRegisteredHandler_ShouldReturnHandler()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);

        // Act
        var handler = TestDataContext().For<TestHandler>(entityId);

        // Assert
        handler.ShouldNotBeNull();
        handler.ShouldBeOfType<TestHandler>();
    }

    private class UnregisteredTestHandler : IComponentHandler
    {
        public Guid EntityId { get; private set; }
        
        public void InitializeContext(Guid entityId)
        {
            EntityId = entityId;
        }
        
        public Task<IComponent> Get()
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void DataContext_For_WithUnregisteredHandler_ShouldThrowComponentNotFoundException()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);

        // Act & Assert
        Should.Throw<ComponentNotFoundException>(() => 
            TestDataContext().For<UnregisteredTestHandler>(entityId));
    }

    [Fact]
    public async Task DataContext_For_HandlerOperations_ShouldWorkEndToEnd()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var testComponent = new TestComponent 
        { 
            OwnerEntityId = entityId,
            Name = "TestDataContext Test Component",
            Status = "INACTIVE"
        };
        var success = await TestDataContext().TryCommit(testComponent);
        success.ShouldBeTrue();
        // Act
        var handler = TestDataContext().For<TestHandler>(entityId);
        await handler.Activate();
        // Assert
        var component = await handler.Get();
        component.Status.ShouldBe("ACTIVE");
        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }

    [Fact]
    public void DataContext_For_MultipleCallsSameEntity_ShouldReturnDifferentInstances()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);

        // Act
        var handler1 = TestDataContext().For<TestHandler>(entityId);
        var handler2 = TestDataContext().For<TestHandler>(entityId);

        // Assert
        handler1.ShouldNotBeNull();
        handler2.ShouldNotBeNull();
        handler1.ShouldNotBeSameAs(handler2);
    }

    [Fact]
    public async Task DataContext_ErrorHandling_ShouldPropagateHandlerExceptions()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        // Intentionally not creating the component to trigger EntityNotFoundException
        var handler = TestDataContext().For<TestHandler>(entityId);

        // Act & Assert
        var ex = await Should.ThrowAsync<EntityNotFoundException>(() => handler.Get());
        ex.EntityId.ShouldBe(entityId);
    }

    [Fact]
    public async Task DataContext_ConcurrentHandlerAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var testComponent = new TestComponent 
        { 
            OwnerEntityId = entityId,
            Name = "Concurrent Test",
            Status = "INACTIVE"
        };
        var success = await TestDataContext().TryCommit(testComponent);
        success.ShouldBeTrue();
        // Act - Multiple concurrent accesses to the same entity using separate service scopes
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            // Create a new service scope for each concurrent operation
            using var scope = _serviceProvider.CreateScope();
            var dataContext = scope.ServiceProvider.GetRequiredService<IDataContext>();
            var handler = dataContext.For<TestHandler>(entityId);
            var component = await handler.Get();
            return component.Name;
        });
        var results = await Task.WhenAll(tasks);
        // Assert - All should succeed and return the same data
        results.Length.ShouldBe(5);
        results.All(name => name == "Concurrent Test").ShouldBeTrue();
        // Cleanup
        await TestDataContext().Remove<TestComponent>(entityId);
    }
}