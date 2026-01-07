using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Middleware;

/// <summary>
/// Middleware to add security headers to all HTTP responses.
/// </summary>
public class SecurityHeadersMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    
    // CSP policy - adjust based on your needs
    // Note: 'unsafe-inline' kept for style-src as many CSS-in-JS solutions require it
    // 'unsafe-eval' removed from script-src to prevent XSS attacks via eval()
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "form-action 'self'; " +
        "base-uri 'self';";

    public SecurityHeadersMiddleware(ILogger<SecurityHeadersMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequest = await context.GetHttpRequestDataAsync();
        
        await next(context);

        var httpResponse = context.GetHttpResponseData();
        if (httpResponse != null)
        {
            var isSwaggerEndpoint = httpRequest?.Url.AbsolutePath.Contains("/swagger/", StringComparison.OrdinalIgnoreCase) ?? false;
            
            AddSecurityHeaders(httpResponse, isSwaggerEndpoint);
        }
    }

    private void AddSecurityHeaders(HttpResponseData response, bool isSwaggerEndpoint = false)
    {
        // Prevent MIME type sniffing
        response.Headers.Add("X-Content-Type-Options", "nosniff");
        
        // Prevent clickjacking
        response.Headers.Add("X-Frame-Options", "DENY");
        
        // Enable XSS protection (legacy browsers)
        response.Headers.Add("X-XSS-Protection", "1; mode=block");
        
        // Force HTTPS
        response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
        
        // Content Security Policy - skip for Swagger endpoints
        if (!isSwaggerEndpoint)
        {
            response.Headers.Add("Content-Security-Policy", ContentSecurityPolicy);
        }
        
        // Referrer Policy
        response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Permissions Policy (replace Feature-Policy)
        response.Headers.Add("Permissions-Policy", 
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
        
        // Remove server header if possible
        response.Headers.Remove("Server");
        response.Headers.Remove("X-Powered-By");
        response.Headers.Remove("X-AspNet-Version");
        
        // Add custom security header
        response.Headers.Add("X-Security-Policy", "jarvis-api-v1");
        
        // Cache control for sensitive endpoints
        if (IsSensitiveEndpoint(response))
        {
            response.Headers.Add("Cache-Control", "no-store, no-cache, must-revalidate, private");
            response.Headers.Add("Pragma", "no-cache");
            response.Headers.Add("Expires", DateTime.UtcNow.ToString("R"));
        }
    }

    private bool IsSensitiveEndpoint(HttpResponseData response)
    {
        // Consider all API endpoints sensitive by default
        return true;
    }
}