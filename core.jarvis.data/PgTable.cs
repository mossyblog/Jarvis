using core.jarvis.data.RLS;
using Dapper;
using Npgsql;

// For StringExtensions

namespace core.jarvis.data;

/// <summary>
/// Provides a strongly-typed, secure, and convention-based interface for interacting with a PostgreSQL table.
/// Handles snake_case mapping, parameterized queries, JWT-based RLS, and column/operator whitelisting.
/// </summary>
/// <typeparam name="T">The entity type mapped to the table.</typeparam>
public class PgTable<T> where T : class, new()
{
    private readonly NpgsqlConnection _conn;
    private readonly PgClient? _client;
    private readonly RLSPolicyRegistry _rlsPolicies;
    private readonly Dictionary<string, string> _jwtClaims;
    private readonly string _tableName;
    private readonly List<string> _whereClauses = new();
    private readonly DynamicParameters _parameters = new();

    // Whitelist of allowed operators for filtering (prevents SQL injection)
    private static readonly HashSet<string> AllowedOperators = new() { "eq", "neq", "lt", "lte", "gt", "gte" };

    // Whitelist of allowed columns (from T's properties, mapped to snake_case)
    private static readonly HashSet<string> AllowedColumns = typeof(T).GetProperties()
        .Select(p => p.Name.ToSnakeCase())
        .ToHashSet();

    /// <summary>
    /// Initializes a new instance of PgTable for the specified entity type.
    /// </summary>
    /// <param name="conn">The Npgsql database connection.</param>
    /// <param name="client">Optional PgClient for JWT/RLS context.</param>
    /// <param name="rlsPolicies">RLS policy registry.</param>
    /// <param name="jwtClaims">JWT claims for RLS enforcement.</param>
    public PgTable(NpgsqlConnection conn, PgClient? client = null, RLSPolicyRegistry? rlsPolicies = null, Dictionary<string, string>? jwtClaims = null)
    {
        _conn = conn;
        _client = client;
        _rlsPolicies = rlsPolicies ?? new RLSPolicyRegistry();
        _jwtClaims = jwtClaims ?? new Dictionary<string, string>();
        _tableName = typeof(T).Name.ToSnakeCase();
    }

