using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        };

        var _client = new DiscordSocketClient(config);
        var services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton<CommandHandler>()
            .AddSingleton<CommandService>()
            .BuildServiceProvider();
        
        
        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            var guild = _client.Guilds.FirstOrDefault();
            var voice = guild?.GetUser(_client.CurrentUser.Id)?.VoiceChannel;
            voice?.DisconnectAsync().GetAwaiter().GetResult();
        };

        var handler = services.GetRequiredService<CommandHandler>();
        await handler.InstallCommandsAsync();

        var token = File.ReadAllText($"{Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent}\\token.txt");
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        await Task.Delay(-1);
    }
}

