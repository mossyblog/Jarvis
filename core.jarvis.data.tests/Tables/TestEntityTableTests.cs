using System.Reflection;
using Npgsql;
using Shouldly;

namespace core.jarvis.data.tests.Tables;

/// <summary>
/// INTENT: Validate PgClient and PgTable basic CRUD operations for TestEntity.
/// PURPOSE: Ensure PgClient can insert and query data in the test_entity table.
/// BUSINESS CONTEXT: Supports all features that depend on PgClient for data access.
/// WHY IMPORTANT: Verifies correctness of the core data access abstraction.
/// ARCHITECTURAL SIGNIFICANCE: PgClient is the foundation for future data context and orchestration.
/// FUTURE RESILIENCE: Protects against regressions in low-level data access.
/// </summary>
public class TestEntityTableTests : IAsyncLifetime
{
    private NpgsqlConnection _conn = null!;
    private PgClient _client = null!;

    public async Task InitializeAsync()
    {
        var connString = TestHelpers.GetConnectionStringFromEnv();
        _conn = new NpgsqlConnection(connString);
        _client = await PgClientFactory.Create(_conn);

        // Clean up and recreate test_entity table for isolation, reset identity
        var cleanupSql = @"
            DROP TABLE IF EXISTS test_entity;
            CREATE TABLE test_entity (
                id SERIAL PRIMARY KEY,
                name TEXT NOT NULL
            );";
        using (var cmd = new NpgsqlCommand(cleanupSql, _conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Ensure sequence is reset for SERIAL PK
        var resetSeqSql = "ALTER SEQUENCE IF EXISTS test_entity_id_seq RESTART WITH 1;";
        using (var cmd = new NpgsqlCommand(resetSeqSql, _conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task DisposeAsync()
    {
        // Cleanup: drop the test table
        var dropSql = "DROP TABLE IF EXISTS test_entity;";
        using var cmd = new NpgsqlCommand(dropSql, _conn);
        await cmd.ExecuteNonQueryAsync();
        await _conn.CloseAsync();
    }

    [Fact]
    public async Task InsertAndGet_Works()
    {
        // Arrange
        var entity = new TestEntity { Name = "hello_" + Guid.NewGuid().ToString("N") };

        // Act
        await _client.From<TestEntity>().Insert(entity);
        var results = await _client.From<TestEntity>().Filter("name", "eq", entity.Name).Get();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldContain(e => e.Name == entity.Name);
    }

    [Fact]
    public async Task Filter_Works()
    {
        // Arrange
        var uniqueName = "filterme_" + Guid.NewGuid().ToString("N");
        var entity = new TestEntity { Name = uniqueName };
        await _client.From<TestEntity>().Insert(entity);

        // Act
        var filtered = await _client.From<TestEntity>().Filter("name", "eq", uniqueName).Get();

        // Assert
        filtered.Count.ShouldBe(1);
        filtered[0].Name.ShouldBe(uniqueName);
    }

    [Fact]
    public async Task MultipleInsertsAndGetAll_Works()
    {
        // Arrange
        var entities = new[]
        {
            new TestEntity { Name = "alpha_" + Guid.NewGuid().ToString("N") },
            new TestEntity { Name = "beta_" + Guid.NewGuid().ToString("N") },
            new TestEntity { Name = "gamma_" + Guid.NewGuid().ToString("N") }
        };

        // Act
        foreach (var entity in entities)
            await _client.From<TestEntity>().Insert(entity);

        var results = await _client.From<TestEntity>().Get();

        // Assert
        foreach (var entity in entities)
            results.ShouldContain(e => e.Name == entity.Name);
    }

    [Fact]
    public async Task FilterWithNoResults_ReturnsEmpty()
    {
        // Arrange
        var entity = new TestEntity { Name = "unique_name_" + Guid.NewGuid().ToString("N") };
        await _client.From<TestEntity>().Insert(entity);

        // Act
        var filtered = await _client.From<TestEntity>().Filter("name", "eq", "does_not_exist_" + Guid.NewGuid().ToString("N")).Get();

        // Assert
        filtered.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("eq", "=")]
    [InlineData("neq", "<>")]
    [InlineData("lt", "<")]
    [InlineData("lte", "<=")]
    [InlineData("gt", ">")]
    [InlineData("gte", ">=")]
    public void TranslateOperator_KnownOperators_ReturnsExpected(string op, string expected)
    {
        // Arrange
        var table = new PrivatePgTable();

        // Act
        var result = table.CallTranslateOperator(op);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void TranslateOperator_UnknownOperator_Throws()
    {
        // Arrange
        var table = new PrivatePgTable();

        // Act & Assert
        var ex = Should.Throw<TargetInvocationException>(() => table.CallTranslateOperator("foobar"));
        ex.InnerException.ShouldBeOfType<ArgumentException>();
        ex.InnerException!.Message.ShouldContain("Unsupported operator");
    }

    // Helper to access private TranslateOperator for coverage
    private class PrivatePgTable : PgTable<TestEntity>
    {
        public PrivatePgTable() : base(null!) { }
        public string CallTranslateOperator(string op) => (string)typeof(PgTable<TestEntity>)
            .GetMethod("TranslateOperator", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(this, new object[] { op })!;
    }
}