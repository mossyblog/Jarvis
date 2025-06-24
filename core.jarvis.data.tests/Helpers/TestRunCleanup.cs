using System.Runtime.CompilerServices;

namespace core.jarvis.data.tests.Helpers;

/// <summary>
/// Ensures test environment is properly configured.
/// </summary>
public static class TestRunCleanup
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Set environment to ensure background services don't start
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
    }
}