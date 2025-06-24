using System.Runtime.CompilerServices;

namespace core.jarvis.data.tests.Helpers;

/// <summary>
/// Ensures proper cleanup when tests complete.
/// </summary>
public static class TestRunCleanup
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Set environment to ensure background services don't start
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        
        // Register cleanup handlers
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
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
            // Force flush any pending logs
            Thread.Sleep(100);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}