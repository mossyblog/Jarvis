using System.Net;
using core.jarvis.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace core.jarvis.api.Middleware;

/*
 * ================================================================================
 *  ComponentValidationMiddleware
 * ================================================================================
 *  Purpose:
 *      This middleware is designed for Azure Functions projects that utilize domain
 *      "component" models (objects implementing IComponent). Its primary role is
 *      to validate incoming HTTP request bodies for POST and PUT operations before
 *      passing them to function handlers.
 *
 *      The middleware ensures that only requests containing either:
 *         1) A valid GUID (representing a component identifier), or
 *         2) A JSON object matching the structure of IComponent
 *            (must have "id", "ownerEntityId", and "updatedAt" properties)
 *      are processed. This helps enforce data integrity and provides fast feedback
 *      to clients on badly-formed input, reducing error-prone downstream processing.
 *
 *  Where and How Used:
 *      - Plugged into the Azure Functions Worker pipeline.
 *      - Invoked for every HTTP-triggered function request (when enabled).
 *      - Mostly relevant for API endpoints dealing with domain data creation or modification.
 *
 *  Key Behaviors:
 *      - Intercepts each request.
 *      - For POST/PUT, reads the body and checks:
 *          * If it's a stringified GUID → OK.
 *          * If it's a JSON object, checks for minimally required component properties.
 *          * If neither, returns a 400 Bad Request and halts further processing.
 *      - For non-HTTP triggers or other methods, or for valid requests, execution passes to the next middleware/function.
 *      - Any errors during validation result in a 500 Internal Server Error.
 *
 *  Benefits:
 *      - Prevents malformed or incomplete "component" data from reaching business logic.
 *      - Improves security and reliability by failing fast on bad input.
 *      - Centralizes validation logic, ensuring consistent enforcement across endpoints.
 *
 *  Customization Notes:
 *      - To adjust what constitutes a valid component, modify IsValidComponentOrGuid.
 *      - To extend validation to other HTTP verbs, update the method checks.
 *      - This middleware can be stacked/composed alongside other validation/auth logic.
 *
 *  Example Error Responses:
 *      - 400 Bad Request: "Request body must be an IComponent implementation or a GUID"
 *      - 500 Internal Server Error: "An error occurred during request validation"
 *
 * ================================================================================
 */
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