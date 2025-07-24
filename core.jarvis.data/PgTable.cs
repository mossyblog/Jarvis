using core.jarvis.data.RLS;
using Dapper;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using core.jarvis.data;

namespace core.jarvis.data;

/// <summary>
/// Provides a strongly-typed, secure, and convention-based interface for interacting with PostgreSQL tables.
/// 
/// Key Features:
/// - **Type Safety**: Strongly-typed operations with automatic C# ↔ PostgreSQL type mapping
/// - **Security**: SQL injection protection via whitelisting and parameterized queries
/// - **Convention-Based**: Automatic PascalCase ↔ snake_case property/column mapping
/// - **PostgreSQL Native**: Specialized handling for UUID arrays, JSONB columns, and PostgreSQL types
/// - **Row Level Security**: SDK-level and database-level RLS policy enforcement
/// - **Performance**: Optimized queries with proper parameter binding and connection management
/// 
/// PostgreSQL Type Mappings:
/// - C# Guid ↔ PostgreSQL UUID (with proper parameter binding)
/// - C# Guid[] ↔ PostgreSQL UUID[] (handles various Npgsql representations)
/// - C# objects ↔ PostgreSQL JSONB (automatic JSON serialization/deserialization)
/// - Standard .NET types ↔ PostgreSQL equivalents (int, string, DateTime, etc.)
/// 
/// Security Architecture:
/// - Column names validated against entity properties (prevents injection)
/// - Operators limited to safe whitelist (eq, neq, lt, lte, gt, gte)
/// - All values parameterized (never concatenated into SQL)
/// - RLS policies enforced at SDK level before database queries
/// - JWT claims integration for user context and permissions
/// 
/// Usage Example:
/// <code>
/// var users = await client.From&lt;User&gt;()
///     .Filter("is_active", "eq", true)
///     .Filter("created_at", "gte", DateTime.Today)
///     .Get();
/// 
/// await client.From&lt;User&gt;().Insert(newUser);
/// await client.From&lt;User&gt;().Upsert(existingUser);
/// </code>
/// </summary>
/// <typeparam name="T">The entity type mapped to the PostgreSQL table. Must be a class with a parameterless constructor.</typeparam>
public class PgTable<T> where T : class, new()
{
    private readonly NpgsqlConnection _conn;
    private readonly PgClient? _client;
    private readonly RLSPolicyRegistry _rlsPolicies;
    private readonly Dictionary<string, string> _jwtClaims;
    private readonly string _tableName;
    private readonly List<string> _whereClauses = new();
    private readonly DynamicParameters _parameters = new();
    private readonly ILogger? _logger;

    // Whitelist of allowed operators for filtering (prevents SQL injection)
    private static readonly HashSet<string> AllowedOperators = new() { "eq", "neq", "lt", "lte", "gt", "gte" };

    // Performance-optimized caches for expensive reflection operations
    private static readonly Lazy<PropertyInfo[]> _writableProperties = new(() => 
        typeof(T).GetProperties().Where(p => p.CanWrite).ToArray());
    
    private static readonly Lazy<HashSet<string>> _allowedColumns = new(() =>
        _writableProperties.Value.Select(p => core.jarvis.data.StringExtensions.ToSnakeCase(p.Name)).ToHashSet());
        
    private static readonly Dictionary<string, string> _propertyToColumnMap;

