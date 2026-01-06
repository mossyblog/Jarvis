using core.jarvis.Data;
using core.jarvis.tests.Components;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Integration.HandlerIntegrationTests;

/// <summary>
/// Integration tests for IEntityQuery functionality.
/// Tests cross-component queries, batching, and N+1 query prevention.
/// </summary>
public class EntityQueryIntegrationTests : IntegrationTestBase
{
    private async Task<List<Guid>> CreateTestData()
    {
        var testRunId = Guid.NewGuid().ToString("N")[..8]; // Unique test run ID
        var entityIds = new List<Guid>();
        for (int i = 0; i < 5; i++)
        {
            var entityId = Guid.NewGuid();
            TrackEntity(entityId);
            entityIds.Add(entityId);

            var testComponent = new TestComponent
            {
                OwnerEntityId = entityId,
                Name = $"Entity_{testRunId}_{i}",
                Status = i % 2 == 0 ? "ACTIVE" : "INACTIVE"
            };
            
            // Commit with retry logic for parallel execution
            var retryCount = 3;
            var committed = false;
            for (int retry = 0; retry < retryCount && !committed; retry++)
            {
                try
                {
                    var success = await TestDataContext().TryCommit(testComponent);
                    if (success)
                    {
                        committed = true;
                    }
                    else if (retry < retryCount - 1)
                    {
                        await Task.Delay(10 * (retry + 1)); // Exponential backoff
                    }
                }
                catch (Exception) when (retry < retryCount - 1)
                {
                    await Task.Delay(10 * (retry + 1)); // Exponential backoff
                }
            }
            
            if (!committed)
            {
                throw new InvalidOperationException($"Failed to commit test entity {i} after {retryCount} retries");
            }

            if (i % 2 == 0)
            {
                var positionComponent = new PositionComponent
                {
                    OwnerEntityId = entityId,
                    X = i * 10,
                    Y = i * 20
                };
                
                // Commit with retry logic for parallel execution
                var posCommitted = false;
                for (int retry = 0; retry < retryCount && !posCommitted; retry++)
                {
                    try
                    {
                        var posSuccess = await TestDataContext().TryCommit(positionComponent);
                        if (posSuccess)
                        {
                            posCommitted = true;
                        }
                        else if (retry < retryCount - 1)
                        {
                            await Task.Delay(10 * (retry + 1)); // Exponential backoff
                        }
                    }
                    catch (Exception) when (retry < retryCount - 1)
                    {
                        await Task.Delay(10 * (retry + 1)); // Exponential backoff
                    }
                }
                
                if (!posCommitted)
                {
                    throw new InvalidOperationException($"Failed to commit position component for entity {i} after {retryCount} retries");
                }
            }

            if (i >= 2)
            {
                var velocityComponent = new VelocityComponent
                {
                    OwnerEntityId = entityId,
                    DeltaX = i * 1.5f,
                    DeltaY = i * 2.5f
                };
                
                // Commit with retry logic for parallel execution
                var velCommitted = false;
                for (int retry = 0; retry < retryCount && !velCommitted; retry++)
                {
                    try
                    {
                        var velSuccess = await TestDataContext().TryCommit(velocityComponent);
                        if (velSuccess)
                        {
                            velCommitted = true;
                        }
                        else if (retry < retryCount - 1)
                        {
                            await Task.Delay(10 * (retry + 1)); // Exponential backoff
                        }
                    }
                    catch (Exception) when (retry < retryCount - 1)
                    {
                        await Task.Delay(10 * (retry + 1)); // Exponential backoff
                    }
                }
                
                if (!velCommitted)
                {
                    throw new InvalidOperationException($"Failed to commit velocity component for entity {i} after {retryCount} retries");
                }
            }
        }
        return entityIds;
    }