    /// <summary>
    /// Inserts a new entity into the table. Uses snake_case mapping for columns.
    /// Enforces RLS policies at the SDK level.
    /// Excludes the 'Id' property from inserts as it's typically auto-generated.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    public async Task Insert(T entity)
    {
        // Check RLS policies for insert operation
        var entityData = new Dictionary<string, object>();
        foreach (var prop in typeof(T).GetProperties())
        {
            var value = prop.GetValue(entity);
            if (value != null)
            {
                entityData[prop.Name.ToSnakeCase()] = value;
            }
        }

        // Check if insert is allowed by RLS policies
        if (!_rlsPolicies.CheckOperation(_tableName, PolicyType.Insert, _jwtClaims, entityData))
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
        
        var props = typeof(T).GetProperties()
            .Where(p => !shouldExcludeId || p.Name != "Id")
            .ToList();
            
        var columns = string.Join(", ", props.Select(p => p.Name.ToSnakeCase()));
        var values = string.Join(", ", props.Select(p => 
        {
            // Special handling for properties that map to JSONB columns
            if ((p.Name == "Metadata" || p.Name == "Snapshots" || p.Name == "ChildTypes") && p.PropertyType == typeof(string))
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

        await _conn.ExecuteAsync(sql, entity);
    }

    /// <summary>
    /// Adds a filter clause to the query using a whitelisted column and operator.
    /// </summary>
    /// <param name="column">The column name (snake_case, must match entity property).</param>
    /// <param name="op">The operator (eq, neq, lt, lte, gt, gte).</param>
    /// <param name="value">The value to compare.</param>
    /// <returns>The current PgTable instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if column or operator is not allowed.</exception>
    public PgTable<T> Filter(string column, string op, object value)
    {
        // Validate column and operator to prevent SQL injection
        if (!AllowedColumns.Contains(column))
            throw new ArgumentException($"Column '{column}' is not allowed.");
        if (!AllowedOperators.Contains(op))
            throw new ArgumentException($"Operator '{op}' is not allowed.");

        string paramName = $"@param_{_parameters.ParameterNames.Count()}";
        _whereClauses.Add($"{column} {TranslateOperator(op)} {paramName}");
        _parameters.Add(paramName, value);
        return this;
    }

    /// <summary>
    /// Executes the SELECT query with any applied filters and returns the result set.
    /// Enforces RLS policies at the SDK level by adding WHERE clauses.
    /// </summary>
    /// <returns>List of entities matching the query.</returns>
    public async Task<List<T>> Get()
    {
        // Build column list with snake_case to PascalCase mapping
        var props = typeof(T).GetProperties();
        var columnMappings = props.Select(p => 
        {
            var snakeCase = p.Name.ToSnakeCase();
            if (snakeCase != p.Name.ToLower())
                return $"{snakeCase} AS {p.Name}";
            return snakeCase;
        });
        var columns = string.Join(", ", columnMappings);
        
        var sql = $"SELECT {columns} FROM {_tableName}";
        var allWhereClauses = new List<string>(_whereClauses);
        
        // Add RLS policy WHERE clauses
        var rlsWhereClause = _rlsPolicies.BuildWhereClause(_tableName, _jwtClaims);
        if (!string.IsNullOrEmpty(rlsWhereClause))
        {
            allWhereClauses.Add(rlsWhereClause);
        }
        
        if (allWhereClauses.Any())
        {
            sql += $" WHERE {string.Join(" AND ", allWhereClauses)}";
        }

        await EnsureConnectionOpen();

        // Set JWT claims for RLS if client is provided (for database-level RLS if enabled)
        if (_client != null)
        {
            await _client.JWTClaims();
        }

        var result = await _conn.QueryAsync<T>(sql, _parameters);
        return result.ToList();
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
                entityData[prop.Name.ToSnakeCase()] = value;
            }
        }

        // Check if update is allowed by RLS policies
        if (!_rlsPolicies.CheckOperation(_tableName, PolicyType.Update, _jwtClaims, entityData))
        {
            // Silently fail like PostgreSQL RLS would
            return;
        }

        var props = typeof(T).GetProperties()
            .Where(p => p.Name != "Id") // Exclude Id from SET clause
            .ToList();
        var setClauses = string.Join(", ", props.Select(p => 
        {
            // Special handling for properties that map to JSONB columns
            if ((p.Name == "Metadata" || p.Name == "Snapshots" || p.Name == "ChildTypes") && p.PropertyType == typeof(string))
                return $"{p.Name.ToSnakeCase()} = @{p.Name}::jsonb";
            return $"{p.Name.ToSnakeCase()} = @{p.Name}";
        }));
        var sql = $"UPDATE {_tableName} SET {setClauses} WHERE id = @Id";

        await EnsureConnectionOpen();

        // Set JWT claims for RLS if client is provided
        if (_client != null)
        {
            await _client.JWTClaims();
        }

        await _conn.ExecuteAsync(sql, entity);
    }

    /// <summary>
    /// Performs an upsert (insert or update) operation on the entity.
    /// For components with OwnerEntityId, checks existence by owner_entity_id (unique constraint).
    /// Otherwise, checks by Id.
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
            // No ID or empty GUID - do insert
            await Insert(entity);
            return;
        }

        // Check existence by id for most cases
        // Only use owner_entity_id for components with unique constraints on that field
        var ownerEntityIdProperty = typeof(T).GetProperty("OwnerEntityId");
        string existsQuery;
        object queryParams;
        
        // Tables with unique constraint on owner_entity_id (one component per entity)
        var hasUniqueOwnerEntityId = _tableName.EndsWith("_component") && 
            !_tableName.Equals("blog_post_component"); // blog_post allows multiple per entity
        
        if (ownerEntityIdProperty != null && hasUniqueOwnerEntityId)
        {
            // For single-instance components, check existence by owner_entity_id
            var ownerEntityIdValue = ownerEntityIdProperty.GetValue(entity);
            existsQuery = $"SELECT COUNT(*) FROM {_tableName} WHERE owner_entity_id = @OwnerEntityId";
            queryParams = new { OwnerEntityId = ownerEntityIdValue };
        }
        else
        {
            // For all other entities (including multi-instance components), check existence by id
            existsQuery = $"SELECT COUNT(*) FROM {_tableName} WHERE id = @Id";
            queryParams = new { Id = idValue };
        }
        
        await EnsureConnectionOpen();
        var exists = await _conn.ExecuteScalarAsync<int>(existsQuery, queryParams) > 0;
        
        if (exists)
        {
            await Update(entity);
        }
        else
        {
            await Insert(entity);
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
        
        // Add RLS policy WHERE clauses
        var rlsWhereClause = _rlsPolicies.BuildWhereClause(_tableName, _jwtClaims);
        if (!string.IsNullOrEmpty(rlsWhereClause))
        {
            allWhereClauses.Add(rlsWhereClause);
        }
        
        if (allWhereClauses.Any())
        {
            sql += $" WHERE {string.Join(" AND ", allWhereClauses)}";
        }

        // Check if delete is allowed by RLS policies
        if (!_rlsPolicies.CheckOperation(_tableName, PolicyType.Delete, _jwtClaims, new Dictionary<string, object>()))
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
}