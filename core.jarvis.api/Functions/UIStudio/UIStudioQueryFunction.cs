using System.Net;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.api.Extensions;
using core.jarvis.api.Exceptions;
using core.jarvis.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace core.jarvis.api.Functions.UIStudio;

/// <summary>
/// Azure Functions for UIStudio data queries with filtering, pagination, and search capabilities.
/// Provides comprehensive read access to UIStudio resources.
/// </summary>
public class UIStudioQueryFunction
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<UIStudioQueryFunction> _logger;

    public UIStudioQueryFunction(IDataContext dataContext, ILogger<UIStudioQueryFunction> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    #region Page Queries

    /// <summary>
    /// GET /api/uistudio/pages/by-owner/{ownerEntityId}
    /// Gets pages for a specific owner.
    /// </summary>
    [Function("GetPagesByOwner")]
    [OpenApiOperation(operationId: "getPagesByOwner", tags: new[] { "UIStudio", "Query" }, Summary = "Get pages by owner", Description = "Retrieves UIStudio pages for a specific owner entity.")]
    [OpenApiParameter(name: "ownerEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the owner")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Pages retrieved successfully")]
    public async Task<HttpResponseData> GetPagesByOwner(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "uistudio/pages/by-owner/{ownerEntityId}")] HttpRequestData req,
        string ownerEntityId)
    {
        try
        {
            if (!Guid.TryParse(ownerEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid ownerEntityId format");
            }

            // Get pages for owner
            var pageHandler = _dataContext.For<UIStudioPageHandler>(Guid.NewGuid());
            var pages = await pageHandler.GetByOwner(entityId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(pages.Cast<IComponent>().ToList()));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pages for owner {OwnerEntityId}", ownerEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get pages");
        }
    }

    /// <summary>
    /// GET /api/uistudio/pages/{pageEntityId}
    /// Gets a specific page component.
    /// </summary>
    [Function("GetPage")]
    [OpenApiOperation(operationId: "getPage", tags: new[] { "UIStudio", "Query" }, Summary = "Get page", Description = "Retrieves a specific UIStudio page component.")]
    [OpenApiParameter(name: "pageEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the page")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Page retrieved successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(object), Summary = "Page not found")]
    public async Task<HttpResponseData> GetPage(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "uistudio/pages/{pageEntityId}")] HttpRequestData req,
        string pageEntityId)
    {
        try
        {
            if (!Guid.TryParse(pageEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid pageEntityId format");
            }

            // Get page
            var pageHandler = _dataContext.For<UIStudioPageHandler>(entityId);
            var page = await pageHandler.Get();
            if (page == null)
            {
                return await req.CreateErrorResponse(HttpStatusCode.NotFound, "Page not found");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(new List<IComponent> { page }));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting page {PageEntityId}", pageEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get page");
        }
    }

    /// <summary>
    /// GET /api/uistudio/pages/published
    /// Gets published pages for public access.
    /// </summary>
    [Function("GetPublishedPages")]
    [OpenApiOperation(operationId: "getPublishedPages", tags: new[] { "UIStudio", "Query" }, Summary = "Get published pages", Description = "Retrieves published UIStudio pages accessible to users.")]
    [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Maximum number of pages to return")]
    [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Number of pages to skip")]
    [OpenApiParameter(name: "pageType", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Filter by page type")]
    [OpenApiParameter(name: "search", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Search in page name")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Published pages retrieved successfully")]
    public async Task<HttpResponseData> GetPublishedPages(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "uistudio/pages/published")] HttpRequestData req)
    {
        try
        {
            // Parse query parameters
            var queryParams = req.Query;
            var limitStr = queryParams["limit"];
            var limit = !string.IsNullOrEmpty(limitStr) && int.TryParse(limitStr, out var l) ? l : 50;
            var pageType = queryParams["pageType"];
            var search = queryParams["search"];

            // Get published pages using basic query
            var query = _dataContext.Query()
                .WithAll<UIStudioPage>(p => p.IsPublished);

            if (!string.IsNullOrEmpty(pageType))
            {
                query = query.WithAll<UIStudioPage>(p => p.PageType == pageType);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.WithAll<UIStudioPage>(p => p.PageName.Contains(search) || (p.Description != null && p.Description.Contains(search)));
            }

            var results = await query.ToEntityComponents();
            var pages = new List<UIStudioPage>();

            foreach (var result in results.Take(limit))
            {
                var handler = _dataContext.For<UIStudioPageHandler>(result.Key);
                var page = await handler.Get();
                if (page != null)
                {
                    pages.Add(page);
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(pages.Cast<IComponent>().ToList()));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting published pages");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get published pages");
        }
    }

    #endregion

    #region Template Queries

    /// <summary>
    /// GET /api/uistudio/templates/by-owner/{ownerEntityId}
    /// Gets templates for a specific owner.
    /// </summary>
    [Function("GetTemplatesByOwner")]
    [OpenApiOperation(operationId: "getTemplatesByOwner", tags: new[] { "UIStudio", "Query" }, Summary = "Get templates by owner", Description = "Retrieves UIStudio templates for a specific owner entity.")]
    [OpenApiParameter(name: "ownerEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the owner")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Templates retrieved successfully")]
    public async Task<HttpResponseData> GetTemplatesByOwner(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "uistudio/templates/by-owner/{ownerEntityId}")] HttpRequestData req,
        string ownerEntityId)
    {
        try
        {
            if (!Guid.TryParse(ownerEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid ownerEntityId format");
            }

            // Get templates for owner
            var templateHandler = _dataContext.For<UIStudioTemplateHandler>(Guid.NewGuid());
            var templates = await templateHandler.GetByOwner(entityId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(templates.Cast<IComponent>().ToList()));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting templates for owner {OwnerEntityId}", ownerEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get templates");
        }
    }

    /// <summary>
    /// GET /api/uistudio/templates/{templateEntityId}
    /// Gets a specific template.
    /// </summary>
    [Function("GetTemplate")]
    [OpenApiOperation(operationId: "getTemplate", tags: new[] { "UIStudio", "Query" }, Summary = "Get template", Description = "Retrieves a specific UIStudio template.")]
    [OpenApiParameter(name: "templateEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the template")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Template retrieved successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(object), Summary = "Template not found")]
    public async Task<HttpResponseData> GetTemplate(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "uistudio/templates/{templateEntityId}")] HttpRequestData req,
        string templateEntityId)
    {
        try
        {
            if (!Guid.TryParse(templateEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid templateEntityId format");
            }

            // Get template
            var templateHandler = _dataContext.For<UIStudioTemplateHandler>(entityId);
            var template = await templateHandler.Get();
            if (template == null)
            {
                return await req.CreateErrorResponse(HttpStatusCode.NotFound, "Template not found");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(new List<IComponent> { template }));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template {TemplateEntityId}", templateEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get template");
        }
    }

    #endregion

    #region Component Binding Queries

    /// <summary>
    /// GET /api/uistudio/pages/{pageEntityId}/bindings
    /// Gets component bindings for a page.
    /// </summary>
    [Function("GetPageBindings")]
    [OpenApiOperation(operationId: "getPageBindings", tags: new[] { "UIStudio", "Query" }, Summary = "Get page bindings", Description = "Retrieves component bindings for a specific page.")]
    [OpenApiParameter(name: "pageEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the page")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Bindings retrieved successfully")]
    public async Task<HttpResponseData> GetPageBindings(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "uistudio/pages/{pageEntityId}/bindings")] HttpRequestData req,
        string pageEntityId)
    {
        try
        {
            if (!Guid.TryParse(pageEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid pageEntityId format");
            }

            // Get page to find its slug
            var pageHandler = _dataContext.For<UIStudioPageHandler>(entityId);
            var page = await pageHandler.Get();
            if (page == null)
            {
                return await req.CreateErrorResponse(HttpStatusCode.NotFound, "Page not found");
            }
            
            // Get bindings by page slug
            var bindingHandler = _dataContext.For<UIStudioComponentBindingHandler>(Guid.NewGuid());
            var bindings = await bindingHandler.GetByPageSlug(page.PageSlug);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(bindings.Cast<IComponent>().ToList()));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting page bindings for {PageEntityId}", pageEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get page bindings");
        }
    }

    #endregion

    #region Permission Queries

    /// <summary>
    /// GET /api/uistudio/resources/{resourceEntityId}/permissions
    /// Gets permissions for a resource.
    /// </summary>
    [Function("GetResourcePermissions")]
    [OpenApiOperation(operationId: "getResourcePermissions", tags: new[] { "UIStudio", "Query" }, Summary = "Get resource permissions", Description = "Retrieves permissions for a UIStudio resource.")]
    [OpenApiParameter(name: "resourceEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the resource")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Permissions retrieved successfully")]
    public async Task<HttpResponseData> GetResourcePermissions(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "uistudio/resources/{resourceEntityId}/permissions")] HttpRequestData req,
        string resourceEntityId)
    {
        try
        {
            if (!Guid.TryParse(resourceEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid resourceEntityId format");
            }

            // Get permissions
            var permissionHandler = _dataContext.For<UIStudioPermissionHandler>(Guid.NewGuid());
            // Get the resource type from query parameters (default to "page")
            var resourceType = req.Query["resourceType"] ?? "page";
            var permissions = await permissionHandler.GetByResource(entityId, resourceType);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(permissions.Cast<IComponent>().ToList()));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resource permissions for {ResourceEntityId}", resourceEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get resource permissions");
        }
    }

    #endregion

    #region Component Registry Queries

    /// <summary>
    /// GET /api/uistudio/components/registry
    /// Gets the component registry for dynamic component loading.
    /// </summary>
    [Function("GetComponentRegistry")]
    [OpenApiOperation(operationId: "getComponentRegistry", tags: new[] { "UIStudio", "Query" }, Summary = "Get component registry", Description = "Retrieves the UIStudio component registry with available components and their metadata.")]
    [OpenApiParameter(name: "category", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Filter components by category")]
    [OpenApiParameter(name: "device", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Filter components by supported device")]
    [OpenApiParameter(name: "search", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Search components by name or tags")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Summary = "Component registry retrieved successfully")]
    public async Task<HttpResponseData> GetComponentRegistry(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "uistudio/components/registry")] HttpRequestData req)
    {
        try
        {
            // Parse query parameters
            var queryParams = req.Query;
            var category = queryParams["category"];
            var device = queryParams["device"] ?? "desktop";
            var search = queryParams["search"];

            // For now, return a structured component registry based on UIStudio bindings
            // In production, this would dynamically generate based on available components
            var componentRegistry = await GetComponentRegistryData(category, device, search);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(new { 
                success = true, 
                components = componentRegistry,
                metadata = new {
                    total = componentRegistry.Count,
                    device = device,
                    category = category ?? "all",
                    search = search
                }
            }));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting component registry");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get component registry");
        }
    }

    /// <summary>
    /// GET /api/uistudio/components/search
    /// Search components in the registry with advanced filtering.
    /// </summary>
    [Function("SearchComponents")]
    [OpenApiOperation(operationId: "searchComponents", tags: new[] { "UIStudio", "Query" }, Summary = "Search components", Description = "Advanced search for components in the UIStudio registry.")]
    [OpenApiParameter(name: "q", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "Search query")]
    [OpenApiParameter(name: "category", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Filter by category")]
    [OpenApiParameter(name: "tags", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Comma-separated tags to filter by")]
    [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Maximum results to return")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Summary = "Component search results")]
    public async Task<HttpResponseData> SearchComponents(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "uistudio/components/search")] HttpRequestData req)
    {
        try
        {
            var queryParams = req.Query;
            var searchQuery = queryParams["q"];
            var category = queryParams["category"];
            var tagsStr = queryParams["tags"];
            var limitStr = queryParams["limit"];
            var limit = !string.IsNullOrEmpty(limitStr) && int.TryParse(limitStr, out var l) ? l : 50;

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Search query 'q' is required");
            }

            var tags = string.IsNullOrEmpty(tagsStr) ? new string[0] : tagsStr.Split(',').Select(t => t.Trim()).ToArray();
            var searchResults = await SearchComponentsInRegistry(searchQuery, category, tags, limit);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(new { 
                success = true, 
                results = searchResults,
                query = searchQuery,
                filters = new {
                    category = category,
                    tags = tags
                },
                total = searchResults.Count
            }));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching components");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to search components");
        }
    }

    /// <summary>
    /// GET /api/uistudio/components/{componentType}
    /// Get detailed information about a specific component type.
    /// </summary>
    [Function("GetComponentMetadata")]
    [OpenApiOperation(operationId: "getComponentMetadata", tags: new[] { "UIStudio", "Query" }, Summary = "Get component metadata", Description = "Retrieves detailed metadata for a specific component type.")]
    [OpenApiParameter(name: "componentType", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Component type identifier")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Summary = "Component metadata retrieved successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(object), Summary = "Component not found")]
    public async Task<HttpResponseData> GetComponentMetadata(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "uistudio/components/{componentType}")] HttpRequestData req,
        string componentType)
    {
        try
        {
            var componentMetadata = await GetComponentTypeMetadata(componentType);
            
            if (componentMetadata == null)
            {
                return await req.CreateErrorResponse(HttpStatusCode.NotFound, $"Component type '{componentType}' not found");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(new { 
                success = true, 
                component = componentMetadata 
            }));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting component metadata for {ComponentType}", componentType);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get component metadata");
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Gets the component registry data with filtering.
    /// </summary>
    private async Task<List<object>> GetComponentRegistryData(string? category, string device, string? search)
    {
        // This would normally query the actual component bindings and generate the registry
        // For now, we'll return a comprehensive list of available components
        var components = new List<object>
        {
            new {
                id = "metric-card",
                name = "Metric Card",
                description = "Display key performance indicators and metrics",
                category = "Analytics",
                icon = "TrendingUp",
                tags = new[] { "metric", "kpi", "number", "statistics", "dashboard" },
                isPremium = false,
                isNew = false,
                isFromRegistry = true,
                usageCount = 156,
                lastUsed = DateTime.UtcNow.AddDays(-2),
                minSize = new { w = 2, h = 2 },
                maxSize = new { w = 4, h = 3 },
                supportedDevices = new[] { "desktop", "tablet", "mobile" },
                requiredDataSources = new[] { "metrics" },
                configurationSchema = new {
                    properties = new {
                        title = new { type = "string", required = true },
                        value = new { type = "number", required = true },
                        trend = new { type = "string", @enum = new[] { "up", "down", "neutral" } },
                        format = new { type = "string", @default = "number" }
                    }
                }
            },
            new {
                id = "line-chart",
                name = "Line Chart",
                description = "Time series data visualization with trends",
                category = "Analytics",
                icon = "BarChart3",
                tags = new[] { "chart", "graph", "time", "trend", "analytics" },
                isPremium = false,
                isNew = false,
                isFromRegistry = true,
                usageCount = 89,
                lastUsed = DateTime.UtcNow.AddDays(-1),
                minSize = new { w = 4, h = 3 },
                maxSize = new { w = 12, h = 8 },
                supportedDevices = new[] { "desktop", "tablet" },
                requiredDataSources = new[] { "timeseries" },
                configurationSchema = new {
                    properties = new {
                        title = new { type = "string" },
                        dataSource = new { type = "string", required = true },
                        xAxis = new { type = "string", required = true },
                        yAxis = new { type = "string", required = true },
                        showLegend = new { type = "boolean", @default = true }
                    }
                }
            },
            new {
                id = "data-table",
                name = "Data Table",
                description = "Tabular data with sorting, filtering, and pagination",
                category = "Data",
                icon = "Table",
                tags = new[] { "table", "data", "grid", "list", "pagination" },
                isPremium = false,
                isNew = false,
                isFromRegistry = true,
                usageCount = 203,
                lastUsed = DateTime.UtcNow.AddHours(-3),
                minSize = new { w = 6, h = 4 },
                maxSize = new { w = 12, h = 12 },
                supportedDevices = new[] { "desktop", "tablet" },
                requiredDataSources = new[] { "table" },
                configurationSchema = new {
                    properties = new {
                        dataSource = new { type = "string", required = true },
                        columns = new { type = "array", required = true },
                        sortable = new { type = "boolean", @default = true },
                        filterable = new { type = "boolean", @default = true },
                        pageSize = new { type = "number", @default = 10 }
                    }
                }
            },
            new {
                id = "user-list",
                name = "User List",
                description = "Display users with avatars and contact information",
                category = "Data",
                icon = "User",
                tags = new[] { "users", "people", "contacts", "directory", "team" },
                isPremium = false,
                isNew = false,
                isFromRegistry = true,
                usageCount = 67,
                lastUsed = DateTime.UtcNow.AddDays(-5),
                minSize = new { w = 3, h = 4 },
                maxSize = new { w = 8, h = 12 },
                supportedDevices = new[] { "desktop", "tablet", "mobile" },
                requiredDataSources = new[] { "users" },
                configurationSchema = new {
                    properties = new {
                        dataSource = new { type = "string", required = true },
                        showAvatars = new { type = "boolean", @default = true },
                        showStatus = new { type = "boolean", @default = false },
                        layout = new { type = "string", @enum = new[] { "list", "grid" }, @default = "list" }
                    }
                }
            },
            new {
                id = "system-health",
                name = "System Health",
                description = "Overall system status and health indicators",
                category = "Status",
                icon = "Activity",
                tags = new[] { "health", "status", "system", "monitoring", "uptime" },
                isPremium = false,
                isNew = false,
                isFromRegistry = true,
                usageCount = 124,
                lastUsed = DateTime.UtcNow.AddMinutes(-30),
                minSize = new { w = 2, h = 2 },
                maxSize = new { w = 4, h = 3 },
                supportedDevices = new[] { "desktop", "tablet", "mobile" },
                requiredDataSources = new[] { "health" },
                configurationSchema = new {
                    properties = new {
                        title = new { type = "string", @default = "System Health" },
                        threshold = new { type = "number", @default = 95 },
                        showDetails = new { type = "boolean", @default = true }
                    }
                }
            },
            new {
                id = "alert-panel",
                name = "Alert Panel",
                description = "Critical alerts and notification center",
                category = "Status",
                icon = "Bell",
                tags = new[] { "alerts", "notifications", "warnings", "errors", "critical" },
                isPremium = false,
                isNew = true,
                isFromRegistry = true,
                usageCount = 45,
                lastUsed = DateTime.UtcNow.AddHours(-1),
                minSize = new { w = 3, h = 2 },
                maxSize = new { w = 6, h = 4 },
                supportedDevices = new[] { "desktop", "tablet", "mobile" },
                requiredDataSources = new[] { "alerts" },
                configurationSchema = new {
                    properties = new {
                        title = new { type = "string", @default = "Alerts" },
                        severity = new { type = "array", items = new { type = "string", @enum = new[] { "info", "warning", "error", "critical" } } },
                        autoRefresh = new { type = "boolean", @default = true },
                        refreshInterval = new { type = "number", @default = 30 }
                    }
                }
            },
            new {
                id = "contact-form",
                name = "Contact Form",
                description = "Contact form with validation and submission",
                category = "Forms",
                icon = "Mail",
                tags = new[] { "form", "contact", "email", "validation", "submission" },
                isPremium = false,
                isNew = false,
                isFromRegistry = true,
                usageCount = 78,
                lastUsed = DateTime.UtcNow.AddDays(-3),
                minSize = new { w = 4, h = 6 },
                maxSize = new { w = 8, h = 12 },
                supportedDevices = new[] { "desktop", "tablet", "mobile" },
                requiredDataSources = new string[0],
                configurationSchema = new {
                    properties = new {
                        title = new { type = "string", @default = "Contact Us" },
                        fields = new { type = "array", required = true },
                        submitUrl = new { type = "string", required = true },
                        successMessage = new { type = "string", @default = "Thank you for your message!" }
                    }
                }
            },
            new {
                id = "text-block",
                name = "Text Block",
                description = "Rich text content with formatting options",
                category = "Layout",
                icon = "Type",
                tags = new[] { "text", "content", "paragraph", "typography", "rich" },
                isPremium = false,
                isNew = false,
                isFromRegistry = true,
                usageCount = 312,
                lastUsed = DateTime.UtcNow.AddMinutes(-15),
                minSize = new { w = 2, h = 2 },
                maxSize = new { w = 12, h = 8 },
                supportedDevices = new[] { "desktop", "tablet", "mobile" },
                requiredDataSources = new string[0],
                configurationSchema = new {
                    properties = new {
                        content = new { type = "string", required = true },
                        format = new { type = "string", @enum = new[] { "plain", "markdown", "html" }, @default = "markdown" },
                        alignment = new { type = "string", @enum = new[] { "left", "center", "right" }, @default = "left" }
                    }
                }
            },
            new {
                id = "image-gallery",
                name = "Image Gallery",
                description = "Photo gallery with lightbox and thumbnails",
                category = "Media",
                icon = "Image",
                tags = new[] { "images", "gallery", "photos", "media", "lightbox" },
                isPremium = false,
                isNew = false,
                isFromRegistry = true,
                usageCount = 91,
                lastUsed = DateTime.UtcNow.AddDays(-1),
                minSize = new { w = 4, h = 4 },
                maxSize = new { w = 8, h = 8 },
                supportedDevices = new[] { "desktop", "tablet", "mobile" },
                requiredDataSources = new[] { "images" },
                configurationSchema = new {
                    properties = new {
                        dataSource = new { type = "string", required = true },
                        layout = new { type = "string", @enum = new[] { "grid", "masonry", "carousel" }, @default = "grid" },
                        showThumbnails = new { type = "boolean", @default = true },
                        enableLightbox = new { type = "boolean", @default = true }
                    }
                }
            },
            new {
                id = "calendar-widget",
                name = "Calendar Widget",
                description = "Interactive calendar with events and scheduling",
                category = "Custom",
                icon = "Calendar",
                tags = new[] { "calendar", "events", "schedule", "dates", "appointments" },
                isPremium = true,
                isNew = false,
                isFromRegistry = true,
                usageCount = 33,
                lastUsed = DateTime.UtcNow.AddDays(-7),
                minSize = new { w = 4, h = 4 },
                maxSize = new { w = 8, h = 8 },
                supportedDevices = new[] { "desktop", "tablet" },
                requiredDataSources = new[] { "events" },
                configurationSchema = new {
                    properties = new {
                        dataSource = new { type = "string", required = true },
                        view = new { type = "string", @enum = new[] { "month", "week", "day" }, @default = "month" },
                        showWeekends = new { type = "boolean", @default = true },
                        allowEdit = new { type = "boolean", @default = false }
                    }
                }
            },
            new {
                id = "chat-widget",
                name = "Chat Widget",
                description = "Real-time chat interface with message history",
                category = "Custom",
                icon = "MessageSquare",
                tags = new[] { "chat", "messaging", "communication", "realtime", "support" },
                isPremium = true,
                isNew = true,
                isFromRegistry = true,
                usageCount = 12,
                lastUsed = DateTime.UtcNow.AddHours(-6),
                minSize = new { w = 3, h = 4 },
                maxSize = new { w = 6, h = 8 },
                supportedDevices = new[] { "desktop", "tablet", "mobile" },
                requiredDataSources = new[] { "messages" },
                configurationSchema = new {
                    properties = new {
                        title = new { type = "string", @default = "Chat" },
                        dataSource = new { type = "string", required = true },
                        showUsernames = new { type = "boolean", @default = true },
                        enableEmojis = new { type = "boolean", @default = true },
                        maxMessages = new { type = "number", @default = 100 }
                    }
                }
            }
        };

        // Apply filters
        var filteredComponents = components.AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            filteredComponents = filteredComponents.Where(c => 
                ((dynamic)c).category.ToString().Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(device))
        {
            filteredComponents = filteredComponents.Where(c => 
                ((dynamic)c).supportedDevices.Contains(device));
        }

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLowerInvariant();
            filteredComponents = filteredComponents.Where(c => {
                dynamic comp = c;
                return comp.name.ToString().ToLowerInvariant().Contains(searchLower) ||
                       comp.description.ToString().ToLowerInvariant().Contains(searchLower) ||
                       ((string[])comp.tags).Any(tag => tag.ToLowerInvariant().Contains(searchLower));
            });
        }

        return filteredComponents.ToList();
    }

    /// <summary>
    /// Searches components in the registry with advanced filtering.
    /// </summary>
    private async Task<List<object>> SearchComponentsInRegistry(string searchQuery, string? category, string[] tags, int limit)
    {
        var allComponents = await GetComponentRegistryData(category, "desktop", null);
        var searchLower = searchQuery.ToLowerInvariant();

        var results = allComponents.Where(c => {
            dynamic comp = c;
            var matchesSearch = comp.name.ToString().ToLowerInvariant().Contains(searchLower) ||
                               comp.description.ToString().ToLowerInvariant().Contains(searchLower) ||
                               ((string[])comp.tags).Any(tag => tag.ToLowerInvariant().Contains(searchLower));

            var matchesTags = tags.Length == 0 || tags.All(tag => 
                ((string[])comp.tags).Any(ctag => ctag.ToLowerInvariant().Contains(tag.ToLowerInvariant())));

            return matchesSearch && matchesTags;
        }).Take(limit).ToList();

        return results;
    }

    /// <summary>
    /// Gets detailed metadata for a specific component type.
    /// </summary>
    private async Task<object?> GetComponentTypeMetadata(string componentType)
    {
        var allComponents = await GetComponentRegistryData(null, "desktop", null);
        return allComponents.FirstOrDefault(c => ((dynamic)c).id.ToString() == componentType);
    }

    #endregion
}

