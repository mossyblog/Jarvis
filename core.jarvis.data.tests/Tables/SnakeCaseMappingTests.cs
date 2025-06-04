using Npgsql;
using Shouldly;

namespace core.jarvis.data.tests.Tables
{
    /// <summary>
    /// INTENT: Validate snake_case mapping for table and property names in PgClient.
    /// PURPOSE: Ensure PgClient correctly maps C# PascalCase to PostgreSQL snake_case.
    /// BUSINESS CONTEXT: Enables convention-based mapping without attributes.
    /// WHY IMPORTANT: Reduces boilerplate and enforces naming consistency.
    /// ARCHITECTURAL SIGNIFICANCE: Ensures all entities work with snake_case tables/columns.
    /// FUTURE RESILIENCE: Protects against regressions in mapping logic.
    /// </summary>
    public class SnakeCaseMappingTests : IAsyncLifetime
    {
        private NpgsqlConnection _conn = null!;
        private PgClient _client = null!;

        public async Task InitializeAsync()
        {
            var connString = TestHelpers.GetConnectionStringFromEnv();
            _conn = new NpgsqlConnection(connString);
            _client = await PgClientFactory.Create(_conn);

            // Clean up and recreate snake_case_entity table for isolation, reset identity
            var cleanupSql = @"
                DROP TABLE IF EXISTS snake_case_entity;
                CREATE TABLE snake_case_entity (
                    id SERIAL PRIMARY KEY,
                    first_name TEXT NOT NULL,
                    last_name TEXT NOT NULL,
                    created_at TIMESTAMP NOT NULL DEFAULT NOW()
                );";
            using (var cmd = new NpgsqlCommand(cleanupSql, _conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Ensure sequence is reset for SERIAL PK
            var resetSeqSql = "ALTER SEQUENCE IF EXISTS snake_case_entity_id_seq RESTART WITH 1;";
            using (var cmd = new NpgsqlCommand(resetSeqSql, _conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task DisposeAsync()
        {
            // Cleanup: drop the test table
            var dropSql = "DROP TABLE IF EXISTS snake_case_entity;";
            using var cmd = new NpgsqlCommand(dropSql, _conn);
            await cmd.ExecuteNonQueryAsync();
            await _conn.CloseAsync();
        }

        /// <summary>
        /// INTENT: Verify that inserting and retrieving a SnakeCaseEntity works with snake_case mapping.
        /// PURPOSE: Test basic insert and select with convention mapping.
        /// BUSINESS CONTEXT: Ensures entities can be persisted and queried without attributes.
        /// WHY IMPORTANT: Validates core mapping logic.
        /// ARCHITECTURAL SIGNIFICANCE: Demonstrates convention-based mapping.
        /// FUTURE RESILIENCE: Protects against mapping regressions.
        /// </summary>
        [Fact]
        public async Task InsertAndGet_SnakeCaseMapping_Works()
        {
            // Arrange
            var entity = new SnakeCaseEntity
            {
                FirstName = "John_" + Guid.NewGuid().ToString("N"),
                LastName = "Doe_" + Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow
            };

            // Act
            await _client.From<SnakeCaseEntity>().Insert(entity);
            var results = await _client.From<SnakeCaseEntity>().Filter("first_name", "eq", entity.FirstName).Get();

            // Assert
            results.ShouldNotBeEmpty();
            results.ShouldContain(e => e.FirstName == entity.FirstName && e.LastName == entity.LastName);
        }

        /// <summary>
        /// INTENT: Validate filtering on a snake_case column.
        /// PURPOSE: Ensure Filter works with snake_case property names.
        /// BUSINESS CONTEXT: Enables querying by convention-mapped columns.
        /// WHY IMPORTANT: Ensures filters are mapped correctly.
        /// ARCHITECTURAL SIGNIFICANCE: Supports flexible querying.
        /// FUTURE RESILIENCE: Prevents regressions in filter mapping.
        /// </summary>
        [Fact]
        public async Task FilterOnSnakeCaseColumn_Works()
        {
            // Arrange
            var uniqueName = "Jane_" + Guid.NewGuid().ToString("N");
            var entity = new SnakeCaseEntity { FirstName = uniqueName, LastName = "Smith", CreatedAt = DateTime.UtcNow };
            await _client.From<SnakeCaseEntity>().Insert(entity);

            // Act
            var filtered = await _client.From<SnakeCaseEntity>().Filter("first_name", "eq", uniqueName).Get();

            // Assert
            filtered.ShouldNotBeEmpty();
            filtered.ShouldContain(e => e.FirstName == uniqueName);
        }

        /// <summary>
        /// INTENT: Validate inserting multiple SnakeCaseEntity records.
        /// PURPOSE: Ensure batch inserts and retrievals work with mapping.
        /// BUSINESS CONTEXT: Supports bulk operations.
        /// WHY IMPORTANT: Ensures mapping is consistent for multiple entities.
        /// ARCHITECTURAL SIGNIFICANCE: Demonstrates scalability of mapping.
        /// FUTURE RESILIENCE: Prevents regressions in batch operations.
        /// </summary>
        [Fact]
        public async Task Insert_MultipleSnakeCaseEntities_Works()
        {
            // Arrange
            var entities = new[]
            {
                new SnakeCaseEntity { FirstName = "Alice_" + Guid.NewGuid().ToString("N"), LastName = "Wonder", CreatedAt = DateTime.UtcNow },
                new SnakeCaseEntity { FirstName = "Bob_" + Guid.NewGuid().ToString("N"), LastName = "Builder", CreatedAt = DateTime.UtcNow }
            };

            // Act
            foreach (var entity in entities)
                await _client.From<SnakeCaseEntity>().Insert(entity);

            var results = await _client.From<SnakeCaseEntity>().Get();

            // Assert
            foreach (var entity in entities)
                results.ShouldContain(e => e.FirstName == entity.FirstName);
        }

        /// <summary>
        /// INTENT: Validate that filtering with no results returns an empty set.
        /// PURPOSE: Ensure negative queries work with mapping.
        /// BUSINESS CONTEXT: Supports robust querying and error handling.
        /// WHY IMPORTANT: Ensures no false positives in mapping.
        /// ARCHITECTURAL SIGNIFICANCE: Demonstrates correctness of filter logic.
        /// FUTURE RESILIENCE: Prevents regressions in negative query handling.
        /// </summary>
        [Fact]
        public async Task FilterWithNoResults_SnakeCaseEntity()
        {
            // Arrange
            var entity = new SnakeCaseEntity { FirstName = "Ghost_" + Guid.NewGuid().ToString("N"), LastName = "User", CreatedAt = DateTime.UtcNow };
            await _client.From<SnakeCaseEntity>().Insert(entity);

            // Act
            var filtered = await _client.From<SnakeCaseEntity>().Filter("first_name", "eq", "DoesNotExist_" + Guid.NewGuid().ToString("N")).Get();

            // Assert
            filtered.ShouldBeEmpty();
        }
    }
}