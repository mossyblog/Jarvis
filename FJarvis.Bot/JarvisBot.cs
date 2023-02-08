using Discord;
using Discord.WebSocket;
namespace JarvisBot;

/// <summary>
///  FJarvis.Bot is a Discord Bot that is designed to be a personal assistant for the user.
/// </summary>
public class JarvisBot
{
    // Lets create a connection to Discord
    private static DiscordSocketClient _client;
    
    // Lets create a constructor for our JarvisBotClient
    public JarvisBot()
    {
        // Lets check to see if the client is null, if it is create a single instance only of the _client
        _client ??= new DiscordSocketClient();
    }
    
    // Lets create a method to start the bot
    public async Task<ConnectionState> Connect()
    {
        // Lets connect to discord with the _client
        await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
        await _client.StartAsync();

        // Lets return whether or not _client is connected
        return _client.ConnectionState;
    }
    
}