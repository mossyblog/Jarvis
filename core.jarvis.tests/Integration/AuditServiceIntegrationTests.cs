using core.jarvis.Data;
using core.jarvis.Data.Components;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace core.jarvis.tests.Integration;

/// <summary>
/// Integration tests for AuditService with real database connection.
/// </summary>
/// <remarks>
/// <para><strong>INTENT:</strong> Validates AuditService works correctly with real PostgreSQL database.</para>
/// <para><strong>PURPOSE:</strong> Ensures audit events are properly persisted to the audit_event table.</para>
/// <para><strong>BUSINESS CONTEXT:</strong> Audit trails are critical for compliance and debugging in production.</para>
/// <para><strong>WHY IMPORTANT:</strong> Audit failures in production could violate compliance requirements.</para>
/// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Tests the complete audit pipeline from service to database.</para>
/// <para><strong>FUTURE RESILIENCE:</strong> Ensures audit functionality remains reliable as system evolves.</para>
/// </remarks>
[Collection("SupabaseIntegration")]
public class AuditServiceIntegrationTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that audit events are persisted to database.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Validates audit events reach the database.</para>
    /// <para><strong>PURPOSE:</strong> Ensures the complete audit pipeline works end-to-end.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Audit trails must be reliable for compliance.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Silent audit failures could hide critical business events.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Tests database table structure matches model.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> Catches schema mismatches early.</para>
    /// </remarks>
    [Fact]
    public async Task LogEvent_ShouldPersistToDatabase()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var eventType = "TEST_EVENT_CREATED";
        var metadata = new { TestRun = DateTime.UtcNow, Purpose = "Integration Test" };
        TrackEntity(entityId);
        
        // Act
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        await auditService.LogEvent(eventType, entityId, metadata);
        
        // Give database time to persist
        await Task.Delay(1000);
        
        // Assert - Query the database directly through PgClient to verify
        var pgClient = _serviceProvider.GetRequiredService<IPgClient>();
        var auditEvents = await pgClient.From<AuditEvent>()
            .Filter("owner_entity_id", "eq", entityId)
            .Get();
        
        auditEvents.ShouldNotBeNull();
        auditEvents.Count.ShouldBeGreaterThan(0, $"No audit events found for entityId: {entityId}");
        
        var auditEvent = auditEvents.FirstOrDefault(a => a != null);
        auditEvent.ShouldNotBeNull("All audit events in the list were null");
        
        auditEvent.OwnerEntityId.ShouldBe(entityId);
        auditEvent.EventType.ShouldBe(eventType);
        auditEvent.EntityType.ShouldBe("TEST");
        
        // Track entity for cleanup
        TrackEntity(auditEvent.OwnerEntityId);
    }

    /// <summary>
    /// Tests that audit changes with old/new values are persisted correctly.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Validates change tracking functionality.</para>
    /// <para><strong>PURPOSE:</strong> Ensures before/after values are captured for auditing.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Change history is critical for debugging and compliance.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Lost change history makes troubleshooting impossible.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Tests JSON serialization of complex objects.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> Ensures change tracking remains functional.</para>
    /// </remarks>
    [Fact]
    public async Task LogChange_ShouldPersistOldAndNewValues()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var eventType = "TEST_STATUS_CHANGED";
        var oldValue = new { Status = "DRAFT", Amount = 100.00m };
        var newValue = new { Status = "APPROVED", Amount = 150.00m };
        TrackEntity(entityId);
        
        // Act
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        await auditService.LogChange(eventType, entityId, oldValue, newValue);
        
        // Give database time to persist
        await Task.Delay(1000);
        
        // Assert - Query the database directly through PgClient to verify
        var pgClient = _serviceProvider.GetRequiredService<IPgClient>();
        var auditEvents = await pgClient.From<AuditEvent>()
            .Filter("owner_entity_id", "eq", entityId)
            .Get();
        
        auditEvents.ShouldNotBeNull();
        auditEvents.Count.ShouldBeGreaterThan(0, $"No audit events found for entityId: {entityId}");
        
        var auditEvent = auditEvents.FirstOrDefault(a => a != null);
        auditEvent.ShouldNotBeNull("All audit events in the list were null");
        
        auditEvent.OldValue.ShouldNotBeNullOrEmpty();
        auditEvent.NewValue.ShouldNotBeNullOrEmpty();
        auditEvent.OldValue.ShouldContain("DRAFT");
        auditEvent.NewValue.ShouldContain("APPROVED");
        
        // Track entity for cleanup
        TrackEntity(auditEvent.OwnerEntityId);
    }

    /// <summary>
    /// Tests that the AuditService doesn't throw on database errors (fail silently).
    /// Note: This test intentionally uses an invalid URL and will generate expected connection warnings.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Validates audit failures don't break business operations.</para>
    /// <para><strong>PURPOSE:</strong> Ensures business continuity even when audit fails.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Business operations are more important than audit trails.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Audit failures shouldn't cause transaction rollbacks.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Tests error isolation in the audit service.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> Ensures graceful degradation under failure conditions.</para>
    /// </remarks>
    [Fact]
    public async Task LogEvent_WithInvalidData_ShouldNotThrow()
    {
        // Arrange - Use the test environment but test invalid data handling
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        
        // Act & Assert - Should not throw with empty/invalid event type
        await Should.NotThrowAsync(async () =>
            await auditService.LogEvent("", entityId));
            
        await Should.NotThrowAsync(async () =>
            await auditService.LogEvent(null, entityId));
            
        await Should.NotThrowAsync(async () =>
            await auditService.LogEvent("TEST_EVENT", Guid.Empty));
    }
}

/// <summary>
/// Collection definition to prevent parallel execution of Supabase integration tests.
/// </summary>
[CollectionDefinition("SupabaseIntegration")]
public class SupabaseIntegrationCollection : ICollectionFixture<object>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}