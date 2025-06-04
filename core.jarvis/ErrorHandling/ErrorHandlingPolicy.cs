using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Context;

namespace core.jarvis.ErrorHandling;

/// <summary>
/// Defines the consistent error handling policy for the Jarvis framework.
/// All error handling must follow these patterns to ensure consistency.
/// </summary>
public static class ErrorHandlingPolicy
{
    /// <summary>
    /// Logs an error and rethrows the exception.
    /// Use this for critical errors that should propagate up the call stack.
    /// </summary>
    public static void LogAndRethrow(
        Exception exception,
        string message,
        object? context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        using (LogContext.PushProperty("ErrorContext", context))
        using (LogContext.PushProperty("Method", memberName))
        using (LogContext.PushProperty("SourceFile", sourceFilePath))
        using (LogContext.PushProperty("Line", sourceLineNumber))
        {
            Log.Error(exception, message);
        }
        
        throw exception;
    }

    /// <summary>
    /// Logs an error and continues execution.
    /// Use this ONLY for non-critical errors where the operation can continue.
    /// </summary>
    public static void LogAndContinue(
        Exception exception,
        string message,
        object? context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        using (LogContext.PushProperty("ErrorContext", context))
        using (LogContext.PushProperty("Method", memberName))
        using (LogContext.PushProperty("SourceFile", sourceFilePath))
        using (LogContext.PushProperty("Line", sourceLineNumber))
        using (LogContext.PushProperty("ErrorHandling", "Swallowed"))
        {
            Log.Warning(exception, message + " (Error was handled and execution continued)");
        }
    }

    /// <summary>
    /// Logs an error and returns a default value.
    /// Use this when you need to return a safe default instead of throwing.
    /// </summary>
    public static T LogAndReturnDefault<T>(
        Exception exception,
        string message,
        T defaultValue,
        object? context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        using (LogContext.PushProperty("ErrorContext", context))
        using (LogContext.PushProperty("Method", memberName))
        using (LogContext.PushProperty("SourceFile", sourceFilePath))
        using (LogContext.PushProperty("Line", sourceLineNumber))
        using (LogContext.PushProperty("ErrorHandling", "ReturnedDefault"))
        using (LogContext.PushProperty("DefaultValue", defaultValue))
        {
            Log.Warning(exception, message + " (Returning default value)");
        }
        
        return defaultValue;
    }

    /// <summary>
    /// Wraps an exception with additional context before rethrowing.
    /// Use this to add domain-specific context to lower-level exceptions.
    /// </summary>
    public static void WrapAndRethrow<TException>(
        Exception innerException,
        string message,
        Func<string, Exception, TException> exceptionFactory,
        object? context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        where TException : Exception
    {
        using (LogContext.PushProperty("ErrorContext", context))
        using (LogContext.PushProperty("Method", memberName))
        using (LogContext.PushProperty("SourceFile", sourceFilePath))
        using (LogContext.PushProperty("Line", sourceLineNumber))
        using (LogContext.PushProperty("WrappedException", typeof(TException).Name))
        {
            Log.Error(innerException, message + " (Wrapping exception)");
        }
        
        throw exceptionFactory(message, innerException);
    }

    /// <summary>
    /// Logs a warning for an expected error condition.
    /// Use this for business rule violations or expected validation failures.
    /// </summary>
    public static void LogExpectedError(
        string message,
        object? context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        using (LogContext.PushProperty("ErrorContext", context))
        using (LogContext.PushProperty("Method", memberName))
        using (LogContext.PushProperty("SourceFile", sourceFilePath))
        using (LogContext.PushProperty("Line", sourceLineNumber))
        using (LogContext.PushProperty("ErrorType", "Expected"))
        {
            Log.Warning(message);
        }
    }
}