using core.jarvis.Data.Components;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace core.jarvis.tests.Helpers;

/// <summary>
/// Helper methods for audit-related tests to improve reliability and diagnostics.
/// </summary>
public static class AuditTestHelpers
{
    /// <summary>
    /// Waits for audit events to be created with retry logic.
    /// </summary>
    public static async Task<List<AuditEvent>> WaitForAuditEvents(
        Func<Task<List<AuditEvent>>> getAuditEvents,
        string eventType,
        int expectedCount,
        ILogger logger,
        int maxRetries = 5,
        int delayMs = 500)
    {
        List<AuditEvent> matchingEvents = new();
        
        for (int retry = 0; retry < maxRetries; retry++)
        {
            var allEvents = await getAuditEvents();
            matchingEvents = allEvents.Where(e => e.EventType == eventType).ToList();
            
            logger.LogInformation("Retry {Retry}/{MaxRetries}: Found {Count}/{Expected} {EventType} events", 
                retry + 1, maxRetries, matchingEvents.Count, expectedCount, eventType);
            
            if (matchingEvents.Count >= expectedCount)
            {
                return matchingEvents;
            }
            
            if (retry < maxRetries - 1)
            {
                await Task.Delay(delayMs * (retry + 1)); // Exponential backoff
            }
        }
        
        // Log all events found for debugging
        var allEventsFinal = await getAuditEvents();
        logger.LogWarning("Failed to find expected {EventType} events. Found {Count}/{Expected}. All events: {Events}",
            eventType, matchingEvents.Count, expectedCount,
            string.Join(", ", allEventsFinal.Select(e => $"{e.EventType}:{e.Metadata?.Substring(0, Math.Min(50, e.Metadata.Length))}")));
        
        return matchingEvents;
    }
    
    /// <summary>
    /// Asserts that audit events contain expected metadata with detailed error messages.
    /// </summary>
    public static void AssertAuditEventMetadata(
        AuditEvent auditEvent,
        string expectedContent,
        string description)
    {
        auditEvent.ShouldNotBeNull($"{description}: Audit event should not be null");
        auditEvent.Metadata.ShouldNotBeNullOrEmpty($"{description}: Metadata should not be empty");
        
        auditEvent.Metadata.ShouldContain(expectedContent, Case.Insensitive,
            $"{description}: Expected metadata to contain '{expectedContent}' but was: {auditEvent.Metadata}");
    }
    
    /// <summary>
    /// Waits for a specific condition with timeout and logging.
    /// </summary>
    public static async Task<T> WaitForCondition<T>(
        Func<Task<T>> condition,
        Func<T, bool> predicate,
        string description,
        ILogger logger,
        int timeoutMs = 5000,
        int pollIntervalMs = 100)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        T result = default!;
        
        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            result = await condition();
            
            if (predicate(result))
            {
                logger.LogInformation("{Description} succeeded after {ElapsedMs}ms", 
                    description, stopwatch.ElapsedMilliseconds);
                return result;
            }
            
            await Task.Delay(pollIntervalMs);
        }
        
        logger.LogError("{Description} timed out after {ElapsedMs}ms. Last result: {Result}",
            description, stopwatch.ElapsedMilliseconds, result);
        
        throw new TimeoutException($"{description} timed out after {timeoutMs}ms");
    }
    
    /// <summary>
    /// Ensures database operations are flushed by waiting a safe amount of time.
    /// </summary>
    public static async Task EnsureDbOperationsFlushed(ILogger logger, int delayMs = 500)
    {
        logger.LogDebug("Waiting {DelayMs}ms for database operations to flush", delayMs);
        await Task.Delay(delayMs);
    }
}