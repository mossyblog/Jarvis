using Microsoft.Extensions.Logging;

namespace core.jarvis.Data;

/// <summary>
/// Manages entity parent-child relationships.
/// </summary>
public class RelationshipManager : IRelationshipManager
{
    private readonly IPgClient _pgClient;
    private readonly IAuditService _auditService;
    private readonly ILogger<RelationshipManager> _logger;

    public RelationshipManager(IPgClient pgClient, IAuditService auditService, ILogger<RelationshipManager> logger)
    {
        _pgClient = pgClient;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Guid?> Parent(Guid entityId)
    {
        try
        {
            var result = await _pgClient.From<Entity>()
                .Filter("id", "eq", entityId)
                .SingleOrDefault();

            var parentId = result?.ParentId == Guid.Empty ? null : result?.ParentId;

            // Log relationship query audit event
            await _auditService.LogEvent(
                AuditEventTypes.RelationshipQueried,
                entityId,
                new {
                    Operation = "GetParent",
                    EntityId = entityId,
                    ParentId = parentId
                });

            return parentId;
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

            var childIds = results.Select(e => e.Id).ToList();

            // Log relationship query audit event
            await _auditService.LogEvent(
                AuditEventTypes.RelationshipQueried,
                entityId,
                new {
                    Operation = "GetChildren",
                    EntityId = entityId,
                    ChildCount = childIds.Count
                });

            return childIds;
        }
        catch
        {
            return new List<Guid>();
        }
    }

    public async Task<bool> ChildOf(Guid childId, Guid parentId)
    {
        var parent = await Parent(childId);
        var isChild = parent == parentId;

        // Log relationship query audit event
        await _auditService.LogEvent(
            AuditEventTypes.RelationshipQueried,
            childId,
            new {
                Operation = "CheckParentChild",
                ChildId = childId,
                ParentId = parentId,
                IsChild = isChild
            });

        return isChild;
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

            _logger.LogDebug("Linked relationship: {ParentId} -> {ChildId}", parentId, childId);

            // Log relationship created audit event
            await _auditService.LogEvent(
                AuditEventTypes.RelationshipCreated,
                parentId,
                new {
                    ParentId = parentId,
                    ChildId = childId,
                    ParentType = parentType,
                    ChildType = childType
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to link relationship {ParentId} -> {ChildId}", parentId, childId);
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

            _logger.LogDebug("Unlinked relationship: {ParentId} -/-> {ChildId}", parentId, childId);

            // Log relationship removed audit event
            await _auditService.LogEvent(
                AuditEventTypes.RelationshipRemoved,
                parentId,
                new {
                    ParentId = parentId,
                    ChildId = childId
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to unlink relationship {ParentId} -> {ChildId}", parentId, childId);
        }
    }

    public async Task<List<Guid>> Ancestors(Guid entityId)
    {
        var ancestors = new List<Guid>();
        var currentId = entityId;
        var visited = new HashSet<Guid> { entityId };

        while (true)
        {
            // Get parent directly without triggering additional audit events
            var result = await _pgClient.From<Entity>()
                .Filter("id", "eq", currentId)
                .SingleOrDefault();

            var parentId = result?.ParentId == Guid.Empty ? null : result?.ParentId;
            if (parentId == null) break;

            // Circular reference detection
            if (visited.Contains(parentId.Value))
            {
                _logger.LogWarning("Circular reference detected in ancestry chain for entity {EntityId}", entityId);
                break;
            }

            visited.Add(parentId.Value);
            ancestors.Add(parentId.Value);
            currentId = parentId.Value;
        }

        // Log relationship query audit event for GetAncestors
        await _auditService.LogEvent(
            AuditEventTypes.RelationshipQueried,
            entityId,
            new {
                Operation = "GetAncestors",
                EntityId = entityId,
                AncestorCount = ancestors.Count
            });

        return ancestors;
    }

    public async Task<List<Guid>> Descendants(Guid entityId)
    {
        var descendants = new List<Guid>();
        var toProcess = new Queue<Guid>();
        var visited = new HashSet<Guid> { entityId };
        toProcess.Enqueue(entityId);

        while (toProcess.Count > 0)
        {
            var current = toProcess.Dequeue();

            // Get children directly without triggering additional audit events
            var results = await _pgClient.From<Entity>()
                .Filter("parent_id", "eq", current)
                .Get();

            var childIds = results.Select(e => e.Id).ToList();

            foreach (var child in childIds)
            {
                // Circular reference detection
                if (visited.Contains(child))
                {
                    _logger.LogWarning("Circular reference detected in descendant chain for entity {EntityId}", entityId);
                    continue;
                }

                visited.Add(child);
                descendants.Add(child);
                toProcess.Enqueue(child);
            }
        }

        // Log relationship query audit event for GetDescendants
        await _auditService.LogEvent(
            AuditEventTypes.RelationshipQueried,
            entityId,
            new {
                Operation = "GetDescendants",
                EntityId = entityId,
                DescendantCount = descendants.Count
            });

        return descendants;
    }
}
