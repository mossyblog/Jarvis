using Npgsql;
using Shouldly;

namespace core.jarvis.data.tests.Tables
{
    /// <summary>
    /// INTENT: Verify that PgTable is protected against SQL injection attacks.
    /// PURPOSE: Security validation test suite for SQL injection prevention.
    /// BUSINESS CONTEXT: Ensures data access layer is secure against malicious input.
    /// WHY IMPORTANT: SQL injection is a critical security vulnerability that must be prevented.
    /// ARCHITECTURAL SIGNIFICANCE: Validates security measures in the data access layer.
    /// FUTURE RESILIENCE: Protects against regressions in SQL injection prevention.
    /// </summary>
    public class SqlInjectionProtectionTests : IAsyncLifetime
    {
        private NpgsqlConnection _conn = null!;
        private PgClient _client = null!;

        public async Task InitializeAsync()
        {
            var connString = TestHelpers.GetConnectionStringFromEnv();
            _conn = new NpgsqlConnection(connString);
            _client = await PgClientFactory.Create(_conn);

            // Create a test table for injection tests
            var createTableSql = @"
                DROP TABLE IF EXISTS injection_test_entity;
                CREATE TABLE injection_test_entity (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    name TEXT NOT NULL,
                    value INTEGER NOT NULL,
                    description TEXT
                );";
            
            using (var cmd = new NpgsqlCommand(createTableSql, _conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Insert some test data
            var testData = new InjectionTestEntity 
            { 
                Id = Guid.NewGuid(),
                Name = "Test Entity",
                Value = 100,
                Description = "Normal test data"
            };
            await _client.From<InjectionTestEntity>().Insert(testData);
        }

        public async Task DisposeAsync()
        {
            var dropSql = "DROP TABLE IF EXISTS injection_test_entity;";
            using var cmd = new NpgsqlCommand(dropSql, _conn);
            await cmd.ExecuteNonQueryAsync();
            await _conn.CloseAsync();
        }

        /// <summary>
        /// INTENT: Test that SQL injection via column names is prevented.
        /// PURPOSE: Verify column whitelisting protects against injection.
        /// BUSINESS CONTEXT: Malicious users might try to inject SQL via column parameters.
        /// WHY IMPORTANT: Column names are often overlooked as injection vectors.
        /// ARCHITECTURAL SIGNIFICANCE: Validates column whitelisting implementation.
        /// FUTURE RESILIENCE: Ensures column validation remains effective.
        /// </summary>
        [Fact]
        public async Task Filter_WithSqlInjectionInColumnName_ThrowsArgumentException()
        {
            // Arrange - Various SQL injection attempts via column name
            var injectionAttempts = new[]
            {
                "name; DROP TABLE injection_test_entity; --",
                "name' OR '1'='1",
                "name); DELETE FROM injection_test_entity; --",
                "name UNION SELECT * FROM pg_user --",
                "name; UPDATE injection_test_entity SET value = 999 --",
                "1=1; DROP TABLE injection_test_entity; SELECT * FROM injection_test_entity WHERE name",
                "name' || (SELECT version()) || '",
                "name'; EXEC xp_cmdshell('net user hacker password /add'); --"
            };

            // Act & Assert
            foreach (var maliciousColumn in injectionAttempts)
            {
                var table = _client.From<InjectionTestEntity>();
                
                Should.Throw<ArgumentException>(() => table.Filter(maliciousColumn, "eq", "test"))
                    .Message.ShouldContain($"Column '{maliciousColumn}' is not allowed");
            }

            // Verify table still exists and has data
            var results = await _client.From<InjectionTestEntity>().Get();
            results.Count.ShouldBe(1);
            results[0].Name.ShouldBe("Test Entity");
        }

        /// <summary>
        /// INTENT: Test that SQL injection via operators is prevented.
        /// PURPOSE: Verify operator whitelisting protects against injection.
        /// BUSINESS CONTEXT: Operators are a common SQL injection vector.
        /// WHY IMPORTANT: Operators directly affect SQL query structure.
        /// ARCHITECTURAL SIGNIFICANCE: Validates operator whitelisting implementation.
        /// FUTURE RESILIENCE: Ensures operator validation remains effective.
        /// </summary>
        [Fact]
        public async Task Filter_WithSqlInjectionInOperator_ThrowsArgumentException()
        {
            // Arrange - Various SQL injection attempts via operator
            var injectionAttempts = new[]
            {
                "=; DROP TABLE injection_test_entity; --",
                "= OR 1=1 --",
                "'; DELETE FROM injection_test_entity; --",
                "= 'test' UNION SELECT * FROM pg_user --",
                ">; UPDATE injection_test_entity SET value = 999 --",
                "IN (SELECT * FROM pg_tables) --",
                "LIKE '%' OR '1'='1",
                "= 'test' OR EXISTS(SELECT * FROM pg_user) --"
            };

            // Act & Assert
            foreach (var maliciousOperator in injectionAttempts)
            {
                var table = _client.From<InjectionTestEntity>();
                
                Should.Throw<ArgumentException>(() => table.Filter("name", maliciousOperator, "test"))
                    .Message.ShouldContain($"Operator '{maliciousOperator}' is not allowed");
            }

            // Verify table still exists and has data
            var results = await _client.From<InjectionTestEntity>().Get();
            results.Count.ShouldBe(1);
            results[0].Value.ShouldBe(100);
        }

        /// <summary>
        /// INTENT: Test that SQL injection via filter values is prevented.
        /// PURPOSE: Verify parameterized queries protect against value injection.
        /// BUSINESS CONTEXT: Filter values are the most common injection vector.
        /// WHY IMPORTANT: User input often flows directly into filter values.
        /// ARCHITECTURAL SIGNIFICANCE: Validates parameterized query implementation.
        /// FUTURE RESILIENCE: Ensures value parameterization remains effective.
        /// </summary>
        [Fact]
        public async Task Filter_WithSqlInjectionInValue_SafelyParameterized()
        {
            // Arrange - Various SQL injection attempts via filter value
            var injectionAttempts = new[]
            {
                "'; DROP TABLE injection_test_entity; --",
                "' OR '1'='1",
                "'; DELETE FROM injection_test_entity; --",
                "' UNION SELECT * FROM pg_user --",
                "'; UPDATE injection_test_entity SET value = 999 WHERE '1'='1",
                "Robert'); DROP TABLE injection_test_entity; --",
                "' OR 1=1 --",
                "'; EXEC xp_cmdshell('net user'); --"
            };

            // Act - These should NOT throw exceptions but should safely parameterize the values
            foreach (var maliciousValue in injectionAttempts)
            {
                var results = await _client.From<InjectionTestEntity>()
                    .Filter("name", "eq", maliciousValue)
                    .Get();
                
                // Assert - Should return no results (no matching name) but NOT execute the injection
                results.Count.ShouldBe(0);
            }

            // Verify table still exists and has original data
            var allResults = await _client.From<InjectionTestEntity>().Get();
            allResults.Count.ShouldBe(1);
            allResults[0].Name.ShouldBe("Test Entity");
            allResults[0].Value.ShouldBe(100);
        }

        /// <summary>
        /// INTENT: Test that complex multi-vector SQL injection attempts are prevented.
        /// PURPOSE: Verify defense against sophisticated injection attacks.
        /// BUSINESS CONTEXT: Real attacks often combine multiple injection techniques.
        /// WHY IMPORTANT: Layered defenses must work together effectively.
        /// ARCHITECTURAL SIGNIFICANCE: Validates comprehensive security approach.
        /// FUTURE RESILIENCE: Ensures multiple security layers remain effective.
        /// </summary>
        [Fact]
        public async Task Filter_WithComplexInjectionAttempts_AllPrevented()
        {
            // Test 1: Valid column, invalid operator
            var table1 = _client.From<InjectionTestEntity>();
            Should.Throw<ArgumentException>(() => 
                table1.Filter("name", "= OR 1=1 --", "test"));

            // Test 2: Invalid column, valid operator
            var table2 = _client.From<InjectionTestEntity>();
            Should.Throw<ArgumentException>(() => 
                table2.Filter("name; DROP TABLE injection_test_entity; --", "eq", "test"));

            // Test 3: Valid column and operator, injection in value (should be safe)
            var results = await _client.From<InjectionTestEntity>()
                .Filter("name", "eq", "'; DROP TABLE injection_test_entity; --")
                .Get();
            results.Count.ShouldBe(0); // No match, but injection prevented

            // Verify data integrity
            var finalCheck = await _client.From<InjectionTestEntity>().Get();
            finalCheck.Count.ShouldBe(1);
            finalCheck[0].Description.ShouldBe("Normal test data");
        }

        /// <summary>
        /// INTENT: Test IN operator with SQL injection attempts.
        /// PURPOSE: Verify IN operator is safe against injection.
        /// BUSINESS CONTEXT: IN clauses are complex and prone to injection.
        /// WHY IMPORTANT: IN operator handles multiple values which increases risk.
        /// ARCHITECTURAL SIGNIFICANCE: Validates special operator implementations.
        /// FUTURE RESILIENCE: Ensures IN operator security is maintained.
        /// </summary>
        [Fact]
        public async Task In_WithSqlInjectionAttempts_SafelyParameterized()
        {
            // Test 1: Invalid column name
            var table1 = _client.From<InjectionTestEntity>();
            Should.Throw<ArgumentException>(() => 
                table1.In("name; DROP TABLE injection_test_entity; --", new[] { "test1", "test2" }));

            // Test 2: SQL injection in values (should be safe)
            var maliciousValues = new object[]
            {
                "'; DROP TABLE injection_test_entity; --",
                "' OR '1'='1",
                "'); DELETE FROM injection_test_entity; --"
            };

            var results = await _client.From<InjectionTestEntity>()
                .In("name", maliciousValues)
                .Get();
            
            results.Count.ShouldBe(0); // No matches, but injection prevented

            // Verify data integrity
            var finalCheck = await _client.From<InjectionTestEntity>().Get();
            finalCheck.Count.ShouldBe(1);
        }

        /// <summary>
        /// INTENT: Test that valid operations still work correctly.
        /// PURPOSE: Ensure security measures don't break legitimate functionality.
        /// BUSINESS CONTEXT: Security must not impede normal operations.
        /// WHY IMPORTANT: Balance between security and functionality.
        /// ARCHITECTURAL SIGNIFICANCE: Validates security doesn't over-restrict.
        /// FUTURE RESILIENCE: Ensures security remains practical.
        /// </summary>
        [Fact]
        public async Task Filter_WithValidInputs_WorksCorrectly()
        {
            // Arrange
            var testEntity = new InjectionTestEntity
            {
                Id = Guid.NewGuid(),
                Name = "O'Reilly", // Name with apostrophe
                Value = 200,
                Description = "Test with special chars: '; -- /* */"
            };
            await _client.From<InjectionTestEntity>().Insert(testEntity);

            // Act - Valid operations should work
            var results1 = await _client.From<InjectionTestEntity>()
                .Filter("name", "eq", "O'Reilly")
                .Get();

            var results2 = await _client.From<InjectionTestEntity>()
                .Filter("value", "gt", 150)
                .Get();

            var results3 = await _client.From<InjectionTestEntity>()
                .In("name", new[] { "O'Reilly", "Test Entity" })
                .Get();

            // Assert
            results1.Count.ShouldBe(1);
            results1[0].Name.ShouldBe("O'Reilly");

            results2.Count.ShouldBe(1);
            results2[0].Value.ShouldBe(200);

            results3.Count.ShouldBe(2);
        }
    }

    /// <summary>
    /// Test entity for SQL injection tests with UUID primary key
    /// </summary>
    public class InjectionTestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public int Value { get; set; }
        public string? Description { get; set; }
    }
}