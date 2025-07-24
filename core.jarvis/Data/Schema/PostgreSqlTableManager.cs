using System.Reflection;
using System.Text;
using System.Text.Json;
using core.jarvis.Data.Components;
using core.jarvis.data;
using Microsoft.Extensions.Logging;

namespace core.jarvis.Data.Schema;

/// <summary>
/// PostgreSQL implementation of table schema management for components.
/// </summary>
public class PostgreSqlTableManager : ITableManager
{
    private readonly IPgClient _pgClient;
    private readonly ILogger<PostgreSqlTableManager> _logger;

    public PostgreSqlTableManager(IPgClient pgClient, ILogger<PostgreSqlTableManager> logger)
    {
        _pgClient = pgClient ?? throw new ArgumentNullException(nameof(pgClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EnsureTableExists<TComponent>() where TComponent : class, IComponent, new()
    {
        var componentType = typeof(TComponent);
        var tableName = GetTableName(componentType);
        
        _logger.LogDebug("Ensuring table exists for component {ComponentType} -> table {TableName}", 
            componentType.Name, tableName);

        var tableExists = await CheckTableExists(tableName);
        
        if (!tableExists)
        {
            _logger.LogInformation("Creating table {TableName} for component {ComponentType}", 
                tableName, componentType.Name);
            await CreateTable<TComponent>();
            return;
        }

        // Table exists - validate schema
        _logger.LogDebug("Table {TableName} exists, validating schema", tableName);
        await ValidateAndUpdateTableSchema<TComponent>();
    }

    public async Task ValidateAllComponentTables()
    {
        // This would be called during application startup to validate all registered components
        _logger.LogInformation("Validating all component tables - implement based on your component registry");
        // Implementation would iterate through all registered component types
        // and call EnsureTableExists for each one
    }

    private async Task<bool> CheckTableExists(string tableName)
    {
        var query = @"
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_name = @tableName
            )";
        
        try
        {
            var result = await _pgClient.ExecuteScalar<bool>(
                query, 
                new { tableName = tableName.ToLowerInvariant() });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if table {TableName} exists", tableName);
            throw;
        }
    }

    private async Task CreateTable<TComponent>() where TComponent : class, IComponent, new()
    {
        var componentType = typeof(TComponent);
        var tableName = GetTableName(componentType);
        var fields = GetExpectedFields(componentType);

        var createTableSql = BuildCreateTableSql(tableName, fields);
        
        try
        {
            await _pgClient.Execute(createTableSql);
            _logger.LogInformation("Successfully created table {TableName}", tableName);
            
            // Create indexes
            await CreateIndexes(tableName, fields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create table {TableName}", tableName);
            throw;
        }
    }

    private async Task ValidateAndUpdateTableSchema<TComponent>() where TComponent : class, IComponent, new()
    {
        var componentType = typeof(TComponent);
        var tableName = GetTableName(componentType);
        var expectedFields = GetExpectedFields(componentType);
        var actualColumns = await GetActualColumns(tableName);

        var missingFields = new List<ComponentFieldInfo>();
        var incompatibleFields = new List<(ComponentFieldInfo expected, DatabaseColumnInfo actual)>();

        foreach (var expectedField in expectedFields)
        {
            var actualColumn = actualColumns.FirstOrDefault(c => 
                c.ColumnName.Equals(expectedField.ColumnName, StringComparison.OrdinalIgnoreCase));

            if (actualColumn == null)
            {
                // Field is missing - we can add it
                _logger.LogDebug("Field {ColumnName} not found in table {TableName}, will add it", 
                    expectedField.ColumnName, tableName);
                missingFields.Add(expectedField);
            }
            else
            {
                // Field exists - check compatibility
                _logger.LogDebug("Field {ColumnName} exists in table {TableName}, checking compatibility", 
                    expectedField.ColumnName, tableName);
                if (!AreTypesCompatible(expectedField, actualColumn))
                {
                    incompatibleFields.Add((expectedField, actualColumn));
                }
            }
        }

        // Handle incompatible fields - throw exception
        if (incompatibleFields.Any())
        {
            var first = incompatibleFields.First();
            throw new SchemaValidationException(
                tableName, 
                first.expected.ColumnName, 
                first.expected.PostgreSqlType, 
                first.actual.DataType);
        }

        // Add missing fields
        if (missingFields.Any())
        {
            await AddMissingFields(tableName, missingFields);
        }
    }

    private async Task<List<DatabaseColumnInfo>> GetActualColumns(string tableName)
    {
        var query = @"
            SELECT 
                c.column_name,
                c.data_type,
                c.is_nullable = 'YES' as is_nullable,
                c.column_default,
                CASE WHEN pk.column_name IS NOT NULL THEN true ELSE false END as is_primary_key,
                CASE WHEN uk.column_name IS NOT NULL THEN true ELSE false END as is_unique
            FROM information_schema.columns c
            LEFT JOIN (
                SELECT kcu.column_name
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu 
                    ON tc.constraint_name = kcu.constraint_name
                WHERE tc.table_name = @tableName 
                    AND tc.constraint_type = 'PRIMARY KEY'
            ) pk ON c.column_name = pk.column_name
            LEFT JOIN (
                SELECT kcu.column_name
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu 
                    ON tc.constraint_name = kcu.constraint_name
                WHERE tc.table_name = @tableName 
                    AND tc.constraint_type = 'UNIQUE'
            ) uk ON c.column_name = uk.column_name
            WHERE c.table_name = @tableName
            ORDER BY c.ordinal_position";

        try
        {
            var result = await _pgClient.Query<DatabaseColumnInfo>(
                query, 
                new { tableName = tableName.ToLowerInvariant() });
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get column information for table {TableName}", tableName);
            throw;
        }
    }

    private async Task AddMissingFields(string tableName, List<ComponentFieldInfo> missingFields)
    {
        _logger.LogInformation("Adding {Count} missing fields to table {TableName}", 
            missingFields.Count, tableName);

        foreach (var field in missingFields)
        {
            // Use IF NOT EXISTS to safely add column
            var alterSql = $@"
                DO $$ 
                BEGIN 
                    BEGIN
                        ALTER TABLE {tableName} ADD COLUMN {BuildColumnDefinition(field)};
                    EXCEPTION
                        WHEN duplicate_column THEN RAISE NOTICE 'column {field.ColumnName} already exists in {tableName}.';
                    END;
                END;
                $$";
            
            try
            {
                await _pgClient.Execute(alterSql);
                _logger.LogDebug("Ensured column {ColumnName} exists in table {TableName}", 
                    field.ColumnName, tableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure column {ColumnName} exists in table {TableName}", 
                    field.ColumnName, tableName);
                throw;
            }
        }
    }

    private string GetTableName(Type componentType)
    {
        // Convert ComponentName to snake_case + _component suffix
        var name = componentType.Name;
        
        // Use the same ToSnakeCase implementation as PgTable for consistency
        var snakeCase = name.ToSnakeCase();
        
        return $"{snakeCase}_component";
    }

    private List<ComponentFieldInfo> GetExpectedFields(Type componentType)
    {
        var fields = new List<ComponentFieldInfo>();
        var properties = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // Skip computed properties
            if (!prop.CanWrite || prop.GetSetMethod() == null)
                continue;

            var field = new ComponentFieldInfo
            {
                PropertyName = prop.Name,
                ColumnName = ConvertToSnakeCase(prop.Name),
                PropertyType = prop.PropertyType,
                IsNullable = IsNullableType(prop.PropertyType),
                IsPrimaryKey = prop.Name == "Id",
                IsUnique = prop.Name == "Id" || prop.Name == "OwnerEntityId",
                PostgreSqlType = GetPostgreSqlType(prop.PropertyType),
                DefaultValue = GetDefaultValue(prop.PropertyType, prop.Name)
            };

            fields.Add(field);
        }

        return fields;
    }

    private string ConvertToSnakeCase(string pascalCase)
    {
        // Use the same ToSnakeCase implementation as PgTable for consistency
        return pascalCase.ToSnakeCase();
    }

    private bool IsNullableType(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null || 
               !type.IsValueType ||
               type == typeof(string);
    }

    private string GetPostgreSqlType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType switch
        {
            Type t when t == typeof(Guid) => "UUID",
            Type t when t == typeof(string) => "TEXT",
            Type t when t == typeof(int) => "INTEGER",
            Type t when t == typeof(long) => "BIGINT",
            Type t when t == typeof(decimal) => "DECIMAL(18,8)",
            Type t when t == typeof(double) => "DOUBLE PRECISION",
            Type t when t == typeof(float) => "REAL",
            Type t when t == typeof(bool) => "BOOLEAN",
            Type t when t == typeof(DateTime) => "TIMESTAMPTZ",
            Type t when t.IsEnum => "INTEGER",
            Type t when t.IsArray && t.GetElementType() == typeof(Guid) => "UUID[]",
            Type t when t.IsArray && t.GetElementType() == typeof(string) => "TEXT[]",
            Type t when t.IsArray && t.GetElementType() == typeof(int) => "INTEGER[]",
            _ => "JSONB" // Complex objects as JSONB
        };
    }

    private string? GetDefaultValue(Type type, string propertyName)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return propertyName switch
        {
            "Id" => "gen_random_uuid()",
            "OwnerEntityId" => "'00000000-0000-0000-0000-000000000000'::uuid",
            "LastUpdated" or "CreatedAt" or "UpdatedAt" => "NOW()",
            "Version" => "1",
            _ when underlyingType == typeof(bool) => "FALSE",
            _ when underlyingType == typeof(decimal) || underlyingType == typeof(double) || underlyingType == typeof(float) => "0",
            _ when underlyingType == typeof(int) || underlyingType == typeof(long) => "0",
            _ when underlyingType == typeof(string) => "''",
            _ when underlyingType.IsArray && underlyingType.GetElementType() == typeof(Guid) => "'{}'",
            _ when underlyingType.IsEnum => "0",
            _ => null
        };
    }

