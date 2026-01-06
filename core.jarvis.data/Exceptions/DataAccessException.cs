namespace core.jarvis.data.Exceptions;

/// <summary>
/// Base exception for data access errors.
/// </summary>
public class DataAccessException : Exception
{
    public DataAccessException(string message) : base(message) { }
    public DataAccessException(string message, Exception innerException) : base(message, innerException) { }
}