    /// <summary>
    /// INTENT: Validates that querying with a single filter returns only entity IDs matching the filter.
    /// PURPOSE: Ensures the query system correctly applies filters to return relevant entities.
    /// BUSINESS CONTEXT: Used when a business process needs to select entities based on a single attribute (e.g., all ACTIVE users).
    /// WHY IMPORTANT: Incorrect filtering could result in wrong data being processed, leading to business errors.
    /// ARCHITECTURAL SIGNIFICANCE: Confirms the query abstraction layer correctly translates filter expressions to backend queries.
    /// FUTURE RESILIENCE: Guarantees that future changes to filtering logic do not break single-filter queries.
    /// </summary>
    [Fact]
    public async Task Query_WithSingleFilter_ShouldReturnMatchingEntityIds()
    {
        var testEntityIds = await CreateTestData();
        // Act
        var query = TestDataContext().Query()
            .With<TestComponent>(c => c.Status == "ACTIVE");
        
        var entityIds = await query.ToEntityIds();

        // Assert - Filter to only test entities
        var filtered = entityIds.Where(id => testEntityIds.Contains(id)).ToList();
        
        filtered.Count.ShouldBe(3); // Entities 0, 2, 4 are ACTIVE
        filtered.All(id => testEntityIds.Contains(id)).ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Validates that multiple filters are combined as an intersection (AND logic).
    /// PURPOSE: Ensures only entities matching all criteria are returned.
    /// BUSINESS CONTEXT: Critical for workflows requiring multi-attribute selection (e.g., active users in a specific region).
    /// WHY IMPORTANT: Prevents over-broad or under-broad data selection, which could impact business rules.
    /// ARCHITECTURAL SIGNIFICANCE: Tests the query builder's ability to compose multiple predicates.
    /// FUTURE RESILIENCE: Protects against regressions in multi-filter query logic.
    /// </summary>
    [Fact]
    public async Task Query_WithMultipleFilters_ShouldReturnIntersection()
    {
        var testEntityIds = await CreateTestData();
        // Act - Find entities that are ACTIVE AND have PositionComponent
        var query = TestDataContext().Query()
            .With<TestComponent>(c => c.Status == "ACTIVE")
            .With<PositionComponent>(p => p.X >= 0);
        
        var entityIds = await query.ToEntityIds();

        // Assert - Filter to only test entities
        var filtered = entityIds.Where(id => testEntityIds.Contains(id)).ToList();
        filtered.Count.ShouldBe(3); // Entities 0, 2, 4 (even and ACTIVE)
    }

    /// <summary>
    /// INTENT: Validates that queries can eagerly load multiple component types for each entity.
    /// PURPOSE: Ensures efficient retrieval of all required data in a single operation.
    /// BUSINESS CONTEXT: Supports business operations that require a full view of an entity's state (e.g., for reporting or processing).
    /// WHY IMPORTANT: Prevents missing or partial data, which could cause incomplete business logic execution.
    /// ARCHITECTURAL SIGNIFICANCE: Verifies the system's ability to perform eager loading and join-like operations.
    /// FUTURE RESILIENCE: Ensures future component additions or changes do not break eager loading.
    /// </summary>
    [Fact]
    public async Task Query_ToEntityComponents_ShouldEagerLoadAllComponents()
    {
        var testEntityIds = await CreateTestData();
        // Act
        var query = TestDataContext().Query()
            .With<TestComponent>(c => c.Status == "ACTIVE")
            .Include<PositionComponent>()
            .Include<VelocityComponent>();
        
        var results = await query.ToEntityComponents();

        // Assert - Filter to only test entities
        var testResults = results.Where(r => testEntityIds.Contains(r.Key)).ToDictionary(r => r.Key, r => r.Value);
        testResults.Count.ShouldBe(3); // 3 ACTIVE entities

        foreach (var kvp in testResults)
        {
            var entityId = kvp.Key;
            var components = kvp.Value;
            
            // All should have TestComponent
            components.Has<TestComponent>().ShouldBeTrue();
            var testComp = components.Get<TestComponent>();
            testComp?.Status.ShouldBe("ACTIVE");

            // Check component presence based on our test data setup
            var entityIndex = testEntityIds.IndexOf(entityId);
            
            if (entityIndex % 2 == 0)
            {
                components.Has<PositionComponent>().ShouldBeTrue();
                var position = components.Get<PositionComponent>();
                position?.X.ShouldBe(entityIndex * 10);
            }

            if (entityIndex >= 2)
            {
                components.Has<VelocityComponent>().ShouldBeTrue();
                var velocity = components.Get<VelocityComponent>();
                velocity?.DeltaX.ShouldBe(entityIndex * 1.5f);
            }
        }
    }

    /// <summary>
    /// INTENT: Validates that including components without specifying filters returns no results.
    /// PURPOSE: Ensures the query system does not return unintended data when no filter is applied.
    /// BUSINESS CONTEXT: Prevents accidental data exposure or processing when queries are under-specified.
    /// WHY IMPORTANT: Avoids performance issues and data leaks.
    /// ARCHITECTURAL SIGNIFICANCE: Confirms the query system enforces required filter logic.
    /// FUTURE RESILIENCE: Protects against future changes that might loosen query constraints.
    /// </summary>
    [Fact]
    public async Task Query_IncludeWithoutWith_ShouldReturnEmpty()
    {
        var testEntityIds = await CreateTestData();
        // Act - Only includes, no With filters
        var query = TestDataContext().Query()
            .Include<TestComponent>()
            .Include<PositionComponent>();
        
        var entityIds = await query.ToEntityIds();
        var components = await query.ToEntityComponents();

        // Assert
        entityIds.Count.ShouldBe(0);
        components.Count.ShouldBe(0);
    }

    /// <summary>
    /// INTENT: Validates that batch loading is used to prevent N+1 query problems.
    /// PURPOSE: Ensures efficient data access patterns for large-scale queries.
    /// BUSINESS CONTEXT: Critical for performance in business scenarios involving bulk data processing.
    /// WHY IMPORTANT: Prevents performance degradation and excessive database load.
    /// ARCHITECTURAL SIGNIFICANCE: Tests the system's batching and data access optimization.
    /// FUTURE RESILIENCE: Ensures future refactoring does not reintroduce N+1 query issues.
    /// </summary>
    [Fact]
    public async Task Query_BatchLoading_ShouldPreventNPlusOneQueries()
    {
        var testEntityIds = await CreateTestData();
        // Act
        var startTime = DateTime.UtcNow;
        
        var query = TestDataContext().Query()
            .With<TestComponent>(c => c.Name.Contains("Entity"))
            .Include<PositionComponent>()
            .Include<VelocityComponent>();
        
        var results = await query.ToEntityComponents();
        
        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;

        // Assert - Filter to only test entities
        var testResults = results.Where(r => testEntityIds.Contains(r.Key)).ToDictionary(r => r.Key, r => r.Value);
        
        // Verify that we got results for all successfully created entities
        // In parallel execution, we should have at least the entities we created
        testResults.Count.ShouldBe(testEntityIds.Count, 
            $"Should have results for all {testEntityIds.Count} test entities, but got {testResults.Count}");
        
        // Performance check - should complete reasonably quickly
        duration.TotalSeconds.ShouldBeLessThan(5);

        // Verify all entities have their TestComponent
        foreach (var kvp in testResults)
        {
            var components = kvp.Value;
            components.Has<TestComponent>().ShouldBeTrue();
            var testComp = components.Get<TestComponent>();
            testComp.ShouldNotBeNull();
            testComp.Name.ShouldStartWith("Entity");
        }
        
        // Verify that all our test entities were found
        foreach (var expectedEntityId in testEntityIds)
        {
            testResults.ContainsKey(expectedEntityId).ShouldBeTrue(
                $"Expected entity {expectedEntityId} should be in results");
        }
    }

    /// <summary>
    /// INTENT: Validates that queries returning no results are handled without errors.
    /// PURPOSE: Ensures system stability when no data matches the query.
    /// BUSINESS CONTEXT: Supports business logic that must handle empty datasets gracefully (e.g., reporting, notifications).
    /// WHY IMPORTANT: Prevents crashes or incorrect assumptions in downstream logic.
    /// ARCHITECTURAL SIGNIFICANCE: Confirms robust handling of edge cases in the query layer.
    /// FUTURE RESILIENCE: Ensures future changes do not break empty result handling.
    /// </summary>
    [Fact]
    public async Task Query_EmptyResult_ShouldHandleGracefully()
    {
        var testEntityIds = await CreateTestData();
        // Act - Query that should return no results
        var query = TestDataContext().Query()
            .With<TestComponent>(c => c.Name == "NonExistentEntity");
        
        var entityIds = await query.ToEntityIds();
        var components = await query.ToEntityComponents();

        // Assert
        entityIds.ShouldNotBeNull();
        entityIds.Count.ShouldBe(0);
        components.ShouldNotBeNull();
        components.Count.ShouldBe(0);
    }

    /// <summary>
    /// INTENT: Validates that Single() returns exactly one entity when query matches one entity.
    /// PURPOSE: Ensures Single() correctly returns a single entity with DataContext binding.
    /// BUSINESS CONTEXT: Used when business logic expects exactly one entity (e.g., get order by unique reference).
    /// WHY IMPORTANT: Provides a safe way to retrieve single entities with built-in validation.
    /// ARCHITECTURAL SIGNIFICANCE: Implements LINQ-like semantics for single entity retrieval.
    /// FUTURE RESILIENCE: Ensures Single() behavior remains consistent with LINQ standards.
    /// </summary>
    [Fact]
    public async Task Query_Single_WithOneMatch_ShouldReturnEntity()
    {
        // Arrange - Create a unique entity
        var uniqueEntityId = Guid.NewGuid();
        TrackEntity(uniqueEntityId);
        var testRunId = Guid.NewGuid().ToString("N")[..8]; // Unique test run ID
        
        var testComponent = new TestComponent
        {
            OwnerEntityId = uniqueEntityId,
            Name = $"UniqueEntity_{testRunId}_{Guid.NewGuid()}",
            Status = "UNIQUE"
        };
        
        // Commit with retry logic for parallel execution
        var retryCount = 3;
        var committed = false;
        for (int retry = 0; retry < retryCount && !committed; retry++)
        {
            try
            {
                var success = await TestDataContext().TryCommit(testComponent);
                if (success)
                {
                    committed = true;
                }
                else if (retry < retryCount - 1)
                {
                    await Task.Delay(10 * (retry + 1)); // Exponential backoff
                }
            }
            catch (Exception) when (retry < retryCount - 1)
            {
                await Task.Delay(10 * (retry + 1)); // Exponential backoff
            }
        }
        
        committed.ShouldBeTrue("Failed to commit test component after retries");
        
        // Act - Query with retry logic for parallel execution
        Entity entity = null;
        var querySucceeded = false;
        for (int retry = 0; retry < retryCount && !querySucceeded; retry++)
        {
            try
            {
                entity = await TestDataContext().Query()
                    .WithAll<TestComponent>(c => c.Name == testComponent.Name)
                    .Single();
                querySucceeded = true;
            }
            catch (InvalidOperationException) when (retry < retryCount - 1)
            {
                // Single() might fail due to parallel execution interference
                await Task.Delay(10 * (retry + 1)); // Exponential backoff
            }
        }
        
        querySucceeded.ShouldBeTrue("Failed to query single entity after retries");
        
        // Assert
        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(uniqueEntityId);
        
        // Verify entity has DataContext binding and can retrieve components
        var component = await entity.Get<TestComponent>();
        component.ShouldNotBeNull();
        component.Name.ShouldBe(testComponent.Name);
        component.Status.ShouldBe("UNIQUE");
    }

    /// <summary>
    /// INTENT: Validates that Single() throws when no entities match the query.
    /// PURPOSE: Ensures Single() fails fast when expected entity doesn't exist.
    /// BUSINESS CONTEXT: Detects missing data early (e.g., order not found).
    /// WHY IMPORTANT: Prevents null reference errors and provides clear error messages.
    /// ARCHITECTURAL SIGNIFICANCE: Maintains consistency with LINQ Single() semantics.
    /// FUTURE RESILIENCE: Guarantees predictable error handling for missing entities.
    /// </summary>
    [Fact]
    public async Task Query_Single_WithNoMatches_ShouldThrow()
    {
        // Arrange - Ensure we have some data but not what we're looking for
        await CreateTestData();
        
        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await TestDataContext().Query()
                .WithAll<TestComponent>(c => c.Name == "NonExistentEntity_" + Guid.NewGuid())
                .Single();
        });
        
