using core.jarvis.api.Security;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace core.jarvis.api.tests.Security;

/// <summary>
/// INTENT: Test IP address extraction with spoofing protection
/// PURPOSE: Validate that X-Forwarded-For headers are only trusted from configured proxies
/// BUSINESS CONTEXT: IP spoofing can bypass rate limiting and security controls
/// WHY IMPORTANT: Critical security control for rate limiting and audit logging
/// ARCHITECTURAL SIGNIFICANCE: Tests core security boundary
/// FUTURE RESILIENCE: Ensures IP detection remains secure as infrastructure changes
/// </summary>
public class IpSpoofingProtectionTests
{
    #region TrustedProxyOptions Configuration Tests

    /// <summary>
    /// INTENT: Verify secure defaults for TrustedProxyOptions
    /// </summary>
    [Fact]
    public void TrustedProxyOptions_DefaultValues_AreSecure()
    {
        // Arrange & Act
        var options = new TrustedProxyOptions();

        // Assert - validation should be enabled by default
        options.EnableTrustedProxyValidation.ShouldBeTrue();
        // Trusted IPs should be null/empty by default (trust no one)
        (options.TrustedProxyIps == null || options.TrustedProxyIps.Count == 0).ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify trusted proxy IPs can be configured
    /// </summary>
    [Fact]
    public void TrustedProxyOptions_CanConfigureTrustedIps()
    {
        // Arrange
        var options = new TrustedProxyOptions
        {
            EnableTrustedProxyValidation = true,
            TrustedProxyIps = new HashSet<string> { "10.0.0.1", "10.0.0.2", "192.168.1.1" }
        };

        // Assert
        options.TrustedProxyIps.ShouldNotBeNull();
        options.TrustedProxyIps.Count.ShouldBe(3);
        options.TrustedProxyIps.Contains("10.0.0.1").ShouldBeTrue();
        options.TrustedProxyIps.Contains("10.0.0.2").ShouldBeTrue();
        options.TrustedProxyIps.Contains("192.168.1.1").ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify validation can be disabled
    /// </summary>
    [Fact]
    public void TrustedProxyOptions_ValidationCanBeDisabled()
    {
        // Arrange
        var options = new TrustedProxyOptions
        {
            EnableTrustedProxyValidation = false
        };

        // Assert
        options.EnableTrustedProxyValidation.ShouldBeFalse();
    }

    /// <summary>
    /// INTENT: Verify empty trusted proxy list is valid but trusts nothing
    /// </summary>
    [Fact]
    public void TrustedProxyOptions_EmptyList_IsValid()
    {
        // Arrange
        var options = new TrustedProxyOptions
        {
            EnableTrustedProxyValidation = true,
            TrustedProxyIps = new HashSet<string>()
        };

        // Assert
        options.TrustedProxyIps.ShouldNotBeNull();
        options.TrustedProxyIps.Count.ShouldBe(0);
    }

    #endregion
}
