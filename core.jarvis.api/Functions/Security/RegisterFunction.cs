using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.Systems;

namespace core.jarvis.api.Functions.Security;

/// <summary>
/// Function for user registration endpoints.
/// </summary>
public class RegisterFunction
{
    private readonly ISystem _system;
    private readonly ILogger<RegisterFunction> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RegisterFunction(
        ISystem system,
        ILogger<RegisterFunction> logger)
    {
        _system = system;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Registers a new user with email and password.
    /// Creates both Account and SecurityProfile components.
    /// </summary>
    [Function("RegisterUser")]
    public async Task<HttpResponseData> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequestData req)
    {
        try
        {
            // Get request body
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                var badRequestError = new Error
                {
                    Code = "INVALID_REQUEST",
                    Message = "Request body is required",
                    StatusCode = 400
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(badRequestError, _jsonOptions));
                return errorResponse;
            }

            // Get client IP address
            string? ipAddress = null;
            if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedFor))
            {
                ipAddress = forwardedFor.FirstOrDefault();
            }
            else if (req.Headers.TryGetValues("X-Real-IP", out var realIp))
            {
                ipAddress = realIp.FirstOrDefault();
            }
            ipAddress ??= "unknown";

            // Execute registration through System - ALL logic in handler
            var result = await _system.ExecuteHandlerWithResult<RegistrationHandler, RegistrationResult>(
                Guid.Empty, // No owner entity for registration
                handler => handler.RegisterFromJson(requestBody, ipAddress));

            // Handle registration result
            if (!result.Success)
            {
                var error = new Error
                {
                    Code = "REGISTRATION_FAILED",
                    Message = result.Message,
                    StatusCode = 400,
                    Details = result.Errors
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(error, _jsonOptions));
                return errorResponse;
            }

            // Success response
            var successResponse = new
            {
                success = true,
                message = result.Message,
                accountId = result.AccountId,
                email = result.Email
            };

            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(successResponse, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            var serverError = new Error
            {
                Code = "INTERNAL_ERROR",
                Message = "Registration failed",
                StatusCode = 500
            };
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(serverError, _jsonOptions));
            return errorResponse;
        }
    }
}