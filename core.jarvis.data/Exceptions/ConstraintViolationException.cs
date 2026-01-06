namespace core.jarvis.data.Exceptions;

/// <summary>
/// Type of database constraint.
/// </summary>
public enum ConstraintType
{
    Unique,
    ForeignKey,
    Check
}

/// <summary>
/// Exception thrown when a database constraint is violated.
/// </summary>
public class ConstraintViolationException : DataAccessException
{
    public string TableName { get; }
    public string SqlState { get; }
    public ConstraintType ConstraintType { get; }

    public ConstraintViolationException(
        string tableName,
        string sqlState,
        ConstraintType constraintType,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        TableName = tableName;
        SqlState = sqlState;
        ConstraintType = constraintType;
    }
}
