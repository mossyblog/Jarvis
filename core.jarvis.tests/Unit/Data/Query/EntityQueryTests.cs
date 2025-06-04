using System.Linq.Expressions;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using core.jarvis.tests.Fixtures.Components;
using core.jarvis.tests.Fixtures.Fakes;
using Shouldly;

namespace core.jarvis.tests.Unit.Data.Query;

/// <summary>
/// Tests for EntityQuery - the type-safe, reflection-free query system.
/// 
/// BUSINESS CONTEXT: EntityQuery enables cross-component queries with WithAll/WithAny/WithNone
/// semantics for ECS-style entity filtering without runtime reflection.
/// 
/// ARCHITECTURE SIGNIFICANCE: This implementation uses handler-based querying to maintain
/// type safety while supporting plugin architectures without coupling to specific components.
/// </summary>
public class EntityQueryTests
{
    /// <summary>
    /// Validates that WithAll creates an intersection (AND) of entity IDs.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Verifies WithAll implements AND logic for entity filtering.</para>
    /// <para><strong>PURPOSE:</strong> Ensures entities must have ALL specified components to match.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Find entities with both Invoice AND Payment components.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Core ECS pattern for finding entities with specific component combinations.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Demonstrates type-safe querying without reflection.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> WithAll semantics must be preserved for ECS compatibility.</para>
    /// </remarks>
    [Fact]
    public async Task WithAll_ShouldIntersectEntityIds()
    {
        // Arrange
        var registry = new FakeComponentQueryHandlerRegistry();
        var query = new EntityQuery(registry);
        
        var handler1 = new FakeComponentQueryHandler(typeof(TestComponent));
        handler1.SetEntityIds(new[] { Guid.Parse("00000000-0000-0000-0000-000000000001"), 
                                       Guid.Parse("00000000-0000-0000-0000-000000000002"),
                                       Guid.Parse("00000000-0000-0000-0000-000000000003") });
        registry.AddHandler(typeof(TestComponent), handler1);
        
        var handler2 = new FakeComponentQueryHandler(typeof(VelocityComponent));
        handler2.SetEntityIds(new[] { Guid.Parse("00000000-0000-0000-0000-000000000002"), 
                                       Guid.Parse("00000000-0000-0000-0000-000000000003"),
                                       Guid.Parse("00000000-0000-0000-0000-000000000004") });
        registry.AddHandler(typeof(VelocityComponent), handler2);
        
        // Act
        var result = await query
            .WithAll<TestComponent>(t => true)
            .WithAll<VelocityComponent>(v => true)
            .ToEntityIds();
        
        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(Guid.Parse("00000000-0000-0000-0000-000000000002"));
        result.ShouldContain(Guid.Parse("00000000-0000-0000-0000-000000000003"));
    }

