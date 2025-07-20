using System;

namespace core.jarvis.api.Attributes;

/// <summary>
/// Attribute to declare required permissions for a function or method.
/// Can be applied at class or method level.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute
{
    /// <summary>
    /// The permission required to access this endpoint.
    /// </summary>
    public string Permission { get; }
    
    /// <summary>
    /// Logical operator when multiple permissions are specified.
    /// </summary>
    public PermissionOperator Operator { get; set; } = PermissionOperator.Or;
    
    /// <summary>
    /// Creates a new permission requirement.
    /// </summary>
    /// <param name="permission">The permission string (e.g., "admin.users.read")</param>
    public RequirePermissionAttribute(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission cannot be null or empty", nameof(permission));
        }
        
        Permission = permission;
    }
}

/// <summary>
/// Logical operator for combining multiple permissions.
/// </summary>
public enum PermissionOperator
{
    /// <summary>
    /// User must have at least one of the specified permissions.
    /// </summary>
    Or,
    
    /// <summary>
    /// User must have all of the specified permissions.
    /// </summary>
    And
}