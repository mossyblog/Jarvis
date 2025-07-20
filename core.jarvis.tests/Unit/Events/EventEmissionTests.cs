using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.jarvis.Events;
using core.jarvis.Events.Emitters;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace core.jarvis.tests.Unit.Events;

public class EventEmissionTests
{
    private readonly ILogger<InMemoryEventEmitter> _inMemoryLogger;

    public EventEmissionTests()
    {
        // Use real null loggers instead of mocks
        _inMemoryLogger = new NullLogger<InMemoryEventEmitter>();
    }


    /// <summary>
    /// INTENT: Verify that InMemoryEventEmitter stores emitted events
    /// PURPOSE: Ensure events are retained in memory for testing and debugging
    /// BUSINESS CONTEXT: Support testing scenarios where event verification is needed
    /// WHY IMPORTANT: Enables unit testing of event-driven components
    /// ARCHITECTURAL SIGNIFICANCE: Provides testable event infrastructure
    /// FUTURE RESILIENCE: Allows verification of event emission in tests
    /// </summary>
    [Fact]
    public async Task InMemoryEventEmitter_Emit_ShouldStoreEvent()
    {
        // Arrange
        var emitter = new InMemoryEventEmitter(_inMemoryLogger);
        var testEvent = new TestEvent { Message = "Test" };

        // Act
        await emitter.Emit(testEvent);

        // Assert
        emitter.EmittedEvents.Count.ShouldBe(1);
        var storedEvent = emitter.EmittedEvents.First();
        storedEvent.ShouldBeOfType<TestEvent>();
        ((TestEvent)storedEvent).Message.ShouldBe("Test");
    }

    /// <summary>
    /// INTENT: Verify that InMemoryEventEmitter stores batch events
    /// PURPOSE: Ensure batch operations correctly store all events
    /// BUSINESS CONTEXT: Support batch event testing scenarios
    /// WHY IMPORTANT: Validates batch processing capabilities
    /// ARCHITECTURAL SIGNIFICANCE: Ensures consistent batch handling
    /// FUTURE RESILIENCE: Supports testing of batch event scenarios
    /// </summary>
    [Fact]
    public async Task InMemoryEventEmitter_EmitBatch_ShouldStoreAllEvents()
    {
        // Arrange
        var emitter = new InMemoryEventEmitter(_inMemoryLogger);
        var events = new List<TestEvent>
        {
            new TestEvent { Message = "Test1" },
            new TestEvent { Message = "Test2" }
        };

        // Act
        await emitter.EmitBatch(events);

        // Assert
        emitter.EmittedEvents.Count.ShouldBe(2);
        emitter.EmittedEvents.OfType<TestEvent>().Select(e => e.Message)
            .ShouldBe(new[] { "Test1", "Test2" });
    }

    /// <summary>
    /// INTENT: Verify that Clear removes all stored events
    /// PURPOSE: Ensure test isolation by clearing events between tests
    /// BUSINESS CONTEXT: Support clean test state management
    /// WHY IMPORTANT: Prevents test interference
    /// ARCHITECTURAL SIGNIFICANCE: Enables proper test isolation
    /// FUTURE RESILIENCE: Maintains test reliability
    /// </summary>
    [Fact]
    public async Task InMemoryEventEmitter_Clear_ShouldRemoveAllEvents()
    {
        // Arrange
        var emitter = new InMemoryEventEmitter(_inMemoryLogger);
        await emitter.Emit(new TestEvent { Message = "Test1" });
        await emitter.Emit(new TestEvent { Message = "Test2" });

        // Act
        emitter.Clear();

        // Assert
        emitter.EmittedEvents.Count.ShouldBe(0);
    }

    /// <summary>
    /// INTENT: Verify that InMemoryEventEmitter maintains event order
    /// PURPOSE: Ensure events are stored in emission order
    /// BUSINESS CONTEXT: Support scenarios requiring event ordering
    /// WHY IMPORTANT: Validates event sequence integrity
    /// ARCHITECTURAL SIGNIFICANCE: Ensures predictable event ordering
    /// FUTURE RESILIENCE: Supports time-based event analysis
    /// </summary>
    [Fact]
    public async Task InMemoryEventEmitter_ShouldMaintainEventOrder()
    {
        // Arrange
        var emitter = new InMemoryEventEmitter(_inMemoryLogger);
        var events = new List<TestEvent>();
        for (int i = 0; i < 5; i++)
        {
            events.Add(new TestEvent { Message = $"Event{i}" });
        }

        // Act
        foreach (var evt in events)
        {
            await emitter.Emit(evt);
        }

        // Assert
        emitter.EmittedEvents.Count.ShouldBe(5);
        for (int i = 0; i < 5; i++)
        {
            ((TestEvent)emitter.EmittedEvents[i]).Message.ShouldBe($"Event{i}");
        }
    }

    /// <summary>
    /// INTENT: Verify event filtering functionality
    /// PURPOSE: Ensure events can be retrieved by type
    /// BUSINESS CONTEXT: Support event analysis and debugging
    /// WHY IMPORTANT: Enables targeted event inspection
    /// ARCHITECTURAL SIGNIFICANCE: Provides event query capabilities
    /// FUTURE RESILIENCE: Supports complex event analysis scenarios
    /// </summary>
    [Fact]
    public async Task InMemoryEventEmitter_GetEvents_ShouldFilterByType()
    {
        // Arrange
        var emitter = new InMemoryEventEmitter(_inMemoryLogger);
        await emitter.Emit(new TestEvent { Message = "Test" });
        await emitter.Emit(new AnotherTestEvent { Value = 42 });

        // Act
        var testEvents = emitter.EmittedEvents.OfType<TestEvent>().ToList();
        var anotherEvents = emitter.EmittedEvents.OfType<AnotherTestEvent>().ToList();

        // Assert
        testEvents.Count.ShouldBe(1);
        testEvents[0].Message.ShouldBe("Test");
        anotherEvents.Count.ShouldBe(1);
        anotherEvents[0].Value.ShouldBe(42);
    }
}

// Test event classes
public class TestEvent : IEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Type => nameof(TestEvent);
    public Dictionary<string, object> Metadata { get; } = new();
    
    public string Message { get; set; } = string.Empty;
}

public class AnotherTestEvent : IEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Type => nameof(AnotherTestEvent);
    public Dictionary<string, object> Metadata { get; } = new();
    
    public int Value { get; set; }
}

// Real logger implementation for tests
public class NullLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NullScope();
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    
    private class NullScope : IDisposable
    {
        public void Dispose() { }
    }
}