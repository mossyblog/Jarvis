using System.Reflection;
using core.jarvis.api.Processors;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Unit.Processors;

/// <summary>
/// INTENT: Test rate limiting and account lockout functionality for brute force protection
/// PURPOSE: Verify that the RateLimitingPreProcessor correctly tracks and enforces rate limits
/// BUSINESS CONTEXT: Rate limiting prevents attackers from brute-forcing authentication endpoints
/// WHY IMPORTANT: Security mechanism to protect user accounts from credential stuffing attacks
/// ARCHITECTURAL SIGNIFICANCE: Core security layer that runs before authentication handlers
/// FUTURE RESILIENCE: Ensures rate limiting logic remains correct as the system evolves
/// </summary>
public class RateLimitingProcessorTests
{
    /// <summary>
    /// INTENT: Verify that failed login attempts are tracked and lead to account lockout
    /// PURPOSE: Ensure the account lockout mechanism activates after threshold is exceeded
    /// BUSINESS CONTEXT: Users attempting to brute-force passwords must be locked out
    /// WHY IMPORTANT: Prevents credential stuffing and password guessing attacks
    /// ARCHITECTURAL SIGNIFICANCE: Tests the static TrackFailedAttempt and IsAccountLockedAsync methods
    /// FUTURE RESILIENCE: Catches regressions in lockout threshold behavior
    /// </summary>
    [Fact]
    public async Task TrackFailedAttempt_AfterMaxAttempts_LocksAccount()
    {
        // Arrange
        var email = $"lockout_test_{Guid.NewGuid()}@example.com";
        var logger = new TestLogger<RateLimitingPreProcessor>();
        const int maxFailedAttempts = 5;

        // Act - Track 5 failed attempts (the lockout threshold)
        for (int i = 0; i < maxFailedAttempts; i++)
        {
            RateLimitingPreProcessor.TrackFailedAttempt(email, logger);
        }

        // Assert - Account should now be locked
        var isLocked = await RateLimitingPreProcessor.IsAccountLockedAsync(email, logger);
        isLocked.ShouldBeTrue("Account should be locked after 5 failed attempts");
    }

    /// <summary>
    /// INTENT: Verify that accounts are not locked before reaching the failure threshold
    /// PURPOSE: Ensure legitimate users are not locked out prematurely
    /// BUSINESS CONTEXT: Users may mistype passwords occasionally without being attackers
    /// WHY IMPORTANT: Prevents false positives that would degrade user experience
    /// ARCHITECTURAL SIGNIFICANCE: Tests the threshold boundary condition
    /// FUTURE RESILIENCE: Ensures threshold changes are intentional and tested
    /// </summary>
    [Fact]
    public async Task TrackFailedAttempt_BelowThreshold_DoesNotLockAccount()
    {
        // Arrange
        var email = $"below_threshold_{Guid.NewGuid()}@example.com";
        var logger = new TestLogger<RateLimitingPreProcessor>();
        const int attemptsBeforeThreshold = 4;

        // Act - Track 4 failed attempts (one below the lockout threshold of 5)
        for (int i = 0; i < attemptsBeforeThreshold; i++)
        {
            RateLimitingPreProcessor.TrackFailedAttempt(email, logger);
        }

        // Assert - Account should NOT be locked yet
        var isLocked = await RateLimitingPreProcessor.IsAccountLockedAsync(email, logger);
        isLocked.ShouldBeFalse("Account should not be locked with only 4 failed attempts");
    }

    /// <summary>
    /// INTENT: Verify that email normalization is applied consistently for lockout tracking
    /// PURPOSE: Ensure attackers cannot bypass lockout by varying email case
    /// BUSINESS CONTEXT: Email addresses are case-insensitive per RFC 5321
    /// WHY IMPORTANT: Prevents trivial bypass of rate limiting via case manipulation
    /// ARCHITECTURAL SIGNIFICANCE: Tests the ToLowerInvariant normalization in tracking
    /// FUTURE RESILIENCE: Ensures case sensitivity bugs do not create security holes
    /// </summary>
    [Fact]
    public async Task TrackFailedAttempt_NormalizesEmailCase_ForConsistentLockout()
    {
        // Arrange
        var uniqueId = Guid.NewGuid();
        var emailLower = $"case_test_{uniqueId}@example.com";
        var emailUpper = $"CASE_TEST_{uniqueId}@EXAMPLE.COM";
        var emailMixed = $"Case_Test_{uniqueId}@Example.COM";
        var logger = new TestLogger<RateLimitingPreProcessor>();

        // Act - Track attempts with different case variations (total of 6 > threshold of 5)
        RateLimitingPreProcessor.TrackFailedAttempt(emailLower, logger);
        RateLimitingPreProcessor.TrackFailedAttempt(emailLower, logger);
        RateLimitingPreProcessor.TrackFailedAttempt(emailUpper, logger);
        RateLimitingPreProcessor.TrackFailedAttempt(emailUpper, logger);
        RateLimitingPreProcessor.TrackFailedAttempt(emailMixed, logger);
        RateLimitingPreProcessor.TrackFailedAttempt(emailMixed, logger);

        // Assert - All case variants should resolve to the same locked account
        var isLockedLower = await RateLimitingPreProcessor.IsAccountLockedAsync(emailLower, logger);
        var isLockedUpper = await RateLimitingPreProcessor.IsAccountLockedAsync(emailUpper, logger);
        var isLockedMixed = await RateLimitingPreProcessor.IsAccountLockedAsync(emailMixed, logger);

        isLockedLower.ShouldBeTrue("Lowercase email check should show account as locked");
        isLockedUpper.ShouldBeTrue("Uppercase email check should show account as locked");
        isLockedMixed.ShouldBeTrue("Mixed case email check should show account as locked");
    }

    /// <summary>
    /// Minimal logger implementation for unit testing.
    /// Captures log messages without requiring full logging infrastructure.
    /// </summary>
    private class TestLogger<T> : ILogger<T>, ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // No-op for unit tests - logs are not captured
        }
    }
}