    static PgTable()
    {
        _propertyToColumnMap = typeof(T).GetProperties()
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name, p => core.jarvis.data.StringExtensions.ToSnakeCase(p.Name));
    }

    // Cached values for high-performance access
    private static PropertyInfo[] WritableProperties => _writableProperties.Value;
    private static HashSet<string> AllowedColumns => _allowedColumns.Value;
    private static Dictionary<string, string> PropertyToColumnMap => _propertyToColumnMap;

    /// <summary>
    /// Initializes a new instance of PgTable for the specified entity type.
    /// 
    /// Table Name Resolution:
    /// - Regular entities: EntityName → "entity_name" (quoted for reserved words)
    /// - ECS Components: ComponentName → "component_name_component" (follows ECS conventions)
    /// - Names are automatically converted to snake_case and quoted to handle PostgreSQL reserved words
    /// 
    /// Security Integration:
    /// - RLS policies are applied at the SDK level before database operations
    /// - JWT claims provide user context for both SDK and database-level security
    /// - Column access is restricted to entity properties (prevents injection via column names)
    /// </summary>
    /// <param name="conn">The active Npgsql database connection. Must be configured for the target database.</param>
    /// <param name="client">Optional PgClient for JWT/RLS context. Enables database-level RLS when provided.</param>
    /// <param name="rlsPolicies">RLS policy registry for SDK-level security enforcement. Uses empty registry if null.</param>
    /// <param name="jwtClaims">JWT claims dictionary for user context and permissions. Uses empty dictionary if null.</param>
    /// <param name="logger">Optional logger for debugging and diagnostics.</param>
    public PgTable(NpgsqlConnection conn, PgClient? client = null, RLSPolicyRegistry? rlsPolicies = null, Dictionary<string, string>? jwtClaims = null, ILogger? logger = null)
    {
        _conn = conn;
        _client = client;
        _rlsPolicies = rlsPolicies ?? new RLSPolicyRegistry();
        _jwtClaims = jwtClaims ?? new Dictionary<string, string>();
        _logger = logger;
        
        // PostgreSQL table name resolution with ECS (Entity Component System) support
        var typeName = typeof(T).Name;
        var snakeCaseName = core.jarvis.data.StringExtensions.ToSnakeCase(typeName);
        
        // Check if this is a component type (implements IComponent)
        var isComponent = typeof(T).GetInterfaces().Any(i => i.Name == "IComponent");
        if (isComponent)
        {
            // ECS components use table_name_component convention in PostgreSQL
            // Example: UserProfile component → "user_profile_component" table
            _tableName = $"\"{snakeCaseName}_component\"";
        }
        else
        {
            // Quote all table names to handle PostgreSQL reserved words safely
            // PostgreSQL has many reserved words (user, order, group, etc.) that need quoting
            _tableName = $"\"{snakeCaseName}\"";
        }
    }

    /// <summary>
    /// Inserts a new entity into the PostgreSQL table with comprehensive security and type handling.
    /// 
    /// Security Enforcement:
    /// - RLS policies are checked before database operation (SDK-level enforcement)
    /// - All values are parameterized to prevent SQL injection
    /// - Only entity properties can be inserted (column whitelisting)
    /// - JWT claims are set for database-level RLS if client is provided
    /// 
    /// Column Mapping:
    /// - C# PascalCase properties → PostgreSQL snake_case columns
    /// - JSONB columns are automatically JSON-serialized
    /// - PostgreSQL arrays (UUID[], int[]) are properly parameter-bound
    /// 
    /// ID Handling:
    /// - Excludes 'Id' property if it has default value (0 for int, empty for Guid)
    /// - Allows explicit ID insertion if non-default value is provided
    /// - Supports both auto-increment and manually assigned IDs
    /// 
    /// Transaction Safety:
    /// - Operation respects existing transaction context
    /// - RLS enforcement may silently prevent insertion without throwing exceptions
    /// </summary>
    /// <param name="entity">The entity to insert. All writable properties will be mapped to table columns.</param>
    /// <exception cref="PostgresException">Thrown for database-level errors (constraint violations, etc.).</exception>
    public async Task Insert(T entity)
    {
        // Check RLS policies for insert operation - use cached properties for performance
        var entityData = new Dictionary<string, object>();
        foreach (var prop in WritableProperties)
        {
            var value = prop.GetValue(entity);
            if (value != null)
            {
                // Use cached property-to-column mapping
                entityData[PropertyToColumnMap[prop.Name]] = value;
            }
        }

        // Check if insert is allowed by RLS policies (use unquoted table name)
        var unquotedTableName = core.jarvis.data.StringExtensions.ToSnakeCase(typeof(T).Name);
        if (!_rlsPolicies.CheckOperation(unquotedTableName, PolicyType.Insert, _jwtClaims, entityData))
        {
            // Silently fail like PostgreSQL RLS would
            return;
        }

        // Get all properties and check if Id should be excluded
        var idProperty = typeof(T).GetProperty("Id");
        var shouldExcludeId = false;
        
        if (idProperty != null)
        {
            var idValue = idProperty.GetValue(entity);
            // Exclude Id if it's the default value (0 for int, empty guid for Guid)
            if ((idProperty.PropertyType == typeof(int) && (int)(idValue ?? 0) == 0) ||
                (idProperty.PropertyType == typeof(Guid) && (Guid)(idValue ?? Guid.Empty) == Guid.Empty))
            {
                shouldExcludeId = true;
            }
        }
        
        // Use cached properties and filter for ID exclusion if needed
        var props = shouldExcludeId 
            ? WritableProperties.Where(p => p.Name != "Id").ToArray()
            : WritableProperties;
            
        var columns = string.Join(", ", props.Select(p => PropertyToColumnMap[p.Name]));
        var values = string.Join(", ", props.Select(p => 
        {
            var columnName = PropertyToColumnMap[p.Name];
            // PostgreSQL JSONB type casting for complex objects
            // The ::jsonb cast ensures proper JSON storage and indexing in PostgreSQL
            if (PostgreSqlTypeConverter.IsJsonColumn(columnName, p.PropertyType))
                return $"@{p.Name}::jsonb";
            return $"@{p.Name}";
        }));
        var sql = $"INSERT INTO {_tableName} ({columns}) VALUES ({values})";

        await EnsureConnectionOpen();

        // Set JWT claims for RLS if client is provided
        if (_client != null)
        {
            await _client.JWTClaims();
        }

        // Convert entity to parameter dictionary with JSON serialization for complex objects
        var parameters = ConvertEntityToParameters(entity);
        await _conn.ExecuteAsync(sql, parameters);
    }

    /// <summary>
    /// Adds a secure filter clause to the query with comprehensive validation and parameter binding.
    /// 
    /// Security Features:
    /// - Column names validated against entity properties (prevents SQL injection via column names)
    /// - Operators limited to safe whitelist (prevents injection via operators)
    /// - All values parameterized (prevents injection via values)
    /// - Supports method chaining for complex filter combinations
    /// 
    /// Column Name Mapping:
    /// - Use snake_case column names that correspond to entity properties
    /// - Example: For property "FirstName", use column "first_name"
    /// - Example: For property "CreatedAt", use column "created_at"
    /// 
    /// Supported Operators:
    /// - "eq": Equals (=)
    /// - "neq": Not equals (&lt;&gt;)
    /// - "lt": Less than (&lt;)
    /// - "lte": Less than or equal (&lt;=)
    /// - "gt": Greater than (&gt;)
    /// - "gte": Greater than or equal (&gt;=)
    /// 
    /// PostgreSQL Type Handling:
    /// - Guid values automatically mapped to UUID type
    /// - DateTime values properly formatted for PostgreSQL
    /// - Numeric types maintain precision
    /// 
    /// Usage Examples:
    /// <code>
    /// .Filter("is_active", "eq", true)
    /// .Filter("created_at", "gte", DateTime.Today)
    /// .Filter("user_id", "eq", userId)
    /// </code>
    /// </summary>
    /// <param name="column">The column name in snake_case format. Must correspond to an entity property.</param>
    /// <param name="op">The comparison operator. Must be one of: eq, neq, lt, lte, gt, gte.</param>
    /// <param name="value">The value to compare against. Will be properly parameterized and type-converted.</param>
    /// <returns>The current PgTable instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if column name is not allowed (not found in entity properties) or operator is not in the whitelist.</exception>
    public PgTable<T> Filter(string column, string op, object value)
    {
        // Validate column and operator to prevent SQL injection
        if (!AllowedColumns.Contains(column))
            throw new ArgumentException($"Column '{column}' is not allowed.");
        if (!AllowedOperators.Contains(op))
            throw new ArgumentException($"Operator '{op}' is not allowed.");

        string paramName = $"param_{_parameters.ParameterNames.Count()}";
        _whereClauses.Add($"{column} {TranslateOperator(op)} @{paramName}");
        _parameters.Add(paramName, value);
        return this;
    }

    /// <summary>
    /// Executes the SELECT query with any applied filters and returns the result set.
    /// Enforces RLS policies at the SDK level by adding WHERE clauses.
    /// 
    /// This method coordinates the entire query process:
    /// 1. Builds the SQL query with proper column mappings
    /// 2. Applies RLS security policies
    /// 3. Executes the query with enhanced parameter handling
    /// 4. Converts results to strongly-typed entities
    /// </summary>
    /// <returns>List of entities matching the query conditions and security policies.</returns>
    public async Task<List<T>> Get()
    {
        // Step 1: Build the SELECT query with column mappings and WHERE clauses
        var sql = BuildSelectQuery();
        
        // Step 2: Prepare connection and security context
        await PrepareQueryExecution();
        
        // Step 3: Execute query with enhanced parameter handling
        var dynamicResult = await ExecuteQuery(sql);
        
        // Step 4: Convert dynamic results to strongly-typed entities
        var typedResults = ConvertDynamicResultsToEntities(dynamicResult);
        
        return typedResults;
    }

    /// <summary>
    /// Builds the SELECT SQL query with proper column mappings and WHERE clauses.
    /// 
    /// Column Mapping Strategy:
    /// - Maps C# PascalCase properties to PostgreSQL snake_case columns
    /// - Uses AS aliases to preserve property names in results
    /// - Excludes computed properties (those without setters)
    /// 
    /// Security Integration:
    /// - Combines user-defined filters with RLS policy WHERE clauses
    /// - Ensures all security policies are enforced at the query level
    /// </summary>
    /// <returns>Complete SQL SELECT statement ready for execution.</returns>
    private string BuildSelectQuery()
    {
        // Get writable properties and build column mappings
        var props = GetWritableProperties();
        var columns = BuildColumnMappings(props);
        
        // Start with basic SELECT statement
        var sql = $"SELECT {columns} FROM {_tableName}";
        
        // Combine user filters with RLS policy clauses
        var allWhereClauses = BuildWhereClauses();
        
        // Add WHERE clause if any conditions exist
        if (allWhereClauses.Any())
        {
            sql += $" WHERE {string.Join(" AND ", allWhereClauses)}";
        }
        
        
        return sql;
    }

    /// <summary>
    /// Gets all writable properties from the entity type, excluding computed properties.
    /// Computed properties are those without setters - typically calculated fields.
    /// 
    /// Performance Optimization:
    /// - Uses cached reflection results to avoid repeated expensive GetProperties() calls
    /// - Properties are cached per generic type T, shared across all instances
    /// - Significantly improves performance for repeated operations on the same entity type
    /// </summary>
    /// <returns>Array of writable PropertyInfo objects (cached for performance).</returns>
    private static PropertyInfo[] GetWritableProperties()
    {
        return WritableProperties;
    }

    /// <summary>
    /// Builds column mappings for SELECT statement, handling snake_case to PascalCase conversion.
    /// 
    /// PostgreSQL columns are snake_case (e.g., first_name, created_at)
    /// C# properties are PascalCase (e.g., FirstName, CreatedAt)
    /// 
    /// Strategy:
    /// - Convert property names to snake_case for column selection
    /// - Use AS aliases to preserve original property names in results
    /// - This allows seamless mapping back to entity properties
    /// 
    /// Performance Optimization:
    /// - Uses cached property-to-column mappings to avoid repeated ToSnakeCase() calls
    /// - Significantly faster for repeated operations on the same entity type
    /// </summary>
    /// <param name="properties">The entity properties to map.</param>
    /// <returns>Comma-separated column list with proper aliases.</returns>
    private static string BuildColumnMappings(PropertyInfo[] properties)
    {
        var columnMappings = properties.Select(p => 
        {
            // Use cached property-to-column mapping for performance
            var snakeCase = PropertyToColumnMap[p.Name];
            // If conversion changed the name, use an alias to preserve the original property name
            if (snakeCase != p.Name)
                return $"{snakeCase} AS \"{p.Name}\""; // Quote alias to preserve case
            return snakeCase;
        });
        
        return string.Join(", ", columnMappings);
    }

    /// <summary>
    /// Builds the complete list of WHERE clauses by combining user filters with RLS policies.
    /// 
    /// RLS (Row Level Security) Integration:
    /// - User filters are applied first (from Filter() and In() methods)
    /// - RLS policies add additional security-based WHERE conditions
    /// - Both types of filters are combined with AND logic
    /// </summary>
    /// <returns>List of WHERE clause strings ready for SQL AND combination.</returns>
    private List<string> BuildWhereClauses()
    {
        var allWhereClauses = new List<string>(_whereClauses);
        
        // Add RLS policy WHERE clauses for security enforcement
        var unquotedTableName = core.jarvis.data.StringExtensions.ToSnakeCase(typeof(T).Name);
        var rlsWhereClause = _rlsPolicies.BuildWhereClause(unquotedTableName, _jwtClaims);
        if (!string.IsNullOrEmpty(rlsWhereClause))
        {
            allWhereClauses.Add(rlsWhereClause);
        }
        
        return allWhereClauses;
    }

    /// <summary>
    /// Prepares the database connection and security context for query execution.
    /// 
    /// Preparation Steps:
    /// 1. Ensures database connection is open and ready
    /// 2. Sets JWT claims for RLS enforcement (if client is available)
    /// 3. Prepares security context for both SDK-level and database-level RLS
    /// </summary>
    private async Task PrepareQueryExecution()
    {
        await EnsureConnectionOpen();

        // Set JWT claims for RLS if client is provided (enables database-level RLS if configured)
        if (_client != null)
        {
            await _client.JWTClaims();
        }
    }

    /// <summary>
    /// Executes the SQL query with enhanced parameter handling for PostgreSQL-specific types.
    /// 
    /// PostgreSQL Type Handling:
    /// - Guid parameters are explicitly typed as UUID for proper database mapping
    /// - This prevents type conversion issues with PostgreSQL's UUID type
    /// - Uses Dapper's DynamicParameters for safe parameter binding
    /// </summary>
    /// <param name="sql">The SQL query to execute.</param>
    /// <returns>Dynamic query results from the database.</returns>
    private async Task<IEnumerable<dynamic>> ExecuteQuery(string sql)
    {
        // Enhance parameters with PostgreSQL-specific type mapping
        // PostgreSQL UUID parameter enhancement for proper type mapping
        // Without explicit typing, Npgsql may misinterpret Guid parameters
        var enhancedParameters = new DynamicParameters(_parameters);
        foreach (var paramName in _parameters.ParameterNames)
        {
            var value = _parameters.Get<object>(paramName);
            if (value is Guid guidValue)
            {
                // Explicitly type Guid parameters as DbType.Guid for PostgreSQL UUID columns
                // This prevents "cannot cast type text to uuid" errors
                enhancedParameters.Add(paramName, guidValue, DbType.Guid);
            }
        }
        
        return await _conn.QueryAsync(sql, enhancedParameters);
    }

    /// <summary>
    /// Converts dynamic query results to strongly-typed entity objects.
    /// 
    /// Conversion Process:
    /// 1. Creates new entity instance for each row
    /// 2. Maps database values to entity properties using property names
    /// 3. Handles type conversion through PostgreSqlTypeConverter
    /// 4. Properly handles null values and nullable types
    /// 
    /// Type Safety:
    /// - Uses PostgreSqlTypeConverter for PostgreSQL-specific type mappings
    /// - Handles UUID arrays, JSONB columns, and standard types
    /// - Preserves null safety for nullable and non-nullable types
    /// </summary>
    /// <param name="dynamicResult">Dynamic results from database query.</param>
    /// <returns>List of strongly-typed entity objects.</returns>
    private List<T> ConvertDynamicResultsToEntities(IEnumerable<dynamic> dynamicResult)
    {
        var typedResults = new List<T>();
        var entityProperties = GetWritableProperties();

        foreach (var row in dynamicResult)
        {
            var entity = new T();
            var rowDict = row as IDictionary<string, object>;
            
            if (rowDict != null)
            {
                MapRowToEntity(entity, rowDict, entityProperties);
            }
            
            typedResults.Add(entity);
        }
        
        return typedResults;
    }

    /// <summary>
    /// Maps a single database row to an entity object, handling all property assignments.
    /// 
    /// Mapping Strategy:
    /// - Uses property names directly (they match due to AS aliases in SELECT)
    /// - Handles null values appropriately for nullable vs non-nullable types
    /// - Delegates type conversion to PostgreSqlTypeConverter for complex types
    /// 
    /// Performance Notes:
    /// - Receives pre-cached property array to avoid repeated reflection calls
    /// - Uses optimized type conversion through PostgreSqlTypeConverter
    /// </summary>
    /// <param name="entity">The entity object to populate.</param>
    /// <param name="rowDict">The database row as a dictionary.</param>
    /// <param name="properties">The cached entity properties to map.</param>
    private void MapRowToEntity(T entity, IDictionary<string, object> rowDict, PropertyInfo[] properties)
    {
        _logger?.LogDebug("MapRowToEntity for {EntityType} - Available columns: [{Columns}]", 
            typeof(T).Name, string.Join(", ", rowDict.Keys));


        foreach (var property in properties)
        {
            // Try both the aliased property name AND the snake_case column name
            var propertyName = property.Name;
            var snakeCaseName = PropertyToColumnMap[property.Name];
            
            object? value = null;
            bool foundValue = false;
            string foundColumnName = string.Empty;
            
            // First try the property name (from AS alias)
            if (rowDict.ContainsKey(propertyName))
            {
                value = rowDict[propertyName];
                foundValue = true;
                foundColumnName = propertyName;
            }
            // Then try the snake_case column name
            else if (rowDict.ContainsKey(snakeCaseName))
            {
                value = rowDict[snakeCaseName];
                foundValue = true;
                foundColumnName = snakeCaseName;
            }
            
            if (foundValue)
            {
                _logger?.LogDebug("Mapping {PropertyName} from column '{ColumnName}' = {Value}", 
                    propertyName, foundColumnName, value);
                
                if (value == null)
                {
                    // Handle null values - don't set non-nullable value types to null
                    if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
                    {
                        _logger?.LogDebug("Skipping null value for non-nullable property {PropertyName}", propertyName);
                        continue; // Skip setting non-nullable value types to null
                    }
                    property.SetValue(entity, null);
                    _logger?.LogDebug("Set {PropertyName} = NULL", propertyName);
                }
                else
                {
                    // Use centralized type conversion for PostgreSQL-specific mappings
                    var convertedValue = PostgreSqlTypeConverter.ConvertValue(value, property.PropertyType, snakeCaseName);
                    property.SetValue(entity, convertedValue);
                    _logger?.LogDebug("Set {PropertyName} = {ConvertedValue} (converted from {OriginalValue})", 
                        propertyName, convertedValue, value);
                }
            }
            else
            {
                _logger?.LogDebug("Could not find column for property {PropertyName} (snake_case: {SnakeCaseName})", 
                    propertyName, snakeCaseName);
            }
        }
    }

    /// <summary>
    /// Maps a logical operator string to its SQL equivalent.
    /// </summary>
    /// <param name="op">The logical operator (eq, neq, lt, lte, gt, gte).</param>
    /// <returns>The SQL operator string.</returns>
    /// <exception cref="ArgumentException">Thrown if the operator is not supported.</exception>
    private string TranslateOperator(string op)
    {
        // Security: This method is protected by operator whitelisting in the Filter method.
        // The switch expression provides compile-time validation and is optimal for the small set of operators.
        
        return op switch
        {
            "eq" => "=",
            "neq" => "<>",
            "lt" => "<",
            "lte" => "<=",
            "gt" => ">",
            "gte" => ">=",
            _ => throw new ArgumentException($"Unsupported operator: {op}")
        };
    }

    /// <summary>
    /// Updates an existing entity in the table. Uses snake_case mapping for columns.
    /// Enforces RLS policies at the SDK level.
    /// Requires 'Id' property to identify the record to update.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    public async Task Update(T entity)
    {
        // Get Id property value for WHERE clause
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty == null)
            throw new InvalidOperationException($"Entity type {typeof(T).Name} must have an 'Id' property for updates.");
        
        var idValue = idProperty.GetValue(entity);
        if (idValue == null)
            throw new InvalidOperationException("Entity Id cannot be null for updates.");

        // Check RLS policies for update operation
        var entityData = new Dictionary<string, object>();
        foreach (var prop in typeof(T).GetProperties())
        {
            var value = prop.GetValue(entity);
            if (value != null)
            {
                entityData[PropertyToColumnMap[prop.Name]] = value;
            }
        }

        // Check if update is allowed by RLS policies (use unquoted table name)
        var unquotedTableName = core.jarvis.data.StringExtensions.ToSnakeCase(typeof(T).Name);
        if (!_rlsPolicies.CheckOperation(unquotedTableName, PolicyType.Update, _jwtClaims, entityData))
        {
            // Silently fail like PostgreSQL RLS would
            return;
        }

        var props = typeof(T).GetProperties()
            .Where(p => p.Name != "Id") // Exclude Id from SET clause
            .Where(p => p.CanWrite) // Skip computed properties (properties without setters)
            .ToList();
        var setClauses = string.Join(", ", props.Select(p => 
        {
            var columnName = PropertyToColumnMap[p.Name];
            // Special handling for properties that map to JSONB columns
            if (PostgreSqlTypeConverter.IsJsonColumn(columnName, p.PropertyType))
                return $"{columnName} = @{p.Name}::jsonb";
            return $"{columnName} = @{p.Name}";
        }));
        var sql = $"UPDATE {_tableName} SET {setClauses} WHERE id = @Id";

        await EnsureConnectionOpen();

        // Set JWT claims for RLS if client is provided
        if (_client != null)
        {
            await _client.JWTClaims();
        }

        // Convert entity to parameter dictionary with JSON serialization for complex objects
        var parameters = ConvertEntityToParameters(entity);
        await _conn.ExecuteAsync(sql, parameters);
    }

    /// <summary>
    /// Performs an upsert (insert or update) operation on the entity.
    /// Uses PostgreSQL's ON CONFLICT clause to eliminate race conditions.
    /// For components with OwnerEntityId unique constraints, conflicts on owner_entity_id.
    /// Otherwise, conflicts on id.
    /// </summary>
    /// <param name="entity">The entity to upsert.</param>
    public async Task Upsert(T entity)
    {
        // Get Id property value
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty == null)
            throw new InvalidOperationException($"Entity type {typeof(T).Name} must have an 'Id' property for upserts.");
        
        var idValue = idProperty.GetValue(entity);
        if (idValue == null || (idValue is Guid g && g == Guid.Empty))
        {
            // Generate a new GUID for the entity if it doesn't have one
            idProperty.SetValue(entity, Guid.NewGuid());
        }

        // Determine the conflict resolution strategy
        var ownerEntityIdProperty = typeof(T).GetProperty("OwnerEntityId");
        var coreComponentTables = new[] { "account", "role", "permission", "auth_token", "security_profile" };
        var unquotedTableName = core.jarvis.data.StringExtensions.ToSnakeCase(typeof(T).Name);
        var hasUniqueOwnerEntityId = (unquotedTableName.EndsWith("_component") && !unquotedTableName.Equals("blog_post_component")) ||
            coreComponentTables.Contains(unquotedTableName);
        
        await EnsureConnectionOpen();

        // Set JWT claims for RLS if client is provided
        if (_client != null)
        {
            await _client.JWTClaims();
        }

        _logger?.LogDebug("Upsert: EntityType={EntityType}, hasOwnerEntityId={HasOwnerEntityId}, hasUniqueOwnerEntityId={HasUniqueOwnerEntityId}, unquotedTableName={TableName}", typeof(T).Name, ownerEntityIdProperty != null, hasUniqueOwnerEntityId, unquotedTableName);
        
        if (ownerEntityIdProperty != null && hasUniqueOwnerEntityId)
        {
            // Use PostgreSQL's ON CONFLICT for components with unique owner_entity_id constraints
            _logger?.LogDebug("Taking OwnerEntityId path: OwnerEntityId={OwnerEntityId}, Id={Id}", ownerEntityIdProperty.GetValue(entity), idProperty.GetValue(entity));
            _logger?.LogDebug("Using UpsertWithOwnerEntityIdConflict for {EntityType} with OwnerEntityId={OwnerEntityId}, Id={Id}", 
                typeof(T).Name, ownerEntityIdProperty.GetValue(entity), idProperty.GetValue(entity));
            await UpsertWithOwnerEntityIdConflict(entity);
        }
        else
        {
            // Use PostgreSQL's ON CONFLICT for entities with unique id constraints
            _logger?.LogDebug("Taking Id path: Id={Id}", idProperty.GetValue(entity));
            _logger?.LogDebug("Using UpsertWithIdConflict for {EntityType} with Id={Id}", 
                typeof(T).Name, idProperty.GetValue(entity));
            await UpsertWithIdConflict(entity);
        }
    }

    /// <summary>
    /// Performs atomic upsert using ON CONFLICT (owner_entity_id) for component tables.
    /// </summary>
    private async Task UpsertWithOwnerEntityIdConflict(T entity)
    {
        var props = typeof(T).GetProperties()
            .Where(p => p.CanWrite) // Skip computed properties
            .ToList();

        var columns = string.Join(", ", props.Select(p => PropertyToColumnMap[p.Name]));
        var valueParams = string.Join(", ", props.Select(p => 
        {
            var columnName = PropertyToColumnMap[p.Name];
            if (PostgreSqlTypeConverter.IsJsonColumn(columnName, p.PropertyType))
                return $"@{p.Name}::jsonb";
            return $"@{p.Name}";
        }));

        // Build the DO UPDATE SET clause (exclude owner_entity_id from updates)
        var updateClauses = string.Join(", ", props
            .Where(p => p.Name != "OwnerEntityId") // Don't update the conflict column
            .Select(p => 
            {
                var columnName = PropertyToColumnMap[p.Name];
                
                // Special handling for Version property in IVersionedComponent
                if (p.Name == "Version" && typeof(IVersionedComponent).IsAssignableFrom(typeof(T)))
                {
                    // Use the explicit version value from the entity (set by DataContext)
                    return $"{columnName} = EXCLUDED.{columnName}";
                }
                
                if (PostgreSqlTypeConverter.IsJsonColumn(columnName, p.PropertyType))
                    return $"{columnName} = EXCLUDED.{columnName}";
                return $"{columnName} = EXCLUDED.{columnName}";
            }));

        var sql = $@"
            INSERT INTO {_tableName} ({columns}) 
            VALUES ({valueParams})
            ON CONFLICT (owner_entity_id) DO UPDATE SET {updateClauses}
            RETURNING version";

        var parameters = ConvertEntityToParameters(entity);
        
        _logger?.LogDebug("UPSERT SQL: {Sql}", sql);
        _logger?.LogDebug("UPSERT Params: [parameters object]");
        _logger?.LogDebug("UpsertWithOwnerEntityIdConflict SQL: {Sql}", sql);
        _logger?.LogDebug("UpsertWithOwnerEntityIdConflict Parameters: [parameters object]");
        
        // For IVersionedComponent, retrieve the updated version and set it on the entity
        if (typeof(IVersionedComponent).IsAssignableFrom(typeof(T)))
        {
            var newVersion = await _conn.QuerySingleAsync<int?>(sql, parameters);
            _logger?.LogDebug("UpsertWithOwnerEntityIdConflict returned version: {Version}", newVersion);
            var versionProperty = typeof(T).GetProperty("Version");
            versionProperty?.SetValue(entity, newVersion);
            _logger?.LogDebug("Set entity version to: {Version}", newVersion);
        }
        else
        {
            await _conn.ExecuteAsync(sql, parameters);
        }
        
    }

    /// <summary>
    /// Performs atomic upsert using ON CONFLICT (id) for regular tables.
    /// </summary>
    private async Task UpsertWithIdConflict(T entity)
    {
        var props = typeof(T).GetProperties()
            .Where(p => p.CanWrite) // Skip computed properties
            .ToList();

        var columns = string.Join(", ", props.Select(p => PropertyToColumnMap[p.Name]));
        var valueParams = string.Join(", ", props.Select(p => 
        {
            var columnName = PropertyToColumnMap[p.Name];
            if (PostgreSqlTypeConverter.IsJsonColumn(columnName, p.PropertyType))
                return $"@{p.Name}::jsonb";
            return $"@{p.Name}";
        }));

        // Build the DO UPDATE SET clause (exclude id from updates)
        var updateClauses = string.Join(", ", props
            .Where(p => p.Name != "Id") // Don't update the conflict column
            .Select(p => 
            {
                var columnName = PropertyToColumnMap[p.Name];
                
                // Special handling for Version property in IVersionedComponent
                if (p.Name == "Version" && typeof(IVersionedComponent).IsAssignableFrom(typeof(T)))
                {
                    // Use the explicit version value from the entity (set by DataContext)
                    return $"{columnName} = EXCLUDED.{columnName}";
                }
                
                if (PostgreSqlTypeConverter.IsJsonColumn(columnName, p.PropertyType))
                    return $"{columnName} = EXCLUDED.{columnName}";
                return $"{columnName} = EXCLUDED.{columnName}";
            }));

        var sql = $@"
            INSERT INTO {_tableName} ({columns}) 
            VALUES ({valueParams})
            ON CONFLICT (id) DO UPDATE SET {updateClauses}
            RETURNING version";

        var parameters = ConvertEntityToParameters(entity);
        
        // For IVersionedComponent, retrieve the updated version and set it on the entity
        if (typeof(IVersionedComponent).IsAssignableFrom(typeof(T)))
        {
            var newVersion = await _conn.QuerySingleAsync<int?>(sql, parameters);
            var versionProperty = typeof(T).GetProperty("Version");
            versionProperty?.SetValue(entity, newVersion);
        }
        else
        {
            await _conn.ExecuteAsync(sql, parameters);
        }
        
    }

    

    /// <summary>
    /// Deletes records matching the current filter criteria.
    /// Enforces RLS policies at the SDK level.
    /// </summary>
    /// <returns>The number of records deleted.</returns>
    public async Task<int> Delete()
    {
        var sql = $"DELETE FROM {_tableName}";
        var allWhereClauses = new List<string>(_whereClauses);
        
        // Add RLS policy WHERE clauses (use unquoted table name for policy lookup)
        var unquotedTableName = core.jarvis.data.StringExtensions.ToSnakeCase(typeof(T).Name);
        var rlsWhereClause = _rlsPolicies.BuildWhereClause(unquotedTableName, _jwtClaims);
        if (!string.IsNullOrEmpty(rlsWhereClause))
        {
            allWhereClauses.Add(rlsWhereClause);
        }
        
        if (allWhereClauses.Any())
        {
            sql += $" WHERE {string.Join(" AND ", allWhereClauses)}";
        }

        // Check if delete is allowed by RLS policies (use unquoted table name)
        if (!_rlsPolicies.CheckOperation(unquotedTableName, PolicyType.Delete, _jwtClaims, new Dictionary<string, object>()))
        {
            // Silently fail like PostgreSQL RLS would
            return 0;
        }

        await EnsureConnectionOpen();

        // Set JWT claims for RLS if client is provided
        if (_client != null)
        {
            await _client.JWTClaims();
        }

        return await _conn.ExecuteAsync(sql, _parameters);
    }

    /// <summary>
    /// Executes the SELECT query and returns a single result.
    /// Throws an exception if no record is found or if multiple records match.
    /// </summary>
    /// <returns>The single entity matching the query.</returns>
    /// <exception cref="InvalidOperationException">Thrown when zero or multiple records are found.</exception>
    public async Task<T> Single()
    {
        var results = await Get();
        
        if (results.Count == 0)
            throw new InvalidOperationException($"No records found in table {_tableName} matching the query criteria.");
        
        if (results.Count > 1)
            throw new InvalidOperationException($"Multiple records found in table {_tableName} matching the query criteria. Expected exactly one.");
        
        return results[0];
    }

    /// <summary>
    /// Executes the SELECT query and returns a single result or null if not found.
    /// Throws an exception if multiple records match.
    /// </summary>
    /// <returns>The single entity matching the query, or null if not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when multiple records are found.</exception>
    public async Task<T?> SingleOrDefault()
    {
        var results = await Get();
        
        if (results.Count == 0)
            return null;
        
        if (results.Count > 1)
            throw new InvalidOperationException($"Multiple records found in table {_tableName} matching the query criteria. Expected at most one.");
        
        return results[0];
    }

    /// <summary>
    /// Adds a filter for the IN operator with multiple values.
    /// </summary>
    /// <param name="column">The column name (snake_case, must match entity property).</param>
    /// <param name="values">The collection of values to match.</param>
    /// <returns>The current PgTable instance for chaining.</returns>
    public PgTable<T> In(string column, IEnumerable<object> values)
    {
        // Validate column to prevent SQL injection
        if (!AllowedColumns.Contains(column))
            throw new ArgumentException($"Column '{column}' is not allowed.");

        var valuesList = values.ToList();
        if (!valuesList.Any())
            return this;

        var paramNames = new List<string>();
        foreach (var value in valuesList)
        {
            var paramName = $"@param_{_parameters.ParameterNames.Count()}";
            paramNames.Add(paramName);
            _parameters.Add(paramName, value);
        }

        _whereClauses.Add($"{column} IN ({string.Join(", ", paramNames)})");
        return this;
    }

    /// <summary>
    /// Ensures the database connection is open before executing commands.
    /// </summary>
    private async Task EnsureConnectionOpen()
    {
        if (_conn.State != System.Data.ConnectionState.Open)
            await _conn.OpenAsync();
    }

    /// <summary>
    /// Converts an entity to a parameter dictionary, serializing complex objects to JSON.
    /// </summary>
    /// <param name="entity">The entity to convert.</param>
    /// <returns>A dictionary of parameters suitable for Dapper.</returns>
    private DynamicParameters ConvertEntityToParameters(T entity)
    {
        var parameters = new DynamicParameters();
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Use cached writable properties for optimal performance
        foreach (var property in WritableProperties)
        {
            var value = property.GetValue(entity);
            // Use original property name for parameter binding to match SQL generation
            var propertyName = property.Name;

            if (value == null)
            {
                parameters.Add(propertyName, null);
                continue;
            }

            // Check if this property maps to a PostgreSQL JSONB column
            var columnName = PropertyToColumnMap[property.Name];
            var needsJsonSerialization = PostgreSqlTypeConverter.IsJsonColumn(columnName, property.PropertyType);

            if (needsJsonSerialization)
            {
                // Serialize complex objects to JSON string for PostgreSQL JSONB storage
                // PostgreSQL JSONB provides better performance and indexing than JSON
                var jsonString = JsonSerializer.Serialize(value, jsonOptions);
                parameters.Add(propertyName, jsonString);
            }
            else
            {
                // PostgreSQL array parameter binding - critical for UUID[] columns
                // PostgreSQL arrays require specific parameter typing for proper handling
                if (value is Guid[] guidArray)
                {
                    // Use DbType.Object to let Npgsql handle the UUID[] array conversion
                    parameters.Add(propertyName, guidArray, DbType.Object);
                }
                else if (value is List<Guid> guidList)
                {
                    // Convert List<Guid> to Guid[] and use NpgsqlParameter for explicit typing
                    // This ensures PostgreSQL correctly recognizes the parameter as UUID[]
                    var guidArr = guidList.ToArray();
                    var param = new NpgsqlParameter(propertyName, NpgsqlDbType.Array | NpgsqlDbType.Uuid)
                    {
                        Value = guidArr
                    };
                    parameters.AddDynamicParams(param);
                }
                else
                {
                    parameters.Add(propertyName, value);
                }
            }
        }

        return parameters;
    }

}