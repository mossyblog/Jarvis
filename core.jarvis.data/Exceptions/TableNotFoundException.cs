namespace core.jarvis.data.Exceptions;

/// <summary>
/// Exception thrown when a database table does not exist.
/// </summary>
public class TableNotFoundException : DataAccessException
{
    public string TableName { get; }

    public TableNotFoundException(string tableName)
        : base($"Table '{tableName}' not found")
    {
        TableName = tableName;
    }

    public TableNotFoundException(string tableName, Exception innerException)
        : base($"Table '{tableName}' not found", innerException)
    {
        TableName = tableName;
    }
}
