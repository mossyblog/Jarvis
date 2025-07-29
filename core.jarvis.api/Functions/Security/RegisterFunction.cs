using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.Data;
using core.jarvis.Exceptions;

namespace core.jarvis.api.Functions.Security;

/// <summary>
/// Function for user registration endpoints following API->Handler pattern.
/// </summary>
public class RegisterFunction
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<RegisterFunction> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RegisterFunction(
        IDataContext dataContext,
        ILogger<RegisterFunction> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Registers a new user - API takes IComponent only as per Jarvis pattern.
    /// Thin API layer delegates to AccountHandler.
    /// </summary>
    [Function("RegisterUser")]
    public async Task<HttpResponseData> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequestData req)
    {
        try
        {
            // Parse JSON request to Account component
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

            // Deserialize to Account IComponent - API only accepts IComponent or GUID
            Account accountComponent;
            try
            {
                accountComponent = JsonSerializer.Deserialize<Account>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new ValidationException(new Dictionary<string, string[]> { { "body", new[] { "Invalid request body" } } });
            }
            catch (JsonException ex)
            {
                var badRequestError = new Error
                {
                    Code = "INVALID_JSON",
                    Message = $"Invalid JSON: {ex.Message}",
                    StatusCode = 400
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(badRequestError, _jsonOptions));
                return errorResponse;
            }

            // Enrich with client metadata
            accountComponent = accountComponent with
            {
                IpAddress = req.Headers.TryGetValues("X-Forwarded-For", out var forwardedFor) 
                    ? forwardedFor.FirstOrDefault() 
                    : req.Headers.TryGetValues("X-Real-IP", out var realIp) 
                        ? realIp.FirstOrDefault() 
                        : "unknown",
                UserAgent = req.Headers.TryGetValues("User-Agent", out var userAgent) 
                    ? userAgent.FirstOrDefault() 
                    : "unknown"
            };

            // Create new entity for the user
            var entityId = Guid.NewGuid();
            
            // Use AccountHandler to register the account (thin API layer)
            var accountHandler = _dataContext.For<AccountHandler>(entityId);
            var registeredAccount = await accountHandler.Register(accountComponent);

            // Return the registered Account component
            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(registeredAccount, _jsonOptions));
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