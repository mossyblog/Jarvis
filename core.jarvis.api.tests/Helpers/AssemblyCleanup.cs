using core.jarvis.api.Middleware;
using System.Runtime.CompilerServices;

namespace core.jarvis.api.tests.Helpers;

/// <summary>
/// Module initializer that ensures cleanup of background tasks when tests complete.
/// This runs automatically when the assembly is loaded/unloaded.
/// </summary>
public static class AssemblyCleanup
{
    /// <summary>
    /// This method is called automatically by the runtime when the module (assembly) is initialized.
    /// We use it to register cleanup that happens when the assembly is unloaded.
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        // Register cleanup to happen when AppDomain is unloading (test run complete)
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        PerformCleanup();
    }

    private static void OnDomainUnload(object? sender, EventArgs e)
    {
        PerformCleanup();
    }

    private static void PerformCleanup()
    {
        try
        {
            // Stop the rate limiting middleware cleanup task
            RateLimitingMiddleware.StopCleanup();
        }
        catch
        {
            // Ignore any errors during cleanup
        }
    }
}