using System.Net;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.api.Extensions;
using core.jarvis.api.Exceptions;
using core.jarvis.Data;
using core.jarvis;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace core.jarvis.api.Functions.UIStudio;

/// <summary>
/// Azure Functions for UIStudio version control operations including snapshots, publishing, and rollbacks.
/// Handles version management following Jarvis patterns.
/// </summary>
public class UIStudioVersionFunction
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<UIStudioVersionFunction> _logger;

    public UIStudioVersionFunction(IDataContext dataContext, ILogger<UIStudioVersionFunction> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/uistudio/versions/snapshots
    /// Creates a manual version snapshot of a page or template.
    /// </summary>
    [Function("CreateVersionSnapshot")]
    [OpenApiOperation(operationId: "createVersionSnapshot", tags: new[] { "UIStudio", "Versions" }, Summary = "Create version snapshot", Description = "Creates a manual version snapshot for rollback and history tracking.")]
    [OpenApiRequestBody("application/json", typeof(UIStudioVersion), Required = true, Description = "Version snapshot component to create")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Snapshot created successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Bad request")]
    public async Task<HttpResponseData> CreateVersionSnapshot(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uistudio/versions/snapshots")] HttpRequestData req)
    {
        try
        {
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Request body is required");
            }

            UIStudioVersion versionComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                versionComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioVersion>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning("Invalid JSON in create snapshot request: {Message}", ex.Message);
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            // Validate required fields
            if (versionComponent.ResourceEntityId == Guid.Empty)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "ResourceEntityId is required");
            }
            if (versionComponent.CreatedByEntityId == Guid.Empty)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "CreatedByEntityId is required");
            }
            if (string.IsNullOrWhiteSpace(versionComponent.VersionLabel))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "VersionLabel is required");
            }

            // Create version entity and handler
            var versionEntity = _dataContext.NewEntity();
            var versionHandler = _dataContext.For<UIStudioVersionHandler>(versionEntity.Id);

            // Get current state data if not provided
            var snapshotData = versionComponent.SnapshotData ?? await GetResourceSnapshot(versionComponent.ResourceEntityId, versionComponent.ResourceType);

            // Create version from component
            var version = versionComponent with 
            {
                OwnerEntityId = versionEntity.Id,
                SnapshotData = snapshotData,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            
            var createdVersion = await versionHandler.CreateVersion(version);

            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(new List<IComponent> { createdVersion }));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Version snapshot creation validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating version snapshot");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create version snapshot");
        }
    }

    /// <summary>
    /// POST /api/uistudio/versions/{versionId}/rollback/{rolledBackById}
    /// Rolls back a resource to a specific version.
    /// </summary>
    [Function("RollbackToVersion")]
    [OpenApiOperation(operationId: "rollbackToVersion", tags: new[] { "UIStudio", "Versions" }, Summary = "Rollback to version", Description = "Restores a page or template to a previous version state.")]
    [OpenApiParameter(name: "versionId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the version to rollback to")]
    [OpenApiParameter(name: "rolledBackById", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the user performing rollback")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Rollback completed successfully")]
    public async Task<HttpResponseData> RollbackToVersion(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uistudio/versions/{versionId}/rollback/{rolledBackById}")] HttpRequestData req,
        string versionId,
        string rolledBackById)
    {
        try
        {
            if (!Guid.TryParse(versionId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid versionId format");
            }

            if (!Guid.TryParse(rolledBackById, out var rollbackId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid rolledBackById format");
            }

            // Get the version to rollback to
            var versionHandler = _dataContext.For<UIStudioVersionHandler>(entityId);
            var version = await versionHandler.Get() ?? throw new InvalidOperationException("Version not found");

            // Perform rollback based on resource type
            var restoredComponents = await PerformRollback(version, rollbackId);

            // Create new version snapshot after rollback
            var newVersionEntity = _dataContext.NewEntity();
            var newVersionHandler = _dataContext.For<UIStudioVersionHandler>(newVersionEntity.Id);
            await newVersionHandler.CreateAutoVersion(
                version.ResourceEntityId,
                version.ResourceType,
                version.SnapshotData ?? new Dictionary<string, object>(),
                rollbackId,
                $"Rolled back to version {version.VersionLabel}");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(restoredComponents));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Version rollback validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back to version {VersionId}", versionId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to rollback to version");
        }
    }

    /// <summary>
    /// GET /api/uistudio/resources/{resourceId}/versions
    /// Gets version history for a resource.
    /// </summary>
    [Function("GetVersionHistory")]
    [OpenApiOperation(operationId: "getVersionHistory", tags: new[] { "UIStudio", "Versions" }, Summary = "Get version history", Description = "Retrieves version history for a page or template.")]
    [OpenApiParameter(name: "resourceId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the resource")]
    [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Maximum number of versions to return")]
    [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Number of versions to skip")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Version history retrieved successfully")]
    public async Task<HttpResponseData> GetVersionHistory(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "uistudio/resources/{resourceId}/versions")] HttpRequestData req,
        string resourceId)
    {
        try
        {
            if (!Guid.TryParse(resourceId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid resourceId format");
            }

            // Parse query parameters
            var queryParams = req.Query;
            var limit = queryParams["limit"] != null && int.TryParse(queryParams["limit"], out var l) ? l : 50;
            var offset = queryParams["offset"] != null && int.TryParse(queryParams["offset"], out var o) ? o : 0;

            // Get version history
            var versionHandler = _dataContext.For<UIStudioVersionHandler>(Guid.NewGuid());
            var versions = await versionHandler.GetVersionHistory(entityId, limit, offset);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(versions.Cast<IComponent>().ToList()));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version history for resource {ResourceEntityId}", resourceId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get version history");
        }
    }

    /// <summary>
    /// POST /api/uistudio/versions/{versionId}/publish/{publishedById}
    /// Publishes a specific version to production.
    /// </summary>
    [Function("PublishVersion")]
    [OpenApiOperation(operationId: "publishVersion", tags: new[] { "UIStudio", "Versions" }, Summary = "Publish version", Description = "Publishes a specific version to production environment.")]
    [OpenApiParameter(name: "versionId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the version to publish")]
    [OpenApiParameter(name: "publishedById", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the user publishing the version")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Version published successfully")]
    public async Task<HttpResponseData> PublishVersion(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uistudio/versions/{versionId}/publish/{publishedById}")] HttpRequestData req,
        string versionId,
        string publishedById)
    {
        try
        {
            if (!Guid.TryParse(versionId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid versionId format");
            }

            if (!Guid.TryParse(publishedById, out var pubId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid publishedById format");
            }

            // Get the version
            var versionHandler = _dataContext.For<UIStudioVersionHandler>(entityId);
            var version = await versionHandler.Get() ?? throw new InvalidOperationException("Version not found");

            // Create published version
            var publishedVersionEntity = _dataContext.NewEntity();
            var publishedVersionHandler = _dataContext.For<UIStudioVersionHandler>(publishedVersionEntity.Id);
            
            var publishedVersion = await publishedVersionHandler.CreatePublishedVersion(
                version.ResourceEntityId,
                version.ResourceType,
                version.SnapshotData ?? new Dictionary<string, object>(),
                pubId,
                $"Published {version.VersionLabel}",
                $"Published version {version.VersionLabel} to production");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(new List<IComponent> { publishedVersion }));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Version publish validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing version {VersionId}", versionId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to publish version");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Gets a complete snapshot of a resource and its related components.
    /// </summary>
    private async Task<Dictionary<string, object>> GetResourceSnapshot(Guid resourceId, string? resourceType)
    {
        var snapshot = new Dictionary<string, object>
        {
            { "timestamp", DateTime.UtcNow },
            { "resource_type", resourceType ?? "page" }
        };

        switch (resourceType?.ToLower())
        {
            case "page":
                await AddPageSnapshotData(snapshot, resourceId);
                break;
            case "template":
                await AddTemplateSnapshotData(snapshot, resourceId);
                break;
            case "layout":
                await AddLayoutSnapshotData(snapshot, resourceId);
                break;
            default:
                throw new ValidationException($"Unsupported resource type: {resourceType}");
        }

        return snapshot;
    }

    /// <summary>
    /// Adds page-specific data to snapshot.
    /// </summary>
    private async Task AddPageSnapshotData(Dictionary<string, object> snapshot, Guid pageId)
    {
        var pageHandler = _dataContext.For<UIStudioPageHandler>(pageId);
        var page = await pageHandler.Get() ?? throw new InvalidOperationException("Page not found");
        snapshot["page"] = page;

        // Get child entities (layouts and bindings)
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
    }

    /// <summary>
    /// Adds template-specific data to snapshot.
    /// </summary>
    private async Task AddTemplateSnapshotData(Dictionary<string, object> snapshot, Guid templateId)
    {
        var templateHandler = _dataContext.For<UIStudioTemplateHandler>(templateId);
        var template = await templateHandler.Get() ?? throw new InvalidOperationException("Template not found");
        snapshot["template"] = template;
    }

    /// <summary>
    /// Adds layout-specific data to snapshot.
    /// </summary>
    private async Task AddLayoutSnapshotData(Dictionary<string, object> snapshot, Guid layoutId)
    {
        var layoutHandler = _dataContext.For<UIStudioLayoutHandler>(layoutId);
        var layout = await layoutHandler.Get() ?? throw new InvalidOperationException("Layout not found");
        snapshot["layout"] = layout;
    }

    /// <summary>
    /// Performs rollback operation based on version data.
    /// </summary>
    private async Task<List<object>> PerformRollback(UIStudioVersion version, Guid rolledBackById)
    {
        var restoredComponents = new List<object>();

        switch (version.ResourceType.ToLower())
        {
            case "page":
                restoredComponents.AddRange(await RollbackPage(version, rolledBackById));
                break;
            case "template":
                restoredComponents.AddRange(await RollbackTemplate(version, rolledBackById));
                break;
            case "layout":
                restoredComponents.AddRange(await RollbackLayout(version, rolledBackById));
                break;
            default:
                throw new ValidationException($"Unsupported resource type for rollback: {version.ResourceType}");
        }

        return restoredComponents;
    }

    /// <summary>
    /// Rolls back a page to version state.
    /// </summary>
    private async Task<List<object>> RollbackPage(UIStudioVersion version, Guid rolledBackById)
    {
        var components = new List<object>();

        if (version.SnapshotData?.ContainsKey("page") == true)
        {
            // Restore page data
            var pageData = version.SnapshotData["page"];
            var pageHandler = _dataContext.For<UIStudioPageHandler>(version.ResourceEntityId);
            
            // Convert and restore page (would need proper deserialization logic)
            var restoredPage = await pageHandler.RestoreFromSnapshot(pageData, rolledBackById);
            components.Add(restoredPage);

            // Restore layout if present
            if (version.SnapshotData.ContainsKey("layout"))
            {
                var layoutData = version.SnapshotData["layout"];
                // Restore layout (implementation would depend on layout structure)
            }

            // Restore component bindings if present
            if (version.SnapshotData.ContainsKey("component_bindings"))
            {
                var bindingsData = version.SnapshotData["component_bindings"];
                // Restore bindings (implementation would depend on binding structure)
            }
        }

        return components;
    }

    /// <summary>
    /// Rolls back a template to version state.
    /// </summary>
    private async Task<List<object>> RollbackTemplate(UIStudioVersion version, Guid rolledBackById)
    {
        var components = new List<object>();

        if (version.SnapshotData?.ContainsKey("template") == true)
        {
            var templateData = version.SnapshotData["template"];
            var templateHandler = _dataContext.For<UIStudioTemplateHandler>(version.ResourceEntityId);
            
            var restoredTemplate = await templateHandler.RestoreFromSnapshot(templateData, rolledBackById);
            components.Add(restoredTemplate);
        }

        return components;
    }

    /// <summary>
    /// Rolls back a layout to version state.
    /// </summary>
    private async Task<List<object>> RollbackLayout(UIStudioVersion version, Guid rolledBackById)
    {
        var components = new List<object>();

        if (version.SnapshotData?.ContainsKey("layout") == true)
        {
            var layoutData = version.SnapshotData["layout"];
            var layoutHandler = _dataContext.For<UIStudioLayoutHandler>(version.ResourceEntityId);
            
            var restoredLayout = await layoutHandler.RestoreFromSnapshot(layoutData, rolledBackById);
            components.Add(restoredLayout);
        }

        return components;
    }

    #endregion
}

