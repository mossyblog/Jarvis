using core.jarvis.Data;
using core.jarvis.Data.GraphQL;
using core.jarvis.Data.Query;
using core.jarvis.Exceptions;
using core.jarvis.tests.Fixtures.Components;
using core.jarvis.tests.Fixtures.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace core.jarvis.tests.Unit.Data;

/// <summary>
/// Tests for ComponentHandler&lt;TComponent&gt; - the base class for all plugin-provided handlers.
/// 
/// BUSINESS CONTEXT: ComponentHandler is the foundation that all business logic handlers inherit from.
/// It provides common functionality like validation, error handling, component creation, and 
/// database operations. Handlers like InvoiceHandler, PaymentHandler extend this base class.
/// 
/// ARCHITECTURE SIGNIFICANCE: This base class embodies the "handler owns all business logic" principle.
/// It provides database access, validation helpers, and component lifecycle management
/// while keeping the core framework agnostic about specific business components.
/// 
/// TEST STRATEGY: Focus on the common functionality that all handlers will inherit:
/// - Constructor validation and dependency injection
/// - Helper methods like Ensure() and Generate()
/// - Property access patterns
/// Database operations (Get, Update, etc.) are too complex for unit testing without integration.
/// </summary>
public class ComponentHandlerTests
{
    private readonly NullLogger<ConcreteTestHandler> _logger;

    public ComponentHandlerTests()
    {
        _logger = new NullLogger<ConcreteTestHandler>();
    }

    /// <summary>
    /// Validates that ComponentHandler constructor throws ValidationException when entityId parameter is empty.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Validates constructor guard clause for entityId parameter.</para>
    /// <para><strong>PURPOSE:</strong> Ensures all handlers fail fast when initialized with invalid entity identifiers.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Every handler operates on a specific entity - without a valid entity ID,
    /// no database operations or business logic can be performed correctly. All handler methods assume a valid entity context.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Empty GUIDs represent uninitialized or invalid entities. Catching this early
    /// prevents silent failures or incorrect database queries later in the handler lifecycle, ensuring data integrity.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Demonstrates that the Guard pattern is enforced at the 
    /// base class level, ensuring all derived handlers get input validation automatically without duplicate validation code.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> Entity ID validation should be preserved even if validation
    /// implementation details change, as all handlers depend on valid entity context.</para>
    /// </remarks>
    [Fact]
    public void Constructor_WithEmptyEntityId_ShouldThrowValidationException()
    {
        // Arrange
        var dataContext = new FakeDataContext();
        var logger = new FakeLogger<ConcreteTestHandler>();
        var handler = new ConcreteTestHandler(dataContext, logger);

        // Act & Assert
        var exception = Should.Throw<ValidationException>(() => 
            handler.InitializeContext(Guid.Empty));
        
        exception.Errors.ShouldContainKey("entityId");
        exception.Errors["entityId"].ShouldContain("entityId cannot be empty");
    }

    /// <summary>
    /// Validates that ComponentHandler constructor throws ArgumentNullException when logger dependency is null.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Validates constructor guard clause for logging dependency.</para>
    /// <para><strong>PURPOSE:</strong> Ensures handlers cannot be created without logging capability.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Handler operations need traceability for debugging and audit.
    /// Business operations like invoice processing require audit trails and troubleshooting visibility.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Logging is essential for production support and troubleshooting.
    /// Without logging, business operation failures would be difficult to diagnose and resolve.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Structured logging enables observability of business
    /// operations without coupling handlers to specific logging implementations.</para>
    /// </remarks>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var dataContext = new FakeDataContext();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new ConcreteTestHandler(dataContext, null!))
            .ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// Validates that ComponentHandler constructor properly initializes all dependencies with valid parameters.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Validates successful constructor execution with valid parameters.</para>
    /// <para><strong>PURPOSE:</strong> Ensures basic dependency injection and property initialization works correctly.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Normal handler creation scenario during plugin operation.
    /// This represents the standard pathway for creating handlers during business operations.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Confirms the constructor properly initializes all dependencies
    /// that handlers will use for business operations. Validates the happy path of dependency injection.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Demonstrates proper dependency injection pattern
    /// that enables both unit testing and production usage with different implementations.</para>
    /// </remarks>
    [Fact]
    public void Constructor_WithValidParameters_ShouldSetProperties()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var dataContext = new FakeDataContext();
        var logger = new FakeLogger<ConcreteTestHandler>();

        // Act
        var handler = new ConcreteTestHandler(dataContext, logger);
        handler.InitializeContext(entityId);

