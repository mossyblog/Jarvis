using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using core.jarvis.api.Exceptions;
using core.jarvis.api.Services;

namespace core.jarvis.api.Extensions;

/// <summary>
/// Extension methods for HttpRequestData
/// </summary>
public static class HttpRequestExtensions
{
    /// <summary>
    /// Gets the client IP address from request headers
    /// </summary>
    public static string? GetClientIpAddress(this HttpRequestData req)
    {
        if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }
        
        if (req.Headers.TryGetValues("X-Real-IP", out var realIp))
        {
            return realIp.FirstOrDefault();
        }
        
        if (req.Headers.TryGetValues("REMOTE_ADDR", out var remoteAddr))
        {
            return remoteAddr.FirstOrDefault();
        }
        
        return null;
    }

    /// <summary>
    /// Gets the user agent from request headers
    /// </summary>
    public static string? GetUserAgent(this HttpRequestData req)
    {
        if (req.Headers.TryGetValues("User-Agent", out var userAgentValues))
        {
            return userAgentValues.FirstOrDefault();
        }
        
        return null;
    }

    /// <summary>
    /// Checks if the content type contains XML (for XXE attack prevention)
    /// </summary>
    public static bool HasXmlContentType(this HttpRequestData req)
    {
        if (req.Headers.TryGetValues("Content-Type", out var contentTypes))
        {
            var contentType = contentTypes.FirstOrDefault()?.ToLower();
            return contentType != null && (contentType.Contains("xml") || contentType.Contains("text/xml"));
        }
        
        return false;
    }

    /// <summary>
    /// Creates an error response with JSON body
    /// </summary>
    public static async Task<HttpResponseData> CreateErrorResponse(
        this HttpRequestData req, 
        HttpStatusCode statusCode, 
        string message)
    {
        var error = new { error = message };
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(error));
        return response;
    }

    /// <summary>
    /// Creates a validation error response
    /// </summary>
    public static async Task<HttpResponseData> CreateValidationErrorResponse(
        this HttpRequestData req,
        ValidationException ex)
    {
        var error = new 
        { 
            error = "Validation failed",
            details = ex.Errors
        };
        
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(error));
        return response;
    }

    /// <summary>
    /// Creates an unauthorized error response
    /// </summary>
    public static async Task<HttpResponseData> CreateUnauthorizedResponse(
        this HttpRequestData req,
        string message = "Unauthorized")
    {
        var error = ErrorResponseService.CreateAuthenticationError();
        var response = req.CreateResponse(HttpStatusCode.Unauthorized);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(error));
        return response;
    }
}