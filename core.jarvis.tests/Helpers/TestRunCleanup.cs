using System.Runtime.CompilerServices;
using System.Threading;
using core.jarvis.api.Middleware;

namespace core.jarvis.tests.Helpers;

/// <summary>
/// Ensures proper cleanup of background tasks when tests complete.
/// </summary>
public static class TestRunCleanup
{
    private static readonly CancellationTokenSource _testRunCancellation = new();
    
    [ModuleInitializer]
    public static void Initialize()
    {
        // Disable rate limiting cleanup task
        RateLimitingMiddleware.DisableCleanup();
        
        // Register cleanup handlers
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
        
        // Set environment to ensure background services don't start
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
    }
    
    private static void OnProcessExit(object? sender, EventArgs e)
    {
        CleanupResources();
    }
    
    private static void OnDomainUnload(object? sender, EventArgs e)
    {
        CleanupResources();
    }
    
    private static void CleanupResources()
    {
        try
        {
            // Cancel any running operations
            _testRunCancellation.Cancel();
            
            // Stop rate limiting cleanup
            RateLimitingMiddleware.StopCleanup();
            
            // Give tasks a moment to finish
            Thread.Sleep(100);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
    
    /// <summary>
    /// Gets a cancellation token that is canceled when tests are complete.
    /// </summary>
    public static CancellationToken TestRunCancellationToken => _testRunCancellation.Token;
}