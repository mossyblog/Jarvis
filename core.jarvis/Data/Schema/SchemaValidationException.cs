namespace core.jarvis.Data.Schema;

/// <summary>
/// Exception thrown when database schema validation fails due to incompatible field types
/// requiring manual migration strategy.
/// </summary>
public class SchemaValidationException : Exception
{
    public string TableName { get; }
    public string FieldName { get; }
    public string ExpectedType { get; }
    public string ActualType { get; }

    public SchemaValidationException(
        string tableName, 
        string fieldName, 
        string expectedType, 
        string actualType)
        : base($"Schema validation failed for table '{tableName}', field '{fieldName}'. " +
               $"Expected type '{expectedType}' but found '{actualType}'. " +
               $"Manual migration required.")
    {
        TableName = tableName;
        FieldName = fieldName;
        ExpectedType = expectedType;
        ActualType = actualType;
    }
    
    public SchemaValidationException(string tableName, string message)
        : base($"Schema validation failed for table '{tableName}': {message}")
    {
        TableName = tableName;
        FieldName = string.Empty;
        ExpectedType = string.Empty;
        ActualType = string.Empty;
    }
}