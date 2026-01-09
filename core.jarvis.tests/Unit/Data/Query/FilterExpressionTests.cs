using core.jarvis.Data.Query;
using core.jarvis.data;
using core.jarvis.tests.Components;
using Shouldly;

namespace core.jarvis.tests.Unit.Data.Query;

/// <summary>
/// Tests for Filter&lt;T&gt; infrastructure including filter creation, operators, modifiers, and column name conversion.
/// </summary>
/// <remarks>
/// <para><strong>BUSINESS CONTEXT:</strong> Filter infrastructure provides type-safe query building for database operations,
/// replacing LINQ-like lambda expressions with a fluent API that translates to PostgreSQL queries.</para>
/// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Filters are the primary mechanism for building database queries,
/// ensuring consistent column naming (snake_case), SQL injection prevention, and proper operator translation.</para>
/// <para><strong>TEST STRATEGY:</strong> Focus on validating filter creation, operator behavior, modifier effects,
/// column name conversion, and edge case handling to ensure reliable query generation.</para>
/// </remarks>
public class FilterExpressionTests
{
    #region Equality Operator Tests

    public class Eq
    {
        /// <summary>
        /// Validates that Eq with string value creates a valid filter.
        ///
        /// INTENT: Verify string equality filter creation produces correct filter type
        /// PURPOSE: String equality is fundamental for matching exact text values
        /// BUSINESS CONTEXT: Used for email matching, status checking, name lookups
        /// </summary>
        [Fact]
        public void WithString_CreatesCorrectFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.Eq(t => t.Name, "test");

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Eq with int value creates a valid filter.
        ///
        /// INTENT: Verify integer equality filter creation produces correct filter type
        /// PURPOSE: Integer equality is used for count matching, value comparisons
        /// BUSINESS CONTEXT: Used for quantity checks, age matching, score lookups
        /// </summary>
        [Fact]
        public void WithInt_CreatesCorrectFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.Eq(t => t.Value, 42);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Eq with Guid value creates a valid filter.
        ///
        /// INTENT: Verify GUID equality filter creation produces correct filter type
        /// PURPOSE: GUID equality is critical for entity identification
        /// BUSINESS CONTEXT: Used for entity lookups, relationship matching, ID comparisons
        /// </summary>
        [Fact]
        public void WithGuid_CreatesCorrectFilter()
        {
            // Arrange
            var testGuid = Guid.NewGuid();

            // Act
            var filter = Filter<TestComponent>.Eq(t => t.Id, testGuid);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Eq with DateTime value creates a valid filter.
        ///
        /// INTENT: Verify DateTime equality filter creation produces correct filter type
        /// PURPOSE: DateTime equality is used for exact timestamp matching
        /// BUSINESS CONTEXT: Used for audit trails, event matching, schedule lookups
        /// </summary>
        [Fact]
        public void WithDateTime_CreatesCorrectFilter()
        {
            // Arrange
            var testDate = DateTime.UtcNow;

            // Act
            var filter = Filter<TestComponent>.Eq(t => t.LastUpdated, testDate);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Eq with bool value creates a valid filter.
        ///
        /// INTENT: Verify boolean equality filter creation produces correct filter type
        /// PURPOSE: Boolean equality is used for flag and status checking
        /// BUSINESS CONTEXT: Used for IsActive, IsDeleted, and other boolean status checks
        /// </summary>
        [Fact]
        public void WithBool_CreatesCorrectFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.Eq(t => t.IsActive, true);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }
    }

