namespace core.jarvis.Data.Schema;

/// <summary>
/// Represents expected database field information for a component property.
/// </summary>
public class ComponentFieldInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string PostgreSqlType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsUnique { get; set; }
    public string? DefaultValue { get; set; }
    public Type PropertyType { get; set; } = typeof(object);
}

/// <summary>
/// Represents actual database column information from PostgreSQL system tables.
/// </summary>
public class DatabaseColumnInfo
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsUnique { get; set; }
}