    private bool AreTypesCompatible(ComponentFieldInfo expected, DatabaseColumnInfo actual)
    {
        var normalizedExpected = NormalizePostgreSqlType(expected.PostgreSqlType);
        var normalizedActual = NormalizePostgreSqlType(actual.DataType);

        return normalizedExpected == normalizedActual &&
               expected.IsNullable == actual.IsNullable;
    }

    private string NormalizePostgreSqlType(string type)
    {
        return type.ToUpperInvariant() switch
        {
            "TEXT" => "TEXT",
            "CHARACTER VARYING" => "TEXT",
            "VARCHAR" => "TEXT",
            "TIMESTAMP WITH TIME ZONE" => "TIMESTAMPTZ",
            "TIMESTAMPTZ" => "TIMESTAMPTZ",
            "TIMESTAMP WITHOUT TIME ZONE" => "TIMESTAMP",
            "BOOLEAN" => "BOOLEAN",
            "INTEGER" => "INTEGER",
            "BIGINT" => "BIGINT",
            "UUID" => "UUID",
            "JSONB" => "JSONB",
            "DECIMAL" => "DECIMAL",
            "NUMERIC" => "DECIMAL",
            _ => type.ToUpperInvariant()
        };
    }

    private string BuildCreateTableSql(string tableName, List<ComponentFieldInfo> fields)
    {
        var sql = new StringBuilder();
        sql.AppendLine($"CREATE TABLE {tableName} (");

        var columnDefinitions = fields.Select(BuildColumnDefinition);
        sql.AppendLine(string.Join(",\n    ", columnDefinitions.Select(def => $"    {def}")));

        sql.AppendLine(");");

        return sql.ToString();
    }

