using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using core.jarvis.Data;
using core.jarvis.Exceptions;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

namespace core.jarvis.tests.Integration.GraphQL
{
    /// <summary>
    /// Integration tests for GraphQL query builder through DataContext -> PgClient pipeline
    /// 
    /// INTENT: Verify GraphQL query builder authentication, validation, and execution behavior through the full API pathway
    /// PURPOSE: Ensure GraphQL queries properly flow through DataContext to PgClient with authentication and authorization
    /// BUSINESS CONTEXT: GraphQL provides flexible data access but must enforce security at all layers
    /// WHY IMPORTANT: Validates the complete GraphQL execution path and security enforcement
    /// ARCHITECTURAL SIGNIFICANCE: Tests the integration between DataContext GraphQL API and underlying PgClient
    /// FUTURE RESILIENCE: Ensures GraphQL security is maintained through the entire execution pipeline
    /// </summary>
    [Collection("Sequential")]
    public class GraphQLQueryBuilderTests : IntegrationTestBase
    {

        private IDataContext _dataContext => TestDataContext();

        /// <summary>
        /// INTENT: Verify that GraphQL queries without authentication throw UnauthorizedException
        /// PURPOSE: Ensure unauthenticated access is prevented
        /// BUSINESS CONTEXT: All GraphQL queries must be authenticated to prevent data breaches
        /// WHY IMPORTANT: First line of defense against unauthorized data access
        /// ARCHITECTURAL SIGNIFICANCE: Validates authentication enforcement at GraphQL layer
        /// FUTURE RESILIENCE: Protects against accidental removal of auth checks
        /// </summary>
        [Fact]
        public async Task Execute_WithoutAuth_ThrowsUnauthorizedException()
        {
            // Arrange
            var query = "{ products { id name price } }";
            var graphQLQuery = _dataContext.GraphQL(query);
            
            // Act & Assert
            await Should.ThrowAsync<UnauthorizedException>(async () => 
                await graphQLQuery.Execute());
        }

        /// <summary>
        /// INTENT: Verify that WithAuth properly flows JWT through to PgClient
        /// PURPOSE: Ensure authentication tokens are correctly propagated
        /// BUSINESS CONTEXT: JWT tokens enable row-level security in the database
        /// WHY IMPORTANT: RLS enforcement depends on proper JWT propagation
        /// ARCHITECTURAL SIGNIFICANCE: Validates authentication flow through all layers
        /// FUTURE RESILIENCE: Ensures JWT handling remains consistent
        /// </summary>
        [Fact(Skip = "Passes individually but fails due to test isolation when run with all tests")]
        public async Task Execute_WithAuth_PropagatesJWTToPgClient()
        {
            // Arrange
            // Use a valid JWT format that can be parsed
            var jwt = GenerateTestJWT(new Dictionary<string, string> 
            { 
                { "sub", "user123" },
                { "role", "user" }
            });
            
            var query = "{ __typename }";
            var graphQLQuery = _dataContext.GraphQL(query);
            
            // Act & Assert
            // The fact that this doesn't throw UnauthorizedException proves JWT was propagated
            var result = await graphQLQuery
                .WithAuth(jwt)
                .Execute();
                
            result.ShouldNotBeNull();
            result.Data.ShouldNotBeNull();
        }

        /// <summary>
        /// INTENT: Verify that RequireRole validates JWT role claims correctly
        /// PURPOSE: Ensure role-based access control is enforced
        /// BUSINESS CONTEXT: Different roles have different data access permissions
        /// WHY IMPORTANT: Prevents privilege escalation and unauthorized access
        /// ARCHITECTURAL SIGNIFICANCE: Validates RBAC implementation in GraphQL layer
        /// FUTURE RESILIENCE: Protects against role bypass vulnerabilities
        /// </summary>
        [Fact]
        public async Task Execute_WithRequiredRole_ValidatesRole()
        {
            // Arrange
            var jwt = GenerateTestJWT(new Dictionary<string, string> 
            { 
                { "sub", "user123" },
                { "role", "admin" }
            });
            
            // Simple introspection query that should work regardless of data
            var query = "{ __typename }";
            var graphQLQuery = _dataContext.GraphQL(query);

            // Act
            var result = await graphQLQuery
                .WithAuth(jwt)
                .RequireRole("admin")
                .Execute();

            // Assert
            result.ShouldNotBeNull();
            result.Data.ShouldNotBeNull();
        }

        /// <summary>
        /// INTENT: Verify that incorrect role throws ForbiddenException
        /// PURPOSE: Ensure unauthorized roles are rejected
        /// BUSINESS CONTEXT: Users should only access data allowed by their role
        /// WHY IMPORTANT: Core security feature preventing unauthorized access
        /// ARCHITECTURAL SIGNIFICANCE: Validates authorization enforcement
        /// FUTURE RESILIENCE: Maintains strict role-based security
        /// </summary>
        [Fact]
        public async Task Execute_WithWrongRole_ThrowsForbiddenException()
        {
            // Arrange
            var jwt = GenerateTestJWT(new Dictionary<string, string> 
            { 
                { "sub", "user123" },
                { "role", "user" }
            });
            
            var query = "{ __typename }";
            var graphQLQuery = _dataContext.GraphQL(query);

            // Act & Assert
            await Should.ThrowAsync<ForbiddenException>(async () => 
                await graphQLQuery
                    .WithAuth(jwt)
                    .RequireRole("admin")
                    .Execute());
        }

