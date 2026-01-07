using core.jarvis.Data.Query;
using core.jarvis.tests.Components;
using core.jarvis.tests.Helpers;
using Shouldly;

namespace core.jarvis.tests.Integration;

/// <summary>
/// Comprehensive E2E integration tests for the Filter&lt;T&gt; solution (jarvis-27).
/// Tests prove that all filter operators work correctly against a real PostgreSQL database.
/// </summary>
public class FilterIntegrationTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify case-insensitive email/string lookup works correctly.
    /// PURPOSE: Applications need to find records regardless of casing (e.g., email lookup at login).
    /// BUSINESS CONTEXT: User enters 'test@example.com' but record stored as 'Test@Example.COM'.
    /// WHY IMPORTANT: Case sensitivity in lookups causes user friction and support tickets.
    /// ARCHITECTURAL SIGNIFICANCE: Tests IgnoreCase() modifier on Eq filter.
    /// </summary>
    [Fact]
    public async Task Filter_Eq_IgnoreCase_ShouldFindAccountCaseInsensitively()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);

        var component = new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId,
            Name = "Test@Example.COM",
            Status = "ACTIVE",
            Value = 100,
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(component);

        // Act - Query with different casing using IgnoreCase
        var filter = Filter<TestComponent>.Eq(c => c.Name, "test@example.com").IgnoreCase();
        var results = await TestDataContext().Query()
            .WithAll(filter)
            .ToEntityIds();

        // Assert
        results.ShouldContain(entityId);
    }

    /// <summary>
    /// INTENT: Verify case-sensitive matching only finds exact matches.
    /// PURPOSE: Some fields require exact case matching (e.g., codes, identifiers).
    /// BUSINESS CONTEXT: Part numbers like 'ABC-123' must match exactly, not 'abc-123'.
    /// WHY IMPORTANT: Case-sensitive matching prevents false positives in lookups.
    /// ARCHITECTURAL SIGNIFICANCE: Tests default case-sensitive behavior of Eq filter.
    /// </summary>
    [Fact]
    public async Task Filter_Eq_CaseSensitive_ShouldOnlyFindExactMatch()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var exactName = $"ExactCase-{uniqueSuffix}";

        var component1 = new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId1,
            Name = exactName,
            Status = "ACTIVE",
            Value = 100,
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(component1);

        var component2 = new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId2,
            Name = exactName.ToLower(),
            Status = "ACTIVE",
            Value = 200,
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(component2);

        // Act - Query with exact case (no IgnoreCase)
        var filterExact = Filter<TestComponent>.Eq(c => c.Name, exactName);
        var exactResults = await TestDataContext().Query()
            .WithAll(filterExact)
            .ToEntityIds();

        // Assert - Should only find the exact match
        exactResults.ShouldContain(entityId1);
        exactResults.ShouldNotContain(entityId2);

        // Act - Query with different case (no IgnoreCase) should NOT find it
        var filterDifferentCase = Filter<TestComponent>.Eq(c => c.Name, exactName.ToUpper());
        var differentCaseResults = await TestDataContext().Query()
            .WithAll(filterDifferentCase)
            .ToEntityIds();

        // Assert - Should not find any matches with different case
        differentCaseResults.ShouldNotContain(entityId1);
        differentCaseResults.ShouldNotContain(entityId2);
    }

    /// <summary>
    /// INTENT: Verify greater-than operator returns only values above threshold.
    /// PURPOSE: Business queries like "find all high-value orders" need comparison operators.
    /// BUSINESS CONTEXT: Reports filtering records above a threshold (e.g., Value > 100).
    /// WHY IMPORTANT: Comparison operators are fundamental to data filtering.
    /// ARCHITECTURAL SIGNIFICANCE: Tests Gt filter operator against database.
    /// </summary>
    [Fact]
    public async Task Filter_Gt_ShouldReturnOnlyGreaterValues()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];

        // Create components with different values: 50, 100, 150
        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId1,
            Name = $"Low-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 50,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId2,
            Name = $"Medium-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 100,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId3,
            Name = $"High-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 150,
            LastUpdated = DateTime.UtcNow
        });

        // Act - Query for Value > 100
        var filter = Filter<TestComponent>.Gt(c => c.Value, 100)
            .And(Filter<TestComponent>.Contains(c => c.Name, uniqueSuffix));
        var results = await TestDataContext().Query()
            .WithAll(filter)
            .ToEntityIds();

        // Assert - Should only return entity with Value = 150
        results.ShouldNotContain(entityId1); // 50 is not > 100
        results.ShouldNotContain(entityId2); // 100 is not > 100 (equal, not greater)
        results.ShouldContain(entityId3);    // 150 > 100
    }

    /// <summary>
    /// INTENT: Verify Contains operator finds partial string matches.
    /// PURPOSE: Search functionality needs to find records containing a substring.
    /// BUSINESS CONTEXT: User searches for "smith" and finds "John Smith", "Smithson", etc.
    /// WHY IMPORTANT: Substring search is essential for user-friendly search interfaces.
    /// ARCHITECTURAL SIGNIFICANCE: Tests Contains filter which uses LIKE '%value%'.
    /// </summary>
    [Fact]
    public async Task Filter_Contains_ShouldFindPartialMatches()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var uniqueMarker = Guid.NewGuid().ToString("N")[..8];
        var searchTerm = $"FINDME{uniqueMarker}";

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId1,
            Name = $"Prefix-{searchTerm}-Suffix",
            Status = "ACTIVE",
            Value = 1,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId2,
            Name = $"{searchTerm}AtStart",
            Status = "ACTIVE",
            Value = 2,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId3,
            Name = $"NoMatch-{uniqueMarker}",
            Status = "ACTIVE",
            Value = 3,
            LastUpdated = DateTime.UtcNow
        });

        // Act - Search for the unique search term
        var filter = Filter<TestComponent>.Contains(c => c.Name, searchTerm);
        var results = await TestDataContext().Query()
            .WithAll(filter)
            .ToEntityIds();

        // Assert - Should find both entities containing the search term
        results.ShouldContain(entityId1); // Contains in middle
        results.ShouldContain(entityId2); // Contains at start
        results.ShouldNotContain(entityId3); // Does not contain FINDME
    }

    /// <summary>
    /// INTENT: Verify And operator returns intersection of two filter conditions.
    /// PURPOSE: Complex queries need to combine multiple conditions.
    /// BUSINESS CONTEXT: "Find active accounts with balance over $1000".
    /// WHY IMPORTANT: Real queries almost always have multiple conditions.
    /// ARCHITECTURAL SIGNIFICANCE: Tests And() combinator on filter expressions.
    /// </summary>
    [Fact]
    public async Task Filter_And_ShouldReturnIntersection()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var activeStatus = $"ACTIVE-{uniqueSuffix}";
        var inactiveStatus = $"INACTIVE-{uniqueSuffix}";

        // Entity 1: Active, high value - should match
        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId1,
            Name = $"Active-High-{uniqueSuffix}",
            Status = activeStatus,
            Value = 150,
            LastUpdated = DateTime.UtcNow
        });

        // Entity 2: Active, low value - should NOT match (value too low)
        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId2,
            Name = $"Active-Low-{uniqueSuffix}",
            Status = activeStatus,
            Value = 50,
            LastUpdated = DateTime.UtcNow
        });

        // Entity 3: Inactive, high value - should NOT match (wrong status)
        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId3,
            Name = $"Inactive-High-{uniqueSuffix}",
            Status = inactiveStatus,
            Value = 200,
            LastUpdated = DateTime.UtcNow
        });

        // Act - Query for Status = 'ACTIVE-xxx' AND Value > 100
        var filter = Filter<TestComponent>.Eq(c => c.Status, activeStatus)
            .And(Filter<TestComponent>.Gt(c => c.Value, 100));
        var results = await TestDataContext().Query()
            .WithAll(filter)
            .ToEntityIds();

        // Assert - Only entity1 meets both conditions
        results.ShouldContain(entityId1);    // Active AND high value
        results.ShouldNotContain(entityId2); // Active but low value
        results.ShouldNotContain(entityId3); // High value but inactive
    }

    /// <summary>
    /// INTENT: Verify In operator matches against multiple values.
    /// PURPOSE: Queries often need to match one of several possible values.
    /// BUSINESS CONTEXT: "Find orders with status: Pending, Processing, or Shipped".
    /// WHY IMPORTANT: In operator is more efficient than multiple OR conditions.
    /// ARCHITECTURAL SIGNIFICANCE: Tests In filter which uses SQL IN clause.
    /// </summary>
    [Fact]
    public async Task Filter_In_ShouldMatchMultipleValues()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        var entityId4 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);
        TrackEntity(entityId4);

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var pending = $"PENDING-{uniqueSuffix}";
        var processing = $"PROCESSING-{uniqueSuffix}";
        var shipped = $"SHIPPED-{uniqueSuffix}";
        var cancelled = $"CANCELLED-{uniqueSuffix}";

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId1,
            Name = $"Order1-{uniqueSuffix}",
            Status = pending,
            Value = 1,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId2,
            Name = $"Order2-{uniqueSuffix}",
            Status = processing,
            Value = 2,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId3,
            Name = $"Order3-{uniqueSuffix}",
            Status = shipped,
            Value = 3,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId4,
            Name = $"Order4-{uniqueSuffix}",
            Status = cancelled,
            Value = 4,
            LastUpdated = DateTime.UtcNow
        });

        // Act - Query for Status IN (pending, processing, shipped) - should exclude cancelled
        var filter = Filter<TestComponent>.In(c => c.Status, new[] { pending, processing, shipped });
        var results = await TestDataContext().Query()
            .WithAll(filter)
            .ToEntityIds();

        // Assert
        results.ShouldContain(entityId1); // Pending
        results.ShouldContain(entityId2); // Processing
        results.ShouldContain(entityId3); // Shipped
        results.ShouldNotContain(entityId4); // Cancelled is not in the list
    }

    /// <summary>
    /// INTENT: Verify All() filter returns all records without filtering.
    /// PURPOSE: Sometimes queries need to return all records of a component type.
    /// BUSINESS CONTEXT: "List all accounts" with no filtering.
    /// WHY IMPORTANT: All() replaces the (c => true) lambda pattern.
    /// ARCHITECTURAL SIGNIFICANCE: Tests MatchAllFilter implementation.
    /// </summary>
    [Fact]
    public async Task Filter_All_ShouldReturnAllRecords()
    {
        // Arrange - Create multiple test entities with unique marker
        var entityIds = new List<Guid>();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];

        for (int i = 0; i < 3; i++)
        {
            var entityId = Guid.NewGuid();
            entityIds.Add(entityId);
            TrackEntity(entityId);

            await TestDataContext().Commit(new TestComponent
            {
                Id = Guid.NewGuid(),
                OwnerEntityId = entityId,
                Name = $"AllTest-{uniqueSuffix}-{i}",
                Status = "ACTIVE",
                Value = i * 10,
                LastUpdated = DateTime.UtcNow
            });
        }

        // Act - Query with All() filter combined with a Contains to narrow to our test data
        var filter = Filter<TestComponent>.All()
            .And(Filter<TestComponent>.Contains(c => c.Name, $"AllTest-{uniqueSuffix}"));
        var results = await TestDataContext().Query()
            .WithAll(filter)
            .ToEntityIds();

        // Assert - Should return all entities with our unique marker
        foreach (var entityId in entityIds)
        {
            results.ShouldContain(entityId);
        }
        results.Count.ShouldBe(entityIds.Count);
    }

    /// <summary>
    /// INTENT: Verify filter prevents SQL injection attacks.
    /// PURPOSE: Security-critical validation that malicious input cannot execute arbitrary SQL.
    /// BUSINESS CONTEXT: Attackers may try to inject SQL via search fields.
    /// WHY IMPORTANT: SQL injection is a critical security vulnerability.
    /// ARCHITECTURAL SIGNIFICANCE: Tests parameterized queries protect against injection.
    /// </summary>
    [Fact]
    public async Task Filter_ShouldPreventSQLInjection()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId,
            Name = $"SafeRecord-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 100,
            LastUpdated = DateTime.UtcNow
        });

        // Act - Attempt SQL injection through filter value
        // This malicious input should be treated as a literal string, not executed as SQL
        var maliciousInput = "'; DROP TABLE test_component; --";
        var filter = Filter<TestComponent>.Eq(c => c.Name, maliciousInput);

        // This should execute safely without errors
        Exception? caughtException = null;
        List<Guid>? results = null;
        try
        {
            results = await TestDataContext().Query()
                .WithAll(filter)
                .ToEntityIds();
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert - Query should execute safely (no exception)
        caughtException.ShouldBeNull("Query with SQL injection attempt should execute safely");
        results.ShouldNotBeNull();

        // The malicious input should be treated as a literal string search
        // No records should match because no record has that name
        results.ShouldNotContain(entityId);

        // Verify our test data still exists (table was not dropped)
        var verifyFilter = Filter<TestComponent>.Contains(c => c.Name, $"SafeRecord-{uniqueSuffix}");
        var verifyResults = await TestDataContext().Query()
            .WithAll(verifyFilter)
            .ToEntityIds();
        verifyResults.ShouldContain(entityId, "Original record should still exist after injection attempt");
    }

    /// <summary>
    /// INTENT: Verify less-than operator works correctly.
    /// PURPOSE: Queries need to find records below a threshold.
    /// BUSINESS CONTEXT: "Find low inventory items" (quantity < 10).
    /// WHY IMPORTANT: Lt operator complements Gt for range queries.
    /// ARCHITECTURAL SIGNIFICANCE: Tests Lt filter operator.
    /// </summary>
    [Fact]
    public async Task Filter_Lt_ShouldReturnOnlyLesserValues()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId1,
            Name = $"Lt-Low-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 50,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId2,
            Name = $"Lt-Equal-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 100,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId3,
            Name = $"Lt-High-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 150,
            LastUpdated = DateTime.UtcNow
        });

        // Act - Query for Value < 100
        var filter = Filter<TestComponent>.Lt(c => c.Value, 100)
            .And(Filter<TestComponent>.Contains(c => c.Name, uniqueSuffix));
        var results = await TestDataContext().Query()
            .WithAll(filter)
            .ToEntityIds();

        // Assert
        results.ShouldContain(entityId1);    // 50 < 100
        results.ShouldNotContain(entityId2); // 100 is not < 100
        results.ShouldNotContain(entityId3); // 150 is not < 100
    }

    /// <summary>
    /// INTENT: Verify greater-than-or-equal operator includes boundary value.
    /// PURPOSE: Range queries often need to include the boundary (inclusive).
    /// BUSINESS CONTEXT: "Find premium accounts with balance >= 1000".
    /// WHY IMPORTANT: Gte vs Gt determines if boundary is included.
    /// ARCHITECTURAL SIGNIFICANCE: Tests Gte filter operator.
    /// </summary>
    [Fact]
    public async Task Filter_Gte_ShouldIncludeBoundaryValue()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId1,
            Name = $"Gte-Below-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 99,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId2,
            Name = $"Gte-Boundary-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 100,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId3,
            Name = $"Gte-Above-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 101,
            LastUpdated = DateTime.UtcNow
        });

        // Act - Query for Value >= 100
        var filter = Filter<TestComponent>.Gte(c => c.Value, 100)
            .And(Filter<TestComponent>.Contains(c => c.Name, uniqueSuffix));
        var results = await TestDataContext().Query()
            .WithAll(filter)
            .ToEntityIds();

        // Assert
        results.ShouldNotContain(entityId1); // 99 is not >= 100
        results.ShouldContain(entityId2);    // 100 >= 100 (boundary included)
        results.ShouldContain(entityId3);    // 101 >= 100
    }

    /// <summary>
    /// INTENT: Verify less-than-or-equal operator includes boundary value.
    /// PURPOSE: Range queries often need to include the upper boundary.
    /// BUSINESS CONTEXT: "Find economy shipping options (weight <= 5kg)".
    /// WHY IMPORTANT: Lte vs Lt determines if boundary is included.
    /// ARCHITECTURAL SIGNIFICANCE: Tests Lte filter operator.
    /// </summary>
    [Fact]
    public async Task Filter_Lte_ShouldIncludeBoundaryValue()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId1,
            Name = $"Lte-Below-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 99,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId2,
            Name = $"Lte-Boundary-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 100,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId3,
            Name = $"Lte-Above-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 101,
            LastUpdated = DateTime.UtcNow
        });

        // Act - Query for Value <= 100
        var filter = Filter<TestComponent>.Lte(c => c.Value, 100)
            .And(Filter<TestComponent>.Contains(c => c.Name, uniqueSuffix));
        var results = await TestDataContext().Query()
            .WithAll(filter)
            .ToEntityIds();

        // Assert
        results.ShouldContain(entityId1);    // 99 <= 100
        results.ShouldContain(entityId2);    // 100 <= 100 (boundary included)
        results.ShouldNotContain(entityId3); // 101 is not <= 100
    }

    /// <summary>
    /// INTENT: Verify not-equal operator excludes matching records.
    /// PURPOSE: Queries need to exclude specific values.
    /// BUSINESS CONTEXT: "Find all orders except cancelled ones".
    /// WHY IMPORTANT: Neq is essential for exclusion patterns.
    /// ARCHITECTURAL SIGNIFICANCE: Tests Neq filter operator.
    /// </summary>
    [Fact]
    public async Task Filter_Neq_ShouldExcludeMatchingRecords()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var excludeStatus = $"EXCLUDE-{uniqueSuffix}";
        var includeStatus = $"INCLUDE-{uniqueSuffix}";

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId1,
            Name = $"Neq-Excluded-{uniqueSuffix}",
            Status = excludeStatus,
            Value = 100,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId2,
            Name = $"Neq-Included-{uniqueSuffix}",
            Status = includeStatus,
            Value = 100,
            LastUpdated = DateTime.UtcNow
        });

        // Act - Query for Status != excludeStatus
        var filter = Filter<TestComponent>.Neq(c => c.Status, excludeStatus)
            .And(Filter<TestComponent>.Contains(c => c.Name, uniqueSuffix));
        var results = await TestDataContext().Query()
            .WithAll(filter)
            .ToEntityIds();

        // Assert
        results.ShouldNotContain(entityId1); // Has excluded status
        results.ShouldContain(entityId2);    // Has different status
    }

    /// <summary>
    /// INTENT: Verify StartsWith operator finds prefix matches.
    /// PURPOSE: Autocomplete and prefix search functionality.
    /// BUSINESS CONTEXT: User types "Joh" and sees "John", "Johnson", "Johanna".
    /// WHY IMPORTANT: StartsWith is more efficient than Contains for prefix search.
    /// ARCHITECTURAL SIGNIFICANCE: Tests StartsWith filter which uses LIKE 'value%'.
    /// </summary>
    [Fact]
    public async Task Filter_StartsWith_ShouldFindPrefixMatches()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var uniquePrefix = $"PREFIX{Guid.NewGuid().ToString("N")[..8]}";

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId1,
            Name = $"{uniquePrefix}-Suffix1",
            Status = "ACTIVE",
            Value = 1,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId2,
            Name = $"{uniquePrefix}-Suffix2",
            Status = "ACTIVE",
            Value = 2,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId3,
            Name = $"Other-{uniquePrefix}",
            Status = "ACTIVE",
            Value = 3,
            LastUpdated = DateTime.UtcNow
        });

        // Act
        var filter = Filter<TestComponent>.StartsWith(c => c.Name, uniquePrefix);
        var results = await TestDataContext().Query()
            .WithAll(filter)
            .ToEntityIds();

        // Assert
        results.ShouldContain(entityId1); // Starts with prefix
        results.ShouldContain(entityId2); // Starts with prefix
        results.ShouldNotContain(entityId3); // Prefix is in middle, not at start
    }

    /// <summary>
    /// INTENT: Verify EndsWith operator finds suffix matches.
    /// PURPOSE: File extension filtering, domain matching.
    /// BUSINESS CONTEXT: "Find all PDF documents" (name ends with .pdf).
    /// WHY IMPORTANT: EndsWith completes string matching operations.
    /// ARCHITECTURAL SIGNIFICANCE: Tests EndsWith filter which uses LIKE '%value'.
    /// </summary>
    [Fact]
    public async Task Filter_EndsWith_ShouldFindSuffixMatches()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var uniqueSuffix = $"SUFFIX{Guid.NewGuid().ToString("N")[..8]}";

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId1,
            Name = $"Prefix1-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 1,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId2,
            Name = $"Prefix2-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 2,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId3,
            Name = $"{uniqueSuffix}-Other",
            Status = "ACTIVE",
            Value = 3,
            LastUpdated = DateTime.UtcNow
        });

        // Act
        var filter = Filter<TestComponent>.EndsWith(c => c.Name, uniqueSuffix);
        var results = await TestDataContext().Query()
            .WithAll(filter)
            .ToEntityIds();

        // Assert
        results.ShouldContain(entityId1); // Ends with suffix
        results.ShouldContain(entityId2); // Ends with suffix
        results.ShouldNotContain(entityId3); // Suffix is at start, not end
    }

    /// <summary>
    /// INTENT: Verify case-insensitive Contains works correctly.
    /// PURPOSE: Search should be case-insensitive for user convenience.
    /// BUSINESS CONTEXT: User searches "SMITH" and finds "Smith", "smith", "SMITH".
    /// WHY IMPORTANT: Case-insensitive search improves UX.
    /// ARCHITECTURAL SIGNIFICANCE: Tests IgnoreCase() with Contains (ILIKE).
    /// </summary>
    [Fact]
    public async Task Filter_Contains_IgnoreCase_ShouldFindCaseInsensitiveMatches()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var uniqueMarker = Guid.NewGuid().ToString("N")[..8];

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId1,
            Name = $"UPPERCASE-{uniqueMarker}",
            Status = "ACTIVE",
            Value = 1,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId2,
            Name = $"lowercase-{uniqueMarker}",
            Status = "ACTIVE",
            Value = 2,
            LastUpdated = DateTime.UtcNow
        });

        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId3,
            Name = $"MixedCase-{uniqueMarker}",
            Status = "ACTIVE",
            Value = 3,
            LastUpdated = DateTime.UtcNow
        });

        // Act - Search for "case" with IgnoreCase
        var filter = Filter<TestComponent>.Contains(c => c.Name, "CASE").IgnoreCase()
            .And(Filter<TestComponent>.Contains(c => c.Name, uniqueMarker));
        var results = await TestDataContext().Query()
            .WithAll(filter)
            .ToEntityIds();

        // Assert - Should find all records containing "case" regardless of casing
        results.ShouldContain(entityId1); // UPPERCASE contains CASE
        results.ShouldContain(entityId2); // lowercase contains case
        results.ShouldContain(entityId3); // MixedCase contains Case
    }

    /// <summary>
    /// INTENT: Verify complex filter with multiple And conditions.
    /// PURPOSE: Real-world queries often have 3+ conditions.
    /// BUSINESS CONTEXT: "Find active premium accounts in California with balance > 1000".
    /// WHY IMPORTANT: Ensures filter chaining scales correctly.
    /// ARCHITECTURAL SIGNIFICANCE: Tests multiple And() operations chained together.
    /// </summary>
    [Fact]
    public async Task Filter_MultipleAndConditions_ShouldReturnCorrectIntersection()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);
        TrackEntity(entityId3);

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var targetStatus = $"TARGET-{uniqueSuffix}";

        // Entity 1: Matches all conditions
        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId1,
            Name = $"Match-All-{uniqueSuffix}",
            Status = targetStatus,
            Value = 150,
            IsActive = true,
            LastUpdated = DateTime.UtcNow
        });

        // Entity 2: Fails IsActive condition
        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId2,
            Name = $"Fail-Active-{uniqueSuffix}",
            Status = targetStatus,
            Value = 150,
            IsActive = false,
            LastUpdated = DateTime.UtcNow
        });

        // Entity 3: Fails Value condition
        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId3,
            Name = $"Fail-Value-{uniqueSuffix}",
            Status = targetStatus,
            Value = 50,
            IsActive = true,
            LastUpdated = DateTime.UtcNow
        });

        // Act - Query with 4 conditions: Contains name, Status = target, Value > 100, IsActive = true
        var filter = Filter<TestComponent>.Contains(c => c.Name, uniqueSuffix)
            .And(Filter<TestComponent>.Eq(c => c.Status, targetStatus))
            .And(Filter<TestComponent>.Gt(c => c.Value, 100))
            .And(Filter<TestComponent>.Eq(c => c.IsActive, true));
        var results = await TestDataContext().Query()
            .WithAll(filter)
            .ToEntityIds();

        // Assert - Only entity1 matches all 4 conditions
        results.ShouldContain(entityId1);
        results.ShouldNotContain(entityId2); // IsActive = false
        results.ShouldNotContain(entityId3); // Value = 50 (not > 100)
    }

    /// <summary>
    /// INTENT: Verify filter handles special characters in LIKE patterns.
    /// PURPOSE: User input may contain SQL wildcards (%, _) that need escaping.
    /// BUSINESS CONTEXT: Search for "50% off" should find literal "50% off", not match everything.
    /// WHY IMPORTANT: Without escaping, % and _ become wildcards causing incorrect results.
    /// ARCHITECTURAL SIGNIFICANCE: Tests EscapeLikePattern function.
    /// </summary>
    [Fact]
    public async Task Filter_Contains_ShouldEscapeSpecialCharacters()
    {
        // Arrange
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        TrackEntity(entityId1);
        TrackEntity(entityId2);

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];

        // Create a record with literal % in the name
        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId1,
            Name = $"50% off sale-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 1,
            LastUpdated = DateTime.UtcNow
        });

        // Create a record without %
        await TestDataContext().Commit(new TestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId2,
            Name = $"Regular sale-{uniqueSuffix}",
            Status = "ACTIVE",
            Value = 2,
            LastUpdated = DateTime.UtcNow
        });

        // Act - Search for literal "50% off"
        var filter = Filter<TestComponent>.Contains(c => c.Name, "50% off")
            .And(Filter<TestComponent>.Contains(c => c.Name, uniqueSuffix));
        var results = await TestDataContext().Query()
            .WithAll(filter)
            .ToEntityIds();

        // Assert - Should only find the record with literal "50% off"
        results.ShouldContain(entityId1);    // Has "50% off"
        results.ShouldNotContain(entityId2); // Does not have "50% off"
    }
}