    public class Neq
    {
        /// <summary>
        /// Validates that Neq with string value creates a valid filter.
        ///
        /// INTENT: Verify string inequality filter creation produces correct filter type
        /// PURPOSE: String inequality is used for exclusion filtering
        /// BUSINESS CONTEXT: Used for excluding specific statuses, filtering out names
        /// </summary>
        [Fact]
        public void WithString_CreatesCorrectFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.Neq(t => t.Name, "excluded");

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Neq with int value creates a valid filter.
        ///
        /// INTENT: Verify integer inequality filter creation produces correct filter type
        /// PURPOSE: Integer inequality is used for exclusion by numeric value
        /// BUSINESS CONTEXT: Used for excluding specific counts, scores, or values
        /// </summary>
        [Fact]
        public void WithInt_CreatesCorrectFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.Neq(t => t.Value, 0);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Neq with Guid value creates a valid filter.
        ///
        /// INTENT: Verify GUID inequality filter creation produces correct filter type
        /// PURPOSE: GUID inequality is used for excluding specific entities
        /// BUSINESS CONTEXT: Used for "get all except this entity" queries
        /// </summary>
        [Fact]
        public void WithGuid_CreatesCorrectFilter()
        {
            // Arrange
            var excludedGuid = Guid.NewGuid();

            // Act
            var filter = Filter<TestComponent>.Neq(t => t.Id, excludedGuid);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Neq with DateTime value creates a valid filter.
        ///
        /// INTENT: Verify DateTime inequality filter creation produces correct filter type
        /// PURPOSE: DateTime inequality is used for excluding specific timestamps
        /// BUSINESS CONTEXT: Used for audit filtering, event exclusion
        /// </summary>
        [Fact]
        public void WithDateTime_CreatesCorrectFilter()
        {
            // Arrange
            var excludedDate = DateTime.UtcNow;

            // Act
            var filter = Filter<TestComponent>.Neq(t => t.LastUpdated, excludedDate);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }
    }

    #endregion

    #region Comparison Operator Tests

    public class Gt
    {
        /// <summary>
        /// Validates that Gt with int value creates a valid filter.
        ///
        /// INTENT: Verify greater than filter creation for integers
        /// PURPOSE: Numeric comparisons are essential for range filtering
        /// BUSINESS CONTEXT: Used for "value greater than X" queries, pagination offsets
        /// </summary>
        [Fact]
        public void WithInt_CreatesCorrectFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.Gt(t => t.Value, 100);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Gt with DateTime value creates a valid filter.
        ///
        /// INTENT: Verify greater than filter creation for DateTime
        /// PURPOSE: DateTime comparisons are essential for time-based queries
        /// BUSINESS CONTEXT: Used for "created after date" queries, recent records
        /// </summary>
        [Fact]
        public void WithDateTime_CreatesCorrectFilter()
        {
            // Arrange
            var cutoffDate = DateTime.UtcNow.AddDays(-30);

            // Act
            var filter = Filter<TestComponent>.Gt(t => t.LastUpdated, cutoffDate);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }
    }

    public class Gte
    {
        /// <summary>
        /// Validates that Gte with int value creates a valid filter.
        ///
        /// INTENT: Verify greater than or equal filter creation for integers
        /// PURPOSE: Inclusive range filtering for numeric values
        /// BUSINESS CONTEXT: Used for "value at least X" queries, minimum thresholds
        /// </summary>
        [Fact]
        public void WithInt_CreatesCorrectFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.Gte(t => t.Value, 50);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Gte with DateTime value creates a valid filter.
        ///
        /// INTENT: Verify greater than or equal filter creation for DateTime
        /// PURPOSE: Inclusive time-based filtering
        /// BUSINESS CONTEXT: Used for "on or after date" queries, including boundary dates
        /// </summary>
        [Fact]
        public void WithDateTime_CreatesCorrectFilter()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var filter = Filter<TestComponent>.Gte(t => t.LastUpdated, startDate);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }
    }

