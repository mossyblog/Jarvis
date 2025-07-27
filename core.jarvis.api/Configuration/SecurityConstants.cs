namespace core.jarvis.api.Configuration;

/// <summary>
/// Centralized configuration constants for security-related operations.
/// Provides a single source of truth for security parameters across the application.
/// </summary>
public static class SecurityConstants
{
    /// <summary>
    /// Security token and authentication configuration.
    /// </summary>
    public static class Authentication
    {
        /// <summary>
        /// Default access token expiration time in minutes.
        /// Balances security and user experience for JWT tokens.
        /// </summary>
        public const int DefaultAccessTokenExpirationMinutes = 15;
        
        /// <summary>
        /// Default refresh token expiration time in days.
        /// Longer duration for refresh tokens as they are used less frequently.
        /// </summary>
        public const int DefaultRefreshTokenExpirationDays = 30;
        
        /// <summary>
        /// Size in bytes for cryptographically secure refresh tokens.
        /// Provides sufficient entropy for secure random token generation.
        /// </summary>
        public const int RefreshTokenSizeBytes = 32;
        
        /// <summary>
        /// Maximum number of active sessions allowed per user.
        /// Prevents session proliferation while accommodating multiple devices.
        /// </summary>
        public const int MaxActiveSessionsPerUser = 5;
        
        /// <summary>
        /// Permission cache TTL in minutes for improved performance.
        /// Balances performance with security by caching permissions temporarily.
        /// </summary>
        public const int DefaultPermissionCacheTTLMinutes = 5;
    }

    /// <summary>
    /// Rate limiting and brute force protection configuration.
    /// </summary>
    public static class RateLimiting
    {
        /// <summary>
        /// Maximum number of authentication attempts allowed per minute per IP address.
        /// This helps prevent high-frequency brute force attacks.
        /// </summary>
        public const int MaxAttemptsPerMinute = 5;
        
        /// <summary>
        /// Maximum number of authentication attempts allowed per hour per IP address.
        /// This provides protection against sustained brute force attacks.
        /// </summary>
        public const int MaxAttemptsPerHour = 20;
        
        /// <summary>
        /// Number of consecutive failed authentication attempts that trigger account lockout.
        /// Balance between security and user experience.
        /// </summary>
        public const int MaxFailedAttemptsBeforeLock = 5;
        
        /// <summary>
        /// Duration in minutes for which an account remains locked after exceeding failed attempts.
        /// Industry standard for temporary lockout duration.
        /// </summary>
        public const int LockoutMinutes = 30;
        
        /// <summary>
        /// Base delay in milliseconds added per failed attempt to prevent timing attacks.
        /// Progressive delay helps slow down automated attacks.
        /// </summary>
        public const int ProgressiveDelayMilliseconds = 1000;
        
        /// <summary>
        /// Maximum progressive delay in milliseconds to prevent indefinite delays.
        /// Caps the maximum delay for failed authentication attempts.
        /// </summary>
        public const int MaxProgressiveDelayMilliseconds = 10000;
        
        /// <summary>
        /// HTTP Retry-After header value in seconds for rate-limited responses.
        /// Standard value indicating client should wait before retrying.
        /// </summary>
        public const string RetryAfterSeconds = "60";
    }

    /// <summary>
    /// Password policy and cryptographic configuration.
    /// </summary>
    public static class PasswordPolicy
    {
        /// <summary>
        /// Minimum required password length for security compliance.
        /// Industry standard minimum length for strong passwords.
        /// </summary>
        public const int MinimumLength = 8;
        
        /// <summary>
        /// Maximum allowed password length to prevent memory exhaustion attacks.
        /// Reasonable upper bound for password length.
        /// </summary>
        public const int MaximumLength = 128;
        
        /// <summary>
        /// Minimum number of unique characters required in a password.
        /// Helps prevent passwords with excessive repetition.
        /// </summary>
        public const int MinimumUniqueCharacters = 5;
        
        /// <summary>
        /// Minimum number of different character types required (upper, lower, digit, special).
        /// Ensures password complexity for better security.
        /// </summary>
        public const int MinimumCharacterTypes = 3;
        
        /// <summary>
        /// BCrypt cost factor for password hashing.
        /// Balances security and performance for password verification.
        /// </summary>
        public const int BCryptCostFactor = 12;
        
        /// <summary>
        /// Minimum password strength score required for acceptance.
        /// Ensures passwords meet basic security requirements.
        /// </summary>
        public const int MinimumPasswordScore = 50;
        
        /// <summary>
        /// Maximum allowed length for user full name field.
        /// Prevents excessively long names that could impact database performance.
        /// </summary>
        public const int MaxFullNameLength = 255;
    }

    /// <summary>
    /// Password scoring algorithm configuration.
    /// </summary>
    public static class PasswordScoring
    {
        /// <summary>
        /// Maximum points awarded for password length component.
        /// Ensures length contributes to but doesn't dominate score.
        /// </summary>
        public const int MaxLengthScore = 30;
        
        /// <summary>
        /// Points per character for length calculation.
        /// Rewards longer passwords without excessive weight.
        /// </summary>
        public const int PointsPerCharacter = 2;
        
        /// <summary>
        /// Points awarded for each character type present (upper, lower, digit, special).
        /// Encourages diverse character usage.
        /// </summary>
        public const int CharacterTypePoints = 10;
        
        /// <summary>
        /// Maximum points awarded for unique character diversity.
        /// Rewards character variety while capping contribution.
        /// </summary>
        public const int MaxUniqueCharScore = 20;
        
        /// <summary>
        /// Points per unique character for diversity calculation.
        /// Balances uniqueness importance in overall score.
        /// </summary>
        public const int PointsPerUniqueChar = 2;
        
        /// <summary>
        /// Maximum points awarded for password entropy.
        /// Caps entropy contribution to prevent skewing scores.
        /// </summary>
        public const int MaxEntropyScore = 10;
        
        /// <summary>
        /// Entropy divisor for calculating entropy points.
        /// Calibrates entropy impact on overall score.
        /// </summary>
        public const int EntropyDivisor = 5;
        
        /// <summary>
        /// Minimum score threshold for "Very Strong" password classification.
        /// Highest security tier for critical applications.
        /// </summary>
        public const int VeryStrongPasswordThreshold = 80;
        
        /// <summary>
        /// Minimum score threshold for "Strong" password classification.
        /// Industry-aligned threshold for secure passwords.
        /// </summary>
        public const int StrongPasswordThreshold = 60;
        
        /// <summary>
        /// Minimum score threshold for "Medium" password classification.
        /// Acceptable baseline for moderate security requirements.
        /// </summary>
        public const int MediumPasswordThreshold = 40;
        
        /// <summary>
        /// Minimum score threshold for "Weak" password classification.
        /// Basic threshold above very weak passwords.
        /// </summary>
        public const int WeakPasswordThreshold = 20;
    }
}