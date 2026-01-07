using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using core.jarvis.Exceptions;
using core.jarvis.api.Services;
using core.jarvis.api.Security;

namespace core.jarvis.api.Extensions;

/// <summary>
/// Extension methods for HttpRequestData
/// </summary>
public static class HttpRequestExtensions
{
    /// <summary>
    /// Gets the client IP address from request headers with trusted proxy validation.
    ///
    /// SECURITY: X-Forwarded-For headers are only trusted when the request comes from
    /// a configured trusted proxy IP. This prevents IP spoofing attacks where malicious
    /// clients send fake X-Forwarded-For headers.
    ///
    /// When using this overload without options, X-Forwarded-For is NOT trusted and
    /// only the socket/Azure client IP is used (secure default).
    /// </summary>
    /// <param name="req">The HTTP request</param>
    /// <param name="options">Optional trusted proxy configuration. If null, forwarded headers are not trusted.</param>
    /// <returns>The client IP address, or "unknown" if it cannot be determined</returns>
    public static string GetClientIpAddress(this HttpRequestData req, TrustedProxyOptions? options = null)
    {
        // Get the socket-level IP address (not spoofable)
        // In Azure Functions, X-Azure-ClientIP contains the actual client IP
        string? socketIp = null;
        if (req.Headers.TryGetValues("X-Azure-ClientIP", out var azureIp))
        {
            socketIp = azureIp.FirstOrDefault()?.Trim();
        }

        // Only trust X-Forwarded-For if:
        // 1. Trusted proxy validation is enabled
        // 2. We have configured trusted proxy IPs
        // 3. The socket IP is from a trusted proxy
        if (options?.EnableTrustedProxyValidation == true &&
            options.TrustedProxyIps?.Any() == true &&
            socketIp != null &&
            options.TrustedProxyIps.Contains(socketIp))
        {
            // Request is from a trusted proxy, use X-Forwarded-For
            if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedFor))
            {
                var forwardedValue = forwardedFor.FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedValue))
                {
                    // Use the LAST (rightmost) IP in the chain - this is the client IP
                    // added by the trusted proxy. Earlier IPs in the chain can be spoofed.
                    var ips = forwardedValue.Split(',');
                    var clientIp = ips.LastOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(clientIp))
                    {
                        return clientIp;
                    }
                }
            }
        }

        // Default: use socket address (not spoofable)
        // This is the secure default when no trusted proxies are configured
        if (!string.IsNullOrEmpty(socketIp))
        {
            return socketIp;
        }

        // Fallback for non-Azure environments (local development)
        if (req.Headers.TryGetValues("REMOTE_ADDR", out var remoteAddr))
        {
            var addr = remoteAddr.FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(addr))
            {
                return addr;
            }
        }

        return "unknown";
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

    /// <summary>
    /// Creates a business rule error response
    /// </summary>
    public static async Task<HttpResponseData> CreateBusinessRuleErrorResponse(
        this HttpRequestData req,
        BusinessRuleException ex)
    {
        var error = new 
        { 
            error = ex.Message,
            code = ex.Code
        };
        
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(error));
        return response;
    }

    /// <summary>
    /// Creates an entity not found error response
    /// </summary>
    public static async Task<HttpResponseData> CreateNotFoundResponse(
        this HttpRequestData req,
        EntityNotFoundException ex)
    {
        var error = new 
        { 
            error = ex.Message,
            entityId = ex.EntityId,
            entityType = ex.EntityType
        };
        
        var response = req.CreateResponse(HttpStatusCode.NotFound);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(error));
        return response;
    }

    /// <summary>
    /// Creates an error response for UnauthorizedException from core.jarvis.Exceptions
    /// </summary>
    public static async Task<HttpResponseData> CreateUnauthorizedExceptionResponse(
        this HttpRequestData req,
        core.jarvis.Exceptions.UnauthorizedException ex)
    {
        var error = new 
        { 
            error = ex.Message
        };
        
        var response = req.CreateResponse(HttpStatusCode.Unauthorized);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(error));
        return response;
    }

    /// <summary>
    /// Creates a validation error response for core.jarvis ValidationException
    /// </summary>
    public static async Task<HttpResponseData> CreateCoreValidationErrorResponse(
        this HttpRequestData req,
        core.jarvis.Exceptions.ValidationException ex)
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
    /// Creates a generic internal server error response
    /// </summary>
    public static async Task<HttpResponseData> CreateInternalErrorResponse(
        this HttpRequestData req,
        Exception ex)
    {
        // Log the actual exception but don't expose details to client
        var error = ErrorResponseService.CreateServerError();
        var response = req.CreateResponse(HttpStatusCode.InternalServerError);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(error));
        return response;
    }
}