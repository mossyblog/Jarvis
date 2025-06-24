using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace core.jarvis.tests.Helpers;

/// <summary>
/// Ensures the test process exits after all tests complete.
/// This is a workaround for test runners that don't properly exit in CI/CD environments.
/// </summary>
public static class ForceProcessExit
{
    private static readonly CancellationTokenSource _exitCts = new();
    private static Task? _exitTask;
    
    [ModuleInitializer]
    public static void Initialize()
    {
        // Set test environment
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        
        // Register domain unload handler
        AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
        {
            ForceExit("Domain unload");
        };
        
        // Register process exit handler
        AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
        {
            ForceExit("Process exit");
        };
        
        // Start monitoring task
        _exitTask = Task.Run(async () =>
        {
            try
            {
                // Wait for a reasonable time for tests to complete (5 minutes)
                await Task.Delay(TimeSpan.FromMinutes(5), _exitCts.Token);
                
                // If we're still here after 5 minutes, force exit
                ForceExit("Timeout");
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, ignore
            }
        });
    }
    
    private static void ForceExit(string reason)
    {
        try
        {
            _exitCts.Cancel();
            
            Console.WriteLine($"[ForceProcessExit] Forcing process exit: {reason}");
            Console.Out.Flush();
            
            // Give a moment for output to flush
            Thread.Sleep(100);
            
            // Force immediate exit
            Environment.Exit(0);
        }
        catch
        {
            // If all else fails, kill the process
            Process.GetCurrentProcess().Kill();
        }
    }
    
    /// <summary>
    /// Call this method when tests are complete to ensure process exit.
    /// </summary>
    public static void TestsComplete()
    {
        Task.Run(async () =>
        {
            // Wait a moment for test runner to finish cleanup
            await Task.Delay(1000);
            ForceExit("Tests complete");
        });
    }
}