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
        
        // Check if table exists (debug logging removed for reduced verbosity)

        var tableExists = await CheckTableExists(tableName);
        
        if (!tableExists)
        {
            // Creating table - only log errors, not routine operations
            await CreateTable<TComponent>();
            return;
        }

        // Table exists - validate schema
        await ValidateAndUpdateTableSchema<TComponent>();
    }

    public Task ValidateAllComponentTables()
    {
        // This would be called during application startup to validate all registered components
        // Validating all component tables - implement based on your component registry
        // Implementation would iterate through all registered component types
        // and call EnsureTableExists for each one
        return Task.CompletedTask;
    }

    public async Task EnsureSnapshotsTableExists()
    {
        const string tableName = "component_snapshots";

        var tableExists = await CheckTableExists(tableName);
        if (tableExists)
            return;

        const string createTableSql = @"
            CREATE TABLE IF NOT EXISTS component_snapshots (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                entity_id UUID NOT NULL,
                component_type TEXT NOT NULL,
                component_id UUID NOT NULL,
                snapshots JSONB DEFAULT '[]'::jsonb,
                created_at TIMESTAMPTZ DEFAULT NOW(),
                last_updated TIMESTAMPTZ DEFAULT NOW(),
                UNIQUE(component_id, component_type)
            )";

        try
        {
            await _pgClient.Execute(createTableSql);

            // Create indexes for common query patterns
            await _pgClient.Execute("CREATE INDEX IF NOT EXISTS idx_component_snapshots_entity_id ON component_snapshots(entity_id)");
            await _pgClient.Execute("CREATE INDEX IF NOT EXISTS idx_component_snapshots_component_type ON component_snapshots(component_type)");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Snapshots table may already exist");
        }
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

        // Debug: Log IsAuditOrHistoryComponent result and generated SQL
        _logger.LogDebug("Creating table {TableName} for {ComponentType}. IsAuditOrHistoryComponent={IsAudit}",
            tableName, componentType.Name, IsAuditOrHistoryComponent(componentType));
        var ownerEntityIdField = fields.FirstOrDefault(f => f.PropertyName == "OwnerEntityId");
        if (ownerEntityIdField != null)
        {
            _logger.LogDebug("OwnerEntityId field: IsUnique={IsUnique}", ownerEntityIdField.IsUnique);
        }
        _logger.LogDebug("Generated SQL: {SQL}", createTableSql);

        try
        {
            await _pgClient.Execute(createTableSql);
            // Table created successfully
            
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
                missingFields.Add(expectedField);
            }
            else
            {
                // Field exists - check compatibility
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
        // Adding missing fields - routine operation, no logging needed

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
                // Column ensured
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
        // Convert ComponentName to snake_case
        var name = componentType.Name;
        
        // Use the same ToSnakeCase implementation as PgTable for consistency
        var snakeCase = name.ToSnakeCase();
        
        // If the name already ends with "_component", don't add it again
        if (snakeCase.EndsWith("_component"))
        {
            return snakeCase;
        }
        
        // Otherwise add the _component suffix
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
                // AuditEvent and similar types should allow multiple entries per entity
                IsUnique = prop.Name == "Id" || (prop.Name == "OwnerEntityId" && !IsAuditOrHistoryComponent(componentType)),
                PostgreSqlType = GetPostgreSqlType(prop.PropertyType),
                DefaultValue = GetDefaultValue(prop.PropertyType, prop.Name)
            };

            fields.Add(field);
        }

        return fields;
    }

    private bool IsAuditOrHistoryComponent(Type componentType)
    {
        // Components that should allow multiple entries per entity:
        // 1. Audit/history/logging components
        // 2. Content components (posts, comments, etc.)
        // 3. Transaction-like components
        var typeName = componentType.Name.ToLower();
        return typeName.Contains("audit") || 
               typeName.Contains("history") || 
               typeName.Contains("log") ||
               typeName.Contains("event") ||
               typeName.Contains("snapshot") ||
               typeName.Contains("post") ||
               typeName.Contains("comment") ||
               typeName.Contains("transaction") ||
               typeName.Contains("record") ||
               typeName.Contains("entry");
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

    private bool TryGetCollectionElementType(Type type, out Type? elementType)
    {
        elementType = null;

        // Check if it's an array
        if (type.IsArray)
        {
            elementType = type.GetElementType();
            return elementType != null;
        }

        // Check if it's a generic collection
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();

            // Check for common collection interfaces and implementations
            if (genericDef == typeof(List<>) ||
                genericDef == typeof(IList<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(HashSet<>) ||
                genericDef == typeof(ISet<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }
        }

        // Check if it implements IEnumerable<T> (covers custom collections)
        var enumerableInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                                 i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableInterface != null)
        {
            elementType = enumerableInterface.GetGenericArguments()[0];
            return true;
        }

        return false;
    }

    private string? GetPostgreSqlArrayType(Type elementType)
    {
        return elementType switch
        {
            Type t when t == typeof(int) => "INTEGER[]",
            Type t when t == typeof(long) => "BIGINT[]",
            Type t when t == typeof(short) => "SMALLINT[]",
            Type t when t == typeof(byte) => "SMALLINT[]",
            Type t when t == typeof(decimal) => "DECIMAL(18,8)[]",
            Type t when t == typeof(double) => "DOUBLE PRECISION[]",
            Type t when t == typeof(float) => "REAL[]",
            Type t when t == typeof(bool) => "BOOLEAN[]",
            Type t when t == typeof(string) => "TEXT[]",
            Type t when t == typeof(Guid) => "UUID[]",
            Type t when t == typeof(DateTime) => "TIMESTAMPTZ[]",
            Type t when t.IsEnum => "INTEGER[]",
            _ => null // Return null for complex types that can't be PostgreSQL arrays
        };
    }

    private string GetPostgreSqlType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        // First check if it's a collection type (List<T>, IList<T>, T[], etc.)
        if (TryGetCollectionElementType(underlyingType, out var elementType) && elementType != null)
        {
            // Try to map to PostgreSQL array type
            var arrayType = GetPostgreSqlArrayType(elementType);
            if (arrayType != null)
            {
                return arrayType;
            }
            // If element type is complex, fall through to JSONB
        }

        // Handle non-collection types
        return underlyingType switch
        {
            Type t when t == typeof(Guid) => "UUID",
            Type t when t == typeof(string) => "TEXT",
            Type t when t == typeof(int) => "INTEGER",
            Type t when t == typeof(long) => "BIGINT",
            Type t when t == typeof(short) => "SMALLINT",
            Type t when t == typeof(byte) => "SMALLINT",
            Type t when t == typeof(decimal) => "DECIMAL(18,8)",
            Type t when t == typeof(double) => "DOUBLE PRECISION",
            Type t when t == typeof(float) => "REAL",
            Type t when t == typeof(bool) => "BOOLEAN",
            Type t when t == typeof(DateTime) => "TIMESTAMPTZ",
            Type t when t.IsEnum => "INTEGER",
            _ => "JSONB" // Complex objects as JSONB
        };
    }

    private string? GetDefaultValue(Type type, string propertyName)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        // Check if it's a collection that maps to PostgreSQL array
        if (TryGetCollectionElementType(underlyingType, out var elementType) && elementType != null)
        {
            // For any collection that maps to a PostgreSQL array, use empty array syntax
            var arrayType = GetPostgreSqlArrayType(elementType);
            if (arrayType != null)
            {
                return "'{}'"; // PostgreSQL empty array literal
            }
        }

        return propertyName switch
        {
            "Id" => "gen_random_uuid()",
            "OwnerEntityId" => "'00000000-0000-0000-0000-000000000000'::uuid",
            "LastUpdated" or "CreatedAt" or "UpdatedAt" => "NOW()",
            "Version" => "1",
            _ when underlyingType == typeof(bool) => "FALSE",
            _ when underlyingType == typeof(decimal) || underlyingType == typeof(double) || underlyingType == typeof(float) => "0",
            _ when underlyingType == typeof(int) || underlyingType == typeof(long) || underlyingType == typeof(short) || underlyingType == typeof(byte) => "0",
            _ when underlyingType == typeof(string) => "''",
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
                // Index created
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create index {IndexName}", indexName);
                // Don't fail on index creation errors
            }
        }
    }
}