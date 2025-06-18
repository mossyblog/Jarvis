using System.Net;
using core.jarvis.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace core.jarvis.api.Middleware;

/// <summary>
/// Middleware to validate that request/response bodies contain valid IComponent implementations.
/// </summary>
public class ComponentValidationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ComponentValidationMiddleware> _logger;

    public ComponentValidationMiddleware(ILogger<ComponentValidationMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            // Check if this is an HTTP trigger
            var httpRequestData = await context.GetHttpRequestDataAsync();
            if (httpRequestData != null)
            {
                // Validate request body if it's a POST/PUT
                if (httpRequestData.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                    httpRequestData.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
                {
                    var body = await httpRequestData.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(body) && !IsValidComponentOrGuid(body))
                    {
                        _logger.LogWarning("Invalid request body - not an IComponent or GUID");
                        await WriteErrorResponse(context, HttpStatusCode.BadRequest, 
                            "Request body must be an IComponent implementation or a GUID");
                        return;
                    }
                }
            }

            // Continue to the function
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in component validation middleware");
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError, 
                "An error occurred during request validation");
        }
    }

    private bool IsValidComponentOrGuid(string body)
    {
        // First check if it's a GUID
        if (Guid.TryParse(body.Trim('"'), out _))
        {
            return true;
        }

        try
        {
            // Try to deserialize as JSON object
            var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);
            if (jsonObject == null)
            {
                return false;
            }

            // Check for IComponent required properties
            var hasId = jsonObject.ContainsKey("id") || jsonObject.ContainsKey("Id");
            var hasOwnerEntityId = jsonObject.ContainsKey("ownerEntityId") || jsonObject.ContainsKey("OwnerEntityId");
            var hasUpdatedAt = jsonObject.ContainsKey("updatedAt") || jsonObject.ContainsKey("UpdatedAt");

            return hasId && hasOwnerEntityId && hasUpdatedAt;
        }
        catch
        {
            return false;
        }
    }

    private async Task WriteErrorResponse(FunctionContext context, HttpStatusCode statusCode, string message)
    {
        var httpRequestData = await context.GetHttpRequestDataAsync();
        if (httpRequestData != null)
        {
            var response = httpRequestData.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(new { error = message }));
            
            // Set the response in the context
            context.GetInvocationResult().Value = response;
        }
    }
}