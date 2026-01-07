using core.jarvis.Data;
using core.jarvis.Data.Query;
using core.jarvis.Events;
using core.jarvis.Events.Emitters;
using core.jarvis.Exceptions;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shouldly;
using Xunit;

namespace core.jarvis.tests.Integration;

public class EventEmissionIntegrationTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify that DataContext can emit events through configured emitter
    /// PURPOSE: Ensure event emission works end-to-end through DataContext
    /// BUSINESS CONTEXT: Support event-driven architectures and integration
    /// WHY IMPORTANT: Validates complete event emission pipeline
    /// ARCHITECTURAL SIGNIFICANCE: Confirms DataContext properly integrates with event system
    /// FUTURE RESILIENCE: Ensures event emission remains functional as system evolves
    /// </summary>
    [Fact]
    public async Task DataContext_Emit_ShouldEmitEventThroughConfiguredEmitter()
    {
        // Arrange
        var dataContext = TestDataContext();
        var eventEmitter = _serviceProvider.GetRequiredService<IEventEmitter>() as InMemoryEventEmitter;

        var testEvent = new OrderCreatedEvent(Guid.NewGuid(), 100m);

        // Act
        await dataContext.Emit(testEvent);

        // Assert
        eventEmitter.ShouldNotBeNull();
        eventEmitter.EmittedEvents.Count.ShouldBe(1);
        eventEmitter.EmittedEvents[0].ShouldBeOfType<OrderCreatedEvent>();
        var emittedEvent = (OrderCreatedEvent)eventEmitter.EmittedEvents[0];
        emittedEvent.OrderId.ShouldBe(testEvent.OrderId);
        emittedEvent.Total.ShouldBe(testEvent.Total);
        emittedEvent.Metadata["EmittedFrom"].ShouldBe("DataContext");
        emittedEvent.Metadata.ContainsKey("EmittedAt").ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify that DataContext can emit multiple events as a batch
    /// PURPOSE: Ensure batch event emission works correctly
    /// BUSINESS CONTEXT: Support high-throughput event scenarios
    /// WHY IMPORTANT: Validates batch operations for performance-critical paths
    /// ARCHITECTURAL SIGNIFICANCE: Confirms batch processing is properly implemented
    /// FUTURE RESILIENCE: Supports efficient bulk event processing
    /// </summary>
    [Fact]
    public async Task DataContext_EmitBatch_ShouldEmitAllEventsThroughConfiguredEmitter()
    {
        // Arrange
        var dataContext = TestDataContext();
        var eventEmitter = _serviceProvider.GetRequiredService<IEventEmitter>() as InMemoryEventEmitter;

        var events = new List<OrderCreatedEvent>
        {
            new OrderCreatedEvent(Guid.NewGuid(), 100m),
            new OrderCreatedEvent(Guid.NewGuid(), 200m),
            new OrderCreatedEvent(Guid.NewGuid(), 300m)
        };

        // Act
        await dataContext.EmitBatch(events);

        // Assert
        eventEmitter.ShouldNotBeNull();
        eventEmitter.EmittedEvents.Count.ShouldBe(3);
        eventEmitter.EmittedEvents.All(e => e is OrderCreatedEvent).ShouldBeTrue();
        eventEmitter.EmittedEvents.All(e => e.Metadata.ContainsKey("EmittedFrom")).ShouldBeTrue();
        eventEmitter.EmittedEvents.All(e => e.Metadata.ContainsKey("EmittedAt")).ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify that event emission failures are properly handled
    /// PURPOSE: Ensure error handling works correctly for event emission
    /// BUSINESS CONTEXT: Support resilient event processing
    /// WHY IMPORTANT: Prevents event failures from crashing the system
    /// ARCHITECTURAL SIGNIFICANCE: Confirms proper error boundaries
    /// FUTURE RESILIENCE: Ensures graceful degradation when event infrastructure fails
    /// </summary>
    [Fact]
    public async Task DataContext_Emit_WhenEmitterFails_ShouldThrowEventEmissionException()
    {
        // Arrange - Create a new service provider with failing emitter
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IComponentQueryHandlerRegistry, ComponentQueryHandlerRegistry>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IEntityQuery, EntityQuery>();
        services.AddScoped<core.jarvis.Data.Schema.ITableManager, core.jarvis.Data.Schema.PostgreSqlTableManager>();
        services.AddSingleton<IPgClient>(_serviceProvider.GetRequiredService<IPgClient>());

        // Register the refactored DataContext dependencies
        services.AddScoped<IDataContextCore, DataContextCore>();
        services.AddScoped<IAuditManager, AuditManager>();
        services.AddScoped<IRelationshipManager, RelationshipManager>();
        services.AddScoped<ITransactionManager, TransactionManager>();

        // Register the failing emitter to test error handling
        services.AddSingleton<IEventEmitter>(new FailingEventEmitter());
        services.AddScoped<IDataContext, DataContext>();

        var provider = services.BuildServiceProvider();
        var dataContext = provider.GetRequiredService<IDataContext>();
        var testEvent = new OrderCreatedEvent(Guid.NewGuid(), 100m);

        // Act & Assert
        var exception = await Should.ThrowAsync<EventEmissionException>(
            async () => await dataContext.Emit(testEvent));

        exception.Message.ShouldContain("Failed to emit event");
        exception.InnerException.ShouldNotBeNull();
    }


    // Test fixtures
    private record OrderCreatedEvent(Guid OrderId, decimal Total) : DomainEvent;

    private class FailingEventEmitter : IEventEmitter
    {
        public Task Emit<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            throw new InvalidOperationException("Emitter failure simulated");
        }

        public Task EmitBatch<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            throw new InvalidOperationException("Batch emitter failure simulated");
        }
    }
}