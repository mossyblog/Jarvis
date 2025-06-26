using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Systems;
using core.jarvis.Data;
using core.jarvis.Exceptions;

namespace core.jarvis.api.Functions.Security;

/// <summary>
/// Function for user registration endpoints.
/// </summary>
public class RegisterFunction
{
    private readonly RegistrationSystem _registrationSystem;
    private readonly ILogger<RegisterFunction> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RegisterFunction(
        RegistrationSystem registrationSystem,
        ILogger<RegisterFunction> logger)
    {
        _registrationSystem = registrationSystem;
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

            // Execute registration through System - returns [Account, SecurityProfile]
            var components = await _registrationSystem.RegisterUser(requestBody, ipAddress);

            // The first component is Account, second is SecurityProfile
            var account = components[0] as Account;
            var profile = components[1] as SecurityProfile;
            
            if (account == null || profile == null)
            {
                throw new InvalidOperationException("Registration did not return expected components");
            }

            // Return the created components
            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(components, _jsonOptions));
            return response;
        }
        catch (ValidationException vex)
        {
            var error = new Error
            {
                Code = "VALIDATION_ERROR",
                Message = "Registration validation failed",
                StatusCode = 400,
                Details = vex.Errors
            };
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(error, _jsonOptions));
            return errorResponse;
        }
        catch (BusinessRuleException brex)
        {
            var error = new Error
            {
                Code = brex.Code,
                Message = brex.Message,
                StatusCode = 400
            };
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(error, _jsonOptions));
            return errorResponse;
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