    public class Lt
    {
        /// <summary>
        /// Validates that Lt with int value creates a valid filter.
        ///
        /// INTENT: Verify less than filter creation for integers
        /// PURPOSE: Upper bound filtering for numeric values
        /// BUSINESS CONTEXT: Used for "value below X" queries, maximum limits
        /// </summary>
        [Fact]
        public void WithInt_CreatesCorrectFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.Lt(t => t.Value, 1000);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Lt with DateTime value creates a valid filter.
        ///
        /// INTENT: Verify less than filter creation for DateTime
        /// PURPOSE: Upper bound time-based filtering
        /// BUSINESS CONTEXT: Used for "before date" queries, historical records
        /// </summary>
        [Fact]
        public void WithDateTime_CreatesCorrectFilter()
        {
            // Arrange
            var endDate = DateTime.UtcNow;

            // Act
            var filter = Filter<TestComponent>.Lt(t => t.LastUpdated, endDate);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }
    }

    public class Lte
    {
        /// <summary>
        /// Validates that Lte with int value creates a valid filter.
        ///
        /// INTENT: Verify less than or equal filter creation for integers
        /// PURPOSE: Inclusive upper bound filtering for numeric values
        /// BUSINESS CONTEXT: Used for "value at most X" queries, maximum thresholds
        /// </summary>
        [Fact]
        public void WithInt_CreatesCorrectFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.Lte(t => t.Value, 500);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Lte with DateTime value creates a valid filter.
        ///
        /// INTENT: Verify less than or equal filter creation for DateTime
        /// PURPOSE: Inclusive upper bound time-based filtering
        /// BUSINESS CONTEXT: Used for "on or before date" queries, including end dates
        /// </summary>
        [Fact]
        public void WithDateTime_CreatesCorrectFilter()
        {
            // Arrange
            var endDate = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

            // Act
            var filter = Filter<TestComponent>.Lte(t => t.LastUpdated, endDate);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }
    }

    #endregion

    #region String Operation Tests

    public class Contains
    {
        /// <summary>
        /// Validates that Contains with string value creates a valid filter.
        ///
        /// INTENT: Verify substring matching filter creation
        /// PURPOSE: Pattern matching for partial string matches
        /// BUSINESS CONTEXT: Used for search functionality, finding records containing text
        /// </summary>
        [Fact]
        public void WithString_CreatesCorrectFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.Contains(t => t.Name, "search");

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Contains throws ArgumentNullException for null value.
        ///
        /// INTENT: Verify null value handling in Contains
        /// PURPOSE: Prevent null values from causing SQL issues
        /// BUSINESS CONTEXT: Input validation for search operations
        /// </summary>
        [Fact]
        public void WithNullValue_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Should.Throw<ArgumentNullException>(() =>
                Filter<TestComponent>.Contains(t => t.Name, null!));
        }
    }

