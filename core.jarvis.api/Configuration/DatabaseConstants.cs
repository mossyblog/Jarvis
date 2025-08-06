namespace core.jarvis.api.Configuration;

/// <summary>
/// Centralized configuration constants for database operations.
/// Provides standardized database connection and pooling parameters.
/// </summary>
public static class DatabaseConstants
{
    /// <summary>
    /// Database connection pooling configuration.
    /// </summary>
    public static class ConnectionPooling
    {
        /// <summary>
        /// Default maximum pool size for database connection pooling.
        /// Balances resource usage with concurrent request handling.
        /// </summary>
        public const int DefaultMaxPoolSize = 20;
        
        /// <summary>
        /// Default minimum pool size for database connection pooling.
        /// Ensures baseline connections are always available.
        /// </summary>
        public const int DefaultMinPoolSize = 5;
        
        /// <summary>
        /// Default connection lifetime in minutes for pooled connections.
        /// Prevents stale connections while maintaining efficiency.
        /// </summary>
        public const int DefaultConnectionLifetimeMinutes = 5;
    }

    /// <summary>
    /// Standard database server configuration.
    /// </summary>
    public static class ServerDefaults
    {
        /// <summary>
        /// Standard PostgreSQL port number.
        /// Default port for PostgreSQL database connections.
        /// </summary>
        public const int DefaultPostgreSqlPort = 5432;
        
        /// <summary>
        /// Standard localhost connection for development/testing.
        /// Used for local development environments.
        /// </summary>
        public const string LocalhostAddress = "localhost";
        
        /// <summary>
        /// Loopback IP address for local connections.
        /// Alternative to localhost for direct IP connections.
        /// </summary>
        public const string LoopbackAddress = "127.0.0.1";
    }

    /// <summary>
    /// Database field and constraint constants.
    /// </summary>
    public static class FieldLimits
    {
        /// <summary>
        /// Standard maximum length for email addresses in database schema.
        /// Accommodates most valid email addresses while preventing abuse.
        /// </summary>
        public const int MaxEmailLength = 255;
        
        /// <summary>
        /// Standard maximum length for password hash storage.
        /// Sufficient for BCrypt and other secure hashing algorithms.
        /// </summary>
        public const int MaxPasswordHashLength = 255;
        
        /// <summary>
        /// Standard maximum length for user name fields.
        /// Reasonable limit for display names and user identifiers.
        /// </summary>
        public const int MaxUserNameLength = 255;
    }
}