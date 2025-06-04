# Security Audit: PgTable.cs SQL Injection Protection

## Executive Summary
The `PgTable<T>` class demonstrates **strong protection against SQL injection attacks** through multiple defensive layers. The implementation follows security best practices with comprehensive whitelisting, parameterized queries, and strict input validation.

## Security Strengths ✅

### 1. Column Whitelisting (Lines 29-31, 109-110)
```csharp
private static readonly HashSet<string> AllowedColumns = typeof(T).GetProperties()
    .Select(p => p.Name.ToSnakeCase())
    .ToHashSet();
```
- **Protection**: Only allows columns that exist as properties on the entity type
- **Implementation**: Static initialization prevents runtime modification
- **Validation**: Throws `ArgumentException` for unauthorized columns

### 2. Operator Whitelisting (Lines 26, 111-112)
```csharp
private static readonly HashSet<string> AllowedOperators = new() { "eq", "neq", "lt", "lte", "gt", "gte" };
```
- **Protection**: Strictly limits allowed SQL operators
- **Implementation**: Hardcoded whitelist prevents injection via operator manipulation
- **Validation**: Throws `ArgumentException` for unauthorized operators

### 3. Parameterized Queries Throughout
All SQL queries use Dapper's parameterized query support:
- **Insert** (Line 95): `await _conn.ExecuteAsync(sql, entity);`
- **Update** (Line 243): `await _conn.ExecuteAsync(sql, entity);`
- **Delete** (Line 341): `await _conn.ExecuteAsync(sql, _parameters);`
- **Select** (Line 161): `await _conn.QueryAsync<T>(sql, _parameters);`

### 4. Dynamic Parameter Building (Lines 114-116)
```csharp
string paramName = $"@param_{_parameters.ParameterNames.Count()}";
_whereClauses.Add($"{column} {TranslateOperator(op)} {paramName}");
_parameters.Add(paramName, value);
```
- **Protection**: Values are never concatenated directly into SQL
- **Implementation**: Uses Dapper's `DynamicParameters` for safe parameter handling

### 5. Table Name Safety (Line 46)
```csharp
_tableName = typeof(T).Name.ToSnakeCase();
```
- **Protection**: Table name derived from type name, not user input
- **Implementation**: Compile-time safety through generic type parameter

## Addressed TODOs

### TranslateOperator Method (Lines 173-174)
The TODOs suggest considering a dictionary for performance, but the current implementation is actually **more secure**:

```csharp
private string TranslateOperator(string op)
{
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
```

**Why the current implementation is secure:**
1. Switch expression provides compile-time validation
2. Default case throws exception for any unexpected input
3. Combined with operator whitelisting, provides double protection
4. Performance impact is negligible for 6 operators

**Recommendation**: Remove the TODOs as the current implementation is optimal for security.

## Additional Security Features

### 1. RLS Policy Enforcement
- SDK-level Row Level Security adds another layer of protection
- Policies are enforced before database operations
- JWT claims validated for access control

### 2. Special Handling for JSONB Columns
```csharp
if ((p.Name == "Metadata" || p.Name == "Snapshots") && p.PropertyType == typeof(string))
    return $"{p.Name.ToSnakeCase()} = @{p.Name}::jsonb";
```
- Proper type casting for JSONB columns
- Still uses parameterized values

### 3. IN Operator Safety (Lines 388-408)
```csharp
public PgTable<T> In(string column, IEnumerable<object> values)
{
    // Column validation
    if (!AllowedColumns.Contains(column))
        throw new ArgumentException($"Column '{column}' is not allowed.");
    
    // Parameter generation for each value
    var paramNames = new List<string>();
    foreach (var value in valuesList)
    {
        var paramName = $"@param_{_parameters.ParameterNames.Count()}";
        paramNames.Add(paramName);
        _parameters.Add(paramName, value);
    }
}
```
- Each value in the IN clause is parameterized
- No string concatenation of values

## Recommendations

### 1. Remove TODOs
The TODOs in the `TranslateOperator` method can be removed as the current implementation is secure and performant.

### 2. Consider Adding More Operators (Optional)
If needed, additional operators can be safely added to the whitelist:
- `"like"` for pattern matching
- `"in"` (already implemented separately)
- `"is"` for NULL checks

### 3. Add Security Documentation
Consider adding XML documentation highlighting the security features:
```csharp
/// <summary>
/// Provides a strongly-typed, secure, and convention-based interface for interacting with a PostgreSQL table.
/// 
/// Security features:
/// - Column whitelisting prevents SQL injection via column names
/// - Operator whitelisting prevents SQL injection via operators
/// - All queries use parameterized values
/// - Table names derived from type system, not user input
/// </summary>
```

## Conclusion

The `PgTable<T>` class exhibits **excellent protection against SQL injection attacks**. The implementation follows security best practices:

1. ✅ **Input Validation**: All user inputs are validated against whitelists
2. ✅ **Parameterized Queries**: All values are parameterized, never concatenated
3. ✅ **Defense in Depth**: Multiple layers of protection
4. ✅ **Type Safety**: Leverages C# type system for additional safety
5. ✅ **Clear Error Messages**: Security violations result in clear exceptions

**Security Rating: A+**

No SQL injection vulnerabilities were identified. The implementation demonstrates a security-first approach with comprehensive protection mechanisms.