    private string BuildColumnDefinition(ComponentFieldInfo field)
    {
        var definition = new StringBuilder();
        definition.Append($"{field.ColumnName} {field.PostgreSqlType}");

        if (field.IsPrimaryKey)
        {
            definition.Append(" PRIMARY KEY");
        }
        else if (field.IsUnique)
        {
            definition.Append(" UNIQUE");
        }

        if (!field.IsNullable && !field.IsPrimaryKey)
        {
            definition.Append(" NOT NULL");
        }

        if (!string.IsNullOrEmpty(field.DefaultValue))
        {
            definition.Append($" DEFAULT {field.DefaultValue}");
        }

        return definition.ToString();
    }

    private async Task CreateIndexes(string tableName, List<ComponentFieldInfo> fields)
    {
        var indexFields = fields.Where(f => 
            f.PropertyName == "OwnerEntityId" || 
            f.PropertyName == "LastUpdated" ||
            f.PropertyName.EndsWith("Status") ||
            f.PropertyName.EndsWith("Type")).ToList();

        foreach (var field in indexFields)
        {
            var indexName = $"idx_{tableName}_{field.ColumnName}";
            var indexSql = $"CREATE INDEX IF NOT EXISTS {indexName} ON {tableName}({field.ColumnName})";
            
            try
            {
                await _pgClient.Execute(indexSql);
                _logger.LogDebug("Created index {IndexName} on {TableName}.{ColumnName}", 
                    indexName, tableName, field.ColumnName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create index {IndexName}", indexName);
                // Don't fail on index creation errors
            }
        }
    }
}