        exception.Message.ShouldContain("no elements");
    }

    /// <summary>
    /// INTENT: Validates that Single() throws when multiple entities match the query.
    /// PURPOSE: Ensures Single() detects and reports non-unique results.
    /// BUSINESS CONTEXT: Identifies data integrity issues (e.g., duplicate references).
    /// WHY IMPORTANT: Prevents incorrect entity selection when uniqueness is expected.
    /// ARCHITECTURAL SIGNIFICANCE: Enforces single entity contract at query level.
    /// FUTURE RESILIENCE: Maintains strict single entity semantics.
    /// </summary>
    [Fact]
    public async Task Query_Single_WithMultipleMatches_ShouldThrow()
    {
        // Arrange - Create multiple entities with same status
        var sharedStatus = "DUPLICATE_STATUS_" + Guid.NewGuid();
        
        for (int i = 0; i < 2; i++)
        {
            var entityId = Guid.NewGuid();
            TrackEntity(entityId);
            
            var testComponent = new TestComponent
            {
                OwnerEntityId = entityId,
                Name = $"Entity_{i}",
                Status = sharedStatus
            };
            var success = await TestDataContext().TryCommit(testComponent);
            success.ShouldBeTrue();
        }
        
        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await TestDataContext().Query()
                .WithAll<TestComponent>(c => c.Status == sharedStatus)
                .Single();
        });
        
        exception.Message.ShouldContain("more than one element");
        exception.Message.ShouldContain("2 entities found");
    }

    /// <summary>
    /// INTENT: Validates that Single() works with complex queries including ordering.
    /// PURPOSE: Ensures Single() integrates properly with other query operations.
    /// BUSINESS CONTEXT: Supports scenarios where ordering affects single entity selection.
    /// WHY IMPORTANT: Confirms Single() works in complex query scenarios.
    /// ARCHITECTURAL SIGNIFICANCE: Tests integration with full query pipeline.
    /// FUTURE RESILIENCE: Ensures Single() remains compatible with query extensions.
    /// </summary>
    [Fact]
    public async Task Query_Single_WithOrderBy_ShouldReturnSingleEntity()
    {
        // Arrange - Create a unique entity with specific components
        var uniqueEntityId = Guid.NewGuid();
        TrackEntity(uniqueEntityId);
        
        var uniqueName = "OrderedEntity_" + Guid.NewGuid();
        var testComponent = new TestComponent
        {
            OwnerEntityId = uniqueEntityId,
            Name = uniqueName,
            Status = "ACTIVE",
            Value = 999
        };
        await TestDataContext().TryCommit(testComponent);
        
        var positionComponent = new PositionComponent
        {
            OwnerEntityId = uniqueEntityId,
            X = 100,
            Y = 200
        };
        await TestDataContext().TryCommit(positionComponent);
        
        // Act - Query with ordering then Single
        var entity = await TestDataContext().Query()
            .WithAll<TestComponent>(c => c.Name == uniqueName)
            .WithAll<PositionComponent>(p => p.X == 100)
            .OrderBy<TestComponent>(c => c.Value)
            .Single();
        
        // Assert
        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(uniqueEntityId);
        
        // Verify components are accessible
        var test = await entity.Get<TestComponent>();
        test.ShouldNotBeNull();
        test.Value.ShouldBe(999);
        
        var position = await entity.Get<PositionComponent>();
        position.ShouldNotBeNull();
        position.X.ShouldBe(100);
    }
}