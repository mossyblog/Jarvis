using Discord;

namespace JarvisBot.Tests;


/// <summary>
///  FJarvis.Bot is a Discord Bot that is designed to be a personal assistant for the user.
/// </summary>
public class DiscordTests
{
    [SetUp]
    public void Setup()
    {
        
    }

    [Test]
    public async Task JarvisBot_Should_ConnectToDiscord()
    {
        // Arrange
        var jarvisBotClient = new JarvisBot();
        
        // Act
        var result = await jarvisBotClient.Connect();
        
        // Assert
       // Assert.AreEqual(ConnectionState.Connected, result);
    }
}