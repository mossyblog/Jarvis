using System;
using System.Collections.Generic;
using System.Net;
using core.jarvis.api.Models;

namespace core.jarvis.api.Services;

/// <summary>
/// Service for creating standardized error responses that don't leak sensitive information.
/// </summary>
public static class ErrorResponseService
{
    // Standard error messages that don't reveal internal details
    private static readonly Dictionary<string, (HttpStatusCode StatusCode, string Message)> StandardErrors = new()
    {
        ["AUTH_FAILED"] = (HttpStatusCode.Unauthorized, "Authentication failed. Please check your credentials."),
        ["AUTH_REQUIRED"] = (HttpStatusCode.Unauthorized, "Authentication required."),
        ["TOKEN_INVALID"] = (HttpStatusCode.Unauthorized, "Invalid or expired token."),
        ["TOKEN_EXPIRED"] = (HttpStatusCode.Unauthorized, "Token has expired."),
        ["ACCESS_DENIED"] = (HttpStatusCode.Forbidden, "Access denied."),
        ["NOT_FOUND"] = (HttpStatusCode.NotFound, "Resource not found."),
        ["BAD_REQUEST"] = (HttpStatusCode.BadRequest, "Invalid request."),
        ["VALIDATION_FAILED"] = (HttpStatusCode.BadRequest, "Request validation failed."),
        ["RATE_LIMIT_EXCEEDED"] = (HttpStatusCode.TooManyRequests, "Too many requests. Please try again later."),
        ["SERVER_ERROR"] = (HttpStatusCode.InternalServerError, "An error occurred. Please try again later."),
        ["SERVICE_UNAVAILABLE"] = (HttpStatusCode.ServiceUnavailable, "Service temporarily unavailable.")
    };

    /// <summary>
    /// Creates a standardized error response that doesn't leak sensitive information.
    /// </summary>
    public static Error CreateError(string errorCode, Guid? ownerEntityId = null)
    {
        if (!StandardErrors.TryGetValue(errorCode, out var errorInfo))
        {
            // Default to generic server error if code not found
            errorInfo = StandardErrors["SERVER_ERROR"];
            errorCode = "SERVER_ERROR";
        }

        return new Error
        {
            OwnerEntityId = ownerEntityId ?? Guid.NewGuid(),
            Code = errorCode,
            Message = errorInfo.Message,
            StatusCode = (int)errorInfo.StatusCode,
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates error response for authentication failures.
    /// Always returns the same message regardless of whether user exists or password is wrong.
    /// </summary>
    public static Error CreateAuthenticationError()
    {
        return CreateError("AUTH_FAILED");
    }

    /// <summary>
    /// Creates error response for validation failures without exposing details.
    /// </summary>
    public static Error CreateValidationError()
    {
        return CreateError("VALIDATION_FAILED");
    }

    /// <summary>
    /// Creates error response for rate limit exceeded.
    /// </summary>
    public static Error CreateRateLimitError()
    {
        return CreateError("RATE_LIMIT_EXCEEDED");
    }

    /// <summary>
    /// Creates error response for server errors without exposing stack traces or internal details.
    /// </summary>
    public static Error CreateServerError()
    {
        return CreateError("SERVER_ERROR");
    }

    /// <summary>
    /// Sanitizes exception messages to remove sensitive information.
    /// </summary>
    public static string SanitizeExceptionMessage(Exception ex)
    {
        // Never expose internal exception details in production
        // Log the real exception details separately
        return "An error occurred. Please try again later.";
    }

    /// <summary>
    /// Gets the appropriate HTTP status code for an error code.
    /// </summary>
    public static HttpStatusCode GetStatusCode(string errorCode)
    {
        return StandardErrors.TryGetValue(errorCode, out var errorInfo) 
            ? errorInfo.StatusCode 
            : HttpStatusCode.InternalServerError;
    }
}