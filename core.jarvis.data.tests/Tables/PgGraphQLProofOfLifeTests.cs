using Dapper;
using Newtonsoft.Json.Linq;
using Npgsql;
using Shouldly;

namespace core.jarvis.data.tests.Tables
{
    /// <summary>
    /// Tests to verify that pg_graphql extension is available and working
    /// in the PostgreSQL Docker instance
    /// </summary>
    public class PgGraphQLProofOfLifeTests : IDisposable
    {
        private readonly NpgsqlConnection _conn;
        private readonly string _connectionString;

        public PgGraphQLProofOfLifeTests()
        {
            // Use supabase_admin credentials for GraphQL tests since graphql schema is owned by supabase_admin
            _connectionString = TestHelpers.GetSupabaseAdminConnectionString();
            _conn = new NpgsqlConnection(_connectionString);
        }

        [Fact]
        public async Task PgGraphQL_ExtensionIsInstalled()
        {
            // Arrange
            await _conn.OpenAsync();
            
            // Act - Check pg_graphql version
            var version = await _conn.ExecuteScalarAsync<string>(@"
                SELECT extversion 
                FROM pg_extension 
                WHERE extname = 'pg_graphql'
            ");

            // Assert
            version.ShouldNotBeNull();
            version.ShouldNotBeEmpty();
            // Version should be something like "1.4.2"
            version.ShouldMatch(@"^\d+\.\d+\.\d+");
        }

        [Fact]
        public async Task PgGraphQL_ResolveFunction_Exists()
        {
            // Arrange
            await _conn.OpenAsync();
            
            // Act - Check if graphql.resolve function exists
            var functionExists = await _conn.ExecuteScalarAsync<bool>(@"
                SELECT EXISTS (
                    SELECT 1 
                    FROM pg_proc p
                    JOIN pg_namespace n ON p.pronamespace = n.oid
                    WHERE n.nspname = 'graphql' 
                    AND p.proname = 'resolve'
                )
            ");

            // Assert
            functionExists.ShouldBe(true);
        }

        [Fact]
        public async Task PgGraphQL_BasicExecution_Works()
        {
            // Arrange
            await _conn.OpenAsync();
            
            // Simple introspection query that should always work
            var graphqlQuery = @"
                {
                    __typename
                }
            ";

            // Act - Execute GraphQL query using pg_graphql's resolve function
            using var cmd = new NpgsqlCommand("SELECT graphql.resolve($1::text)", _conn);
            cmd.Parameters.AddWithValue(graphqlQuery);
            var resultJson = await cmd.ExecuteScalarAsync() as string;

            // Assert
            resultJson.ShouldNotBeNull();
            
            var result = JObject.Parse(resultJson);
            result.ShouldNotBeNull();
            
            // Should have data property
            result["data"].ShouldNotBeNull();
        }

        public void Dispose()
        {
            _conn?.Dispose();
        }
    }
}