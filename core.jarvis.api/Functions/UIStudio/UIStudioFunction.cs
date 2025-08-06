using System.Net;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Systems;
using core.jarvis.api.Extensions;
using core.jarvis.api.Exceptions;
using core.jarvis.api.Handlers;
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
/// Azure Functions for UIStudio operations including pages, layouts, components, and templates.
/// Follows Jarvis API->System->Handler->Component pattern.
/// </summary>
public class UIStudioFunction
{
    private readonly UIStudioSystem _uiStudioSystem;
    private readonly IDataContext _dataContext;
    private readonly ILogger<UIStudioFunction> _logger;

    public UIStudioFunction(UIStudioSystem uiStudioSystem, IDataContext dataContext, ILogger<UIStudioFunction> logger)
    {
        _uiStudioSystem = uiStudioSystem;
        _dataContext = dataContext;
        _logger = logger;
    }

    #region Page Management Operations

    /// <summary>
    /// POST /api/uistudio/pages
    /// Creates a new UIStudio page with layout and optional template.
    /// </summary>
    [Function("CreatePage")]
    [OpenApiOperation(operationId: "createPage", tags: new[] { "UIStudio" }, Summary = "Create new page", Description = "Creates a new UIStudio page with layout configuration and optional template application.")]
    [OpenApiRequestBody("application/json", typeof(UIStudioPage), Required = true, Description = "Page component to create")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Page created successfully", Description = "Returns list of created components (page, layout, bindings)")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Bad request", Description = "Invalid request parameters")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object), Summary = "Unauthorized", Description = "Authentication required")]
    public async Task<HttpResponseData> CreatePage(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uistudio/pages")] HttpRequestData req)
    {
        try
        {
            // Reject XML content type
            if (req.HasXmlContentType())
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "XML content type not allowed");
            }

            // Parse request body
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Request body is required");
            }

            UIStudioPage pageComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                pageComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioPage>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning("Invalid JSON in create page request: {Message}", ex.Message);
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(pageComponent.PageName))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "PageName is required");
            }
            if (string.IsNullOrWhiteSpace(pageComponent.PageSlug))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "PageSlug is required");
            }
            if (pageComponent.CreatedByEntityId == Guid.Empty)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "CreatedByEntityId is required");
            }

            // Delegate to system
            var components = await _uiStudioSystem.CreatePageFromComponent(pageComponent);

            // Return success response
            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(components));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Page creation validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (UnauthorizedException)
        {
            return await req.CreateUnauthorizedResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating page");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create page");
        }
    }

    /// <summary>
    /// PUT /api/uistudio/pages/{pageEntityId}
    /// Updates an existing UIStudio page.
    /// </summary>
    [Function("UpdatePage")]
    [OpenApiOperation(operationId: "updatePage", tags: new[] { "UIStudio" }, Summary = "Update page", Description = "Updates an existing UIStudio page and creates a new version snapshot.")]
    [OpenApiParameter(name: "pageEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the page to update")]
    [OpenApiRequestBody("application/json", typeof(UIStudioPage), Required = true, Description = "Updated page component")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Page updated successfully", Description = "Returns list of updated components")]
    public async Task<HttpResponseData> UpdatePage(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "uistudio/pages/{pageEntityId}")] HttpRequestData req,
        string pageEntityId)
    {
        try
        {
            if (!Guid.TryParse(pageEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid pageEntityId format");
            }

            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Request body is required");
            }

            UIStudioPage updatedPageComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                updatedPageComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioPage>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning("Invalid JSON in update page request: {Message}", ex.Message);
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            // Set OwnerEntityId from route parameter
            updatedPageComponent = updatedPageComponent with { OwnerEntityId = entityId };

            // Delegate to system
            var components = await _uiStudioSystem.UpdatePageFromComponent(updatedPageComponent);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(components));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Page update validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (UnauthorizedException)
        {
            return await req.CreateUnauthorizedResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating page {PageEntityId}", pageEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update page");
        }
    }

    /// <summary>
    /// POST /api/uistudio/pages/{pageEntityId}/publish/{publishedByEntityId}
    /// Publishes a page, making it accessible to users.
    /// </summary>
    [Function("PublishPage")]
    [OpenApiOperation(operationId: "publishPage", tags: new[] { "UIStudio" }, Summary = "Publish page", Description = "Publishes a page making it accessible to users and creates a published version snapshot.")]
    [OpenApiParameter(name: "pageEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the page to publish")]
    [OpenApiParameter(name: "publishedByEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the user publishing the page")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Page published successfully")]
    public async Task<HttpResponseData> PublishPage(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uistudio/pages/{pageEntityId}/publish/{publishedByEntityId}")] HttpRequestData req,
        string pageEntityId,
        string publishedByEntityId)
    {
        try
        {
            if (!Guid.TryParse(pageEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid pageEntityId format");
            }

            if (!Guid.TryParse(publishedByEntityId, out var pubEntityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid publishedByEntityId format");
            }

            // Delegate to system
            var components = await _uiStudioSystem.PublishPage(entityId, pubEntityId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(components));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Page publish validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (UnauthorizedException)
        {
            return await req.CreateUnauthorizedResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing page {PageEntityId}", pageEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to publish page");
        }
    }

    #endregion

    #region Layout Operations

    /// <summary>
    /// PUT /api/uistudio/layouts/{layoutEntityId}/grid
    /// Updates grid configuration for a layout.
    /// </summary>
    [Function("UpdateLayoutGrid")]
    [OpenApiOperation(operationId: "updateLayoutGrid", tags: new[] { "UIStudio" }, Summary = "Update layout grid", Description = "Updates the grid configuration for a layout including responsive breakpoints.")]
    [OpenApiParameter(name: "layoutEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the layout")]
    [OpenApiRequestBody("application/json", typeof(UIStudioLayout), Required = true, Description = "Updated layout configuration")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Layout updated successfully")]
    public async Task<HttpResponseData> UpdateLayoutGrid(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "uistudio/layouts/{layoutEntityId}/grid")] HttpRequestData req,
        string layoutEntityId)
    {
        try
        {
            if (!Guid.TryParse(layoutEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid layoutEntityId format");
            }

            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            UIStudioLayout layoutComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                layoutComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioLayout>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            layoutComponent = layoutComponent with { OwnerEntityId = entityId };
            var components = await _uiStudioSystem.UpdateLayoutFromComponent(layoutComponent);
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(components));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Layout grid update validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating layout grid {LayoutEntityId}", layoutEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update layout grid");
        }
    }

    #endregion

    #region Component Binding Operations

    /// <summary>
    /// POST /api/uistudio/pages/{pageEntityId}/bindings
    /// Creates component bindings for a page.
    /// </summary>
    [Function("CreateComponentBindings")]
    [OpenApiOperation(operationId: "createComponentBindings", tags: new[] { "UIStudio" }, Summary = "Create component bindings", Description = "Creates ECS component field mappings for a page.")]
    [OpenApiParameter(name: "pageEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the page")]
    [OpenApiRequestBody("application/json", typeof(UIStudioComponentBinding), Required = true, Description = "Component binding to create")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Binding created successfully")]
    public async Task<HttpResponseData> CreateComponentBindings(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uistudio/pages/{pageEntityId}/bindings")] HttpRequestData req,
        string pageEntityId)
    {
        try
        {
            if (!Guid.TryParse(pageEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid pageEntityId format");
            }

            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            UIStudioComponentBinding bindingComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                bindingComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioComponentBinding>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            if (bindingComponent.CreatedByEntityId == Guid.Empty)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "CreatedByEntityId is required");
            }

            // Get the page to find its slug
            var pageHandler = _dataContext.For<UIStudioPageHandler>(entityId);
            var page = await pageHandler.Get();
            if (page == null)
            {
                return await req.CreateErrorResponse(HttpStatusCode.NotFound, "Page not found");
            }

            // Set the page slug from the page
            bindingComponent = bindingComponent with { PageSlug = page.PageSlug };
            
            // Create the binding with proper parent-child relationship
            var bindingEntity = _dataContext.NewEntity();
            var bindingHandler = _dataContext.For<UIStudioComponentBindingHandler>(bindingEntity.Id);
            
            var binding = bindingComponent with 
            { 
                OwnerEntityId = bindingEntity.Id,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            
            var createdBinding = await bindingHandler.CreateBinding(binding);
            
            // Link page -> binding (parent -> child)
            await _dataContext.LinkRelationship(
                entityId,                      // parent (page)
                bindingEntity.Id,              // child (binding)
                "UIStudioPage",                // parent type
                "UIStudioComponentBinding"     // child type
            );
            
            var components = new List<IComponent> { createdBinding };

            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(components));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Component binding creation validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating component bindings for page {PageEntityId}", pageEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create component bindings");
        }
    }

    /// <summary>
    /// POST /api/uistudio/bindings/bulk
    /// Creates or updates multiple component bindings in bulk.
    /// </summary>
    [Function("BulkManageComponentBindings")]
    [OpenApiOperation(operationId: "bulkManageComponentBindings", tags: new[] { "UIStudio" }, Summary = "Bulk manage component bindings", Description = "Creates, updates, or deletes multiple component bindings in a single operation.")]
    [OpenApiRequestBody("application/json", typeof(List<UIStudioComponentBinding>), Required = true, Description = "List of component bindings to manage")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Bindings managed successfully")]
    public async Task<HttpResponseData> BulkManageComponentBindings(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uistudio/bindings/bulk")] HttpRequestData req)
    {
        try
        {
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            List<UIStudioComponentBinding> bindings;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                bindings = System.Text.Json.JsonSerializer.Deserialize<List<UIStudioComponentBinding>>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            if (bindings.Count == 0)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "At least one binding is required");
            }

            var allComponents = new List<IComponent>();

            // Process each binding
            foreach (var binding in bindings)
            {
                var components = await _uiStudioSystem.CreateBindingFromComponent(binding);
                allComponents.AddRange(components);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(allComponents));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Bulk binding operation validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk component binding operation");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to perform bulk binding operation");
        }
    }

    #endregion

    #region Permission Management

    /// <summary>
    /// POST /api/uistudio/permissions
    /// Grants permission for a UIStudio resource.
    /// </summary>
    [Function("GrantPermission")]
    [OpenApiOperation(operationId: "grantPermission", tags: new[] { "UIStudio" }, Summary = "Grant permission", Description = "Grants access permission for a UIStudio resource to a user or role.")]
    [OpenApiRequestBody("application/json", typeof(UIStudioPermission), Required = true, Description = "Permission component to grant")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Permission granted successfully")]
    public async Task<HttpResponseData> GrantPermission(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uistudio/permissions")] HttpRequestData req)
    {
        try
        {
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            UIStudioPermission permissionComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                permissionComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioPermission>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            // Validate required fields
            if (permissionComponent.ResourceEntityId == Guid.Empty)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "ResourceEntityId is required");
            }
            if (permissionComponent.GranteeEntityId == Guid.Empty)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "GranteeEntityId is required");
            }
            if (permissionComponent.GrantedByEntityId == Guid.Empty)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "GrantedByEntityId is required");
            }

            // Delegate to system
            var components = await _uiStudioSystem.GrantPermissionFromComponent(permissionComponent);

            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(components));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Permission grant validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting permission");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to grant permission");
        }
    }

    #endregion

    #region Template Operations

    /// <summary>
    /// POST /api/uistudio/templates
    /// Creates a template from an existing page.
    /// </summary>
    [Function("CreateTemplate")]
    [OpenApiOperation(operationId: "createTemplate", tags: new[] { "UIStudio" }, Summary = "Create template", Description = "Creates a reusable template from an existing page.")]
    [OpenApiRequestBody("application/json", typeof(UIStudioTemplate), Required = true, Description = "Template component to create")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Template created successfully")]
    public async Task<HttpResponseData> CreateTemplate(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uistudio/templates")] HttpRequestData req)
    {
        try
        {
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            UIStudioTemplate templateComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                templateComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioTemplate>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(templateComponent.TemplateName))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "TemplateName is required");
            }
            if (templateComponent.CreatedByEntityId == Guid.Empty)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "CreatedByEntityId is required");
            }

            // Delegate to system
            var components = await _uiStudioSystem.CreateTemplateFromComponent(templateComponent);

            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(components));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Template creation validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create template");
        }
    }

    /// <summary>
    /// DELETE /api/uistudio/pages/{pageEntityId}/{deletedByEntityId}
    /// Deletes a page and its related components.
    /// </summary>
    [Function("DeletePage")]
    [OpenApiOperation(operationId: "deletePage", tags: new[] { "UIStudio" }, Summary = "Delete page", Description = "Deletes a UIStudio page and all its related components.")]
    [OpenApiParameter(name: "pageEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the page to delete")]
    [OpenApiParameter(name: "deletedByEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the user deleting the page")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Page deleted successfully")]
    public async Task<HttpResponseData> DeletePage(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "uistudio/pages/{pageEntityId}/{deletedByEntityId}")] HttpRequestData req,
        string pageEntityId,
        string deletedByEntityId)
    {
        try
        {
            if (!Guid.TryParse(pageEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid pageEntityId format");
            }

            if (!Guid.TryParse(deletedByEntityId, out var delEntityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid deletedByEntityId format");
            }

            // Delegate to system
            var deletedComponents = await _uiStudioSystem.DeletePage(entityId, delEntityId, null);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(deletedComponents));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Page deletion validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (UnauthorizedException)
        {
            return await req.CreateUnauthorizedResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting page {PageEntityId}", pageEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete page");
        }
    }

    /// <summary>
    /// POST /api/uistudio/pages/{pageEntityId}/duplicate
    /// Creates a copy of an existing page.
    /// </summary>
    [Function("DuplicatePage")]
    [OpenApiOperation(operationId: "duplicatePage", tags: new[] { "UIStudio" }, Summary = "Duplicate page", Description = "Creates a copy of an existing page with optional modifications.")]
    [OpenApiParameter(name: "pageEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the page to duplicate")]
    [OpenApiRequestBody("application/json", typeof(UIStudioPage), Required = true, Description = "New page component configuration")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Page duplicated successfully")]
    public async Task<HttpResponseData> DuplicatePage(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uistudio/pages/{pageEntityId}/duplicate")] HttpRequestData req,
        string pageEntityId)
    {
        try
        {
            if (!Guid.TryParse(pageEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid pageEntityId format");
            }

            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            UIStudioPage newPageComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                newPageComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioPage>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(newPageComponent.PageName))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "PageName is required");
            }
            if (newPageComponent.CreatedByEntityId == Guid.Empty)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "CreatedByEntityId is required");
            }

            // Delegate to system
            var duplicatedComponents = await _uiStudioSystem.DuplicatePageFromComponent(entityId, newPageComponent);

            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(duplicatedComponents));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Page duplication validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating page {PageEntityId}", pageEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to duplicate page");
        }
    }

    #endregion

    #region Advanced Layout Operations


    /// <summary>
    /// PUT /api/uistudio/layouts/{layoutEntityId}/responsive
    /// Updates responsive breakpoints for a layout.
    /// </summary>
    [Function("UpdateLayoutResponsive")]
    [OpenApiOperation(operationId: "updateLayoutResponsive", tags: new[] { "UIStudio" }, Summary = "Update layout responsive settings", Description = "Updates responsive breakpoints and configurations for a layout.")]
    [OpenApiParameter(name: "layoutEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the layout")]
    [OpenApiRequestBody("application/json", typeof(UIStudioLayout), Required = true, Description = "Updated layout with responsive configuration")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Layout responsive settings updated successfully")]
    public async Task<HttpResponseData> UpdateLayoutResponsive(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "uistudio/layouts/{layoutEntityId}/responsive")] HttpRequestData req,
        string layoutEntityId)
    {
        try
        {
            if (!Guid.TryParse(layoutEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid layoutEntityId format");
            }

            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            UIStudioLayout layoutComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                layoutComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioLayout>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            layoutComponent = layoutComponent with { OwnerEntityId = entityId };
            var components = await _uiStudioSystem.UpdateLayoutFromComponent(layoutComponent);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(components));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Layout responsive update validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating layout responsive settings {LayoutEntityId}", layoutEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update layout responsive settings");
        }
    }

    #endregion

    #region Advanced Component Binding Operations

    /// <summary>
    /// PUT /api/uistudio/bindings/{bindingEntityId}
    /// Updates a specific component binding.
    /// </summary>
    [Function("UpdateComponentBinding")]
    [OpenApiOperation(operationId: "updateComponentBinding", tags: new[] { "UIStudio" }, Summary = "Update component binding", Description = "Updates an existing component binding configuration.")]
    [OpenApiParameter(name: "bindingEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the binding")]
    [OpenApiRequestBody("application/json", typeof(UIStudioComponentBinding), Required = true, Description = "Updated component binding")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Binding updated successfully")]
    public async Task<HttpResponseData> UpdateComponentBinding(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "uistudio/bindings/{bindingEntityId}")] HttpRequestData req,
        string bindingEntityId)
    {
        try
        {
            if (!Guid.TryParse(bindingEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid bindingEntityId format");
            }

            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            UIStudioComponentBinding bindingComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                bindingComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioComponentBinding>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            if (bindingComponent.CreatedByEntityId == Guid.Empty)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "CreatedByEntityId is required");
            }

            bindingComponent = bindingComponent with { OwnerEntityId = entityId };
            var components = await _uiStudioSystem.UpdateBindingFromComponent(bindingComponent);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(components));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Binding update validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating component binding {BindingEntityId}", bindingEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update component binding");
        }
    }

    /// <summary>
    /// DELETE /api/uistudio/bindings/{bindingEntityId}
    /// Deletes a component binding.
    /// </summary>
    [Function("DeleteComponentBinding")]
    [OpenApiOperation(operationId: "deleteComponentBinding", tags: new[] { "UIStudio" }, Summary = "Delete component binding", Description = "Deletes a component binding from a page.")]
    [OpenApiParameter(name: "bindingEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the binding to delete")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Summary = "Binding deleted successfully")]
    public async Task<HttpResponseData> DeleteComponentBinding(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "uistudio/bindings/{bindingEntityId}")] HttpRequestData req,
        string bindingEntityId)
    {
        try
        {
            if (!Guid.TryParse(bindingEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid bindingEntityId format");
            }

            // Note: In Azure Functions, we need to inject dependencies differently
            // For now, we'll delegate to the system pattern instead of direct handler access
            throw new NotImplementedException("This endpoint should use UIStudioSystem pattern instead of direct handler access");

            // var bindingHandler = dataContext.For<UIStudioComponentBindingHandler>(entityId);
            // await bindingHandler.Remove();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(new { success = true, message = "Component binding deleted successfully" }));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting component binding {BindingEntityId}", bindingEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete component binding");
        }
    }

    #endregion

    #region Advanced Permission Management

    /// <summary>
    /// PUT /api/uistudio/permissions/{permissionEntityId}
    /// Updates an existing permission.
    /// </summary>
    [Function("UpdatePermission")]
    [OpenApiOperation(operationId: "updatePermission", tags: new[] { "UIStudio" }, Summary = "Update permission", Description = "Updates an existing permission's level or expiration.")]
    [OpenApiParameter(name: "permissionEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the permission")]
    [OpenApiRequestBody("application/json", typeof(UIStudioPermission), Required = true, Description = "Updated permission component")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Permission updated successfully")]
    public async Task<HttpResponseData> UpdatePermission(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "uistudio/permissions/{permissionEntityId}")] HttpRequestData req,
        string permissionEntityId)
    {
        try
        {
            if (!Guid.TryParse(permissionEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid permissionEntityId format");
            }

            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            UIStudioPermission permissionComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                permissionComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioPermission>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            if (permissionComponent.GrantedByEntityId == Guid.Empty)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "GrantedByEntityId is required");
            }

            // This endpoint is deprecated - use component-based patterns instead
            return await req.CreateErrorResponse(HttpStatusCode.MethodNotAllowed, 
                "This endpoint is deprecated. Use component-based operations instead.");
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Permission update validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission {PermissionEntityId}", permissionEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update permission");
        }
    }

    /// <summary>
    /// DELETE /api/uistudio/permissions/{permissionEntityId}
    /// Revokes a permission.
    /// </summary>
    [Function("RevokePermission")]
    [OpenApiOperation(operationId: "revokePermission", tags: new[] { "UIStudio" }, Summary = "Revoke permission", Description = "Revokes access permission for a UIStudio resource.")]
    [OpenApiParameter(name: "permissionEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the permission to revoke")]
    [OpenApiParameter(name: "revokedByEntityId", In = ParameterLocation.Query, Required = true, Type = typeof(Guid), Description = "Entity ID of the user revoking the permission")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Summary = "Permission revoked successfully")]
    public async Task<HttpResponseData> RevokePermission(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "uistudio/permissions/{permissionEntityId}")] HttpRequestData req,
        string permissionEntityId)
    {
        try
        {
            if (!Guid.TryParse(permissionEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid permissionEntityId format");
            }

            var queryParams = req.Query;
            var revokedByParam = queryParams["revokedByEntityId"];
            if (string.IsNullOrEmpty(revokedByParam) || !Guid.TryParse(revokedByParam, out var revokedByEntityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "RevokedByEntityId query parameter is required");
            }

            var reason = queryParams["reason"] ?? "Permission revoked";

            // Delegate to system
            await _uiStudioSystem.RevokePermission(entityId, revokedByEntityId, reason);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(new { success = true, message = "Permission revoked successfully" }));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Permission revocation validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking permission {PermissionEntityId}", permissionEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to revoke permission");
        }
    }

    #endregion

    #region Advanced Template Operations

    /// <summary>
    /// POST /api/uistudio/templates/{templateEntityId}/apply
    /// Applies a template to create a new page.
    /// </summary>
    [Function("ApplyTemplate")]
    [OpenApiOperation(operationId: "applyTemplate", tags: new[] { "UIStudio" }, Summary = "Apply template", Description = "Applies a template to create a new page with the template's configuration.")]
    [OpenApiParameter(name: "templateEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the template to apply")]
    [OpenApiRequestBody("application/json", typeof(UIStudioPage), Required = true, Description = "Page component to create from template")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Template applied successfully")]
    public async Task<HttpResponseData> ApplyTemplate(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uistudio/templates/{templateEntityId}/apply")] HttpRequestData req,
        string templateEntityId)
    {
        try
        {
            if (!Guid.TryParse(templateEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid templateEntityId format");
            }

            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            UIStudioPage pageComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                pageComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioPage>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(pageComponent.PageName))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "PageName is required");
            }
            if (string.IsNullOrWhiteSpace(pageComponent.PageSlug))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "PageSlug is required");
            }
            if (pageComponent.CreatedByEntityId == Guid.Empty)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "CreatedByEntityId is required");
            }

            // Delegate to system
            var createdComponents = await _uiStudioSystem.ApplyTemplate(
                entityId,
                pageComponent.PageName,
                pageComponent.PageSlug,
                pageComponent.CreatedByEntityId,
                null);

            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(createdComponents));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Template application validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying template {TemplateEntityId}", templateEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to apply template");
        }
    }

    /// <summary>
    /// PUT /api/uistudio/templates/{templateEntityId}
    /// Updates a template configuration.
    /// </summary>
    [Function("UpdateTemplate")]
    [OpenApiOperation(operationId: "updateTemplate", tags: new[] { "UIStudio" }, Summary = "Update template", Description = "Updates a template's configuration and metadata.")]
    [OpenApiParameter(name: "templateEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the template")]
    [OpenApiRequestBody("application/json", typeof(UIStudioTemplate), Required = true, Description = "Updated template component")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Template updated successfully")]
    public async Task<HttpResponseData> UpdateTemplate(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "uistudio/templates/{templateEntityId}")] HttpRequestData req,
        string templateEntityId)
    {
        try
        {
            if (!Guid.TryParse(templateEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid templateEntityId format");
            }

            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            UIStudioTemplate templateComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                templateComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioTemplate>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            if (templateComponent.CreatedByEntityId == Guid.Empty)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "CreatedByEntityId is required");
            }

            // This endpoint is deprecated - use component-based patterns instead
            return await req.CreateErrorResponse(HttpStatusCode.MethodNotAllowed, 
                "This endpoint is deprecated. Use component-based operations instead.");
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Template update validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateEntityId}", templateEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update template");
        }
    }

    #endregion

    #region Simple Jarvis-Compliant Endpoints

    /// <summary>
    /// POST /api/uistudio/layouts
    /// Creates a new layout.
    /// </summary>
    [Function("CreateLayout")]
    [OpenApiOperation(operationId: "createLayout", tags: new[] { "UIStudio" }, Summary = "Create layout", Description = "Creates a new layout configuration.")]
    [OpenApiRequestBody("application/json", typeof(UIStudioLayout), Required = true, Description = "Layout component to create")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Layout created successfully")]
    public async Task<HttpResponseData> CreateLayout(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uistudio/layouts")] HttpRequestData req)
    {
        try
        {
            if (req.HasXmlContentType())
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "XML content type not allowed");
            }

            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Request body is required");
            }

            UIStudioLayout layoutComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                layoutComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioLayout>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            var components = await _uiStudioSystem.CreateLayoutFromComponent(layoutComponent);

            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(components));
            return response;
        }
        catch (ValidationException vex)
        {
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating layout");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create layout");
        }
    }

    /// <summary>
    /// PUT /api/uistudio/layouts/{layoutEntityId}
    /// Updates a layout.
    /// </summary>
    [Function("UpdateLayout")]
    [OpenApiOperation(operationId: "updateLayout", tags: new[] { "UIStudio" }, Summary = "Update layout", Description = "Updates an existing layout configuration.")]
    [OpenApiParameter(name: "layoutEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the layout")]
    [OpenApiRequestBody("application/json", typeof(UIStudioLayout), Required = true, Description = "Updated layout component")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Layout updated successfully")]
    public async Task<HttpResponseData> UpdateLayout(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "uistudio/layouts/{layoutEntityId}")] HttpRequestData req,
        string layoutEntityId)
    {
        try
        {
            if (!Guid.TryParse(layoutEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid layoutEntityId format");
            }

            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            UIStudioLayout layoutComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                layoutComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioLayout>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            layoutComponent = layoutComponent with { OwnerEntityId = entityId };
            var components = await _uiStudioSystem.UpdateLayoutFromComponent(layoutComponent);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(components));
            return response;
        }
        catch (ValidationException vex)
        {
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating layout {LayoutEntityId}", layoutEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update layout");
        }
    }

    /// <summary>
    /// POST /api/uistudio/bindings
    /// Creates a component binding.
    /// </summary>
    [Function("CreateBinding")]
    [OpenApiOperation(operationId: "createBinding", tags: new[] { "UIStudio" }, Summary = "Create component binding", Description = "Creates a new component binding.")]
    [OpenApiRequestBody("application/json", typeof(UIStudioComponentBinding), Required = true, Description = "Component binding to create")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Binding created successfully")]
    public async Task<HttpResponseData> CreateBinding(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uistudio/bindings")] HttpRequestData req)
    {
        try
        {
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            UIStudioComponentBinding bindingComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                bindingComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioComponentBinding>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            var components = await _uiStudioSystem.CreateBindingFromComponent(bindingComponent);

            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(components));
            return response;
        }
        catch (ValidationException vex)
        {
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating component binding");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create component binding");
        }
    }

    /// <summary>
    /// PUT /api/uistudio/bindings/{bindingEntityId}
    /// Updates a component binding.
    /// </summary>
    [Function("UpdateBinding")]
    [OpenApiOperation(operationId: "updateBinding", tags: new[] { "UIStudio" }, Summary = "Update component binding", Description = "Updates an existing component binding.")]
    [OpenApiParameter(name: "bindingEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the binding")]
    [OpenApiRequestBody("application/json", typeof(UIStudioComponentBinding), Required = true, Description = "Updated component binding")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<IComponent>), Summary = "Binding updated successfully")]
    public async Task<HttpResponseData> UpdateBinding(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "uistudio/bindings/{bindingEntityId}")] HttpRequestData req,
        string bindingEntityId)
    {
        try
        {
            if (!Guid.TryParse(bindingEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid bindingEntityId format");
            }

            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            UIStudioComponentBinding bindingComponent;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                bindingComponent = System.Text.Json.JsonSerializer.Deserialize<UIStudioComponentBinding>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            bindingComponent = bindingComponent with { OwnerEntityId = entityId };
            var components = await _uiStudioSystem.UpdateBindingFromComponent(bindingComponent);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(components));
            return response;
        }
        catch (ValidationException vex)
        {
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating component binding {BindingEntityId}", bindingEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update component binding");
        }
    }

    #endregion
}