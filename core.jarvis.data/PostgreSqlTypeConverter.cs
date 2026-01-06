using System.Collections;
using System.Text.Json;

namespace core.jarvis.data;

/// <summary>
/// Handles type conversion between PostgreSQL database types and .NET types.
/// This class specializes in handling PostgreSQL-specific type mappings that require 
/// custom conversion logic, particularly for UUID arrays and JSONB columns.
/// 
/// PostgreSQL Type Mappings:
/// - UUID[] → Guid[] (with various intermediate representations from Npgsql)
/// - JSONB → Complex .NET objects (via JSON deserialization)
/// - Standard types → Direct conversion with proper null handling
/// </summary>
public static class PostgreSqlTypeConverter
{
    /// <summary>
    /// Converts a database value to the target property type, handling PostgreSQL-specific type mappings.
    /// This method centralizes all type conversion logic that was previously scattered throughout PgTable.
    /// </summary>
    /// <param name="value">The raw value returned from the database query.</param>
    /// <param name="targetType">The target .NET type for the property.</param>
    /// <param name="columnName">The database column name (used for JSONB detection).</param>
    /// <returns>The converted value ready for property assignment.</returns>
    public static object? ConvertValue(object? value, Type targetType, string columnName)
    {
        // Handle null values appropriately
        if (value == null)
        {
            return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null 
                ? Activator.CreateInstance(targetType) 
                : null;
        }

        // Special handling for PostgreSQL UUID arrays → .NET Guid arrays
        if (targetType == typeof(Guid[]))
        {
            return ConvertToGuidArray(value);
        }

        // Handle JSONB columns that need JSON deserialization
        if (IsJsonColumn(columnName, targetType) && value is string jsonString)
        {
            return DeserializeJsonValue(jsonString, targetType);
        }

        // Handle enum types with robust conversion
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        var enumType = underlyingType?.IsEnum == true ? underlyingType : (targetType.IsEnum ? targetType : null);
        
        if (enumType != null)
        {
            return ConvertEnumValue(value, targetType, enumType);
        }

        // Standard type conversion for primitive and simple types
        try
        {
            // Handle nullable types properly
            if (underlyingType != null)
            {
                // For nullable types, convert to the underlying type
                return Convert.ChangeType(value, underlyingType);
            }
            
            return Convert.ChangeType(value, targetType);
        }
        catch (InvalidCastException)
        {
            // Return default value if conversion fails
            return GetDefaultValue(targetType);
        }
        catch (FormatException)
        {
            // Return default value if format is invalid (e.g., GUID parsing)
            return GetDefaultValue(targetType);
        }
    }

    /// <summary>
    /// Converts database values to enum types with robust handling.
    /// Prevents data corruption by preserving actual stored enum values.
    /// </summary>
    private static object? ConvertEnumValue(object? value, Type targetType, Type enumType)
    {
        if (value == null)
        {
            return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null 
                ? Enum.ToObject(enumType, 0) 
                : null;
        }
        