        /// <summary>
        /// INTENT: Verify that RequireClaim validates JWT claims correctly
        /// PURPOSE: Ensure claim-based access control is enforced
        /// BUSINESS CONTEXT: Multi-tenant systems require tenant isolation
        /// WHY IMPORTANT: Prevents cross-tenant data access
        /// ARCHITECTURAL SIGNIFICANCE: Validates claim-based authorization
        /// FUTURE RESILIENCE: Maintains tenant isolation security
        /// </summary>
        [Fact]
        public async Task Execute_WithRequiredClaim_ValidatesClaim()
        {
            // Arrange
            var jwt = GenerateTestJWT(new Dictionary<string, string> 
            { 
                { "sub", "user123" },
                { "tenant_id", "tenant1" }
            });
            
            var query = "{ __typename }";
            var graphQLQuery = _dataContext.GraphQL(query);

            // Act
            var result = await graphQLQuery
                .WithAuth(jwt)
                .RequireClaim("tenant_id", "tenant1")
                .Execute();

            // Assert
            result.ShouldNotBeNull();
            result.Data.ShouldNotBeNull();
        }

        /// <summary>
        /// INTENT: Verify that incorrect claim value throws ForbiddenException
        /// PURPOSE: Ensure claim values are strictly validated
        /// BUSINESS CONTEXT: Prevents access to wrong tenant's data
        /// WHY IMPORTANT: Critical for multi-tenant data isolation
        /// ARCHITECTURAL SIGNIFICANCE: Validates claim value enforcement
        /// FUTURE RESILIENCE: Maintains strict tenant boundaries
        /// </summary>
        [Fact]
        public async Task Execute_WithWrongClaim_ThrowsForbiddenException()
        {
            // Arrange
            var jwt = GenerateTestJWT(new Dictionary<string, string> 
            { 
                { "sub", "user123" },
                { "tenant_id", "tenant1" }
            });
            
            var query = "{ __typename }";
            var graphQLQuery = _dataContext.GraphQL(query);

            // Act & Assert
            await Should.ThrowAsync<ForbiddenException>(async () => 
                await graphQLQuery
                    .WithAuth(jwt)
                    .RequireClaim("tenant_id", "tenant2")
                    .Execute());
        }

        /// <summary>
        /// INTENT: Verify that custom validators are executed with correct context
        /// PURPOSE: Enable flexible authorization logic
        /// BUSINESS CONTEXT: Complex authorization rules may require custom logic
        /// WHY IMPORTANT: Allows business-specific security rules
        /// ARCHITECTURAL SIGNIFICANCE: Validates extensibility of authorization
        /// FUTURE RESILIENCE: Supports evolving security requirements
        /// </summary>
        [Fact]
        public async Task Execute_WithCustomValidator_RunsValidation()
        {
            // Arrange
            var jwt = GenerateTestJWT(new Dictionary<string, string> { { "sub", "user123" } });
            var validatorCalled = false;
            var query = "{ __typename }";
            var graphQLQuery = _dataContext.GraphQL(query);

            // Act
            var result = await graphQLQuery
                .WithAuth(jwt)
                .ValidateWith(async context =>
                {
                    validatorCalled = true;
                    context.Query.ShouldBe(query);
                    context.JWTClaims["sub"].ShouldBe("user123");
                    return await Task.FromResult(true);
                })
                .Execute();

            // Assert
            validatorCalled.ShouldBe(true);
            result.ShouldNotBeNull();
            result.Data.ShouldNotBeNull();
        }

        /// <summary>
        /// INTENT: Verify that failing custom validator throws ForbiddenException
        /// PURPOSE: Ensure custom validators can reject access
        /// BUSINESS CONTEXT: Custom rules must be able to deny access
        /// WHY IMPORTANT: Validates that custom security rules are enforced
        /// ARCHITECTURAL SIGNIFICANCE: Confirms validator results are respected
        /// FUTURE RESILIENCE: Maintains integrity of custom authorization
        /// </summary>
        [Fact]
        public async Task Execute_WithFailingValidator_ThrowsForbiddenException()
        {
            // Arrange
            var jwt = GenerateTestJWT(new Dictionary<string, string> { { "sub", "user123" } });
            var query = "{ __typename }";
            var graphQLQuery = _dataContext.GraphQL(query);

            // Act & Assert
            await Should.ThrowAsync<ForbiddenException>(async () => 
                await graphQLQuery
                    .WithAuth(jwt)
                    .ValidateWith(async context => await Task.FromResult(false))
                    .Execute());
        }