        // Assert
        handler.TestEntityId.ShouldBe(entityId);
        handler.TestLogger.ShouldBe(logger);
    }

    /// <summary>
    /// Validates that Ensure() helper method does not throw when business rule conditions are satisfied.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Validates the Ensure() helper when business rule conditions are met.</para>
    /// <para><strong>PURPOSE:</strong> Confirms that valid business conditions don't trigger exceptions.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Business rules like "invoice must be SENT before writeoff" need validation.
    /// Valid business states should allow operations to proceed normally without interruption.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Ensure() is the primary method handlers use for business rule validation.
    /// When conditions are valid, business operations should proceed without interruption or false positives.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Demonstrates the Guard pattern at the business logic level,
    /// providing a clean API for expressing business constraints in handler methods without coupling to exception types.</para>
    /// </remarks>
    [Fact]
    public void Ensure_WhenConditionIsTrue_ShouldNotThrow()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var dataContext = new FakeDataContext();
        var logger = new FakeLogger<ConcreteTestHandler>();
        var handler = new ConcreteTestHandler(dataContext, logger);
        handler.InitializeContext(entityId);

        // Act & Assert
        Should.NotThrow(() => handler.Ensure(true, "This should not throw"));
    }

    /// <summary>
    /// Validates that Ensure() helper method throws BusinessRuleException when business rule conditions are violated.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Validates the Ensure() helper when business rule conditions are violated.</para>
    /// <para><strong>PURPOSE:</strong> Confirms that invalid business conditions trigger appropriate domain exceptions.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Business rules must be enforced - "cannot writeoff a paid invoice".
    /// Invalid business states should be caught and prevented before data corruption occurs.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Business rule violations should immediately stop processing and provide
    /// clear error context to the caller for appropriate user feedback and error handling.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> BusinessRuleException is domain-specific and includes the
    /// handler type context, enabling proper error categorization and handling upstream in the application layers.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> If error handling changes, this documents that Ensure() should
    /// throw typed domain exceptions rather than generic validation errors to preserve business context.</para>
    /// </remarks>
    [Fact]
    public void Ensure_WhenConditionIsFalse_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var dataContext = new FakeDataContext();
        var logger = new FakeLogger<ConcreteTestHandler>();
        var handler = new ConcreteTestHandler(dataContext, logger);
        handler.InitializeContext(entityId);

        var errorMessage = "Business rule violated";

        // Act & Assert
        var exception = Should.Throw<BusinessRuleException>(() =>
            handler.Ensure(false, errorMessage));

        exception.Code.ShouldBe("RULE_CONCRETETESTHANDLER");
        exception.Message.ShouldContain(errorMessage);
    }


    // For testing database operations, we'll need to create integration tests
    // with a real Supabase connection or use a different approach.
    // The Get methods involve complex Supabase interaction that's hard to mock.
}

public class FakeDataContext : IDataContext {
    public IComponentHandler For(Type componentType, Guid entityId) => throw new NotImplementedException();
    public THandler For<THandler>(Guid entityId) where THandler : class, IComponentHandler => throw new NotImplementedException();
    public IEntityQuery Query() => throw new NotImplementedException();
    public Task<bool> TryCommit<TComponent>(TComponent component) where TComponent : class, IComponent, new() => Task.FromResult(true);
    public async Task Commit<TComponent>(TComponent component) where TComponent : class, IComponent, new()
    {
        throw new NotImplementedException();
    }

    public Task Remove<TComponent>(Guid entityId) where TComponent : class, IComponent, new() => Task.CompletedTask;
    public Task Insert<TModel>(TModel model) where TModel : class, new() => Task.CompletedTask;
    public ISnapshotQuery Snapshots() => throw new NotImplementedException();
    public IGraphQLQuery GraphQL(string query) => throw new NotImplementedException();
    public Task<Guid?> Parent(Guid entityId) => Task.FromResult<Guid?>(null);
    public Task<List<Guid>> Children(Guid entityId) => Task.FromResult(new List<Guid>());
    public Task<bool> ChildOf(Guid childId, Guid parentId) => Task.FromResult(false);
    public Task LinkRelationship(Guid parentId, Guid childId, string? parentType = null, string? childType = null) => Task.CompletedTask;
    public Task UnlinkRelationship(Guid parentId, Guid childId) => Task.CompletedTask;
    public Task<List<Guid>> Ancestors(Guid entityId) => Task.FromResult(new List<Guid>());
    public Task<List<Guid>> Descendants(Guid entityId) => Task.FromResult(new List<Guid>());
    public Task Emit<TEvent>(TEvent @event) where TEvent : core.jarvis.Events.IEvent => Task.CompletedTask;
    public Task EmitBatch<TEvent>(IEnumerable<TEvent> events) where TEvent : core.jarvis.Events.IEvent => Task.CompletedTask;
    public Entity NewEntity() => new Entity(Guid.NewGuid());
    public Task EnsureEntityTableExists() => Task.CompletedTask;
}

public class FakeLogger<T> : ILogger<T> {
    public IDisposable BeginScope<TState>(TState state) => null!;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}