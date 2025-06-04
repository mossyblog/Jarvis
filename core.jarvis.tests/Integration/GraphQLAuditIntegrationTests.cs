using core.jarvis.Data;
using core.jarvis.Data.Components;
using core.jarvis.Exceptions;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace core.jarvis.tests.Integration;

/// <summary>
/// Integration tests for GraphQL operation audit logging.
/// 
/// INTENT: Verify that GraphQL operations produce proper audit trails
/// PURPOSE: Ensure GraphQL security and access patterns are tracked
/// BUSINESS CONTEXT: GraphQL provides flexible data access that must be monitored
/// WHY IMPORTANT: GraphQL queries can access sensitive data and must be audited
/// ARCHITECTURAL SIGNIFICANCE: Validates GraphQL audit integration
/// FUTURE RESILIENCE: Ensures GraphQL audit tracking as API evolves
/// </summary>
public class GraphQLAuditIntegrationTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that GraphQL queries generate audit events.
    /// 
    /// INTENT: Verify GraphQL queries are audited
    /// PURPOSE: Track data access patterns
    /// BUSINESS CONTEXT: Need to know who queries what data
    /// WHY IMPORTANT: Query tracking helps with security and optimization
    /// ARCHITECTURAL SIGNIFICANCE: Validates GraphQL query auditing
    /// FUTURE RESILIENCE: Ensures query tracking remains intact
    /// </summary>
    [Fact]
    public async Task GraphQLQuery_ShouldCreateAuditEvent()
    {
        // Arrange
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0LXVzZXIiLCJlbWFpbCI6InRlc3RAZXhhbXBsZS5jb20iLCJyb2xlIjoidXNlciJ9.test";
        var query = @"
            query GetTestData {
                testcomponent {
                    id
                    name
                    value
                }
            }";

        // Act
        try
        {
            await TestDataContext().GraphQL(query)
                .WithAuth(jwt)
                .Execute();
        }
        catch
        {
            // GraphQL execution might fail, but we're testing audit logging
        }
        
        // Small delay to ensure audit events are committed
        await Task.Delay(500);

        // Assert
        var auditEvents = await GetAuditEventsForEntity(Guid.Empty);
        var graphqlEvent = auditEvents.FirstOrDefault(e => e.EventType == AuditEventTypes.GraphQLQuery);
        
        graphqlEvent.ShouldNotBeNull();
        graphqlEvent.Metadata.ShouldContain("query");
        graphqlEvent.Metadata.ShouldContain("userId");
    }

    /// <summary>
    /// Tests that GraphQL mutations generate audit events.
    /// 
    /// INTENT: Verify GraphQL mutations are audited
    /// PURPOSE: Track data modifications via GraphQL
    /// BUSINESS CONTEXT: Mutations change data and must be tracked
    /// WHY IMPORTANT: Data changes via GraphQL need audit trails
    /// ARCHITECTURAL SIGNIFICANCE: Validates GraphQL mutation auditing
    /// FUTURE RESILIENCE: Ensures mutation tracking remains intact
    /// </summary>
    [Fact]
    public async Task GraphQLMutation_ShouldCreateAuditEvent()
    {
        // Arrange
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0LXVzZXIiLCJlbWFpbCI6InRlc3RAZXhhbXBsZS5jb20iLCJyb2xlIjoidXNlciJ9.test";
        var mutation = @"
            mutation CreateTestData {
                insert_testcomponent(objects: {name: ""Test"", value: 42}) {
                    id
                }
            }";

        // Act
        try
        {
            await TestDataContext().GraphQL(mutation)
                .WithAuth(jwt)
                .Execute();
        }
        catch
        {
            // GraphQL execution might fail, but we're testing audit logging
        }
        
        // Small delay to ensure audit events are committed
        await Task.Delay(500);

        // Assert
        var auditEvents = await GetAuditEventsForEntity(Guid.Empty);
        var mutationEvent = auditEvents.FirstOrDefault(e => e.EventType == AuditEventTypes.GraphQLMutation);
        
        mutationEvent.ShouldNotBeNull();
        mutationEvent.Metadata.ShouldContain("mutation");
        mutationEvent.Metadata.ShouldContain("CreateTestData");
    }

    /// <summary>
    /// Tests that unauthorized GraphQL attempts generate audit events.
    /// 
    /// INTENT: Verify unauthorized access attempts are audited
    /// PURPOSE: Track security violations
    /// BUSINESS CONTEXT: Need to monitor unauthorized access attempts
    /// WHY IMPORTANT: Security breach attempts must be tracked
    /// ARCHITECTURAL SIGNIFICANCE: Validates security event auditing
    /// FUTURE RESILIENCE: Ensures security tracking remains intact
    /// </summary>
    [Fact]
    public async Task GraphQLUnauthorized_ShouldCreateAuditEvent()
    {
        // Arrange
        var query = @"query { testcomponent { id } }";

        // Act - Try to execute without authentication
        await Should.ThrowAsync<UnauthorizedException>(async () =>
            await TestDataContext().GraphQL(query).Execute());

        // Assert
        var auditEvents = await GetAuditEventsForEntity(Guid.Empty);
        var unauthorizedEvent = auditEvents.FirstOrDefault(e => e.EventType == AuditEventTypes.GraphQLUnauthorized);
        
        unauthorizedEvent.ShouldNotBeNull();
        unauthorizedEvent.Metadata.ShouldContain("Missing JWT token");
        unauthorizedEvent.Metadata.ShouldContain("query");
    }

    /// <summary>
    /// Tests that GraphQL authorization failures generate audit events.
    /// 
    /// INTENT: Verify authorization failures are audited
    /// PURPOSE: Track insufficient permission attempts
    /// BUSINESS CONTEXT: Users may attempt to access restricted data
    /// WHY IMPORTANT: Permission violations must be tracked
    /// ARCHITECTURAL SIGNIFICANCE: Validates authorization auditing
    /// FUTURE RESILIENCE: Ensures authorization tracking remains intact
    /// </summary>
    [Fact]
    public async Task GraphQLAuthorizationFailed_ShouldCreateAuditEvent()
    {
        // Arrange
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0LXVzZXIiLCJlbWFpbCI6InRlc3RAZXhhbXBsZS5jb20iLCJyb2xlIjoidXNlciJ9.test";
        var query = @"query { admin_only_data { id } }";

        // Act - Try to access admin-only data with user role
        await Should.ThrowAsync<ForbiddenException>(async () =>
            await TestDataContext().GraphQL(query)
                .WithAuth(jwt)
                .RequireRole("admin")
                .Execute());

        // Assert
        var auditEvents = await GetAuditEventsForEntity(Guid.Empty);
        var authFailEvent = auditEvents.FirstOrDefault(e => e.EventType == AuditEventTypes.AuthorizationFailed);
        
        authFailEvent.ShouldNotBeNull();
        authFailEvent.Metadata.ShouldContain("requiredRole");
        authFailEvent.Metadata.ShouldContain("admin");
        authFailEvent.Metadata.ShouldContain("actualRole");
        authFailEvent.Metadata.ShouldContain("user");
    }

    /// <summary>
    /// Tests that GraphQL claim validation failures generate audit events.
    /// 
    /// INTENT: Verify claim validation failures are audited
    /// PURPOSE: Track custom authorization failures
    /// BUSINESS CONTEXT: Custom claims may restrict access
    /// WHY IMPORTANT: All authorization failures must be tracked
    /// ARCHITECTURAL SIGNIFICANCE: Validates claim-based authorization auditing
    /// FUTURE RESILIENCE: Ensures claim validation tracking
    /// </summary>
    [Fact]
    public async Task GraphQLClaimValidationFailed_ShouldCreateAuditEvent()
    {
        // Arrange
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0LXVzZXIiLCJ0ZW5hbnRfaWQiOiJ0ZW5hbnQxIn0.test";
        var query = @"query { tenant_data { id } }";

        // Act - Try to access data for wrong tenant
        await Should.ThrowAsync<ForbiddenException>(async () =>
            await TestDataContext().GraphQL(query)
                .WithAuth(jwt)
                .RequireClaim("tenant_id", "tenant2")
                .Execute());

        // Assert
        var auditEvents = await GetAuditEventsForEntity(Guid.Empty);
        var claimFailEvent = auditEvents.FirstOrDefault(e => 
            e.EventType == AuditEventTypes.AuthorizationFailed &&
            e.Metadata.Contains("tenant_id"));
        
        claimFailEvent.ShouldNotBeNull();
        claimFailEvent.Metadata.ShouldContain("expectedValue");
        claimFailEvent.Metadata.ShouldContain("tenant2");
        claimFailEvent.Metadata.ShouldContain("actualValue");
        claimFailEvent.Metadata.ShouldContain("tenant1");
    }

    /// <summary>
    /// Tests that GraphQL errors generate audit events.
    /// 
    /// INTENT: Verify GraphQL errors are audited
    /// PURPOSE: Track query/mutation failures
    /// BUSINESS CONTEXT: Failed operations need investigation
    /// WHY IMPORTANT: Error tracking helps debug issues
    /// ARCHITECTURAL SIGNIFICANCE: Validates error auditing
    /// FUTURE RESILIENCE: Ensures error tracking remains intact
    /// </summary>
    [Fact]
    public async Task GraphQLError_ShouldCreateAuditEvent()
    {
        // Arrange
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0LXVzZXIifQ.test";
        var invalidQuery = @"query { invalid_syntax "; // Missing closing brace

        // Act
        try
        {
            await TestDataContext().GraphQL(invalidQuery)
                .WithAuth(jwt)
                .Execute();
        }
        catch (GraphQLException)
        {
            // Expected - GraphQL will fail with syntax error
        }
        
        // Small delay to ensure audit events are committed
        await Task.Delay(500);

        // Assert
        var auditEvents = await GetAuditEventsForEntity(Guid.Empty);
        var errorEvent = auditEvents.FirstOrDefault(e => 
            e.EventType == AuditEventTypes.GraphQLError && 
            e.Metadata.Contains("invalid_syntax"));
        
        errorEvent.ShouldNotBeNull();
        errorEvent.Metadata.ShouldContain("errors");
    }

    /// <summary>
    /// Tests that GraphQL operations with variables generate proper audit events.
    /// 
    /// INTENT: Verify GraphQL variables are audited
    /// PURPOSE: Track parameterized queries
    /// BUSINESS CONTEXT: Variables contain user input
    /// WHY IMPORTANT: Variable values may contain sensitive data
    /// ARCHITECTURAL SIGNIFICANCE: Validates variable auditing
    /// FUTURE RESILIENCE: Ensures complete query tracking
    /// </summary>
    [Fact]
    public async Task GraphQLWithVariables_ShouldIncludeVariablesInAudit()
    {
        // Arrange
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0LXVzZXIifQ.test";
        var query = @"
            query GetById($id: ID!) {
                testcomponent(id: $id) {
                    name
                }
            }";
        var variables = new { id = Guid.NewGuid() };

        // Act
        try
        {
            await TestDataContext().GraphQL(query)
                .WithAuth(jwt)
                .WithVariables(variables)
                .Execute();
        }
        catch
        {
            // GraphQL execution might fail, but we're testing audit logging
        }
        
        // Small delay to ensure audit events are committed
        await Task.Delay(500);

        // Assert
        var auditEvents = await GetAuditEventsForEntity(Guid.Empty);
        var queryEvent = auditEvents.FirstOrDefault(e => 
            e.EventType == AuditEventTypes.GraphQLQuery &&
            e.Metadata != null && 
            e.Metadata.Contains("GetById"));
        
        queryEvent.ShouldNotBeNull();
        queryEvent.Metadata.ShouldContain("variables");
        queryEvent.Metadata.ShouldContain(variables.id.ToString());
    }

    // Helper methods
    private async Task<List<AuditEvent>> GetAuditEventsForEntity(Guid entityId)
    {
        using var scope = _serviceProvider.CreateScope();
        var pgClient = scope.ServiceProvider.GetRequiredService<IPgClient>();
        
        // For GraphQL tests, we use Guid.Empty as the entityId for system-wide events
        // So we need to query all audit events when entityId is empty
        List<AuditEvent> auditEvents;
        if (entityId == Guid.Empty)
        {
            // Get all recent audit events for GraphQL operations
            auditEvents = await pgClient.From<AuditEvent>()
                .Get();
            
            // Filter to recent events (last 30 seconds) to avoid picking up old test data
            var cutoffTime = DateTime.UtcNow.AddSeconds(-30);
            auditEvents = auditEvents
                .Where(e => e.Timestamp >= cutoffTime)
                .OrderByDescending(e => e.Timestamp)
                .ToList();
        }
        else
        {
            auditEvents = await pgClient.From<AuditEvent>()
                .Filter("owner_entity_id", "eq", entityId)
                .Get();
        }
        
        return auditEvents;
    }
}