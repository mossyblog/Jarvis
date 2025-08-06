using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing access control and sharing settings for UIStudio resources.
/// Manages permissions for pages, layouts, and component bindings.
/// Table: ui_studio_permission (automatic snake_case mapping)
/// </summary>
public record UIStudioPermission : IComponent, IVersionedComponent
{
    /// <summary>
    /// Unique identifier for this component instance.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The entity this component belongs to.
    /// Required by IComponent interface.
    /// </summary>
    public Guid OwnerEntityId { get; set; }

    /// <summary>
    /// Entity ID of the resource this permission applies to.
    /// Can be a page, layout, or component binding entity.
    /// Maps to resource_entity_id in database.
    /// </summary>
    public Guid ResourceEntityId { get; init; }

    /// <summary>
    /// Type of resource: "page", "layout", "component_binding".
    /// Maps to resource_type in database.
    /// </summary>
    public string ResourceType { get; init; } = string.Empty;

    /// <summary>
    /// Entity ID of the user or role being granted permission.
    /// Maps to grantee_entity_id in database.
    /// </summary>
    public Guid GranteeEntityId { get; init; }

    /// <summary>
    /// Type of grantee: "user", "role", "group".
    /// Maps to grantee_type in database.
    /// </summary>
    public string GranteeType { get; init; } = "user";

    /// <summary>
    /// Permission level: "view", "edit", "admin", "owner".
    /// Maps to permission_level in database.
    /// </summary>
    public string PermissionLevel { get; init; } = "view";

    /// <summary>
    /// Specific permissions granted stored as JSON.
    /// Can include fine-grained permissions like specific fields or actions.
    /// Maps to specific_permissions in database.
    /// </summary>
    public Dictionary<string, object>? SpecificPermissions { get; init; }

    /// <summary>
    /// Whether this permission is inherited from a parent resource.
    /// Maps to is_inherited in database.
    /// </summary>
    public bool IsInherited { get; init; } = false;

    /// <summary>
    /// Whether this permission can be inherited by child resources.
    /// Maps to is_inheritable in database.
    /// </summary>
    public bool IsInheritable { get; init; } = false;

    /// <summary>
    /// Entity ID of the parent resource if permission is inherited.
    /// Maps to inherited_from_entity_id in database.
    /// </summary>
    public Guid? InheritedFromEntityId { get; init; }

    /// <summary>
    /// Whether this permission can be further shared by the grantee.
    /// Maps to can_share in database.
    /// </summary>
    public bool CanShare { get; init; } = false;

    /// <summary>
    /// Expiration date for temporary permissions.
    /// Maps to expires_at in database.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// Conditions that must be met for this permission to be active.
    /// Stored as JSON (e.g., time-based, location-based conditions).
    /// Maps to conditions in database.
    /// </summary>
    public Dictionary<string, object>? Conditions { get; init; }

    /// <summary>
    /// Additional metadata about the permission stored as JSON.
    /// Maps to permission_metadata in database.
    /// </summary>
    public Dictionary<string, object>? PermissionMetadata { get; init; }

    /// <summary>
    /// Entity ID of the user who granted this permission.
    /// Maps to granted_by_entity_id in database.
    /// </summary>
    public Guid GrantedByEntityId { get; init; }

    /// <summary>
    /// When the permission was granted.
    /// Maps to granted_at in database.
    /// </summary>
    public DateTime GrantedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the permission was last updated.
    /// Required by IComponent interface.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version number for change tracking.
    /// Required by IVersionedComponent interface.
    /// </summary>
    public int? Version { get; set; }

    /// <summary>
    /// Optional reason or note for granting this permission.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Whether this permission is currently active.
    /// Maps to is_active in database.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// When this permission was revoked (if applicable).
    /// Maps to revoked_at in database.
    /// </summary>
    public DateTime? RevokedAt { get; init; }

    /// <summary>
    /// Reason for revoking this permission.
    /// Maps to revocation_reason in database.
    /// </summary>
    public string? RevocationReason { get; init; }
}