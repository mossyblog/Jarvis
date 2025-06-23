using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Services;

/// <summary>
/// Service to prevent timing attacks by ensuring operations take constant time.
/// </summary>
public interface IConstantTimeService
{
    /// <summary>
    /// Compares two strings in constant time to prevent timing attacks.
    /// </summary>
    bool ConstantTimeEquals(string? a, string? b);
    
    /// <summary>
    /// Wraps an operation to ensure it takes a minimum amount of time.
    /// </summary>
    Task<T> ExecuteWithMinimumTime<T>(Func<Task<T>> operation, int minimumMilliseconds = 100);
    
    /// <summary>
    /// Adds cryptographically secure random delay.
    /// </summary>
    Task AddRandomDelay(int minMilliseconds = 50, int maxMilliseconds = 150);
}

/// <summary>
/// Implementation of constant-time operations to prevent timing attacks.
/// </summary>
public class ConstantTimeService : IConstantTimeService
{
    private readonly ILogger<ConstantTimeService> _logger;
    private readonly RandomNumberGenerator _rng;

    public ConstantTimeService(ILogger<ConstantTimeService> logger)
    {
        _logger = logger;
        _rng = RandomNumberGenerator.Create();
    }

    /// <summary>
    /// Performs constant-time string comparison to prevent timing attacks.
    /// </summary>
    public bool ConstantTimeEquals(string? a, string? b)
    {
        // Handle null cases
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        // Convert to byte arrays for comparison
        var aBytes = System.Text.Encoding.UTF8.GetBytes(a);
        var bBytes = System.Text.Encoding.UTF8.GetBytes(b);

        // Make both arrays the same length to ensure constant time
        var maxLength = Math.Max(aBytes.Length, bBytes.Length);
        Array.Resize(ref aBytes, maxLength);
        Array.Resize(ref bBytes, maxLength);

        // Compare in constant time
        var result = 0;
        for (int i = 0; i < maxLength; i++)
        {
            result |= aBytes[i] ^ bBytes[i];
        }

        return result == 0 && a.Length == b.Length;
    }

    /// <summary>
    /// Executes an operation with a minimum execution time to prevent timing analysis.
    /// </summary>
    public async Task<T> ExecuteWithMinimumTime<T>(Func<Task<T>> operation, int minimumMilliseconds = 100)
    {
        if (minimumMilliseconds <= 0)
        {
            throw new ArgumentException("Minimum time must be positive", nameof(minimumMilliseconds));
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Execute the operation
            var result = await operation();
            
            // Calculate remaining time
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;
            
            if (elapsed < minimumMilliseconds)
            {
                // Add delay to reach minimum time
                var remaining = minimumMilliseconds - (int)elapsed;
                await Task.Delay(remaining);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            // Even on exception, ensure minimum time
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;
            
            if (elapsed < minimumMilliseconds)
            {
                var remaining = minimumMilliseconds - (int)elapsed;
                await Task.Delay(remaining);
            }
            
            _logger.LogError(ex, "Error during constant-time operation");
            throw;
        }
    }

    /// <summary>
    /// Adds a cryptographically secure random delay to operations.
    /// This helps prevent statistical timing analysis across multiple requests.
    /// </summary>
    public async Task AddRandomDelay(int minMilliseconds = 50, int maxMilliseconds = 150)
    {
        if (minMilliseconds < 0 || maxMilliseconds < minMilliseconds)
        {
            throw new ArgumentException("Invalid delay range");
        }

        // Generate cryptographically secure random delay
        var range = maxMilliseconds - minMilliseconds;
        if (range == 0)
        {
            await Task.Delay(minMilliseconds);
            return;
        }

        var randomBytes = new byte[4];
        _rng.GetBytes(randomBytes);
        var randomValue = BitConverter.ToUInt32(randomBytes, 0);
        var delay = minMilliseconds + (int)(randomValue % (uint)range);
        
        await Task.Delay(delay);
    }

    public void Dispose()
    {
        _rng?.Dispose();
    }
}