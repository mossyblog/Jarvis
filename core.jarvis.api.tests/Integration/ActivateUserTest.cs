using System;
using System.Threading.Tasks;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.tests.Helpers;
using core.jarvis.Exceptions;
using Xunit;
using Shouldly;

namespace core.jarvis.Scripts;

/// <summary>
/// UTILITY SCRIPT for manually activating a specific user account.
/// 
/// INTENT: This is NOT a test - it's a utility script for operational purposes
/// PURPOSE: Manually activate a specific user account created via curl or other means
/// BUSINESS CONTEXT: Sometimes accounts need manual activation for testing/debugging
/// WHY IMPORTANT: Provides a way to activate accounts without going through the full flow
/// ARCHITECTURAL SIGNIFICANCE: Demonstrates direct handler usage for administrative tasks
/// FUTURE RESILIENCE: Should be moved to a separate utilities/scripts project
/// 
/// USAGE: dotnet test --filter "FullyQualifiedName~ActivateSpecificUser" --logger console --verbosity normal
/// WARNING: This performs a real operation on a real account - use with caution!
/// </summary>
public class ActivateUserTest : IntegrationTestBase
{
    private const string TargetEntityId = "021536e2-035c-450b-95e0-27732100db46";
    private const string TargetEmail = "curltest@example.com";

    /// <summary>
    /// UTILITY SCRIPT: Activates a specific user account.
    /// 
    /// INTENT: Perform actual account activation, not test activation functionality
    /// PURPOSE: Administrative utility for activating the curltest@example.com account
    /// BUSINESS CONTEXT: Used for manual account management during development/debugging
    /// WHY IMPORTANT: Provides direct database manipulation capability for edge cases
    /// ARCHITECTURAL SIGNIFICANCE: Shows how handlers can be used for administrative tasks
    /// FUTURE RESILIENCE: Consider moving to a separate administrative tools project
    /// </summary>
    [Fact(Skip = "This is a utility script, not a test. Remove Skip to run manually.")]
    public async Task ActivateSpecificUser()
    {
        Console.WriteLine("=== User Activation Test ===");
        Console.WriteLine($"Target Entity ID: {TargetEntityId}");
        Console.WriteLine($"Target Email: {TargetEmail}");
        Console.WriteLine();

        // Parse the entity ID
        var entityGuid = Guid.Parse(TargetEntityId);
        
        // Get the account handler for this entity
        var accountHandler = TestDataContext().For<AccountHandler>(entityGuid);
        
        Console.WriteLine("1. Checking if account exists...");
        Account? existingAccount = null;
        try
        {
            existingAccount = await accountHandler.Get();
        }
        catch (EntityNotFoundException)
        {
            // Account doesn't exist
        }
        
        if (existingAccount == null)
        {
            Console.WriteLine($"❌ ERROR: No account found for entity ID {TargetEntityId}");
            throw new InvalidOperationException($"No account found for entity ID {TargetEntityId}");
        }
        
        Console.WriteLine($"✅ Account found:");
        Console.WriteLine($"   - Email: {existingAccount.Email}");
        Console.WriteLine($"   - IsActive: {existingAccount.IsActive}");
        Console.WriteLine($"   - CreatedAt: {existingAccount.CreatedAt}");
        Console.WriteLine();
        
        // Verify this is the correct account
        existingAccount.Email.ShouldBe(TargetEmail);
        
        if (existingAccount.IsActive)
        {
            Console.WriteLine("ℹ️  Account is already active. No action needed.");
            return;
        }
        
        // Activate the account
        Console.WriteLine("2. Activating account...");
        var activatedAccount = await accountHandler.Activate();
        
        Console.WriteLine($"✅ Account activated successfully!");
        Console.WriteLine($"   - Email: {activatedAccount.Email}");
        Console.WriteLine($"   - IsActive: {activatedAccount.IsActive}");
        Console.WriteLine($"   - LastUpdated: {activatedAccount.LastUpdated}");
        Console.WriteLine();
        
        // Verify activation
        activatedAccount.IsActive.ShouldBeTrue();
        activatedAccount.Email.ShouldBe(TargetEmail);
        
        // Verify activation by fetching again
        Console.WriteLine("3. Verifying activation...");
        Account? verifiedAccount = null;
        try
        {
            verifiedAccount = await accountHandler.Get();
        }
        catch (EntityNotFoundException)
        {
            // Account doesn't exist
        }
        
        verifiedAccount.ShouldNotBeNull();
        verifiedAccount.IsActive.ShouldBeTrue();
        verifiedAccount.Email.ShouldBe(TargetEmail);
        
        Console.WriteLine("✅ Activation verified! User is now active.");
        
        // Note: We don't track this entity for cleanup since it's a real user account
        // that was created via curl and should persist
    }
}