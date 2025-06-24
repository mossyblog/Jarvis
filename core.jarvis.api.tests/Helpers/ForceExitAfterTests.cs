using System.Runtime.CompilerServices;
using System.Threading;
using core.jarvis.api.Middleware;

namespace core.jarvis.api.tests.Helpers;

/// <summary>
/// Forces process exit after tests complete to prevent hanging.
/// </summary>
public static class ForceExitAfterTests
{
    private static Timer? _exitTimer;
    private static int _testCount = 0;
    private static readonly object _lock = new();
    
    [ModuleInitializer]
    public static void Initialize()
    {
        // Disable background services
        RateLimitingMiddleware.DisableCleanup();
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        
        // Monitor test execution
        AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
        {
            // Track that tests are running
            lock (_lock)
            {
                _testCount++;
                ResetExitTimer();
            }
        };
        
        // Start exit timer
        ResetExitTimer();
    }
    
    private static void ResetExitTimer()
    {
        _exitTimer?.Dispose();
        
        // If no test activity for 5 seconds after tests start, force exit
        _exitTimer = new Timer(_ =>
        {
            if (_testCount > 0)
            {
                // Tests have run and now there's no activity, force exit
                Console.WriteLine("Forcing process exit after test completion");
                Environment.Exit(0);
            }
        }, null, TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
    }
}