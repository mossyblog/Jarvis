using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using core.jarvis.api.Models;

namespace core.jarvis.api.Middleware;

/// <summary>
/// Enhanced middleware for comprehensive input validation and sanitization.
/// </summary>
public class InputValidationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<InputValidationMiddleware> _logger;
    
    // Configuration
    private const int MaxRequestSizeBytes = 1_048_576; // 1MB
    private const int MaxStringLength = 1000;
    private const int MaxEmailLength = 254; // RFC 5321
    private const int MaxArraySize = 100;
    private const int MaxJsonDepth = 10;
    
    // Validation patterns
    private static readonly Regex EmailPattern = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    private static readonly Regex SqlInjectionPattern = new(
        @"(\b(union|select|insert|update|delete|drop|create|alter|exec|execute|script|javascript|vbscript|onload|onerror|onclick)\b)|(-{2})|(/\*.*?\*/)|;|'[\s]*OR[\s]*'|'[\s]*OR[\s]*\d|'[\s]*AND[\s]*'|\|\||'[\s]*#|'[\s]*--",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    private static readonly Regex XssPattern = new(
        @"<[^>]*(script|iframe|object|embed|form|input|button|textarea|select|link|style|base|meta)[^>]*>|javascript:|vbscript:|on\w+\s*=",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    private static readonly Regex PathTraversalPattern = new(
        @"(\.\.[\\/])|([<>:""|?*])",
        RegexOptions.Compiled);

    public InputValidationMiddleware(ILogger<InputValidationMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequest = await context.GetHttpRequestDataAsync();
        if (httpRequest == null)
        {
            await next(context);
            return;
        }

        // Validate request headers
        if (!ValidateHeaders(httpRequest))
        {
            await CreateValidationErrorResponse(context, "Invalid request headers");
            return;
        }

        // Check request size
        if (httpRequest.Headers.TryGetValues("Content-Length", out var contentLength))
        {
            if (int.TryParse(contentLength.FirstOrDefault(), out var size) && size > MaxRequestSizeBytes)
            {
                _logger.LogWarning("Request size {Size} exceeds maximum allowed {Max}", size, MaxRequestSizeBytes);
                await CreateValidationErrorResponse(context, "Request size exceeds maximum allowed");
                return;
            }
        }

        // Validate request body
        if (httpRequest.Method != "GET" && httpRequest.Method != "DELETE")
        {
            var body = await httpRequest.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(body))
            {
                var validationResult = await ValidateRequestBody(body, httpRequest.Url.AbsolutePath);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Input validation failed: {Errors}", string.Join("; ", validationResult.Errors));
                    await CreateValidationErrorResponse(context, string.Join(". ", validationResult.Errors));
                    return;
                }
            }
        }

        await next(context);
    }

    private bool ValidateHeaders(HttpRequestData request)
    {
        // Check for suspicious headers
        var suspiciousHeaders = new[] { "X-Forwarded-Host", "X-Original-URL", "X-Rewrite-URL" };
        
        foreach (var header in suspiciousHeaders)
        {
            if (request.Headers.Contains(header))
            {
                var value = request.Headers.GetValues(header).FirstOrDefault();
                if (!string.IsNullOrEmpty(value) && PathTraversalPattern.IsMatch(value))
                {
                    _logger.LogWarning("Suspicious header detected: {Header} = {Value}", header, value);
                    return false;
                }
            }
        }

        return true;
    }

    private async Task<ValidationResult> ValidateRequestBody(string body, string endpoint)
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            // Parse JSON and check depth
            var token = JToken.Parse(body);
            if (GetJsonDepth(token) > MaxJsonDepth)
            {
                result.AddError("JSON structure too deep");
                return result;
            }

            // Route-specific validation
            if (endpoint.Contains("/security/auth", StringComparison.OrdinalIgnoreCase))
            {
                ValidateAuthRequest(token, result);
            }
            else if (endpoint.Contains("/security/refresh", StringComparison.OrdinalIgnoreCase))
            {
                ValidateRefreshRequest(token, result);
            }
            else if (endpoint.Contains("/roles", StringComparison.OrdinalIgnoreCase))
            {
                ValidateRoleRequest(token, result);
            }
            else if (endpoint.Contains("/accounts", StringComparison.OrdinalIgnoreCase))
            {
                ValidateAccountRequest(token, result);
            }
            else
            {
                // Generic component validation
                ValidateGenericComponent(token, result);
            }

            // Check for common injection patterns in all string values
            ValidateForInjectionAttacks(token, result);
        }
        catch (JsonException)
        {
            result.AddError("Invalid JSON format");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during input validation");
            result.AddError("Input validation error");
        }

        return result;
    }

    private void ValidateAuthRequest(JToken token, ValidationResult result)
    {
        var email = token["email"]?.ToString();
        var password = token["password"]?.ToString();
        var authMethod = token["authMethod"]?.ToString() ?? "password";

        // Email validation
        if (string.IsNullOrEmpty(email))
        {
            result.AddError("Email is required");
        }
        else if (email.Length > MaxEmailLength)
        {
            result.AddError($"Email must not exceed {MaxEmailLength} characters");
        }
        else if (!EmailPattern.IsMatch(email))
        {
            result.AddError("Invalid email format");
        }

        // Password validation (basic checks - full policy in handler)
        if (authMethod == "password")
        {
            if (string.IsNullOrEmpty(password))
            {
                result.AddError("Password is required");
            }
            else if (password.Length > MaxStringLength)
            {
                result.AddError($"Password must not exceed {MaxStringLength} characters");
            }
        }

        // Validate auth method
        var allowedAuthMethods = new[] { "password", "otp", "pin", "magic_link", "oauth", "saml" };
        if (!allowedAuthMethods.Contains(authMethod.ToLowerInvariant()))
        {
            result.AddError("Invalid authentication method");
        }
    }

    private void ValidateRefreshRequest(JToken token, ValidationResult result)
    {
        var refreshToken = token["refreshToken"]?.ToString();
        
        if (string.IsNullOrEmpty(refreshToken))
        {
            result.AddError("Refresh token is required");
        }
        else if (refreshToken.Length > 500) // JWT tokens can be long
        {
            result.AddError("Invalid refresh token format");
        }
    }

    private void ValidateRoleRequest(JToken token, ValidationResult result)
    {
        var name = token["name"]?.ToString();
        var permissions = token["permissionIds"]?.ToObject<string[]>();

        if (string.IsNullOrEmpty(name))
        {
            result.AddError("Role name is required");
        }
        else if (name.Length > 100)
        {
            result.AddError("Role name must not exceed 100 characters");
        }
        else if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_\-\s]+$"))
        {
            result.AddError("Role name contains invalid characters");
        }

        if (permissions != null && permissions.Length > MaxArraySize)
        {
            result.AddError($"Cannot assign more than {MaxArraySize} permissions");
        }
    }

    private void ValidateAccountRequest(JToken token, ValidationResult result)
    {
        var email = token["email"]?.ToString();
        
        if (!string.IsNullOrEmpty(email))
        {
            if (email.Length > MaxEmailLength)
            {
                result.AddError($"Email must not exceed {MaxEmailLength} characters");
            }
            else if (!EmailPattern.IsMatch(email))
            {
                result.AddError("Invalid email format");
            }
        }
    }

    private void ValidateGenericComponent(JToken token, ValidationResult result)
    {
        // Validate required IComponent fields
        var id = token["id"]?.ToString();
        var ownerEntityId = token["ownerEntityId"]?.ToString();

        if (!string.IsNullOrEmpty(id) && !Guid.TryParse(id, out _))
        {
            result.AddError("Invalid component ID format");
        }

        if (!string.IsNullOrEmpty(ownerEntityId) && !Guid.TryParse(ownerEntityId, out _))
        {
            result.AddError("Invalid owner entity ID format");
        }
    }

    private void ValidateForInjectionAttacks(JToken token, ValidationResult result)
    {
        var stringValues = GetAllStringValues(token);
        
        foreach (var value in stringValues)
        {
            if (value.Length > MaxStringLength)
            {
                result.AddError($"String value exceeds maximum length of {MaxStringLength} characters");
                continue;
            }

            if (SqlInjectionPattern.IsMatch(value))
            {
                result.AddError("Potential SQL injection detected");
                _logger.LogWarning("SQL injection attempt detected: {Value}", value.Substring(0, Math.Min(50, value.Length)));
            }

            if (XssPattern.IsMatch(value))
            {
                result.AddError("Potential XSS attack detected");
                _logger.LogWarning("XSS attempt detected: {Value}", value.Substring(0, Math.Min(50, value.Length)));
            }

            if (PathTraversalPattern.IsMatch(value))
            {
                result.AddError("Invalid characters detected");
            }
        }
    }

    private IEnumerable<string> GetAllStringValues(JToken token)
    {
        var values = new List<string>();
        
        switch (token.Type)
        {
            case JTokenType.Object:
                foreach (var child in token.Children<JProperty>())
                {
                    values.AddRange(GetAllStringValues(child.Value));
                }
                break;
            case JTokenType.Array:
                foreach (var child in token.Children())
                {
                    values.AddRange(GetAllStringValues(child));
                }
                break;
            case JTokenType.String:
                var value = token.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    values.Add(value);
                }
                break;
        }
        
        return values;
    }

    private int GetJsonDepth(JToken token)
    {
        return token.Type switch
        {
            JTokenType.Object => 1 + (token.Children<JProperty>().Select(c => GetJsonDepth(c.Value)).DefaultIfEmpty(0).Max()),
            JTokenType.Array => 1 + (token.Children().Select(GetJsonDepth).DefaultIfEmpty(0).Max()),
            _ => 0
        };
    }

    private async Task CreateValidationErrorResponse(FunctionContext context, string message)
    {
        var error = new Error
        {
            OwnerEntityId = Guid.NewGuid(),
            Code = "VALIDATION_FAILED",
            Message = message,
            StatusCode = 400
        };

        var request = await context.GetHttpRequestDataAsync();
        if (request != null)
        {
            var response = request.CreateResponse();
            response.StatusCode = HttpStatusCode.BadRequest;
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(error));
            
            // Set the invocation result to prevent further processing
            context.GetInvocationResult().Value = response;
        }
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; } = new();

        public void AddError(string error)
        {
            IsValid = false;
            if (!Errors.Contains(error))
            {
                Errors.Add(error);
            }
        }
    }
}