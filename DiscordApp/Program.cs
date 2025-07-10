using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordApp.Services;
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
            .AddTransient<AudioStreamService>()
            .AddSingleton<MusicQueueService>()
            .AddSingleton <VoiceStateService>()
            .AddSingleton<MusicPlayerService>()
            .BuildServiceProvider();
        
        var handler = services.GetRequiredService<CommandHandler>();
        await handler.InstallCommandsAsync();

        var token = Environment.GetEnvironmentVariable("token");
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        await Task.Delay(-1);
    }
}