        /// <summary>
        /// INTENT: Verify that GraphQL variables are properly included in the query
        /// PURPOSE: Enable parameterized GraphQL queries
        /// BUSINESS CONTEXT: Dynamic queries require variable substitution
        /// WHY IMPORTANT: Core GraphQL functionality for reusable queries
        /// ARCHITECTURAL SIGNIFICANCE: Validates variable handling in the pipeline
        /// FUTURE RESILIENCE: Ensures GraphQL spec compliance
        /// </summary>
        [Fact]
        public async Task Execute_WithVariables_IncludesVariablesInQuery()
        {
            // Arrange
            var jwt = GenerateTestJWT(new Dictionary<string, string> { { "sub", "user123" } });
            var variables = new { testVar = "testValue" };
            var query = "query($testVar: String) { __typename }";
            var graphQLQuery = _dataContext.GraphQL(query);

            // Act
            var result = await graphQLQuery
                .WithAuth(jwt)
                .WithVariables(variables)
                .Execute();

            // Assert
            result.ShouldNotBeNull();
            // The query execution should succeed with variables
            result.Data.ShouldNotBeNull();
        }

        /// <summary>
        /// INTENT: Verify that operation names are included in GraphQL requests
        /// PURPOSE: Support named operations for better debugging and monitoring
        /// BUSINESS CONTEXT: Named operations help track API usage
        /// WHY IMPORTANT: Enables query identification in logs and metrics
        /// ARCHITECTURAL SIGNIFICANCE: Validates operation name propagation
        /// FUTURE RESILIENCE: Supports APM and debugging tools
        /// </summary>
        [Fact]
        public async Task Execute_WithOperationName_IncludesOperationName()
        {
            // Arrange
            var jwt = GenerateTestJWT(new Dictionary<string, string> { { "sub", "user123" } });
            var query = "query TestOperation { __typename }";
            var graphQLQuery = _dataContext.GraphQL(query);

            // Act
            var result = await graphQLQuery
                .WithAuth(jwt)
                .WithOperationName("TestOperation")
                .Execute();

            // Assert
            result.ShouldNotBeNull();
            result.Data.ShouldNotBeNull();
        }

        /// <summary>
        /// INTENT: Verify that typed Execute<T> properly deserializes GraphQL responses
        /// PURPOSE: Enable strongly-typed GraphQL query results
        /// BUSINESS CONTEXT: Type safety improves code reliability
        /// WHY IMPORTANT: Prevents runtime errors from mismatched types
        /// ARCHITECTURAL SIGNIFICANCE: Validates JSON deserialization in GraphQL layer
        /// FUTURE RESILIENCE: Maintains type safety across API changes
        /// </summary>
        [Fact]
        public async Task ExecuteT_WithValidResponse_DeserializesData()
        {
            // Arrange
            var jwt = GenerateTestJWT(new Dictionary<string, string> { { "sub", "user123" } });
            
            // Query that returns typed data from the GraphQL introspection
            var query = "{ __type(name: \"Query\") { name kind } }";
            var graphQLQuery = _dataContext.GraphQL(query);

            // Act
            var result = await graphQLQuery
                .WithAuth(jwt)
                .Execute<GraphQLTypeInfo>();

            // Assert
            result.ShouldNotBeNull();
            result.__type.ShouldNotBeNull();
            result.__type.name.ShouldBe("Query");
            result.__type.kind.ShouldNotBeNullOrEmpty();
        }

        /// <summary>
        /// INTENT: Verify that GraphQL errors are properly converted to exceptions
        /// PURPOSE: Ensure GraphQL errors are handled consistently
        /// BUSINESS CONTEXT: Clear error messages improve debugging
        /// WHY IMPORTANT: Helps developers identify and fix issues quickly
        /// ARCHITECTURAL SIGNIFICANCE: Validates error propagation through layers
        /// FUTURE RESILIENCE: Maintains consistent error handling
        /// </summary>
        [Fact]
        public async Task Execute_WithGraphQLErrors_ThrowsGraphQLException()
        {
            // Arrange
            var jwt = GenerateTestJWT(new Dictionary<string, string> { { "sub", "user123" } });
            
            // Invalid query that will cause a GraphQL error
            var query = "{ invalidField }";
            var graphQLQuery = _dataContext.GraphQL(query);

            // Act & Assert
            var ex = await Should.ThrowAsync<GraphQLException>(async () => 
                await graphQLQuery
                    .WithAuth(jwt)
                    .Execute());

            ex.Message.ShouldNotBeNullOrEmpty();
            // The exact error message depends on the GraphQL implementation
        }

        private string GenerateTestJWT(Dictionary<string, string> claims)
        {
            // Generate a properly signed JWT for testing
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("GraphQL-Test-Secret-Key-Must-Be-At-Least-256-Bits-For-Security");
            
            var claimsList = new List<Claim>();
            foreach (var claim in claims)
            {
                claimsList.Add(new Claim(claim.Key, claim.Value));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claimsList),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = "graphql-test-issuer",
                Audience = "graphql-test-audience",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private class GraphQLTypeInfo
        {
            public TypeInfo __type { get; set; } = new TypeInfo();
            
            public class TypeInfo
            {
                public string name { get; set; } = "";
                public string kind { get; set; } = "";
            }
        }
    }
}