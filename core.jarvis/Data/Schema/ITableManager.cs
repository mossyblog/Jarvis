using core.jarvis.Data.Components;

namespace core.jarvis.Data.Schema;

/// <summary>
/// Manages database table schema validation and creation for components.
/// </summary>
public interface ITableManager
{
    /// <summary>
    /// Ensures the table for the specified component type exists and matches the expected schema.
    /// - If table doesn't exist: Creates it
    /// - If table exists but missing fields: Adds missing fields  
    /// - If table exists but fields have wrong data types: Throws exception requiring manual migration
    /// </summary>
    /// <typeparam name="TComponent">The component type to validate/create table for</typeparam>
    /// <exception cref="SchemaValidationException">Thrown when existing fields have incompatible data types</exception>
    Task EnsureTableExists<TComponent>() where TComponent : class, IComponent, new();
    
    /// <summary>
    /// Validates all registered component tables in the system.
    /// </summary>
    Task ValidateAllComponentTables();
}