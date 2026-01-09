using core.jarvis.api.Handlers;
using core.jarvis.api.Services;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace core.jarvis.tests.Handlers;

/// <summary>
/// Integration tests for the SecurityAuditHandler.
/// Validates security audit event logging operations including authentication events,
/// account lockouts, and permission changes.
/// </summary>
/// <remarks>
/// <para><strong>INTENT:</strong> Validates SecurityAuditHandler correctly persists security audit events to the database.</para>
/// <para><strong>PURPOSE:</strong> Ensures security events are accurately captured for compliance and incident response.</para>
/// <para><strong>BUSINESS CONTEXT:</strong> Security audit trails are critical for regulatory compliance, forensics, and threat detection.</para>
/// <para><strong>WHY IMPORTANT:</strong> Missing or corrupted audit logs can result in compliance violations and hinder security investigations.</para>
/// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Tests the complete audit pipeline from handler through DataContext to database storage.</para>
/// <para><strong>FUTURE RESILIENCE:</strong> Ensures audit functionality remains reliable as authentication and security systems evolve.</para>
/// </remarks>
[Collection("Sequential")]
public class SecurityAuditHandlerTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that LogAuthenticationFailed correctly persists a failed authentication audit event.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Verify failed authentication attempts are properly logged with all context.</para>
    /// <para><strong>PURPOSE:</strong> Ensure security teams can track brute force and credential stuffing attacks.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Failed login tracking is essential for detecting account compromise attempts.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Without failed auth logs, security teams cannot identify attack patterns or lock compromised accounts.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Validates handler correctly sets EventType, Severity, and contextual fields.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> Ensures authentication failure tracking remains functional across auth system changes.</para>
    /// </remarks>
    [Fact]
    public async Task LogAuthenticationFailed_WithValidData_ShouldPersistAuditEvent()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var email = "attacker@example.com";
        var ipAddress = "192.168.1.100";
        var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        var reason = "Invalid password";
        TrackEntity(entityId);

        var handler = TestDataContext().For<SecurityAuditHandler>(entityId);

        // Act
        await handler.LogAuthenticationFailed(email, ipAddress, userAgent, reason);

        // Assert
        var pgClient = _serviceProvider.GetRequiredService<IPgClient>();
        var auditEvents = await pgClient.From<SecurityAuditEvent>()
            .Filter("owner_entity_id", "eq", entityId)
            .Get();

        auditEvents.ShouldNotBeNull();
        auditEvents.Count.ShouldBe(1);

        var auditEvent = auditEvents.First();
        auditEvent.EventType.ShouldBe("AUTHENTICATION_FAILED");
        auditEvent.TargetEmail.ShouldBe(email);
        auditEvent.IpAddress.ShouldBe(ipAddress);
        auditEvent.UserAgent.ShouldBe(userAgent);
        auditEvent.Reason.ShouldBe(reason);
        auditEvent.Severity.ShouldBe("MEDIUM");
        auditEvent.OwnerEntityId.ShouldBe(entityId);
    }

    /// <summary>
    /// Tests that LogAccountLocked correctly persists an account lockout audit event with lockout details.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Verify account lockout events capture failed attempt count and unlock time.</para>
    /// <para><strong>PURPOSE:</strong> Enable security monitoring and automated response to lockout patterns.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Account lockouts indicate potential attacks or user credential issues requiring attention.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Lockout events with context help security teams distinguish attacks from forgotten passwords.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Validates nullable fields (FailedAttempts, LockedUntil) are correctly persisted.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> Ensures lockout tracking adapts to changing lockout policies and thresholds.</para>
    /// </remarks>
    [Fact]
    public async Task LogAccountLocked_WithLockoutDetails_ShouldPersistHighSeverityEvent()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var email = "locked.user@example.com";
        var failedAttempts = 5;
        var lockedUntil = DateTime.UtcNow.AddMinutes(30);
        TrackEntity(entityId);

        var handler = TestDataContext().For<SecurityAuditHandler>(entityId);

        // Act
        await handler.LogAccountLocked(email, failedAttempts, lockedUntil);

        // Assert
        var pgClient = _serviceProvider.GetRequiredService<IPgClient>();
        var auditEvents = await pgClient.From<SecurityAuditEvent>()
            .Filter("owner_entity_id", "eq", entityId)
            .Get();

        auditEvents.ShouldNotBeNull();
        auditEvents.Count.ShouldBe(1);

        var auditEvent = auditEvents.First();
        auditEvent.EventType.ShouldBe("ACCOUNT_LOCKED");
        auditEvent.TargetEmail.ShouldBe(email);
        auditEvent.FailedAttempts.ShouldBe(failedAttempts);
        auditEvent.LockedUntil.ShouldNotBeNull();
        auditEvent.LockedUntil!.Value.ShouldBeInRange(lockedUntil.AddSeconds(-1), lockedUntil.AddSeconds(1));
        auditEvent.Severity.ShouldBe("HIGH");
    }

    /// <summary>
    /// Tests that LogSuspiciousActivity correctly persists a critical severity event with activity details.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Verify suspicious activity events are logged with CRITICAL severity for immediate attention.</para>
    /// <para><strong>PURPOSE:</strong> Enable real-time alerting and incident response for potential security breaches.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Suspicious activity may indicate active attacks requiring immediate investigation.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Critical events must be distinguishable from routine logs for SOC prioritization.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Validates dynamic EventType construction using ToUpperInvariant().</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> Ensures new suspicious activity types can be logged without code changes.</para>
    /// </remarks>
    [Fact]
    public async Task LogSuspiciousActivity_WithActivityDetails_ShouldPersistCriticalSeverityEvent()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var activityType = "IP_GEOLOCATION_MISMATCH";
        var details = "Login from unexpected country after credential change";
        var ipAddress = "203.0.113.50";
        TrackEntity(entityId);

        var handler = TestDataContext().For<SecurityAuditHandler>(entityId);

        // Act
        await handler.LogSuspiciousActivity(activityType, details, ipAddress);

        // Assert
        var pgClient = _serviceProvider.GetRequiredService<IPgClient>();
        var auditEvents = await pgClient.From<SecurityAuditEvent>()
            .Filter("owner_entity_id", "eq", entityId)
            .Get();

        auditEvents.ShouldNotBeNull();
        auditEvents.Count.ShouldBe(1);

        var auditEvent = auditEvents.First();
        auditEvent.EventType.ShouldBe("SUSPICIOUS_IP_GEOLOCATION_MISMATCH");
        auditEvent.Reason.ShouldBe(details);
        auditEvent.IpAddress.ShouldBe(ipAddress);
        auditEvent.Severity.ShouldBe("CRITICAL");
        auditEvent.OwnerEntityId.ShouldBe(entityId);
    }
}
