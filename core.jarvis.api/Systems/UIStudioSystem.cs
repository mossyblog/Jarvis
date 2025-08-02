using core.jarvis.Data;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace core.jarvis.api.Systems;

/// <summary>
/// System for orchestrating UIStudio operations across multiple component handlers.
/// Provides high-level operations for page management, layout configuration, and template handling.
/// </summary>
public class UIStudioSystem
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<UIStudioSystem> _logger;

    public UIStudioSystem(IDataContext dataContext, ILogger<UIStudioSystem> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    /// <summary>
    /// Creates a complete page with layout and initial component bindings.
    /// </summary>
    /// <param name="pageName">Name of the page</param>
    /// <param name="pageSlug">URL slug for the page</param>
    /// <param name="pageType">Type of page (dynamic, fixed, hybrid)</param>
    /// <param name="layoutConfig">Layout configuration</param>
    /// <param name="createdById">Entity ID of the creator</param>
    /// <param name="description">Optional page description</param>
    /// <param name="templateId">Optional template to use</param>
    /// <returns>List of created components (page, layout, and any bindings)</returns>
    public async Task<List<IComponent>> CreatePage(
        string pageName,
        string pageSlug,
        string pageType,
        Dictionary<string, object> layoutConfig,
        Guid createdById,
        string? description = null,
        Guid? templateId = null)
    {
        var components = new List<IComponent>();
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Create page entity first
            var pageEntity = _dataContext.NewEntity();
            var pageHandler = _dataContext.For<UIStudioPageHandler>(pageEntity.Id);

            var page = new UIStudioPage
            {
                OwnerEntityId = pageEntity.Id,
                PageName = pageName,
                PageSlug = pageSlug,
                PageType = pageType,
                Description = description,
                CreatedByEntityId = createdById
            };

            var createdPage = await pageHandler.CreatePage(page);
            components.Add(createdPage);

            // Create layout entity
            var layoutEntity = _dataContext.NewEntity();
            var layoutHandler = _dataContext.For<UIStudioLayoutHandler>(layoutEntity.Id);

            var layout = new UIStudioLayout
            {
                OwnerEntityId = layoutEntity.Id,
                LayoutName = $"{pageName} Layout",
                LayoutType = layoutConfig.GetValueOrDefault("type", "bento").ToString() ?? "bento",
                GridConfig = layoutConfig,
                CreatedByEntityId = createdById
            };

            var createdLayout = await layoutHandler.CreateLayout(layout);
            components.Add(createdLayout);

            // Link page -> layout (parent -> child)
            await _dataContext.LinkRelationship(
                pageEntity.Id,      // parent
                layoutEntity.Id,    // child
                "UIStudioPage",     // parent type
                "UIStudioLayout"    // child type
            );

            // If using a template, create component bindings from template
            if (templateId.HasValue)
            {
                var templateBindings = await CreateComponentBindingsFromTemplate(
                    pageEntity.Id,
                    pageSlug,
                    templateId.Value, 
                    createdById);
                components.AddRange(templateBindings);
            }

            // Create version snapshot
            await CreatePageVersion(pageEntity.Id, components, createdById, "Initial page creation");

            // Log the action
            await LogAuditAction(
                createdById,
                "create",
                $"Created page '{pageName}' with layout",
                pageEntity.Id,
                "page",
                new Dictionary<string, object>
                {
                    { "page_slug", pageSlug },
                    { "page_type", pageType },
                    { "layout_entity_id", layoutEntity.Id },
                    { "template_used", templateId.HasValue }
                },
                correlationId: correlationId);

            return components;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create page {PageName}", pageName);
            
            await LogAuditAction(
                createdById,
                "create",
                $"Failed to create page '{pageName}'",
                isSuccess: false,
                errorMessage: ex.Message,
                correlationId: correlationId);

            throw;
        }
    }

    /// <summary>
    /// Updates a page and creates a new version snapshot.
    /// </summary>
    /// <param name="pageId">Entity ID of the page to update</param>
    /// <param name="updates">Dictionary of updates to apply</param>
    /// <param name="modifiedById">Entity ID of the user making changes</param>
    /// <param name="changeSummary">Summary of changes made</param>
    /// <returns>List of updated components</returns>
    public async Task<List<IComponent>> UpdatePage(
        Guid pageId,
        Dictionary<string, object> updates,
        Guid modifiedById,
        string? changeSummary = null)
    {
        var components = new List<IComponent>();
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var pageHandler = _dataContext.For<UIStudioPageHandler>(pageId);
            var currentPage = await pageHandler.Get() ?? throw new InvalidOperationException("Page not found");

            // Apply updates to page
            var updatedPage = ApplyPageUpdates(currentPage, updates, modifiedById);
            var savedPage = await pageHandler.UpdatePage(updatedPage);
            components.Add(savedPage);

            // Update layout if layout changes are included
            if (updates.ContainsKey("layout_config"))
            {
                // Find child layout entity
                var childEntityIds = await _dataContext.Children(pageId);
                foreach (var childId in childEntityIds)
                {
                    try
                    {
                        var layoutHandler = _dataContext.For<UIStudioLayoutHandler>(childId);
                        var layout = await layoutHandler.Get();
                        if (layout != null)
                        {
                            var layoutConfig = (Dictionary<string, object>)updates["layout_config"];
                            var updatedLayout = await layoutHandler.UpdateGridConfig(layoutConfig);
                            components.Add(updatedLayout);
                            break; // Assuming one layout per page
                        }
                    }
                    catch
                    {
                        // Not a layout, continue
                    }
                }
            }

            // Create version snapshot
            await CreatePageVersion(pageId, components, modifiedById, changeSummary ?? "Page updated");

            // Log the action
            await LogAuditAction(
                modifiedById,
                "update",
                $"Updated page '{currentPage.PageName}'",
                pageId,
                "page",
                new Dictionary<string, object>
                {
                    { "changes", updates.Keys.ToArray() },
                    { "change_summary", changeSummary ?? "Page updated" }
                },
                correlationId: correlationId);

            return components;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update page {PageId}", pageId);
            
            await LogAuditAction(
                modifiedById,
                "update",
                $"Failed to update page",
                pageId,
                "page",
                isSuccess: false,
                errorMessage: ex.Message,
                correlationId: correlationId);

            throw;
        }
    }

    /// <summary>
    /// Publishes a page, making it accessible to users.
    /// </summary>
    /// <param name="pageId">Entity ID of the page to publish</param>
    /// <param name="publishedById">Entity ID of the user publishing the page</param>
    /// <returns>List of components involved in publishing</returns>
    public async Task<List<IComponent>> PublishPage(Guid pageId, Guid publishedById)
    {
        var components = new List<IComponent>();
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var pageHandler = _dataContext.For<UIStudioPageHandler>(pageId);
            var publishedPage = await pageHandler.PublishPage();
            components.Add(publishedPage);

            // Create published version snapshot
            var pageData = await GetPageSnapshot(pageId);
            var versionHandler = _dataContext.For<UIStudioVersionHandler>(Guid.NewGuid());
            var publishedVersion = await versionHandler.CreatePublishedVersion(
                pageId,
                "page",
                pageData,
                publishedById,
                $"Published v{await versionHandler.GetNextVersionNumber(pageId)}",
                "Page published to production");

            components.Add(publishedVersion);

            // Log the action
            await LogAuditAction(
                publishedById,
                "publish",
                $"Published page '{publishedPage.PageName}'",
                pageId,
                "page",
                securityLevel: "medium",
                correlationId: correlationId);

            return components;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish page {PageId}", pageId);
            
            await LogAuditAction(
                publishedById,
                "publish",
                "Failed to publish page",
                pageId,
                "page",
                isSuccess: false,
                errorMessage: ex.Message,
                securityLevel: "medium",
                correlationId: correlationId);

            throw;
        }
    }

    /// <summary>
    /// Gets a page with all its related components (layout, bindings) using ECS relationships.
    /// </summary>
    /// <param name="pageSlug">Page slug to retrieve</param>
    /// <returns>List of all components related to the page</returns>
    public async Task<List<IComponent>> GetPageWithRelatedComponents(string pageSlug)
    {
        var components = new List<IComponent>();
        
        // Find page by slug
        var pageQuery = await _dataContext.Query()
            .WithAll<UIStudioPage>(p => p.PageSlug == pageSlug)
            .ToEntityComponents();
            
        var pageEntity = pageQuery.FirstOrDefault();
        if (pageEntity.Value == null) return components;
        
        // Add page component
        var page = pageEntity.Value.Get<UIStudioPage>();
        if (page != null)
        {
            components.Add(page);
        }
        var pageEntityId = pageEntity.Key;
        
        // Get child entities (layouts and bindings)
        var childEntityIds = await _dataContext.Children(pageEntityId);
        foreach (var childId in childEntityIds)
        {
            // Try to get as layout
            try
            {
                var layoutHandler = _dataContext.For<UIStudioLayoutHandler>(childId);
                var layout = await layoutHandler.Get();
                if (layout != null)
                {
                    components.Add(layout);
                }
            }
            catch
            {
                // Not a layout, might be a binding
            }
            
            // Try to get as binding
            try
            {
                var bindingHandler = _dataContext.For<UIStudioComponentBindingHandler>(childId);
                var binding = await bindingHandler.Get();
                if (binding != null)
                {
                    components.Add(binding);
                }
            }
            catch
            {
                // Not a binding either
            }
        }
        
        return components;
    }

    /// <summary>
    /// Creates a template from an existing page.
    /// </summary>
    /// <param name="pageId">Entity ID of the source page</param>
    /// <param name="templateName">Name for the template</param>
    /// <param name="category">Template category</param>
    /// <param name="description">Template description</param>
    /// <param name="createdById">Entity ID of the creator</param>
    /// <param name="isPublic">Whether the template should be public</param>
    /// <returns>The created template component</returns>
    public async Task<UIStudioTemplate> CreateTemplateFromPage(
        Guid pageId,
        string templateName,
        string category,
        string? description,
        Guid createdById,
        bool isPublic = false)
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Get page snapshot
            var pageData = await GetPageSnapshot(pageId);

            // Create template entity
            var templateEntity = _dataContext.NewEntity();
            var templateHandler = _dataContext.For<UIStudioTemplateHandler>(templateEntity.Id);

            var template = new UIStudioTemplate
            {
                OwnerEntityId = templateEntity.Id,
                TemplateName = templateName,
                TemplateType = "page",
                Category = category,
                Description = description,
                TemplateData = pageData,
                IsPublic = isPublic,
                CreatedByEntityId = createdById
            };

            var createdTemplate = await templateHandler.CreateTemplate(template);

            // Log the action
            await LogAuditAction(
                createdById,
                "create",
                $"Created template '{templateName}' from page",
                templateEntity.Id,
                "template",
                new Dictionary<string, object>
                {
                    { "source_page_entity_id", pageId },
                    { "template_category", category },
                    { "is_public", isPublic }
                },
                correlationId: correlationId);

            return createdTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template from page {PageId}", pageId);
            
            await LogAuditAction(
                createdById,
                "create",
                $"Failed to create template '{templateName}' from page",
                isSuccess: false,
                errorMessage: ex.Message,
                correlationId: correlationId);

            throw;
        }
    }

    /// <summary>
    /// Grants permission for a UIStudio resource.
    /// </summary>
    /// <param name="resourceId">Entity ID of the resource</param>
    /// <param name="resourceType">Type of resource</param>
    /// <param name="granteeId">Entity ID of the grantee</param>
    /// <param name="granteeType">Type of grantee</param>
    /// <param name="permissionLevel">Permission level to grant</param>
    /// <param name="grantedById">Entity ID of the user granting permission</param>
    /// <param name="expiresAt">Optional expiration date</param>
    /// <returns>The created permission component</returns>
    public async Task<UIStudioPermission> GrantPermission(
        Guid resourceId,
        string resourceType,
        Guid granteeId,
        string granteeType,
        string permissionLevel,
        Guid grantedById,
        DateTime? expiresAt = null)
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var permissionEntity = _dataContext.NewEntity();
            var permissionHandler = _dataContext.For<UIStudioPermissionHandler>(permissionEntity.Id);

            var permission = new UIStudioPermission
            {
                OwnerEntityId = permissionEntity.Id,
                ResourceEntityId = resourceId,
                ResourceType = resourceType,
                GranteeEntityId = granteeId,
                GranteeType = granteeType,
                PermissionLevel = permissionLevel,
                GrantedByEntityId = grantedById,
                ExpiresAt = expiresAt
            };

            var grantedPermission = await permissionHandler.GrantPermission(permission);

            // Log the action
            await LogAuditAction(
                grantedById,
                "share",
                $"Granted {permissionLevel} permission to {granteeType}",
                resourceId,
                resourceType,
                new Dictionary<string, object>
                {
                    { "grantee_entity_id", granteeId },
                    { "grantee_type", granteeType },
                    { "permission_level", permissionLevel },
                    { "expires_at", expiresAt?.ToString() ?? "never" }
                },
                securityLevel: permissionLevel == "owner" ? "high" : "medium",
                correlationId: correlationId);

            return grantedPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant permission for resource {ResourceId}", resourceId);
            
            await LogAuditAction(
                grantedById,
                "share",
                "Failed to grant permission",
                resourceId,
                resourceType,
                isSuccess: false,
                errorMessage: ex.Message,
                securityLevel: "high",
                correlationId: correlationId);

            throw;
        }
    }

    /// <summary>
    /// Gets a complete page snapshot including all related components.
    /// </summary>
    /// <param name="pageId">Entity ID of the page</param>
    /// <returns>Dictionary containing complete page data</returns>
    private async Task<Dictionary<string, object>> GetPageSnapshot(Guid pageId)
    {
        var pageHandler = _dataContext.For<UIStudioPageHandler>(pageId);
        var page = await pageHandler.Get() ?? throw new InvalidOperationException("Page not found");

        var snapshot = new Dictionary<string, object>
        {
            { "page", page }
        };

        // Get child entities
        var childEntityIds = await _dataContext.Children(pageId);
        var layouts = new List<UIStudioLayout>();
        var bindings = new List<UIStudioComponentBinding>();
        
        foreach (var childId in childEntityIds)
        {
            // Try to get as layout
            try
            {
                var layoutHandler = _dataContext.For<UIStudioLayoutHandler>(childId);
                var layout = await layoutHandler.Get();
                if (layout != null)
                {
                    layouts.Add(layout);
                }
            }
            catch
            {
                // Not a layout
            }
            
            // Try to get as binding
            try
            {
                var bindingHandler = _dataContext.For<UIStudioComponentBindingHandler>(childId);
                var binding = await bindingHandler.Get();
                if (binding != null)
                {
                    bindings.Add(binding);
                }
            }
            catch
            {
                // Not a binding
            }
        }
        
        if (layouts.Any())
        {
            snapshot["layout"] = layouts.First(); // Assuming one layout per page
        }
        
        snapshot["component_bindings"] = bindings;

        return snapshot;
    }

    /// <summary>
    /// Creates component bindings from a template.
    /// </summary>
    /// <param name="pageId">Entity ID of the target page</param>
    /// <param name="pageSlug">Slug of the page</param>
    /// <param name="templateId">Entity ID of the template</param>
    /// <param name="createdById">Entity ID of the creator</param>
    /// <returns>List of created component bindings</returns>
    private async Task<List<UIStudioComponentBinding>> CreateComponentBindingsFromTemplate(
        Guid pageId,
        string pageSlug,
        Guid templateId,
        Guid createdById)
    {
        var templateHandler = _dataContext.For<UIStudioTemplateHandler>(templateId);
        var template = await templateHandler.Get() ?? throw new InvalidOperationException("Template not found");

        var bindings = new List<UIStudioComponentBinding>();

        if (template.TemplateData?.ContainsKey("component_bindings") == true)
        {
            var templateBindings = (List<object>)template.TemplateData["component_bindings"];
            
            foreach (var bindingData in templateBindings)
            {
                var bindingEntity = _dataContext.NewEntity();
                var bindingHandler = _dataContext.For<UIStudioComponentBindingHandler>(bindingEntity.Id);

                // Create binding from template data (would need proper deserialization)
                var binding = new UIStudioComponentBinding
                {
                    OwnerEntityId = bindingEntity.Id,
                    PageSlug = pageSlug,
                    // Map other properties from template data
                    CreatedByEntityId = createdById
                };

                var createdBinding = await bindingHandler.CreateBinding(binding);
                bindings.Add(createdBinding);
                
                // Link page -> binding (parent -> child)
                await _dataContext.LinkRelationship(
                    pageId,                      // parent
                    bindingEntity.Id,            // child
                    "UIStudioPage",              // parent type
                    "UIStudioComponentBinding"   // child type
                );
            }
        }

        return bindings;
    }

    /// <summary>
    /// Creates a version snapshot for a page.
    /// </summary>
    /// <param name="pageId">Entity ID of the page</param>
    /// <param name="components">Components to include in snapshot</param>
    /// <param name="createdById">Entity ID of the creator</param>
    /// <param name="changeSummary">Summary of changes</param>
    private async Task CreatePageVersion(
        Guid pageId,
        List<IComponent> components,
        Guid createdById,
        string changeSummary)
    {
        var snapshotData = new Dictionary<string, object>
        {
            { "components", components.Select(c => new { Type = c.GetType().Name, Data = c }).ToList() },
            { "timestamp", DateTime.UtcNow }
        };

        var versionEntity = _dataContext.NewEntity();
        var versionHandler = _dataContext.For<UIStudioVersionHandler>(versionEntity.Id);

        await versionHandler.CreateAutoVersion(
            pageId,
            "page",
            snapshotData,
            createdById,
            changeSummary);
    }

    /// <summary>
    /// Applies updates to a page component.
    /// </summary>
    /// <param name="currentPage">Current page state</param>
    /// <param name="updates">Updates to apply</param>
    /// <param name="modifiedById">Entity ID of the modifier</param>
    /// <returns>Updated page component</returns>
    private static UIStudioPage ApplyPageUpdates(UIStudioPage currentPage, Dictionary<string, object> updates, Guid modifiedById)
    {
        var updatedPage = currentPage with { ModifiedByEntityId = modifiedById, LastUpdated = DateTime.UtcNow };

        if (updates.ContainsKey("page_name"))
            updatedPage = updatedPage with { PageName = updates["page_name"].ToString() ?? updatedPage.PageName };

        if (updates.ContainsKey("page_slug"))
            updatedPage = updatedPage with { PageSlug = updates["page_slug"].ToString() ?? updatedPage.PageSlug };

        if (updates.ContainsKey("description"))
            updatedPage = updatedPage with { Description = updates["description"].ToString() };

        if (updates.ContainsKey("is_published"))
            updatedPage = updatedPage with { IsPublished = (bool)updates["is_published"] };

        if (updates.ContainsKey("is_default"))
            updatedPage = updatedPage with { IsDefault = (bool)updates["is_default"] };

        if (updates.ContainsKey("metadata"))
            updatedPage = updatedPage with { Metadata = (Dictionary<string, object>)updates["metadata"] };

        return updatedPage;
    }

    /// <summary>
    /// Logs an audit action.
    /// </summary>
    private async Task LogAuditAction(
        Guid? userId,
        string actionType,
        string actionDescription,
        Guid? resourceId = null,
        string? resourceType = null,
        Dictionary<string, object>? actionDetails = null,
        bool isSuccess = true,
        string? errorMessage = null,
        string securityLevel = "low",
        string? correlationId = null)
    {
        try
        {
            var auditEntity = _dataContext.NewEntity();
            var auditHandler = _dataContext.For<UIStudioAuditLogHandler>(auditEntity.Id);

            if (userId.HasValue)
            {
                await auditHandler.LogUserAction(
                    userId.Value,
                    actionType,
                    actionDescription,
                    resourceId,
                    resourceType,
                    actionDetails,
                    isSuccess: isSuccess,
                    errorMessage: errorMessage,
                    securityLevel: securityLevel,
                    correlationId: correlationId);
            }
            else
            {
                await auditHandler.LogSystemAction(
                    actionType,
                    actionDescription,
                    resourceId,
                    resourceType,
                    actionDetails,
                    isSuccess: isSuccess,
                    errorMessage: errorMessage,
                    securityLevel: securityLevel,
                    correlationId: correlationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit action: {ActionDescription}", actionDescription);
            // Don't throw - audit logging failure shouldn't break the main operation
        }
    }

    /// <summary>
    /// Deletes a page and all its related components.
    /// </summary>
    /// <param name="pageId">Entity ID of the page to delete</param>
    /// <param name="deletedById">Entity ID of the user deleting the page</param>
    /// <param name="reason">Reason for deletion</param>
    /// <returns>List of deleted components</returns>
    public async Task<List<IComponent>> DeletePage(Guid pageId, Guid deletedById, string? reason = null)
    {
        var components = new List<IComponent>();
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var pageHandler = _dataContext.For<UIStudioPageHandler>(pageId);
            var page = await pageHandler.Get() ?? throw new InvalidOperationException("Page not found");

            // Delete child entities (layouts and bindings)
            var childEntityIds = await _dataContext.Children(pageId);
            foreach (var childId in childEntityIds)
            {
                // Unlink the relationship first
                await _dataContext.UnlinkRelationship(pageId, childId);
                
                // Try to remove as component binding
                try
                {
                    var bindingHandler = _dataContext.For<UIStudioComponentBindingHandler>(childId);
                    var binding = await bindingHandler.Get();
                    if (binding != null)
                    {
                        await _dataContext.Remove<UIStudioComponentBinding>(binding.Id);
                    }
                }
                catch
                {
                    // Not a binding
                }
                
                // Try to remove as layout
                try
                {
                    var layoutHandler = _dataContext.For<UIStudioLayoutHandler>(childId);
                    var layout = await layoutHandler.Get();
                    if (layout != null)
                    {
                        await _dataContext.Remove<UIStudioLayout>(layout.Id);
                    }
                }
                catch
                {
                    // Not a layout
                }
            }

            // Delete the page
            await _dataContext.Remove<UIStudioPage>(page.Id);
            components.Add(page);

            // Log the action
            await LogAuditAction(
                deletedById,
                "delete",
                $"Deleted page '{page.PageName}'",
                pageId,
                "page",
                new Dictionary<string, object>
                {
                    { "page_slug", page.PageSlug },
                    { "page_type", page.PageType },
                    { "reason", reason ?? "No reason provided" }
                },
                securityLevel: "medium",
                correlationId: correlationId);

            return components;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete page {PageId}", pageId);
            
            await LogAuditAction(
                deletedById,
                "delete",
                "Failed to delete page",
                pageId,
                "page",
                isSuccess: false,
                errorMessage: ex.Message,
                securityLevel: "medium",
                correlationId: correlationId);

            throw;
        }
    }

    /// <summary>
    /// Duplicates an existing page with optional modifications.
    /// </summary>
    /// <param name="sourcePageId">Entity ID of the page to duplicate</param>
    /// <param name="newPageName">Name for the new page</param>
    /// <param name="newPageSlug">Slug for the new page</param>
    /// <param name="createdById">Entity ID of the user creating the duplicate</param>
    /// <param name="includeBindings">Whether to duplicate component bindings</param>
    /// <returns>List of created components</returns>
    public async Task<List<IComponent>> DuplicatePage(
        Guid sourcePageId,
        string newPageName,
        string? newPageSlug,
        Guid createdById,
        bool includeBindings = true)
    {
        var components = new List<IComponent>();
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Get source page
            var sourcePageHandler = _dataContext.For<UIStudioPageHandler>(sourcePageId);
            var sourcePage = await sourcePageHandler.Get() ?? throw new InvalidOperationException("Source page not found");

            // Generate slug if not provided
            if (string.IsNullOrWhiteSpace(newPageSlug))
            {
                newPageSlug = newPageName.ToLowerInvariant().Replace(' ', '-');
            }

            // Create new page
            var newPageEntity = _dataContext.NewEntity();
            var newPageHandler = _dataContext.For<UIStudioPageHandler>(newPageEntity.Id);
            
            var duplicatedPage = sourcePage with
            {
                OwnerEntityId = newPageEntity.Id,
                PageName = newPageName,
                PageSlug = newPageSlug,
                IsPublished = false, // Duplicated pages start as unpublished
                IsDefault = false, // Duplicated pages are never default
                CreatedByEntityId = createdById,
                ModifiedByEntityId = null,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            
            var createdPage = await newPageHandler.CreatePage(duplicatedPage);
            components.Add(createdPage);

            // Duplicate child entities (layout and bindings)
            var sourceChildIds = await _dataContext.Children(sourcePageId);
            foreach (var sourceChildId in sourceChildIds)
            {
                // Try to duplicate as layout
                try
                {
                    var sourceLayoutHandler = _dataContext.For<UIStudioLayoutHandler>(sourceChildId);
                    var sourceLayout = await sourceLayoutHandler.Get();
                    if (sourceLayout != null)
                    {
                        var newLayoutEntity = _dataContext.NewEntity();
                        var newLayoutHandler = _dataContext.For<UIStudioLayoutHandler>(newLayoutEntity.Id);
                        
                        var duplicatedLayout = sourceLayout with
                        {
                            OwnerEntityId = newLayoutEntity.Id,
                            LayoutName = $"{newPageName} Layout",
                            CreatedByEntityId = createdById,
                            CreatedAt = DateTime.UtcNow,
                            LastUpdated = DateTime.UtcNow
                        };
                        
                        var createdLayout = await newLayoutHandler.CreateLayout(duplicatedLayout);
                        components.Add(createdLayout);
                        
                        // Link new page -> new layout
                        await _dataContext.LinkRelationship(
                            newPageEntity.Id,
                            newLayoutEntity.Id,
                            "UIStudioPage",
                            "UIStudioLayout"
                        );
                    }
                }
                catch
                {
                    // Not a layout
                }
            }

            // Duplicate component bindings if requested
            if (includeBindings)
            {
                foreach (var sourceChildId in sourceChildIds)
                {
                    // Try to duplicate as binding
                    try
                    {
                        var sourceBindingHandler = _dataContext.For<UIStudioComponentBindingHandler>(sourceChildId);
                        var sourceBinding = await sourceBindingHandler.Get();
                        if (sourceBinding != null)
                        {
                            var newBindingEntity = _dataContext.NewEntity();
                            var newBindingHandler = _dataContext.For<UIStudioComponentBindingHandler>(newBindingEntity.Id);
                            
                            var duplicatedBinding = sourceBinding with
                            {
                                OwnerEntityId = newBindingEntity.Id,
                                PageSlug = newPageSlug,
                                CreatedByEntityId = createdById,
                                CreatedAt = DateTime.UtcNow,
                                LastUpdated = DateTime.UtcNow
                            };
                            
                            var createdBinding = await newBindingHandler.CreateBinding(duplicatedBinding);
                            components.Add(createdBinding);
                            
                            // Link new page -> new binding
                            await _dataContext.LinkRelationship(
                                newPageEntity.Id,
                                newBindingEntity.Id,
                                "UIStudioPage",
                                "UIStudioComponentBinding"
                            );
                        }
                    }
                    catch
                    {
                        // Not a binding
                    }
                }
            }

            // Create version snapshot
            await CreatePageVersion(newPageEntity.Id, components, createdById, "Initial page creation (duplicated)");

            // Log the action
            await LogAuditAction(
                createdById,
                "duplicate",
                $"Duplicated page '{sourcePage.PageName}' as '{newPageName}'",
                newPageEntity.Id,
                "page",
                new Dictionary<string, object>
                {
                    { "source_page_entity_id", sourcePageId },
                    { "source_page_name", sourcePage.PageName },
                    { "new_page_slug", newPageSlug },
                    { "include_bindings", includeBindings }
                },
                correlationId: correlationId);

            return components;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duplicate page {SourcePageId}", sourcePageId);
            
            await LogAuditAction(
                createdById,
                "duplicate",
                "Failed to duplicate page",
                sourcePageId,
                "page",
                isSuccess: false,
                errorMessage: ex.Message,
                correlationId: correlationId);

            throw;
        }
    }

    /// <summary>
    /// Applies a template to create a new page.
    /// </summary>
    /// <param name="templateId">Entity ID of the template to apply</param>
    /// <param name="pageName">Name for the new page</param>
    /// <param name="pageSlug">Slug for the new page</param>
    /// <param name="createdById">Entity ID of the user creating the page</param>
    /// <param name="customizations">Optional customizations to apply</param>
    /// <returns>List of created components</returns>
    public async Task<List<IComponent>> ApplyTemplate(
        Guid templateId,
        string pageName,
        string pageSlug,
        Guid createdById,
        Dictionary<string, object>? customizations = null)
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Get template
            var templateHandler = _dataContext.For<UIStudioTemplateHandler>(templateId);
            var template = await templateHandler.Get() ?? throw new InvalidOperationException("Template not found");

            // Extract template data
            if (template.TemplateData == null)
            {
                throw new InvalidOperationException("Template has no data");
            }

            // Use CreatePage with template for consistency
            var layoutConfig = customizations?.GetValueOrDefault("layout_config", new Dictionary<string, object>()) as Dictionary<string, object>
                ?? new Dictionary<string, object>();
            
            var components = await CreatePage(
                pageName,
                pageSlug,
                "dynamic", // Default to dynamic unless specified in customizations
                layoutConfig,
                createdById,
                customizations?.GetValueOrDefault("description", null)?.ToString(),
                templateId);

            // Log the action
            await LogAuditAction(
                createdById,
                "apply_template",
                $"Applied template '{template.TemplateName}' to create page '{pageName}'",
                resourceId: components.FirstOrDefault()?.OwnerEntityId,
                "page",
                new Dictionary<string, object>
                {
                    { "template_entity_id", templateId },
                    { "template_name", template.TemplateName },
                    { "page_slug", pageSlug },
                    { "has_customizations", customizations != null }
                },
                correlationId: correlationId);

            return components;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply template {TemplateId}", templateId);
            
            await LogAuditAction(
                createdById,
                "apply_template",
                "Failed to apply template",
                templateId,
                "template",
                isSuccess: false,
                errorMessage: ex.Message,
                correlationId: correlationId);

            throw;
        }
    }

    /// <summary>
    /// Updates a template configuration.
    /// </summary>
    /// <param name="templateId">Entity ID of the template to update</param>
    /// <param name="updates">Updates to apply</param>
    /// <param name="modifiedById">Entity ID of the user making updates</param>
    /// <returns>Updated template</returns>
    public async Task<UIStudioTemplate> UpdateTemplate(
        Guid templateId,
        Dictionary<string, object> updates,
        Guid modifiedById)
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var templateHandler = _dataContext.For<UIStudioTemplateHandler>(templateId);
            var currentTemplate = await templateHandler.Get() ?? throw new InvalidOperationException("Template not found");

            // Apply updates to template
            var updatedTemplate = ApplyTemplateUpdates(currentTemplate, updates, modifiedById);
            var savedTemplate = await templateHandler.UpdateTemplate(updatedTemplate);

            // Log the action
            await LogAuditAction(
                modifiedById,
                "update",
                $"Updated template '{currentTemplate.TemplateName}'",
                templateId,
                "template",
                new Dictionary<string, object>
                {
                    { "changes", updates.Keys.ToArray() }
                },
                correlationId: correlationId);

            return savedTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template {TemplateId}", templateId);
            
            await LogAuditAction(
                modifiedById,
                "update",
                "Failed to update template",
                templateId,
                "template",
                isSuccess: false,
                errorMessage: ex.Message,
                correlationId: correlationId);

            throw;
        }
    }

    /// <summary>
    /// Updates a permission's level or expiration.
    /// </summary>
    /// <param name="permissionId">Entity ID of the permission to update</param>
    /// <param name="permissionLevel">New permission level</param>
    /// <param name="expiresAt">New expiration date</param>
    /// <param name="modifiedById">Entity ID of the user making changes</param>
    /// <returns>Updated permission</returns>
    public async Task<UIStudioPermission> UpdatePermission(
        Guid permissionId,
        string? permissionLevel,
        DateTime? expiresAt,
        Guid modifiedById)
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var permissionHandler = _dataContext.For<UIStudioPermissionHandler>(permissionId);
            var existingPermission = await permissionHandler.Get() ?? throw new InvalidOperationException("Permission not found");
            var updatedPermission = existingPermission with 
            { 
                PermissionLevel = permissionLevel ?? existingPermission.PermissionLevel,
                ExpiresAt = expiresAt ?? existingPermission.ExpiresAt,
                LastUpdated = DateTime.UtcNow
            };
            await permissionHandler.UpdatePermission(updatedPermission);

            // Log the action
            await LogAuditAction(
                modifiedById,
                "update_permission",
                $"Updated permission level to {permissionLevel ?? "unchanged"}",
                updatedPermission.ResourceEntityId,
                updatedPermission.ResourceType,
                new Dictionary<string, object>
                {
                    { "permission_entity_id", permissionId },
                    { "grantee_entity_id", updatedPermission.GranteeEntityId },
                    { "new_permission_level", permissionLevel ?? "unchanged" },
                    { "new_expires_at", expiresAt?.ToString() ?? "unchanged" }
                },
                securityLevel: "medium",
                correlationId: correlationId);

            return updatedPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update permission {PermissionId}", permissionId);
            
            await LogAuditAction(
                modifiedById,
                "update_permission",
                "Failed to update permission",
                isSuccess: false,
                errorMessage: ex.Message,
                securityLevel: "high",
                correlationId: correlationId);

            throw;
        }
    }

    /// <summary>
    /// Revokes a permission.
    /// </summary>
    /// <param name="permissionId">Entity ID of the permission to revoke</param>
    /// <param name="revokedById">Entity ID of the user revoking permission</param>
    /// <param name="reason">Reason for revocation</param>
    public async Task RevokePermission(Guid permissionId, Guid revokedById, string? reason = null)
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var permissionHandler = _dataContext.For<UIStudioPermissionHandler>(permissionId);
            var permission = await permissionHandler.Get() ?? throw new InvalidOperationException("Permission not found");

            await _dataContext.Remove<UIStudioPermission>(permission.Id);

            // Log the action
            await LogAuditAction(
                revokedById,
                "revoke_permission",
                $"Revoked {permission.PermissionLevel} permission",
                permission.ResourceEntityId,
                permission.ResourceType,
                new Dictionary<string, object>
                {
                    { "permission_entity_id", permissionId },
                    { "grantee_entity_id", permission.GranteeEntityId },
                    { "permission_level", permission.PermissionLevel },
                    { "reason", reason ?? "No reason provided" }
                },
                securityLevel: "medium",
                correlationId: correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke permission {PermissionId}", permissionId);
            
            await LogAuditAction(
                revokedById,
                "revoke_permission",
                "Failed to revoke permission",
                isSuccess: false,
                errorMessage: ex.Message,
                securityLevel: "high",
                correlationId: correlationId);

            throw;
        }
    }

    /// <summary>
    /// Applies updates to a template component.
    /// </summary>
    /// <param name="currentTemplate">Current template state</param>
    /// <param name="updates">Updates to apply</param>
    /// <param name="modifiedById">Entity ID of the modifier</param>
    /// <returns>Updated template component</returns>
    private static UIStudioTemplate ApplyTemplateUpdates(UIStudioTemplate currentTemplate, Dictionary<string, object> updates, Guid modifiedById)
    {
        var updatedTemplate = currentTemplate with { ModifiedByEntityId = modifiedById, LastUpdated = DateTime.UtcNow };

        if (updates.ContainsKey("template_name"))
            updatedTemplate = updatedTemplate with { TemplateName = updates["template_name"].ToString() ?? updatedTemplate.TemplateName };

        if (updates.ContainsKey("description"))
            updatedTemplate = updatedTemplate with { Description = updates["description"].ToString() };

        if (updates.ContainsKey("category"))
            updatedTemplate = updatedTemplate with { Category = updates["category"].ToString() ?? updatedTemplate.Category };

        if (updates.ContainsKey("is_public"))
            updatedTemplate = updatedTemplate with { IsPublic = (bool)updates["is_public"] };

        if (updates.ContainsKey("template_data"))
            updatedTemplate = updatedTemplate with { TemplateData = (Dictionary<string, object>)updates["template_data"] };

        return updatedTemplate;
    }

    /// <summary>
    /// Creates a page from a UIStudioPage component following strict Jarvis patterns.
    /// </summary>
    /// <param name="pageComponent">Complete page component with all required data</param>
    /// <returns>List of created components</returns>
    public async Task<List<IComponent>> CreatePageFromComponent(UIStudioPage pageComponent)
    {
        return await CreatePage(
            pageComponent.PageName,
            pageComponent.PageSlug,
            pageComponent.PageType,
            pageComponent.Metadata ?? new Dictionary<string, object>(),
            pageComponent.CreatedByEntityId,
            pageComponent.Description);
    }

    /// <summary>
    /// Updates a page from a UIStudioPage component following strict Jarvis patterns.
    /// </summary>
    /// <param name="pageComponent">Updated page component</param>
    /// <returns>List of updated components</returns>
    public async Task<List<IComponent>> UpdatePageFromComponent(UIStudioPage pageComponent)
    {
        var updates = new Dictionary<string, object>
        {
            { "page_name", pageComponent.PageName },
            { "page_slug", pageComponent.PageSlug },
            { "description", pageComponent.Description ?? "" },
            { "is_published", pageComponent.IsPublished },
            { "is_default", pageComponent.IsDefault },
            { "metadata", pageComponent.Metadata ?? new Dictionary<string, object>() }
        };

        return await UpdatePage(
            pageComponent.OwnerEntityId,
            updates,
            pageComponent.ModifiedByEntityId ?? pageComponent.CreatedByEntityId,
            "Page updated via component");
    }

    /// <summary>
    /// Duplicates a page from a new page component following strict Jarvis patterns.
    /// </summary>
    /// <param name="sourcePageId">Source page entity ID</param>
    /// <param name="newPageComponent">New page component configuration</param>
    /// <returns>List of created components</returns>
    public async Task<List<IComponent>> DuplicatePageFromComponent(Guid sourcePageId, UIStudioPage newPageComponent)
    {
        return await DuplicatePage(
            sourcePageId,
            newPageComponent.PageName,
            newPageComponent.PageSlug,
            newPageComponent.CreatedByEntityId,
            true);
    }

    /// <summary>
    /// Grants permission from a UIStudioPermission component following strict Jarvis patterns.
    /// </summary>
    /// <param name="permissionComponent">Permission component to grant</param>
    /// <returns>List containing the granted permission component</returns>
    public async Task<List<IComponent>> GrantPermissionFromComponent(UIStudioPermission permissionComponent)
    {
        var permission = await GrantPermission(
            permissionComponent.ResourceEntityId,
            permissionComponent.ResourceType,
            permissionComponent.GranteeEntityId,
            permissionComponent.GranteeType,
            permissionComponent.PermissionLevel,
            permissionComponent.GrantedByEntityId,
            permissionComponent.ExpiresAt);
        
        return new List<IComponent> { permission };
    }

    /// <summary>
    /// Creates a template from a UIStudioTemplate component following strict Jarvis patterns.
    /// </summary>
    /// <param name="templateComponent">Template component to create</param>
    /// <returns>List containing the created template component</returns>
    public async Task<List<IComponent>> CreateTemplateFromComponent(UIStudioTemplate templateComponent)
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var templateEntity = _dataContext.NewEntity();
            var templateHandler = _dataContext.For<UIStudioTemplateHandler>(templateEntity.Id);

            var template = templateComponent with 
            { 
                OwnerEntityId = templateEntity.Id,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            var createdTemplate = await templateHandler.CreateTemplate(template);

            await LogAuditAction(
                templateComponent.CreatedByEntityId,
                "create",
                $"Created template '{templateComponent.TemplateName}'",
                templateEntity.Id,
                "template",
                new Dictionary<string, object>
                {
                    { "template_category", templateComponent.Category },
                    { "is_public", templateComponent.IsPublic }
                },
                correlationId: correlationId);

            return new List<IComponent> { createdTemplate };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template from component");
            
            await LogAuditAction(
                templateComponent.CreatedByEntityId,
                "create",
                $"Failed to create template '{templateComponent.TemplateName}'",
                isSuccess: false,
                errorMessage: ex.Message,
                correlationId: correlationId);

            throw;
        }
    }

    /// <summary>
    /// Creates a layout from a UIStudioLayout component following strict Jarvis patterns.
    /// </summary>
    /// <param name="layoutComponent">Layout component to create</param>
    /// <returns>List containing the created layout component</returns>
    public async Task<List<IComponent>> CreateLayoutFromComponent(UIStudioLayout layoutComponent)
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var layoutEntity = _dataContext.NewEntity();
            var layoutHandler = _dataContext.For<UIStudioLayoutHandler>(layoutEntity.Id);

            var layout = layoutComponent with 
            { 
                OwnerEntityId = layoutEntity.Id,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            var createdLayout = await layoutHandler.CreateLayout(layout);

            await LogAuditAction(
                layoutComponent.CreatedByEntityId,
                "create",
                $"Created layout '{layoutComponent.LayoutName}'",
                layoutEntity.Id,
                "layout",
                new Dictionary<string, object>
                {
                    { "layout_type", layoutComponent.LayoutType },
                    { "is_template", layoutComponent.IsTemplate }
                },
                correlationId: correlationId);

            return new List<IComponent> { createdLayout };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create layout from component");
            
            await LogAuditAction(
                layoutComponent.CreatedByEntityId,
                "create",
                $"Failed to create layout '{layoutComponent.LayoutName}'",
                isSuccess: false,
                errorMessage: ex.Message,
                correlationId: correlationId);

            throw;
        }
    }

    /// <summary>
    /// Updates a layout from a UIStudioLayout component following strict Jarvis patterns.
    /// </summary>
    /// <param name="layoutComponent">Updated layout component</param>
    /// <returns>List containing the updated layout component</returns>
    public async Task<List<IComponent>> UpdateLayoutFromComponent(UIStudioLayout layoutComponent)
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var layoutHandler = _dataContext.For<UIStudioLayoutHandler>(layoutComponent.OwnerEntityId);
            var updatedLayout = await layoutHandler.UpdateLayout(layoutComponent with { LastUpdated = DateTime.UtcNow });

            await LogAuditAction(
                layoutComponent.CreatedByEntityId,
                "update",
                $"Updated layout '{layoutComponent.LayoutName}'",
                layoutComponent.OwnerEntityId,
                "layout",
                correlationId: correlationId);

            return new List<IComponent> { updatedLayout };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update layout from component {LayoutEntityId}", layoutComponent.OwnerEntityId);
            
            await LogAuditAction(
                layoutComponent.CreatedByEntityId,
                "update",
                $"Failed to update layout '{layoutComponent.LayoutName}'",
                layoutComponent.OwnerEntityId,
                "layout",
                isSuccess: false,
                errorMessage: ex.Message,
                correlationId: correlationId);

            throw;
        }
    }

    /// <summary>
    /// Creates a component binding from a UIStudioComponentBinding component following strict Jarvis patterns.
    /// </summary>
    /// <param name="bindingComponent">Component binding to create</param>
    /// <returns>List containing the created binding component</returns>
    public async Task<List<IComponent>> CreateBindingFromComponent(UIStudioComponentBinding bindingComponent)
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var bindingEntity = _dataContext.NewEntity();
            var bindingHandler = _dataContext.For<UIStudioComponentBindingHandler>(bindingEntity.Id);

            var binding = bindingComponent with 
            { 
                OwnerEntityId = bindingEntity.Id,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            var createdBinding = await bindingHandler.CreateBinding(binding);

            await LogAuditAction(
                bindingComponent.CreatedByEntityId,
                "create",
                $"Created component binding for '{bindingComponent.ComponentType}'",
                bindingEntity.Id,
                "component_binding",
                new Dictionary<string, object>
                {
                    { "page_slug", bindingComponent.PageSlug },
                    { "component_type", bindingComponent.ComponentType }
                },
                correlationId: correlationId);

            return new List<IComponent> { createdBinding };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create component binding from component");
            
            await LogAuditAction(
                bindingComponent.CreatedByEntityId,
                "create",
                $"Failed to create component binding for '{bindingComponent.ComponentType}'",
                isSuccess: false,
                errorMessage: ex.Message,
                correlationId: correlationId);

            throw;
        }
    }

    /// <summary>
    /// Updates a component binding from a UIStudioComponentBinding component following strict Jarvis patterns.
    /// </summary>
    /// <param name="bindingComponent">Updated component binding</param>
    /// <returns>List containing the updated binding component</returns>
    public async Task<List<IComponent>> UpdateBindingFromComponent(UIStudioComponentBinding bindingComponent)
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var bindingHandler = _dataContext.For<UIStudioComponentBindingHandler>(bindingComponent.OwnerEntityId);
            var updatedBinding = await bindingHandler.UpdateBinding(bindingComponent with { LastUpdated = DateTime.UtcNow });

            await LogAuditAction(
                bindingComponent.CreatedByEntityId,
                "update",
                $"Updated component binding for '{bindingComponent.ComponentType}'",
                bindingComponent.OwnerEntityId,
                "component_binding",
                new Dictionary<string, object>
                {
                    { "page_slug", bindingComponent.PageSlug },
                    { "component_type", bindingComponent.ComponentType }
                },
                correlationId: correlationId);

            return new List<IComponent> { updatedBinding };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update component binding from component {BindingId}", bindingComponent.OwnerEntityId);
            
            await LogAuditAction(
                bindingComponent.CreatedByEntityId,
                "update",
                $"Failed to update component binding for '{bindingComponent.ComponentType}'",
                bindingComponent.OwnerEntityId,
                "component_binding",
                isSuccess: false,
                errorMessage: ex.Message,
                correlationId: correlationId);

            throw;
        }
    }
}