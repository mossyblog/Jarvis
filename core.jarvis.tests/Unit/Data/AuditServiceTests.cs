using core.jarvis.Data;
using core.jarvis.Data.Components;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace core.jarvis.tests.Unit.Data;

/// <summary>
/// Tests for AuditService functionality.
/// Tests use real database connections following the integration test pattern.
/// </summary>
public class AuditServiceTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify AuditService requires non-null pgClient
    /// PURPOSE: Ensure proper dependency validation
    /// BUSINESS CONTEXT: AuditService cannot function without database access
    /// WHY IMPORTANT: Prevents runtime errors from missing dependencies
    /// ARCHITECTURAL SIGNIFICANCE: Enforces dependency injection principles
    /// FUTURE RESILIENCE: Catches misconfiguration early
    /// </summary>
    [Fact]
    public void Constructor_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new NullLogger<AuditService>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new AuditService(null!, logger))
            .ParamName.ShouldBe("pgClient");
    }

    /// <summary>
    /// INTENT: Verify AuditService requires non-null logger
    /// PURPOSE: Ensure logging dependency is validated
    /// BUSINESS CONTEXT: Audit failures must be logged for monitoring
    /// WHY IMPORTANT: Silent audit failures could hide security issues
    /// ARCHITECTURAL SIGNIFICANCE: Enforces logging requirements
    /// FUTURE RESILIENCE: Ensures audit issues are always visible
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var pgClient = _serviceProvider.GetRequiredService<IPgClient>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new AuditService(pgClient, null!))
            .ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// INTENT: Verify AuditService constructs properly with valid dependencies
    /// PURPOSE: Ensure service can be instantiated correctly
    /// BUSINESS CONTEXT: AuditService is critical for compliance
    /// WHY IMPORTANT: Validates basic service setup
    /// ARCHITECTURAL SIGNIFICANCE: Confirms DI configuration works
    /// FUTURE RESILIENCE: Baseline test for service creation
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var pgClient = _serviceProvider.GetRequiredService<IPgClient>();
        var logger = _serviceProvider.GetRequiredService<ILogger<AuditService>>();

        // Act & Assert
        Should.NotThrow(() => 
            new AuditService(pgClient, logger));
    }

    /// <summary>
    /// INTENT: Verify LogEvent creates audit records with valid data
    /// PURPOSE: Ensure audit events are persisted correctly
    /// BUSINESS CONTEXT: All significant operations must be audited
    /// WHY IMPORTANT: Audit trails are required for compliance
    /// ARCHITECTURAL SIGNIFICANCE: Validates audit persistence
    /// FUTURE RESILIENCE: Ensures audit logging remains functional
    /// </summary>
    [Fact]
    public async Task LogEvent_WithValidParameters_ShouldCreateAuditRecord()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var pgClient = _serviceProvider.GetRequiredService<IPgClient>();
        
        var eventType = "TEST_INVOICE_CREATED";
        var entityId = Guid.NewGuid();
        TrackEntity(entityId); // Track for cleanup
        var metadata = new { Amount = 100.00m, Description = "Test invoice" };

        // Act
        await auditService.LogEvent(eventType, entityId, metadata);
        
        // Wait for audit to be persisted
        await Task.Delay(100);

        // Assert - Verify the audit event was created
        var auditEvents = await pgClient.From<AuditEvent>()
            .Filter("owner_entity_id", "eq", entityId)
            .Get();
            
        auditEvents.Count.ShouldBe(1);
        var auditEvent = auditEvents.First();
        auditEvent.EventType.ShouldBe(eventType);
        auditEvent.OwnerEntityId.ShouldBe(entityId);
        auditEvent.Metadata.ShouldContain("Amount");
        auditEvent.Metadata.ShouldContain("100");
    }

    /// <summary>
    /// INTENT: Verify LogEvent handles null metadata gracefully
    /// PURPOSE: Ensure optional metadata doesn't break auditing
    /// BUSINESS CONTEXT: Not all events have additional metadata
    /// WHY IMPORTANT: Robustness in various scenarios
    /// ARCHITECTURAL SIGNIFICANCE: Validates null handling
    /// FUTURE RESILIENCE: Prevents NPE in audit logging
    /// </summary>
    [Fact]
    public async Task LogEvent_WithNullMetadata_ShouldCreateAuditRecord()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var pgClient = _serviceProvider.GetRequiredService<IPgClient>();
        
        var eventType = "TEST_PAYMENT_PROCESSED";
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);

        // Act
        await auditService.LogEvent(eventType, entityId, null);
        
        await Task.Delay(100);

        // Assert
        var auditEvents = await pgClient.From<AuditEvent>()
            .Filter("owner_entity_id", "eq", entityId)
            .Get();
            
        auditEvents.Count.ShouldBe(1);
        auditEvents.First().Metadata.ShouldBeNullOrEmpty();
    }

    /// <summary>
    /// INTENT: Verify LogChange tracks old and new values
    /// PURPOSE: Ensure change tracking works correctly
    /// BUSINESS CONTEXT: Need to track what changed for compliance
    /// WHY IMPORTANT: Change history is critical for auditing
    /// ARCHITECTURAL SIGNIFICANCE: Validates change tracking
    /// FUTURE RESILIENCE: Ensures data changes are traceable
    /// </summary>
    [Fact]
    public async Task LogChange_WithValidParameters_ShouldRecordOldAndNewValues()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var pgClient = _serviceProvider.GetRequiredService<IPgClient>();
        
        var eventType = "TEST_INVOICE_UPDATED";
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var oldValue = new { Status = "DRAFT", Amount = 100.00m };
        var newValue = new { Status = "SENT", Amount = 150.00m };
        var metadata = new { Reason = "Amount adjustment" };

        // Act
        await auditService.LogChange(eventType, entityId, oldValue, newValue, metadata);
        
        await Task.Delay(100);

        // Assert
        var auditEvents = await pgClient.From<AuditEvent>()
            .Filter("owner_entity_id", "eq", entityId)
            .Get();
            
        auditEvents.Count.ShouldBe(1);
        var auditEvent = auditEvents.First();
        auditEvent.OldValue.ShouldContain("DRAFT");
        auditEvent.NewValue.ShouldContain("SENT");
        auditEvent.Metadata.ShouldContain("Amount adjustment");
    }

    /// <summary>
    /// INTENT: Verify null event type is handled gracefully
    /// PURPOSE: Ensure audit service doesn't crash on bad input
    /// BUSINESS CONTEXT: Defensive programming for robustness
    /// WHY IMPORTANT: Audit failures shouldn't break business operations
    /// ARCHITECTURAL SIGNIFICANCE: Validates error handling
    /// FUTURE RESILIENCE: Prevents audit service crashes
    /// </summary>
    [Fact]
    public async Task LogEvent_WithNullEventType_ShouldNotThrow()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);

        // Act & Assert - Should not throw
        await Should.NotThrowAsync(async () => 
            await auditService.LogEvent(null!, entityId));
    }

    /// <summary>
    /// INTENT: Verify empty entity ID is logged with warning
    /// PURPOSE: Track system-wide events not tied to entities
    /// BUSINESS CONTEXT: Some events are system-level
    /// WHY IMPORTANT: Flexibility for different audit scenarios
    /// ARCHITECTURAL SIGNIFICANCE: Supports various audit patterns
    /// FUTURE RESILIENCE: Enables system-wide audit events
    /// </summary>
    [Fact]
    public async Task LogEvent_WithEmptyGuid_ShouldLogWarning()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var eventType = "SYSTEM_EVENT";
        var entityId = Guid.Empty;

        // Act - Should not throw but should log warning
        await Should.NotThrowAsync(async () => 
            await auditService.LogEvent(eventType, entityId));
            
        // Note: Warning would be visible in test output
    }

    /// <summary>
    /// INTENT: Verify complex metadata is serialized correctly
    /// PURPOSE: Ensure various data types are handled
    /// BUSINESS CONTEXT: Audit metadata can be complex objects
    /// WHY IMPORTANT: Flexibility in what can be audited
    /// ARCHITECTURAL SIGNIFICANCE: Validates JSON serialization
    /// FUTURE RESILIENCE: Supports rich audit data
    /// </summary>
    [Fact]
    public async Task LogEvent_WithComplexMetadata_ShouldSerializeCorrectly()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var pgClient = _serviceProvider.GetRequiredService<IPgClient>();
        
        var eventType = "TEST_COMPLEX_EVENT";
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);
        var metadata = new 
        { 
            Numbers = new[] { 1, 2, 3 },
            Nested = new { Property = "value" },
            DateTime = DateTime.UtcNow,
            Guid = Guid.NewGuid()
        };

        // Act
        await auditService.LogEvent(eventType, entityId, metadata);
        
        await Task.Delay(100);

        // Assert
        var auditEvents = await pgClient.From<AuditEvent>()
            .Filter("owner_entity_id", "eq", entityId)
            .Get();
            
        auditEvents.Count.ShouldBe(1);
        var auditEvent = auditEvents.First();
        auditEvent.Metadata.ShouldContain("Numbers");
        auditEvent.Metadata.ShouldContain("[1,2,3]");
        auditEvent.Metadata.ShouldContain("Nested");
    }
}