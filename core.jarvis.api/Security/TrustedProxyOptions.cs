namespace core.jarvis.api.Security;

/// <summary>
/// Configuration options for trusted proxy IP addresses.
///
/// SECURITY NOTE: Only trust X-Forwarded-For headers when requests come from
/// known, trusted proxy IP addresses. Without this validation, attackers can
/// spoof their IP address by sending a crafted X-Forwarded-For header.
/// </summary>
public class TrustedProxyOptions
{
    /// <summary>
    /// List of trusted proxy IP addresses. X-Forwarded-For headers will only
    /// be trusted if the immediate connection comes from one of these IPs.
    ///
    /// If empty or null, X-Forwarded-For headers are NOT trusted and the
    /// socket address is used instead (secure default).
    ///
    /// Example values: "10.0.0.1", "192.168.1.1", "::1"
    /// </summary>
    public HashSet<string>? TrustedProxyIps { get; set; }

    /// <summary>
    /// Whether to enable trusted proxy validation.
    /// Default is true (secure by default).
    /// </summary>
    public bool EnableTrustedProxyValidation { get; set; } = true;
}
