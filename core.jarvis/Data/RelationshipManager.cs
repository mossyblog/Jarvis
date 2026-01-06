using Microsoft.Extensions.Logging;

namespace core.jarvis.Data;

/// <summary>
/// Manages entity parent-child relationships.
/// </summary>
public class RelationshipManager : IRelationshipManager
{
    private readonly IPgClient _pgClient;
    private readonly ILogger<RelationshipManager> _logger;

    public RelationshipManager(IPgClient pgClient, ILogger<RelationshipManager> logger)
    {
        _pgClient = pgClient;
        _logger = logger;
    }

    public async Task<Guid?> Parent(Guid entityId)
    {
        try
        {
            var result = await _pgClient.From<Entity>()
                .Filter("id", "eq", entityId)
                .SingleOrDefault();

            if (result?.ParentId == Guid.Empty)
                return null;

            return result?.ParentId;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<Guid>> Children(Guid entityId)
    {
        try
        {
            var results = await _pgClient.From<Entity>()
                .Filter("parent_id", "eq", entityId)
                .Get();

            return results.Select(e => e.Id).ToList();
        }
        catch
        {
            return new List<Guid>();
        }
    }

    public async Task<bool> ChildOf(Guid childId, Guid parentId)
    {
        var parent = await Parent(childId);
        return parent == parentId;
    }

    public async Task LinkRelationship(Guid parentId, Guid childId, string? parentType = null, string? childType = null)
    {
        try
        {
            var child = await _pgClient.From<Entity>()
                .Filter("id", "eq", childId)
                .SingleOrDefault();

            if (child != null)
            {
                child.ParentId = parentId;
                await _pgClient.From<Entity>().Update(child);
            }
            else
            {
                var newEntity = new Entity
                {
                    Id = childId,
                    ParentId = parentId
                };
                await _pgClient.From<Entity>().Insert(newEntity);
            }

            _logger.LogDebug("Linked relationship: {ChildId} -> {ParentId}", childId, parentId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to link relationship {ChildId} -> {ParentId}", childId, parentId);
        }
    }

    public async Task UnlinkRelationship(Guid parentId, Guid childId)
    {
        try
        {
            var child = await _pgClient.From<Entity>()
                .Filter("id", "eq", childId)
                .SingleOrDefault();

            if (child != null && child.ParentId == parentId)
            {
                child.ParentId = Guid.Empty;
                await _pgClient.From<Entity>().Update(child);
            }

            _logger.LogDebug("Unlinked relationship: {ChildId} -/-> {ParentId}", childId, parentId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to unlink relationship {ChildId} -> {ParentId}", childId, parentId);
        }
    }

    public async Task<List<Guid>> Ancestors(Guid entityId)
    {
        var ancestors = new List<Guid>();
        var currentId = entityId;

        while (true)
        {
            var parent = await Parent(currentId);
            if (parent == null) break;

            ancestors.Add(parent.Value);
            currentId = parent.Value;
        }

        return ancestors;
    }

    public async Task<List<Guid>> Descendants(Guid entityId)
    {
        var descendants = new List<Guid>();
        var toProcess = new Queue<Guid>();
        toProcess.Enqueue(entityId);

        while (toProcess.Count > 0)
        {
            var current = toProcess.Dequeue();
            var children = await Children(current);

            foreach (var child in children)
            {
                descendants.Add(child);
                toProcess.Enqueue(child);
            }
        }

        return descendants;
    }
}
