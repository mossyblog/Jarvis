using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.tests.Helpers;
using Shouldly;
using Xunit;

namespace core.jarvis.tests.Integration;

public class SimpleAuthTest : IntegrationTestBase
{
    [Fact]
    public async Task Can_Create_Account_Component()
    {
        // Arrange
        var userEntityId = Guid.NewGuid();
        var account = new Account
        {
            OwnerEntityId = userEntityId,
            Email = "simple@test.com",
            PasswordHash = "hash",
            Password = "",
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Act
        await TestDataContext().Commit(account);
        
        // Assert
        var handler = TestDataContext().For<AccountHandler>(userEntityId);
        var retrieved = await handler.Get();
        retrieved.ShouldNotBeNull();
        retrieved.Email.ShouldBe("simple@test.com");
        
        // Track for cleanup
        TrackEntity(userEntityId);
    }
}