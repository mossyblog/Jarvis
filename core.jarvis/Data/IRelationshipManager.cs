namespace core.jarvis.Data;

/// <summary>
/// Entity relationship management interface.
/// </summary>
public interface IRelationshipManager
{
    Task<Guid?> Parent(Guid entityId);
    Task<List<Guid>> Children(Guid entityId);
    Task<bool> ChildOf(Guid childId, Guid parentId);
    Task LinkRelationship(Guid parentId, Guid childId, string? parentType = null, string? childType = null);
    Task UnlinkRelationship(Guid parentId, Guid childId);
    Task<List<Guid>> Ancestors(Guid entityId);
    Task<List<Guid>> Descendants(Guid entityId);
}
