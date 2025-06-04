using core.jarvis.Events;
using core.jarvis.Events.Emitters;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace core.jarvis.tests.Unit.Events;

public class EventEmissionTests
{
    private readonly Mock<ILogger<NoOpEventEmitter>> _noOpLoggerMock;
    private readonly Mock<ILogger<InMemoryEventEmitter>> _inMemoryLoggerMock;

    public EventEmissionTests()
    {
        _noOpLoggerMock = new Mock<ILogger<NoOpEventEmitter>>();
        _inMemoryLoggerMock = new Mock<ILogger<InMemoryEventEmitter>>();
    }

    /// <summary>
    /// INTENT: Verify that NoOpEventEmitter successfully processes events without performing any action
    /// PURPOSE: Ensure the no-op emitter can be used when event emission is disabled
    /// BUSINESS CONTEXT: Support scenarios where event emission should be turned off
    /// WHY IMPORTANT: Allows system to run without external event dependencies
    /// ARCHITECTURAL SIGNIFICANCE: Provides a safe default when no event infrastructure is configured
    /// FUTURE RESILIENCE: Ensures system can operate in minimal configuration mode
    /// </summary>
    [Fact]
    public async Task NoOpEventEmitter_Emit_ShouldCompleteWithoutAction()
    {
        // Arrange
        var emitter = new NoOpEventEmitter(_noOpLoggerMock.Object);
        var testEvent = new TestEvent { Message = "Test" };

        // Act
        await emitter.Emit(testEvent);

        // Assert
        _noOpLoggerMock.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Event emission disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// INTENT: Verify that NoOpEventEmitter handles batch emissions without action
    /// PURPOSE: Ensure batch operations are supported in no-op mode
    /// BUSINESS CONTEXT: Support batch event scenarios even when emission is disabled
    /// WHY IMPORTANT: Maintains API consistency across all emitter implementations
    /// ARCHITECTURAL SIGNIFICANCE: Ensures uniform behavior regardless of configuration
    /// FUTURE RESILIENCE: Allows code to use batch operations without checking emitter type
    /// </summary>
    [Fact]
    public async Task NoOpEventEmitter_EmitBatch_ShouldCompleteWithoutAction()
    {
        // Arrange
        var emitter = new NoOpEventEmitter(_noOpLoggerMock.Object);
        var events = new List<TestEvent>
        {
            new TestEvent { Message = "Test1" },
            new TestEvent { Message = "Test2" }
        };

        // Act
        await emitter.EmitBatch(events);

        // Assert
        _noOpLoggerMock.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Event emission disabled") && v.ToString().Contains("2")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// INTENT: Verify that InMemoryEventEmitter correctly stores emitted events
    /// PURPOSE: Enable testing of event emission without external dependencies
    /// BUSINESS CONTEXT: Support unit testing of components that emit events
    /// WHY IMPORTANT: Allows verification of event emission in tests
    /// ARCHITECTURAL SIGNIFICANCE: Provides testability for event-driven components
    /// FUTURE RESILIENCE: Ensures event emission can be tested in isolation
    /// </summary>
    [Fact]
    public async Task InMemoryEventEmitter_Emit_ShouldStoreEvent()
    {
        // Arrange
        var emitter = new InMemoryEventEmitter(_inMemoryLoggerMock.Object);
        var testEvent = new TestEvent { Message = "Test" };

        // Act
        await emitter.Emit(testEvent);

        // Assert
        emitter.EmittedEvents.Count.ShouldBe(1);
        emitter.EmittedEvents[0].ShouldBe(testEvent);
    }

    /// <summary>
    /// INTENT: Verify that InMemoryEventEmitter correctly stores multiple events
    /// PURPOSE: Ensure batch emission works correctly in memory
    /// BUSINESS CONTEXT: Support testing of batch event scenarios
    /// WHY IMPORTANT: Validates batch operations work as expected
    /// ARCHITECTURAL SIGNIFICANCE: Ensures batch operations are properly implemented
    /// FUTURE RESILIENCE: Supports testing of high-throughput event scenarios
    /// </summary>
    [Fact]
    public async Task InMemoryEventEmitter_EmitBatch_ShouldStoreAllEvents()
    {
        // Arrange
        var emitter = new InMemoryEventEmitter(_inMemoryLoggerMock.Object);
        var events = new List<TestEvent>
        {
            new TestEvent { Message = "Test1" },
            new TestEvent { Message = "Test2" },
            new TestEvent { Message = "Test3" }
        };

        // Act
        await emitter.EmitBatch(events);

        // Assert
        emitter.EmittedEvents.Count.ShouldBe(3);
        emitter.EmittedEvents.ShouldContain(e => ((TestEvent)e).Message == "Test1");
        emitter.EmittedEvents.ShouldContain(e => ((TestEvent)e).Message == "Test2");
        emitter.EmittedEvents.ShouldContain(e => ((TestEvent)e).Message == "Test3");
    }

    /// <summary>
    /// INTENT: Verify that InMemoryEventEmitter.Clear removes all stored events
    /// PURPOSE: Allow resetting of event storage between tests
    /// BUSINESS CONTEXT: Support test isolation and cleanup
    /// WHY IMPORTANT: Prevents test interference in shared instances
    /// ARCHITECTURAL SIGNIFICANCE: Ensures testability best practices
    /// FUTURE RESILIENCE: Supports complex test scenarios with state reset
    /// </summary>
    [Fact]
    public async Task InMemoryEventEmitter_Clear_ShouldRemoveAllEvents()
    {
        // Arrange
        var emitter = new InMemoryEventEmitter(_inMemoryLoggerMock.Object);
        var events = new List<TestEvent>
        {
            new TestEvent { Message = "Test1" },
            new TestEvent { Message = "Test2" }
        };
        await emitter.EmitBatch(events);

        // Act
        emitter.Clear();

        // Assert
        emitter.EmittedEvents.Count.ShouldBe(0);
    }

    /// <summary>
    /// INTENT: Verify that DomainEvent correctly initializes with default values
    /// PURPOSE: Ensure domain events have proper metadata
    /// BUSINESS CONTEXT: Support consistent event structure across the system
    /// WHY IMPORTANT: Ensures all events have required metadata
    /// ARCHITECTURAL SIGNIFICANCE: Establishes event contract standards
    /// FUTURE RESILIENCE: Supports event processing and auditing requirements
    /// </summary>
    [Fact]
    public void DomainEvent_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var domainEvent = new TestDomainEvent("TestData");

        // Assert
        domainEvent.Id.ShouldNotBe(Guid.Empty);
        domainEvent.OccurredAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
        domainEvent.Type.ShouldBe("TestDomainEvent");
        domainEvent.Metadata.ShouldNotBeNull();
        domainEvent.Data.ShouldBe("TestData");
    }

    // Test fixtures
    private record TestEvent : IEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public string Type => "TestEvent";
        public Dictionary<string, object> Metadata { get; } = new();
        public string Message { get; init; } = string.Empty;
    }

    private record TestDomainEvent(string Data) : DomainEvent;
}