namespace core.jarvis.Data;

/// <summary>
/// Configuration options for database connection pooling.
/// </summary>
public class ConnectionPoolingOptions
{
    /// <summary>
    /// Configuration section name for binding.
    /// </summary>
    public const string SectionName = "Jarvis:Database:ConnectionPooling";
    
    /// <summary>
    /// Gets or sets whether connection pooling is enabled.
    /// Default: false (for backward compatibility).
    /// </summary>
    public bool Enabled { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the maximum number of connections allowed in the pool.
    /// Default: 20.
    /// </summary>
    public int MaxPoolSize { get; set; } = 20;
    
    /// <summary>
    /// Gets or sets the minimum number of connections maintained in the pool.
    /// Default: 5.
    /// </summary>
    public int MinPoolSize { get; set; } = 5;
    
    /// <summary>
    /// Gets or sets the maximum lifetime of a connection in minutes.
    /// Default: 5 minutes.
    /// </summary>
    public int ConnectionLifetimeMinutes { get; set; } = 5;
    
    /// <summary>
    /// Gets or sets the connection idle timeout in minutes.
    /// Default: 1 minute.
    /// </summary>
    public int ConnectionIdleTimeoutMinutes { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets whether to enable connection pool statistics logging.
    /// Default: false.
    /// </summary>
    public bool EnableStatisticsLogging { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the interval in seconds for logging pool statistics.
    /// Only used when EnableStatisticsLogging is true.
    /// Default: 60 seconds.
    /// </summary>
    public int StatisticsLogIntervalSeconds { get; set; } = 60;
    
    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <returns>A list of validation errors, if any.</returns>
    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();
        
        if (MaxPoolSize < 1)
            errors.Add("MaxPoolSize must be at least 1");
            
        if (MinPoolSize < 0)
            errors.Add("MinPoolSize must be non-negative");
            
        if (MinPoolSize > MaxPoolSize)
            errors.Add("MinPoolSize cannot be greater than MaxPoolSize");
            
        if (ConnectionLifetimeMinutes < 1)
            errors.Add("ConnectionLifetimeMinutes must be at least 1");
            
        if (ConnectionIdleTimeoutMinutes < 1)
            errors.Add("ConnectionIdleTimeoutMinutes must be at least 1");
            
        if (StatisticsLogIntervalSeconds < 1)
            errors.Add("StatisticsLogIntervalSeconds must be at least 1");
            
        return errors;
    }
}