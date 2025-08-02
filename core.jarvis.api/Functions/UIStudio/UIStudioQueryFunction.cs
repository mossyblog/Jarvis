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
}