        try
        {
            // Handle integer values (most common case)
            if (value is int intValue)
            {
                return Enum.ToObject(enumType, intValue);
            }
            
            // Handle other numeric types
            if (value is long || value is short || value is byte || value is decimal)
            {
                var intVal = Convert.ToInt32(value);
                return Enum.ToObject(enumType, intVal);
            }
            
            // Handle string values
            if (value is string stringValue)
            {
                // Try parsing as enum name first
                if (Enum.TryParse(enumType, stringValue, true, out var enumValue))
                    return enumValue;
                
                // Try parsing as integer
                if (int.TryParse(stringValue, out var intVal))
                    return Enum.ToObject(enumType, intVal);
            }
            
            // Last resort: try direct conversion
            return Enum.ToObject(enumType, Convert.ToInt32(value));
        }
        catch (Exception ex)
        {
            // For enums, we should not silently return default values as this causes data corruption
            // Instead, throw with detailed information to help debug the issue
            throw new InvalidCastException(
                $"Cannot convert value '{value}' (type: {value?.GetType().Name}) to enum {enumType.Name}. " +
                $"Target type: {targetType.Name}. Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Converts various PostgreSQL UUID array representations to .NET Guid arrays.
    /// 
    /// PostgreSQL UUID[] columns can be returned by Npgsql as different .NET types depending on:
    /// - Npgsql driver version
    /// - Query execution context (direct query vs. parameterized)
    /// - Connection pooling settings
    /// 
    /// This method handles all known variations to ensure consistent Guid[] conversion.
    /// </summary>
    /// <param name="value">The value returned from PostgreSQL (UUID[] column).</param>
    /// <returns>A .NET Guid array containing the converted UUIDs.</returns>
    public static Guid[] ConvertToGuidArray(object value)
    {
        return value switch
        {
            // Direct Guid array (ideal case - modern Npgsql versions)
            Guid[] guidArray => guidArray,
            
            // Object array containing Guids (common with older Npgsql versions)
            object[] objArray => ConvertObjectArrayToGuids(objArray),
            
            // Generic array type (handle various array implementations)
            Array array => ConvertGenericArrayToGuids(array),
            
            // IEnumerable variations (List<Guid>, HashSet<Guid>, etc.)
            IEnumerable<Guid> guidEnum => guidEnum.ToArray(),
            IEnumerable enumerable when enumerable is not string => 
                ConvertEnumerableToGuids(enumerable),
            
            // Single Guid value (edge case for single-element arrays)
            Guid singleGuid => new[] { singleGuid },
            
            // String representation (rare, but possible with some query types)
            string str when TryParseGuidArray(str, out var guids) => guids,
            
            // Fallback: empty array for unrecognized types
            _ => Array.Empty<Guid>()
        };
    }

    /// <summary>
    /// Converts an object array to Guid array, handling mixed types within the array.
    /// </summary>
    /// <param name="objArray">The object array from PostgreSQL.</param>
    /// <returns>Converted Guid array.</returns>
    private static Guid[] ConvertObjectArrayToGuids(object[] objArray)
    {
        var result = new Guid[objArray.Length];
        for (int i = 0; i < objArray.Length; i++)
        {
            result[i] = ConvertSingleObjectToGuid(objArray[i]);
        }
        return result;
    }

    /// <summary>
    /// Converts a generic array to Guid array, handling various element types.
    /// </summary>
    /// <param name="array">The source array from PostgreSQL.</param>
    /// <returns>Converted Guid array.</returns>
    private static Guid[] ConvertGenericArrayToGuids(Array array)
    {
        var result = new Guid[array.Length];
        for (int i = 0; i < array.Length; i++)
        {
            var element = array.GetValue(i);
            result[i] = ConvertSingleObjectToGuid(element);
        }
        return result;
    }

    /// <summary>
    /// Converts an IEnumerable to Guid array, handling dynamic collections from PostgreSQL.
    /// </summary>
    /// <param name="enumerable">The enumerable collection from PostgreSQL.</param>
    /// <returns>Converted Guid array.</returns>
    private static Guid[] ConvertEnumerableToGuids(IEnumerable enumerable)
    {
        var guidList = new List<Guid>();
        foreach (var item in enumerable)
        {
            guidList.Add(ConvertSingleObjectToGuid(item));
        }
        return guidList.ToArray();
    }

    /// <summary>
    /// Converts a single object to Guid, handling various input types from PostgreSQL.
    /// </summary>
    /// <param name="value">The value to convert to Guid.</param>
    /// <returns>Converted Guid or Guid.Empty if conversion fails.</returns>
    private static Guid ConvertSingleObjectToGuid(object? value)
    {
        return value switch
        {
            Guid guid => guid,
            string str when Guid.TryParse(str, out var parsed) => parsed,
            byte[] bytes when bytes.Length == 16 => new Guid(bytes),
            _ => Guid.Empty
        };
    }

    /// <summary>
    /// Attempts to parse a string representation of a Guid array.
    /// Handles PostgreSQL array literal format: {guid1,guid2,guid3}
    /// </summary>
    /// <param name="str">The string to parse.</param>
    /// <param name="guids">The resulting Guid array if parsing succeeds.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    private static bool TryParseGuidArray(string str, out Guid[] guids)
    {
        guids = Array.Empty<Guid>();
        
        if (string.IsNullOrEmpty(str) || !str.StartsWith('{') || !str.EndsWith('}'))
            return false;

        try
        {
            var content = str.Substring(1, str.Length - 2); // Remove { and }
            if (string.IsNullOrEmpty(content))
            {
                guids = Array.Empty<Guid>();
                return true;
            }

            var parts = content.Split(',');
            var result = new Guid[parts.Length];
            
            for (int i = 0; i < parts.Length; i++)
            {
                if (!Guid.TryParse(parts[i].Trim(), out result[i]))
                    return false;
            }
            
            guids = result;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Deserializes a JSON string to the target type for JSONB columns.
    /// Uses camelCase naming policy to match PostgreSQL JSONB conventions.
    /// </summary>
    /// <param name="jsonString">The JSON string from the JSONB column.</param>
    /// <param name="targetType">The target .NET type for deserialization.</param>
    /// <returns>The deserialized object or the default value if deserialization fails.</returns>
    private static object? DeserializeJsonValue(string jsonString, Type targetType)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            
            return JsonSerializer.Deserialize(jsonString, targetType, jsonOptions);
        }
        catch (JsonException)
        {
            // If JSON deserialization fails, return default value
            return GetDefaultValue(targetType);
        }
    }

    /// <summary>
    /// Determines if a property should be treated as a JSONB column based on column name and type.
    /// 
    /// JSONB Detection Strategy:
    /// 1. Explicit column name matching for known JSONB columns
    /// 2. Complex object types that aren't PostgreSQL native types
    /// 3. Excludes primitive types and PostgreSQL array types
    /// </summary>
    /// <param name="columnName">The snake_case column name in PostgreSQL.</param>
    /// <param name="propertyType">The .NET property type.</param>
    /// <returns>True if the property should be JSON serialized/deserialized.</returns>
    public static bool IsJsonColumn(string columnName, Type propertyType)
    {
        // Known JSONB columns from database schema
        var jsonColumns = new HashSet<string>
        {
            "payment_policy", "client_communication", "client_work_preferences", 
            "departments", "metadata", "snapshots", "child_types", "preferences"
        };

        // If the column is explicitly known to be JSONB, serialize it
        if (jsonColumns.Contains(columnName))
        {
            return true;
        }

        // Define primitive types that should NOT be JSON serialized
        var primitiveTypes = new HashSet<Type>
        {
            typeof(string), typeof(int), typeof(long), typeof(decimal), typeof(double), typeof(float),
            typeof(bool), typeof(DateTime), typeof(Guid), typeof(byte[]), 
            typeof(int?), typeof(long?), typeof(decimal?), typeof(double?), typeof(float?),
            typeof(bool?), typeof(DateTime?), typeof(Guid?)
        };

        // PostgreSQL native array types - these should NOT be JSON serialized
        var postgresArrayTypes = new HashSet<Type>
        {
            typeof(Guid[]), typeof(int[]), typeof(string[]), typeof(long[]),
            typeof(decimal[]), typeof(bool[]), typeof(DateTime[]), typeof(double[]), typeof(float[]),
            typeof(List<Guid>), typeof(List<int>), typeof(List<string>), typeof(List<long>),
            typeof(List<decimal>), typeof(List<bool>), typeof(List<DateTime>), typeof(List<double>), typeof(List<float>)
        };

        // Enums and nullable enums should be treated as primitives (they convert to integers)
        if (propertyType.IsEnum || (Nullable.GetUnderlyingType(propertyType)?.IsEnum == true))
        {
            return false;
        }

        // Don't serialize PostgreSQL native array types
        if (postgresArrayTypes.Contains(propertyType))
        {
            return false;
        }

        // If it's not a primitive type and not explicitly excluded, serialize as JSON
        return !primitiveTypes.Contains(propertyType);
    }

    /// <summary>
    /// Gets the default value for a given type, handling both value types and reference types.
    /// </summary>
    /// <param name="type">The type to get the default value for.</param>
    /// <returns>The default value for the type.</returns>
    private static object? GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }
}