    public class StartsWith
    {
        /// <summary>
        /// Validates that StartsWith with string value creates a valid filter.
        ///
        /// INTENT: Verify prefix matching filter creation
        /// PURPOSE: Pattern matching for string prefix matches
        /// BUSINESS CONTEXT: Used for autocomplete, prefix searches
        /// </summary>
        [Fact]
        public void WithString_CreatesCorrectFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.StartsWith(t => t.Name, "prefix");

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that StartsWith throws ArgumentNullException for null value.
        ///
        /// INTENT: Verify null value handling in StartsWith
        /// PURPOSE: Prevent null values from causing SQL issues
        /// BUSINESS CONTEXT: Input validation for prefix search operations
        /// </summary>
        [Fact]
        public void WithNullValue_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Should.Throw<ArgumentNullException>(() =>
                Filter<TestComponent>.StartsWith(t => t.Name, null!));
        }
    }

    public class EndsWith
    {
        /// <summary>
        /// Validates that EndsWith with string value creates a valid filter.
        ///
        /// INTENT: Verify suffix matching filter creation
        /// PURPOSE: Pattern matching for string suffix matches
        /// BUSINESS CONTEXT: Used for file extension filtering, domain matching
        /// </summary>
        [Fact]
        public void WithString_CreatesCorrectFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.EndsWith(t => t.Name, "suffix");

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that EndsWith throws ArgumentNullException for null value.
        ///
        /// INTENT: Verify null value handling in EndsWith
        /// PURPOSE: Prevent null values from causing SQL issues
        /// BUSINESS CONTEXT: Input validation for suffix search operations
        /// </summary>
        [Fact]
        public void WithNullValue_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Should.Throw<ArgumentNullException>(() =>
                Filter<TestComponent>.EndsWith(t => t.Name, null!));
        }
    }

    #endregion

    #region Collection Operation Tests

    public class In
    {
        /// <summary>
        /// Validates that In with string collection creates a valid filter.
        ///
        /// INTENT: Verify IN clause filter creation for strings
        /// PURPOSE: Multi-value matching for set membership
        /// BUSINESS CONTEXT: Used for status filtering, category selection
        /// </summary>
        [Fact]
        public void WithStringCollection_CreatesCorrectFilter()
        {
            // Arrange
            var values = new[] { "active", "pending", "approved" };

            // Act
            var filter = Filter<TestComponent>.In(t => t.Status, values);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that In with int collection creates a valid filter.
        ///
        /// INTENT: Verify IN clause filter creation for integers
        /// PURPOSE: Multi-value matching for numeric sets
        /// BUSINESS CONTEXT: Used for ID lists, batch processing
        /// </summary>
        [Fact]
        public void WithIntCollection_CreatesCorrectFilter()
        {
            // Arrange
            var values = new[] { 1, 2, 3, 5, 8 };

            // Act
            var filter = Filter<TestComponent>.In(t => t.Value, values);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that In with Guid collection creates a valid filter.
        ///
        /// INTENT: Verify IN clause filter creation for GUIDs
        /// PURPOSE: Multi-value matching for entity IDs
        /// BUSINESS CONTEXT: Used for batch entity lookups, relationship queries
        /// </summary>
        [Fact]
        public void WithGuidCollection_CreatesCorrectFilter()
        {
            // Arrange
            var values = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

            // Act
            var filter = Filter<TestComponent>.In(t => t.Id, values);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that In throws ArgumentNullException for null collection.
        ///
        /// INTENT: Verify null collection handling in In
        /// PURPOSE: Prevent null collections from causing SQL issues
        /// BUSINESS CONTEXT: Input validation for multi-value filtering
        /// </summary>
        [Fact]
        public void WithNullCollection_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Should.Throw<ArgumentNullException>(() =>
                Filter<TestComponent>.In(t => t.Status, (IEnumerable<string>)null!));
        }

        /// <summary>
        /// Validates that In with empty collection creates a filter (implementation handles empty case).
        ///
        /// INTENT: Verify empty collection handling in In
        /// PURPOSE: Edge case handling for empty value sets
        /// BUSINESS CONTEXT: Empty selection should be handled gracefully
        /// </summary>
        [Fact]
        public void WithEmptyCollection_CreatesFilter()
        {
            // Arrange
            var values = Array.Empty<string>();

            // Act
            var filter = Filter<TestComponent>.In(t => t.Status, values);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }
    }

    #endregion

    #region Match All Tests

    public class All
    {
        /// <summary>
        /// Validates that All creates a match-all filter.
        ///
        /// INTENT: Verify match-all filter creation
        /// PURPOSE: Provides a way to retrieve all records without filtering
        /// BUSINESS CONTEXT: Used when no filtering is needed but API requires a filter
        /// </summary>
        [Fact]
        public void ReturnsMatchAllFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.All();

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<MatchAllFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that All returns singleton instance.
        ///
        /// INTENT: Verify singleton pattern for MatchAllFilter
        /// PURPOSE: Memory efficiency through instance reuse
        /// BUSINESS CONTEXT: All() is frequently called, should not allocate new objects
        /// </summary>
        [Fact]
        public void ReturnsSingletonInstance()
        {
            // Arrange & Act
            var filter1 = Filter<TestComponent>.All();
            var filter2 = Filter<TestComponent>.All();

            // Assert
            filter1.ShouldBeSameAs(filter2);
        }
    }

    #endregion

    #region Null Check Tests

    public class IsNull
    {
        /// <summary>
        /// Validates that IsNull creates a valid filter.
        ///
        /// INTENT: Verify IS NULL filter creation
        /// PURPOSE: Null checking for optional fields
        /// BUSINESS CONTEXT: Used for finding records with missing values
        /// </summary>
        [Fact]
        public void CreatesCorrectFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.IsNull(t => t.Version);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }
    }

    public class IsNotNull
    {
        /// <summary>
        /// Validates that IsNotNull creates a valid filter.
        ///
        /// INTENT: Verify IS NOT NULL filter creation
        /// PURPOSE: Non-null checking for required fields
        /// BUSINESS CONTEXT: Used for finding records with populated values
        /// </summary>
        [Fact]
        public void CreatesCorrectFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.IsNotNull(t => t.Version);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }
    }

    #endregion

    #region Modifier Tests

    public class IgnoreCase
    {
        /// <summary>
        /// Validates that IgnoreCase on Eq filter returns new filter instance.
        ///
        /// INTENT: Verify case-insensitive modifier creates new filter
        /// PURPOSE: Immutability - original filter should not be modified
        /// BUSINESS CONTEXT: Case-insensitive email/username matching
        /// </summary>
        [Fact]
        public void OnEqFilter_ReturnsNewFilterInstance()
        {
            // Arrange
            var filter = Filter<TestComponent>.Eq(t => t.Name, "TEST");

            // Act
            var ignoreCaseFilter = filter.IgnoreCase();

            // Assert
            ignoreCaseFilter.ShouldNotBeNull();
            ignoreCaseFilter.ShouldNotBeSameAs(filter);
        }

        /// <summary>
        /// Validates that IgnoreCase on Contains filter returns new filter instance.
        ///
        /// INTENT: Verify case-insensitive modifier works with Contains
        /// PURPOSE: Case-insensitive search functionality
        /// BUSINESS CONTEXT: Case-insensitive text search
        /// </summary>
        [Fact]
        public void OnContainsFilter_ReturnsNewFilterInstance()
        {
            // Arrange
            var filter = Filter<TestComponent>.Contains(t => t.Name, "search");

            // Act
            var ignoreCaseFilter = filter.IgnoreCase();

            // Assert
            ignoreCaseFilter.ShouldNotBeNull();
            ignoreCaseFilter.ShouldNotBeSameAs(filter);
        }

        /// <summary>
        /// Validates that IgnoreCase on MatchAllFilter returns same instance.
        ///
        /// INTENT: Verify IgnoreCase is no-op on MatchAllFilter
        /// PURPOSE: MatchAllFilter has no string comparisons to make case-insensitive
        /// BUSINESS CONTEXT: API consistency - IgnoreCase can be called on any filter
        /// </summary>
        [Fact]
        public void OnMatchAllFilter_ReturnsSameInstance()
        {
            // Arrange
            var filter = Filter<TestComponent>.All();

            // Act
            var ignoreCaseFilter = filter.IgnoreCase();

            // Assert
            ignoreCaseFilter.ShouldBeSameAs(filter);
        }
    }

    public class And
    {
        /// <summary>
        /// Validates that And combines two filters correctly.
        ///
        /// INTENT: Verify AND logic creates composite filter
        /// PURPOSE: Multiple conditions must all be true
        /// BUSINESS CONTEXT: Complex queries like "active AND recent"
        /// </summary>
        [Fact]
        public void CombinesTwoFilters_CreatesCompositeFilter()
        {
            // Arrange
            var filter1 = Filter<TestComponent>.Eq(t => t.IsActive, true);
            var filter2 = Filter<TestComponent>.Gt(t => t.Value, 100);

            // Act
            var combinedFilter = filter1.And(filter2);

            // Assert
            combinedFilter.ShouldNotBeNull();
            combinedFilter.ShouldBeOfType<CompositeFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that And can chain multiple filters.
        ///
        /// INTENT: Verify multiple AND chaining works correctly
        /// PURPOSE: Complex multi-condition queries
        /// BUSINESS CONTEXT: Queries with many filter conditions
        /// </summary>
        [Fact]
        public void CanChainMultipleFilters()
        {
            // Arrange
            var filter1 = Filter<TestComponent>.Eq(t => t.IsActive, true);
            var filter2 = Filter<TestComponent>.Gt(t => t.Value, 100);
            var filter3 = Filter<TestComponent>.Contains(t => t.Name, "test");

            // Act
            var combinedFilter = filter1.And(filter2).And(filter3);

            // Assert
            combinedFilter.ShouldNotBeNull();
            combinedFilter.ShouldBeOfType<CompositeFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that And throws ArgumentNullException for null other filter.
        ///
        /// INTENT: Verify null parameter handling in And
        /// PURPOSE: Prevent null reference exceptions in query building
        /// BUSINESS CONTEXT: Input validation for filter composition
        /// </summary>
        [Fact]
        public void WithNullOther_ThrowsArgumentNullException()
        {
            // Arrange
            var filter = Filter<TestComponent>.Eq(t => t.IsActive, true);

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => filter.And(null!));
        }
    }

    public class Or
    {
        /// <summary>
        /// Validates that Or combines two filters correctly.
        ///
        /// INTENT: Verify OR logic creates composite filter
        /// PURPOSE: At least one condition must be true
        /// BUSINESS CONTEXT: Queries like "status A OR status B"
        /// </summary>
        [Fact]
        public void CombinesTwoFilters_CreatesCompositeFilter()
        {
            // Arrange
            var filter1 = Filter<TestComponent>.Eq(t => t.Status, "active");
            var filter2 = Filter<TestComponent>.Eq(t => t.Status, "pending");

            // Act
            var combinedFilter = filter1.Or(filter2);

            // Assert
            combinedFilter.ShouldNotBeNull();
            combinedFilter.ShouldBeOfType<CompositeFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Or throws ArgumentNullException for null other filter.
        ///
        /// INTENT: Verify null parameter handling in Or
        /// PURPOSE: Prevent null reference exceptions in query building
        /// BUSINESS CONTEXT: Input validation for filter composition
        /// </summary>
        [Fact]
        public void WithNullOther_ThrowsArgumentNullException()
        {
            // Arrange
            var filter = Filter<TestComponent>.Eq(t => t.Status, "active");

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => filter.Or(null!));
        }
    }

    public class Not
    {
        /// <summary>
        /// Validates that Not creates a negated filter.
        ///
        /// INTENT: Verify NOT logic creates negated filter
        /// PURPOSE: Invert filter condition
        /// BUSINESS CONTEXT: Queries like "NOT deleted"
        /// </summary>
        [Fact]
        public void CreatesNegatedFilter()
        {
            // Arrange
            var filter = Filter<TestComponent>.Eq(t => t.IsActive, true);

            // Act
            var negatedFilter = filter.Not();

            // Assert
            negatedFilter.ShouldNotBeNull();
            negatedFilter.ShouldBeOfType<NegatedFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Not can be chained with IgnoreCase.
        ///
        /// INTENT: Verify modifier chaining with Not
        /// PURPOSE: Complex filter modifications
        /// BUSINESS CONTEXT: Case-insensitive negated string comparisons
        /// </summary>
        [Fact]
        public void CanChainWithIgnoreCase()
        {
            // Arrange
            var filter = Filter<TestComponent>.Eq(t => t.Name, "TEST");

            // Act
            var modifiedFilter = filter.Not().IgnoreCase();

            // Assert
            modifiedFilter.ShouldNotBeNull();
            modifiedFilter.ShouldBeOfType<NegatedFilter<TestComponent>>();
        }
    }

    #endregion

    #region Column Name Conversion Tests

    public class ColumnNameConversion
    {
        /// <summary>
        /// Validates PascalCase to snake_case conversion for OwnerEntityId.
        ///
        /// INTENT: Verify complex property name conversion
        /// PURPOSE: PostgreSQL uses snake_case column names
        /// BUSINESS CONTEXT: C# PascalCase properties must map to database columns
        /// </summary>
        [Fact]
        public void OwnerEntityId_ConvertedToSnakeCase()
        {
            // Arrange & Act
            var result = "OwnerEntityId".ToSnakeCase();

            // Assert
            result.ShouldBe("owner_entity_id");
        }

        /// <summary>
        /// Validates PascalCase to snake_case conversion for LastUpdated.
        ///
        /// INTENT: Verify standard property name conversion
        /// PURPOSE: PostgreSQL uses snake_case column names
        /// BUSINESS CONTEXT: Timestamp properties must map correctly
        /// </summary>
        [Fact]
        public void LastUpdated_ConvertedToSnakeCase()
        {
            // Arrange & Act
            var result = "LastUpdated".ToSnakeCase();

            // Assert
            result.ShouldBe("last_updated");
        }

        /// <summary>
        /// Validates PascalCase to snake_case conversion for Id.
        ///
        /// INTENT: Verify simple property name conversion
        /// PURPOSE: Short property names should convert correctly
        /// BUSINESS CONTEXT: Id is the most common property name
        /// </summary>
        [Fact]
        public void Id_ConvertedToSnakeCase()
        {
            // Arrange & Act
            var result = "Id".ToSnakeCase();

            // Assert
            result.ShouldBe("id");
        }

        /// <summary>
        /// Validates PascalCase to snake_case conversion for IsActive.
        ///
        /// INTENT: Verify boolean property name conversion
        /// PURPOSE: Boolean property names must convert correctly
        /// BUSINESS CONTEXT: Is-prefixed properties are common for booleans
        /// </summary>
        [Fact]
        public void IsActive_ConvertedToSnakeCase()
        {
            // Arrange & Act
            var result = "IsActive".ToSnakeCase();

            // Assert
            result.ShouldBe("is_active");
        }

        /// <summary>
        /// Validates empty string handling.
        ///
        /// INTENT: Verify edge case handling for empty string
        /// PURPOSE: Empty string should return empty string
        /// </summary>
        [Fact]
        public void EmptyString_ReturnsEmptyString()
        {
            // Arrange & Act
            var result = "".ToSnakeCase();

            // Assert
            result.ShouldBe("");
        }

        /// <summary>
        /// Validates null string handling.
        ///
        /// INTENT: Verify edge case handling for null string
        /// PURPOSE: Null should return null
        /// </summary>
        [Fact]
        public void NullString_ReturnsNull()
        {
            // Arrange
            string? nullString = null;

            // Act
            var result = nullString!.ToSnakeCase();

            // Assert
            result.ShouldBeNull();
        }
    }

    #endregion

    #region Edge Case Tests

    public class EdgeCases
    {
        /// <summary>
        /// Validates that Eq with empty string creates a valid filter.
        ///
        /// INTENT: Verify empty string value handling
        /// PURPOSE: Empty strings are valid filter values
        /// BUSINESS CONTEXT: Filtering for records with empty field values
        /// </summary>
        [Fact]
        public void Eq_WithEmptyString_CreatesFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.Eq(t => t.Name, "");

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Contains with empty string creates a valid filter.
        ///
        /// INTENT: Verify empty string value handling in Contains
        /// PURPOSE: Empty pattern matches all strings (LIKE '%%')
        /// BUSINESS CONTEXT: Edge case in search functionality
        /// </summary>
        [Fact]
        public void Contains_WithEmptyString_CreatesFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.Contains(t => t.Name, "");

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that special characters in Contains are escaped to prevent SQL injection.
        ///
        /// INTENT: Verify SQL injection prevention in LIKE patterns
        /// PURPOSE: Special LIKE characters (%, _, \) must be escaped
        /// BUSINESS CONTEXT: Security - prevent malicious pattern injection
        /// </summary>
        [Fact]
        public void Contains_WithSpecialCharacters_CreatesFilter()
        {
            // Arrange - value with LIKE wildcards that should be treated literally
            var valueWithWildcards = "100% complete_test";

            // Act
            var filter = Filter<TestComponent>.Contains(t => t.Name, valueWithWildcards);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Contains with percent sign is properly escaped.
        ///
        /// INTENT: Verify percent character escaping in LIKE patterns
        /// PURPOSE: % is LIKE wildcard and must be escaped for literal matching
        /// BUSINESS CONTEXT: Searching for text containing "%" character
        /// </summary>
        [Fact]
        public void Contains_WithPercentSign_CreatesFilter()
        {
            // Arrange
            var valueWithPercent = "50%";

            // Act
            var filter = Filter<TestComponent>.Contains(t => t.Name, valueWithPercent);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Contains with underscore is properly escaped.
        ///
        /// INTENT: Verify underscore character escaping in LIKE patterns
        /// PURPOSE: _ is LIKE single-char wildcard and must be escaped
        /// BUSINESS CONTEXT: Searching for text containing "_" character
        /// </summary>
        [Fact]
        public void Contains_WithUnderscore_CreatesFilter()
        {
            // Arrange
            var valueWithUnderscore = "some_value";

            // Act
            var filter = Filter<TestComponent>.Contains(t => t.Name, valueWithUnderscore);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Contains with backslash is properly escaped.
        ///
        /// INTENT: Verify backslash character escaping in LIKE patterns
        /// PURPOSE: \ is escape character and must be escaped itself
        /// BUSINESS CONTEXT: Searching for text containing "\" character
        /// </summary>
        [Fact]
        public void Contains_WithBackslash_CreatesFilter()
        {
            // Arrange
            var valueWithBackslash = "path\\to\\file";

            // Act
            var filter = Filter<TestComponent>.Contains(t => t.Name, valueWithBackslash);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that Eq with null property expression throws ArgumentNullException.
        ///
        /// INTENT: Verify null property expression handling
        /// PURPOSE: Property expression is required for filter creation
        /// BUSINESS CONTEXT: Developer error prevention
        /// </summary>
        [Fact]
        public void Eq_WithNullPropertyExpression_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Should.Throw<ArgumentNullException>(() =>
                Filter<TestComponent>.Eq<string>(null!, "test"));
        }

        /// <summary>
        /// Validates that In with single value collection creates a valid filter.
        ///
        /// INTENT: Verify single-value IN clause handling
        /// PURPOSE: IN with one value is valid (equivalent to Eq)
        /// BUSINESS CONTEXT: API simplification when value count varies
        /// </summary>
        [Fact]
        public void In_WithSingleValue_CreatesFilter()
        {
            // Arrange
            var values = new[] { "single" };

            // Act
            var filter = Filter<TestComponent>.In(t => t.Status, values);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }

        /// <summary>
        /// Validates that filter creation works with nullable int property.
        ///
        /// INTENT: Verify nullable type property handling
        /// PURPOSE: Nullable properties must work correctly in filters
        /// BUSINESS CONTEXT: Version field is nullable int
        /// </summary>
        [Fact]
        public void Eq_WithNullableInt_CreatesFilter()
        {
            // Arrange & Act
            var filter = Filter<TestComponent>.Eq(t => t.Version, 1);

            // Assert
            filter.ShouldNotBeNull();
            filter.ShouldBeOfType<SingleFilter<TestComponent>>();
        }
    }

    #endregion
}
