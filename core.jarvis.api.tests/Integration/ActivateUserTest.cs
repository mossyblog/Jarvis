using System;
using System.Threading.Tasks;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.tests.Helpers;
using Xunit;
using Shouldly;

namespace core.jarvis.Scripts;

/// <summary>
/// This is actually a test that can be run to activate the specific user.
/// Run with: dotnet test --filter "FullyQualifiedName~ActivateSpecificUser" --logger console --verbosity normal
/// </summary>
public class ActivateUserTest : IntegrationTestBase
{
    private const string TargetEntityId = "021536e2-035c-450b-95e0-27732100db46";
    private const string TargetEmail = "curltest@example.com";

    [Fact]
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
        var existingAccount = await accountHandler.GetOrDefault();
        
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
        var verifiedAccount = await accountHandler.GetOrDefault();
        
        verifiedAccount.ShouldNotBeNull();
        verifiedAccount.IsActive.ShouldBeTrue();
        verifiedAccount.Email.ShouldBe(TargetEmail);
        
        Console.WriteLine("✅ Activation verified! User is now active.");
        
        // Note: We don't track this entity for cleanup since it's a real user account
        // that was created via curl and should persist
    }
}