    /// <summary>
    /// Validates that WithAny creates a union (OR) of entity IDs.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Verifies WithAny implements OR logic for entity filtering.</para>
    /// <para><strong>PURPOSE:</strong> Ensures entities with ANY of the specified components match.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Find entities with either Invoice OR Payment components.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Enables flexible queries for entities with alternative components.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Supports union queries without SQL complexity.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> WithAny semantics must be preserved for query flexibility.</para>
    /// </remarks>
    [Fact]
    public async Task WithAny_ShouldUnionEntityIds()
    {
        // Arrange
        var registry = new FakeComponentQueryHandlerRegistry();
        var query = new EntityQuery(registry);
        
        var handler1 = new FakeComponentQueryHandler(typeof(TestComponent));
        handler1.SetEntityIds(new[] { Guid.Parse("00000000-0000-0000-0000-000000000001"), 
                                       Guid.Parse("00000000-0000-0000-0000-000000000002") });
        registry.AddHandler(typeof(TestComponent), handler1);
        
        var handler2 = new FakeComponentQueryHandler(typeof(VelocityComponent));
        handler2.SetEntityIds(new[] { Guid.Parse("00000000-0000-0000-0000-000000000003"), 
                                       Guid.Parse("00000000-0000-0000-0000-000000000004") });
        registry.AddHandler(typeof(VelocityComponent), handler2);
        
        // Act
        var result = await query
            .WithAny<TestComponent>(t => true)
            .WithAny<VelocityComponent>(v => true)
            .ToEntityIds();
        
        // Assert
        result.Count.ShouldBe(4);
        result.ShouldContain(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        result.ShouldContain(Guid.Parse("00000000-0000-0000-0000-000000000002"));
        result.ShouldContain(Guid.Parse("00000000-0000-0000-0000-000000000003"));
        result.ShouldContain(Guid.Parse("00000000-0000-0000-0000-000000000004"));
    }

    /// <summary>
    /// Validates that WithNone excludes entities with matching components.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Verifies WithNone implements NOT logic for entity filtering.</para>
    /// <para><strong>PURPOSE:</strong> Ensures entities with specified components are excluded.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Find entities without Payment components (unpaid invoices).</para>
    /// <para><strong>WHY IMPORTANT:</strong> Enables exclusion queries for finding entities missing components.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Completes the AND/OR/NOT query algebra.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> WithNone semantics must be preserved for exclusion queries.</para>
    /// </remarks>
    [Fact]
    public async Task WithNone_ShouldExcludeEntityIds()
    {
        // Arrange
        var registry = new FakeComponentQueryHandlerRegistry();
        var query = new EntityQuery(registry);
        
        var handler1 = new FakeComponentQueryHandler(typeof(TestComponent));
        handler1.SetEntityIds(new[] { Guid.Parse("00000000-0000-0000-0000-000000000001"), 
                                       Guid.Parse("00000000-0000-0000-0000-000000000002"),
                                       Guid.Parse("00000000-0000-0000-0000-000000000003") });
        registry.AddHandler(typeof(TestComponent), handler1);
        
        var handler2 = new FakeComponentQueryHandler(typeof(VelocityComponent));
        handler2.SetEntityIds(new[] { Guid.Parse("00000000-0000-0000-0000-000000000003") });
        registry.AddHandler(typeof(VelocityComponent), handler2);
        
        // Act
        var result = await query
            .WithAll<TestComponent>(t => true)
            .WithNone<VelocityComponent>(v => true)
            .ToEntityIds();
        
        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        result.ShouldContain(Guid.Parse("00000000-0000-0000-0000-000000000002"));
        result.ShouldNotContain(Guid.Parse("00000000-0000-0000-0000-000000000003"));
    }

    /// <summary>
    /// Validates that ToEntityComponents loads all relevant components.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Verifies component loading for matched entities.</para>
    /// <para><strong>PURPOSE:</strong> Ensures all components are loaded for query results.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Load Invoice and Payment data for processing.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Prevents N+1 query problems with eager loading.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Batched loading without SQL JOINs.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> Component loading must remain efficient.</para>
    /// </remarks>
    [Fact]
    public async Task ToEntityComponents_ShouldLoadAllComponents()
    {
        // Arrange
        var registry = new FakeComponentQueryHandlerRegistry();
        var query = new EntityQuery(registry);
        
        var entityId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        var handler1 = new FakeComponentQueryHandler(typeof(TestComponent));
        handler1.SetEntityIds(new[] { entityId });
        var testComponent = new TestComponent { Id = Guid.NewGuid(), OwnerEntityId = entityId };
        handler1.SetComponents(new[] { testComponent });
        registry.AddHandler(typeof(TestComponent), handler1);
        
        var handler2 = new FakeComponentQueryHandler(typeof(VelocityComponent));
        handler2.SetEntityIds(new[] { entityId });
        var velocityComponent = new VelocityComponent { Id = Guid.NewGuid(), OwnerEntityId = entityId };
        handler2.SetComponents(new[] { velocityComponent });
        registry.AddHandler(typeof(VelocityComponent), handler2);
        
        // Act
        var result = await query
            .WithAll<TestComponent>(t => true)
            .WithAll<VelocityComponent>(v => true)
            .ToEntityComponents();
        
        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContainKey(entityId);
        
        var components = result[entityId];
        components.Get<TestComponent>().ShouldBe(testComponent);
        components.Get<VelocityComponent>().ShouldBe(velocityComponent);
    }

    /// <summary>
    /// Validates that handlers receive the correct filter expressions.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Verifies filter expressions are passed to handlers correctly.</para>
    /// <para><strong>PURPOSE:</strong> Ensures component-specific filtering works as expected.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Filter unpaid invoices or pending work orders.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Enables component-specific business logic in queries.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Type-safe filter propagation without reflection.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> Filter expressions must remain type-safe.</para>
    /// </remarks>
    [Fact]
    public async Task Query_ShouldPassFiltersToHandlers()
    {
        // Arrange
        var registry = new FakeComponentQueryHandlerRegistry();
        var query = new EntityQuery(registry);
        
        LambdaExpression? capturedFilter = null;
        var handler = new TestingComponentQueryHandler(
            typeof(TestComponent),
            filter => { capturedFilter = filter; return Task.FromResult(Enumerable.Empty<Guid>()); });
        registry.AddHandler(typeof(TestComponent), handler);
        
        Expression<Func<TestComponent, bool>> expectedFilter = t => t.Name == "Test";
        
        // Act
        await query.WithAll(expectedFilter).ToEntityIds();
        
        // Assert
        capturedFilter.ShouldNotBeNull();
        capturedFilter.ShouldBe(expectedFilter);
    }

    private class TestingComponentQueryHandler : IComponentQueryHandler
    {
        private readonly Type _componentType;
        private readonly Func<LambdaExpression, Task<IEnumerable<Guid>>> _queryFunc;

        public TestingComponentQueryHandler(Type componentType, Func<LambdaExpression, Task<IEnumerable<Guid>>> queryFunc)
        {
            _componentType = componentType;
            _queryFunc = queryFunc;
        }

        public Type ComponentType => _componentType;

        public Task<IEnumerable<Guid>> QueryEntityIds(LambdaExpression filter)
        {
            return _queryFunc(filter);
        }

        public Task<IEnumerable<IComponent>> LoadComponents(IEnumerable<Guid> entityIds)
        {
            return Task.FromResult(Enumerable.Empty<IComponent>());
        }
    }
}